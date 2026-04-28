using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Setting;
using CugaCalibration.ViewModels.Common;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels;

[IOCAppService(ServiceType = typeof(LoadingWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LoadingWindowViewModel(
    StatusViewModel statusViewModel,
    StageViewModel stageViewModel,
    ReviewViewModel reviewViewModel,
    MicroscopeViewModel microscopeViewModel,
    AfViewModel afViewModel,
    AdsViewModel adsViewModel,
    LaserViewModel laserViewModel,
    EFEMViewModel efemViewModel,
    FourierViewModel fourierViewModel,
    OpticsViewModel opticsViewModel,
    CIBViewModel cibViewModel,
    MonitorViewModel monitorViewModel,
    ICacheProvider cacheProvider,
    ILogger<LoadingWindowViewModel> logger,
    IDialogWindowProvider dialogWindowProvider,
    ISynchronizationContextProvider contextProvider,
    CalibrationSetting calibrationSetting,
    string applicationName) : ViewModelBase
{
    private const int ConnectCount = 10;

    [ObservableProperty]
    private string _title = applicationName;

    [ObservableProperty]
    private string _message = "Please Wait, Connecting ...";

    [ObservableProperty]
    private double _processValue;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseCommand))]
    private bool _isCanClose;

    [ObservableProperty]
    private bool _isFailed;

    [RelayCommand]
    private async Task LoadedAsync()
    {
        try
        {
            if (await ConnectAsync(afViewModel.Connect, "Connecting Auto Focus Service", 1).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(microscopeViewModel.Connect, "Connecting Microscope Service", 2).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(reviewViewModel.Connect, "Connecting Review Service", 3).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(stageViewModel.Connect, "Connecting Stage Service", 4).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(adsViewModel.Connect, "Connecting Ads Service", 5).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(laserViewModel.Connect, "Connecting Laser Service", 6).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(efemViewModel.Connect, "Connecting EFEM Service", 7).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(fourierViewModel.Connect, "Connecting Fourier Service", 8).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(opticsViewModel.Connect, "Connecting Optics Service", 9).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(cibViewModel.Connect, "Connecting CIB Service", 10).ConfigureAwait(false) == false) return;
            if (await ConnectAsync(monitorViewModel.Connect, "Connecting Monitor Service", 11).ConfigureAwait(false) == false) return;

            Message = "Connected OK!!!";

            Guard.IsTrue(await statusViewModel.RefreshCookieAsync(true));

            calibrationSetting.AdaptIn(cacheProvider.GetOrDefault<CalibrationSetting>());

            contextProvider.Send(() => CloseView(true));

            await Task.Delay(300).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            IsFailed = true;
            Message = $"Connecting Failed: {ex}";
            logger.LogError(ex, "{@Name}: Connecting Failed", nameof(LoadingWindowViewModel));
            dialogWindowProvider.ShowDialog($"Connecting Failed: {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
        finally
        {
            contextProvider.Send(() => IsCanClose = true);
        }

        return;

        async Task<bool> ConnectAsync(Func<bool> func, string title, int index)
        {
            Message = $"{title} ...";
            var stageConnectResult = await Task.Run(func).ConfigureAwait(false);
            if (stageConnectResult == false)
            {
                IsFailed = true;
                Message = $"{title} Failed.";
            }

            ProcessValue = 100d / ConnectCount * index;
            await Task.Delay(300).ConfigureAwait(false);

            return stageConnectResult;
        }
    }

    [RelayCommand(CanExecute = nameof(IsCanClose))]
    private void Close()
    {
        CloseView(false);
    }
}