using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Local.NoSQL.DB.Providers.Bases;
using Local.NoSQL.DB.Providers.Interfaces;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformUniformityCache<TParam> : ObservableCacheBase
    where TParam : AbstractGenerateAODWaveformParam, new()
{
    [ObservableProperty]
    private TParam _param = new();

    [ObservableProperty]
    private double _waitTime = 15;

    [ObservableProperty]
    private double _startFrequency;

    [ObservableProperty]
    private double _stepFrequency;

    [ObservableProperty]
    private double _stopFrequency;

    [ObservableProperty]
    private double _defaultAmplitude;

    [ObservableProperty]
    private double _targetMeasurePower;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetThresholdRateMin))]
    [NotifyPropertyChangedFor(nameof(TargetThresholdRateMax))]
    private double _targetThreshold = 0.05;

    public double TargetThresholdRateMin => 1 - TargetThreshold;

    public double TargetThresholdRateMax => 1 + TargetThreshold;

    [ObservableProperty]
    private double _retryCount = 20;
}

public sealed partial class AODWaveformUniformityItem<TProfile> : ObservableCacheBase
    where TProfile : AbstractAODWaveformProfile
{
    [ObservableProperty]
    private IReadOnlyList<TProfile> _profiles = [];

    [ObservableProperty]
    private string _aODWaveformResultFilePath = string.Empty;

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
    private double _measurePower;

    [ObservableProperty]
    private double _rate;

    [ObservableProperty]
    private bool _isOk;
}

