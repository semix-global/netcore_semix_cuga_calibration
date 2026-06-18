using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Setting;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(SelectDisableConfigViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SelectDisableConfigViewModel(
    ICacheProvider cacheProvider,
    ILogger<SelectDisableConfigViewModel> logger) : ViewModelBase
{
    [ObservableProperty]
    public partial ObservableCollection<SettingDisableCalibrationConfig> ConfigList { get; set; } = [];

    [ObservableProperty]
    public partial SettingDisableCalibrationConfig? SelectedConfig { get; set; }

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                var configs = cacheProvider.GetOrDefaultArray<SettingDisableCalibrationConfig>();

                ConfigList = new ObservableCollection<SettingDisableCalibrationConfig>(configs);

                if (ConfigList.Count > 0)
                {
                    SelectedConfig = ConfigList[0];
                }
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