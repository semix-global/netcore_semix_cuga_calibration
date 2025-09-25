using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.AODWaveform.Generates;
using MathNet.Numerics;
using MiniExcelLibs;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformUniformityCache<TItem> : AODWaveformCommonCache
    where TItem : AODWaveformUniformityItem, new()
{
    #region Param

    [ObservableProperty]
    private double _startFrequency;

    [ObservableProperty]
    private double _stepFrequency;

    [ObservableProperty]
    private double _stopFrequency;

    [ObservableProperty]
    private double _targetMeasurePower;

    [ObservableProperty]
    private double _retryCount = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetThresholdRateMin))]
    [NotifyPropertyChangedFor(nameof(TargetThresholdRateMax))]
    private double _targetThreshold = 0.05;

    public double TargetThresholdRateMin => 1 - TargetThreshold;

    public double TargetThresholdRateMax => 1 + TargetThreshold;

    #endregion Param

    #region Result

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MeasurePowerPoints))]
    [NotifyPropertyChangedFor(nameof(CoefficientPoints))]
    private TItem[] _items = [];

    public Point[] MeasurePowerPoints => [.. Items.Select(t => new Point(t.Frequency, t.MeasurePower))];

    public Point[] CoefficientPoints => [.. Items.Select(t => new Point(t.Frequency, t.Coefficient))];

    #endregion Result

    public override object ToHtmlAnonymous() => new
    {
        StartFrequency,
        StepFrequency,
        StopFrequency,
        TargetMeasurePower,
        RetryCount,
        TargetThreshold,
        TargetThresholdRateMin,
        TargetThresholdRateMax,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}

public partial class AODWaveformUniformityItem : AODWaveformCommonItem
{
    [ObservableProperty]
    private double _frequency;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Coefficient))]
    private double _defaultAmplitude;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Coefficient))]
    private double _amplitude;

    public double Coefficient => Amplitude <= DefaultAmplitude ? Amplitude / DefaultAmplitude : ThrowHelper.ThrowArgumentException<double>(nameof(Amplitude), "Amplitude must be greater than DefaultAmplitude");

    [ObservableProperty]
    private double _rate;

    [ObservableProperty]
    private bool _isOk;

    public override object ToHtmlAnonymous() => new
    {
        Frequency,
        DefaultAmplitude,
        Amplitude,
        Coefficient,
        Base = new HtmlQuote(base.ToHtmlAnonymous()),
        Rate,
        IsOk
    };
}

