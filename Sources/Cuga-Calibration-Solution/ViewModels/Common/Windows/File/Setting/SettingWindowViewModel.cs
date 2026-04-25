using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Setting;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting;

[IOCAppService(ServiceType = typeof(SettingWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class SettingWindowViewModel(
    CalibrationSetting calibrationSetting,
    ApplicationCookie applicationCookie,
    ICacheProvider cacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    ILogger<SettingWindowViewModel> logger) : ViewModelBase
{
    [DefaultCache]
    [ObservableProperty]
    private CalibrationSetting _calibrationSetting = calibrationSetting;

    [ObservableProperty]
    private ApplicationCookie _applicationCookie = applicationCookie;

    [ObservableProperty]
    private SettingCalibrateItemsStatusViewModel _settingCalibrateItemsStatusViewModel = HostApplication.GetRequiredService<SettingCalibrateItemsStatusViewModel>();

    [ObservableProperty]
    private SettingRequiredCalibrationViewModel _settingRequiredCalibrationViewModel = HostApplication.GetRequiredService<SettingRequiredCalibrationViewModel>();

    [RelayCommand]
    private void Restore()
    {
        try
        {
            CalibrationSetting.AdaptIn(cacheProvider.GetOrDefault<CalibrationSetting>());
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Restore Failed
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Restore Setting");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            if (await SettingCalibrateItemsStatusViewModel.SavingAsync().ConfigureAwait(false) == false)
            {
                dialogWindowProvider.ShowDialog("Save Calibrations Enable Status Failed, Please Check Settings and try again!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            if (await SettingRequiredCalibrationViewModel.SavingAsync().ConfigureAwait(false) == false)
            {
                dialogWindowProvider.ShowDialog("Save Calibrations Enable Status Failed, Please Check Settings and try again!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            cacheProvider.Set(CalibrationSetting, cancellationTokenSource.Token);

            CloseView(true);
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Save Failed
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Save Setting");
        }
    }

    [RelayCommand]
    private void Close()
    {
        SettingCalibrateItemsStatusViewModel.Closing();

        CloseView(true);
    }
}