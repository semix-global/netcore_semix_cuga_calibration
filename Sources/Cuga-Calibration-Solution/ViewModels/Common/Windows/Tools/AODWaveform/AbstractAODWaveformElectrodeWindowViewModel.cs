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
using CommunityToolkit.Diagnostics;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformElectrodeCache<TParam, TProfile, TResult> : ObservableCacheBase
    where TParam : AbstractGenerateAODWaveformParam, new()
    where TProfile : AbstractAODWaveformProfile
    where TResult : AODWaveformElectrodeItem<TProfile>, new()
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
    [NotifyPropertyChangedFor(nameof(LowPoints))]
    private TResult[] _lowItems = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HighPoints))]
    private TResult[] _highItems = [];

    public Point[] LowPoints => [.. LowItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))];

    public Point[] HighPoints => [.. HighItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))];

    public virtual object ToHtmlAnonymous() => new
    {
        WaitTime,
        LowFrequency,
        HighFrequency,
        OffsetFrequency,
        StartOffsetFrequencyPeriodCoefficient,
        StepOffsetFrequencyPeriodCoefficient,
        StopOffsetFrequencyPeriodCoefficient,
    };
}

public partial class AODWaveformElectrodeItem<TProfile> : ObservableCacheBase
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
}

public abstract partial class AbstractAODWaveformElectrodeWindowViewModel<TParam, TProfile, TResult, TCache> : AbstractAODWaveformCommonViewModel<TParam, TProfile>
    where TParam : AbstractGenerateAODWaveformParam, new()
    where TProfile : AbstractAODWaveformProfile
    where TResult : AODWaveformElectrodeItem<TProfile>, new()
    where TCache : AODWaveformElectrodeCache<TParam, TProfile, TResult>, new()
{
    protected readonly ApplicationSetting ApplicationSetting;
    protected readonly ILogger<AbstractAODWaveformElectrodeWindowViewModel<TParam, TProfile, TResult, TCache>> Logger;
    protected readonly ICacheProvider CacheProvider;
    protected readonly IDialogWindowProvider DialogWindowProvider;
    protected readonly LaserViewModel LaserViewModel;
    protected readonly StageViewModel StageViewModel;

    public string AODWaveformDirectoryPath => Path.Combine(ApplicationSetting.AppHomeDirectory, GetType().Name, DateTime.Now.ToString(Constants.MiddleFileDateTimeFormat));

    [ObservableProperty]
    private TCache _cache = new();

    protected Guid HtmlLogUniqueId { get; private set; }

    protected AbstractAODWaveformElectrodeWindowViewModel()
    {
        ApplicationSetting = HostApplication.GetRequiredService<IOptions<ApplicationSetting>>().Value;
        Logger = (ILogger<AbstractAODWaveformElectrodeWindowViewModel<TParam, TProfile, TResult, TCache>>)HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
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

        Logger.LogWarning("{@Name}: Please Calibrate {@OpticsMagTypeEnum} Optical Power First", nameof(AbstractAODWaveformElectrodeWindowViewModel<TParam, TProfile, TResult, TCache>), Cache.Param.OpticsMagTypeEnum);
        DialogWindowProvider.ShowDialog($"Please Calibrate {Cache.Param.OpticsMagTypeEnum} Optical Power First!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeAsync("Step1 Measure Power", async () =>
        {
            Cache.LowItems = [];
            Cache.HighItems = [];

            Logger.LogHtmlInformation($"{Cache.LowFrequency}MHz", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
            foreach (var offsetFrequencyPeriodCoefficient in Generate.LinearRange(Cache.StartOffsetFrequencyPeriodCoefficient, Cache.StepOffsetFrequencyPeriodCoefficient, Cache.StopOffsetFrequencyPeriodCoefficient))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = new TResult
                {
                    Frequency = Cache.LowFrequency,
                    OffsetFrequencyPeriodCoefficient = offsetFrequencyPeriodCoefficient
                };

                Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

                Cache.LowItems = [.. Cache.LowItems, item];
            }

            Logger.LogHtmlInformation($"{Cache.HighFrequency}MHz", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
            foreach (var offsetFrequencyPeriodCoefficient in Generate.LinearRange(Cache.StartOffsetFrequencyPeriodCoefficient, Cache.StepOffsetFrequencyPeriodCoefficient, Cache.StopOffsetFrequencyPeriodCoefficient))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = new TResult
                {
                    Frequency = Cache.HighFrequency,
                    OffsetFrequencyPeriodCoefficient = offsetFrequencyPeriodCoefficient
                };

                Logger.LogHtmlInformation($"{item.OffsetFrequencyPeriodCoefficient}(2pi)", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                await UpdateMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false);

                Cache.LowItems = [.. Cache.LowItems, item];
            }


            return true;
        });
    }

    [RelayCommand]
    private void Step2(CancellationToken cancellationToken)
    {
        Guard.IsNotEmpty(Cache.LowItems);
        Guard.IsNotEmpty(Cache.HighItems);
        Guard.IsTrue(Cache.LowItems.Length == Cache.HighItems.Length);

        var orderByItems = Cache.LowItems
            .Zip(Cache.HighItems, (low, high) => (Low: low, High: high))
            .OrderByDescending(t => t.Low.MeasurePower + t.High.MeasurePower).ToArray();

        DialogWindowProvider.ShowDialog($"{orderByItems[0].Low.OffsetFrequencyPeriodCoefficient:f3}");
    }

    [RelayCommand]
    private void Close()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        if (CacheProvider.Set(Cache, cancellationTokenSource.Token) == false)
            Logger.LogWarning("{@Name}: Save {@AODWaveformName} AOD Waveform Uniformity Param Failed", nameof(AbstractAODWaveformElectrodeWindowViewModel<TParam, TProfile, TResult, TCache>), AODWaveformName);

        CloseView(null);
    }

    private async Task InvokeAsync(string stepName, Func<Task<bool>> func)
    {
        HtmlLogUniqueId = Guid.NewGuid();

        Logger.LogHtmlInformation(AODWaveformName, HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
        Logger.LogHtmlInformation(stepName, HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Cache.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

        var result = false;
        try
        {
            await func();

            Logger.LogHtmlInformation("Plot", HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
            {
                LowItems = new HtmlPlot2DLinesChart([(string.Empty, [..Cache.LowItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))])], string.Empty),
                HighItems = new HtmlPlot2DLinesChart([(string.Empty, [..Cache.HighItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))])], string.Empty)
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

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, new HtmlQuote(new
            {
                item.AODWaveformResultFilePath,
                item.Frequency,
                item.OffsetFrequencyPeriodCoefficient,
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