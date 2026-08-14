using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Laser.OpticalPowerMeter;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Services.Interfaces;
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
    where TCache : AODWaveformCommonCache<TResult>, new()
    where TItem : AODWaveformCommonItem, new()
    where TResult : AODWaveformCommonResult, new()
{
    protected readonly ILogger<AbstractAODWaveformCommonWindowViewModel<TCache, TItem, TResult>> Logger;
    protected readonly ApplicationSetting ApplicationSetting;
    protected readonly ICacheProvider CacheProvider;
    protected readonly IApplicationCookieService ApplicationCookieService;
    protected readonly IWindowManagerService WindowManagerService;
    protected readonly IDialogWindowProvider DialogWindowProvider;
    protected readonly LaserViewModel LaserViewModel;
    protected readonly OpticsViewModel OpticsViewModel;
    protected readonly StageViewModel StageViewModel;
    protected readonly ConfigViewModel ConfigViewModel;

    public string AODWaveformDirectoryPath => Path.Combine(ApplicationSetting.AppHomeDirectory, "AODWaveform", GetType().Name, DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public string ResultAODWaveformCsvResultFilePath => Path.Combine(ApplicationSetting.AppHomeDirectory, "Result", "CSV", $"{GetType().Name}.CSV");

    public string ResultAODWaveformDirectoryPath => Path.Combine(ApplicationSetting.AppHomeDirectory, "Result", "AODWaveform", GetType().Name, DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public ApplicationCookie ApplicationCookie { get; }

    public abstract TCache Cache { get; set; }

    public abstract string Name { get; }

    public abstract string[] Steps { get; }

    public Guid HtmlLogUniqueId { get; private set; }

    protected abstract void GenerateAndSetFlatnessAODWaveform(TItem item, Guid htmlLogUniqueId, CancellationToken cancellationToken);

    protected abstract void GenerateResultAODWaveform(TResult result, Guid htmlLogUniqueId, CancellationToken cancellationToken);

    protected abstract void SetResultAODWaveformConfiguration(TResult result, Guid htmlLogUniqueId, CancellationToken cancellationToken);

    protected AbstractAODWaveformCommonWindowViewModel()
    {
        Logger = (ILogger<AbstractAODWaveformCommonWindowViewModel<TCache, TItem, TResult>>)HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        ApplicationSetting = HostApplication.GetRequiredService<IOptions<ApplicationSetting>>().Value;
        CacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
        ApplicationCookieService = HostApplication.GetRequiredService<IApplicationCookieService>();
        WindowManagerService = HostApplication.GetRequiredService<IWindowManagerService>();
        DialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();
        LaserViewModel = HostApplication.GetRequiredService<LaserViewModel>();
        OpticsViewModel = HostApplication.GetRequiredService<OpticsViewModel>();
        StageViewModel = HostApplication.GetRequiredService<StageViewModel>();
        ConfigViewModel = HostApplication.GetRequiredService<ConfigViewModel>();

        ApplicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
    }

    [RelayCommand]
    protected async Task LoadedAsync() => await Task.Run(() => Cache = CacheProvider.GetOrDefault<TCache>());

    [RelayCommand]
    private void RefreshMeasureMachinePosition()
    {
        var laserOpticalPowerDtos = ApplicationCookieService.GetCalibrations<LaserOpticalPowerMeterDTO>();
        if (laserOpticalPowerDtos.Length > 0)
        {
            var laserOpticalPowerDto = laserOpticalPowerDtos.SingleOrDefault(t => t.ProductivityInformation.OpticsIlluminationModeEnum == Cache.ProductivityInformation.OpticsIlluminationModeEnum
                                                                                  && t.ProductivityInformation.OpticsMagType == Cache.ProductivityInformation.OpticsMagType
                                                                                  && t.IsOk);
            if (laserOpticalPowerDto is not null)
            {
                Cache.MeasureMaxPowerMachinePosition = laserOpticalPowerDto.MaxMeasurePowerPosition;

                return;
            }
        }

        DialogWindowProvider.ShowDialog($"Please Calibrate {Cache.ProductivityInformation} Laser Optical Power First", DialogButtonsEnum.OK, DialogIconEnum.Warning);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task TestSetResultAODWaveformConfigurationAsync(TResult result, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            try
            {
                SetResultAODWaveformConfiguration(result, Guid.Empty, cancellationToken);

                DialogWindowProvider.ShowDialog($"{Name}: Set AOD Waveform Configuration Success");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    DialogWindowProvider.ShowDialog($"{Name}: Set AOD Waveform Configuration Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                DialogWindowProvider.ShowDialog($"""
                                                 {Name}: Set AOD Waveform Configuration Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogError(ex, "Set AOD Waveform Configuration");
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> StepSecondLastAsync(bool isNotSilent, CancellationToken cancellationToken)
    {
        return await InvokeAsync(Steps.Length - 2, () =>
        {
            Guard.IsNotEmpty(Cache.Results);

            foreach (var result in Cache.Results)
            {
                cancellationToken.ThrowIfCancellationRequested();

                GenerateResultAODWaveform(result, HtmlLogUniqueId, cancellationToken);
            }

            return Task.FromResult(true);
        }, isNotSilent).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> StepFirstLastAsync(bool isNotSilent, CancellationToken cancellationToken)
    {
        return await InvokeAsync(Steps.Length - 1, () =>
        {
            Guard.IsNotEmpty(Cache.Results);

            foreach (var result in Cache.Results)
            {
                cancellationToken.ThrowIfCancellationRequested();

                SetResultAODWaveformConfiguration(result, HtmlLogUniqueId, cancellationToken);
            }

            return Task.FromResult(true);
        }, isNotSilent).ConfigureAwait(false);
    }

    [RelayCommand]
    private void Close()
    {
        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            Cache.Id = 0;
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
        bool isNotSilent)
    {
        return await Task.Run(async () =>
        {
            var isInitHtmlLog = isNotSilent || stepIndex == 0;
            var isEndHtml = isNotSilent || stepIndex == Steps.Length - 1;

            HtmlLogUniqueId = isInitHtmlLog ? Guid.NewGuid() : HtmlLogUniqueId;

            if (isInitHtmlLog) Logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation(Steps[stepIndex], HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Cache.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                isSuccess = await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    if (isNotSilent == false) isEndHtml = true;

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
                if (isEndHtml)
                    Logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{(isNotSilent ? Steps[stepIndex].Replace(" ", string.Empty) : "All")}_{(isSuccess ? "OK" : "Failed")}"));
            }

            if (isSuccess)
            {
                if (isEndHtml) DialogWindowProvider.ShowDialog($"{Name}: {(isNotSilent ? Steps[stepIndex] : "All")} Success");
            }
            else
                DialogWindowProvider.ShowDialog($"{Name}: {Steps[stepIndex]} Error", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return isSuccess;
        }).ConfigureAwait(false);
    }

    protected async Task UpdateMeasurePowerAsync(TItem item, Guid htmlLogUniqueId, CancellationToken cancellationToken)
    {
        try
        {
            Guard.IsGreaterThan(Cache.TotalMeasurePower, 0);
            Guard.IsGreaterThan(Cache.MeasurePowerTimes, 0);

            GenerateAndSetFlatnessAODWaveform(item, htmlLogUniqueId, cancellationToken);

            LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

            await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);

            var times = 0;
            double measurePower;
            while (true)
            {
                measurePower = LaserViewModel.GetOpticalMeasurePower();
                if (0 < measurePower && measurePower <= Cache.TotalMeasurePower) break;

                Logger.LogWarning("Get Optical Measure Power Failed, Retrying...");
                if (++times > Cache.MeasurePowerTimes - 1) ThrowHelper.ThrowNotSupportedException($"Get Optical Measure Power Failed, Over Max Retry Count({Cache.MeasurePowerTimes})");
            }

            item.MeasurePower = measurePower;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, new HtmlQuote(item.ToHtmlAnonymous()), htmlLogUniqueId.LoggingHtml());
        }
        finally
        {
            LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
        }
    }
}