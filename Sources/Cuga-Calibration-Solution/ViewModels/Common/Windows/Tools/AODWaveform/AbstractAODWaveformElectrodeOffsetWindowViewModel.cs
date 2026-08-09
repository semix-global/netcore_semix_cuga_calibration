using System.IO;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using MathNet.Numerics;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Python.Runtime;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public abstract partial class AbstractAODWaveformElectrodeOffsetWindowViewModel<TCache, TItem, TResult> : AbstractAODWaveformCommonWindowViewModel<TCache, TItem, TResult>
    where TCache : AODWaveformElectrodeOffsetCache<TItem, TResult>, new()
    where TItem : AODWaveformElectrodeOffsetItem, new()
    where TResult : AODWaveformElectrodeOffsetResult, new()
{
    public string PhaseOptimizerStateFilePath => Path.Combine(ApplicationSetting.AppHomeDirectory, "Python", "PhaseOptimizer", $"{GetType().Name}.pkl");

    public override IReadOnlyList<string> Steps { get; } =
    [
        "Step 1 Electrode Offset",
        "Step 2 Uniformity",
        "Step 3 Generate AOD Waveform",
        "Step 4 Set AOD Waveform Config"
    ];

    [RelayCommand]
    private void ResetPhaseOptimizer()
    {
        if (System.IO.File.Exists(PhaseOptimizerStateFilePath))
        {
            var backupFilePath = Path.Combine(PhaseOptimizerStateFilePath, $"_{DateTime.Now.ToString(Constants.LongFileDateTimeFormat)}");
            System.IO.File.Move(PhaseOptimizerStateFilePath, backupFilePath);

            DialogWindowProvider.ShowDialog($"Phase optimizer state file backup: {backupFilePath} Ok.");
        }

        DialogWindowProvider.ShowDialog("Phase optimizer state file does not exist.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
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

            Guard.IsNotEmpty(Cache.ElectrodeOffsetFrequencyPeriodParams);
            Guard.IsGreaterThanOrEqualTo(Cache.ElectrodeOffsetFrequencyPeriodParams.Count, 1);
            Guard.IsGreaterThanOrEqualTo(Cache.Frequencies.Count, 2);
            Guard.IsTrue(Cache.Frequencies.IsIncreasing(true));

            Cache.Step0Items = [];
            Cache.ElectrodeConfigurationResults =
            [
                new GenerateAODWaveformElectrodeConfiguration
                {
                    OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1,
                    OffsetFrequency = Cache.OffsetFrequency,
                    OffsetFrequencyPeriodCoefficient = 0d,
                    Amplitude = 1d, // 生成result默认幅值都是1
                    IsGenerateAODWaveformZero = false
                }
            ];

            var isSuccess = false;

            try
            {
                foreach (var param in Cache.ElectrodeOffsetFrequencyPeriodParams)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (Cache.ElectrodeConfigurationResults.Any(t => t.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum)) continue;

                    var electrodes = (OpticsAODElectrodeEnum[])[.. Cache.ElectrodeConfigurationResults.Select(t => t.OpticsAODElectrodeEnum), param.OpticsAODElectrodeEnum];

                    var aodWaveformElectrodeOffsetFrequencyPeriod = new AODWaveformElectrodeOffsetFrequencyPeriod<TItem> { Electrodes = electrodes };
                    Cache.Step0Items = [.. Cache.Step0Items, aodWaveformElectrodeOffsetFrequencyPeriod];

                    var offsetFrequencyPeriodCoefficients = Generate.LinearRangeContainsEdge(param.StartOffsetFrequencyPeriodCoefficient, param.StepOffsetFrequencyPeriodCoefficient, param.StopOffsetFrequencyPeriodCoefficient);
                    Guard.IsNotEmpty(offsetFrequencyPeriodCoefficients);

                    var aodWaveformElectrodeOffsetFrequencyPeriodHmlLogUniqueId = Guid.NewGuid();

                    var fileName = $"Details_{Steps[stepIndex].Replace(" ", string.Empty)}_{string.Join("_", aodWaveformElectrodeOffsetFrequencyPeriod.Electrodes)}";
                    Logger.LogHtmlInformation(aodWaveformElectrodeOffsetFrequencyPeriod.Title, HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {fileName}({aodWaveformElectrodeOffsetFrequencyPeriodHmlLogUniqueId:N})"),
                        HtmlLogUniqueId.LoggingHtml());
                    Logger.LogHtmlInformation($"{aodWaveformElectrodeOffsetFrequencyPeriodHmlLogUniqueId:N}", HtmlHeaderLevelEnum.Header1, new HtmlComment(Name), aodWaveformElectrodeOffsetFrequencyPeriodHmlLogUniqueId.LoggingHtml());
                    try
                    {
                        foreach (var frequency in Cache.Frequencies)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var aodWaveformElectrodeOffsetFrequencyPeriodItem = new AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem>();
                            aodWaveformElectrodeOffsetFrequencyPeriod.Items = [.. aodWaveformElectrodeOffsetFrequencyPeriod.Items, aodWaveformElectrodeOffsetFrequencyPeriodItem];

                            Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, aodWaveformElectrodeOffsetFrequencyPeriodHmlLogUniqueId.LoggingHtml());

                            foreach (var currentOffsetFrequencyPeriodCoefficient in offsetFrequencyPeriodCoefficients)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                var item = new TItem
                                {
                                    ElectrodeConfigurations =
                                    [
                                        .. Cache.ElectrodeOffsetFrequencyPeriodParams
                                            .Select(t => new GenerateAODWaveformElectrodeConfiguration
                                            {
                                                OpticsAODElectrodeEnum = t.OpticsAODElectrodeEnum,
                                                OffsetFrequency = Cache.OffsetFrequency,
                                                OffsetFrequencyPeriodCoefficient = t.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum
                                                    ? currentOffsetFrequencyPeriodCoefficient
                                                    : Cache.ElectrodeConfigurationResults
                                                        .SingleOrDefault(tt => tt.OpticsAODElectrodeEnum == t.OpticsAODElectrodeEnum)
                                                        ?.OffsetFrequencyPeriodCoefficient ?? 0,
                                                Amplitude = Cache.DefaultAmplitude,
                                                IsGenerateAODWaveformZero = electrodes.Contains(t.OpticsAODElectrodeEnum) == false
                                            })
                                    ],
                                    Frequency = frequency,
                                    Amplitude = Cache.DefaultAmplitude,
                                    OffsetFrequencyPeriodCoefficient = currentOffsetFrequencyPeriodCoefficient
                                };

                                Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header5, aodWaveformElectrodeOffsetFrequencyPeriodHmlLogUniqueId.LoggingHtml());

                                await UpdateMeasurePowerAsync(item, true, aodWaveformElectrodeOffsetFrequencyPeriodHmlLogUniqueId, cancellationToken).ConfigureAwait(false);

                                aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems = [.. aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems, item];
                            }
                        }
                    }
                    finally
                    {
                        Logger.LogHtmlInformation(aodWaveformElectrodeOffsetFrequencyPeriodHmlLogUniqueId.LoggedEndHtml(fileName));
                    }

                    aodWaveformElectrodeOffsetFrequencyPeriod.InterpolationMaxima(Cache.InterpolationCount);

                    var weightParams = (IReadOnlyList<AODWaveformElectrodeOffsetFrequencyWeightParam>)
                    [
                        .. Cache.ElectrodeOffsetFrequencyWeightParams
                            .Where(tt => tt.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum)
                    ];

                    aodWaveformElectrodeOffsetFrequencyPeriod.OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetFrequencyPeriod.ClosestMaximaPoints
                        .Index()
                        .Select(t => weightParams.Single(tt => tt.Frequency - aodWaveformElectrodeOffsetFrequencyPeriod.Items[t.Index].FrequencyItems[0].Frequency == 0).Weight * t.Item.X)
                        .Sum() / weightParams.Sum(t => t.Weight);

                    if (Cache.IsConfirmAODWaveformElectrodeOffsetResult)
                    {
                        var aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel = HostApplication.GetRequiredService<AODWaveformElectrodeOffsetFrequencyPeriodConfirmResultWindowViewModel>();
                        aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel.OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetFrequencyPeriod.OffsetFrequencyPeriodCoefficient.Value;

                        var showDialog = WindowManagerService.ShowDialog(aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel);
                        if (showDialog == true) aodWaveformElectrodeOffsetFrequencyPeriod.OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel.OffsetFrequencyPeriodCoefficient;
                    }

                    Cache.ElectrodeConfigurationResults =
                    [
                        .. Cache.ElectrodeConfigurationResults, new GenerateAODWaveformElectrodeConfiguration
                        {
                            OpticsAODElectrodeEnum = param.OpticsAODElectrodeEnum,
                            OffsetFrequency = Cache.OffsetFrequency,
                            OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetFrequencyPeriod.OffsetFrequencyPeriodCoefficient.Value,
                            Amplitude = 1d, // 生成result默认幅值都是1
                            IsGenerateAODWaveformZero = false
                        }
                    ];
                }

                isSuccess = Cache.ElectrodeConfigurationResults.Count == Cache.ElectrodeOffsetFrequencyPeriodParams.Count;
            }
            finally
            {
                var htmlBullet = new HtmlBullet(new
                {
                    Table = new HtmlTable([.. Cache.ElectrodeConfigurationResults.Select(t => t.ToHtmlAnonymous())]),
                    ElectrodeOffsetItems = new HtmlContainer([.. Cache.Step0Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])))])
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

            Guard.IsEqualTo(Cache.ElectrodeConfigurationResults.Count, Cache.ElectrodeOffsetFrequencyPeriodParams.Count);
            Guard.IsNotEmpty(Cache.ElectrodeOffsetFrequencyUniformityParams);
            Guard.IsTrue(Cache.ElectrodeOffsetFrequencyUniformityParams.All(t => Cache.ElectrodeOffsetFrequencyPeriodParams.Any(tt => t.OpticsAODElectrodeEnum == tt.OpticsAODElectrodeEnum)));
            Guard.IsGreaterThanOrEqualTo(Cache.ElectrodeOffsetFrequencyUniformityParams.Count, Cache.ElectrodeOffsetFrequencyUniformityParamChunkSize);
            Guard.IsGreaterThanOrEqualTo(Cache.Frequencies.Count, 2);
            Guard.IsTrue(Cache.Frequencies.IsIncreasing(true));

            Cache.Step1Items = [];
            foreach (var electrodeConfiguration in Cache.ElectrodeConfigurationResults)
            {
                electrodeConfiguration.UniformityConfigurations = [];
            }

            var isSuccess = false;

            try
            {
                var frequencies = Generate.LinearRangeContainsEdge(Cache.Frequencies[0], Cache.StepFrequency, Cache.Frequencies[^1]);
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

                                await UpdateMeasurePowerAsync(item, true, aodWaveformElectrodeOffsetFrequencyUniformityHmlLogUniqueId, cancellationToken).ConfigureAwait(false);

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
                    ElectrodeOffsetItems = new HtmlContainer([.. Cache.Step0Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])))]),
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