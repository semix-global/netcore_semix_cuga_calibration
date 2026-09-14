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

public abstract partial class V0AbstractAODWaveformElectrodeDelayWindowViewModel<TCache, TItem, TResult> : AbstractAODWaveformCommonWindowViewModel<TCache, TItem, TResult>
    where TCache : V0AODWaveformElectrodeDelayCache<TItem, TResult>, new()
    where TItem : V0AODWaveformElectrodeDelayItem, new()
    where TResult : V0AODWaveformElectrodeDelayResult, new()
{
    private readonly IReadOnlyList<OpticsAODElectrodeEnum> _opticsAODElectrodeEnums = [OpticsAODElectrodeEnum.Electrode1, OpticsAODElectrodeEnum.Electrode2, OpticsAODElectrodeEnum.Electrode3, OpticsAODElectrodeEnum.Electrode4];

    public override string[] Steps { get; } =
    [
        "Step 1 Electrode Delay",
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
                    .. _opticsAODElectrodeEnums.Select(t => new GenerateAODWaveformElectrodeConfiguration
                    {
                        OpticsAODElectrodeEnum = t,
                        Delay = 0d,
                        Amplitude = 1d, // 生成result默认幅值都是1
                        IsGenerateAODWaveformZero = false
                    })
                ];

                var isSuccess = false;

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await InvokeElectrodeMaxMeasurePowerAsync([OpticsAODElectrodeEnum.Electrode1, OpticsAODElectrodeEnum.Electrode2], Cache.Electrode2DelayParam, Cache.Electrode2Weights).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();
                    await InvokeElectrodeMaxMeasurePowerAsync([OpticsAODElectrodeEnum.Electrode3, OpticsAODElectrodeEnum.Electrode4], Cache.Electrode4DelayParam, Cache.Electrode4Weights).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();
                    await InvokeElectrodeMaxMeasurePowerAsync([OpticsAODElectrodeEnum.Electrode1, OpticsAODElectrodeEnum.Electrode2, OpticsAODElectrodeEnum.Electrode3, OpticsAODElectrodeEnum.Electrode4], Cache.Electrode3DelayParam,
                        Cache.Electrode3Weights).ConfigureAwait(false);

                    isSuccess = Cache.ElectrodeConfigurationResults.Count == _opticsAODElectrodeEnums.Count;
                }
                finally
                {
                    var htmlBullet = new HtmlBullet(new
                    {
                        Table = new HtmlTable([.. Cache.ElectrodeConfigurationResults.Select(t => t.ToHtmlAnonymous())]),
                        ElectrodeDelayItems = new HtmlContainer([.. Cache.Step0Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])))])
                    });

                    if (isSuccess)
                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                    else
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                }

                return isSuccess;

                async Task InvokeElectrodeMaxMeasurePowerAsync(IReadOnlyList<OpticsAODElectrodeEnum> electrodes, V0AODWaveformElectrodeDelayParam param, IReadOnlyList<double> weights)
                {
                    var aodWaveformElectrodeDelay = new V0AODWaveformElectrodeDelay<TItem> { Electrodes = electrodes };
                    Cache.Step0Items = [.. Cache.Step0Items, aodWaveformElectrodeDelay];

                    var delays = Generate.LinearRangeContainsEdge(param.StartDelay, param.StepDelay, param.StopDelay);
                    Guard.IsNotEmpty(delays);

                    var currentDetailLogUniqueId = Guid.NewGuid();
                    var fileName = $"Details_{Steps[stepIndex].Replace(" ", string.Empty)}_{string.Join("_", aodWaveformElectrodeDelay.Electrodes)}";

                    Logger.LogHtmlInformation(aodWaveformElectrodeDelay.Title, HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {fileName}({currentDetailLogUniqueId:N})"), HtmlLogUniqueId.LoggingHtml());
                    Logger.LogHtmlInformation($"{currentDetailLogUniqueId:N}", HtmlHeaderLevelEnum.Header1, new HtmlComment(Name), currentDetailLogUniqueId.LoggingHtml());
                    Logger.LogHtmlInformation($"{string.Join("_", aodWaveformElectrodeDelay.Electrodes)}", HtmlHeaderLevelEnum.Header3, currentDetailLogUniqueId.LoggingHtml());

                    try
                    {
                        foreach (var frequency in Cache.Frequencies)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var aodWaveformElectrodeDelayItem = new V0AODWaveformElectrodeDelayItem<TItem>();
                            aodWaveformElectrodeDelay.Items = [.. aodWaveformElectrodeDelay.Items, aodWaveformElectrodeDelayItem];

                            Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, currentDetailLogUniqueId.LoggingHtml());

                            foreach (var currentDelay in delays)
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
                                                    Delay = t.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum
                                                        ? currentDelay
                                                        : 0d,
                                                    Amplitude = t.Amplitude,
                                                    IsGenerateAODWaveformZero = electrodes.Contains(t.OpticsAODElectrodeEnum) == false
                                                })
                                        ],
                                        Frequency = frequency,
                                        Amplitude = Cache.DefaultAmplitude,
                                        Delay = currentDelay
                                    };
                                else
                                {
                                    var electrodes2 = Cache.ElectrodeConfigurationResults.Single(t => t.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode2);
                                    var electrodes4 = Cache.ElectrodeConfigurationResults.Single(t => t.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode4);
                                    var electrodes3Delay = currentDelay;
                                    var electrodes4Delay = currentDelay + electrodes4.Delay;

                                    item = new TItem
                                    {
                                        ElectrodeConfigurations =
                                        [
                                            .. Cache.ElectrodeConfigurationResults
                                                .Select(t => new GenerateAODWaveformElectrodeConfiguration
                                                {
                                                    OpticsAODElectrodeEnum = t.OpticsAODElectrodeEnum,
                                                    Delay = t.OpticsAODElectrodeEnum switch
                                                    {
                                                        OpticsAODElectrodeEnum.Electrode1 => 0d,
                                                        OpticsAODElectrodeEnum.Electrode2 => electrodes2.Delay,
                                                        OpticsAODElectrodeEnum.Electrode3 => electrodes3Delay,
                                                        OpticsAODElectrodeEnum.Electrode4 => electrodes4Delay,
                                                        _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(t.OpticsAODElectrodeEnum))
                                                    },
                                                    Amplitude = t.Amplitude,
                                                    IsGenerateAODWaveformZero = false
                                                })
                                        ],
                                        Frequency = frequency,
                                        Amplitude = Cache.DefaultAmplitude,
                                        Delay = currentDelay
                                    };
                                }

                                Logger.LogHtmlInformation($"{item.Delay}(ns)", HtmlHeaderLevelEnum.Header5, currentDetailLogUniqueId.LoggingHtml());

                                await UpdateMeasurePowerAsync(item, currentDetailLogUniqueId, cancellationToken).ConfigureAwait(false);

                                aodWaveformElectrodeDelayItem.FrequencyItems = [.. aodWaveformElectrodeDelayItem.FrequencyItems, item];
                            }
                        }
                    }
                    finally
                    {
                        Logger.LogHtmlInformation(currentDetailLogUniqueId.LoggedEndHtml(fileName));
                    }

                    aodWaveformElectrodeDelay.InterpolationMaxima(Cache.InterpolationCount);

                    aodWaveformElectrodeDelay.Delay = aodWaveformElectrodeDelay.ClosestMaximaPoints
                        .Index()
                        .Select(t => weights[t.Index] * t.Item.X)
                        .Sum() / weights.Sum();

                    if (Cache.IsConfirmAODWaveformElectrodeDelayResult)
                    {
                        var aodWaveformElectrodeDelayStep0ConfirmResultWindowViewModel = HostApplication.GetRequiredService<V0AODWaveformElectrodeDelayConfirmResultWindowViewModel>();
                        aodWaveformElectrodeDelayStep0ConfirmResultWindowViewModel.Delay = aodWaveformElectrodeDelay.Delay.Value;

                        var showDialog = WindowManagerService.ShowDialog(aodWaveformElectrodeDelayStep0ConfirmResultWindowViewModel);
                        if (showDialog == true) aodWaveformElectrodeDelay.Delay = aodWaveformElectrodeDelayStep0ConfirmResultWindowViewModel.Delay;
                    }

                    if (param.OpticsAODElectrodeEnum != OpticsAODElectrodeEnum.Electrode3)
                        Cache.ElectrodeConfigurationResults.Single(t => t.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum)
                            .Delay = aodWaveformElectrodeDelay.Delay.Value;
                    else
                    {
                        Cache.ElectrodeConfigurationResults.Single(t => t.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode3)
                            .Delay = aodWaveformElectrodeDelay.Delay.Value;
                        Cache.ElectrodeConfigurationResults.Single(t => t.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode4)
                            .Delay += aodWaveformElectrodeDelay.Delay.Value;
                    }
                }
            }, isSilent).ConfigureAwait(false)
            : await InvokeAsync(stepIndex, async () =>
            {
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.MeasureMaxPowerMachinePosition);
                LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
                OpticsViewModel.ToggleODFilter(false);

                Guard.IsNotEmpty(Cache.ElectrodeDelayParams);
                Guard.IsGreaterThanOrEqualTo(Cache.ElectrodeDelayParams.Count, 1);
                Guard.IsGreaterThanOrEqualTo(Cache.Frequencies.Count, 2);
                Guard.IsTrue(Cache.Frequencies.IsIncreasing(true));

                Cache.Step0Items = [];
                Cache.ElectrodeConfigurationResults =
                [
                    new GenerateAODWaveformElectrodeConfiguration
                    {
                        OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1,
                        Delay = 0d,
                        Amplitude = 1d, // 生成result默认幅值都是1
                        IsGenerateAODWaveformZero = false
                    }
                ];

                var isSuccess = false;

                try
                {
                    foreach (var param in Cache.ElectrodeDelayParams)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (Cache.ElectrodeConfigurationResults.Any(t => t.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum)) continue;

                        var electrodes = (OpticsAODElectrodeEnum[])[.. Cache.ElectrodeConfigurationResults.Select(t => t.OpticsAODElectrodeEnum), param.OpticsAODElectrodeEnum];

                        var aodWaveformElectrodeDelay = new V0AODWaveformElectrodeDelay<TItem> { Electrodes = electrodes };
                        Cache.Step0Items = [.. Cache.Step0Items, aodWaveformElectrodeDelay];

                        var delays = Generate.LinearRangeContainsEdge(param.StartDelay, param.StepDelay, param.StopDelay);
                        Guard.IsNotEmpty(delays);

                        var currentDetailLogUniqueId = Guid.NewGuid();
                        var fileName = $"Details_{Steps[stepIndex].Replace(" ", string.Empty)}_{string.Join("_", aodWaveformElectrodeDelay.Electrodes)}";

                        Logger.LogHtmlInformation(aodWaveformElectrodeDelay.Title, HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {fileName}({currentDetailLogUniqueId:N})"), HtmlLogUniqueId.LoggingHtml());
                        Logger.LogHtmlInformation($"{currentDetailLogUniqueId:N}", HtmlHeaderLevelEnum.Header1, new HtmlComment(Name), currentDetailLogUniqueId.LoggingHtml());
                        Logger.LogHtmlInformation($"{string.Join("_", aodWaveformElectrodeDelay.Electrodes)}", HtmlHeaderLevelEnum.Header3, currentDetailLogUniqueId.LoggingHtml());

                        try
                        {
                            foreach (var frequency in Cache.Frequencies)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                var aodWaveformElectrodeDelayItem = new V0AODWaveformElectrodeDelayItem<TItem>();
                                aodWaveformElectrodeDelay.Items = [.. aodWaveformElectrodeDelay.Items, aodWaveformElectrodeDelayItem];

                                Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, currentDetailLogUniqueId.LoggingHtml());

                                foreach (var currentDelay in delays)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    var item = new TItem
                                    {
                                        ElectrodeConfigurations =
                                        [
                                            .. Cache.ElectrodeDelayParams
                                                .Select(t => new GenerateAODWaveformElectrodeConfiguration
                                                {
                                                    OpticsAODElectrodeEnum = t.OpticsAODElectrodeEnum,
                                                    Delay = t.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum
                                                        ? currentDelay
                                                        : Cache.ElectrodeConfigurationResults
                                                            .SingleOrDefault(tt => tt.OpticsAODElectrodeEnum == t.OpticsAODElectrodeEnum)
                                                            ?.Delay ?? 0d,
                                                    Amplitude = Cache.DefaultAmplitude,
                                                    IsGenerateAODWaveformZero = electrodes.Contains(t.OpticsAODElectrodeEnum) == false
                                                })
                                        ],
                                        Frequency = frequency,
                                        Amplitude = Cache.DefaultAmplitude,
                                        Delay = currentDelay
                                    };

                                    Logger.LogHtmlInformation($"{item.Delay}(ns)", HtmlHeaderLevelEnum.Header5, currentDetailLogUniqueId.LoggingHtml());

                                    await UpdateMeasurePowerAsync(item, currentDetailLogUniqueId, cancellationToken).ConfigureAwait(false);

                                    aodWaveformElectrodeDelayItem.FrequencyItems = [.. aodWaveformElectrodeDelayItem.FrequencyItems, item];
                                }
                            }
                        }
                        finally
                        {
                            Logger.LogHtmlInformation(currentDetailLogUniqueId.LoggedEndHtml(fileName));
                        }

                        aodWaveformElectrodeDelay.InterpolationMaxima(Cache.InterpolationCount);

                        var weightParams = (IReadOnlyList<V0AODWaveformElectrodeDelayFrequencyWeightParam>)
                        [
                            .. Cache.ElectrodeDelayFrequencyWeightParams
                                .Where(tt => tt.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum)
                        ];

                        aodWaveformElectrodeDelay.Delay = aodWaveformElectrodeDelay.ClosestMaximaPoints
                            .Index()
                            .Select(t => weightParams.Single(tt => tt.Frequency - aodWaveformElectrodeDelay.Items[t.Index].FrequencyItems[0].Frequency == 0).Weight * t.Item.X)
                            .Sum() / weightParams.Sum(t => t.Weight);

                        if (Cache.IsConfirmAODWaveformElectrodeDelayResult)
                        {
                            var aodWaveformElectrodeDelayStep0ConfirmResultWindowViewModel = HostApplication.GetRequiredService<V0AODWaveformElectrodeDelayConfirmResultWindowViewModel>();
                            aodWaveformElectrodeDelayStep0ConfirmResultWindowViewModel.Delay = aodWaveformElectrodeDelay.Delay.Value;

                            var showDialog = WindowManagerService.ShowDialog(aodWaveformElectrodeDelayStep0ConfirmResultWindowViewModel);
                            if (showDialog == true) aodWaveformElectrodeDelay.Delay = aodWaveformElectrodeDelayStep0ConfirmResultWindowViewModel.Delay;
                        }

                        Cache.ElectrodeConfigurationResults =
                        [
                            .. Cache.ElectrodeConfigurationResults, new GenerateAODWaveformElectrodeConfiguration
                            {
                                OpticsAODElectrodeEnum = param.OpticsAODElectrodeEnum,
                                Delay = aodWaveformElectrodeDelay.Delay.Value,
                                Amplitude = 1d, // 生成result默认幅值都是1
                                IsGenerateAODWaveformZero = false
                            }
                        ];
                    }

                    isSuccess = Cache.ElectrodeConfigurationResults.Count == Cache.ElectrodeDelayParams.Count;
                }
                finally
                {
                    var htmlBullet = new HtmlBullet(new
                    {
                        Table = new HtmlTable([.. Cache.ElectrodeConfigurationResults.Select(t => t.ToHtmlAnonymous())]),
                        ElectrodeDelayItems = new HtmlContainer([.. Cache.Step0Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])))])
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

            Guard.IsEqualTo(Cache.ElectrodeConfigurationResults.Count, Cache.IsOnlyElectrode4 ? 4 : Cache.ElectrodeDelayParams.Count);
            Guard.IsNotEmpty(Cache.ElectrodeDelayFrequencyUniformityParams);
            Guard.IsTrue(Cache.IsOnlyElectrode4
                ? Cache.ElectrodeDelayFrequencyUniformityParams.All(t => _opticsAODElectrodeEnums.Any(tt => t.OpticsAODElectrodeEnum == tt))
                : Cache.ElectrodeDelayFrequencyUniformityParams.All(t => Cache.ElectrodeDelayParams.Any(tt => t.OpticsAODElectrodeEnum == tt.OpticsAODElectrodeEnum)));
            Guard.IsGreaterThanOrEqualTo(Cache.ElectrodeDelayFrequencyUniformityParams.Count, Cache.ElectrodeDelayFrequencyUniformityParamChunkSize);
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
                foreach (var electrodeFrequencyUniformityParams in Cache.ElectrodeDelayFrequencyUniformityParams.Chunk(Cache.ElectrodeDelayFrequencyUniformityParamChunkSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var electrodes = electrodeFrequencyUniformityParams.Select(t => t.OpticsAODElectrodeEnum).ToArray();

                    var aodWaveformElectrodeDelayFrequencyUniformity = new V0AODWaveformElectrodeDelayFrequencyUniformity<TItem> { Electrodes = electrodes };
                    Cache.Step1Items = [.. Cache.Step1Items, aodWaveformElectrodeDelayFrequencyUniformity];

                    var aodWaveformElectrodeDelayFrequencyUniformityHmlLogUniqueId = Guid.NewGuid();

                    var fileName = $"Details_{Steps[stepIndex].Replace(" ", string.Empty)}_{string.Join("_", aodWaveformElectrodeDelayFrequencyUniformity.Electrodes)}";
                    Logger.LogHtmlInformation(aodWaveformElectrodeDelayFrequencyUniformity.Title, HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {fileName}({aodWaveformElectrodeDelayFrequencyUniformityHmlLogUniqueId:N})"),
                        HtmlLogUniqueId.LoggingHtml());
                    Logger.LogHtmlInformation($"{aodWaveformElectrodeDelayFrequencyUniformityHmlLogUniqueId:N}", HtmlHeaderLevelEnum.Header1, new HtmlComment(Name), aodWaveformElectrodeDelayFrequencyUniformityHmlLogUniqueId.LoggingHtml());
                    try
                    {
                        foreach (var frequency in frequencies)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var aodWaveformElectrodeDelayFrequencyUniformityItem = new V0AODWaveformElectrodeDelayFrequencyUniformityItem<TItem>();
                            aodWaveformElectrodeDelayFrequencyUniformity.Items = [.. aodWaveformElectrodeDelayFrequencyUniformity.Items, aodWaveformElectrodeDelayFrequencyUniformityItem];

                            Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, aodWaveformElectrodeDelayFrequencyUniformityHmlLogUniqueId.LoggingHtml());

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

                                Logger.LogHtmlInformation($"{item.Amplitude}(AMP)", HtmlHeaderLevelEnum.Header5, aodWaveformElectrodeDelayFrequencyUniformityHmlLogUniqueId.LoggingHtml());

                                await UpdateMeasurePowerAsync(item, aodWaveformElectrodeDelayFrequencyUniformityHmlLogUniqueId, cancellationToken).ConfigureAwait(false);

                                aodWaveformElectrodeDelayFrequencyUniformityItem.FrequencyItems = [.. aodWaveformElectrodeDelayFrequencyUniformityItem.FrequencyItems, item];
                            }
                        }
                    }
                    finally
                    {
                        Logger.LogHtmlInformation(aodWaveformElectrodeDelayFrequencyUniformityHmlLogUniqueId.LoggedEndHtml(fileName));
                    }

                    foreach (var electrodeConfiguration in Cache.ElectrodeConfigurationResults.Where(t => electrodes.Contains(t.OpticsAODElectrodeEnum)))
                    {
                        var points = aodWaveformElectrodeDelayFrequencyUniformity.PlotDataSource.GetScatterLines(1).Single().Source.Data;
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
                    ElectrodeDelayItems = new HtmlContainer([.. Cache.Step0Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])))]),
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