using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Laser.OpticalPower;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Bases;
using Local.NoSQL.DB.Providers.Interfaces;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniExcelLibs;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using System.IO;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformUniformityCache<TParam> : ObservableCacheBase
    where TParam : AbstractGenerateAODWaveformParam, new()
{
    [ObservableProperty]
    private TParam _param = new();

    [ObservableProperty]
    private Point _measureMaxPowerMachinePosition = Point.Origin;

    [ObservableProperty]
    private double _defaultAmplitude = 1;

    [ObservableProperty]
    private double _waitTime = 15;

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
}

public partial class AODWaveformUniformityItem<TProfile> : ObservableCacheBase
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

public abstract partial class AbstractAODWaveformUniformityWindowViewModel<TCache, TResult, TParam, TProfile> : AbstractAODWaveformCommonViewModel<TParam, TProfile>
    where TCache : AODWaveformUniformityCache<TParam>, new()
    where TResult : AODWaveformUniformityItem<TProfile>, new()
    where TParam : AbstractGenerateAODWaveformParam, new()
    where TProfile : AbstractAODWaveformProfile
{
    protected readonly ApplicationSetting ApplicationSetting;
    protected readonly ILogger<AbstractAODWaveformUniformityWindowViewModel<TCache, TResult, TParam, TProfile>> Logger;
    protected readonly ICacheProvider CacheProvider;
    protected readonly IDialogWindowProvider DialogWindowProvider;
    protected readonly LaserViewModel LaserViewModel;
    protected readonly StageViewModel StageViewModel;

    public string AODWaveformDirectoryPath => Path.Combine(ApplicationSetting.AppHomeDirectory, GetType().Name, DateTime.Now.ToString(Constants.MiddleFileDateTimeFormat));

    [ObservableProperty]
    private TCache _cache = new();

    [ObservableProperty]
    private Point[] _measureCoefficientPowerPoints = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MeasurePowerPoints))]
    [NotifyPropertyChangedFor(nameof(CoefficientPoints))]
    private TResult[] _items = [];

    public Point[] MeasurePowerPoints => [.. Items.Select(t => new Point(t.Frequency, t.MeasurePower))];

    public Point[] CoefficientPoints => [.. Items.Select(t => new Point(t.Frequency, t.Coefficient))];

    [ObservableProperty]
    private TResult? _selectedItem;

    protected Guid HtmlLogUniqueId { get; private set; }

    protected AbstractAODWaveformUniformityWindowViewModel()
    {
        ApplicationSetting = HostApplication.GetRequiredService<IOptions<ApplicationSetting>>().Value;
        Logger = (ILogger<AbstractAODWaveformUniformityWindowViewModel<TCache, TResult, TParam, TProfile>>)HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        CacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
        DialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();
        LaserViewModel = HostApplication.GetRequiredService<LaserViewModel>();
        StageViewModel = HostApplication.GetRequiredService<StageViewModel>();
    }

    [RelayCommand]
    private void Loaded() => Cache = CacheProvider.GetOrDefault<TCache>();

    [RelayCommand]
    private void RefreshMeasureMachinePosition()
    {
        if (CacheProvider.TryGetOrDefaultArray<LaserOpticalPowerDto>(out var laserOpticalPowerDtos))
        {
            var laserOpticalPowerDto = laserOpticalPowerDtos.SingleOrDefault(t => t.OpticsMagTypeEnum == Cache.Param.OpticsMagTypeEnum);
            if (laserOpticalPowerDto is not null && laserOpticalPowerDto.IsOk)
            {
                Cache.MeasureMaxPowerMachinePosition = laserOpticalPowerDto.MeasureMaxPowerPosition;

                return;
            }
        }

        Logger.LogWarning("{@Name}: Please Calibrate {@OpticsMagTypeEnum} Optical Power First", nameof(AbstractAODWaveformUniformityWindowViewModel<TCache, TResult, TParam, TProfile>), Cache.Param.OpticsMagTypeEnum);
        DialogWindowProvider.ShowDialog($"Please Calibrate {Cache.Param.OpticsMagTypeEnum} Optical Power First!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeAsync("Step1 Measure Power", async () =>
        {
            Items = [];

            foreach (var frequency in Generate.LinearRange(Cache.StartFrequency, Cache.StepFrequency, Cache.StopFrequency))
            {
                var item = new TResult
                {
                    Frequency = frequency,
                    DefaultAmplitude = Cache.DefaultAmplitude,
                    Amplitude = Cache.DefaultAmplitude
                };

                Logger.LogHtmlInformation($"{item.Frequency}MHz", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
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
            var results = new List<bool>();
            foreach (var item in Items)
            {
                Logger.LogHtmlInformation($"{item.Frequency}MHz", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                results.Add(await UniformityAsync(item, cancellationToken).ConfigureAwait(false));
            }

            return results.All(t => t);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1OneAsync(CancellationToken cancellationToken)
    {
        await InvokeAsync("Step1 Measure Power One", async () =>
        {
            if (SelectedItem is null) return false;

            Logger.LogHtmlInformation($"{SelectedItem.Frequency}MHz", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            await UpdateMeasurePowerAsync(SelectedItem, cancellationToken).ConfigureAwait(false);

            Cache.TargetMeasurePower = Items.Min(t => t.MeasurePower);

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step2OneAsync(CancellationToken cancellationToken)
    {
        await InvokeAsync("Step2 Uniformity One", async () =>
        {
            if (SelectedItem is null) return false;

            Logger.LogHtmlInformation($"{SelectedItem.Frequency}MHz", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            return await UniformityAsync(SelectedItem, cancellationToken).ConfigureAwait(false);
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
            MiniExcel.SaveAs(filePath, CoefficientPoints.Select(t => new GenerateAODWaveformUniformityConfiguration { Frequency = t.X, Coefficient = t.Y }));

            DialogWindowProvider.ShowDialog($"Save {AODWaveformName} AOD Waveform Uniformity Success!");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Save {@AODWaveformName} AOD Waveform Uniformity", nameof(AbstractAODWaveformUniformityWindowViewModel<TCache, TResult, TParam, TProfile>), AODWaveformName);
            DialogWindowProvider.ShowDialog($"""
                                             Save {AODWaveformName} AOD Waveform Uniformity Failed!
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private void Close()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        if (CacheProvider.Set(Cache, cancellationTokenSource.Token) == false)
            Logger.LogWarning("{@Name}: Save {@AODWaveformName} AOD Waveform Uniformity Param Failed", nameof(AbstractAODWaveformUniformityWindowViewModel<TCache, TResult, TParam, TProfile>), AODWaveformName);

        CloseView(null);
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
            Logger.LogHtmlInformation("Plot", HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
            {
                Param = new HtmlQuote(Cache.Param.ToHtmlAnonymous()),
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

    private async Task<bool> UniformityAsync(TResult item, CancellationToken cancellationToken)
    {
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
            MeasureCoefficientPowerPoints = new HtmlPlot2DLinesChart([(string.Empty, [.. MeasureCoefficientPowerPoints])], string.Empty)
        });
    }

    private async Task UpdateMeasurePowerAsync(TResult item, CancellationToken cancellationToken)
    {
        try
        {
            Cache.Param.WithFrequencyFlatness(item.Frequency);
            Cache.Param.Amplitude = item.Amplitude;
            Cache.Param.DirectoryPath = AODWaveformDirectoryPath;

            Logger.LogHtmlInformation($"{nameof(item.Amplitude)}：{item.Amplitude}", HtmlHeaderLevelEnum.Header5, new HtmlQuote(Cache.Param.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            var (isSuccess, result, resultFilePath, exception) = GenerateAODWaveform(Cache.Param, cancellationToken);
            if (isSuccess == false) throw GuardUtils.IsNotNullAndReturn(exception);

            item.Profiles = result;
            item.AODWaveformResultFilePath = resultFilePath;

            SetAODWaveProfiles(Cache.Param, item.Profiles);

            LaserViewModel.ToggleOpticsMagType(Cache.Param.OpticsMagTypeEnum);
            LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Through);
            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.MeasureMaxPowerMachinePosition);

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