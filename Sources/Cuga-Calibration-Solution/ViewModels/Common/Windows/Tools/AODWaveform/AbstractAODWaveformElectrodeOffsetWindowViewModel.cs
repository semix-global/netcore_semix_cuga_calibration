using System.Collections;
using System.IO;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.AODWaveform.Generates;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using Python.Runtime;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public abstract partial class AbstractAODWaveformElectrodeOffsetWindowViewModel<TCache, TItem, TResult> : AbstractAODWaveformCommonWindowViewModel<TCache, TItem, TResult>
    where TCache : AODWaveformElectrodeOffsetCache<TItem, TResult>, new()
    where TItem : AODWaveformElectrodeOffsetItem, new()
    where TResult : AODWaveformElectrodeOffsetResult, new()
{
    public string PhaseOptimizerStateFilePath => Path.Combine(ApplicationSetting.AppHomeDirectory, "Python", "PhaseOptimizer", $"{GetType().Name}_{string.Join(',', Cache.ElectrodeOffsetFrequencyPeriodParams.Select(p => p.OpticsAODElectrodeEnum))}.pkl");

    public override IReadOnlyList<string> Steps { get; } =
    [
        "Step 1 Electrode Offset",
        "Step 2 Uniformity",
        "Step 3 Generate AOD Waveform",
        "Step 4 Set AOD Waveform Config"
    ];

    [RelayCommand]
    private void ImportUniformityConfiguration(AODWaveformElectrodeOffsetFrequencyPeriodParam aodWaveformElectrodeOffsetFrequencyPeriodParam)
    {
        try
        {
            var dialog = DialogWindowProvider.TryShowSelectFilePathDialog(".xlsx", out var filePath);
            if (dialog == false) return;

            aodWaveformElectrodeOffsetFrequencyPeriodParam.UniformityConfigurations = [];

            var values = MiniExcel.Query<GenerateAODWaveformUniformityConfiguration>(filePath)
                .Where(t => t.Frequency > 0)
                .ToArray();
            if (values.Length <= 0)
            {
                values =
                [
                    .. MiniExcel.Query(filePath, useHeaderRow: true)
                        .Cast<IDictionary<string, object>>()
                        .Select(t => new GenerateAODWaveformUniformityConfiguration { Frequency = (double)t[nameof(Point.X)], Coefficient = (double)t[nameof(Point.Y)] })
                        .Where(t => t.Frequency > 0)
                ];
            }

            if (values.Length > 0)
            {
                aodWaveformElectrodeOffsetFrequencyPeriodParam.UniformityConfigurations = values;
                Logger.LogInformation("Import Uniformity Configuration OK!");
                DialogWindowProvider.ShowDialog("Import Uniformity Configuration OK!");
            }
            else
            {
                Logger.LogWarning("Import Uniformity Configuration Failed! No data found.");
                DialogWindowProvider.ShowDialog("Import Uniformity Configuration Failed! No data found.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Import Uniformity Configuration");
            DialogWindowProvider.ShowDialog($"""
                                             Import Uniformity Configuration Failed!
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private void AddUniformityConfiguration(AODWaveformElectrodeOffsetFrequencyPeriodParam aodWaveformElectrodeOffsetFrequencyPeriodParam)
    {
        var configurationList = aodWaveformElectrodeOffsetFrequencyPeriodParam.UniformityConfigurations.ToList();
        configurationList.Add(new GenerateAODWaveformUniformityConfiguration());

        aodWaveformElectrodeOffsetFrequencyPeriodParam.UniformityConfigurations = configurationList;
    }

    [RelayCommand]
    private void RemoveUniformityConfiguration((AODWaveformElectrodeOffsetFrequencyPeriodParam AODWaveformElectrodeOffsetFrequencyPeriodParam, IEnumerable? SelectItems)? valueTuple)
    {
        if (valueTuple?.SelectItems is null) return;

        var configurationList = valueTuple.Value.AODWaveformElectrodeOffsetFrequencyPeriodParam.UniformityConfigurations.ToList();
        foreach (GenerateAODWaveformUniformityConfiguration selectItem in valueTuple.Value.SelectItems) configurationList.Remove(selectItem);

        valueTuple.Value.AODWaveformElectrodeOffsetFrequencyPeriodParam.UniformityConfigurations = configurationList;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ResetAlgorithmPhaseOptimizerStateAsync(TResult result, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            try
            {
                if (System.IO.File.Exists(PhaseOptimizerStateFilePath))
                {
                    var backupFilePath = $"{PhaseOptimizerStateFilePath}_{DateTime.Now.ToString(Constants.LongFileDateTimeFormat)}";
                    System.IO.File.Move(PhaseOptimizerStateFilePath, backupFilePath);

                    DialogWindowProvider.ShowDialog($"Algorithm Phase optimizer state file backup: {backupFilePath} , Reset Ok.");
                }
                else DialogWindowProvider.ShowDialog("Reset Algorithm Phase optimizer state file does not exist.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    DialogWindowProvider.ShowDialog($"{Name}:Reset Algorithm Phase optimizer state Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                DialogWindowProvider.ShowDialog($"""
                                                 {Name}: Reset Algorithm Phase optimizer state Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogError(ex, "Reset Algorithm Phase optimizer state");
            }
        }, cancellationToken).ConfigureAwait(false);
    }

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
            Guard.IsGreaterThanOrEqualTo(Cache.Frequencies.Count, 2);
            Guard.IsNotEmpty(Cache.ElectrodeOffsetFrequencyPeriodParams);

            Guard.IsGreaterThan(Cache.TotalMeasurePower, 0);
            Guard.IsGreaterThan(Cache.AlgorithmInitialPoints, 0);
            Guard.IsGreaterThan(Cache.AlgorithmNoise, 0d);
            Guard.IsGreaterThan(Cache.AlgorithmEarlyStop, 0);
            Guard.IsGreaterThan(Cache.AlgorithmRandomState, 0);
            Guard.IsGreaterThan(Cache.AlgorithmRetryTimes, 0);

            Cache.Step0 = new AODWaveformElectrodeOffsetFrequencyPeriod<TItem>();
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

            if (Cache.ElectrodeOffsetFrequencyPeriodParams.Count == 1) return true;

            var linearSplines = Cache.ElectrodeOffsetFrequencyPeriodParams.Select(t => t.UniformityConfigurations.Count > 0
                    ? LinearSpline.InterpolateSorted(
                        [.. t.UniformityConfigurations.Select(configuration => configuration.Frequency)],
                        [.. t.UniformityConfigurations.Select(configuration => configuration.Coefficient)])
                    : null)
                .ToArray();

            var isSuccess = false;

            try
            {
                double? lastCost = null;
                var times = 0;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    var (phases, isDone) = AlgorithmSuggest(
                        lastCost,
                        Cache.ElectrodeOffsetFrequencyPeriodParams.Count - 1,
                        Cache.AlgorithmInitialPoints,
                        Cache.AlgorithmNoise,
                        Cache.AlgorithmEarlyStop,
                        Cache.AlgorithmRandomState);

                    if (isDone) isSuccess = true;

                    var aodWaveformElectrodeOffsetFrequencyPeriodItem = new AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem>
                    {
                        OffsetFrequencyPeriodCoefficients =
                        [
                            .. Cache.ElectrodeOffsetFrequencyPeriodParams
                                .Index()
                                .Select(t => t.Index <= Cache.ElectrodeOffsetFrequencyPeriodParams.Count - 2
                                    ? phases[t.Index] + t.Item.BoardCardOffsetFrequencyPeriodCoefficient
                                    : 0d)
                        ],
                        FrequencyItems = [],
                        Score = 0d
                    };

                    foreach (var frequency in Cache.Frequencies)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

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
                                        UniformityConfigurations = t.Item.UniformityConfigurations
                                    })
                            ],
                            Frequency = frequency,
                            Amplitude = Cache.DefaultAmplitude
                        };

                        await UpdateMeasurePowerAsync(item, HtmlLogUniqueId, cancellationToken).ConfigureAwait(false);

                        aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems = [.. aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems, item];
                    }

                    var vector = 10 * (Vector<double>.Build.Dense([.. aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems.Select(t => t.MeasurePower)]) / Cache.TotalMeasurePower).PointwiseLog10();
                    aodWaveformElectrodeOffsetFrequencyPeriodItem.Score = vector.Average() - Cache.AlgorithmLambda * vector.StandardDeviation();

                    lastCost = -aodWaveformElectrodeOffsetFrequencyPeriodItem.Score;

                    Cache.Step0.Items = [.. Cache.Step0.Items, aodWaveformElectrodeOffsetFrequencyPeriodItem];

                    if (++times > Cache.AlgorithmRetryTimes - 1)
                    {
                        isSuccess = false;
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("More than the number of times."), HtmlLogUniqueId.LoggingHtml());

                        break;
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
                var htmlBullet = new HtmlBullet(new
                {
                    Table = new HtmlTable([.. Cache.ElectrodeConfigurationResults.Select(t => t.ToHtmlAnonymous())]),
                    ElectrodeOffsetItems = new HtmlContainer([.. Cache.Step0.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
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
            Guard.IsGreaterThanOrEqualTo(Cache.Frequencies.Count, 2);
            Guard.IsNotEmpty(Cache.ElectrodeOffsetFrequencyPeriodParams);

            Guard.IsEqualTo(Cache.ElectrodeConfigurationResults.Count, Cache.ElectrodeOffsetFrequencyPeriodParams.Count);
            Guard.IsNotEmpty(Cache.ElectrodeOffsetFrequencyUniformityParams);
            Guard.IsTrue(Cache.ElectrodeOffsetFrequencyUniformityParams.All(t => Cache.ElectrodeOffsetFrequencyPeriodParams.Any(tt => t.OpticsAODElectrodeEnum == tt.OpticsAODElectrodeEnum)));
            Guard.IsGreaterThanOrEqualTo(Cache.ElectrodeOffsetFrequencyUniformityParams.Count, Cache.ElectrodeOffsetFrequencyUniformityParamChunkSize);

            Cache.Step1Items = [];
            foreach (var electrodeConfiguration in Cache.ElectrodeConfigurationResults)
            {
                electrodeConfiguration.UniformityConfigurations = [];
            }

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

                    var aodWaveformElectrodeOffsetFrequencyUniformityHmlLogUniqueId = Guid.NewGuid();

                    var fileName = $"Details_{Steps[stepIndex].Replace(" ", string.Empty)}_{string.Join("_", aodWaveformElectrodeOffsetFrequencyUniformity.Electrodes)}";
                    Logger.LogHtmlInformation(aodWaveformElectrodeOffsetFrequencyUniformity.Title, HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {fileName}({aodWaveformElectrodeOffsetFrequencyUniformityHmlLogUniqueId:N})"),
                        HtmlLogUniqueId.LoggingHtml());
                    Logger.LogHtmlInformation($"{aodWaveformElectrodeOffsetFrequencyUniformityHmlLogUniqueId:N}", HtmlHeaderLevelEnum.Header1, new HtmlComment(Name), aodWaveformElectrodeOffsetFrequencyUniformityHmlLogUniqueId.LoggingHtml());
                    try
                    {
                        foreach (var frequency in frequencies)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var aodWaveformElectrodeOffsetFrequencyUniformityItem = new AODWaveformElectrodeOffsetFrequencyUniformityItem<TItem>();
                            aodWaveformElectrodeOffsetFrequencyUniformity.Items = [.. aodWaveformElectrodeOffsetFrequencyUniformity.Items, aodWaveformElectrodeOffsetFrequencyUniformityItem];

                            Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, aodWaveformElectrodeOffsetFrequencyUniformityHmlLogUniqueId.LoggingHtml());

                            var amplitudes = Generate.LinearRangeContainsEdge(electrodeFrequencyUniformityParams[0].StartAmplitude, electrodeFrequencyUniformityParams[0].StepAmplitude, electrodeFrequencyUniformityParams[0].StopAmplitude).AsEnumerable()
                                .Reverse().ToArray();
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
                                                if (electrodes.Contains(t.OpticsAODElectrodeEnum)) return t.Clone().WithAmplitude(amplitude).WithUniformityConfigurations([]);

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

                                Logger.LogHtmlInformation($"{item.Amplitude}(AMP)", HtmlHeaderLevelEnum.Header5, aodWaveformElectrodeOffsetFrequencyUniformityHmlLogUniqueId.LoggingHtml());

                                await UpdateMeasurePowerAsync(item, aodWaveformElectrodeOffsetFrequencyUniformityHmlLogUniqueId, cancellationToken).ConfigureAwait(false);

                                aodWaveformElectrodeOffsetFrequencyUniformityItem.FrequencyItems = [.. aodWaveformElectrodeOffsetFrequencyUniformityItem.FrequencyItems, item];
                            }
                        }
                    }
                    finally
                    {
                        Logger.LogHtmlInformation(aodWaveformElectrodeOffsetFrequencyUniformityHmlLogUniqueId.LoggedEndHtml(fileName));
                    }

                    foreach (var electrodeConfiguration in Cache.ElectrodeConfigurationResults.Where(t => electrodes.Contains(t.OpticsAODElectrodeEnum)))
                    {
                        var points = aodWaveformElectrodeOffsetFrequencyUniformity.ScatterPlotControl.GetScatterLines(1).Single().ScatterSourcePoints.Points;
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
                    ElectrodeOffsetItems = new HtmlContainer([.. Cache.Step0.PlotDataSource.GetAllHtmlPlot2DLinesCharts()]),
                    UniformityItems = new HtmlContainer([.. Cache.Step1Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])))])
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
        using var result = suggest.Invoke(pyCost, pyPhaseCount, pyInitialPoints, pyNoise, pyRandomState);

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