using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.View;

[IOCAppService(ServiceType = typeof(ReviewWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ReviewWindowViewModel(
    StatusViewModel statusViewModel,
    ReviewViewModel reviewViewModel,
    IMessenger messenger,
    ILogger<ReviewWindowViewModel> logger) : PopupWindowViewModelBase(messenger, logger)
{
    [ObservableProperty]
    public partial StatusViewModel StatusViewModel { get; set; } = statusViewModel;

    [ObservableProperty]
    public partial ReviewViewModel ReviewViewModel { get; set; } = reviewViewModel;

    protected override void Loadeding(CancellationToken cancellationToken)
    {
    }
}