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

public partial class AODWaveformElectrodeOffsetCache<TParam, TProfile, TResult> : ObservableCacheBase
    where TParam : AbstractGenerateAODWaveformParam, new()
    where TProfile : AbstractAODWaveformProfile
    where TResult : AODWaveformElectrodeOffsetItem<TProfile>, new()
{
    #region Param

    [ObservableProperty]
    private TParam _param = new();

    [ObservableProperty]
    private Point _measureMaxPowerMachinePosition = Point.Origin;

    [ObservableProperty]
    private double _defaultAmplitude = 1;

    [ObservableProperty]
    private double _waitTime = 15;

    [ObservableProperty]
    private double _lowFrequency;

    [ObservableProperty]
    private double _highFrequency;

    [ObservableProperty]
    private double _offsetFrequency;

    [ObservableProperty]
    private double _startLowFrequencyOffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _stepLowFrequencyOffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _stopLowFrequencyOffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _startHighFrequencyOffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _stepHighFrequencyOffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _stopHighFrequencyOffsetFrequencyPeriodCoefficient;

    #endregion Param

    #region Result

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LowFrequencyPoints))]
    private TResult[] _lowFrequencyItems = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HighFrequencyPoints))]
    private TResult[] _highFrequencyItems = [];

    public Point[] LowFrequencyPoints => [.. LowFrequencyItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))];

    public Point[] HighFrequencyPoints => [.. HighFrequencyItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))];

    #endregion Result

    public virtual object ToHtmlAnonymous() => new
    {
        MeasureMaxPowerMachinePosition,
        DefaultAmplitude,
        WaitTime,
        LowFrequency,
        HighFrequency,
        OffsetFrequency,
        StartLowFrequencyOffsetFrequencyPeriodCoefficient,
        StepLowFrequencyOffsetFrequencyPeriodCoefficient,
        StopLowFrequencyOffsetFrequencyPeriodCoefficient,
        StartHighFrequencyOffsetFrequencyPeriodCoefficient,
        StepHighFrequencyOffsetFrequencyPeriodCoefficient,
        StopHighFrequencyOffsetFrequencyPeriodCoefficient,
        Param = Param.ToHtmlAnonymous()
    };
}

public partial class AODWaveformElectrodeOffsetItem<TProfile> : ObservableCacheBase
    where TProfile : AbstractAODWaveformProfile
{
    [ObservableProperty]
    private IReadOnlyList<TProfile> _profiles = [];

    [ObservableProperty]
    private string _aODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private double _frequency;

    [ObservableProperty]
    private double _offsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _measurePower;

    public virtual object ToHtmlAnonymous() => new HtmlQuote(new
    {
        AODWaveformResultFilePath,
        Frequency,
        MeasurePower,
        AODWaveform = new HtmlTable([.. Profiles.Select(t => t.ToHtmlAnonymous())])
    });
}

