using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.View;

[IOCAppService(ServiceType = typeof(EnableStageSpeedWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class EnableStageSpeedWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<EnableStageSpeedItem> _stageSpeedEnableList =
    [
        new() { StageSpeedEnum = StageSpeedEnum.Low, IsEnable = false },
        new() { StageSpeedEnum = StageSpeedEnum.Middle, IsEnable = false },
        new() { StageSpeedEnum = StageSpeedEnum.High, IsEnable = false }
    ];

    [RelayCommand]
    private void Close()
    {
        CloseView(null);
    }
}

public sealed partial class EnableStageSpeedItem : ObservableObject
{
    [ObservableProperty]
    private StageSpeedEnum _stageSpeedEnum;

    [ObservableProperty]
    private bool _isEnable;
}