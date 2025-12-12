using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public abstract partial class AbstractAODWaveformElectrodeInitializeWindowViewModel<TCache, TItem, TResult> : AbstractAODWaveformCommonWindowViewModel<TCache, TItem, TResult>
    where TCache : AODWaveformElectrodeInitializeCache<TItem, TResult>, new()
    where TItem : AODWaveformElectrodeInitializeItem, new()
    where TResult : AODWaveformElectrodeInitializeResult, new()
{
    public override IReadOnlyList<string> Steps { get; } =
    [
        "Step1 Electrode2 Electrode4",
        "Step2 Electrode3",
        "Step3 Generate AOD Waveform",
        "Step4 Set AOD Waveform Config"
    ];

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0Async(bool isNotSilent, CancellationToken cancellationToken)
    {
        var htmlLogUniqueId = Guid.NewGuid();

        return await InvokeAsync(0, htmlLogUniqueId, async () =>
        {
            Guard.IsEqualTo(Cache.ElectrodeConfigurationResults.Count, 4);
            Cache.Step0.Items = [];

            await GetElectrodeMaxMeasurePowerAsync([OpticsAODElectrodeEnum.Electrode1, OpticsAODElectrodeEnum.Electrode2], Cache.Electrode2OffsetFrequencyPeriodCoefficients).ConfigureAwait(false);
            await GetElectrodeMaxMeasurePowerAsync([OpticsAODElectrodeEnum.Electrode3, OpticsAODElectrodeEnum.Electrode4], Cache.Electrode4OffsetFrequencyPeriodCoefficients).ConfigureAwait(false);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                ElectrodeOffsetItems = Cache.Step0.ScatterPlotControl.GetHtmlPlot2DLinesChart()
            }), htmlLogUniqueId.LoggingHtml());

            return true;

            async Task GetElectrodeMaxMeasurePowerAsync(IReadOnlyList<OpticsAODElectrodeEnum> electrodes, IReadOnlyList<double> offsetFrequencyPeriodCoefficients)
            {
                var step0Item = new AODWaveformElectrodeInitializeStep0Item<TItem> { Electrodes = electrodes };
                Cache.Step0.Items = [.. Cache.Step0.Items, step0Item];

                Logger.LogHtmlInformation(step0Item.Title, HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

                foreach (var offsetFrequencyPeriodCoefficient in offsetFrequencyPeriodCoefficients)
                {
                    var item = new TItem
                    {
                        ElectrodeConfigurations =
                        [
                            ..Cache.ElectrodeConfigurationResults
                                .Select(t => new GenerateAODWaveformElectrodeConfiguration
                                {
                                    OpticsAODElectrodeEnum = t.OpticsAODElectrodeEnum,
                                    OffsetFrequency = t.OffsetFrequency,
                                    OffsetFrequencyPeriodCoefficient = t.OpticsAODElectrodeEnum == electrodes[^1]
                                        ? offsetFrequencyPeriodCoefficient
                                        : 0,
                                    Amplitude = Cache.DefaultAmplitude,
                                    IsGenerateAODWaveformZero = electrodes.Contains(t.OpticsAODElectrodeEnum) == false
                                })
                        ],
                        OffsetFrequencyPeriodCoefficient = offsetFrequencyPeriodCoefficient
                    };

                    Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header5, htmlLogUniqueId.LoggingHtml());

                    await UpdateMeasurePowerAsync(item, false, htmlLogUniqueId, cancellationToken).ConfigureAwait(false);

                    step0Item.Items = [.. step0Item.Items, item];
                }

                Cache.ElectrodeConfigurationResults.Single(t => t.OpticsAODElectrodeEnum == electrodes[^1])
                    .OffsetFrequencyPeriodCoefficient = step0Item.Items.Maxima(t => t.MeasurePower).Single().OffsetFrequencyPeriodCoefficient;

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlTable([.. Cache.ElectrodeConfigurationResults.Select(t => t.ToHtmlAnonymous())]), htmlLogUniqueId.LoggingHtml());
            }
        }, isNotSilent).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1Async(bool isNotSilent, CancellationToken cancellationToken)
    {
        var htmlLogUniqueId = Guid.NewGuid();

        return await InvokeAsync(1, htmlLogUniqueId, async () =>
        {
            Guard.IsEqualTo(Cache.ElectrodeConfigurationResults.Count, 4);
            Guard.IsGreaterThanOrEqualTo(Cache.Frequencies.Count, 2);
            Guard.IsTrue(Cache.Frequencies.IsIncreasing(true));
            Guard.IsEqualTo(Cache.Frequencies.Count, Cache.Weights.Count);

            Cache.Step1Items = [];

            var electrodes = (IReadOnlyList<OpticsAODElectrodeEnum>)[OpticsAODElectrodeEnum.Electrode1, OpticsAODElectrodeEnum.Electrode2, OpticsAODElectrodeEnum.Electrode3];

            var aodWaveformElectrodeOffsetFrequencyPeriod = new AODWaveformElectrodeOffsetFrequencyPeriod<TItem> { Electrodes = electrodes };
            Cache.Step1Items = [.. Cache.Step1Items, aodWaveformElectrodeOffsetFrequencyPeriod];

            Logger.LogHtmlInformation(aodWaveformElectrodeOffsetFrequencyPeriod.Title, HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());

            var electrode2OffsetFrequencyPeriodCoefficient = Cache.ElectrodeConfigurationResults.Single(t => t.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode2).OffsetFrequencyPeriodCoefficient;
            var offsetFrequencyPeriodCoefficients = GenerateUtils.LinearContainsEdgeRange(
                electrode2OffsetFrequencyPeriodCoefficient + Cache.Electrode3OffsetFrequencyPeriodParam.StartOffsetFrequencyPeriodCoefficient,
                Cache.Electrode3OffsetFrequencyPeriodParam.StepOffsetFrequencyPeriodCoefficient,
                electrode2OffsetFrequencyPeriodCoefficient + Cache.Electrode3OffsetFrequencyPeriodParam.StopOffsetFrequencyPeriodCoefficient);
            Guard.IsNotEmpty(offsetFrequencyPeriodCoefficients);

            foreach (var frequency in Cache.Frequencies)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var aodWaveformElectrodeOffsetFrequencyPeriodItem = new AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem>();
                aodWaveformElectrodeOffsetFrequencyPeriod.Items = [.. aodWaveformElectrodeOffsetFrequencyPeriod.Items, aodWaveformElectrodeOffsetFrequencyPeriodItem];

                Logger.LogHtmlInformation($"{frequency}(MHz)", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

                foreach (var currentOffsetFrequencyPeriodCoefficient in offsetFrequencyPeriodCoefficients)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var item = new TItem
                    {
                        ElectrodeConfigurations =
                        [
                            ..Cache.ElectrodeConfigurationResults
                                .Select(t => new GenerateAODWaveformElectrodeConfiguration
                                {
                                    OpticsAODElectrodeEnum = t.OpticsAODElectrodeEnum,
                                    OffsetFrequency = t.OffsetFrequency,
                                    OffsetFrequencyPeriodCoefficient = t.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode3
                                        ? currentOffsetFrequencyPeriodCoefficient
                                        : t.OffsetFrequencyPeriodCoefficient,
                                    Amplitude = Cache.DefaultAmplitude,
                                    IsGenerateAODWaveformZero = electrodes.Contains(t.OpticsAODElectrodeEnum) == false
                                })
                        ],
                        Frequency = frequency,
                        OffsetFrequencyPeriodCoefficient = currentOffsetFrequencyPeriodCoefficient
                    };

                    Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header5, htmlLogUniqueId.LoggingHtml());

                    await UpdateMeasurePowerAsync(item, true, htmlLogUniqueId, cancellationToken).ConfigureAwait(false);

                    aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems = [.. aodWaveformElectrodeOffsetFrequencyPeriodItem.FrequencyItems, item];
                }
            }

            aodWaveformElectrodeOffsetFrequencyPeriod.InterpolationMaxima(Cache.InterpolationCount);

            aodWaveformElectrodeOffsetFrequencyPeriod.OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetFrequencyPeriod.ClosestMaximaPoints
                .Index()
                .Select(t => Cache.Weights[t.Index] * t.Item.X)
                .Sum() / Cache.Weights.Sum();

            Cache.ElectrodeConfigurationResults.Single(t => t.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode3)
                .OffsetFrequencyPeriodCoefficient = aodWaveformElectrodeOffsetFrequencyPeriod.OffsetFrequencyPeriodCoefficient.Value;
            Cache.ElectrodeConfigurationResults.Single(t => t.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode4)
                .OffsetFrequencyPeriodCoefficient += aodWaveformElectrodeOffsetFrequencyPeriod.OffsetFrequencyPeriodCoefficient.Value;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlTable([.. Cache.ElectrodeConfigurationResults.Select(t => t.ToHtmlAnonymous())]), htmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                ElectrodeOffsetItems = new HtmlContainer([.. Cache.Step1Items.Select(t => new HtmlExpand(t.Title, new HtmlContainer([.. t.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])))]),
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