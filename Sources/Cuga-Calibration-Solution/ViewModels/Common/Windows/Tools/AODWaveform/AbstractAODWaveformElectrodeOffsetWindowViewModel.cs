using System.IO;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.AODWaveform.Generates;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using Net.Utilities.Algorithms.Extensions;
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
    private double? _lastCost;

    public string PhaseOptimizerStateFilePath => Path.Combine(ApplicationSetting.AppHomeDirectory, "Python", "PhaseOptimizer", "optimizer_state.json");

    public override string[] Steps { get; } =
    [
        "Step 1 Electrode Offset",
        "Step 2 Uniformity",
        "Step 3 Generate AOD Waveform",
        "Step 4 Set AOD Waveform Config"
    ];

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0Async(bool isNotSilent, CancellationToken cancellationToken)
    {
        const int stepIndex = 0;

        return await InvokeAsync(stepIndex, async () =>
        {
            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.MeasureMaxPowerMachinePosition);
            LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
            OpticsViewModel.ToggleODFilter(false);

            Guard.IsGreaterThan(Cache.OffsetFrequency, 0);
            Guard.IsTrue(Cache.Frequencies.IsIncreasing(true));
            Guard.IsGreaterThanOrEqualTo(Cache.Frequencies.Length, 2);

            Guard.IsGreaterThan(Cache.TotalMeasurePower, 0);
            Guard.IsGreaterThan(Cache.AlgorithmInitialPoints, 0);
            Guard.IsGreaterThan(Cache.AlgorithmNoise, 0d);
            Guard.IsGreaterThan(Cache.AlgorithmEarlyStop, 0);
            Guard.IsGreaterThan(Cache.AlgorithmRandomState, 0);
            Guard.IsGreaterThan(Cache.AlgorithmRetryTimes, 0);
            Guard.IsGreaterThan(Cache.DetailLogInterval, 0);

            Guard.IsNotEmpty(Cache.ElectrodeOffsetFrequencyPeriodParams);

            if (DialogWindowProvider.TryShowDialog(
                    "Yes: reset the algorithm phase optimizer state. No: continue from the existing state.",
                    out var dialogResult,
                    DialogButtonsEnum.YesNo,
                    DialogIconEnum.Question) == true && dialogResult == DialogResultEnum.Yes)
            {
                _lastCost = null;

                if (System.IO.File.Exists(PhaseOptimizerStateFilePath))
                {
                    var backupFilePath = $"{PhaseOptimizerStateFilePath}_{DateTime.Now.ToString(Constants.LongFileDateTimeFormat)}";
                    System.IO.File.Move(PhaseOptimizerStateFilePath, backupFilePath);
                }

                Cache.Step0 = new AODWaveformElectrodeOffsetFrequencyPeriod<TItem>();
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

            var linearSplines = Cache.ElectrodeOffsetFrequencyPeriodParams.Select(t => t.UniformityConfigurations.Length > 0
                    ? LinearSpline.InterpolateSorted(
                        [.. t.UniformityConfigurations.Select(configuration => configuration.Frequency)],
                        [.. t.UniformityConfigurations.Select(configuration => configuration.Coefficient)])
                    : null)
                .ToArray();

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
                        var (phases, isDone) = AlgorithmSuggest(
                            _lastCost,
                            Cache.ElectrodeOffsetFrequencyPeriodParams.Length - 1,
                            Cache.AlgorithmInitialPoints,
                            Cache.AlgorithmNoise,
                            Cache.AlgorithmEarlyStop,
                            Cache.AlgorithmRandomState);

                        if (isDone)
                        {
                            isSuccess = true;

                            break;
                        }

                        var aodWaveformElectrodeOffsetFrequencyPeriodItem = new AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem>
                        {
                            OffsetFrequencyPeriodCoefficients =
                            [
                                .. Cache.ElectrodeOffsetFrequencyPeriodParams
                                    .Index()
                                    .Select(t => t.Index == 0
                                        ? 0d
                                        : Generate.LinearRangeInt32(0, t.Index - 1).Sum(tt => phases[tt]) / (2d * Math.PI) + t.Item.BoardCardOffsetFrequencyPeriodCoefficient)
                            ]
                        };

                        Cache.Step0.Items = [.. Cache.Step0.Items, aodWaveformElectrodeOffsetFrequencyPeriodItem];

                        var isCurrentFrequenciesOk = false;
                        try
                        {
                            foreach (var frequency in Cache.Frequencies)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, currentDetailLogUniqueId.LoggingHtml());

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
                                                OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetFrequencyPeriodItem.OffsetFrequencyPeriodCoefficients[t.Index],
                                                Amplitude = linearSplines[t.Index]?.Interpolate(frequency) ?? Cache.DefaultAmplitude,
                                                IsGenerateAODWaveformZero = false,
                                                UniformityConfigurations = []
                                            })
                                    ],
                                    Frequency = frequency,
                                    Amplitude = Cache.DefaultAmplitude
                                };

                                await UpdateMeasurePowerAsync(item, currentDetailLogUniqueId, cancellationToken).ConfigureAwait(false);

                                aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems = [.. aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems, item];
                            }

                            isCurrentFrequenciesOk = true;
                        }
                        finally
                        {
                            if (isCurrentFrequenciesOk == false) Cache.Step0.Items = [.. Cache.Step0.Items.AsSpan()[..^1]];
                        }

                        var vector = 10d * (Vector<double>.Build.Dense([.. aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems.Select(t => t.MeasurePower)]) / Cache.TotalMeasurePower).PointwiseLog10();
                        aodWaveformElectrodeOffsetFrequencyPeriodItem.Score = vector.Average() - Cache.AlgorithmLambda * vector.StandardDeviation();

                        _lastCost = -aodWaveformElectrodeOffsetFrequencyPeriodItem.Score;

                        if (++times > Cache.AlgorithmRetryTimes - 1)
                        {
                            isSuccess = false;
                            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("More than the number of times."), HtmlLogUniqueId.LoggingHtml());

                            break;
                        }
                    }
                    finally
                    {
                        if (times % Cache.DetailLogInterval == 0) EndDetailLog();
                    }
                }

                var bestScoreItem = Cache.Step0.Items.Maxima(t => t.Score).First();

                foreach (var (index, result) in Cache.ElectrodeConfigurationResults.Index())
                {
                    result.OffsetFrequencyPeriodCoefficient = bestScoreItem.OffsetFrequencyPeriodCoefficients[index];
                }
            }
            finally
            {
                EndDetailLog();

                var htmlBullet = new HtmlBullet(new
                {
                    Table = new HtmlTable([.. Cache.ElectrodeConfigurationResults.Select(t => t.ToHtmlAnonymous())]),
                    Step0Items = new HtmlTable([.. Cache.Step0.Items.Select(t => t.ToHtmlAnonymous())]),
                    Step0Plot = new HtmlContainer([.. Cache.Step0.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
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
                detailLogFileName = $"Details_{Steps[stepIndex].Replace(" ", string.Empty)}_{string.Join("_", Cache.ElectrodeOffsetFrequencyPeriodParams.Select(t => t.OpticsAODElectrodeEnum))}_{startTimes}-{stopTimes}";

                var title = $"{startTimes}-{stopTimes}";
                Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {detailLogFileName}({currentDetailLogUniqueId:N})"), HtmlLogUniqueId.LoggingHtml());
                Logger.LogHtmlInformation($"{currentDetailLogUniqueId:N}", HtmlHeaderLevelEnum.Header1, new HtmlComment(Name), currentDetailLogUniqueId.LoggingHtml());

                return currentDetailLogUniqueId;
            }

            void EndDetailLog()
            {
                Guard.IsNotNull(detailLogUniqueId);
                Guard.IsNotNull(detailLogFileName);

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
        }, isNotSilent).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1Async(bool isNotSilent, CancellationToken cancellationToken)
    {
        const int stepIndex = 1;

        return await InvokeAsync(stepIndex, async () =>
        {
            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.MeasureMaxPowerMachinePosition);
            LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
            OpticsViewModel.ToggleODFilter(false);

            Guard.IsGreaterThan(Cache.OffsetFrequency, 0);
            Guard.IsTrue(Cache.Frequencies.IsIncreasing(true));
            Guard.IsGreaterThanOrEqualTo(Cache.Frequencies.Length, 2);

            Guard.IsNotEmpty(Cache.ElectrodeOffsetFrequencyPeriodParams);

            Guard.IsGreaterThan(Cache.ElectrodeOffsetFrequencyUniformityParamStepFrequency, 0);
            Guard.IsGreaterThan(Cache.ElectrodeOffsetFrequencyUniformityParamChunkSize, 0);

            Guard.IsEqualTo(Cache.ElectrodeConfigurationResults.Length, Cache.ElectrodeOffsetFrequencyPeriodParams.Length);
            Guard.IsNotEmpty(Cache.ElectrodeOffsetFrequencyUniformityParams);
            Guard.IsTrue(Cache.ElectrodeOffsetFrequencyUniformityParams.All(t => Cache.ElectrodeOffsetFrequencyPeriodParams.Any(tt => t.OpticsAODElectrodeEnum == tt.OpticsAODElectrodeEnum)));
            Guard.IsGreaterThanOrEqualTo(Cache.ElectrodeOffsetFrequencyUniformityParams.Length, Cache.ElectrodeOffsetFrequencyUniformityParamChunkSize);

            Cache.Step1Items = [];
            foreach (var electrodeConfiguration in Cache.ElectrodeConfigurationResults) electrodeConfiguration.UniformityConfigurations = [];

            var isSuccess = false;

            try
            {
                var frequencies = Generate.LinearRangeContainsEdge(Cache.Frequencies[0], Cache.ElectrodeOffsetFrequencyUniformityParamStepFrequency, Cache.Frequencies[^1]);
                Guard.IsNotEmpty(frequencies);

                foreach (var electrodeFrequencyUniformityParams in Cache.ElectrodeOffsetFrequencyUniformityParams.Chunk(Cache.ElectrodeOffsetFrequencyUniformityParamChunkSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var electrodes = electrodeFrequencyUniformityParams.Select(t => t.OpticsAODElectrodeEnum).ToArray();

                    var aodWaveformElectrodeOffsetFrequencyUniformity = new AODWaveformElectrodeOffsetFrequencyUniformity<TItem> { Electrodes = electrodes };
                    Cache.Step1Items = [.. Cache.Step1Items, aodWaveformElectrodeOffsetFrequencyUniformity];

                    var detailLogUniqueId = Guid.NewGuid();
                    var detailLogFileName = $"Details_{Steps[stepIndex].Replace(" ", string.Empty)}_{string.Join("_", aodWaveformElectrodeOffsetFrequencyUniformity.Electrodes)}";
                    Logger.LogHtmlInformation(aodWaveformElectrodeOffsetFrequencyUniformity.Title, HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {detailLogFileName}({detailLogUniqueId:N})"), HtmlLogUniqueId.LoggingHtml());
                    Logger.LogHtmlInformation($"{detailLogUniqueId:N}", HtmlHeaderLevelEnum.Header1, new HtmlComment(Name), detailLogUniqueId.LoggingHtml());

                    try
                    {
                        foreach (var frequency in frequencies)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var aodWaveformElectrodeOffsetFrequencyUniformityItem = new AODWaveformElectrodeOffsetFrequencyUniformityItem<TItem>();
                            aodWaveformElectrodeOffsetFrequencyUniformity.Items = [.. aodWaveformElectrodeOffsetFrequencyUniformity.Items, aodWaveformElectrodeOffsetFrequencyUniformityItem];

                            Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, detailLogUniqueId.LoggingHtml());

                            var amplitudes = Generate.LinearRangeContainsEdge(electrodeFrequencyUniformityParams[0].StartAmplitude, electrodeFrequencyUniformityParams[0].StepAmplitude, electrodeFrequencyUniformityParams[0].StopAmplitude)
                                .AsEnumerable()
                                .Reverse()
                                .ToArray();
                            Guard.IsNotEmpty(amplitudes);

                            foreach (var amplitude in amplitudes)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                var item = new TItem
                                {
                                    ElectrodeConfigurations =
                                    [
                                        .. Cache.ElectrodeConfigurationResults
                                            .Select(t =>
                                            {
                                                if (electrodes.Contains(t.OpticsAODElectrodeEnum))
                                                {
                                                    return t.Clone()
                                                        .WithAmplitude(amplitude)
                                                        .WithUniformityConfigurations([]);
                                                }

                                                var uniformityConfigurationResults = Cache.ElectrodeConfigurationResults
                                                    .Single(tt => tt.OpticsAODElectrodeEnum == t.OpticsAODElectrodeEnum)
                                                    .UniformityConfigurations;

                                                return t.Clone()
                                                    .WithAmplitude(uniformityConfigurationResults.Count > 0
                                                        ? uniformityConfigurationResults.Single(tt => Equals(tt.Frequency, frequency)).Coefficient
                                                        : Cache.DefaultAmplitude)
                                                    .WithUniformityConfigurations([]);
                                            })
                                    ],
                                    Amplitude = amplitude,
                                    Frequency = frequency
                                };

                                Logger.LogHtmlInformation($"{item.Amplitude}(AMP)", HtmlHeaderLevelEnum.Header5, detailLogUniqueId.LoggingHtml());

                                await UpdateMeasurePowerAsync(item, detailLogUniqueId, cancellationToken).ConfigureAwait(false);

                                aodWaveformElectrodeOffsetFrequencyUniformityItem.FrequencyItems = [.. aodWaveformElectrodeOffsetFrequencyUniformityItem.FrequencyItems, item];
                            }
                        }
                    }
                    finally
                    {
                        Logger.LogHtmlInformation(detailLogUniqueId.LoggedEndHtml(detailLogFileName));
                    }

                    foreach (var electrodeConfiguration in Cache.ElectrodeConfigurationResults.Where(t => electrodes.Contains(t.OpticsAODElectrodeEnum)))
                    {
                        var points = aodWaveformElectrodeOffsetFrequencyUniformity.PlotDataSource.GetScatterLines(1).Single().ScatterSourcePoints.Points;
                        if (points.Count != frequencies.Length) return ThrowHelper.ThrowArgumentException<bool>($"{nameof(points)} count != {nameof(frequencies)} count");

                        electrodeConfiguration.UniformityConfigurations =
                        [
                            .. points.Select(t => new GenerateAODWaveformUniformityConfiguration
                            {
                                Frequency = t.X,
                                Coefficient = t.Y
                            })
                        ];
                    }
                }

                isSuccess = true;
            }
            finally
            {
                var htmlBullet = new HtmlBullet(new
                {
                    Table = new HtmlTable([.. Cache.ElectrodeConfigurationResults.Select(t => t.ToHtmlAnonymous())]),
                    Step0Items = new HtmlTable([.. Cache.Step0.Items.Select(t => t.ToHtmlAnonymous())]),
                    Step0Plot = new HtmlContainer([.. Cache.Step0.PlotDataSource.GetAllHtmlPlot2DLinesCharts()]),
                    Step1Plot = new HtmlContainer([.. Cache.Step1Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])))])
                });

                if (isSuccess)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
            }

            return isSuccess;
        }, isNotSilent).ConfigureAwait(false);
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

        var step0Task = Guard.IsAssignableToTypeAndReturn<Task<bool>>(Step0Command.ExecuteAsync( /* isNotSilent */ false));
        if (await step0Task == false) return;

        var step1Task = Guard.IsAssignableToTypeAndReturn<Task<bool>>(Step1Command.ExecuteAsync( /* isNotSilent */ false));
        if (await step1Task == false) return;

        var stepSecondLastTask = Guard.IsAssignableToTypeAndReturn<Task<bool>>(StepSecondLastCommand.ExecuteAsync( /* isNotSilent */ false));
        if (await stepSecondLastTask == false) return;

        await StepFirstLastCommand.ExecuteAsync( /* isNotSilent */ false);
    }

    private (double[] Phases, bool IsDone) AlgorithmSuggest(
        double? previousCost,
        int phaseCount,
        int initialPoints,
        double noise,
        int earlyStop,
        int randomState)
    {
        DirectoryHelper.CreateFileDirectoryIfNotExists(PhaseOptimizerStateFilePath);

        using var _ = Py.GIL();

        using var sys = Py.Import("sys");
        using var pathObject = sys.GetAttr("path");
        using var pyList = new PyList(pathObject);
        using var pyModuleDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Python").ToPython();
        pyList.Insert(0, pyModuleDirectory);

        using var module = Py.Import("phase_optimizer");

        using var pyPhaseOptimizerStateFilePath = PhaseOptimizerStateFilePath.ToPython();
        module.SetAttr("_STATE_FILE", pyPhaseOptimizerStateFilePath);

        using var suggest = module.GetAttr("suggest");

        using var pyCost = previousCost.ToPython();
        using var pyPhaseCount = phaseCount.ToPython();
        using var pyInitialPoints = initialPoints.ToPython();
        using var pyNoise = noise.ToPython();
        using var pyEarlyStop = earlyStop.ToPython();
        using var pyRandomState = randomState.ToPython();
        using var result = suggest.Invoke(pyCost, pyPhaseCount, pyInitialPoints, pyNoise, pyEarlyStop, pyRandomState);

        using var pyPhases = Guard.IsNotNullAndReturn(result["phases"]);
        using var pyBestCost = Guard.IsNotNullAndReturn(result["best_cost"]);
        using var pyBestPhases = Guard.IsNotNullAndReturn(result["best_phases"]);
        using var pyDone = Guard.IsNotNullAndReturn(result["done"]);

        return (ToDoubles(pyPhases), pyDone.As<bool>());
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
