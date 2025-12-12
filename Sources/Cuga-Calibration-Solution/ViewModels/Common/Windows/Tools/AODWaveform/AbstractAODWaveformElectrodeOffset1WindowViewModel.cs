using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.MVVM;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public abstract partial class AbstractAODWaveformElectrodeOffset1WindowViewModel<TCache, TItem, TResult> : AbstractAODWaveformCommonWindowViewModel<TCache, TItem, TResult>
    where TCache : AODWaveformElectrodeOffset1Cache<TItem, TResult>, new()
    where TItem : AODWaveformElectrodeOffset1Item, new()
    where TResult : AODWaveformElectrodeOffset1Result, new()
{
    public readonly IReadOnlyList<OpticsAODElectrodeEnum> OpticsAODElectrodeEnums = [OpticsAODElectrodeEnum.Electrode1, OpticsAODElectrodeEnum.Electrode2, OpticsAODElectrodeEnum.Electrode3, OpticsAODElectrodeEnum.Electrode4];

    public override IReadOnlyList<string> Steps { get; } =
    [
        "Step1 Electrode Offset",
        "Step2 Uniformity",
        "Step3 Generate AOD Waveform",
        "Step4 Set AOD Waveform Config"
    ];

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0Async(bool isNotSilent, CancellationToken cancellationToken)
    {
        var htmlLogUniqueId = Guid.NewGuid();
        const int stepIndex = 0;

        return await InvokeAsync(0, htmlLogUniqueId, async () =>
        {
            Guard.IsGreaterThanOrEqualTo(Cache.Frequencies.Count, 2);
            Guard.IsTrue(Cache.Frequencies.IsIncreasing(true));

            Cache.Step0Items = [];
            Cache.ElectrodeConfigurationResults =
            [
                ..OpticsAODElectrodeEnums.Select(t => new GenerateAODWaveformElectrodeConfiguration
                {
                    OpticsAODElectrodeEnum = t,
                    OffsetFrequency = Cache.OffsetFrequency,
                    OffsetFrequencyPeriodCoefficient = 0d,
                    Amplitude = Cache.DefaultAmplitude,
                    IsGenerateAODWaveformZero = true
                })
            ];

            cancellationToken.ThrowIfCancellationRequested();
            await InvokeElectrodeMaxMeasurePowerAsync([OpticsAODElectrodeEnum.Electrode1, OpticsAODElectrodeEnum.Electrode2], Cache.Electrode2OffsetFrequencyPeriodParam).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            await InvokeElectrodeMaxMeasurePowerAsync([OpticsAODElectrodeEnum.Electrode3, OpticsAODElectrodeEnum.Electrode4], Cache.Electrode4OffsetFrequencyPeriodParam).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            await InvokeElectrodeMaxMeasurePowerAsync([OpticsAODElectrodeEnum.Electrode1, OpticsAODElectrodeEnum.Electrode2, OpticsAODElectrodeEnum.Electrode3, OpticsAODElectrodeEnum.Electrode4], Cache.Electrode3OffsetFrequencyPeriodParam).ConfigureAwait(false);

            var isSuccess = Cache.ElectrodeConfigurationResults.Count == OpticsAODElectrodeEnums.Count;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                ElectrodeOffsetItems = new HtmlContainer([.. Cache.Step0Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])))])
            }), htmlLogUniqueId.LoggingHtml());

            return isSuccess;

            async Task InvokeElectrodeMaxMeasurePowerAsync(IReadOnlyList<OpticsAODElectrodeEnum> electrodes, AODWaveformElectrodeOffsetFrequencyPeriodParam param)
            {
                var aodWaveformElectrodeOffsetFrequencyPeriod = new AODWaveformElectrodeOffsetFrequencyPeriod<TItem> { Electrodes = electrodes };
                Cache.Step0Items = [.. Cache.Step0Items, aodWaveformElectrodeOffsetFrequencyPeriod];

                var fileName = $"{Name}_{Steps[stepIndex]}_{aodWaveformElectrodeOffsetFrequencyPeriod.Title}";
                Logger.LogHtmlInformation(aodWaveformElectrodeOffsetFrequencyPeriod.Title, HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {fileName}"), htmlLogUniqueId.LoggingHtml());

                var offsetFrequencyPeriodCoefficients = GenerateUtils.LinearContainsEdgeRange(param.StartOffsetFrequencyPeriodCoefficient, param.StepOffsetFrequencyPeriodCoefficient, param.StopOffsetFrequencyPeriodCoefficient);
                Guard.IsNotEmpty(offsetFrequencyPeriodCoefficients);

                var aodWaveformElectrodeOffsetFrequencyPeriodHmlLogUniqueId = Guid.NewGuid();

                foreach (var frequency in Cache.Frequencies)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var aodWaveformElectrodeOffsetFrequencyPeriodItem = new AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem>();
                    aodWaveformElectrodeOffsetFrequencyPeriod.Items = [.. aodWaveformElectrodeOffsetFrequencyPeriod.Items, aodWaveformElectrodeOffsetFrequencyPeriodItem];

                    Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, aodWaveformElectrodeOffsetFrequencyPeriodHmlLogUniqueId.LoggingHtml());

                    foreach (var currentOffsetFrequencyPeriodCoefficient in offsetFrequencyPeriodCoefficients)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        TItem item;
                        if (param.OpticsAODElectrodeEnum != OpticsAODElectrodeEnum.Electrode3)
                            item = new TItem
                            {
                                ElectrodeConfigurations =
                                [
                                    ..Cache.ElectrodeConfigurationResults
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
                                    ..Cache.ElectrodeConfigurationResults
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

                        Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header5, aodWaveformElectrodeOffsetFrequencyPeriodHmlLogUniqueId.LoggingHtml());

                        await UpdateMeasurePowerAsync(item, true, aodWaveformElectrodeOffsetFrequencyPeriodHmlLogUniqueId, cancellationToken).ConfigureAwait(false);

                        aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems = [.. aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems, item];
                    }
                }

                Logger.LogHtmlInformation(aodWaveformElectrodeOffsetFrequencyPeriodHmlLogUniqueId.LoggedEndHtml(fileName));

                aodWaveformElectrodeOffsetFrequencyPeriod.InterpolationMaxima(Cache.InterpolationCount);

                aodWaveformElectrodeOffsetFrequencyPeriod.OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetFrequencyPeriod.ClosestMaximaPoints
                    .Index()
                    .Select(t => Cache.Weights[t.Index] * t.Item.X)
                    .Sum() / Cache.Weights.Sum();

                if (Cache.IsConfirmAODWaveformElectrodeOffsetResult)
                {
                    var aodWaveformElectrodeOffsetStep0ConfirmResultWindowViewModel = HostApplication.GetRequiredService<AODWaveformElectrodeOffsetFrequencyPeriodConfirmResultWindowViewModel>();
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
        }, isNotSilent).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1Async(bool isNotSilent, CancellationToken cancellationToken)
    {
        var htmlLogUniqueId = Guid.NewGuid();
        const int stepIndex = 1;

        return await InvokeAsync(1, htmlLogUniqueId, async () =>
        {
            Guard.IsNotEmpty(Cache.ElectrodeOffsetFrequencyUniformityParams);
            Guard.IsGreaterThanOrEqualTo(Cache.ElectrodeOffsetFrequencyUniformityParams.Count, Cache.ElectrodeOffsetFrequencyUniformityParamChunkSize);
            Guard.IsGreaterThanOrEqualTo(Cache.Frequencies.Count, 2);
            Guard.IsTrue(Cache.Frequencies.IsIncreasing(true));

            // 配合界面直接修改结果, 更新step0的界面
            foreach (var step0 in Cache.Step0Items)
            {
                step0.OffsetFrequencyPeriodCoefficient = Cache.ElectrodeConfigurationResults
                    .Single(t => t.OpticsAODElectrodeEnum == step0.Electrodes[^1])
                    .OffsetFrequencyPeriodCoefficient;
            }

            Cache.Step1Items = [];
            foreach (var electrodeConfiguration in Cache.ElectrodeConfigurationResults)
            {
                electrodeConfiguration.UniformityConfigurations = [];
            }

            var frequencies = GenerateUtils.LinearContainsEdgeRange(Cache.Frequencies[0], Cache.StepFrequency, Cache.Frequencies[^1]);
            Guard.IsNotEmpty(frequencies);
            foreach (var electrodeFrequencyUniformityParams in Cache.ElectrodeOffsetFrequencyUniformityParams.Chunk(Cache.ElectrodeOffsetFrequencyUniformityParamChunkSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var electrodes = electrodeFrequencyUniformityParams.Select(t => t.OpticsAODElectrodeEnum).ToArray();

                var aodWaveformElectrodeOffsetFrequencyUniformity = new AODWaveformElectrodeOffsetFrequencyUniformity<TItem> { Electrodes = electrodes };
                Cache.Step1Items = [.. Cache.Step1Items, aodWaveformElectrodeOffsetFrequencyUniformity];

                var fileName = $"{Name}_{Steps[stepIndex]}_{aodWaveformElectrodeOffsetFrequencyUniformity.Title}";
                Logger.LogHtmlInformation(aodWaveformElectrodeOffsetFrequencyUniformity.Title, HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {fileName}"), htmlLogUniqueId.LoggingHtml());

                var aodWaveformElectrodeOffsetFrequencyUniformityHmlLogUniqueId = Guid.NewGuid();

                foreach (var frequency in frequencies)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var aodWaveformElectrodeOffsetFrequencyUniformityItem = new AODWaveformElectrodeOffsetFrequencyUniformityItem<TItem>();
                    aodWaveformElectrodeOffsetFrequencyUniformity.Items = [.. aodWaveformElectrodeOffsetFrequencyUniformity.Items, aodWaveformElectrodeOffsetFrequencyUniformityItem];

                    Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, aodWaveformElectrodeOffsetFrequencyUniformityHmlLogUniqueId.LoggingHtml());

                    var amplitudes = GenerateUtils.LinearContainsEdgeRange(electrodeFrequencyUniformityParams[0].StartAmplitude, electrodeFrequencyUniformityParams[0].StepAmplitude, electrodeFrequencyUniformityParams[0].StopAmplitude).Reverse().ToArray();
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

                Logger.LogHtmlInformation(aodWaveformElectrodeOffsetFrequencyUniformityHmlLogUniqueId.LoggedEndHtml(fileName));

                foreach (var electrodeConfiguration in Cache.ElectrodeConfigurationResults.Where(t => electrodes.Contains(t.OpticsAODElectrodeEnum)))
                {
                    var points = aodWaveformElectrodeOffsetFrequencyUniformity.ScatterPlotControl.GetScatterLines(1).Single().ScatterSourcePoints.Points;
                    if (points.Count != frequencies.Length) return ThrowHelper.ThrowArgumentException<bool>($"{nameof(points)} count != {nameof(frequencies)} count");

                    electrodeConfiguration.UniformityConfigurations =
                    [
                        ..points.Select(t => new GenerateAODWaveformUniformityConfiguration
                        {
                            Frequency = t.X,
                            Coefficient = t.Y
                        })
                    ];
                }
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                ElectrodeOffsetItems = new HtmlContainer([.. Cache.Step0Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])))]),
                UniformityItems = new HtmlContainer([.. Cache.Step1Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])))])
            }), htmlLogUniqueId.LoggingHtml());

            return true;
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

        var step0Task = GuardUtils.IsAssignableToType<Task<bool>>(Step0Command.ExecuteAsync( /* isNotSilent */ false));
        if (await step0Task == false) return;

        var step1Task = GuardUtils.IsAssignableToType<Task<bool>>(Step1Command.ExecuteAsync( /* isNotSilent */ false));
        if (await step1Task == false) return;

        var stepSecondLastTask = GuardUtils.IsAssignableToType<Task<bool>>(StepSecondLastCommand.ExecuteAsync( /* isNotSilent */ false));
        if (await stepSecondLastTask == false) return;

        await StepFirstLastCommand.ExecuteAsync( /* isNotSilent */ false);
    }
}