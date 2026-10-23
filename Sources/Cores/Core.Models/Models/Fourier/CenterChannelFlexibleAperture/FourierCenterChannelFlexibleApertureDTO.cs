using Core.Models.Extensions;
using Core.Wcf.Models.Fourier;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Fourier.CenterChannelFlexibleAperture;

[CacheVersion("2.0.0")]
public sealed class FourierCenterChannelFlexibleApertureDTO : CalibrationDTOBase<FourierCenterChannelFlexibleApertureDTO>, IAdaptTo<CalibrationPupilCenterChannelFlexibleApertureV1>, IDisposable
{
    private const double AngleStep = 360d / 11d;

    public FourierCenterChannelFlexibleApertureDTOItem XApertureItem { get; private init; } = new(CenterChannelMotionKindEnum.XAperture, AngleStep * 9d) { Step0MotorAbsoluteValue = 10d, Step1MotorAbsoluteValue = 15d };

    public FourierCenterChannelFlexibleApertureDTOItem[] XRodItems { get; private init; } =
    [
        new(CenterChannelMotionKindEnum.XRod, AngleStep * 1d) { Step0MotorAbsoluteValue = -2d, Step1MotorAbsoluteValue = -7d },
        new(CenterChannelMotionKindEnum.XRod, AngleStep * 2d) { Step0MotorAbsoluteValue = 0d, Step1MotorAbsoluteValue = -5d },
        new(CenterChannelMotionKindEnum.XRod, AngleStep * 3d) { Step0MotorAbsoluteValue = 2d, Step1MotorAbsoluteValue = -3d },
        new(CenterChannelMotionKindEnum.XRod, AngleStep * 4d) { Step0MotorAbsoluteValue = 2d, Step1MotorAbsoluteValue = -3d }
    ];

    public FourierCenterChannelFlexibleApertureDTOItem XPushItem { get; private init; } = new(CenterChannelMotionKindEnum.XPush, 0d) { Step0MotorAbsoluteValue = 34d, Step1MotorAbsoluteValue = 39d };

    public FourierCenterChannelFlexibleApertureDTOItem XCalibrationPatternItem { get; private init; } = new(CenterChannelMotionKindEnum.XCalibrationPattern, AngleStep * 8d) { Step0MotorAbsoluteValue = 10d, Step1MotorAbsoluteValue = 15d };

    public FourierCenterChannelFlexibleApertureDTOItem[] YRodItems { get; private init; } =
    [
        new(CenterChannelMotionKindEnum.YRod, AngleStep * 5d) { Step0MotorAbsoluteValue = 0d, Step1MotorAbsoluteValue = 5d },
        new(CenterChannelMotionKindEnum.YRod, AngleStep * 6d) { Step0MotorAbsoluteValue = 0d, Step1MotorAbsoluteValue = 5d },
        new(CenterChannelMotionKindEnum.YRod, AngleStep * 7d) { Step0MotorAbsoluteValue = 0d, Step1MotorAbsoluteValue = 5d },
        new(CenterChannelMotionKindEnum.YRod, AngleStep * 10d) { Step0MotorAbsoluteValue = 0d, Step1MotorAbsoluteValue = 5d }
    ];

    public FourierCenterChannelFlexibleApertureDTOItem XDiffractionCoarsePushItem { get; private init; } = new(CenterChannelMotionKindEnum.XDiffractionCoarsePush, 0d) { Step0MotorAbsoluteValue = 0d, Step1MotorAbsoluteValue = 5d };

    public FourierCenterChannelFlexibleApertureDTOItem XDiffractionFinePushItem { get; private init; } = new(CenterChannelMotionKindEnum.XDiffractionFinePush, 0d) { Step0MotorAbsoluteValue = 0d, Step1MotorAbsoluteValue = 5d };

#pragma warning disable IDISP003

    public override FourierCenterChannelFlexibleApertureDTO Clone() => new()
    {
        XApertureItem = XApertureItem.Clone(),
        XRodItems = [.. XRodItems.Select(t => t.Clone())],
        XPushItem = XPushItem.Clone(),
        XCalibrationPatternItem = XCalibrationPatternItem.Clone(),
        YRodItems = [.. YRodItems.Select(t => t.Clone())],
        XDiffractionCoarsePushItem = XDiffractionCoarsePushItem.Clone(),
        XDiffractionFinePushItem = XDiffractionFinePushItem.Clone(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

#pragma warning restore IDISP003

    public CalibrationPupilCenterChannelFlexibleApertureV1 AdaptTo() => new()
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
        CgFFBoxTurnXCalibrationPatternCircleAngleCh3 = XCalibrationPatternItem.AngleMotorAbsoluteValue,
        CgFFBoxTurnXCalibrationPatternCircleRelationCh3 = XCalibrationPatternItem.PixelSize,
        CgFFBoxTurnXCalibrationPatternCirclePositionCh3 = XCalibrationPatternItem.Step0MotorAbsoluteValue,
        CgFFBoxTurnXCalibrationPatternCircleCenterCh3 = XCalibrationPatternItem.Step0ROI.ImageROI.Center.ToCgPoint(),
        CgFFBoxTurnXCalibrationPatternCircleRadiusCh3 = XCalibrationPatternItem.Step0ROI.ImageROI.Width / 2d,
        CgFFBoxPushXWidthCh3 = XPushItem.Step0ROI.ImageROI.Width,
        CgFFBoxPushXMotorRelationCh3 = XPushItem.PixelSize,
        CgFFBoxPushXMotorPositionCh3 = XPushItem.Step0MotorAbsoluteValue,
        CgFFBoxPushXRectPositionCh3 = XPushItem.Step0ROI.ImageROI.ToRectD(),
        CgFFBoxPushXDiffractionCoarseWidthCh3 = XDiffractionCoarsePushItem.Step0ROI.ImageROI.Width,
        CgFFBoxPushXDiffractionCoarseMotorRelationCh3 = XDiffractionCoarsePushItem.PixelSize,
        CgFFBoxPushXDiffractionCoarseMotorPositionCh3 = XDiffractionCoarsePushItem.Step0MotorAbsoluteValue,
        CgFFBoxPushXDiffractionCoarseRectPositionCh3 = XDiffractionCoarsePushItem.Step0ROI.ImageROI.ToRectD(),
        CgFFBoxPushXDiffractionFineWidthCh3 = XDiffractionFinePushItem.Step0ROI.ImageROI.Width,
        CgFFBoxPushXDiffractionFineMotorRelationCh3 = XDiffractionFinePushItem.PixelSize,
        CgFFBoxPushXDiffractionFineMotorPositionCh3 = XDiffractionFinePushItem.Step0MotorAbsoluteValue,
        CgFFBoxPushXDiffractionFineRectPositionCh3 = XDiffractionFinePushItem.Step0ROI.ImageROI.ToRectD(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    public void Dispose()
    {
        XApertureItem.Dispose();
        foreach (var xItem in XRodItems) xItem.Dispose();
        XPushItem.Dispose();

        XCalibrationPatternItem.Dispose();
        foreach (var yItem in YRodItems) yItem.Dispose();
        XDiffractionCoarsePushItem.Dispose();
        XDiffractionFinePushItem.Dispose();
    }
}