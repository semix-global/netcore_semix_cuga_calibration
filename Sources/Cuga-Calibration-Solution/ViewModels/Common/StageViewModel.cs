using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(StageViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class StageViewModel(
    ICalibrationStageService calibrationStageService,
    AfViewModel afViewModel,
    CalibrationSetting calibrationSetting,
    ILogger<StageViewModel> logger) : ViewModelBase
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

        var ret = calibrationStageService.SetMachineAbsoluteStageXy(result);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public Point GetBrightFieldStagePosition()
    {
        var ret = calibrationStageService.GetBrightFieldStagePosition();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public Point GetDarkFieldStagePosition()
    {
        var ret = calibrationStageService.GetDarkFieldStagePosition();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public Point GetMachineStagePosition()
    {
        var ret = calibrationStageService.GetMachineStagePosition();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public (double XDirection, double YDirection) GetMachineDirection()
    {
        var ret = calibrationStageService.GetMachineDirection();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public AlgorithmWaferTypeEnum GetAlgorithmWaferType()
    {
        var ret = calibrationStageService.GetAlgorithmWaferType();

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

    public void SetBrightFieldAbsoluteStageXy(Point point, CalChipSiteModelEnum calChipSiteModelEnum)
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

    public void SetBrightFieldAbsoluteStageXyByNotAutoFocus(Point point, CalChipSiteModelEnum calChipSiteModelEnum)
    {
        afViewModel.ToggleBrightFieldEnable(false);

        var ret = calibrationStageService.SetBrightFieldAbsoluteStageXy(point);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        afViewModel.ToggleCalChipSiteModelEnum(calChipSiteModelEnum);
    }

    public void SetDarkFieldAbsoluteStageXyByNotAutoFocus(Point point, CalChipSiteModelEnum calChipSiteModelEnum)
    {
        afViewModel.ToggleBrightFieldEnable(false);

        var ret = calibrationStageService.SetDarkFieldAbsoluteStageXy(point);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        afViewModel.ToggleCalChipSiteModelEnum(calChipSiteModelEnum);
    }

    public void SetMachineAbsoluteStageXyByNotAutoFocus(Point point)
    {
        afViewModel.ToggleBrightFieldEnable(false);

        var ret = calibrationStageService.SetMachineAbsoluteStageXy(point);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        afViewModel.ToggleCalChipSiteModelEnum(CalChipSiteModelEnum.ChuckModel);
    }

    public void SetMachineAbsoluteStageXyByFixedSpeedAndNotAutoFocus(Point point)
    {
        afViewModel.ToggleBrightFieldEnable(false);

        var ret = calibrationStageService.SetMachineAbsoluteStageXyByFixedSpeed(point);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        afViewModel.ToggleCalChipSiteModelEnum(CalChipSiteModelEnum.ChuckModel);
    }

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

    public AlignmentSiteDto MarkAlignSite1(AlgorithmTemplateSizeEnum algorithmTemplateSizeEnum, TemplateMatchSetting templateMatchSetting)
    {
        var ret = calibrationStageService.MarkAlignSite1(algorithmTemplateSizeEnum, templateMatchSetting.Halcon.TemplateType);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public AlignmentSiteDto MarkAlignSite1(AlgorithmTemplateSizeEnum algorithmTemplateSizeEnum, AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum)
    {
        var ret = calibrationStageService.MarkAlignSite1(algorithmTemplateSizeEnum, algorithmTemplateTypeEnum);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public AlignmentSiteDto MarkAlignSite2(AlignmentSiteDto site)
    {
        var ret = calibrationStageService.MarkAlignSite2(site);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public AlignmentResultDto Alignment(
        AlignmentSiteDto lowSite1,
        AlignmentSiteDto lowSite2,
        AlignmentSiteDto highSite1,
        AlignmentSiteDto highSite2,
        MicroscopeLensInformation lowMicroscopeLensInformation,
        MicroscopeLensInformation highMicroscopeLensInformation,
        CalChipSiteModelEnum calChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel)
    {
        SetAbsoluteStageTheta(0);
        afViewModel.ToggleCalChipSiteModelEnum(calChipSiteModelEnum);

        var ret = calibrationStageService.Alignment(lowSite1, lowSite2, highSite1, highSite2, lowMicroscopeLensInformation, highMicroscopeLensInformation, calChipSiteModelEnum is not CalChipSiteModelEnum.ChuckModel);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        var result = ret.Anything;

        result.MarkPoint1 = BrightFieldToMachinePosition(result.MarkPoint1);
        result.MarkPoint2 = BrightFieldToMachinePosition(result.MarkPoint2);

        return result;
    }

    public AlignmentResultDto AlignmentVerify(
        AlignmentSiteDto lowSite1,
        AlignmentSiteDto lowSite2,
        AlignmentSiteDto highSite1,
        AlignmentSiteDto highSite2,
        MicroscopeLensInformation lowMicroscopeLensInformation,
        MicroscopeLensInformation highMicroscopeLensInformation,
        CalChipSiteModelEnum calChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel)
    {
        afViewModel.ToggleCalChipSiteModelEnum(calChipSiteModelEnum);

        var ret = calibrationStageService.AlignmentVerify(lowSite1, lowSite2, highSite1, highSite2, lowMicroscopeLensInformation, highMicroscopeLensInformation, calChipSiteModelEnum is not CalChipSiteModelEnum.ChuckModel);

        if (ret.IsSuccess == false)
            throw new CugaException(ret.ErrorMsg);

        var result = ret.Anything;
        result.MarkPoint1 = BrightFieldToMachinePosition(result.MarkPoint1);
        result.MarkPoint2 = BrightFieldToMachinePosition(result.MarkPoint2);
        return result;
    }

    public AlignmentSiteDto MarkAlignSite1DarkField(
        ProductivityInformation productivityInformation,
        AlgorithmTemplateSizeEnum algorithmTemplateSizeEnum,
        LaserLightInformation? laserLightInformation = null)
    {
        laserLightInformation ??= calibrationSetting.SettingCommonParam.MainLaserLightInformation;
        var ret = calibrationStageService.MarkAlignSite1DarkField(productivityInformation, algorithmTemplateSizeEnum, laserLightInformation);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        SetBrightFieldAbsoluteStageXy(ret.Anything.Location, CalChipSiteModelEnum.ChuckModel);

        return ret.Anything;
    }

    public AlignmentSiteDto MarkAlignSite2DarkField(
        ProductivityInformation productivityInformation,
        AlignmentSiteDto site)
    {
        var ret = calibrationStageService.MarkAlignSite2DarkField(productivityInformation, site);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        SetBrightFieldAbsoluteStageXy(ret.Anything.Location, CalChipSiteModelEnum.ChuckModel);

        return ret.Anything;
    }

    public AlignmentResultDto AlignmentDarkField(
        AlignmentSiteDto brightFieldLowSite1,
        AlignmentSiteDto brightFieldLowSite2,
        AlignmentSiteDto darkFieldHighSite1,
        AlignmentSiteDto darkFieldHighSite2,
        ProductivityInformation productivityInformation,
        MicroscopeLensInformation lowMicroscopeLensInformation,
        LaserLightInformation? laserLightInformation = null)
    {
        laserLightInformation ??= calibrationSetting.SettingCommonParam.MainLaserLightInformation;
        var ret = calibrationStageService.AlignmentDarkField(
            brightFieldLowSite1,
            brightFieldLowSite2,
            darkFieldHighSite1,
            darkFieldHighSite2,
            productivityInformation,
            lowMicroscopeLensInformation,
            laserLightInformation);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        var result = ret.Anything;
        SetBrightFieldAbsoluteStageXy(result.MarkPoint2, CalChipSiteModelEnum.ChuckModel);

        result.MarkPoint1 = DarkFieldToMachinePosition(result.MarkPoint1);
        result.MarkPoint2 = DarkFieldToMachinePosition(result.MarkPoint2);

        return result;
    }

    public void AlignmentBlankWafer()
    {
        var ret = calibrationStageService.AlignmentBlankWafer();

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetGantryOffset(double gantryOffset, Guid guid)
    {
        logger.LogHtmlInformation("Set Gantry Offset", HtmlHeaderLevelEnum.Header5, guid.LoggingHtml());

        var ret = calibrationStageService.SetGantryOffset(gantryOffset);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        var reReadGantryOffsetRet = calibrationStageService.GetGantryOffset();
        if (reReadGantryOffsetRet.IsSuccess == false)
            throw new CugaException($"Failed to validate the read-back value.{reReadGantryOffsetRet.ErrorMsg}. ApplyGantryOffset={gantryOffset}.");

        var reReadGantryOffset = reReadGantryOffsetRet.Anything;
        var result = Math.Abs(gantryOffset - reReadGantryOffset) <= 0.1d;
        var htmlBullet = new HtmlBullet(new
        {
            ApplyGantryOffset = gantryOffset,
            reReadGantryOffset
        });

        if (result) logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, htmlBullet, guid.LoggingHtml());
        else logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, htmlBullet, guid.LoggingHtml());

        if (result == false) throw new CugaException($"Failed to validate the read-back value.ApplyGantryOffset={gantryOffset}, ReadGantryOffset={reReadGantryOffsetRet.Anything}.");
    }

    public double GetGantryOffset()
    {
        var ret = calibrationStageService.GetGantryOffset();

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        return ret.Anything;
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

    public void SetStageMap(StageMapErrorDTO stageMapError)
    {
        var ret = calibrationStageService.SetStageMap(stageMapError);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetBrightFieldCenterMachinePositionValue(Point position, Guid guid)
    {
        logger.LogHtmlInformation("Set Bright Field Center", HtmlHeaderLevelEnum.Header5, guid.LoggingHtml());

        var ret = calibrationStageService.SetBrightFieldCenterMachinePositionValue(position);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        var reReadPositionRet = calibrationStageService.GetBrightFieldCenterMachinePositionValue();

        if (reReadPositionRet.IsSuccess == false)
            throw new CugaException($"Failed to validate the read-back value.{reReadPositionRet.ErrorMsg}. ApplyBFCenterMachinePosition={position}");

        var reReadPosition = reReadPositionRet.Anything;
        var result = Math.Abs(position.X - reReadPosition.X) < Constants.Tolerance
                     && Math.Abs(position.Y - reReadPosition.Y) < Constants.Tolerance;
        var htmlBullet = new HtmlBullet(new
        {
            ApplyBrightFieldCenter = position,
            ReadBrightFieldCenter = reReadPosition
        });

        if (result) logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, htmlBullet, guid.LoggingHtml());
        else logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, htmlBullet, guid.LoggingHtml());

        if (result == false) throw new CugaException($"Failed to validate the read-back value.ApplyBFCenterMachinePosition={position}, ReadBFCenterMachinePosition={reReadPositionRet.Anything}");

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

    public void SetXYGlobalScale(double xScale, double yScale, Guid guid)
    {
        logger.LogHtmlInformation("Set XY Global Scale", HtmlHeaderLevelEnum.Header5, guid.LoggingHtml());

        var ret = calibrationStageService.SetXYGlobalScale(xScale, yScale);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        var readAxisScaleRet = calibrationStageService.GetXYGlobalScale();
        if (readAxisScaleRet.IsSuccess == false)
            throw new CugaException($"Failed to validate the read-back value.{readAxisScaleRet.ErrorMsg}. ApplyXYScale=({xScale},{yScale}).");

        var readAxisScale = readAxisScaleRet.Anything;
        var result = Math.Abs(xScale - readAxisScale.X) < Constants.Tolerance && Math.Abs(yScale - readAxisScale.Y) < Constants.Tolerance;
        var htmlBullet = new HtmlBullet(new
        {
            ApplyXScale = xScale,
            ApplyYScale = yScale,
            ReadXScale = readAxisScale.X,
            ReadYScale = readAxisScale.Y
        });

        if (result) logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, htmlBullet, guid.LoggingHtml());
        else logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, htmlBullet, guid.LoggingHtml());

        if (result == false) throw new CugaException($"Failed to validate the read-back value.ApplyXYScale=({xScale},{yScale}), ReadXYScale=({readAxisScaleRet.Anything.X},{readAxisScaleRet.Anything.Y})");
    }

    public (double X, double Y) GetXYGlobalScale()
    {
        var ret = calibrationStageService.GetXYGlobalScale();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetTScale(double scaleT, Guid guid)
    {
        logger.LogHtmlInformation("Set T Scale", HtmlHeaderLevelEnum.Header5, guid.LoggingHtml());

        var ret = calibrationStageService.SetTScale(scaleT);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        var readAxisScaleRet = calibrationStageService.GetTScale();
        if (readAxisScaleRet.IsSuccess == false)
            throw new CugaException($"Failed to validate the read-back value.{readAxisScaleRet.ErrorMsg}. ApplyTScale={scaleT}.");

        var readAxisScale = readAxisScaleRet.Anything;
        var result = Math.Abs(scaleT - readAxisScale) < Constants.Tolerance;
        var htmlBullet = new HtmlBullet(new
        {
            ApplyTScale = scaleT,
            ReadTScale = readAxisScale
        });

        if (result) logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, htmlBullet, guid.LoggingHtml());
        else logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, htmlBullet, guid.LoggingHtml());

        if (result == false) throw new CugaException($"Failed to validate the read-back value.ApplyTScale={scaleT}, ReadTScale={readAxisScaleRet.Anything}");
    }

    public double GetTScale()
    {
        var ret = calibrationStageService.GetTScale();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
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
}