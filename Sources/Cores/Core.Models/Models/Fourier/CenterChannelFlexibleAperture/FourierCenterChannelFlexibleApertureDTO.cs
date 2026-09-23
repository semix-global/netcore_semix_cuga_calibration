using Core.Models.Extensions;
using Core.Wcf.Models.Fourier;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Fourier.CenterChannelFlexibleAperture;

[CacheVersion("2.0.0")]
public sealed class FourierCenterChannelFlexibleApertureDTO : CalibrationDTOBase<FourierCenterChannelFlexibleApertureDTO>, IAdaptTo<CalibrationPupilCenterChannelFlexibleAperture>, IDisposable
{
    public FourierCenterChannelFlexibleApertureDTOItem XApertureItem { get; private init; } = new(CenterChannelMotionKindEnum.XAperture, 60d);

    public FourierCenterChannelFlexibleApertureDTOItem[] XRodItems { get; private init; } =
    [
        new(CenterChannelMotionKindEnum.XRod, 120d) { Step0MotorAbsoluteValue = -6d, Step1MotorAbsoluteValue = -8d },
        new(CenterChannelMotionKindEnum.XRod, 180d) { Step0MotorAbsoluteValue = -8d, Step1MotorAbsoluteValue = -10d },
        new(CenterChannelMotionKindEnum.XRod, 240d) { Step0MotorAbsoluteValue = -8d, Step1MotorAbsoluteValue = -10d },
        new(CenterChannelMotionKindEnum.XRod, 300d) { Step0MotorAbsoluteValue = -8d, Step1MotorAbsoluteValue = -10d }
    ];

    public FourierCenterChannelFlexibleApertureDTOItem XPushItem { get; private init; } = new(CenterChannelMotionKindEnum.XPush, 0) { Step0MotorAbsoluteValue = 42d, Step1MotorAbsoluteValue = 43d };

    public FourierCenterChannelFlexibleApertureDTOItem YApertureItem { get; private init; } = new(CenterChannelMotionKindEnum.YAperture, 60d);

    public FourierCenterChannelFlexibleApertureDTOItem[] YRodItems { get; private init; } =
    [
        new(CenterChannelMotionKindEnum.YRod, 120d) { Step0MotorAbsoluteValue = 2d, Step1MotorAbsoluteValue = 4d },
        new(CenterChannelMotionKindEnum.YRod, 180d) { Step0MotorAbsoluteValue = 4d, Step1MotorAbsoluteValue = 6d },
        new(CenterChannelMotionKindEnum.YRod, 240d) { Step0MotorAbsoluteValue = 6d, Step1MotorAbsoluteValue = 8d },
        new(CenterChannelMotionKindEnum.YRod, 300d) { Step0MotorAbsoluteValue = 8d, Step1MotorAbsoluteValue = 10d }
    ];

#pragma warning disable IDISP003

    public override FourierCenterChannelFlexibleApertureDTO Clone() => new()
    {
        XApertureItem = XApertureItem.Clone(),
        XRodItems = [.. XRodItems.Select(t => t.Clone())],
        XPushItem = XPushItem.Clone(),
        YApertureItem = YApertureItem.Clone(),
        YRodItems = [.. YRodItems.Select(t => t.Clone())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

#pragma warning restore IDISP003

    public CalibrationPupilCenterChannelFlexibleAperture AdaptTo() => new()
    {
        CgFFBoxTurnXAngleCh3 = [.. XRodItems.Select(t => t.AngleMotorAbsoluteValue)],
        CgFFBoxTurnXWidthCh3 = [.. XRodItems.Select(t => t.Step0ROI.ImageROI.Width)],
        CgFFBoxTurnXMotorRelationCh3 = [.. XRodItems.Select(t => t.PixelSize)],
        CgFFBoxTurnXMotorPositionCh3 = [.. XRodItems.Select(t => t.Step0MotorAbsoluteValue)],
        CgFFBoxTurnXRectPositionCh3 = [.. XRodItems.Select(t => t.Step0ROI.ImageROI.ToRectD())],
        CgFFBoxTurnXLightHoleCircleAngleCh3 = XApertureItem.AngleMotorAbsoluteValue,
        CgFFBoxTurnXLightHoleCircleRelationCh3 = XApertureItem.PixelSize,
        CgFFBoxTurnXLightHoleCirclePositionCh3 = XApertureItem.Step0MotorAbsoluteValue,
        CgFFBoxTurnXLightHoleCircleCenterCh3 = XApertureItem.Step0ROI.ImageROI.Center.ToCgPoint(),
        CgFFBoxTurnXLightHoleCircleRadiusCh3 = XApertureItem.Step0ROI.ImageROI.Width / 2d,
        CgFFBoxTurnYAngleCh3 = [.. YRodItems.Select(t => t.AngleMotorAbsoluteValue)],
        CgFFBoxTurnYWidthCh3 = [.. YRodItems.Select(t => t.Step0ROI.ImageROI.Height)],
        CgFFBoxTurnYMotorRelationCh3 = [.. YRodItems.Select(t => t.PixelSize)],
        CgFFBoxTurnYMotorPositionCh3 = [.. YRodItems.Select(t => t.Step0MotorAbsoluteValue)],
        CgFFBoxTurnYRectPositionCh3 = [.. YRodItems.Select(t => t.Step0ROI.ImageROI.ToRectD())],
        CgFFBoxTurnYLightHoleCircleAngleCh3 = YApertureItem.AngleMotorAbsoluteValue,
        CgFFBoxTurnYLightHoleCircleRelationCh3 = YApertureItem.PixelSize,
        CgFFBoxTurnYLightHoleCirclePositionCh3 = YApertureItem.Step0MotorAbsoluteValue,
        CgFFBoxTurnYLightHoleCircleCenterCh3 = YApertureItem.Step0ROI.ImageROI.Center.ToCgPoint(),
        CgFFBoxTurnYLightHoleCircleRadiusCh3 = YApertureItem.Step0ROI.ImageROI.Width / 2d,
        CgFFBoxPushXWidthCh3 = XPushItem.Step0ROI.ImageROI.Width,
        CgFFBoxPushXMotorRelationCh3 = XPushItem.PixelSize,
        CgFFBoxPushXMotorPositionCh3 = XPushItem.Step0MotorAbsoluteValue,
        CgFFBoxPushXRectPositionCh3 = XPushItem.Step0ROI.ImageROI.ToRectD(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    public void Dispose()
    {
        XApertureItem.Dispose();
        foreach (var xItem in XRodItems) xItem.Dispose();
        XPushItem.Dispose();

        YApertureItem.Dispose();
        foreach (var yItem in YRodItems) yItem.Dispose();
    }
}