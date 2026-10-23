using Core.Models.Extensions;
using Core.Wcf.Models.Fourier;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Fourier.V0CenterChannelFlexibleAperture;

[CacheVersion("2.0.0")]
public sealed class V0FourierCenterChannelFlexibleApertureDTO : CalibrationDTOBase<V0FourierCenterChannelFlexibleApertureDTO>, IAdaptTo<CalibrationPupilCenterChannelFlexibleAperture>, IDisposable
{
    public V0FourierCenterChannelFlexibleApertureDTOItem XApertureItem { get; private init; } = new(CenterChannelMotionKindEnum.XAperture, 60d) { Step0MotorAbsoluteValue = 10d, Step1MotorAbsoluteValue = 15d };

    public V0FourierCenterChannelFlexibleApertureDTOItem[] XRodItems { get; private init; } =
    [
        new(CenterChannelMotionKindEnum.XRod, 120d) { Step0MotorAbsoluteValue = -2d, Step1MotorAbsoluteValue = -7d },
        new(CenterChannelMotionKindEnum.XRod, 180d) { Step0MotorAbsoluteValue = 0d, Step1MotorAbsoluteValue = -5d },
        new(CenterChannelMotionKindEnum.XRod, 240d) { Step0MotorAbsoluteValue = 2d, Step1MotorAbsoluteValue = -3d },
        new(CenterChannelMotionKindEnum.XRod, 300d) { Step0MotorAbsoluteValue = 2d, Step1MotorAbsoluteValue = -3d }
    ];

    public V0FourierCenterChannelFlexibleApertureDTOItem XPushItem { get; private init; } = new(CenterChannelMotionKindEnum.XPush, 0d) { Step0MotorAbsoluteValue = 34d, Step1MotorAbsoluteValue = 39d };

    public V0FourierCenterChannelFlexibleApertureDTOItem YApertureItem { get; private init; } = new(CenterChannelMotionKindEnum.YAperture, 60d) { Step0MotorAbsoluteValue = 10d, Step1MotorAbsoluteValue = 15d };

    public V0FourierCenterChannelFlexibleApertureDTOItem[] YRodItems { get; private init; } =
    [
        new(CenterChannelMotionKindEnum.YRod, 120d) { Step0MotorAbsoluteValue = 0d, Step1MotorAbsoluteValue = 5d },
        new(CenterChannelMotionKindEnum.YRod, 180d) { Step0MotorAbsoluteValue = 0d, Step1MotorAbsoluteValue = 5d },
        new(CenterChannelMotionKindEnum.YRod, 240d) { Step0MotorAbsoluteValue = 0d, Step1MotorAbsoluteValue = 5d },
        new(CenterChannelMotionKindEnum.YRod, 300d) { Step0MotorAbsoluteValue = 0d, Step1MotorAbsoluteValue = 5d }
    ];

#pragma warning disable IDISP003

    public override V0FourierCenterChannelFlexibleApertureDTO Clone() => new()
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
        CgFFBoxTurnYLightHoleCircleRadiusCh3 = YApertureItem.Step0ROI.ImageROI.Height / 2d,
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