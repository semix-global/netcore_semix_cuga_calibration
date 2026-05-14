using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Setting;
using CugaCalibration.Core.Services.Interfaces;
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
    IApplicationCookieService applicationCookieService,
    ILogger<LoadingWindowViewModel> logger,
    IDialogWindowProvider dialogWindowProvider,
    ISynchronizationContextProvider contextProvider,
    CalibrationSetting calibrationSetting,
    ApplicationCookie applicationCookie,
    string applicationName) : ViewModelBase
{
    private const int ConnectCount = 11;
    private const double ConnectMaxProgress = 30d;
    private const double ConnectCacheProgress = 50d;

    [ObservableProperty]
    public partial string Title { get; set; } = applicationName;

    [ObservableProperty]
    public partial string Message { get; set; } = "Please Wait, Connecting ...";

    [ObservableProperty]
    public partial double ProcessValue { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseCommand))]
    public partial bool IsCanClose { get; set; }

    [ObservableProperty]
    public partial bool IsFailed { get; set; }

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await Task.Run(async () =>
        {
            try
            {
                Message = "Connecting ...";
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

                Message = "Refreshing Cookie...";
                ProcessValue = ConnectMaxProgress;
                Guard.IsTrue(await statusViewModel.RefreshCookieAsync(true));
                calibrationSetting.AdaptIn(cacheProvider.GetOrDefault<CalibrationSetting>());
                ProcessValue = ConnectCacheProgress;
                Message = "Refreshing Cookie OK!!!";

                Message = "Loading Calibration Cache...";
                await LoadCalibrationCacheAsync();
                Message = "Loading Calibration Cache OK!!!";

                contextProvider.Send(() => CloseView(true));

                await Task.Delay(300).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                IsFailed = true;
                Message = $"Loading Failed: {ex}";
                logger.LogError(ex, "{@Name}: Loading Failed", nameof(LoadingWindowViewModel));
                dialogWindowProvider.ShowDialog($"Loading Failed: {ex}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
            finally
            {
                contextProvider.Send(() => IsCanClose = true);
            }
        });

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

            ProcessValue = ConnectMaxProgress / ConnectCount * index;
            await Task.Delay(300).ConfigureAwait(false);

            return stageConnectResult;
        }

        async Task LoadCalibrationCacheAsync()
        {
            var calibrationMenus = applicationCookie.CalibrationMenu.GetAllChildren();

            var total = calibrationMenus.Count;
            var progressPerItem = ConnectCacheProgress / total;

            for (var i = 0; i < total; i++)
            {
                var menu = calibrationMenus[i];

                Message = $"Loading {menu.SysMenu.Name} Cache...";

                applicationCookieService.GetCache(menu.Entry.CacheType);

                if (menu.Entry.IsArray)
                    applicationCookieService.GetCalibration(menu.Entry.DTOType);
                else
                    applicationCookieService.GetCalibrations(menu.Entry.DTOType);

                Message = $"Loading {menu.SysMenu.Name} Cache OK!!!";

                ProcessValue = ConnectCacheProgress + progressPerItem * (i + 1);
                await Task.Delay(300).ConfigureAwait(false);
            }
        }
    }

    [RelayCommand(CanExecute = nameof(IsCanClose))]
    private void Close()
    {
        CloseView(false);
    }
}