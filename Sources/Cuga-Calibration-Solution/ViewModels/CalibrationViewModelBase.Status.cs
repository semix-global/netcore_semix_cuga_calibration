using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Enums;
using Core.Models.Events;
using Core.Models.Models;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM.Events;
using System.Diagnostics.CodeAnalysis;

namespace CugaCalibration.ViewModels;

public partial class CalibrationViewModelBase : IRecipient<PropertyChangedMessage<bool>>
{
    public virtual IReadOnlyList<CalibrationItemStep> CalibrationStepList => [];

    public double CalibrationProgress =>
        ViewEnum switch
        {
            CalibrationItemViewEnum.Welcome => 0d,
            CalibrationItemViewEnum.Review => 100d,
            CalibrationItemViewEnum.Loading or CalibrationItemViewEnum.Calibration =>
                0 <= CalibrationStepIndex && CalibrationStepIndex < CalibrationStepList.Count - 1
                    ? CalibrationStepList[CalibrationStepIndex].StepIsNextEnable
                        ? (CalibrationStepIndex + 1d) / CalibrationStepList.Count * 100d
                        : (CalibrationStepIndex + 0d) / CalibrationStepList.Count * 100d
                    : 0,
            _ => 0d
        };

    public string CalibrationStepName => 0 <= CalibrationStepIndex && CalibrationStepIndex < CalibrationStepList.Count - 1
        ? CalibrationStepList[CalibrationStepIndex].StepName
        : string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrationProgress))]
    [NotifyPropertyChangedFor(nameof(CalibrationStepName))]
    public partial CalibrationItemViewEnum ViewEnum { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrationProgress))]
    public partial int CalibrationStepIndex { get; set; } = -1;

    public virtual void UpdateEntryStatus(CancellationToken cancellationToken)
    {
    }

    [MemberNotNull(nameof(_cancellationTokenSource))]
    private void CheckStatus()
    {
        if (_cancellationTokenSource is null) RefreshToken();

        if (_cancellationTokenSource.IsCancellationRequested) ThrowHelper.ThrowOperationCanceledException();
    }

    private void UpdateFailedStatus()
    {
        CalibrationStepIndex = int.MinValue;

        UpdateDisableAll();
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCancelEnable(true));
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());

        ViewEnum = CalibrationItemViewEnum.Error;
    }

    private void UpdateCancelStatus()
    {
        CalibrationStepIndex = int.MinValue;

        UpdateDisableAll();
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());

        ViewEnum = CalibrationItemViewEnum.Welcome;
    }

    private void UpdateWelcomeStatus()
    {
        CalibrationStepIndex = int.MinValue;

        UpdateDisableAll();
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCalibrateEnable(true));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsReviewEnable(true));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCancelEnable(true));
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());

        ViewEnum = CalibrationItemViewEnum.Welcome;
    }

    private void UpdateCalibrateStatus()
    {
        CalibrationStepIndex = int.MinValue;

        UpdateDisableAll();
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCalibrateEnable(false));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsReviewEnable(false));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCancelEnable(true));
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());

        ViewEnum = CalibrationItemViewEnum.Calibration;

        if (CalibrationStepIndex < 0) CalibrationStepIndex = 0;
        UpdatePreviousNextStatus();
    }

    private void UpdateReviewStatus()
    {
        CalibrationStepIndex = CalibrationStepList.Count - 1;

        UpdateDisableAll();
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCalibrateEnable(false));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsReviewEnable(false));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCancelEnable(true));
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());

        ViewEnum = CalibrationItemViewEnum.Review;
    }

    private void UpdatePreviousNextStatus()
    {
        UpdateDisableAll();
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCalibrateEnable(false));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsReviewEnable(false));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCancelEnable(true));
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());
        if (IsCalibrated)
        {
            Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingClearHtml());
            HtmlLogUniqueId = Guid.NewGuid();
            DialogWindowProvider.ShowDialog($"Calibration {Name} Ok!");

            UpdateWelcomeStatus();
        }
        else
        {
            UpdatePreviousStatus();
            UpdateNextStatus();

            ViewEnum = CalibrationItemViewEnum.Calibration;
        }
    }

    private void UpdateDisableAll()
    {
        Messenger.Send(ToggleCalibrateEventFactory.Disable());
        Messenger.Send(PopupWindowEventFactory.DisableIsPopupWindowEnable());
    }

    private void UpdatePreviousStatus()
    {
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsPreviousEnable(0 < CalibrationStepIndex && CalibrationStepIndex <= CalibrationStepList.Count - 1));
    }

    private void UpdateNextStatus()
    {
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsNextEnable(0 <= CalibrationStepIndex && CalibrationStepIndex < CalibrationStepList.Count && CalibrationStepList[CalibrationStepIndex].StepIsNextEnable));
    }
}