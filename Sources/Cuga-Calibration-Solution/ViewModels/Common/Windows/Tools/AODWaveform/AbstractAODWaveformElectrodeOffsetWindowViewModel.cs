using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathNet.Numerics;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using CommunityToolkit.Diagnostics;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformElectrodeOffsetCache<TItem> : AODWaveformCommonCache
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    #region Param

    [ObservableProperty]
    private double _lowFrequency;

    [ObservableProperty]
    private double _highFrequency;

    [ObservableProperty]
    private double _offsetFrequency;

    [ObservableProperty]
    private double _startOffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _stepOffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _stopOffsetFrequencyPeriodCoefficient;

    #endregion Param

    #region Result

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LowFrequencyPoints))]
    [NotifyPropertyChangedFor(nameof(ResultPoints))]
    private TItem[] _lowFrequencyItems = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HighFrequencyPoints))]
    [NotifyPropertyChangedFor(nameof(ResultPoints))]
    private TItem[] _highFrequencyItems = [];

    public Point[] LowFrequencyPoints => [.. LowFrequencyItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))];

    public Point[] HighFrequencyPoints => [.. HighFrequencyItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))];

    public Point[] ResultPoints => LowFrequencyItems.Concat(HighFrequencyItems)
        .GroupBy(t => t.OffsetFrequencyPeriodCoefficient)
        .Select(g => new Point(
            g.Key,
            g.Sum(x => x.MeasurePower)
        ))
        .ToArray();

    #endregion Result

    public override object ToHtmlAnonymous() => new
    {
        LowFrequency,
        HighFrequency,
        OffsetFrequency,
        StartOffsetFrequencyPeriodCoefficient,
        StepOffsetFrequencyPeriodCoefficient,
        StopOffsetFrequencyPeriodCoefficient,
        Base = new HtmlBullet(base.ToHtmlAnonymous())
    };
}

public partial class AODWaveformElectrodeOffsetItem : AODWaveformCommonItem
{
    [ObservableProperty]
    private double _frequency;

    [ObservableProperty]
    private double _offsetFrequencyPeriodCoefficient;

    public override object ToHtmlAnonymous() => new HtmlQuote(new
    {
        Frequency,
        OffsetFrequencyPeriodCoefficient,
        Base = new HtmlBullet(base.ToHtmlAnonymous())
    });
}

public abstract partial class AbstractAODWaveformElectrodeOffsetWindowViewModel<TCache, TItem> : AbstractAODWaveformCommonWindowViewModel<TCache, TItem>
    where TCache : AODWaveformElectrodeOffsetCache<TItem>, new()
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    protected override void LoggerResult()
    {
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step2Async(CancellationToken cancellationToken)
    {
        await InvokeAsync("Step2 Electrode Offset", async () =>
        {
            var offsetFrequencyPeriodCoefficients = Generate.LinearRange(Cache.StartOffsetFrequencyPeriodCoefficient, Cache.StepOffsetFrequencyPeriodCoefficient, Cache.StopOffsetFrequencyPeriodCoefficient);
            Guard.IsNotEmpty(offsetFrequencyPeriodCoefficients);

            Cache.LowFrequencyItems = [];
            Cache.HighFrequencyItems = [];

            Logger.LogHtmlInformation($"{Cache.LowFrequency}(MHz)", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            foreach (var currentOffsetFrequencyPeriodCoefficient in offsetFrequencyPeriodCoefficients)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = new TItem
                {
                    Frequency = Cache.LowFrequency,
                    OffsetFrequencyPeriodCoefficient = currentOffsetFrequencyPeriodCoefficient
                };

                Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

                Cache.LowFrequencyItems = [.. Cache.LowFrequencyItems, item];
            }

            Logger.LogHtmlInformation($"{Cache.HighFrequency}(MHz)", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            foreach (var currentOffsetFrequencyPeriodCoefficient in offsetFrequencyPeriodCoefficients)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = new TItem
                {
                    Frequency = Cache.HighFrequency,
                    OffsetFrequencyPeriodCoefficient = currentOffsetFrequencyPeriodCoefficient
                };

                Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

                Cache.HighFrequencyItems = [.. Cache.HighFrequencyItems, item];
            }

            Logger.LogHtmlInformation("Low Frequency Table", HtmlHeaderLevelEnum.Header3, new HtmlTable([.. Cache.LowFrequencyItems.Select(t => t.ToHtmlAnonymous())]), HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation("High Frequency Table", HtmlHeaderLevelEnum.Header3, new HtmlTable([.. Cache.HighFrequencyItems.Select(t => t.ToHtmlAnonymous())]), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                FrequencyPoints = new HtmlPlot2DLinesChart([(nameof(Cache.LowFrequencyPoints), Cache.LowFrequencyPoints), (nameof(Cache.HighFrequencyPoints), Cache.HighFrequencyPoints)], string.Empty),
                ResultPoints = new HtmlPlot2DLinesChart([(string.Empty, Cache.ResultPoints)], string.Empty),
                Result = Cache.ResultPoints.OrderBy(t => t.Y).First()
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        }).ConfigureAwait(false);
    }
}