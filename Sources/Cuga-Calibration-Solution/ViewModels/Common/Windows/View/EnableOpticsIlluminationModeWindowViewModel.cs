using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.View;

[IOCAppService(ServiceType = typeof(EnableOpticsIlluminationModeWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class EnableOpticsIlluminationModeWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<OpticsIlluminationModeItem> _opticsIlluminationModeEnableList =
    [
        new() { OpticsIlluminationModeEnum = OpticsIlluminationModeEnum.OI, IsEnable = false },
        new() { OpticsIlluminationModeEnum = OpticsIlluminationModeEnum.NI, IsEnable = false },
    ];

    [RelayCommand]
    private void Close()
    {
        CloseView(null);
    }
}

public sealed partial class OpticsIlluminationModeItem : ObservableObject
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum;

    [ObservableProperty]
    private bool _isEnable;
}