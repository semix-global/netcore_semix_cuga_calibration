using Core.Models.Extensions;
using Core.Wcf.Models.Fourier;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Fourier.CenterChannelFlexibleAperture;

[CacheVersion("2.0.0")]
public sealed partial class FourierCenterChannelFlexibleApertureDTO : CalibrationDTOBase<FourierCenterChannelFlexibleApertureDTO>, IAdaptTo<CalibrationPupilCenterChannelFlexibleAperture>, IDisposable
{
    public FourierCenterChannelFlexibleApertureLightHoleItem XLightHoleItem { get; private init; } = new(CenterChannelMotionKindEnum.X);

    public FourierCenterChannelFlexibleApertureMotionItem[] XItems { get; private init; } =
    [
        new(CenterChannelMotionKindEnum.X, 120d) { Step0MotorAbsoluteValue = -6d, Step1MotorAbsoluteValue = -8d },
        new(CenterChannelMotionKindEnum.X, 180d) { Step0MotorAbsoluteValue = -8d, Step1MotorAbsoluteValue = -10d },
        new(CenterChannelMotionKindEnum.X, 240d) { Step0MotorAbsoluteValue = -8d, Step1MotorAbsoluteValue = -10d },
        new(CenterChannelMotionKindEnum.X, 300d) { Step0MotorAbsoluteValue = -8d, Step1MotorAbsoluteValue = -10d }
    ];

    public FourierCenterChannelFlexibleApertureMotionItem PushItem { get; private init; } = new(CenterChannelMotionKindEnum.Push, 0) { Step0MotorAbsoluteValue = 42d, Step1MotorAbsoluteValue = 43d };

    public FourierCenterChannelFlexibleApertureLightHoleItem YLightHoleItem { get; private init; } = new(CenterChannelMotionKindEnum.Y);

    public FourierCenterChannelFlexibleApertureMotionItem[] YItems { get; private init; } =
    [
        new(CenterChannelMotionKindEnum.Y, 120d) { Step0MotorAbsoluteValue = 2d, Step1MotorAbsoluteValue = 4d },
        new(CenterChannelMotionKindEnum.Y, 180d) { Step0MotorAbsoluteValue = 4d, Step1MotorAbsoluteValue = 6d },
        new(CenterChannelMotionKindEnum.Y, 240d) { Step0MotorAbsoluteValue = 6d, Step1MotorAbsoluteValue = 8d },
        new(CenterChannelMotionKindEnum.Y, 300d) { Step0MotorAbsoluteValue = 8d, Step1MotorAbsoluteValue = 10d }
    ];

#pragma warning disable IDISP003

    public override FourierCenterChannelFlexibleApertureDTO Clone() => new()
    {
        XLightHoleItem = XLightHoleItem.Clone(),
        XItems = [.. XItems.Select(t => t.Clone())],
        PushItem = PushItem.Clone(),
        YLightHoleItem = YLightHoleItem.Clone(),
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
        CgFFBoxTurnXWidthCh3 = [.. XItems.Select(t => t.Step0FindItem.ImageROI.Width)],
        CgFFBoxTurnXMotorRelationCh3 = [.. XItems.Select(t => t.PixelSize)],
        CgFFBoxTurnXMotorPositionCh3 = [.. XItems.Select(t => t.Step0MotorAbsoluteValue)],
        CgFFBoxTurnXRectPositionCh3 = [.. XItems.Select(t => t.Step0FindItem.ImageROI.ToRectD())],
        CgFFBoxTurnXLightHoleCircleCenterCh3 = XLightHoleItem.Circle.Center.ToCgPoint(),
        CgFFBoxTurnXLightHoleCircleRadiusCh3 = XLightHoleItem.Circle.Radius,
        CgFFBoxTurnYAngleCh3 = [.. YItems.Select(t => t.AngleMotorAbsoluteValue)],
        CgFFBoxTurnYWidthCh3 = [.. YItems.Select(t => t.Step0FindItem.ImageROI.Height)],
        CgFFBoxTurnYMotorRelationCh3 = [.. YItems.Select(t => t.PixelSize)],
        CgFFBoxTurnYMotorPositionCh3 = [.. YItems.Select(t => t.Step0MotorAbsoluteValue)],
        CgFFBoxTurnYRectPositionCh3 = [.. YItems.Select(t => t.Step0FindItem.ImageROI.ToRectD())],
        CgFFBoxTurnYLightHoleCircleCenterCh3 = YLightHoleItem.Circle.Center.ToCgPoint(),
        CgFFBoxTurnYLightHoleCircleRadiusCh3 = YLightHoleItem.Circle.Radius,
        CgFFBoxPushXWidthCh3 = PushItem.Step0FindItem.ImageROI.Width,
        CgFFBoxPushXMotorRelationCh3 = PushItem.PixelSize,
        CgFFBoxPushXMotorPositionCh3 = PushItem.Step0MotorAbsoluteValue,
        CgFFBoxPushXRectPositionCh3 = PushItem.Step0FindItem.ImageROI.ToRectD(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    public void Dispose()
    {
        XLightHoleItem.Dispose();
        foreach (var xItem in XItems) xItem.Dispose();
        PushItem.Dispose();

        YLightHoleItem.Dispose();
        foreach (var yItem in YItems) yItem.Dispose();
    }
}