public abstract partial class AbstractAODWaveformUniformityWindowViewModel<TParam, TProfile> : AbstractAODWaveformCommonViewModel<TParam, TProfile>
    where TParam : AbstractGenerateAODWaveformParam, new()
    where TProfile : AbstractAODWaveformProfile
{
    protected readonly ILogger<AbstractAODWaveformUniformityWindowViewModel<TParam, TProfile>> Logger;
    protected readonly ICacheProvider CacheProvider;
    protected readonly IDialogWindowProvider DialogWindowProvider;
    protected readonly LaserViewModel LaserViewModel;

    [ObservableProperty]
    private AODWaveformUniformityCache<TParam> _cache = new();

    [ObservableProperty]
    private Point[] _measureCoefficientPowerPoints = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MeasurePowerPoints))]
    [NotifyPropertyChangedFor(nameof(CoefficientPoints))]
    private AODWaveformUniformityItem<TProfile>[] _items = [];

    public Point[] MeasurePowerPoints => [.. Items.Select(t => new Point(t.Frequency, t.MeasurePower))];

    public Point[] CoefficientPoints => [.. Items.Select(t => new Point(t.Frequency, t.Coefficient))];

    protected Guid HtmlLogUniqueId { get; private set; }

    protected AbstractAODWaveformUniformityWindowViewModel()
    {
        Logger = (ILogger<AbstractAODWaveformUniformityWindowViewModel<TParam, TProfile>>)HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        CacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
        DialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();
        LaserViewModel = HostApplication.GetRequiredService<LaserViewModel>();
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeAsync("Step1 Measure Power", async () =>
        {
            Items = [];

            foreach (var frequency in Generate.LinearRange(Cache.StartFrequency, Cache.StepFrequency, Cache.StopFrequency))
            {
                var item = new AODWaveformUniformityItem<TProfile>
                {
                    Frequency = frequency,
                    DefaultAmplitude = Cache.DefaultAmplitude,
                    Amplitude = Cache.DefaultAmplitude
                };

                Logger.LogHtmlInformation($"{item.Frequency}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

                Items = [.. Items, item];
            }

            Cache.TargetMeasurePower = Items.Min(t => t.MeasurePower);

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step2Async(CancellationToken cancellationToken)
    {
        await InvokeAsync("Step2 Uniformity", async () =>
        {
            Items = [];
            var results = new List<bool>();
            foreach (var item in Items)
            {
                results.Add(await UniformityAsync(item, cancellationToken).ConfigureAwait(false));
            }

            return results.All(t => t);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1OneAsync(AODWaveformUniformityItem<TProfile>? selectItem, CancellationToken cancellationToken)
    {
        await InvokeAsync("Step1 Measure Power One", async () =>
        {
            if (selectItem is null) return false;

            Logger.LogHtmlInformation($"{selectItem.Frequency}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            await UpdateMeasurePowerAsync(selectItem, cancellationToken).ConfigureAwait(false);

            Cache.TargetMeasurePower = Items.Min(t => t.MeasurePower);

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step2OneAsync(AODWaveformUniformityItem<TProfile>? selectItem, CancellationToken cancellationToken)
    {
        await InvokeAsync("Step2 Uniformity One", async () =>
        {
            if (selectItem is null) return false;

            return await UniformityAsync(selectItem, cancellationToken).ConfigureAwait(false);
        });
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            var dialog = DialogWindowProvider.TryShowSaveFilePathDialog(".xlsx", out var filePath);
            if (dialog == false) return;

            FileHelper.DeleteFileIfExists(filePath);
            MiniExcel.SaveAs(filePath, Items.Select(t => new GenerateAODWaveformUniformityConfiguration { Frequency = t.Frequency, Coefficient = t.Coefficient }));

            DialogWindowProvider.ShowDialog($"Save {AODWaveformName} AOD Waveform Uniformity Success!");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Save {@AODWaveformName} AOD Waveform Uniformity", nameof(AbstractAODWaveformUniformityWindowViewModel<TParam, TProfile>), AODWaveformName);
            DialogWindowProvider.ShowDialog($"""
                                             Save {AODWaveformName} AOD Waveform Uniformity Failed!
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseView(true);
    }

    private async Task InvokeAsync(string stepName, Func<Task<bool>> func)
    {
        HtmlLogUniqueId = Guid.NewGuid();

        Logger.LogHtmlInformation(AODWaveformName, HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
        Logger.LogHtmlInformation(stepName, HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Cache.WaitTime,
            Cache.StartFrequency,
            Cache.StepFrequency,
            Cache.StopFrequency,
            Cache.TargetMeasurePower,
            Cache.TargetThreshold,
            Cache.TargetThresholdRateMin,
            Cache.TargetThresholdRateMax,
            Cache.RetryCount
        }), HtmlLogUniqueId.LoggingHtml());

        var result = false;
        try
        {
            await func();

            Logger.LogHtmlInformation("Table", HtmlHeaderLevelEnum.Header3, new HtmlTable([
                .. Items.Select(t => new
                {
                    t.Frequency,
                    t.DefaultAmplitude,
                    t.Amplitude,
                    t.Coefficient,
                    t.MeasurePower,
                    t.Rate,
                    t.IsOk,
                    AODWaveform = new HtmlTable([.. t.Profiles.Select(tt => tt.ToHtmlAnonymous())]),
                })
            ]), HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation("3. Plot", HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
            {
                MeasurePowerPoints = new HtmlPlot2DLinesChart([(string.Empty, MeasurePowerPoints)], string.Empty),
                CoefficientPoints = new HtmlPlot2DLinesChart([(string.Empty, CoefficientPoints)], string.Empty)
            }), HtmlLogUniqueId.LoggingHtml());

            result = true;
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                DialogWindowProvider.ShowDialog($"{AODWaveformName} {stepName} Canceled!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlWarning($"{AODWaveformName}{stepName}  Canceled!", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                return;
            }

            DialogWindowProvider.ShowDialog($"""
                                             {AODWaveformName} {stepName} Failed!
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            Logger.LogHtmlError(ex, "Error", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
        }
        finally
        {
            Logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{AODWaveformName}_{stepName}_{(result ? "OK" : "Failed")}"));
        }
    }

    private async Task<bool> UniformityAsync(AODWaveformUniformityItem<TProfile> item, CancellationToken cancellationToken)
    {
        Logger.LogHtmlInformation($"{item.Frequency}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

        item.IsOk = false;
        MeasureCoefficientPowerPoints = [];

        if (Verify().IsOk)
        {
            item.IsOk = true;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, GetHtmlQuote(), HtmlLogUniqueId.LoggingHtml());

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

            await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

            MeasureCoefficientPowerPoints = [.. MeasureCoefficientPowerPoints, new Point(midAmplitude, item.MeasurePower)];
            MeasureCoefficientPowerPoints = [.. MeasureCoefficientPowerPoints.OrderBy(t => t.X)];

            var (isOk, isLessThan) = Verify();
            if (isOk)
            {
                item.IsOk = true;

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, GetHtmlQuote(), HtmlLogUniqueId.LoggingHtml());

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

        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, GetHtmlQuote(), HtmlLogUniqueId.LoggingHtml());

        return false;

        (bool IsOk, bool IsLessThan) Verify()
        {
            var rate = item.MeasurePower / Cache.TargetMeasurePower;
            item.Rate = rate;

            return (Cache.TargetThresholdRateMin < rate && rate < Cache.TargetThresholdRateMax, item.MeasurePower < Cache.TargetMeasurePower);
        }

        HtmlQuote GetHtmlQuote() => new(new
        {
            item.Frequency,
            item.DefaultAmplitude,
            item.Amplitude,
            item.Coefficient,
            item.MeasurePower,
            item.Rate,
            item.IsOk,
            MeasureCoefficientPowerPoints = new HtmlPlot2DLinesChart([(string.Empty, [..MeasureCoefficientPowerPoints])], string.Empty)
        });
    }

    private async Task UpdateMeasurePowerAsync(AODWaveformUniformityItem<TProfile> item, CancellationToken cancellationToken)
    {
        try
        {
            Cache.Param.WithFrequencyFlatness(item.Frequency);
            Cache.Param.Amplitude = item.Amplitude;

            Logger.LogHtmlInformation($"{item.Amplitude}", HtmlHeaderLevelEnum.Header5, new HtmlQuote(Cache.Param.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            var (isSuccess, result, resultFilePath, exception) = GenerateAODWaveform(Cache.Param, cancellationToken);
            if (isSuccess == false) throw GuardUtils.IsNotNullAndReturn(exception);

            item.Profiles = result;
            item.AODWaveformResultFilePath = resultFilePath;

            SetAODWaveProfiles(Cache.Param, item.Profiles);

            LaserViewModel.ToggleOpticsMagType(Cache.Param.OpticsMagTypeEnum);
            LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Through);
            await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);

            var measurePower = LaserViewModel.GetOpticalPowerMeter();

            item.MeasurePower = measurePower;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, new HtmlQuote(new
            {
                item.AODWaveformResultFilePath,
                item.Frequency,
                item.Amplitude,
                item.MeasurePower,
                AODWaveform = new HtmlTable([.. item.Profiles.Select(t => t.ToHtmlAnonymous())])
            }), HtmlLogUniqueId.LoggingHtml());
        }
        finally
        {
            LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Scan);
        }
    }
}