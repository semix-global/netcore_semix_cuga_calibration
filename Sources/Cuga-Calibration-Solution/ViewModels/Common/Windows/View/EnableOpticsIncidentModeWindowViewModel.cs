using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.View;

[IOCAppService(ServiceType = typeof(EnableOpticsIncidentModeWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class EnableOpticsIncidentModeWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<EnableOpticsIncidentModeItem> _opticsIncidentModeEnableList =
    [
        new() { OpticsIncidentModeEnum = OpticsIncidentModeEnum.OI, IsEnable = false },
        new() { OpticsIncidentModeEnum = OpticsIncidentModeEnum.NI, IsEnable = false },
    ];

    [RelayCommand]
    private void Close()
    {
        CloseView(null);
    }
}

public sealed partial class EnableOpticsIncidentModeItem : ObservableObject
{
    [ObservableProperty]
    private OpticsIncidentModeEnum _opticsIncidentModeEnum;

    [ObservableProperty]
    private bool _isEnable;
}