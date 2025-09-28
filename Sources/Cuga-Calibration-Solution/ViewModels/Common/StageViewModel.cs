using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.StageMap;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(StageViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class StageViewModel(
    ICalibrationStageService calibrationStageService,
    AfViewModel afViewModel) : ViewModelBase
{
    #region 服务

    public bool Connect()
    {
        var ret = calibrationStageService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleEnableJoystick(bool isJoystickEnabled)
    {
        var ret = calibrationStageService.ToggleEnableJoystick(isJoystickEnabled);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetSpeed(StageSpeedEnum stageSpeedEnum, OpticsMagTypeEnum opticsMagTypeEnum)
    {
        var ret = calibrationStageService.SetSpeed(stageSpeedEnum, opticsMagTypeEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetXSpeedValue(double speedValue)
    {
        var ret = calibrationStageService.SetXSpeedValue(speedValue);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetYSpeedValue(double speedValue)
    {
        var ret = calibrationStageService.SetYSpeedValue(speedValue);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public double GetMachineStageTheta()
    {
        var ret = calibrationStageService.GetMachineStageTheta();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void MoveRelativeStageTheta(double degrees)
    {
        var ret = calibrationStageService.MoveRelativeStageTheta(degrees);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetAbsoluteStageTheta(double degrees)
    {
        var ret = calibrationStageService.SetAbsoluteStageTheta(degrees);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void MoveRelativeStageXy(StageDirectionTypeEnum stageDirectionTypeEnum, double step)
    {
        var ret = calibrationStageService.MoveRelativeStageXy(stageDirectionTypeEnum, step);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void MoveRelativeStageXy(Point point)
    {
        var result = GetBrightFieldStagePosition();
        result = BrightFieldToMachinePosition(result + (Vector)point);

        SetSpeed(StageSpeedEnum.Low, OpticsMagTypeEnum.Low);
        var ret = calibrationStageService.SetMachineAbsoluteStageXy(result);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public Point GetBrightFieldStagePosition()
    {
        var ret = calibrationStageService.GetBrightFieldStagePosition();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetBrightFieldAbsoluteStageXy(Point point)
        => SetCalChipBrightFieldAbsoluteStageXy(point, CalChipSiteModelEnum.ChuckModel);

    public void SetBrightFieldAbsoluteStageXyByNotAutoFocus(Point point)
    {
        var result = BrightFieldToMachinePosition(point);

        SetMachineAbsoluteStageXyByNotAutoFocus(result);
    }

    public Point GetDarkFieldStagePosition()
    {
        var ret = calibrationStageService.GetDarkFieldStagePosition();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetDarkFieldAbsoluteStageXyByNotAutoFocus(Point point)
        => SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(point, CalChipSiteModelEnum.ChuckModel);

    public Point GetMachineStagePosition()
    {
        var ret = calibrationStageService.GetMachineStagePosition();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetMachineAbsoluteStageXy(Point point)
    {
        var result = MachineToBrightFieldPosition(point);

        SetBrightFieldAbsoluteStageXy(result);
    }

    public void SetMachineAbsoluteStageXyByNotAutoFocus(Point point, bool isAutoSpeedMove = true)
    {
        afViewModel.ToggleBrightFieldEnable(false);

        if (isAutoSpeedMove) SetSpeed(StageSpeedEnum.Low, OpticsMagTypeEnum.Low);

        var ret = calibrationStageService.SetMachineAbsoluteStageXy(point);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        afViewModel.ToggleCalChipSiteModelEnum(CalChipSiteModelEnum.ChuckModel);
    }

    public (double XDirection, double YDirection) GetMachineDirection()
    {
        var ret = calibrationStageService.GetMachineDirection();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public Point BrightFieldToMachinePosition(Point brightPosition)
    {
        var ret = calibrationStageService.BrightFieldToMachinePosition(brightPosition);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public Point DarkFieldToMachinePosition(Point darkPosition)
    {
        var ret = calibrationStageService.DarkFieldToMachinePosition(darkPosition);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public Point MachineToBrightFieldPosition(Point machinePosition)
    {
        var ret = calibrationStageService.MachineToBrightFieldPosition(machinePosition);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public Point MachineToDarkFieldPosition(Point machinePosition)
    {
        var ret = calibrationStageService.MachineToDarkFieldPosition(machinePosition);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetCalChipBrightFieldAbsoluteStageXy(Point point, CalChipSiteModelEnum calChipSiteModelEnum)
    {
        var (isReview, _) = afViewModel.GetBrightFieldStatus();
        if (isReview == false)
        {
            afViewModel.ToggleBrightFieldEnable(false);
        }

        var ret = calibrationStageService.SetBrightFieldAbsoluteStageXy(point);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        afViewModel.ToggleCalChipSiteModelEnum(calChipSiteModelEnum);

        afViewModel.ToggleBrightFieldEnable(true);
    }

    public void SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(Point point, CalChipSiteModelEnum calChipSiteModelEnum)
    {
        afViewModel.ToggleBrightFieldEnable(false);

        SetSpeed(StageSpeedEnum.Low, OpticsMagTypeEnum.Low);

        var ret = calibrationStageService.SetDarkFieldAbsoluteStageXy(point);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        afViewModel.ToggleCalChipSiteModelEnum(calChipSiteModelEnum);
    }

    public void SetCalChipDswBrightFieldAbsoluteStageXy(Point point) => SetCalChipBrightFieldAbsoluteStageXy(point, CalChipSiteModelEnum.DswModel);

    public void SetCalChipDswDarkFieldAbsoluteStageXyByNotAutoFocus(Point point) => SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(point, CalChipSiteModelEnum.DswModel);

    public void SetCalChipUndefinedBrightFieldAbsoluteStageXy(Point point) => SetCalChipBrightFieldAbsoluteStageXy(point, CalChipSiteModelEnum.UndefinedModel);

    public void SetCalChipUndefinedDarkFieldAbsoluteStageXyByNotAutoFocus(Point point) => SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(point, CalChipSiteModelEnum.UndefinedModel);

    public void SetCalChipHazeBrightFieldAbsoluteStageXy(Point point) => SetCalChipBrightFieldAbsoluteStageXy(point, CalChipSiteModelEnum.HazeModel);

    public void SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(Point point) => SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(point, CalChipSiteModelEnum.HazeModel);

    public void SetCalChipShinyWaferBrightFieldAbsoluteStageXy(Point point) => SetCalChipBrightFieldAbsoluteStageXy(point, CalChipSiteModelEnum.ShinyWaferModel);

    public void SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(Point point) => SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(point, CalChipSiteModelEnum.ShinyWaferModel);

    public Point FindWaferCenterByAutomatic(int offsetThreshold = 100)
    {
        var ret = calibrationStageService.FindWaferCenterByAutomatic(offsetThreshold);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public (Point Result, List<byte[]> bitmapMemoryBytes) FindWaferCenterByManually(Point offset, List<Point>? waferEdgeOffsets = null)
    {
        var ret = calibrationStageService.FindWaferCenterByManually(out var bitmapMemoryBytes, offset, waferEdgeOffsets);

        return ret.IsSuccess ? (ret.Anything, bitmapMemoryBytes) : throw new CugaException(ret.ErrorMsg);
    }

    public AlignmentSiteDto MarkAlignSite1(AlgorithmTemplateSizeEnum algorithmTemplateSizeEnum, AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, AlgorithmWaferTypeEnum algorithmWaferTypeEnum)
    {
        var ret = calibrationStageService.MarkAlignSite1(algorithmTemplateSizeEnum, algorithmTemplateTypeEnum, algorithmWaferTypeEnum);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public AlignmentSiteDto MarkAlignSite2(AlignmentSiteDto site, AlgorithmWaferTypeEnum algorithmWaferTypeEnum)
    {
        var ret = calibrationStageService.MarkAlignSite2(site, algorithmWaferTypeEnum);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public AlignmentResultDto Alignment(
        AlignmentSiteDto lowSite1,
        AlignmentSiteDto lowSite2,
        AlignmentSiteDto highSite1,
        AlignmentSiteDto highSite2,
        MicroscopeLensInformation lowMicroscopeLensInformation,
        MicroscopeLensInformation highMicroscopeLensInformation,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum)
    {
        var ret = calibrationStageService.Alignment(lowSite1, lowSite2, highSite1, highSite2, lowMicroscopeLensInformation, highMicroscopeLensInformation, algorithmWaferTypeEnum);

        if (ret.IsSuccess == false)
            throw new CugaException(ret.ErrorMsg);

        var result = ret.Anything;
        result.MarkPoint1 = BrightFieldToMachinePosition(result.MarkPoint1);
        result.MarkPoint2 = BrightFieldToMachinePosition(result.MarkPoint2);
        return result;
    }

    public AlignmentSiteDto MarkAlignSite1DarkField(
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        AlgorithmTemplateSizeEnum algorithmTemplateSizeEnum,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum
    )
    {
        var ret = calibrationStageService.MarkAlignSite1DarkField(opticsMagTypeEnum, xStageSpeedEnum, algorithmTemplateSizeEnum, algorithmWaferTypeEnum);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        SetBrightFieldAbsoluteStageXy(ret.Anything.Location);

        return ret.Anything;
    }

    public AlignmentSiteDto MarkAlignSite2DarkField(
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        AlignmentSiteDto site,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum)
    {
        var ret = calibrationStageService.MarkAlignSite2DarkField(opticsMagTypeEnum, xStageSpeedEnum, site, algorithmWaferTypeEnum);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        SetBrightFieldAbsoluteStageXy(ret.Anything.Location);

        return ret.Anything;
    }

    public AlignmentResultDto AlignmentDarkField(
        AlignmentSiteDto brightFieldLowSite1,
        AlignmentSiteDto brightFieldLowSite2,
        AlignmentSiteDto darkFieldHighSite1,
        AlignmentSiteDto darkFieldHighSite2,
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        MicroscopeLensInformation lowMicroscopeLensInformation,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum
    )
    {
        var ret = calibrationStageService.AlignmentDarkField(
            brightFieldLowSite1,
            brightFieldLowSite2,
            darkFieldHighSite1,
            darkFieldHighSite2,
            opticsMagTypeEnum,
            xStageSpeedEnum,
            lowMicroscopeLensInformation,
            algorithmWaferTypeEnum);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        SetBrightFieldAbsoluteStageXy(ret.Anything.MarkPoint2);

        return ret.Anything;
    }

    public void SetGantryOffset(double gantryOffset)
    {
        var ret = calibrationStageService.SetGantryOffset(gantryOffset);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetEnableStageMap(bool enable)
    {
        var ret = calibrationStageService.ToggleEnableStageMap(enable);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetStageMap(StageMapDto stageMapDto)
    {
        var ret = calibrationStageService.SetStageMap(stageMapDto);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetBrightFieldCenterMachinePositionValue(Point position)
    {
        var ret = calibrationStageService.SetBrightFieldCenterMachinePositionValue(position);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        afViewModel.SetSensorBrightFieldChuckCenterMachinePositionValue(position);
    }

    public void SetDarkFieldCenterMachinePositionValue(Point position)
    {
        var ret = calibrationStageService.SetDarkFieldCenterMachinePositionValue(position);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        afViewModel.SetSensorDarkFieldChuckCenterMachinePositionValue(position);
    }

    public Point GetBrightFieldCenterMachineStagePosition()
    {
        var ret = calibrationStageService.GetBrightFieldCenterMachinePositionValue();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void ResetXYGlobalScale()
    {
        var ret = calibrationStageService.SetXYGlobalScale(1, 1);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetXYGlobalScale(double xScale, double yScale)
    {
        var ret = calibrationStageService.SetXYGlobalScale(xScale, yScale);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ResetTScale()
    {
        var ret = calibrationStageService.SetTScale(1);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetTScale(double scaleT)
    {
        var ret = calibrationStageService.SetTScale(scaleT);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public Point GetEfemLoadWaferMachineStagePosition()
    {
        var ret = calibrationStageService.GetEfemLoadWaferMachineStagePosition();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public double GetEfemLoadWaferMachineStageTheta()
    {
        var ret = calibrationStageService.GetEfemLoadWaferMachineStageTheta();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    #endregion 服务

    #region Command

    [RelayCommand]
    private async Task SetBrightFieldAbsoluteStageXyAsync(Point? point) => await InvokeAsync(point, SetBrightFieldAbsoluteStageXy).ConfigureAwait(false);

    [RelayCommand]
    private async Task SetMachineAbsoluteStageXyAsync(Point? point) => await InvokeAsync(point, SetMachineAbsoluteStageXy).ConfigureAwait(false);

    [RelayCommand]
    private async Task SetMachineAbsoluteStageXyByNotAutoFocusAsync(Point? point) => await InvokeAsync(point, p => SetMachineAbsoluteStageXyByNotAutoFocus(p)).ConfigureAwait(false);

    [RelayCommand]
    private async Task SetCalChipDswBrightFieldAbsoluteStageXyAsync(Point? point) => await InvokeAsync(point, SetCalChipDswBrightFieldAbsoluteStageXy).ConfigureAwait(false);

    [RelayCommand]
    private async Task SetCalChipDswDarkFieldAbsoluteStageXyByNotAutoFocusAsync(Point? point) => await InvokeAsync(point, SetCalChipDswDarkFieldAbsoluteStageXyByNotAutoFocus).ConfigureAwait(false);

    [RelayCommand]
    private async Task SetCalChipUndefinedBrightFieldAbsoluteStageXyAsync(Point? point) => await InvokeAsync(point, SetCalChipUndefinedBrightFieldAbsoluteStageXy).ConfigureAwait(false);

    [RelayCommand]
    private async Task SetCalChipUndefinedDarkFieldAbsoluteStageXyByNotAutoFocusAsync(Point? point) => await InvokeAsync(point, SetCalChipUndefinedDarkFieldAbsoluteStageXyByNotAutoFocus).ConfigureAwait(false);

    [RelayCommand]
    private async Task SetCalChipHazeBrightFieldAbsoluteStageXyAsync(Point? point) => await InvokeAsync(point, SetCalChipHazeBrightFieldAbsoluteStageXy).ConfigureAwait(false);

    [RelayCommand]
    private async Task SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocusAsync(Point? point) => await InvokeAsync(point, SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus).ConfigureAwait(false);

    [RelayCommand]
    private async Task SetCalChipShinyWaferBrightFieldAbsoluteStageXyAsync(Point? point) => await InvokeAsync(point, SetCalChipShinyWaferBrightFieldAbsoluteStageXy).ConfigureAwait(false);

    [RelayCommand]
    private async Task SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocusAsync(Point? point) => await InvokeAsync(point, SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus).ConfigureAwait(false);

    private static Task InvokeAsync(Point? point, Action<Point> action)
    {
        return Task.Run(() =>
        {
            if (point is null) return;

            action(point.Value);
        });
    }

    #endregion Command
}