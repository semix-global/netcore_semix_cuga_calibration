using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.View;

[IOCAppService(ServiceType = typeof(EnableProductiveInformationWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class EnableProductiveInformationWindowViewModel(ApplicationCookie applicationCookie) : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<EnableOpticsMagItem> _productiveInformationEnableList = [];

    [RelayCommand]
    private void Loaded()
    {
        ProductiveInformationEnableList = [.. applicationCookie.ProductivityInformations.Select(t => new EnableOpticsMagItem() { ProductivityInformation = t, IsEnable = false })];
    }

    [RelayCommand]
    private void Close()
    {
        CloseView(null);
    }
}

public sealed partial class EnableOpticsMagItem : ObservableObject
{
    // todo:delete
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private bool _isEnable;
}