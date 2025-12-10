using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Laser.OpticalPowerMeter;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Extensions;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public abstract partial class AbstractAODWaveformCommonWindowViewModel<TCache, TItem, TResult> : ViewModelBase
    where TCache : AODWaveformCommonCache, new()
    where TItem : AODWaveformCommonItem, new()
    where TResult : AODWaveformCommonResult, new()
{
    protected readonly ILogger<AbstractAODWaveformCommonWindowViewModel<TCache, TItem, TResult>> Logger;
    protected readonly ApplicationSetting ApplicationSetting;
    protected readonly ICacheProvider CacheProvider;
    protected readonly IWindowManagerService WindowManagerService;
    protected readonly IDialogWindowProvider DialogWindowProvider;
    protected readonly LaserViewModel LaserViewModel;
    protected readonly StageViewModel StageViewModel;
    protected readonly ConfigViewModel ConfigViewModel;

    public string AODWaveformDirectoryPath => Path.Combine(ApplicationSetting.AppHomeDirectory, "AODWaveform", GetType().Name, DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public string ResultAODWaveformCsvResultFilePath => Path.Combine(ApplicationSetting.AppHomeDirectory, "Result", "CSV", $"{GetType().Name}.CSV");

    public string ResultAODWaveformDirectoryPath => Path.Combine(ApplicationSetting.AppHomeDirectory, "Result", "AODWaveform", GetType().Name, DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public ApplicationCookie ApplicationCookie { get; }

    public Guid HtmlLogUniqueId { get; private set; }

    [ObservableProperty]
    private TCache _cache = new();

    public abstract string Name { get; }

    public abstract IReadOnlyList<string> Steps { get; }

    protected abstract void GenerateFlatnessFixedAODWaveform(TItem item, CancellationToken cancellationToken);

    protected abstract void GenerateFlatnessChangedAODWaveform(TItem item, CancellationToken cancellationToken);

    protected abstract void GenerateScanFixedAODWaveform(TItem item, CancellationToken cancellationToken);

    protected abstract void GenerateScanChangedAODWaveform(TItem item, CancellationToken cancellationToken);

    protected abstract void GenerateResultAODWaveform(TResult result, CancellationToken cancellationToken);

    protected abstract void SetResultAODWaveformConfig(TResult result, CancellationToken cancellationToken);

    protected abstract void SetAODWaveformProfiles(TItem item);

    protected abstract void LoggerResult(int stepIndex);

    protected AbstractAODWaveformCommonWindowViewModel()
    {
        Logger = (ILogger<AbstractAODWaveformCommonWindowViewModel<TCache, TItem, TResult>>)HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        ApplicationSetting = HostApplication.GetRequiredService<IOptions<ApplicationSetting>>().Value;
        CacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
        WindowManagerService = HostApplication.GetRequiredService<IWindowManagerService>();
        DialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();
        LaserViewModel = HostApplication.GetRequiredService<LaserViewModel>();
        StageViewModel = HostApplication.GetRequiredService<StageViewModel>();
        ConfigViewModel = HostApplication.GetRequiredService<ConfigViewModel>();

        ApplicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
    }

    [RelayCommand]
    private void RefreshMeasureMachinePosition()
    {
        if (CacheProvider.TryGetOrDefaultArray<LaserOpticalPowerMeterDto>(out var laserOpticalPowerDtos))
        {
            // todo: 改了之后记得这儿也得改
            var laserOpticalPowerDto = laserOpticalPowerDtos.SingleOrDefault(t => t.ProductivityInformation == Cache.ProductivityInformation);
            if (laserOpticalPowerDto is not null && laserOpticalPowerDto.IsOk)
            {
                Cache.MeasureMaxPowerMachinePosition = laserOpticalPowerDto.MeasureMaxPowerPosition;

                return;
            }
        }

        DialogWindowProvider.ShowDialog($"Please Calibrate {Cache.ProductivityInformation} Laser Optical Power First", DialogButtonsEnum.OK, DialogIconEnum.Warning);
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

    protected async Task<bool> InvokeAsync(
        int stepIndex,
        Func<Task<bool>> func,
        bool isSilent)
    {
        return await Task.Run(async () =>
        {
            var isInitHtmlLog = isSilent == false || stepIndex == 0;
            HtmlLogUniqueId = isInitHtmlLog ? Guid.NewGuid() : HtmlLogUniqueId;

            if (isInitHtmlLog) Logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation(Steps[stepIndex], HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Cache.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                isSuccess = await func().ConfigureAwait(false);
                LoggerResult(stepIndex);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    DialogWindowProvider.ShowDialog($"{Name}: {Steps[stepIndex]} Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    Logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    return false;
                }

                DialogWindowProvider.ShowDialog($"""
                                                 {Name}: {Steps[stepIndex]} Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlError(ex, Name, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            }
            finally
            {
                Logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{Name}_{Steps[stepIndex]}_{(isSuccess ? "OK" : "Failed")}"));
            }

            if (isSuccess)
            {
                if (isSilent) DialogWindowProvider.ShowDialog($"{Name}: {Steps[stepIndex]} Success");
            }
            else
                DialogWindowProvider.ShowDialog($"{Name}: {Steps[stepIndex]} Error", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return isSuccess;
        }).ConfigureAwait(false);
    }

    protected async Task UpdateMeasurePowerAsync(TItem item, CancellationToken cancellationToken)
    {
        try
        {
            GenerateFlatnessChangedAODWaveform(item, cancellationToken);
            SetAODWaveformProfiles(item);

            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.MeasureMaxPowerMachinePosition);
            LaserViewModel.ToggleOpticsMagType(Cache.OpticsIlluminationModeEnum, Cache.ProductivityInformation);
            LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

            await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);

            var measurePower = LaserViewModel.GetOpticalMeasurePower();

            item.MeasurePower = measurePower;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
        }
        finally
        {
            LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
        }
    }
}