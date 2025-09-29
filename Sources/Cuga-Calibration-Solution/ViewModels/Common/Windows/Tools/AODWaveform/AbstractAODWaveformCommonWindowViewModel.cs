using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Laser.OpticalPower;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Bases;
using Local.NoSQL.DB.Providers.Extensions;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformCommonCache : ObservableCacheBase
{
    [ObservableProperty]
    private GeneratePrescanAODWaveformParam _generatePrescanAODWaveformParam = new();

    [ObservableProperty]
    private GenerateChirpAODWaveformParam _generateChirpAODWaveformParam = new();

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private double _defaultAmplitude = 1;

    [ObservableProperty]
    private Point _measureMaxPowerMachinePosition = Point.Origin;

    [ObservableProperty]
    private double _waitTime = 15;

    public virtual object ToHtmlAnonymous() => new
    {
        OpticsMagTypeEnum,
        DefaultAmplitude,
        MeasureMaxPowerMachinePosition,
        WaitTime
    };
}

public partial class AODWaveformCommonItem : ObservableObject
{
    [ObservableProperty]
    private double _measurePower;

    public virtual object ToHtmlAnonymous() => new
    {
        MeasurePower
    };
}

public abstract partial class AbstractAODWaveformCommonWindowViewModel<TCache, TItem> : ViewModelBase
    where TCache : AODWaveformCommonCache, new()
    where TItem : AODWaveformCommonItem, new()
{
    protected readonly ApplicationSetting ApplicationSetting;
    protected readonly ILogger<AbstractAODWaveformCommonWindowViewModel<TCache, TItem>> Logger;
    protected readonly ICacheProvider CacheProvider;
    protected readonly IDialogWindowProvider DialogWindowProvider;
    protected readonly LaserViewModel LaserViewModel;
    protected readonly StageViewModel StageViewModel;

    protected string AODWaveformDirectoryPath => Path.Combine(ApplicationSetting.AppHomeDirectory, nameof(AODWaveform), GetType().Name, DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    [ObservableProperty]
    private TCache _cache = new();

    protected Guid HtmlLogUniqueId { get; private set; }

    public abstract string Name { get; }

    protected abstract void GenerateFixedAODWaveform(CancellationToken cancellationToken);

    protected abstract void GenerateChangedAODWaveform(TItem item, CancellationToken cancellationToken);

    protected abstract void SetAODWaveformProfiles(TItem item);

    protected abstract void LoggerResult();

    protected AbstractAODWaveformCommonWindowViewModel()
    {
        ApplicationSetting = HostApplication.GetRequiredService<IOptions<ApplicationSetting>>().Value;
        Logger = (ILogger<AbstractAODWaveformCommonWindowViewModel<TCache, TItem>>)HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
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
            var laserOpticalPowerDto = laserOpticalPowerDtos.SingleOrDefault(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum);
            if (laserOpticalPowerDto is not null && laserOpticalPowerDto.IsOk)
            {
                Cache.MeasureMaxPowerMachinePosition = laserOpticalPowerDto.MeasureMaxPowerPosition;

                return;
            }
        }

        DialogWindowProvider.ShowDialog($"Please Calibrate {Cache.OpticsMagTypeEnum} Laser Optical Power First", DialogButtonsEnum.OK, DialogIconEnum.Warning);
    }

    [RelayCommand]
    private void Close()
    {
        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            CacheProvider.Set(Cache, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save cache");
        }

        CloseView(null);
    }

    protected async Task InvokeAsync(string stepName, Func<Task<bool>> func)
    {
        await Task.Run(async () =>
        {
            HtmlLogUniqueId = Guid.NewGuid();

            Logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation(stepName, HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Cache.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            var result = false;
            try
            {
                result = await func().ConfigureAwait(false);

                if (result) DialogWindowProvider.ShowDialog($"{Name}: {stepName} Success");
                else DialogWindowProvider.ShowDialog($"{Name}: {stepName} Error", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                LoggerResult();
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    DialogWindowProvider.ShowDialog($"{Name}: {stepName} Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    Logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    return;
                }

                DialogWindowProvider.ShowDialog($"""
                                                 {Name}: {stepName} Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlError(ex, "Failed", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            }
            finally
            {
                Logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{Name}_{stepName}_{(result ? "OK" : "Failed")}"));
            }
        }).ConfigureAwait(false);
    }

    protected async Task UpdateMeasurePowerAsync(TItem item, CancellationToken cancellationToken)
    {
        try
        {
            GenerateChangedAODWaveform(item, cancellationToken);
            SetAODWaveformProfiles(item);

            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.MeasureMaxPowerMachinePosition);
            LaserViewModel.ToggleOpticsMagType(Cache.OpticsMagTypeEnum);
            LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Through);

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