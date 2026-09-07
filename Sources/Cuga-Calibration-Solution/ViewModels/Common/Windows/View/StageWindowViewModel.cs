using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Enums.Stage;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.View;

[IOCAppService(ServiceType = typeof(StageWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class StageWindowViewModel(
    StatusViewModel statusViewModel,
    StageViewModel stageViewModel,
    IMessenger messenger,
    ILogger<StageWindowViewModel> logger) : PopupWindowViewModelBase(messenger, logger)
{
    [ObservableProperty]
    public partial StatusViewModel StatusViewModel { get; set; } = statusViewModel;

    [ObservableProperty]
    public partial StageCoordinateSystemEnum StageCoordinateSystemEnum { get; set; } = StageCoordinateSystemEnum.Bright;

    [ObservableProperty]
    public partial bool IsJoystickEnabled { get; set; }

    [ObservableProperty]
    public partial double StageStep { get; set; } = 1;

    [ObservableProperty]
    public partial Point GotoPosition { get; set; }

    [ObservableProperty]
    public partial double RotateTheta { get; set; }

    protected override void Loadeding(CancellationToken cancellationToken)
    {
        try
        {
            IsJoystickEnabled = true;
            stageViewModel.ToggleEnableJoystick(IsJoystickEnabled);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Toggle Enable Joystick Failed");
        }
    }

    [RelayCommand]
    private async Task ToggleEnableJoystickAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                IsJoystickEnabled = !IsJoystickEnabled;
                stageViewModel.ToggleEnableJoystick(IsJoystickEnabled);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Toggle Enable Joystick Failed");
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task MoveRelativeStageXyAsync(StageDirectionTypeEnum? stageDirectionLocalEnum)
    {
        if (stageDirectionLocalEnum is null) return;

        await Task.Run(() =>
        {
            try
            {
                stageViewModel.MoveRelativeStageXy(stageDirectionLocalEnum.Value, StageStep);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Move Relative Stage Xy Failed");
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private void GetGotoPosition()
    {
        GotoPosition = StageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Bright => StatusViewModel.BrightFieldPosition,
            StageCoordinateSystemEnum.Dark => StatusViewModel.DarkFieldPosition,
            StageCoordinateSystemEnum.Machine => StatusViewModel.MachinePosition,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
        };
    }

    [RelayCommand]
    private async Task SetAbsoluteStageXyAsync(CalChipSiteModelEnum? calChipSiteModelEnum)
    {
        if (calChipSiteModelEnum is null) return;

        await Task.Run(() =>
        {
            try
            {
                switch (StageCoordinateSystemEnum)
                {
                    case StageCoordinateSystemEnum.Bright:
                        stageViewModel.SetBrightFieldAbsoluteStageXy(GotoPosition, calChipSiteModelEnum.Value);
                        break;

                    case StageCoordinateSystemEnum.Dark:
                        stageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(GotoPosition, calChipSiteModelEnum.Value);
                        break;

                    case StageCoordinateSystemEnum.Machine:
                        stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(GotoPosition);
                        break;

                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(StageCoordinateSystemEnum));
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Set Absolute Stage Xy Failed");
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SetStageThetaAsync(bool? isAbsolute)
    {
        if (isAbsolute is null) return;

        await Task.Run(() =>
        {
            try
            {
                if (isAbsolute.Value)
                    stageViewModel.SetAbsoluteStageTheta(RotateTheta);
                else
                    stageViewModel.MoveRelativeStageTheta(RotateTheta);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Set Stage Theta Failed");
            }
        }).ConfigureAwait(false);
    }
}