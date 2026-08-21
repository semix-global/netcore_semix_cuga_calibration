using System.Diagnostics;
using System.IO;
using System.Reflection;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.AODWaveform.Generates;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.Logging;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.WPF.Enums;
using Python.Runtime;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public abstract partial class AbstractAODWaveformElectrodeOffsetWindowViewModel<TCache, TItem, TResult> : AbstractAODWaveformCommonWindowViewModel<TCache, TItem, TResult>
    where TCache : AODWaveformElectrodeOffsetCache<TItem, TResult>, new()
    where TItem : AODWaveformElectrodeOffsetItem, new()
    where TResult : AODWaveformElectrodeOffsetResult, new()
{
    public string PhaseOptimizerStateFilePath => Path.Combine(ApplicationSetting.AppHomeDirectory, "Python", GetType().Name, "optimizer_state.pkl");

    public override string[] Steps { get; } =
    [
        "Step 1 Measure Noise",
        "Step 2 Electrode Offset",
        "Step 3 Generate AOD Waveform",
        "Step 4 Set AOD Waveform Config"
    ];

    private double? _lastCost;

    protected override void Closing()
    {
        TestSetResultAODWaveformConfigurationCancelCommand.Execute(null);

        AllCancelCommand.Execute(null);

        StepFirstLastCancelCommand.Execute(null);
        StepSecondLastCancelCommand.Execute(null);
        Step1CancelCommand.Execute(null);
        Step0CancelCommand.Execute(null);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0Async(bool isSilent, CancellationToken cancellationToken)
    {
        const int stepIndex = 0;

        return await InvokeAsync(stepIndex, async () =>
        {
            Guard.IsGreaterThan(Cache.DefaultAmplitude, 0);
            Guard.IsGreaterThan(Cache.WaitTime, 0);
            Guard.IsGreaterThan(Cache.TotalMeasurePower, 0);
            Guard.IsGreaterThan(Cache.MeasurePowerTimes, 0);

            Guard.IsGreaterThan(Cache.OffsetFrequency, 0);
            Guard.IsTrue(Cache.Frequencies.IsIncreasing(true));
            Guard.IsGreaterThanOrEqualTo(Cache.Frequencies.Length, 2);

            Guard.IsGreaterThan(Cache.NoiseMeasureTimes, 1);
            Guard.IsNotEmpty(Cache.ElectrodeOffsetFrequencyPeriodParams);

            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.MeasureMaxPowerMachinePosition);
            LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
            OpticsViewModel.ToggleODFilter(false);

            if (isSilent || (DialogWindowProvider.TryShowDialog(
                    "Yes: reset noise measure state. No: continue from the existing state.",
                    out var dialogResult,
                    DialogButtonsEnum.YesNo,
                    DialogIconEnum.Question) == true && dialogResult == DialogResultEnum.Yes))
            {
                Cache.Step0 = new AODWaveformElectrodeOffsetFrequencyPeriod<TItem>();
            }

            Cache.Noise = 0d;

            var isSuccess = false;

            try
            {
                for (var index = 0; index < Cache.NoiseMeasureTimes; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Logger.LogHtmlInformation($"{index + 1}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    var item = new AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem>
                    {
                        OffsetFrequencyPeriodCoefficients = Generate.Repeat(Cache.ElectrodeOffsetFrequencyPeriodParams.Length, 0d)
                    };

                    Cache.Step0.Items = [.. Cache.Step0.Items, item];

                    var isCurrentFrequenciesOk = false;
                    try
                    {
                        await UpdateElectrodeOffsetFrequencyPeriodItemAsync(item, null, HtmlLogUniqueId, cancellationToken).ConfigureAwait(false);

                        isCurrentFrequenciesOk = true;
                    }
                    finally
                    {
                        if (isCurrentFrequenciesOk == false) Cache.Step0.Items = [.. Cache.Step0.Items.AsSpan()[..^1]];

                        foreach (var temp in Cache.Step0.Items) temp.IsSelected = false;
                    }
                }

                var standardDeviation = Cache.Step0.Items.Select(t => t.Score).StandardDeviation();
                Cache.Noise = standardDeviation * standardDeviation;

                isSuccess = true;
            }
            finally
            {
                var htmlBullet = new HtmlBullet(new
                {
                    Cache.Noise,
                    Step1Items = new HtmlTable([.. Cache.Step0.Items.Select(t => t.ToHtmlAnonymous())]),
                    Step1Plot = new HtmlContainer([.. Cache.Step0.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
                });

                if (isSuccess)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
            }

            return isSuccess;
        }, isSilent).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1Async(bool isSilent, CancellationToken cancellationToken)
    {
        const int stepIndex = 1;

        return await InvokeAsync(stepIndex, async () =>
        {
            Guard.IsGreaterThan(Cache.DefaultAmplitude, 0);
            Guard.IsGreaterThan(Cache.WaitTime, 0);
            Guard.IsGreaterThan(Cache.TotalMeasurePower, 0);
            Guard.IsGreaterThan(Cache.MeasurePowerTimes, 0);

            Guard.IsGreaterThan(Cache.OffsetFrequency, 0);
            Guard.IsTrue(Cache.Frequencies.IsIncreasing(true));
            Guard.IsGreaterThanOrEqualTo(Cache.Frequencies.Length, 2);

            Guard.IsNotEmpty(Cache.ElectrodeOffsetFrequencyPeriodParams);

            Guard.IsGreaterThanOrEqualTo(Cache.Noise, 0d);

            Guard.IsGreaterThan(Cache.AlgorithmInitialPoints, 0);
            Guard.IsGreaterThan(Cache.AlgorithmEarlyStop, 0);
            Guard.IsGreaterThan(Cache.AlgorithmRetryTimes, 0);

            Guard.IsGreaterThan(Cache.DetailLogInterval, 0);

            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.MeasureMaxPowerMachinePosition);
            LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
            OpticsViewModel.ToggleODFilter(false);

            if (isSilent || (DialogWindowProvider.TryShowDialog(
                    "Yes: reset the algorithm phase optimizer state. No: continue from the existing state.",
                    out var dialogResult,
                    DialogButtonsEnum.YesNo,
                    DialogIconEnum.Question) == true && dialogResult == DialogResultEnum.Yes))
            {
                _lastCost = null;

                if (System.IO.File.Exists(PhaseOptimizerStateFilePath))
                {
                    var backupFilePath = $"{PhaseOptimizerStateFilePath}_{DateTime.Now.ToString(Constants.LongFileDateTimeFormat)}";
                    System.IO.File.Move(PhaseOptimizerStateFilePath, backupFilePath);
                }

                Cache.Step1 = new AODWaveformElectrodeOffsetFrequencyPeriod<TItem>();
            }

            Cache.ElectrodeConfigurationResults =
            [
                .. Cache.ElectrodeOffsetFrequencyPeriodParams.Select(t => new GenerateAODWaveformElectrodeConfiguration
                {
                    OpticsAODElectrodeEnum = t.OpticsAODElectrodeEnum,
                    OffsetFrequency = Cache.OffsetFrequency,
                    OffsetFrequencyPeriodCoefficient = 0d,
                    Amplitude = 1d, // 生成result默认幅值都是1
                    IsGenerateAODWaveformZero = false
                })
            ];

            if (Cache.ElectrodeOffsetFrequencyPeriodParams.Length == 1) return true;

            var isSuccess = false;
            Guid? detailLogUniqueId = null;
            string? detailLogFileName = null;

            try
            {
                var times = 0;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var currentDetailLogUniqueId = detailLogUniqueId ?? StartDetailLog(times);

                    Logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header3, currentDetailLogUniqueId.LoggingHtml());

                    try
                    {
                        (isSuccess, var phases, var uniformities) = AlgorithmSuggest(_lastCost, Cache.ElectrodeOffsetFrequencyPeriodParams.Length - 1);
                        if (isSuccess) break;

                        var linearSpaced = Generate.LinearSpaced(uniformities.Length, Cache.Frequencies.Min(), Cache.Frequencies.Max());
                        var linearSpline = LinearSpline.InterpolateSorted(linearSpaced, uniformities);

                        var item = new AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem>
                        {
                            OffsetFrequencyPeriodCoefficients =
                            [
                                .. Cache.ElectrodeOffsetFrequencyPeriodParams
                                    .Index()
                                    .Select(t => t.Index == 0
                                        ? 0d
                                        : Generate.LinearRangeInt32(0, t.Index - 1).Sum(tt => phases[tt]) + t.Item.BoardCardOffsetFrequencyPeriodCoefficient)
                            ]
                        };

                        Cache.Step1.Items = [.. Cache.Step1.Items, item];

                        var isCurrentFrequenciesOk = false;
                        try
                        {
                            await UpdateElectrodeOffsetFrequencyPeriodItemAsync(item, linearSpline, currentDetailLogUniqueId, cancellationToken).ConfigureAwait(false);

                            isCurrentFrequenciesOk = true;
                        }
                        finally
                        {
                            if (isCurrentFrequenciesOk == false) Cache.Step1.Items = [.. Cache.Step1.Items.AsSpan()[..^1]];

                            foreach (var temp in Cache.Step1.Items) temp.IsSelected = false;

                            if (Cache.Step1.Items.Length > 0)
                            {
                                var bestScoreItem = Cache.Step1.Items.Maxima(t => t.Score).First();
                                bestScoreItem.IsSelected = true;

                                foreach (var (index, result) in Cache.ElectrodeConfigurationResults.Index())
                                {
                                    result.OffsetFrequencyPeriodCoefficient = bestScoreItem.OffsetFrequencyPeriodCoefficients[index];
                                    result.UniformityConfigurations =
                                    [
                                        .. bestScoreItem.FrequencyItems.Select(t => new GenerateAODWaveformUniformityConfiguration
                                        {
                                            Frequency = t.Frequency,
                                            Coefficient = t.Amplitude
                                        })
                                    ];
                                }
                            }
                        }

                        _lastCost = -item.Score;

                        if (++times > Cache.AlgorithmRetryTimes - 1)
                        {
                            isSuccess = false;
                            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("More than the number of times."), HtmlLogUniqueId.LoggingHtml());

                            break;
                        }
                    }
                    finally
                    {
                        if (times % Cache.DetailLogInterval == 0)
                        {
                            Guard.IsNotNull(detailLogUniqueId);
                            Guard.IsNotNull(detailLogFileName);

                            EndDetailLog();
                        }
                    }
                }
            }
            finally
            {
                EndDetailLog();

                var htmlBullet = new HtmlBullet(new
                {
                    Cache.Noise,
                    Table = new HtmlTable([.. Cache.ElectrodeConfigurationResults.Select(t => t.ToHtmlAnonymous())]),
                    Step2Items = new HtmlTable([.. Cache.Step1.Items.Select(t => t.ToHtmlAnonymous())]),
                    Step2Plot = new HtmlContainer([.. Cache.Step1.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
                });

                if (isSuccess)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
            }

            return isSuccess;

            Guid StartDetailLog(int times)
            {
                var startTimes = times + 1;
                var stopTimes = Math.Min(times + Cache.DetailLogInterval, Cache.AlgorithmRetryTimes);
                var currentDetailLogUniqueId = Guid.NewGuid();

                detailLogUniqueId = currentDetailLogUniqueId;
                detailLogFileName = $"Details_{Steps[stepIndex].Replace(" ", string.Empty)}_{startTimes}-{stopTimes}";

                var title = $"{startTimes}-{stopTimes}";
                Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {detailLogFileName}({currentDetailLogUniqueId:N})"), HtmlLogUniqueId.LoggingHtml());
                Logger.LogHtmlInformation($"{currentDetailLogUniqueId:N}", HtmlHeaderLevelEnum.Header1, new HtmlComment(Name), currentDetailLogUniqueId.LoggingHtml());

                return currentDetailLogUniqueId;
            }

            void EndDetailLog()
            {
                if (detailLogUniqueId is null || detailLogFileName is null)
                {
                    Guard.IsNull(detailLogUniqueId);
                    Guard.IsNull(detailLogFileName);

                    return;
                }

                try
                {
                    Logger.LogHtmlInformation(detailLogUniqueId.Value.LoggedEndHtml(detailLogFileName));
                }
                finally
                {
                    detailLogUniqueId = null;
                    detailLogFileName = null;
                }
            }
        }, isSilent).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task AllAsync(CancellationToken cancellationToken)
    {
#if NET
        await
#endif
        using var _ = cancellationToken.Register(() =>
        {
            if (Step0Command.CanBeCanceled) Step0Command.Cancel();
            if (Step1Command.CanBeCanceled) Step1Command.Cancel();
            if (StepSecondLastCommand.CanBeCanceled) StepSecondLastCommand.Cancel();
            if (StepFirstLastCommand.CanBeCanceled) StepFirstLastCommand.Cancel();
        });

        var step0Task = Guard.IsAssignableToTypeAndReturn<Task<bool>>(Step0Command.ExecuteAsync( /* isSilent */ true));
        if (await step0Task == false) return;

        var step1Task = Guard.IsAssignableToTypeAndReturn<Task<bool>>(Step1Command.ExecuteAsync( /* isSilent */ true));
        if (await step1Task == false) return;

        var stepSecondLastTask = Guard.IsAssignableToTypeAndReturn<Task<bool>>(StepSecondLastCommand.ExecuteAsync( /* isSilent */ true));
        if (await stepSecondLastTask == false) return;

        await StepFirstLastCommand.ExecuteAsync( /* isSilent */ true);
    }

    private async Task UpdateElectrodeOffsetFrequencyPeriodItemAsync(
        AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem> frequencyPeriodItem,
        LinearSpline? linearSpline,
        Guid htmlLogUniqueId,
        CancellationToken cancellationToken)
    {
        foreach (var frequency in Cache.Frequencies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var amplitude = linearSpline?.Interpolate(frequency) * Cache.DefaultAmplitude ?? Cache.DefaultAmplitude;

            Logger.LogHtmlInformation($"{frequency}(MHz)-[{amplitude}]", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

            var item = new TItem
            {
                ElectrodeConfigurations =
                [
                    .. Cache.ElectrodeOffsetFrequencyPeriodParams
                        .Index()
                        .Select(t => new GenerateAODWaveformElectrodeConfiguration
                        {
                            OpticsAODElectrodeEnum = t.Item.OpticsAODElectrodeEnum,
                            OffsetFrequency = Cache.OffsetFrequency,
                            OffsetFrequencyPeriodCoefficient = frequencyPeriodItem.OffsetFrequencyPeriodCoefficients[t.Index],
                            Amplitude = amplitude,
                            IsGenerateAODWaveformZero = false,
                            UniformityConfigurations = []
                        })
                ],
                Frequency = frequency,
                Amplitude = amplitude
            };

            await UpdateMeasurePowerAsync(item, htmlLogUniqueId, cancellationToken).ConfigureAwait(false);

            frequencyPeriodItem.FrequencyItems = [.. frequencyPeriodItem.FrequencyItems, item];
        }

        var vector = 10d * (Vector<double>.Build.Dense([.. frequencyPeriodItem.FrequencyItems.Select(t => t.MeasurePower)]) / Cache.TotalMeasurePower).PointwiseLog10();
        frequencyPeriodItem.Score = vector.Average() - Cache.ScoreLambda * vector.StandardDeviation() - Cache.ScoreGamma * (vector.Max() - vector.Min());
    }

    private (bool IsSuccess, double[] Phases, double[] Uniformities) AlgorithmSuggest(double? previousCost, int phaseCount)
    {
        var timestamp = Stopwatch.GetTimestamp();

        try
        {
            Logger.LogTrace("Algorithm Suggest Starting...");

            DirectoryHelper.CreateFileDirectoryIfNotExists(PhaseOptimizerStateFilePath);

            using var _ = Py.GIL();
            using var module = PyModule.FromString("phase_optimizer", AODWaveformElectrodeOffsetWindowViewModelShared.PhaseOptimizerPythonScript);

            using var pyPhaseOptimizerStateFilePath = PhaseOptimizerStateFilePath.ToPython();
            module.SetAttr("_STATE_FILE", pyPhaseOptimizerStateFilePath);

            using var suggest = module.GetAttr("suggest");

            using var pyCost = previousCost.ToPython();
            using var pyPhaseCount = (phaseCount + Cache.AlgorithmUniformityCount).ToPython();
            using var pyInitialPoints = Cache.AlgorithmInitialPoints.ToPython();
            using var pyNoise = Cache.Noise.ToPython();
            using var pyEarlyStop = Cache.AlgorithmEarlyStop.ToPython();
            using var pyAcquisitionFunction = Cache.AlgorithmAcquisitionFunctionEnum.ToString().ToPython();
            using var result = suggest.Invoke(pyCost, pyPhaseCount, pyInitialPoints, pyNoise, pyEarlyStop, pyAcquisitionFunction);

            using var pyPhases = Guard.IsNotNullAndReturn(result["x"]);
            using var pyDone = Guard.IsNotNullAndReturn(result["done"]);

            var doubles = ToDoubles(pyPhases);
            var phases = doubles.AsSpan()[..phaseCount].ToArray();
            var uniformities = doubles.AsSpan()[phaseCount..].ToArray();

            return (pyDone.As<bool>(), phases, uniformities);
        }
        finally
        {
            Logger.LogTrace("Algorithm Suggest Stopped: {TotalMilliseconds}ms", Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);
        }
    }

    private static double[] ToDoubles(PyObject pyValues)
    {
        using var values = new PyList(pyValues);
        var result = new double[values.Length()];

        for (var index = 0; index < result.Length; index++)
        {
            using var value = Guard.IsNotNullAndReturn(values[index]);

            result[index] = value.As<double>();
        }

        return result;
    }
}

public static class AODWaveformElectrodeOffsetWindowViewModelShared
{
    public static readonly string PhaseOptimizerPythonScript = GetEmbeddedResource("CugaCalibration.Assets.Python.phase_optimizer.py");

    private static string GetEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null) ThrowHelper.ThrowArgumentException($"Resource '{resourceName}' not found in assembly '{assembly.FullName}'.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}