public abstract partial class AbstractAODWaveformElectrodeOffsetWindowViewModel<TParam, TProfile, TResult, TCache> : AbstractAODWaveformCommonViewModel<TParam, TProfile>
    where TParam : AbstractGenerateAODWaveformParam, new()
    where TProfile : AbstractAODWaveformProfile
    where TResult : AODWaveformElectrodeOffsetItem<TProfile>, new()
    where TCache : AODWaveformElectrodeOffsetCache<TParam, TProfile, TResult>, new()
{
    protected readonly ApplicationSetting ApplicationSetting;
    protected readonly ILogger<AbstractAODWaveformElectrodeOffsetWindowViewModel<TParam, TProfile, TResult, TCache>> Logger;
    protected readonly ICacheProvider CacheProvider;
    protected readonly IDialogWindowProvider DialogWindowProvider;
    protected readonly LaserViewModel LaserViewModel;
    protected readonly StageViewModel StageViewModel;

    public string AODWaveformDirectoryPath => Path.Combine(ApplicationSetting.AppHomeDirectory, GetType().Name, DateTime.Now.ToString(Constants.MiddleFileDateTimeFormat));

    [ObservableProperty]
    private TCache _cache = new();

    protected Guid HtmlLogUniqueId { get; private set; }

    protected AbstractAODWaveformElectrodeOffsetWindowViewModel()
    {
        ApplicationSetting = HostApplication.GetRequiredService<IOptions<ApplicationSetting>>().Value;
        Logger = (ILogger<AbstractAODWaveformElectrodeOffsetWindowViewModel<TParam, TProfile, TResult, TCache>>)HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
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

        Logger.LogWarning("{@Name}: Please Calibrate {@OpticsMagTypeEnum} Optical Power First", nameof(AbstractAODWaveformElectrodeOffsetWindowViewModel<TParam, TProfile, TResult, TCache>), Cache.Param.OpticsMagTypeEnum);
        DialogWindowProvider.ShowDialog($"Please Calibrate {Cache.Param.OpticsMagTypeEnum} Optical Power First!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeAsync("Step1 Measure Power", async () =>
        {
            Cache.LowFrequencyItems = [];

            Logger.LogHtmlInformation($"{Cache.LowFrequency}MHz", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
            foreach (var offsetFrequencyPeriodCoefficient in Generate.LinearRange(Cache.StartLowFrequencyOffsetFrequencyPeriodCoefficient, Cache.StepLowFrequencyOffsetFrequencyPeriodCoefficient, Cache.StopLowFrequencyOffsetFrequencyPeriodCoefficient))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = new TResult
                {
                    Frequency = Cache.LowFrequency,
                    OffsetFrequencyPeriodCoefficient = offsetFrequencyPeriodCoefficient
                };

                Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

                Cache.LowFrequencyItems = [.. Cache.LowFrequencyItems, item];
            }

            Cache.HighFrequencyItems = [];
            Logger.LogHtmlInformation($"{Cache.HighFrequency}MHz", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
            foreach (var offsetFrequencyPeriodCoefficient in Generate.LinearRange(Cache.StartHighFrequencyOffsetFrequencyPeriodCoefficient, Cache.StepHighFrequencyOffsetFrequencyPeriodCoefficient, Cache.StopHighFrequencyOffsetFrequencyPeriodCoefficient))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = new TResult
                {
                    Frequency = Cache.HighFrequency,
                    OffsetFrequencyPeriodCoefficient = offsetFrequencyPeriodCoefficient
                };

                Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

                Cache.HighFrequencyItems = [.. Cache.HighFrequencyItems, item];
            }

            return true;
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private void Step2(CancellationToken cancellationToken)
    {
        /*Guard.IsNotEmpty(Cache.LowFrequencyItems);
        Guard.IsNotEmpty(Cache.HighFrequencyItems);
        Guard.IsTrue(Cache.LowFrequencyItems.Length == Cache.HighFrequencyItems.Length);

        var orderByItems = Cache.LowFrequencyItems
            .Zip(Cache.HighFrequencyItems, (t1, t2) => (LowFrequencyItem: t1, HighFrequencyItem: t2))
            .OrderByDescending(t => t.LowFrequencyItem.MeasurePower + t.HighFrequencyItem.MeasurePower).ToArray();

        DialogWindowProvider.ShowDialog($"{orderByItems[0].Low.OffsetFrequencyPeriodCoefficient:f3}");*/
    }

    [RelayCommand]
    private void Close()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        if (CacheProvider.Set(Cache, cancellationTokenSource.Token) == false)
            Logger.LogWarning("{@Name}: Save {@AODWaveformName} AOD Waveform Electrode Offset Param Failed", nameof(AbstractAODWaveformElectrodeOffsetWindowViewModel<TParam, TProfile, TResult, TCache>), AODWaveformName);

        CloseView(null);
    }

    private async Task InvokeAsync(string stepName, Func<Task<bool>> func)
    {
        await Task.Run(async () =>
        {
            HtmlLogUniqueId = Guid.NewGuid();

            Logger.LogHtmlInformation(AODWaveformName, HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation(stepName, HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Cache.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            var result = false;
            try
            {
                await func().ConfigureAwait(false);

                Logger.LogHtmlInformation("Low Frequency Table", HtmlHeaderLevelEnum.Header3, new HtmlTable([.. Cache.LowFrequencyItems.Select(t => t.ToHtmlAnonymous())]), HtmlLogUniqueId.LoggingHtml());
                Logger.LogHtmlInformation("High Frequency Table", HtmlHeaderLevelEnum.Header3, new HtmlTable([.. Cache.HighFrequencyItems.Select(t => t.ToHtmlAnonymous())]), HtmlLogUniqueId.LoggingHtml());
                Logger.LogHtmlInformation("Plot", HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
                {
                    LowFrequencyPoints = new HtmlPlot2DLinesChart([(string.Empty, Cache.LowFrequencyPoints)], string.Empty),
                    HighFrequencyPoints = new HtmlPlot2DLinesChart([(string.Empty, Cache.HighFrequencyPoints)], string.Empty)
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
        }).ConfigureAwait(false);
    }

    private async Task UpdateMeasurePowerAsync(TResult item, CancellationToken cancellationToken)
    {
        try
        {
            Cache.Param.WithFrequencyFlatness(item.Frequency);
            Cache.Param.Amplitude = Cache.DefaultAmplitude;
            Cache.Param.DirectoryPath = AODWaveformDirectoryPath;
            Cache.Param.ElectrodeConfigurations =
            [
                new GenerateAODWaveformElectrodeConfiguration
                {
                    OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1,
                    OffsetFrequency = Cache.OffsetFrequency,
                    OffsetFrequencyPeriodCoefficient = 0
                },
                new GenerateAODWaveformElectrodeConfiguration
                {
                    OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode2,
                    OffsetFrequency = Cache.OffsetFrequency,
                    OffsetFrequencyPeriodCoefficient = item.OffsetFrequencyPeriodCoefficient
                }
            ];

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

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
        }
        finally
        {
            LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Scan);
        }
    }
}