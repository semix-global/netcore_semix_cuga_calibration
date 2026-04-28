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
        IsJoystickEnabled = true;
        stageViewModel.ToggleEnableJoystick(IsJoystickEnabled);
    }

    [RelayCommand]
    private async Task ToggleEnableJoystickAsync()
    {
        await Task.Run(() =>
        {
            IsJoystickEnabled = !IsJoystickEnabled;
            stageViewModel.ToggleEnableJoystick(IsJoystickEnabled);
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task MoveRelativeStageXyAsync(StageDirectionTypeEnum? stageDirectionLocalEnum)
    {
        if (stageDirectionLocalEnum is null) return;
        
        await Task.Run(() => stageViewModel.MoveRelativeStageXy(stageDirectionLocalEnum.Value, StageStep)).ConfigureAwait(false);
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
            switch (StageCoordinateSystemEnum)
            {
                case StageCoordinateSystemEnum.Bright:
                    stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(GotoPosition, calChipSiteModelEnum.Value);
                    break;

                case StageCoordinateSystemEnum.Dark:
                    stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(GotoPosition, calChipSiteModelEnum.Value);
                    break;

                case StageCoordinateSystemEnum.Machine:
                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(GotoPosition);
                    break;

                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(StageCoordinateSystemEnum));
                    break;
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SetStageThetaAsync(bool? isAbsolute)
    {
        await Task.Run(() =>
        {
            if (isAbsolute is null) return;

            if (isAbsolute.Value)
                stageViewModel.SetAbsoluteStageTheta(RotateTheta);
            else
                stageViewModel.MoveRelativeStageTheta(RotateTheta);
        }).ConfigureAwait(false);
    }
}