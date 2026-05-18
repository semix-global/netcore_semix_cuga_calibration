using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Models.Common.Cookies;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.View;

[IOCAppService(ServiceType = typeof(CalibrationStatusWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CalibrationStatusWindowViewModel(
    IMessenger messenger,
    ILogger<CalibrationStatusWindowViewModel> logger) : PopupWindowViewModelBase(messenger, logger)
{
    [ObservableProperty]
    public partial CalibrationMenu? Menu { get; set; }

    protected override void Loadeding(CancellationToken cancellationToken)
    {
    }
}
