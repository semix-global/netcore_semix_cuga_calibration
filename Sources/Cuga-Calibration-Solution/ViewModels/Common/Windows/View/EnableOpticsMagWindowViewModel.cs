using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.View;

[IOCAppService(ServiceType = typeof(EnableOpticsMagWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class EnableOpticsMagWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<EnableOpticsMagItem> _opticsMagEnableList =
    [
        new EnableOpticsMagItem { OpticsMagTypeEnum = OpticsMagTypeEnum.Low, IsEnable = false },
        new EnableOpticsMagItem { OpticsMagTypeEnum = OpticsMagTypeEnum.Middle, IsEnable = false },
        new EnableOpticsMagItem { OpticsMagTypeEnum = OpticsMagTypeEnum.High, IsEnable = false }
    ];

    [RelayCommand]
    private void Close()
    {
        CloseView(null);
    }
}

public sealed partial class EnableOpticsMagItem : ObservableObject
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private bool _isEnable;
}