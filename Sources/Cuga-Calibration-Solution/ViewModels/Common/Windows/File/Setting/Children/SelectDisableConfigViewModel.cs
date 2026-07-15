using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Setting;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(SelectDisableConfigViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SelectDisableConfigViewModel(
    ICacheProvider cacheProvider,
    ILogger<SelectDisableConfigViewModel> logger) : ViewModelBase
{
    [ObservableProperty]
    public partial IReadOnlyList<SettingDisableCalibrationConfig> ConfigList { get; set; } = [];

    [ObservableProperty]
    public partial SettingDisableCalibrationConfig? SelectedConfig { get; set; }

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                ConfigList = cacheProvider.GetOrDefaultArray<SettingDisableCalibrationConfig>();
                SelectedConfig = ConfigList.Count > 0 ? ConfigList[0] : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Load disable calibration configurations failed!");
            }
        });
    }

    [RelayCommand]
    private void Confirm()
    {
        if (SelectedConfig != null)
        {
            CloseView(true);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        SelectedConfig = null;
        CloseView(false);
    }
}