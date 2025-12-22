using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Engine.Interface;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using Semix.WcfTransfer.DTO;
using Semix.WcfTransfer.DTO.Basic;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationStageService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationStageServiceImpl(
    ICalibrationMicroscopeService calibrationMicroscopeService,
    CalibrationSetting calibrationSetting) : BaseService<ICgCalibrationService>, ICalibrationStageService
{
    public SxExecuteRet<bool> Connect()
    {
        if (IsConnected) return SxExecuteRetHelper.CreateSuccess(true);

        return Invoke(() =>
        {
            var ep = new SxWcfEndPoint("127.0.0.1", 80, CgInernalAddr.CalAddr);
            var createService = CreateService(ep);
            IsConnected = createService.IsSuccess;
            return createService;
        }, false);
    }

    public SxExecuteRet<bool> ToggleEnableJoystick(bool enable)
    {
        var sxExecuteRet = enable
            ? Invoke(() => Service!.OpenJoystick())
            : Invoke(() => Service!.CloseJoystick());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }


    public SxExecuteRet<bool> SetXSpeedValue(double speedValue)
    {
        var speedVel = speedValue * 1000;
        var accVel = speedVel * 15;
        var decVel = accVel;
        var jerkVel = accVel * 10;
        var kdecVel = jerkVel;
        var sxExecuteRet = Invoke(() => Service!.SetVelParam(ESxAxisEnum.X, speedVel, accVel, decVel, jerkVel, kdecVel));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetYSpeedValue(double speedValue)
    {
        var speedVel = speedValue * 1000;
        var accVel = speedVel * 15;
        var decVel = accVel;
        var jerkVel = accVel * 10;
        var kdecVel = jerkVel;
        var sxExecuteRet = Invoke(() => Service!.SetVelParam(ESxAxisEnum.Y1, speedVel, accVel, decVel, jerkVel, kdecVel));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetMachineStageTheta()
    {
        var sxExecuteRet = Invoke(() => Service!.ReadAutofocusData());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<double>(sxExecuteRet.Msg, 0)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.Theta);
    }

    public SxExecuteRet<bool> MoveRelativeStageTheta(double degrees)
    {
        var sxExecuteRet = Invoke(() => Service!.RotateTheta(degrees));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetAbsoluteStageTheta(double degrees)
    {
        var sxExecuteRet = Invoke(() => Service!.RotateToTheta(degrees));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> MoveRelativeStageXy(StageDirectionTypeEnum dir, double step)
    {
        var sxExecuteRet = Invoke(() => Service!.RelativeMove(dir.ToESxMotionDirection(), step));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<Point> GetBrightFieldStagePosition()
    {
        var sxExecuteRet = Invoke(() => Service!.GetBrightFieldPosition());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, Point.Origin)
            : SxExecuteRetHelper.CreateSuccess(new Point(sxExecuteRet.Anything.X, sxExecuteRet.Anything.Y));
    }

    public SxExecuteRet<bool> SetBrightFieldAbsoluteStageXy(Point point)
    {
        var sxExecuteRet = Invoke(() => Service!.ToBFWaferPositionNoMic(point.X, point.Y));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<Point> GetDarkFieldStagePosition()
    {
        var sxExecuteRet = Invoke(() => Service!.GetDarkFieldPosition());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, Point.Origin)
            : SxExecuteRetHelper.CreateSuccess(new Point(sxExecuteRet.Anything.X, sxExecuteRet.Anything.Y));
    }

    public SxExecuteRet<bool> SetDarkFieldAbsoluteStageXy(Point point)
    {
        var pointRet = DarkFieldToMachinePosition(point);
        if (pointRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(pointRet.Msg, false);
        var sxExecuteRet = SetMachineAbsoluteStageXy(pointRet.Anything);

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<Point> GetMachineStagePosition()
    {
        var sxExecuteRet = Invoke(() => Service!.GetStagePosition());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, Point.Origin)
            : SxExecuteRetHelper.CreateSuccess(new Point(sxExecuteRet.Anything.X, sxExecuteRet.Anything.Y));
    }

    public SxExecuteRet<bool> SetMachineAbsoluteStageXy(Point point)
    {
        var sxExecuteRet = Invoke(() => Service!.PTP(point.X, point.Y));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetMachineAbsoluteStageXyByFixedSpeed(Point point)
    {
        var sxExecuteRet = Invoke(() => Service!.PTPByFixSpeed(point.X, point.Y));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double XDirection, double YDirection)> GetMachineDirection()
    {
        var sxExecuteRet = Invoke(() => Service!.GetStageCoordinateSystem());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<(double XDirection, double YDirection)>(sxExecuteRet.Msg, default)
            : SxExecuteRetHelper.CreateSuccess<(double XDirection, double YDirection)>((sxExecuteRet.Anything.XCSYS, sxExecuteRet.Anything.YCSYS));
    }

    public SxExecuteRet<bool> InitYAxis()
    {
        var sxExecuteRet = Invoke(() => Service!.InitYAxis());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double BeforeOffset, double AfterOffset)> OriginYOffsetCalibration()
    {
        var sxExecuteRet = Invoke(() => Service!.ExecYOffsetCalib());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<(double BeforeOffet, double AfterOffset)>(sxExecuteRet.Msg, default)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<Point> BrightFieldToMachinePosition(Point point)
    {
        var sxExecuteRet = Invoke(() => Service!.BFToStage(point.ToCgPoint()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<Point>(sxExecuteRet.Msg, default)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.ToPoint());
    }

    public SxExecuteRet<Point> DarkFieldToMachinePosition(Point point)
    {
        var sxExecuteRet = Invoke(() => Service!.DFToStage(point.ToCgPoint()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<Point>(sxExecuteRet.Msg, default)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.ToPoint());
    }

    public SxExecuteRet<Point> MachineToBrightFieldPosition(Point point)
    {
        var sxExecuteRet = Invoke(() => Service!.StageToBF(point.ToCgPoint()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<Point>(sxExecuteRet.Msg, default)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.ToPoint());
    }

    public SxExecuteRet<Point> MachineToDarkFieldPosition(Point point)
    {
        var sxExecuteRet = Invoke(() => Service!.StageToDF(point.ToCgPoint()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<Point>(sxExecuteRet.Msg, default)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.ToPoint());
    }

    public SxExecuteRet<Point> FindWaferCenterByAutomatic(int offsetThreshold = 100)
    {
        var sxExecuteRet = Invoke(() => Service!.FindWaferCenter(out _, null, offsetThreshold));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, Point.Origin)
            : SxExecuteRetHelper.CreateSuccess(new Point(sxExecuteRet.Anything.X, sxExecuteRet.Anything.Y));
    }

    public SxExecuteRet<Point> FindWaferCenterByManually(out List<byte[]> bitmapMemoryBytes, Point offset, List<Point>? waferEdgeOffsets = null)
    {
        List<System.Drawing.Point> offsets = [];
        if (waferEdgeOffsets is not null && waferEdgeOffsets.Count > 0)
        {
            offsets = [.. waferEdgeOffsets.Select(t => t.ToSystemDrawingPoint())];
        }

        List<byte[]> tempBitmaps = [];

        var sxExecuteRet = Invoke(() =>
        {
            var waferCenter = Service!.FindWaferCenterWithoutVerify(out var bitmaps, offset.ToSxPointD(), offsets);
            tempBitmaps = bitmaps ?? [];

            return waferCenter;
        });
        bitmapMemoryBytes = tempBitmaps;

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, Point.Origin)
            : SxExecuteRetHelper.CreateSuccess(new Point(sxExecuteRet.Anything.X, sxExecuteRet.Anything.Y));
    }

    public SxExecuteRet<AlignmentSiteDto> MarkAlignSite1(
        AlgorithmTemplateSizeEnum algorithmTemplateSizeEnum,
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum)
    {
        var (isSuccess, message) = CheckAlignment(algorithmWaferTypeEnum);
        if (isSuccess == false) return SxExecuteRetHelper.CreateError(message, new AlignmentSiteDto());

        var sxExecuteRet = Invoke(() => Service!.MarkAlignSite1(
            algorithmTemplateSizeEnum.ToSize().ToSystemDrawingSize(),
            algorithmTemplateTypeEnum.ToAlgorithmTemplateType()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, new AlignmentSiteDto())
            : SxExecuteRetHelper.CreateSuccess(new AlignmentSiteDto().AdaptIn(sxExecuteRet.Anything));
    }

    public SxExecuteRet<AlignmentSiteDto> MarkAlignSite2(
        AlignmentSiteDto site,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum)
    {
        var (isSuccess, message) = CheckAlignment(algorithmWaferTypeEnum);
        if (isSuccess == false) return SxExecuteRetHelper.CreateError(message, new AlignmentSiteDto());

        site.UpdateTemplateMatchScoreThreshold(calibrationSetting);
        var sxExecuteRet = Invoke(() => Service!.MarkAlignSite2(site.AdaptTo()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, new AlignmentSiteDto())
            : SxExecuteRetHelper.CreateSuccess(new AlignmentSiteDto().AdaptIn(sxExecuteRet.Anything));
    }

    public SxExecuteRet<AlignmentResultDto> Alignment(
        AlignmentSiteDto lowSite1,
        AlignmentSiteDto lowSite2,
        AlignmentSiteDto highSite1,
        AlignmentSiteDto highSite2,
        MicroscopeLensInformation lowMicroscopeLensInformation,
        MicroscopeLensInformation highMicroscopeLensInformation,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum)
    {
        var (isSuccess, message) = CheckAlignment(algorithmWaferTypeEnum);
        if (isSuccess == false) return SxExecuteRetHelper.CreateError(message, new AlignmentResultDto());

        lowSite1.UpdateTemplateMatchScoreThreshold(calibrationSetting);
        lowSite2.UpdateTemplateMatchScoreThreshold(calibrationSetting);
        highSite1.UpdateTemplateMatchScoreThreshold(calibrationSetting);
        highSite2.UpdateTemplateMatchScoreThreshold(calibrationSetting);

        var sxExecuteRet = Invoke(() => Service!.Alignment(
            lowSite1.AdaptTo(),
            lowSite2.AdaptTo(),
            highSite1.AdaptTo(),
            highSite2.AdaptTo(),
            Convert.ToUInt16(lowMicroscopeLensInformation.AdaptTo().LensCode),
            Convert.ToUInt16(highMicroscopeLensInformation.AdaptTo().LensCode),
            type: C2MAlignType.Mid));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, new AlignmentResultDto())
            : SxExecuteRetHelper.CreateSuccess(new AlignmentResultDto().AdaptIn(sxExecuteRet.Anything));
    }

    public SxExecuteRet<AlignmentResultDto> AlignmentVerify(AlignmentSiteDto lowSite1,
        AlignmentSiteDto lowSite2,
        AlignmentSiteDto highSite1,
        AlignmentSiteDto highSite2,
        MicroscopeLensInformation lowMicroscopeLensInformation,
        MicroscopeLensInformation highMicroscopeLensInformation,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum)
    {
        var (isSuccess, message) = CheckAlignment(algorithmWaferTypeEnum);
        if (isSuccess == false) return SxExecuteRetHelper.CreateError(message, new AlignmentResultDto());

        lowSite1.UpdateTemplateMatchScoreThreshold(calibrationSetting);
        lowSite2.UpdateTemplateMatchScoreThreshold(calibrationSetting);
        highSite1.UpdateTemplateMatchScoreThreshold(calibrationSetting);
        highSite2.UpdateTemplateMatchScoreThreshold(calibrationSetting);

        var sxExecuteRet = Invoke(() => Service!.AlignmentVerify(
            lowSite1.AdaptTo(),
            lowSite2.AdaptTo(),
            highSite1.AdaptTo(),
            highSite2.AdaptTo(),
            Convert.ToUInt16(lowMicroscopeLensInformation.AdaptTo().LensCode),
            Convert.ToUInt16(highMicroscopeLensInformation.AdaptTo().LensCode),
            type: C2MAlignType.Mid));

        return sxExecuteRet.Anything is null
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, new AlignmentResultDto())
            : SxExecuteRetHelper.CreateSuccess(new AlignmentResultDto().AdaptIn(sxExecuteRet.Anything));
    }

    public SxExecuteRet<AlignmentSiteDto> MarkAlignSite1DarkField(
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        ProductivityInformation productivityInformation,
        AlgorithmTemplateSizeEnum algorithmTemplateSizeEnum,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum,
        LaserLightInformation laserLightInformation)
    {
        var (isSuccess, message) = CheckAlignment(algorithmWaferTypeEnum);
        if (isSuccess == false) return SxExecuteRetHelper.CreateError(message, new AlignmentSiteDto());

        var sxExecuteRet = Invoke(() => Service!.MarkAlignDFSite1(
            ((SxMAGEnum)productivityInformation.OpticsMagType).ToESxLevelEnum(),
            ((SxSpeedEnum)productivityInformation.StageSpeedType).ToESxLevelEnum(),
            algorithmTemplateSizeEnum.ToSize().ToSystemDrawingSize(),
            laserLightInformation.Level));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, new AlignmentSiteDto())
            : SxExecuteRetHelper.CreateSuccess(new AlignmentSiteDto().AdaptIn(sxExecuteRet.Anything));
    }

    public SxExecuteRet<AlignmentSiteDto> MarkAlignSite2DarkField(
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        ProductivityInformation productivityInformation,
        AlignmentSiteDto site,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum)
    {
        var (isSuccess, message) = CheckAlignment(algorithmWaferTypeEnum);
        if (isSuccess == false) return SxExecuteRetHelper.CreateError(message, new AlignmentSiteDto());

        var sxExecuteRet = Invoke(() => Service!.MarkAlignDFSite2(
            ((SxMAGEnum)productivityInformation.OpticsMagType).ToESxLevelEnum(),
            ((SxSpeedEnum)productivityInformation.StageSpeedType).ToESxLevelEnum(),
            site.AdaptTo()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, new AlignmentSiteDto())
            : SxExecuteRetHelper.CreateSuccess(new AlignmentSiteDto().AdaptIn(sxExecuteRet.Anything));
    }

    public SxExecuteRet<AlignmentResultDto> AlignmentDarkField(
        AlignmentSiteDto brightFieldLowSite1,
        AlignmentSiteDto brightFieldLowSite2,
        AlignmentSiteDto darkFieldHighSite1,
        AlignmentSiteDto darkFieldHighSite2,
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        ProductivityInformation productivityInformation,
        MicroscopeLensInformation lowMicroscopeLensInformation,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum,
        LaserLightInformation laserLightInformation)
    {
        var (isSuccess, message) = CheckAlignment(algorithmWaferTypeEnum);
        if (isSuccess == false) return SxExecuteRetHelper.CreateError(message, new AlignmentResultDto());

        brightFieldLowSite1.UpdateTemplateMatchScoreThreshold(calibrationSetting);
        brightFieldLowSite2.UpdateTemplateMatchScoreThreshold(calibrationSetting);
        darkFieldHighSite1.UpdateTemplateMatchScoreThreshold(calibrationSetting);
        darkFieldHighSite2.UpdateTemplateMatchScoreThreshold(calibrationSetting);

        var sxExecuteRet = Invoke(() => Service!.DFAlignment(
            brightFieldLowSite1.AdaptTo(),
            brightFieldLowSite2.AdaptTo(),
            darkFieldHighSite1.AdaptTo(),
            darkFieldHighSite2.AdaptTo(),
            ((SxMAGEnum)productivityInformation.OpticsMagType).ToESxLevelEnum(),
            ((SxSpeedEnum)productivityInformation.StageSpeedType).ToESxLevelEnum(),
            Convert.ToUInt16(lowMicroscopeLensInformation.AdaptTo().LensCode),
            laserLightInformation.Level));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, new AlignmentResultDto())
            : SxExecuteRetHelper.CreateSuccess(new AlignmentResultDto().AdaptIn(sxExecuteRet.Anything));
    }

    private (bool IsSuccess, string Message) CheckAlignment(AlgorithmWaferTypeEnum algorithmWaferTypeEnum)
    {
        var sxExecuteRetWaferSetting = Invoke(() => Service!.ReadSystemWaferSetting());
        if (sxExecuteRetWaferSetting.IsSuccess == false) return (false, sxExecuteRetWaferSetting.Msg);
        return sxExecuteRetWaferSetting.Anything.ToAlgorithmWaferTypeEnum() == algorithmWaferTypeEnum
            ? (true, string.Empty)
            : (false, $"Cuga Wafer type is not match({sxExecuteRetWaferSetting.Anything.ToAlgorithmWaferTypeEnum()}), Please modify!");
    }

    public SxExecuteRet<bool> SetGantryOffset(double gantryOffset)
    {
        var sxExecuteRet = Invoke(() => Service!.SetGantry(gantryOffset));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableStageMap(bool enable)
    {
        var sxExecuteRet = Invoke(() => Service!.ErrorMapSwich(enable));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        if (enable == false)
        {
            sxExecuteRet = Invoke(() => Service!.CloseErrorMap());
            if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);
        }

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetStageMap(StageMapDto stageMapDto)
    {
        var sxExecuteRet = Invoke(() => Service!.ExecErrorMap(stageMapDto.AdaptTo().ToCgErrorMap()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetBrightFieldCenterMachinePositionValue(Point position)
    {
        var sxExecuteRet = Invoke(() => Service!.UpdateChuckCenter(position.ToCgPoint()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetDarkFieldCenterMachinePositionValue(Point position)
    {
        var sxExecuteRet = Invoke(() => Service!.UpdataDFCenter(position.ToCgPoint()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<Point> GetBrightFieldCenterMachinePositionValue()
    {
        var sxExecuteRet = Invoke(() => Service!.GetStagePoint2BFCenter());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, Point.Origin)
            : SxExecuteRetHelper.CreateSuccess(new Point(sxExecuteRet.Anything.X, sxExecuteRet.Anything.Y));
    }

    public SxExecuteRet<bool> SetXYGlobalScale(double xScale, double yScale)
    {
        var sxExecuteRet = Invoke(() => Service!.UpdateScale(ESxAxisEnum.X, xScale));
        if (sxExecuteRet.IsSuccess == false)
            return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        sxExecuteRet = Invoke(() => Service!.UpdateScale(ESxAxisEnum.Y1, yScale));
        if (sxExecuteRet.IsSuccess == false)
            return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetTScale(double tScale)
    {
        var sxExecuteRet = Invoke(() => Service!.UpdateScale(ESxAxisEnum.T, tScale));

        return sxExecuteRet.IsSuccess
            ? SxExecuteRetHelper.CreateSuccess(true)
            : SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);
    }

    public SxExecuteRet<Point> GetEfemLoadWaferMachineStagePosition()
    {
        var sxExecuteRet = Invoke(() => Service!.GetStagePoint2EfemLoadWafer());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, Point.Origin)
            : SxExecuteRetHelper.CreateSuccess(new Point(sxExecuteRet.Anything.X, sxExecuteRet.Anything.Y));
    }

    public SxExecuteRet<double> GetEfemLoadWaferMachineStageTheta()
    {
        var sxExecuteRet = Invoke(() => Service!.GetChuckAngle2EfemLoadWafer());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<double>(sxExecuteRet.Msg, 0)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }
}