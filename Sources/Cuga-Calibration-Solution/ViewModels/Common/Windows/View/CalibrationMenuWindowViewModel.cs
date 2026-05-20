using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Models.Common.Cookies;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.View;

[IOCAppService(ServiceType = typeof(CalibrationMenuWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class CalibrationMenuWindowViewModel(
    IMessenger messenger,
    ILogger<CalibrationMenuWindowViewModel> logger) : PopupWindowViewModelBase(messenger, logger)
{
    [ObservableProperty]
    public partial CalibrationMenu CalibrationMenu { get; set; } = new();

    protected override void Loadeding(CancellationToken cancellationToken)
    {
    }
}