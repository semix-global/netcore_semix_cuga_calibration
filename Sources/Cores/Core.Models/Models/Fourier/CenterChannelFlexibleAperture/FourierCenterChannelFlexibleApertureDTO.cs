using Core.Models.Extensions;
using Core.Wcf.Models.Fourier;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Fourier.CenterChannelFlexibleAperture;

[CacheVersion("2.0.0")]
public sealed class FourierCenterChannelFlexibleApertureDTO : CalibrationDTOBase<FourierCenterChannelFlexibleApertureDTO>, IAdaptTo<CalibrationPupilCenterChannelFlexibleApertureV1>, IDisposable
{
    public const double AngleStep = 360d / 11d;

    public FourierCenterChannelFlexibleApertureDTOItem XApertureItem { get; private init; } = new(CenterChannelMotionKindEnum.XAperture, AngleStep * 9d) { Step0MotorAbsoluteValue = 5d, Step1MotorAbsoluteValue = 10d };

    public FourierCenterChannelFlexibleApertureDTOItem XCalibrationPatternItem { get; private init; } = new(CenterChannelMotionKindEnum.XCalibrationPattern, AngleStep * 8d) { Step0MotorAbsoluteValue = -13d, Step1MotorAbsoluteValue = -8d };

    public FourierCenterChannelFlexibleApertureDTOItem[] XRodItems { get; private init; } =
    [
        new(CenterChannelMotionKindEnum.XRod, AngleStep * 1d) { Step0MotorAbsoluteValue = 2d, Step1MotorAbsoluteValue = -3d },
        new(CenterChannelMotionKindEnum.XRod, AngleStep * 2d) { Step0MotorAbsoluteValue = 2d, Step1MotorAbsoluteValue = -3d },
        new(CenterChannelMotionKindEnum.XRod, AngleStep * 3d) { Step0MotorAbsoluteValue = 2d, Step1MotorAbsoluteValue = -3d },
        new(CenterChannelMotionKindEnum.XRod, AngleStep * 4d) { Step0MotorAbsoluteValue = 2d, Step1MotorAbsoluteValue = -3d }
    ];

    public FourierCenterChannelFlexibleApertureDTOItem XDiffractionPushItem { get; private init; } = new(CenterChannelMotionKindEnum.XDiffractionPush, 0d) { Step0MotorAbsoluteValue = 15d, Step1MotorAbsoluteValue = 20d };

    public FourierCenterChannelFlexibleApertureDTOItem XReflectionCoarsePushItem { get; private init; } = new(CenterChannelMotionKindEnum.XReflectionCoarsePush, 0d) { Step0MotorAbsoluteValue = 20d, Step1MotorAbsoluteValue = 15d };

    public FourierCenterChannelFlexibleApertureDTOItem XReflectionFinePushItem { get; private init; } = new(CenterChannelMotionKindEnum.XReflectionFinePush, 0d) { Step0MotorAbsoluteValue = -10d, Step1MotorAbsoluteValue = -15d };

    public FourierCenterChannelFlexibleApertureDTOItem[] YRodItems { get; private init; } =
    [
        new(CenterChannelMotionKindEnum.YRod, AngleStep * 5d) { Step0MotorAbsoluteValue = 2d, Step1MotorAbsoluteValue = -3d },
        new(CenterChannelMotionKindEnum.YRod, AngleStep * 6d) { Step0MotorAbsoluteValue = 2d, Step1MotorAbsoluteValue = -3d },
        new(CenterChannelMotionKindEnum.YRod, AngleStep * 7d) { Step0MotorAbsoluteValue = 2d, Step1MotorAbsoluteValue = -3d },
        new(CenterChannelMotionKindEnum.YRod, AngleStep * 10d) { Step0MotorAbsoluteValue = 2d, Step1MotorAbsoluteValue = -3d }
    ];

#pragma warning disable IDISP003

