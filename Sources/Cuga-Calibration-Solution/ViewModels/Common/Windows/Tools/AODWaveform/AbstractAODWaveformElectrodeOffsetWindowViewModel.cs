using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public abstract partial class AbstractAODWaveformElectrodeOffsetWindowViewModel<TCache, TItem, TResult> : AbstractAODWaveformCommonWindowViewModel<TCache, TItem, TResult>
    where TCache : AODWaveformElectrodeOffsetCache<TItem, TResult>, new()
    where TItem : AODWaveformElectrodeOffsetItem, new()
    where TResult : AODWaveformElectrodeOffsetResult, new()
{
    public override IReadOnlyList<string> Steps { get; } =
    [
        "Step1 Electrode Offset",
        "Step2 Uniformity",
        "Step3 Generate AOD Waveform",
        "Step4 Set AOD Waveform Config"
    ];

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0Async(bool isSilent, CancellationToken cancellationToken)
    {
        return await InvokeAsync(0, async () =>
        {
            Guard.IsNotEmpty(Cache.ElectrodeOffsetFrequencyPeriodParams);
            Guard.IsGreaterThanOrEqualTo(Cache.ElectrodeOffsetFrequencyPeriodParams.Count, 2);
            Guard.IsGreaterThanOrEqualTo(Cache.Frequencies.Count, 2);
            Guard.IsTrue(Cache.Frequencies.IsIncreasing(true));

            Cache.Step0Items = [];
            Cache.ElectrodeConfigurationResults =
            [
                new GenerateAODWaveformElectrodeConfiguration
                {
                    OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1,
                    OffsetFrequency = Cache.OffsetFrequency,
                    OffsetFrequencyPeriodCoefficient = 0d
                }
            ];

            foreach (var param in Cache.ElectrodeOffsetFrequencyPeriodParams)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (Cache.ElectrodeConfigurationResults.Any(t => t.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum)) continue;

                var electrodes = (OpticsAODElectrodeEnum[])[.. Cache.ElectrodeConfigurationResults.Select(t => t.OpticsAODElectrodeEnum), param.OpticsAODElectrodeEnum];

                var aodWaveformElectrodeOffsetFrequencyPeriod = new AODWaveformElectrodeOffsetFrequencyPeriod<TItem> { Electrodes = electrodes };
                Cache.Step0Items = [.. Cache.Step0Items, aodWaveformElectrodeOffsetFrequencyPeriod];

                Logger.LogHtmlInformation(aodWaveformElectrodeOffsetFrequencyPeriod.Title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                
                var offsetFrequencyPeriodCoefficients = GenerateUtils.LinearContainsEdgeRange(param.StartOffsetFrequencyPeriodCoefficient, param.StepOffsetFrequencyPeriodCoefficient, param.StopOffsetFrequencyPeriodCoefficient);
                Guard.IsNotEmpty(offsetFrequencyPeriodCoefficients);

                foreach (var frequency in Cache.Frequencies)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var aodWaveformElectrodeOffsetFrequencyPeriodItem = new AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem>();
                    aodWaveformElectrodeOffsetFrequencyPeriod.Items = [.. aodWaveformElectrodeOffsetFrequencyPeriod.Items, aodWaveformElectrodeOffsetFrequencyPeriodItem];

                    Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    foreach (var currentOffsetFrequencyPeriodCoefficient in offsetFrequencyPeriodCoefficients)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var item = new TItem
                        {
                            ElectrodeConfigurations =
                            [
                                ..Cache.ElectrodeOffsetFrequencyPeriodParams
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

                        Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

                        await UpdateMeasurePowerAsync(item, cancellationToken, isGenerateFlatnessAODWaveform: true).ConfigureAwait(false);

                        aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems = [.. aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems, item];
                    }
                }

                aodWaveformElectrodeOffsetFrequencyPeriod.InterpolationMaxima(Cache.InterpolationCount);

                var allWeight = Cache.ElectrodeOffsetFrequencyWeightParams
                    .Where(tt => tt.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum)
                    .Select(t => t.Weight)
                    .Sum();

                aodWaveformElectrodeOffsetFrequencyPeriod.OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetFrequencyPeriod.ClosestMaximaPoints
                    .Index()
                    .Select(t => Cache.ElectrodeOffsetFrequencyWeightParams
                        .Single(tt => tt.OpticsAODElectrodeEnum == param.OpticsAODElectrodeEnum &&
                                      Equals(tt.Frequency, aodWaveformElectrodeOffsetFrequencyPeriod.Items[t.Index].FrequencyItems[0].Frequency)).Weight * t.Item.X)
                    .Sum() / allWeight;

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
                        OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetFrequencyPeriod.OffsetFrequencyPeriodCoefficient.Value
                    }
                ];

                GC.Collect();
            }

            return Cache.ElectrodeConfigurationResults.Count == Cache.ElectrodeOffsetFrequencyPeriodParams.Count;
        }, isSilent).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1Async(bool isSilent, CancellationToken cancellationToken)
    {
        return await InvokeAsync(1, async () =>
        {
            Guard.IsNotEmpty(Cache.ElectrodeOffsetFrequencyPeriodParams);
            Guard.IsGreaterThanOrEqualTo(Cache.ElectrodeOffsetFrequencyPeriodParams.Count, 2);
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

                Logger.LogHtmlInformation(aodWaveformElectrodeOffsetFrequencyUniformity.Title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                foreach (var frequency in frequencies)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var aodWaveformElectrodeOffsetFrequencyUniformityItem = new AODWaveformElectrodeOffsetFrequencyUniformityItem<TItem>();
                    aodWaveformElectrodeOffsetFrequencyUniformity.Items = [.. aodWaveformElectrodeOffsetFrequencyUniformity.Items, aodWaveformElectrodeOffsetFrequencyUniformityItem];

                    Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

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

                        Logger.LogHtmlInformation($"{item.Amplitude}(AMP)", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

                        await UpdateMeasurePowerAsync(item, cancellationToken, isGenerateFlatnessAODWaveform: true).ConfigureAwait(false);

                        aodWaveformElectrodeOffsetFrequencyUniformityItem.FrequencyItems = [.. aodWaveformElectrodeOffsetFrequencyUniformityItem.FrequencyItems, item];
                    }
                }

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

                GC.Collect();
            }

            return true;
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

        var step0Task = GuardUtils.IsAssignableToType<Task<bool>>(Step0Command.ExecuteAsync(false));
        if (await step0Task == false) return;

        var step1Task = GuardUtils.IsAssignableToType<Task<bool>>(Step1Command.ExecuteAsync(false));
        if (await step1Task == false) return;

        var stepSecondLastTask = GuardUtils.IsAssignableToType<Task<bool>>(StepSecondLastCommand.ExecuteAsync(false));
        if (await stepSecondLastTask == false) return;

        await StepFirstLastCommand.ExecuteAsync(false);
    }
}