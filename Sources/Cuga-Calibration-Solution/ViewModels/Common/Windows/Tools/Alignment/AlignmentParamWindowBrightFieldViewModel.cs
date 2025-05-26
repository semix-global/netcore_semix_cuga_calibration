using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.Alignment;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;

[IOCAppService(ServiceType = typeof(AlignmentParamWindowBrightFieldViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AlignmentParamWindowBrightFieldViewModel : ViewModelBase
{
    [ObservableProperty]
    private AlignmentCacheBrightField _cache = new();

    [RelayCommand]
    private void Close()
    {
        CloseView(true);
    }
}