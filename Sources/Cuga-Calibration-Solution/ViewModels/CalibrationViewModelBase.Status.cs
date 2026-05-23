using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Enums;
using Core.Models.Events;
using Core.Models.Models;
using Net.Utilities.WPF.MVVM.Events;
using System.Diagnostics.CodeAnalysis;

namespace CugaCalibration.ViewModels;

public partial class CalibrationViewModelBase
{
    public virtual IReadOnlyList<CalibrationItemStep> CalibrationSteps => [];

    public double CalibrationProgress => ViewEnum switch
    {
        CalibrationItemViewEnum.Welcome => 0d,
        CalibrationItemViewEnum.Review => 100d,
        _ => 0 <= CalibrationStepIndex && CalibrationStepIndex <= CalibrationSteps.Count - 1
            ? CalibrationSteps[CalibrationStepIndex].StepIsNextEnable
                ? (CalibrationStepIndex + 1d) / CalibrationSteps.Count * 100d
                : (CalibrationStepIndex + 0d) / CalibrationSteps.Count * 100d
            : 0d
    };

    public int CalibrationDisplayStepIndex => 0 <= CalibrationStepIndex && CalibrationStepIndex <= CalibrationSteps.Count - 1
        ? CalibrationStepIndex + 1
        : int.MinValue;

    public string CalibrationStepName => 0 <= CalibrationStepIndex && CalibrationStepIndex <= CalibrationSteps.Count - 1
        ? CalibrationSteps[CalibrationStepIndex].StepName
        : string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrationProgress))]
    public partial CalibrationItemViewEnum ViewEnum { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrationProgress))]
    [NotifyPropertyChangedFor(nameof(CalibrationDisplayStepIndex))]
    [NotifyPropertyChangedFor(nameof(CalibrationStepName))]
    public partial int CalibrationStepIndex { get; set; } = -1;

    public virtual void UpdateEntryStatus(CalibrationDTOBase calibration, CancellationToken cancellationToken)
    {
    }

    public virtual void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
    }

    [MemberNotNull(nameof(_cancellationTokenSource))]
    private void CheckStatus()
    {
        if (_cancellationTokenSource is null) RefreshToken();

        if (_cancellationTokenSource.IsCancellationRequested) ThrowHelper.ThrowOperationCanceledException();
    }

    private void UpdateLoadingStatus()
    {
        UpdateDisableAll();
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCancelEnable(true));
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());

        ViewEnum = CalibrationItemViewEnum.Loading;
    }

    private void UpdateCancelLoadingStatus()
    {
        UpdateDisableAll();
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());

        ViewEnum = CalibrationItemViewEnum.Loading;
    }

    private void UpdateFailedStatus()
    {
        UpdateDisableAll();
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCancelEnable(true));
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());

        ViewEnum = CalibrationItemViewEnum.Failed;
    }

    private void UpdateCancelStatus()
    {
        CalibrationStepIndex = int.MinValue;

        UpdateDisableAll();
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());

        ViewEnum = CalibrationItemViewEnum.Cancel;
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
        CalibrationStepIndex = 0;
        foreach (var calibrationItemStep in CalibrationSteps) calibrationItemStep.StepIsNextEnable = calibrationItemStep.DefaultIsNextEnable;

        UpdateDisableAll();
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCalibrateEnable(false));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsReviewEnable(false));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsCancelEnable(true));
        Messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());

        ViewEnum = CalibrationItemViewEnum.Calibration;

        UpdatePreviousNextStatus();
    }

    private void UpdateReviewStatus()
    {
        CalibrationStepIndex = int.MinValue;

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

        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsPreviousEnable(0 < CalibrationStepIndex && CalibrationStepIndex <= CalibrationSteps.Count - 1));
        Messenger.Send(ToggleCalibrateEventFactory.UpdateIsNextEnable(0 <= CalibrationStepIndex && CalibrationStepIndex < CalibrationSteps.Count && CalibrationSteps[CalibrationStepIndex].StepIsNextEnable));
        OnPropertyChanged(nameof(CalibrationProgress));

        ViewEnum = CalibrationItemViewEnum.Calibration;
    }

    private void UpdateDisableAll()
    {
        Messenger.Send(ToggleCalibrateEventFactory.Disable());
        Messenger.Send(PopupWindowEventFactory.DisableIsPopupWindowEnable());
    }
}