public abstract partial class AbstractAODWaveformUniformityWindowViewModel<TCache, TItem> : AbstractAODWaveformCommonWindowViewModel<TCache, TItem>
    where TCache : AODWaveformUniformityCache<TItem>, new()
    where TItem : AODWaveformUniformityItem, new()
{
    [ObservableProperty]
    private Point[] _measureCoefficientPowerPoints = [];

    [ObservableProperty]
    private TItem? _selectedItem;

    protected override void LoggerResult()
    {
        Logger.LogHtmlInformation("Table", HtmlHeaderLevelEnum.Header3, new HtmlTable([.. Cache.Items.Select(t => t.ToHtmlAnonymous())]), HtmlLogUniqueId.LoggingHtml());
        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            Cache.TargetMeasurePower,
            MeasurePowerPoints = new HtmlPlot2DLinesChart([(string.Empty, Cache.MeasurePowerPoints)], string.Empty),
            CoefficientPoints = new HtmlPlot2DLinesChart([(string.Empty, Cache.CoefficientPoints)], string.Empty)
        }), HtmlLogUniqueId.LoggingHtml());
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeAsync("Step1 Measure Power", async () =>
        {
            Cache.Items = [];

            GenerateFixedAODWaveform(cancellationToken);

            var frequencies = Generate.LinearRange(Cache.StartFrequency, Cache.StepFrequency, Cache.StopFrequency);
            Guard.IsNotEmpty(frequencies);

            foreach (var frequency in frequencies)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = new TItem
                {
                    Frequency = frequency,
                    DefaultAmplitude = Cache.DefaultAmplitude,
                    Amplitude = Cache.DefaultAmplitude
                };

                Logger.LogHtmlInformation($"{item.Frequency}(MHz) {item.Amplitude}(AMP)", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

                Cache.Items = [.. Cache.Items, item];
            }

            Cache.TargetMeasurePower = Cache.Items.Min(t => t.MeasurePower);

            foreach (var item in Cache.Items) item.Rate = item.MeasurePower / Cache.TargetMeasurePower;

            return true;
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1OneAsync(CancellationToken cancellationToken)
    {
        await InvokeAsync("Step1 Measure Power One", async () =>
        {
            if (SelectedItem is null) return false;

            foreach (var item in Cache.Items) item.IsOk = false;

            Logger.LogHtmlInformation($"{SelectedItem.Frequency}(MHz) {SelectedItem.Amplitude}(AMP)", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            await UpdateMeasurePowerAsync(SelectedItem, cancellationToken).ConfigureAwait(false);

            Cache.TargetMeasurePower = Cache.Items.Min(t => t.MeasurePower);

            return true;
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step2Async(CancellationToken cancellationToken)
    {
        await InvokeAsync("Step2 Uniformity", async () =>
        {
            var results = new List<bool>();
            foreach (var item in Cache.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Logger.LogHtmlInformation($"{item.Frequency}(MHz) {item.Amplitude}(AMP)", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                results.Add(await UniformityAsync(item, cancellationToken).ConfigureAwait(false));
            }

            return results.All(t => t);
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step2OneAsync(CancellationToken cancellationToken)
    {
        await InvokeAsync("Step2 Uniformity One", async () =>
        {
            if (SelectedItem is null) return false;

            Logger.LogHtmlInformation($"{SelectedItem.Frequency}(MHz) {SelectedItem.Amplitude}(AMP)", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            return await UniformityAsync(SelectedItem, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step3Async(CancellationToken cancellationToken)
    {
        await InvokeAsync("Step3 Save", () =>
        {
            if (SelectedItem is null) return Task.FromResult(false);

            Guard.IsTrue(Cache.Items.All(t => t.IsOk));

            var dialog = DialogWindowProvider.TryShowSaveFilePathDialog(".xlsx", out var filePath);
            if (dialog == false) return Task.FromResult(false);

            FileHelper.DeleteFileIfExists(filePath);
            MiniExcel.SaveAs(filePath, Cache.CoefficientPoints.Select(t => new GenerateAODWaveformUniformityConfiguration { Frequency = t.X, Coefficient = t.Y }));

            Logger.LogHtmlInformation("Save", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                filePath
            }), HtmlLogUniqueId.LoggingHtml());

            return Task.FromResult(true);
        }).ConfigureAwait(false);
    }

    private async Task<bool> UniformityAsync(TItem item, CancellationToken cancellationToken)
    {
        item.IsOk = false;
        MeasureCoefficientPowerPoints = [];

        if (Verify().IsOk)
        {
            item.IsOk = true;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, GetHtmlQuote(), HtmlLogUniqueId.LoggingHtml());

            return true;
        }

        var retryCount = 0;

        // 二分查找
        var lowAmplitude = 0d;
        var highAmplitude = item.DefaultAmplitude;
        while (lowAmplitude <= highAmplitude)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var midAmplitude = (highAmplitude + lowAmplitude) / 2;
            item.Amplitude = midAmplitude;

            Logger.LogHtmlInformation($"{item.Amplitude}(AMP)", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());
            await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

            MeasureCoefficientPowerPoints = [.. MeasureCoefficientPowerPoints, new Point(midAmplitude, item.MeasurePower)];
            MeasureCoefficientPowerPoints = [.. MeasureCoefficientPowerPoints.OrderBy(t => t.X)];

            var (isOk, isLessThan) = Verify();
            if (isOk)
            {
                item.IsOk = true;

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, GetHtmlQuote(), HtmlLogUniqueId.LoggingHtml());

                return true;
            }

            if (isLessThan)
            {
                lowAmplitude = midAmplitude;
            }
            else
            {
                highAmplitude = midAmplitude;
            }

            retryCount += 1;

            if (retryCount > Cache.RetryCount) break;
        }

        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, GetHtmlQuote(), HtmlLogUniqueId.LoggingHtml());

        return false;

        (bool IsOk, bool IsLessThan) Verify()
        {
            var rate = item.MeasurePower / Cache.TargetMeasurePower;
            item.Rate = rate;

            return (Cache.TargetThresholdRateMin < rate && rate < Cache.TargetThresholdRateMax, item.MeasurePower < Cache.TargetMeasurePower);
        }

        HtmlQuote GetHtmlQuote() => new(new
        {
            item = new HtmlQuote(item.ToHtmlAnonymous()),
            MeasureCoefficientPowerPoints = new HtmlPlot2DLinesChart([(string.Empty, [.. MeasureCoefficientPowerPoints])], string.Empty)
        });
    }
}