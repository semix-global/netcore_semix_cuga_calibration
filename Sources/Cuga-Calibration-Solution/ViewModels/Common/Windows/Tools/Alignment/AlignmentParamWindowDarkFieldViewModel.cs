using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;

[IOCAppService(ServiceType = typeof(AlignmentParamWindowDarkFieldViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AlignmentParamWindowDarkFieldViewModel : ViewModelBase
{
    [ObservableProperty]
    private AlignmentCacheDarkField _cache = new();

    [ObservableProperty]
    private ObservableCollection<MicroscopeLensInformation> _microscopeLensInformationList = [];

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();


    [RelayCommand]
    private void Close()
    {
        CloseView(true);
    }
}