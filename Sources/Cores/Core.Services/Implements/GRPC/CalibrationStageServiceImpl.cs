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
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Cuga.Data.DataStruct.Stage;
using Cuga.Interface.Calibration;
using Cuga.Interface.Facade;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using Semix.GRPC.DTO;
using Semix.GRPC.DTO.Basic;

namespace Core.Services.Implements.GRPC;

[IOCAppService(ServiceType = typeof(ICalibrationStageService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationStageServiceImpl(CalibrationSetting calibrationSetting) : BaseService<ICgCalibStageService, ICgFacadeAlignService, ICgFacadeMWHSerivce>, ICalibrationStageService
{
    public SxExecuteRet<bool> Connect()
    {
        if (IsConnected) return SxExecuteRetHelper.CreateSuccess(true);

        return Invoke(() =>
        {
            var createService = CreateService();
            IsConnected = createService.IsSuccess;

            return createService;
        });
    }

    public SxExecuteRet<bool> ToggleEnableJoystick(bool enable)
    {
        var sxExecuteRet = Invoke(() => Service?.ToggleEnableJoystick(new SxParamObj<bool>(enable)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSpeed(StageSpeedEnum stageSpeedEnum, OpticsMagTypeEnum opticsMagTypeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.SetSpeed(new SxParamObj<(CgSpeedLevelType speed, CgMagTypeEnum mag)>((stageSpeedEnum.ToCgSpeedLevelType(), opticsMagTypeEnum.ToCgMagTypeEnum()))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetXSpeedValue(double speedValue)
    {
        var sxExecuteRet = Invoke(() => Service?.SetXSpeedValue(new SxParamObj<double>(speedValue)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetYSpeedValue(double speedValue)
    {
        var sxExecuteRet = Invoke(() => Service?.SetYSpeedValue(new SxParamObj<double>(speedValue)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetMachineStageTheta()
    {
        var sxExecuteRet = Invoke(() => Service?.GetMachineStageTheta());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<double>(sxExecuteRet.Msg, 0)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<bool> MoveRelativeStageTheta(double degrees)
    {
        var sxExecuteRet = Invoke(() => Service?.MoveRelativeStageTheta(new SxParamObj<double>(degrees)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetAbsoluteStageTheta(double degrees)
    {
        var sxExecuteRet = Invoke(() => Service?.SetAbsoluteStageTheta(new SxParamObj<double>(degrees)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> MoveRelativeStageXy(StageDirectionTypeEnum dir, double step)
    {
        var sxExecuteRet = Invoke(() => Service?.MoveRelativeStageXy(new SxParamObj<(CgMotionDir dir, double step)>((dir.ToCgMotionDir(), step))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<Point> GetBrightFieldStagePosition()
    {
        var sxExecuteRet = Invoke(() => Service?.GetBrightFieldStagePosition());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, Point.Origin)
            : SxExecuteRetHelper.CreateSuccess(new Point(sxExecuteRet.Anything.X, sxExecuteRet.Anything.Y));
    }

    public SxExecuteRet<bool> SetBrightFieldAbsoluteStageXy(Point point)
    {
        var sxExecuteRet = Invoke(() => Service?.SetBrightFieldAbsoluteStageXy(new SxParamObj<CgPoint>(point.ToCgPoint())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<Point> GetDarkFieldStagePosition()
    {
        var sxExecuteRet = Invoke(() => Service?.GetDarkFieldStagePosition());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, Point.Origin)
            : SxExecuteRetHelper.CreateSuccess(new Point(sxExecuteRet.Anything.X, sxExecuteRet.Anything.Y));
    }

    public SxExecuteRet<bool> SetDarkFieldAbsoluteStageXy(Point point)
    {
        var sxExecuteRet = Invoke(() => Service?.SetDarkFieldAbsoluteStageXy(new SxParamObj<CgPoint>(point.ToCgPoint())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<Point> GetMachineStagePosition()
    {
        var sxExecuteRet = Invoke(() => Service?.GetMachineStagePosition());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, Point.Origin)
            : SxExecuteRetHelper.CreateSuccess(new Point(sxExecuteRet.Anything.X, sxExecuteRet.Anything.Y));
    }

    public SxExecuteRet<bool> SetMachineAbsoluteStageXy(Point point)
    {
        var sxExecuteRet = Invoke(() => Service?.SetMachineAbsoluteStageXy(new SxParamObj<CgPoint>(point.ToCgPoint())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double XDirection, double YDirection)> GetMachineDirection()
    {
        var sxExecuteRet = Invoke(() => Service?.GetMachineDirection());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<(double XDirection, double YDirection)>(sxExecuteRet.Msg, default)
            : SxExecuteRetHelper.CreateSuccess((sxExecuteRet.Anything.XDirection, sxExecuteRet.Anything.YDirection));
    }

    public SxExecuteRet<bool> InitYAxis()
    {
        var sxExecuteRet = Invoke(() => Service?.InitYAxis());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double BeforeOffset, double AfterOffset)> OriginYOffsetCalibration()
    {
        var sxExecuteRet = Invoke(() => Service?.OriginYOffsetCalibration());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<(double BeforeOffet, double AfterOffset)>(sxExecuteRet.Msg, default)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<Point> BrightFieldToMachinePosition(Point point)
    {
        var sxExecuteRet = Invoke(() => Service?.BrightFieldToMachinePosition(new SxParamObj<CgPoint>(point.ToCgPoint())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<Point>(sxExecuteRet.Msg, default)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.ToPoint());
    }

    public SxExecuteRet<Point> DarkFieldToMachinePosition(Point point)
    {
        var sxExecuteRet = Invoke(() => Service?.DarkFieldToMachinePosition(new SxParamObj<CgPoint>(point.ToCgPoint())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<Point>(sxExecuteRet.Msg, default)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.ToPoint());
    }

    public SxExecuteRet<Point> MachineToBrightFieldPosition(Point point)
    {
        var sxExecuteRet = Invoke(() => Service?.MachineToBrightFieldPosition(new SxParamObj<CgPoint>(point.ToCgPoint())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<Point>(sxExecuteRet.Msg, default)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.ToPoint());
    }

    public SxExecuteRet<Point> MachineToDarkFieldPosition(Point point)
    {
        var sxExecuteRet = Invoke(() => Service?.MachineToDarkFieldPosition(new SxParamObj<CgPoint>(point.ToCgPoint())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<Point>(sxExecuteRet.Msg, default)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.ToPoint());
    }

    public SxExecuteRet<Point> FindWaferCenterByAutomatic(int offsetThreshold = 100)
    {
        var sxExecuteRet = Invoke(() => Service2?.FindWaferCenter(new SxParamObj<(List<SxPointD> offsets, int th)>(([], offsetThreshold))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, Point.Origin)
            : SxExecuteRetHelper.CreateSuccess(new Point(sxExecuteRet.Anything.Center.X, sxExecuteRet.Anything.Center.Y));
    }

    public SxExecuteRet<Point> FindWaferCenterByManually(out List<byte[]> bitmapMemoryBytes, Point offset, List<Point>? waferEdgeOffsets = null)
    {
        List<SxPointD> offsets = [];
        if (waferEdgeOffsets is not null && waferEdgeOffsets.Count > 0)
        {
            offsets = [.. waferEdgeOffsets.Select(t => t.ToSxPointD())];
        }

        var sxExecuteRet = Invoke(() => Service2?.FindWaferCenterWithoutVerify(new SxParamObj<(SxPointD offset, List<SxPointD> offsets)>((offset.ToSxPointD(), offsets))));
        if (sxExecuteRet.IsSuccess == false)
        {
            bitmapMemoryBytes = [];

            return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, Point.Origin);
        }

        bitmapMemoryBytes = sxExecuteRet.Anything.ImageCollection;

        return SxExecuteRetHelper.CreateSuccess(new Point(sxExecuteRet.Anything.Center.X, sxExecuteRet.Anything.Center.Y));
    }

    public SxExecuteRet<AlignmentSiteDto> MarkAlignSite1(
        AlgorithmTemplateSizeEnum algorithmTemplateSizeEnum,
        AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum)
    {
        var (isSuccess, message) = CheckAlignment(algorithmWaferTypeEnum);
        if (isSuccess == false) return SxExecuteRetHelper.CreateError(message, new AlignmentSiteDto());

        var sxExecuteRet = Invoke(() => Service2?.MarkBFAlignSite1(new SxParamObj<(SxSizeD size, ushort algo)>((
            algorithmTemplateSizeEnum.ToSize().ToSxSizeD(),
            algorithmTemplateTypeEnum.ToAlgorithmTemplateType()))));

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

        var sxExecuteRet = Invoke(() => Service2?.MarkBFAlignSite2(new SxParamObj<C2MSiteDTO>(site.AdaptTo())));

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

        var sxExecuteRet = Invoke(() => Service2?.BFAlignment(new SxParamObj<(C2MSiteDTO low1, C2MSiteDTO low2, C2MSiteDTO high1, C2MSiteDTO high2, ushort ll, ushort hl, C2MAlignTypeDTO type)>((
            lowSite1.AdaptTo(),
            lowSite2.AdaptTo(),
            highSite1.AdaptTo(),
            highSite2.AdaptTo(),
            Convert.ToUInt16(lowMicroscopeLensInformation.AdaptTo().LensCode),
            Convert.ToUInt16(highMicroscopeLensInformation.AdaptTo().LensCode),
            C2MAlignTypeDTO.Mid))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, new AlignmentResultDto())
            : SxExecuteRetHelper.CreateSuccess(new AlignmentResultDto().AdaptIn(sxExecuteRet.Anything));
    }

    public SxExecuteRet<AlignmentResultDto> AlignmentVerify(
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

        var sxExecuteRet = Invoke(() =>
            Service2?.BFAlignmentVerify(new SxParamObj<(C2MSiteDTO low1, C2MSiteDTO low2, C2MSiteDTO high1, C2MSiteDTO high2, ushort ll, ushort hl, C2MAlignTypeDTO type)>((
                lowSite1.AdaptTo(),
                lowSite2.AdaptTo(),
                highSite1.AdaptTo(),
                highSite2.AdaptTo(),
                Convert.ToUInt16(lowMicroscopeLensInformation.AdaptTo().LensCode),
                Convert.ToUInt16(highMicroscopeLensInformation.AdaptTo().LensCode),
                C2MAlignTypeDTO.Mid))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, new AlignmentResultDto())
            : SxExecuteRetHelper.CreateSuccess(new AlignmentResultDto().AdaptIn(sxExecuteRet.Anything));
    }

    public SxExecuteRet<AlignmentSiteDto> MarkAlignSite1DarkField(
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        AlgorithmTemplateSizeEnum algorithmTemplateSizeEnum,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum,
        LaserLightInformation laserLightInformation)
    {
        var (isSuccess, message) = CheckAlignment(algorithmWaferTypeEnum);
        if (isSuccess == false) return SxExecuteRetHelper.CreateError(message, new AlignmentSiteDto());

        var sxExecuteRet = Invoke(() => Service2?.MarkAlignDFSite1(new SxParamObj<(ESxLevelEnum mag, ESxLevelEnum speed, SxSizeD size)>((
            opticsMagTypeEnum.ToESxLevelEnum(),
            xStageSpeedEnum.ToESxLevelEnum(),
            algorithmTemplateSizeEnum.ToSize().ToSxSizeD()))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, new AlignmentSiteDto())
            : SxExecuteRetHelper.CreateSuccess(new AlignmentSiteDto().AdaptIn(sxExecuteRet.Anything));
    }

    public SxExecuteRet<AlignmentSiteDto> MarkAlignSite2DarkField(
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        AlignmentSiteDto site,
        AlgorithmWaferTypeEnum algorithmWaferTypeEnum)
    {
        var (isSuccess, message) = CheckAlignment(algorithmWaferTypeEnum);
        if (isSuccess == false) return SxExecuteRetHelper.CreateError(message, new AlignmentSiteDto());

        var sxExecuteRet = Invoke(() => Service2?.MarkAlignDFSite2(new SxParamObj<(ESxLevelEnum mag, ESxLevelEnum speed, C2MSiteDTO site)>((
            opticsMagTypeEnum.ToESxLevelEnum(),
            xStageSpeedEnum.ToESxLevelEnum(),
            site.AdaptTo()))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, new AlignmentSiteDto())
            : SxExecuteRetHelper.CreateSuccess(new AlignmentSiteDto().AdaptIn(sxExecuteRet.Anything));
    }

    public SxExecuteRet<AlignmentResultDto> AlignmentDarkField(
        AlignmentSiteDto brightFieldLowSite1,
        AlignmentSiteDto brightFieldLowSite2,
        AlignmentSiteDto darkFieldHighSite1,
        AlignmentSiteDto darkFieldHighSite2,
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
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

        // todo:缺光强
        var sxExecuteRet = Invoke(() => Service2?.DFAlignment(new SxParamObj<(C2MSiteDTO low1, C2MSiteDTO low2, C2MSiteDTO high1, C2MSiteDTO high2, ESxLevelEnum mag, ESxLevelEnum speed, ushort ll)>
        ((brightFieldLowSite1.AdaptTo(),
            brightFieldLowSite2.AdaptTo(),
            darkFieldHighSite1.AdaptTo(),
            darkFieldHighSite2.AdaptTo(),
            opticsMagTypeEnum.ToESxLevelEnum(),
            xStageSpeedEnum.ToESxLevelEnum(),
            Convert.ToUInt16(lowMicroscopeLensInformation.AdaptTo().LensCode)
            ))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, new AlignmentResultDto())
            : SxExecuteRetHelper.CreateSuccess(new AlignmentResultDto().AdaptIn(sxExecuteRet.Anything));
    }

    private (bool IsSuccess, string Message) CheckAlignment(AlgorithmWaferTypeEnum algorithmWaferTypeEnum)
    {
        var sxExecuteRetWaferSetting = Invoke(() => Service3?.ReadSystemWaferSetting());
        if (sxExecuteRetWaferSetting.IsSuccess == false) return (false, sxExecuteRetWaferSetting.Msg);
        return sxExecuteRetWaferSetting.Anything.ToAlgorithmWaferTypeEnum() == algorithmWaferTypeEnum
            ? (true, string.Empty)
            : (false, $"Cuga Wafer type is not match({sxExecuteRetWaferSetting.Anything.ToAlgorithmWaferTypeEnum()}), Please modify!");
    }

    public SxExecuteRet<bool> SetGantryOffset(double gantryOffset)
    {
        var sxExecuteRet = Invoke(() => Service?.SetGantryOffset(new SxParamObj<double>(gantryOffset)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableStageMap(bool enable)
    {
        var sxExecuteRet = Invoke(() => Service?.ToggleEnableStageMap(new SxParamObj<bool>(enable)));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        if (enable == false)
        {
            sxExecuteRet = Invoke(() => Service?.CloseStageMap());
            if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);
        }

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetStageMap(StageMapDto stageMapDto)
    {
        var sxExecuteRet = Invoke(() => Service?.SetStageMap(new SxParamObj<CgErrorMap>(stageMapDto.AdaptTo().ToCgErrorMap())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetBrightFieldCenterMachinePositionValue(Point position)
    {
        var sxExecuteRet = Invoke(() => Service?.SetBrightFieldCenterMachinePositionValue(new SxParamObj<CgPoint>(position.ToCgPoint())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetDarkFieldCenterMachinePositionValue(Point position)
    {
        var sxExecuteRet = Invoke(() => Service?.SetDarkFieldCenterMachinePositionValue(new SxParamObj<CgPoint>(position.ToCgPoint())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<Point> GetBrightFieldCenterMachinePositionValue()
    {
        var sxExecuteRet = Invoke(() => Service?.GetBrightFieldCenterMachinePositionValue());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, Point.Origin)
            : SxExecuteRetHelper.CreateSuccess(new Point(sxExecuteRet.Anything.X, sxExecuteRet.Anything.Y));
    }

    public SxExecuteRet<bool> SetXYGlobalScale(double xScale, double yScale)
    {
        var sxExecuteRet = Invoke(() => Service?.SetGlobalScaleErrorCoefficient(new SxParamObj<(CgAxisEnum CgAxisEnum, double xScale)>((CgAxisEnum.X, xScale))));
        if (sxExecuteRet.IsSuccess == false)
            return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        sxExecuteRet = Invoke(() => Service?.SetGlobalScaleErrorCoefficient(new SxParamObj<(CgAxisEnum CgAxisEnum, double xScale)>((CgAxisEnum.Y1, yScale))));
        if (sxExecuteRet.IsSuccess == false)
            return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetTScale(double tScale)
    {
        var sxExecuteRet = Invoke(() => Service?.SetGlobalScaleErrorCoefficient(new SxParamObj<(CgAxisEnum CgAxisEnum, double tScale)>((CgAxisEnum.T, tScale))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<Point> GetEfemLoadWaferMachineStagePosition()
    {
        var sxExecuteRet = Invoke(() => Service?.GetEfemLoadWaferMachineStagePosition());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, Point.Origin)
            : SxExecuteRetHelper.CreateSuccess(new Point(sxExecuteRet.Anything.X, sxExecuteRet.Anything.Y));
    }

    public SxExecuteRet<double> GetEfemLoadWaferMachineStageTheta()
    {
        var sxExecuteRet = Invoke(() => Service?.GetEfemLoadWaferMachineStageTheta());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<double>(sxExecuteRet.Msg, 0)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }
}