using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Helper;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using R3;

namespace CugaCalibration.ViewModels.Common.Windows.View;

[IOCAppService(ServiceType = typeof(ReviewWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ReviewWindowViewModel(
    ReviewViewModel reviewViewModel,
    IMessenger messenger,
    ILogger<ReviewWindowViewModel> logger) : PopupWindowViewModelBase(messenger, logger)
{
    [ObservableProperty]
    private ReviewViewModel _reviewViewModel = reviewViewModel;

    protected override void Loadeding(CancellationToken cancellationToken)
    {
        ReviewViewModel.Monitor(cancellationToken);

#pragma warning disable IDE0079
#pragma warning disable IDISP001
        var subscribe = Observable.Interval(TimeSpan.FromMilliseconds(CalibrationConstantsHelper.MonitorStageMilliseconds), cancellationToken).Subscribe(_ =>
        {
            try
            {
                ReviewViewModel.GetBrightFieldImageMemoryByteArray();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Get Bright Field Image Failed!");
            }
        });
        cancellationToken.Register(subscribe.Dispose);
#pragma warning restore IDISP001
#pragma warning restore IDE0079
    }
}