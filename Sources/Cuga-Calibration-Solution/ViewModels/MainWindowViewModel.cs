using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Helper;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Setting;
using Core.Recipe.Models;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common;
using CugaCalibration.ViewModels.Common.Windows.Management.Recipe.Management;
using CugaCalibration.ViewModels.Common.Windows.View;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels;

[IOCAppService(ServiceType = typeof(MainWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IMessenger _messenger;
    private readonly ICacheProvider _cacheProvider;
    private readonly ICacheProvider _recipeCacheProvider;
    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly IWindowManagerService _windowManagerService;
    private readonly ICalibrationCacheProvider _calibrationCacheProviderService;

    [ObservableProperty]
    public partial CalibrationSetting CalibrationSetting { get; set; }

    [ObservableProperty]
    public partial ApplicationCookie ApplicationCookie { get; set; }

    [ObservableProperty]
    public partial RecipeCookie RecipeCookie { get; set; }

    [ObservableProperty]
    public partial StatusViewModel StatusViewModel { get; set; }

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        IMessenger messenger,
        ICacheProvider cacheProvider,
        [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
        ICacheProvider recipeCacheProvider,
        IDialogWindowProvider dialogWindowProvider,
        IWindowManagerService windowManagerService,
        ICalibrationCacheProvider calibrationCacheProviderService,
        CalibrationSetting calibrationSetting,
        ApplicationCookie applicationCookie,
        RecipeCookie recipeCookie,
        StatusViewModel statusViewModel)
    {
        _logger = logger;
        _messenger = messenger;
        _cacheProvider = cacheProvider;
        _recipeCacheProvider = recipeCacheProvider;
        _dialogWindowProvider = dialogWindowProvider;
        _windowManagerService = windowManagerService;
        _calibrationCacheProviderService = calibrationCacheProviderService;
        CalibrationSetting = calibrationSetting;
        ApplicationCookie = applicationCookie;
        RecipeCookie = recipeCookie;
        StatusViewModel = statusViewModel;

        _messenger.RegisterAll(this);
    }

    [RelayCommand]
    private void Loaded()
    {
        var isLoadingOk = false;
        try
        {
            var showDialog = _windowManagerService.ShowDialog(HostApplication.GetRequiredService<LoginWindowViewModel>());
            if (showDialog == false)
            {
                return;
            }

            var recipeManagementViewModel = HostApplication.GetRequiredService<RecipeManagementViewModel>();
            recipeManagementViewModel.IsLoading = true;
            showDialog = _windowManagerService.ShowDialog(recipeManagementViewModel);
            if (showDialog == false)
            {
                return;
            }

            showDialog = _windowManagerService.ShowDialog(HostApplication.GetRequiredService<LoadingWindowViewModel>());
            if (showDialog == false)
            {
                return;
            }


            _windowManagerService.ShowWindow(HostApplication.GetRequiredService<StageWindowViewModel>());
            _windowManagerService.ShowWindow(HostApplication.GetRequiredService<MicroscopeWindowViewModel>());

            isLoadingOk = true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "{@Name}: Loaded Failed", nameof(MainWindowViewModel));
        }
        finally
        {
            StatusViewModel.Monitor(isLoadingOk);
        }
    }

    [RelayCommand]
    private void Closed()
    {
#pragma warning disable IDE0079
#pragma warning disable IDISP007

        StatusViewModel.Dispose();

        _messenger.UnregisterAll(this);
        _recipeCacheProvider.Dispose();
        _cacheProvider.Dispose();

#pragma warning restore IDISP007
#pragma warning restore IDE0079
    }


    [RelayCommand]
    private async Task ShowLogAsync()
    {
        await Task.Run(() =>
        {
            var logWindowViewModel = HostApplication.GetRequiredService<LogWindowViewModel>();

            if (logWindowViewModel.Show() == false) _windowManagerService.ShowWindow(logWindowViewModel);
        });
    }
}