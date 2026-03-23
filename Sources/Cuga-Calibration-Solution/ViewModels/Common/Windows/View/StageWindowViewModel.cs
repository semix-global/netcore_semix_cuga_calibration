using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using R3;

namespace CugaCalibration.ViewModels.Common.Windows.View;

[IOCAppService(ServiceType = typeof(StageWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class StageWindowViewModel(
    StageViewModel stageViewModel,
    IMessenger messenger,
    ILogger<StageWindowViewModel> logger) : PopupWindowViewModelBase(messenger, logger)
{
    [ObservableProperty]
    private StageCoordinateSystemEnum _stageCoordinateSystemEnum = StageCoordinateSystemEnum.Bright;

    [ObservableProperty]
    private bool _isJoystickEnabled;

    [ObservableProperty]
    private Point _brightFieldPosition;

    [ObservableProperty]
    private Point _darkFieldPosition;

    [ObservableProperty]
    private Point _machinePosition;

    [ObservableProperty]
    private double _machineTheta;

    [ObservableProperty]
    private double _stageStep = 1;

    [ObservableProperty]
    private Point _gotoPosition;

    [ObservableProperty]
    private double _rotateTheta;

    protected override void Loadeding(CancellationToken cancellationToken)
    {
        IsJoystickEnabled = true;
        stageViewModel.ToggleEnableJoystick(IsJoystickEnabled);

#pragma warning disable IDE0079
#pragma warning disable IDISP001
        var subscribe = Observable.Interval(TimeSpan.FromMilliseconds(CalibrationConstantsHelper.MonitorStageMilliseconds), cancellationToken).Subscribe(_ =>
        {
            try
            {
                GetPosition();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Get Position Failed!");
            }
        });
        cancellationToken.Register(subscribe.Dispose /*在适当的时候取消订阅*/); // CancellationTokenRegistration.Dispose() // 注册将被删除, 无CancellationTokenRegistration.Unregister()
#pragma warning restore IDISP001
#pragma warning restore IDE0079
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
        await Task.Run(() =>
        {
            stageViewModel.MoveRelativeStageXy(stageDirectionLocalEnum.Value, StageStep);
            GetPosition();
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private void GetGotoPosition()
    {
        GotoPosition = StageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Bright => BrightFieldPosition,
            StageCoordinateSystemEnum.Dark => DarkFieldPosition,
            StageCoordinateSystemEnum.Machine => MachinePosition,
            _ => throw new ArgumentOutOfRangeException()
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

            GetPosition();
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

    private void GetPosition()
    {
        var resultBright = stageViewModel.GetBrightFieldStagePosition();
        var resultDark = stageViewModel.GetDarkFieldStagePosition();
        var resultMachine = stageViewModel.GetMachineStagePosition();
        var resultMachineStageTheta = stageViewModel.GetMachineStageTheta();

        BrightFieldPosition = resultBright;
        DarkFieldPosition = resultDark;
        MachinePosition = resultMachine;
        MachineTheta = resultMachineStageTheta;
    }
}