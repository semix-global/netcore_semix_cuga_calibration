using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;

[IOCAppService(ServiceType = typeof(AlignmentParamWindowDarkFieldViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AlignmentParamWindowDarkFieldViewModel(ApplicationCookie applicationCookie) : ViewModelBase
{
    [ObservableProperty]
    public partial AlignmentCacheDarkField Cache { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<MicroscopeLensInformation> MicroscopeLensInformationList { get; set; } = [];

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    public ApplicationCookie ApplicationCookie => applicationCookie;

    [ObservableProperty]
    public partial bool IsToolsEnable { get; set; } = true;

    [RelayCommand]
    private void Close()
    {
        CloseView(true);
    }
}