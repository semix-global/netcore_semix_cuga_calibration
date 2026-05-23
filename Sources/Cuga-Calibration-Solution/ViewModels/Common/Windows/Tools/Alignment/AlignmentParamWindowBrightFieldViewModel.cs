using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;

[IOCAppService(ServiceType = typeof(AlignmentParamWindowBrightFieldViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AlignmentParamWindowBrightFieldViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial AlignmentCacheBrightField Cache { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<MicroscopeLensInformation> MicroscopeLensInformationList { get; set; } = [];

    [ObservableProperty]
    public partial bool IsToolsEnable { get; set; } = true;

    [RelayCommand]
    private void Close()
    {
        CloseView(true);
    }
}