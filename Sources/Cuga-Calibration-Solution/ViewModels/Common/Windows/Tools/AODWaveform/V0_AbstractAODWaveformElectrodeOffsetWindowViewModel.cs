using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using MathNet.Numerics;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.WPF.MVVM;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public abstract partial class V0AbstractAODWaveformElectrodeOffsetWindowViewModel<TCache, TItem, TResult> : AbstractAODWaveformCommonWindowViewModel<TCache, TItem, TResult>
    where TCache : V0AODWaveformElectrodeOffsetCache<TItem, TResult>, new()
    where TItem : V0AODWaveformElectrodeOffsetItem, new()
    where TResult : V0AODWaveformElectrodeOffsetResult, new()
{
    public readonly IReadOnlyList<OpticsAODElectrodeEnum> OpticsAODElectrodeEnums = [OpticsAODElectrodeEnum.Electrode1, OpticsAODElectrodeEnum.Electrode2, OpticsAODElectrodeEnum.Electrode3, OpticsAODElectrodeEnum.Electrode4];

    public override string[] Steps { get; } =
    [
        "Step 1 Electrode Offset",
        "Step 2 Uniformity",
        "Step 3 Generate AOD Waveform",
        "Step 4 Set AOD Waveform Config"
    ];

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

        return Cache.IsOnlyElectrode4
            ? await InvokeAsync(0, async () =>
            {
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.MeasureMaxPowerMachinePosition);
                LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
                OpticsViewModel.ToggleODFilter(false);

                Guard.IsGreaterThanOrEqualTo(Cache.Frequencies.Count, 2);
                Guard.IsTrue(Cache.Frequencies.IsIncreasing(true));

                Cache.Step0Items = [];
                Cache.ElectrodeConfigurationResults =
                [
                    .. OpticsAODElectrodeEnums.Select(t => new GenerateAODWaveformElectrodeConfiguration
                    {
                        OpticsAODElectrodeEnum = t,
                        OffsetFrequency = Cache.OffsetFrequency,
                        OffsetFrequencyPeriodCoefficient = 0d,
                        Amplitude = 1d, // 生成result默认幅值都是1
                        IsGenerateAODWaveformZero = false
                    })
                ];

                var isSuccess = false;

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await InvokeElectrodeMaxMeasurePowerAsync([OpticsAODElectrodeEnum.Electrode1, OpticsAODElectrodeEnum.Electrode2], Cache.Electrode2OffsetFrequencyPeriodParam, Cache.Electrode2Weights).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();
                    await InvokeElectrodeMaxMeasurePowerAsync([OpticsAODElectrodeEnum.Electrode3, OpticsAODElectrodeEnum.Electrode4], Cache.Electrode4OffsetFrequencyPeriodParam, Cache.Electrode4Weights).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();
                    await InvokeElectrodeMaxMeasurePowerAsync([OpticsAODElectrodeEnum.Electrode1, OpticsAODElectrodeEnum.Electrode2, OpticsAODElectrodeEnum.Electrode3, OpticsAODElectrodeEnum.Electrode4], Cache.Electrode3OffsetFrequencyPeriodParam,
                        Cache.Electrode3Weights).ConfigureAwait(false);

                    isSuccess = Cache.ElectrodeConfigurationResults.Count == OpticsAODElectrodeEnums.Count;
                }
                finally
                {
                    var htmlBullet = new HtmlBullet(new
                    {
                        Table = new HtmlTable([.. Cache.ElectrodeConfigurationResults.Select(t => t.ToHtmlAnonymous())]),
                        ElectrodeOffsetItems = new HtmlContainer([.. Cache.Step0Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])))])
                    });

                    if (isSuccess)
                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                    else
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                }

                return isSuccess;

                async Task InvokeElectrodeMaxMeasurePowerAsync(IReadOnlyList<OpticsAODElectrodeEnum> electrodes, V0AODWaveformElectrodeOffsetFrequencyPeriodParam param, IReadOnlyList<double> weights)
                {
                    var aodWaveformElectrodeOffsetFrequencyPeriod = new V0AODWaveformElectrodeOffsetFrequencyPeriod<TItem> { Electrodes = electrodes };
                    Cache.Step0Items = [.. Cache.Step0Items, aodWaveformElectrodeOffsetFrequencyPeriod];

                    var offsetFrequencyPeriodCoefficients = Generate.LinearRangeContainsEdge(param.StartOffsetFrequencyPeriodCoefficient, param.StepOffsetFrequencyPeriodCoefficient, param.StopOffsetFrequencyPeriodCoefficient);
                    Guard.IsNotEmpty(offsetFrequencyPeriodCoefficients);

                    var currentDetailLogUniqueId = Guid.NewGuid();
                    var fileName = $"Details_{Steps[stepIndex].Replace(" ", string.Empty)}_{string.Join("_", aodWaveformElectrodeOffsetFrequencyPeriod.Electrodes)}";

                    Logger.LogHtmlInformation(aodWaveformElectrodeOffsetFrequencyPeriod.Title, HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {fileName}({currentDetailLogUniqueId:N})"), HtmlLogUniqueId.LoggingHtml());
                    Logger.LogHtmlInformation($"{currentDetailLogUniqueId:N}", HtmlHeaderLevelEnum.Header1, new HtmlComment(Name), currentDetailLogUniqueId.LoggingHtml());
                    Logger.LogHtmlInformation($"{string.Join("_", aodWaveformElectrodeOffsetFrequencyPeriod.Electrodes)}", HtmlHeaderLevelEnum.Header3, currentDetailLogUniqueId.LoggingHtml());

                    try
                    {
                        foreach (var frequency in Cache.Frequencies)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var aodWaveformElectrodeOffsetFrequencyPeriodItem = new V0AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem>();
                            aodWaveformElectrodeOffsetFrequencyPeriod.Items = [.. aodWaveformElectrodeOffsetFrequencyPeriod.Items, aodWaveformElectrodeOffsetFrequencyPeriodItem];

                            Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, currentDetailLogUniqueId.LoggingHtml());

                            foreach (var currentOffsetFrequencyPeriodCoefficient in offsetFrequencyPeriodCoefficients)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                TItem item;
                                if (param.OpticsAODElectrodeEnum != OpticsAODElectrodeEnum.Electrode3)
                                    item = new TItem
                                    {
                                        ElectrodeConfigurations =
                                        [
                                            .. Cache.ElectrodeConfigurationResults
                                                .Select(t => new GenerateAODWaveformElectrodeConfiguration
                                                {
                                                    OpticsAODElectrodeEnum = t.OpticsAODElectrodeEnum,
                                                    OffsetFrequency = t.OffsetFrequency,
                                                    OffsetFrequencyPeriodCoefficient = t.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum
                                                        ? currentOffsetFrequencyPeriodCoefficient
                                                        : 0,
                                                    Amplitude = t.Amplitude,
                                                    IsGenerateAODWaveformZero = electrodes.Contains(t.OpticsAODElectrodeEnum) == false
                                                })
                                        ],
                                        Frequency = frequency,
                                        Amplitude = Cache.DefaultAmplitude,
                                        OffsetFrequencyPeriodCoefficient = currentOffsetFrequencyPeriodCoefficient
                                    };
                                else
                                {
                                    var electrodes2 = Cache.ElectrodeConfigurationResults.Single(t => t.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode2);
                                    var electrodes4 = Cache.ElectrodeConfigurationResults.Single(t => t.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode4);
                                    var electrodes3OffsetFrequencyPeriodCoefficient = currentOffsetFrequencyPeriodCoefficient;
                                    var electrodes4OffsetFrequencyPeriodCoefficient = currentOffsetFrequencyPeriodCoefficient + electrodes4.OffsetFrequencyPeriodCoefficient;

                                    item = new TItem
                                    {
                                        ElectrodeConfigurations =
                                        [
                                            .. Cache.ElectrodeConfigurationResults
                                                .Select(t => new GenerateAODWaveformElectrodeConfiguration
                                                {
                                                    OpticsAODElectrodeEnum = t.OpticsAODElectrodeEnum,
                                                    OffsetFrequency = t.OffsetFrequency,
                                                    OffsetFrequencyPeriodCoefficient = t.OpticsAODElectrodeEnum switch
                                                    {
                                                        OpticsAODElectrodeEnum.Electrode1 => 0,
                                                        OpticsAODElectrodeEnum.Electrode2 => electrodes2.OffsetFrequencyPeriodCoefficient,
                                                        OpticsAODElectrodeEnum.Electrode3 => electrodes3OffsetFrequencyPeriodCoefficient,
                                                        OpticsAODElectrodeEnum.Electrode4 => electrodes4OffsetFrequencyPeriodCoefficient,
                                                        _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(t.OpticsAODElectrodeEnum))
                                                    },
                                                    Amplitude = t.Amplitude,
                                                    IsGenerateAODWaveformZero = false
                                                })
                                        ],
                                        Frequency = frequency,
                                        Amplitude = Cache.DefaultAmplitude,
                                        OffsetFrequencyPeriodCoefficient = currentOffsetFrequencyPeriodCoefficient
                                    };
                                }

                                Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header5, currentDetailLogUniqueId.LoggingHtml());

                                await UpdateMeasurePowerAsync(item, currentDetailLogUniqueId, cancellationToken).ConfigureAwait(false);

                                aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems = [.. aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems, item];
                            }
                        }
                    }
                    finally
                    {
                        Logger.LogHtmlInformation(currentDetailLogUniqueId.LoggedEndHtml(fileName));
                    }

                    aodWaveformElectrodeOffsetFrequencyPeriod.InterpolationMaxima(Cache.InterpolationCount);

                    aodWaveformElectrodeOffsetFrequencyPeriod.OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetFrequencyPeriod.ClosestMaximaPoints
                        .Index()
                        .Select(t => weights[t.Index] * t.Item.X)
                        .Sum() / weights.Sum();

                    if (Cache.IsConfirmAODWaveformElectrodeOffsetResult)
                    {
                        var aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel = HostApplication.GetRequiredService<V0AODWaveformElectrodeOffsetFrequencyPeriodConfirmResultWindowViewModel>();
                        aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel.OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetFrequencyPeriod.OffsetFrequencyPeriodCoefficient.Value;

                        var showDialog = WindowManagerService.ShowDialog(aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel);
                        if (showDialog == true) aodWaveformElectrodeOffsetFrequencyPeriod.OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel.OffsetFrequencyPeriodCoefficient;
                    }

                    if (param.OpticsAODElectrodeEnum != OpticsAODElectrodeEnum.Electrode3)
                        Cache.ElectrodeConfigurationResults.Single(t => t.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum)
                            .OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetFrequencyPeriod.OffsetFrequencyPeriodCoefficient.Value;
                    else
                    {
                        Cache.ElectrodeConfigurationResults.Single(t => t.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode3)
                            .OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetFrequencyPeriod.OffsetFrequencyPeriodCoefficient.Value;
                        Cache.ElectrodeConfigurationResults.Single(t => t.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode4)
                            .OffsetFrequencyPeriodCoefficient += aodWaveformElectrodeOffsetFrequencyPeriod.OffsetFrequencyPeriodCoefficient.Value;
                    }
                }
            }, isSilent).ConfigureAwait(false)
            : await InvokeAsync(stepIndex, async () =>
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

                        var aodWaveformElectrodeOffsetFrequencyPeriod = new V0AODWaveformElectrodeOffsetFrequencyPeriod<TItem> { Electrodes = electrodes };
                        Cache.Step0Items = [.. Cache.Step0Items, aodWaveformElectrodeOffsetFrequencyPeriod];

                        var offsetFrequencyPeriodCoefficients = Generate.LinearRangeContainsEdge(param.StartOffsetFrequencyPeriodCoefficient, param.StepOffsetFrequencyPeriodCoefficient, param.StopOffsetFrequencyPeriodCoefficient);
                        Guard.IsNotEmpty(offsetFrequencyPeriodCoefficients);

                        var currentDetailLogUniqueId = Guid.NewGuid();
                        var fileName = $"Details_{Steps[stepIndex].Replace(" ", string.Empty)}_{string.Join("_", aodWaveformElectrodeOffsetFrequencyPeriod.Electrodes)}";

                        Logger.LogHtmlInformation(aodWaveformElectrodeOffsetFrequencyPeriod.Title, HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {fileName}({currentDetailLogUniqueId:N})"), HtmlLogUniqueId.LoggingHtml());
                        Logger.LogHtmlInformation($"{currentDetailLogUniqueId:N}", HtmlHeaderLevelEnum.Header1, new HtmlComment(Name), currentDetailLogUniqueId.LoggingHtml());
                        Logger.LogHtmlInformation($"{string.Join("_", aodWaveformElectrodeOffsetFrequencyPeriod.Electrodes)}", HtmlHeaderLevelEnum.Header3, currentDetailLogUniqueId.LoggingHtml());

                        try
                        {
                            foreach (var frequency in Cache.Frequencies)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                var aodWaveformElectrodeOffsetFrequencyPeriodItem = new V0AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem>();
                                aodWaveformElectrodeOffsetFrequencyPeriod.Items = [.. aodWaveformElectrodeOffsetFrequencyPeriod.Items, aodWaveformElectrodeOffsetFrequencyPeriodItem];

                                Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, currentDetailLogUniqueId.LoggingHtml());

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

                                    Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header5, currentDetailLogUniqueId.LoggingHtml());

                                    await UpdateMeasurePowerAsync(item, currentDetailLogUniqueId, cancellationToken).ConfigureAwait(false);

                                    aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems = [.. aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems, item];
                                }
                            }
                        }
                        finally
                        {
                            Logger.LogHtmlInformation(currentDetailLogUniqueId.LoggedEndHtml(fileName));
                        }

                        aodWaveformElectrodeOffsetFrequencyPeriod.InterpolationMaxima(Cache.InterpolationCount);

                        var weightParams = (IReadOnlyList<V0AODWaveformElectrodeOffsetFrequencyWeightParam>)
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
                            var aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel = HostApplication.GetRequiredService<V0AODWaveformElectrodeOffsetFrequencyPeriodConfirmResultWindowViewModel>();
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
                        ElectrodeOffsetItems = new HtmlContainer([.. Cache.Step0Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])))])
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
            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.MeasureMaxPowerMachinePosition);
            LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
            OpticsViewModel.ToggleODFilter(false);

            Guard.IsEqualTo(Cache.ElectrodeConfigurationResults.Count, Cache.IsOnlyElectrode4 ? 4 : Cache.ElectrodeOffsetFrequencyPeriodParams.Count);
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

                    var aodWaveformElectrodeOffsetFrequencyUniformity = new V0AODWaveformElectrodeOffsetFrequencyUniformity<TItem> { Electrodes = electrodes };
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

                            var aodWaveformElectrodeOffsetFrequencyUniformityItem = new V0AODWaveformElectrodeOffsetFrequencyUniformityItem<TItem>();
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
                        var points = aodWaveformElectrodeOffsetFrequencyUniformity.PlotDataSource.GetScatterLines(1).Single().Source.Data;
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
                    ElectrodeOffsetItems = new HtmlContainer([.. Cache.Step0Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])))]),
                    UniformityItems = new HtmlContainer([.. Cache.Step1Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])))])
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
}