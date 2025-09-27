using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathNet.Numerics;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using CommunityToolkit.Diagnostics;
using MiniExcelLibs;
using Constants = Net.Utilities.Models.Constants;

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

    [ObservableProperty]
    private double _resultStartFrequency;

    [ObservableProperty]
    private double _resultStepFrequency;

    [ObservableProperty]
    private double _resultStopFrequency;

    #endregion Param

    #region Result

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LowFrequencyPoints))]
    [NotifyPropertyChangedFor(nameof(MergeFrequencyPoints))]
    private TItem[] _lowFrequencyItems = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HighFrequencyPoints))]
    [NotifyPropertyChangedFor(nameof(MergeFrequencyPoints))]
    private TItem[] _highFrequencyItems = [];

    public Point[] LowFrequencyPoints => [.. LowFrequencyItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))];

    public Point[] HighFrequencyPoints => [.. HighFrequencyItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))];

    public Point[] MergeFrequencyPoints => LowFrequencyItems.Concat(HighFrequencyItems)
        .GroupBy(t => t.OffsetFrequencyPeriodCoefficient)
        .Select(g => new Point(
            g.Key,
            g.Sum(x => x.MeasurePower)
        ))
        .ToArray();

    [ObservableProperty]
    private double _resultOffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _resultFrequencyMeasurePower;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResultMeasurePowerPoints))]
    private TItem[] _resultItems = [];

    public Point[] ResultMeasurePowerPoints => [.. ResultItems.Select(t => new Point(t.Frequency, t.MeasurePower))];

    #endregion Result

    public override object ToHtmlAnonymous() => new
    {
        LowFrequency,
        HighFrequency,
        OffsetFrequency,
        StartOffsetFrequencyPeriodCoefficient,
        StepOffsetFrequencyPeriodCoefficient,
        StopOffsetFrequencyPeriodCoefficient,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}

public partial class AODWaveformElectrodeOffsetItem : AODWaveformCommonItem
{
    [ObservableProperty]
    private double _frequency;

    [ObservableProperty]
    private double _offsetFrequencyPeriodCoefficient;

    public override object ToHtmlAnonymous() => new
    {
        Frequency,
        OffsetFrequencyPeriodCoefficient,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}

public abstract partial class AbstractAODWaveformElectrodeOffsetWindowViewModel<TCache, TItem> : AbstractAODWaveformCommonWindowViewModel<TCache, TItem>
    where TCache : AODWaveformElectrodeOffsetCache<TItem>, new()
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    protected string AODWaveformCsvResultFilePath => Path.Combine(ApplicationSetting.AppHomeDirectory, "csv", $"{GetType().Name}.csv");

    protected override void LoggerResult()
    {
        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            FrequencyPoints = new HtmlPlot2DLinesChart([(nameof(Cache.LowFrequencyPoints), Cache.LowFrequencyPoints), (nameof(Cache.HighFrequencyPoints), Cache.HighFrequencyPoints), (nameof(Cache.HighFrequencyPoints), Cache.MergeFrequencyPoints)], string.Empty),
            Cache.ResultOffsetFrequencyPeriodCoefficient,
            Cache.ResultFrequencyMeasurePower,
            ResultMeasurePowerPoints = new HtmlPlot2DLinesChart([(nameof(Cache.ResultMeasurePowerPoints), Cache.ResultMeasurePowerPoints)], string.Empty),
        }), HtmlLogUniqueId.LoggingHtml());
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeAsync("Step1 Electrode Offset", async () =>
        {
            Cache.LowFrequencyItems = [];
            Cache.HighFrequencyItems = [];
            Cache.ResultItems = [];

            GenerateFixedAODWaveform(cancellationToken);

            var offsetFrequencyPeriodCoefficients = Generate.LinearRange(Cache.StartOffsetFrequencyPeriodCoefficient, Cache.StepOffsetFrequencyPeriodCoefficient, Cache.StopOffsetFrequencyPeriodCoefficient);
            Guard.IsNotEmpty(offsetFrequencyPeriodCoefficients);

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

                Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

                Cache.HighFrequencyItems = [.. Cache.HighFrequencyItems, item];
            }

            var maxMergeFrequencyPoint = Cache.MergeFrequencyPoints.OrderByDescending(t => t.Y).First();
            Cache.ResultOffsetFrequencyPeriodCoefficient = maxMergeFrequencyPoint.X;
            Cache.ResultFrequencyMeasurePower = maxMergeFrequencyPoint.Y;

            var frequencies = Generate.LinearRange(Cache.ResultStartFrequency, Cache.ResultStepFrequency, Cache.ResultStopFrequency);
            Guard.IsNotEmpty(frequencies);

            Logger.LogHtmlInformation($"{Cache.ResultOffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            foreach (var frequency in frequencies)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = new TItem
                {
                    Frequency = frequency,
                    OffsetFrequencyPeriodCoefficient = Cache.ResultOffsetFrequencyPeriodCoefficient
                };

                Logger.LogHtmlInformation($"{item.Frequency}(MHz)", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

                Cache.ResultItems = [.. Cache.ResultItems, item];
            }

            await MiniExcel.InsertAsync(AODWaveformCsvResultFilePath, new
            {
                DateTime = DateTime.Now.ToString(Constants.LongFileDateTimeFormat),
                Cache.ResultOffsetFrequencyPeriodCoefficient,
                Cache.ResultFrequencyMeasurePower,
                LowFrequencyPoints = string.Join(";", Cache.LowFrequencyPoints.Select(t => $"{t.X}(2pi) {t.Y}(mW)")),
                HighFrequencyPoints = string.Join(";", Cache.HighFrequencyPoints.Select(t => $"{t.X}(2pi) {t.Y}(mW)")),
                MergeFrequencyPoints = string.Join(";", Cache.MergeFrequencyPoints.Select(t => $"{t.X}(2pi) {t.Y}(mW)")),
                ResultMeasurePowerPoints = string.Join(";", Cache.ResultMeasurePowerPoints.Select(t => $"{t.X}(MHz) {t.Y}(mW)"))
            }, cancellationToken: cancellationToken);

            DialogWindowProvider.ShowDialog($"Result Period: {Cache.ResultOffsetFrequencyPeriodCoefficient}(2pi)  Power: {Cache.ResultFrequencyMeasurePower}(mW)");

            return true;
        }).ConfigureAwait(false);
    }
}