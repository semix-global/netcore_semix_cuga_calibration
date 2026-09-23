using Core.Models.Extensions;
using Core.Wcf.Models.Fourier;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Fourier.CenterChannelFlexibleAperture;

[CacheVersion("2.0.0")]
public sealed partial class FourierCenterChannelFlexibleApertureDTO : CalibrationDTOBase<FourierCenterChannelFlexibleApertureDTO>, IAdaptTo<CalibrationPupilCenterChannelFlexibleAperture>, IDisposable
{
    public FourierCenterChannelFlexibleApertureDTOApertureItem XApertureItem { get; private init; } = new(CenterChannelMotionKindEnum.X);

    public FourierCenterChannelFlexibleApertureDTORodItem[] XItems { get; private init; } =
    [
        new(CenterChannelMotionKindEnum.X, 120d) { Step0MotorAbsoluteValue = -6d, Step1MotorAbsoluteValue = -8d },
        new(CenterChannelMotionKindEnum.X, 180d) { Step0MotorAbsoluteValue = -8d, Step1MotorAbsoluteValue = -10d },
        new(CenterChannelMotionKindEnum.X, 240d) { Step0MotorAbsoluteValue = -8d, Step1MotorAbsoluteValue = -10d },
        new(CenterChannelMotionKindEnum.X, 300d) { Step0MotorAbsoluteValue = -8d, Step1MotorAbsoluteValue = -10d }
    ];

    public FourierCenterChannelFlexibleApertureDTORodItem PushItem { get; private init; } = new(CenterChannelMotionKindEnum.Push, 0) { Step0MotorAbsoluteValue = 42d, Step1MotorAbsoluteValue = 43d };

    public FourierCenterChannelFlexibleApertureDTOApertureItem YApertureItem { get; private init; } = new(CenterChannelMotionKindEnum.Y);

    public FourierCenterChannelFlexibleApertureDTORodItem[] YItems { get; private init; } =
    [
        new(CenterChannelMotionKindEnum.Y, 120d) { Step0MotorAbsoluteValue = 2d, Step1MotorAbsoluteValue = 4d },
        new(CenterChannelMotionKindEnum.Y, 180d) { Step0MotorAbsoluteValue = 4d, Step1MotorAbsoluteValue = 6d },
        new(CenterChannelMotionKindEnum.Y, 240d) { Step0MotorAbsoluteValue = 6d, Step1MotorAbsoluteValue = 8d },
        new(CenterChannelMotionKindEnum.Y, 300d) { Step0MotorAbsoluteValue = 8d, Step1MotorAbsoluteValue = 10d }
    ];

#pragma warning disable IDISP003

    public override FourierCenterChannelFlexibleApertureDTO Clone() => new()
    {
        XApertureItem = XApertureItem.Clone(),
        XItems = [.. XItems.Select(t => t.Clone())],
        PushItem = PushItem.Clone(),
        YApertureItem = YApertureItem.Clone(),
        YItems = [.. YItems.Select(t => t.Clone())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

#pragma warning restore IDISP003

    public CalibrationPupilCenterChannelFlexibleAperture AdaptTo() => new()
    {
        CgFFBoxTurnXAngleCh3 = [.. XItems.Select(t => t.AngleMotorAbsoluteValue)],
        CgFFBoxTurnXWidthCh3 = [.. XItems.Select(t => t.Step0ROI.ImageROI.Width)],
        CgFFBoxTurnXMotorRelationCh3 = [.. XItems.Select(t => t.PixelSize)],
        CgFFBoxTurnXMotorPositionCh3 = [.. XItems.Select(t => t.Step0MotorAbsoluteValue)],
        CgFFBoxTurnXRectPositionCh3 = [.. XItems.Select(t => t.Step0ROI.ImageROI.ToRectD())],
        CgFFBoxTurnXLightHoleCircleCenterCh3 = XApertureItem.Aperture.ImageCircle.Center.ToCgPoint(),
        CgFFBoxTurnXLightHoleCircleRadiusCh3 = XApertureItem.Aperture.ImageCircle.Radius,
        CgFFBoxTurnYAngleCh3 = [.. YItems.Select(t => t.AngleMotorAbsoluteValue)],
        CgFFBoxTurnYWidthCh3 = [.. YItems.Select(t => t.Step0ROI.ImageROI.Height)],
        CgFFBoxTurnYMotorRelationCh3 = [.. YItems.Select(t => t.PixelSize)],
        CgFFBoxTurnYMotorPositionCh3 = [.. YItems.Select(t => t.Step0MotorAbsoluteValue)],
        CgFFBoxTurnYRectPositionCh3 = [.. YItems.Select(t => t.Step0ROI.ImageROI.ToRectD())],
        CgFFBoxTurnYLightHoleCircleCenterCh3 = YApertureItem.Aperture.ImageCircle.Center.ToCgPoint(),
        CgFFBoxTurnYLightHoleCircleRadiusCh3 = YApertureItem.Aperture.ImageCircle.Radius,
        CgFFBoxPushXWidthCh3 = PushItem.Step0ROI.ImageROI.Width,
        CgFFBoxPushXMotorRelationCh3 = PushItem.PixelSize,
        CgFFBoxPushXMotorPositionCh3 = PushItem.Step0MotorAbsoluteValue,
        CgFFBoxPushXRectPositionCh3 = PushItem.Step0ROI.ImageROI.ToRectD(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    public void Dispose()
    {
        XApertureItem.Dispose();
        foreach (var xItem in XItems) xItem.Dispose();
        PushItem.Dispose();

        YApertureItem.Dispose();
        foreach (var yItem in YItems) yItem.Dispose();
    }
}