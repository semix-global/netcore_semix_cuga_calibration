using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Models.Setting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.Events;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels;

[IOCAppService(ServiceType = typeof(LogWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LogWindowViewModel(
    IMessenger messenger,
    CalibrationSetting calibrationSetting,
    ILogger<LogWindowViewModel> logger) : PopupWindowViewModelBase(messenger, logger)
{
    [ObservableProperty]
    private CalibrationSetting _calibrationSetting = calibrationSetting;

    protected override void Loadeding(CancellationToken cancellationToken)
    {
    }

    public override void Receive(PopupWindowEvent popupWindowEvent)
    {
    }
}