    public override FourierCenterChannelFlexibleApertureDTO Clone() => new()
    {
        XApertureItem = XApertureItem.Clone(),
        XCalibrationPatternItem = XCalibrationPatternItem.Clone(),
        XRodItems = [.. XRodItems.Select(t => t.Clone())],
        XDiffractionPushItem = XDiffractionPushItem.Clone(),
        XReflectionCoarsePushItem = XReflectionCoarsePushItem.Clone(),
        XReflectionFinePushItem = XReflectionFinePushItem.Clone(),
        YRodItems = [.. YRodItems.Select(t => t.Clone())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

#pragma warning restore IDISP003

    public CalibrationPupilCenterChannelFlexibleApertureV1 AdaptTo() => new()
    {
        CgFFBoxTurnXLightHoleCircleAngleCh3 = XApertureItem.AngleMotorAbsoluteValue,
        CgFFBoxTurnXLightHoleCircleRelationCh3 = XApertureItem.PixelSize,
        CgFFBoxTurnXLightHoleCirclePositionCh3 = XApertureItem.Step0MotorAbsoluteValue,
        CgFFBoxTurnXLightHoleCircleCenterCh3 = XApertureItem.Step0ROI.ImageROI.Center.ToCgPoint(),
        CgFFBoxTurnXLightHoleCircleRadiusCh3 = XApertureItem.Step0ROI.ImageROI.Width / 2d,
        CgFFBoxTurnXCalibrationPatternCircleAngleCh3 = XCalibrationPatternItem.AngleMotorAbsoluteValue,
        CgFFBoxTurnXCalibrationPatternCircleRelationCh3 = XCalibrationPatternItem.PixelSize,
        CgFFBoxTurnXCalibrationPatternCirclePositionCh3 = XCalibrationPatternItem.Step0MotorAbsoluteValue,
        CgFFBoxTurnXCalibrationPatternCircleCenterCh3 = XCalibrationPatternItem.Step0ROI.ImageROI.Center.ToCgPoint(),
        CgFFBoxTurnXCalibrationPatternCircleRadiusCh3 = XCalibrationPatternItem.Step0ROI.ImageROI.Width / 2d,
        CgFFBoxTurnXAngleCh3 = [.. XRodItems.Select(t => t.AngleMotorAbsoluteValue)],
        CgFFBoxTurnXWidthCh3 = [.. XRodItems.Select(t => t.Step0ROI.ImageROI.Width)],
        CgFFBoxTurnXMotorRelationCh3 = [.. XRodItems.Select(t => t.PixelSize)],
        CgFFBoxTurnXMotorPositionCh3 = [.. XRodItems.Select(t => t.Step0MotorAbsoluteValue)],
        CgFFBoxTurnXRectPositionCh3 = [.. XRodItems.Select(t => t.Step0ROI.ImageROI.ToRectD())],
        CgFFBoxDiffractionPushXWidthCh3 = XDiffractionPushItem.Step0ROI.ImageROI.Width,
        CgFFBoxDiffractionPushXMotorRelationCh3 = XDiffractionPushItem.PixelSize,
        CgFFBoxDiffractionPushXMotorPositionCh3 = XDiffractionPushItem.Step0MotorAbsoluteValue,
        CgFFBoxDiffractionPushXRectPositionCh3 = XDiffractionPushItem.Step0ROI.ImageROI.ToRectD(),
        CgFFBoxReflectionCoarsePushXWidthCh3 = XReflectionCoarsePushItem.Step0ROI.ImageROI.Width,
        CgFFBoxReflectionCoarsePushXMotorRelationCh3 = XReflectionCoarsePushItem.PixelSize,
        CgFFBoxReflectionCoarsePushXMotorPositionCh3 = XReflectionCoarsePushItem.Step0MotorAbsoluteValue,
        CgFFBoxReflectionCoarsePushXRectPositionCh3 = XReflectionCoarsePushItem.Step0ROI.ImageROI.ToRectD(),
        CgFFBoxReflectionFinePushXWidthCh3 = XReflectionFinePushItem.Step0ROI.ImageROI.Width,
        CgFFBoxReflectionFinePushXMotorRelationCh3 = XReflectionFinePushItem.PixelSize,
        CgFFBoxReflectionFinePushXMotorPositionCh3 = XReflectionFinePushItem.Step0MotorAbsoluteValue,
        CgFFBoxReflectionFinePushXRectPositionCh3 = XReflectionFinePushItem.Step0ROI.ImageROI.ToRectD(),
        CgFFBoxTurnYAngleCh3 = [.. YRodItems.Select(t => t.AngleMotorAbsoluteValue)],
        CgFFBoxTurnYWidthCh3 = [.. YRodItems.Select(t => t.Step0ROI.ImageROI.Height)],
        CgFFBoxTurnYMotorRelationCh3 = [.. YRodItems.Select(t => t.PixelSize)],
        CgFFBoxTurnYMotorPositionCh3 = [.. YRodItems.Select(t => t.Step0MotorAbsoluteValue)],
        CgFFBoxTurnYRectPositionCh3 = [.. YRodItems.Select(t => t.Step0ROI.ImageROI.ToRectD())],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    public void Dispose()
    {
        XApertureItem.Dispose();
        XCalibrationPatternItem.Dispose();
        foreach (var xItem in XRodItems) xItem.Dispose();
        XDiffractionPushItem.Dispose();
        XReflectionCoarsePushItem.Dispose();
        XReflectionFinePushItem.Dispose();
        foreach (var yItem in YRodItems) yItem.Dispose();
    }
}