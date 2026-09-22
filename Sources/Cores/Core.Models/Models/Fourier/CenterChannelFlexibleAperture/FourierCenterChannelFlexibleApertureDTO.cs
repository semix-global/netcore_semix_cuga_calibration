using Core.Models.Extensions;
using Core.Models.Models;
using Core.Wcf.Models.Fourier;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Fourier.CenterChannelFlexibleAperture;

[CacheVersion("2.0.0")]
public sealed class FourierCenterChannelFlexibleApertureDTO : CalibrationDTOBase<FourierCenterChannelFlexibleApertureDTO>, IAdaptTo<CalibrationPupilCenterChannelFlexibleAperture>, IDisposable
{
    public FourierCenterChannelFlexibleApertureLightHoleItem XLightHoleItem { get; private init; }

    public FourierCenterChannelFlexibleApertureLightHoleItem YLightHoleItem { get; private init; }

    public FourierCenterChannelFlexibleApertureMotionItem[] MotionItems { get; private init; }

    [Newtonsoft.Json.JsonIgnore]
    public IReadOnlyList<FourierCenterChannelFlexibleApertureMotionItem> XMotionItems => MotionItems.Where(t => t.Kind == CenterChannelMotionKind.X).OrderBy(t => t.NominalAngle).ToArray();

    [Newtonsoft.Json.JsonIgnore]
    public IReadOnlyList<FourierCenterChannelFlexibleApertureMotionItem> YMotionItems => MotionItems.Where(t => t.Kind == CenterChannelMotionKind.Y).OrderBy(t => t.NominalAngle).ToArray();

    [Newtonsoft.Json.JsonIgnore]
    public FourierCenterChannelFlexibleApertureMotionItem PushItem => MotionItems.Single(t => t.Kind == CenterChannelMotionKind.Push);

    [Newtonsoft.Json.JsonIgnore]
    public FourierCenterChannelFlexibleApertureMotionItem X120Item => XMotionItems[0];

    [Newtonsoft.Json.JsonIgnore]
    public FourierCenterChannelFlexibleApertureMotionItem X180Item => XMotionItems[1];

    [Newtonsoft.Json.JsonIgnore]
    public FourierCenterChannelFlexibleApertureMotionItem X240Item => XMotionItems[2];

    [Newtonsoft.Json.JsonIgnore]
    public FourierCenterChannelFlexibleApertureMotionItem X300Item => XMotionItems[3];

    [Newtonsoft.Json.JsonIgnore]
    public FourierCenterChannelFlexibleApertureMotionItem Y120Item => YMotionItems[0];

    [Newtonsoft.Json.JsonIgnore]
    public FourierCenterChannelFlexibleApertureMotionItem Y180Item => YMotionItems[1];

    [Newtonsoft.Json.JsonIgnore]
    public FourierCenterChannelFlexibleApertureMotionItem Y240Item => YMotionItems[2];

    [Newtonsoft.Json.JsonIgnore]
    public FourierCenterChannelFlexibleApertureMotionItem Y300Item => YMotionItems[3];

    [Newtonsoft.Json.JsonIgnore]
    public bool IsComplete => XLightHoleItem.IsCalculated
                              && YLightHoleItem.IsCalculated
                              && MotionItems.All(t => t.IsSecondCompleted)
                              && XMotionItems.Count == 4
                              && YMotionItems.Count == 4;

    public FourierCenterChannelFlexibleApertureDTO()
    {
        XLightHoleItem = new FourierCenterChannelFlexibleApertureLightHoleItem(CenterChannelMotionKind.X);
        YLightHoleItem = new FourierCenterChannelFlexibleApertureLightHoleItem(CenterChannelMotionKind.Y);
        MotionItems =
        [
            new(CenterChannelMotionKind.X, 120d),
            new(CenterChannelMotionKind.X, 180d),
            new(CenterChannelMotionKind.X, 240d),
            new(CenterChannelMotionKind.X, 300d),
            new(CenterChannelMotionKind.Y, 120d),
            new(CenterChannelMotionKind.Y, 180d),
            new(CenterChannelMotionKind.Y, 240d),
            new(CenterChannelMotionKind.Y, 300d),
            new(CenterChannelMotionKind.Push, null)
        ];
    }

    [Newtonsoft.Json.JsonConstructor]
    private FourierCenterChannelFlexibleApertureDTO(
        FourierCenterChannelFlexibleApertureLightHoleItem xLightHoleItem,
        FourierCenterChannelFlexibleApertureLightHoleItem yLightHoleItem,
        FourierCenterChannelFlexibleApertureMotionItem[] motionItems)
    {
        XLightHoleItem = xLightHoleItem;
        YLightHoleItem = yLightHoleItem;
        MotionItems = motionItems;
    }

    public override FourierCenterChannelFlexibleApertureDTO Clone() => new(
        XLightHoleItem.Clone(),
        YLightHoleItem.Clone(),
        [.. MotionItems.Select(t => t.Clone())])
    {
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationPupilCenterChannelFlexibleAperture AdaptTo()
    {
        if (IsComplete == false)
        {
            return new CalibrationPupilCenterChannelFlexibleAperture
            {
                IsCalibrated = false,
                IsVerified = false,
                IsRequiredCalibrate = IsRequiredSelfCheck,
                CgFFBoxTurnXAngleCh3 = [],
                CgFFBoxTurnXWidthCh3 = [],
                CgFFBoxTurnXMotorRelationCh3 = [],
                CgFFBoxTurnXMotorPositionCh3 = [],
                CgFFBoxTurnXRectPositionCh3 = [],
                CgFFBoxTurnYAngleCh3 = [],
                CgFFBoxTurnYWidthCh3 = [],
                CgFFBoxTurnYMotorRelationCh3 = [],
                CgFFBoxTurnYMotorPositionCh3 = [],
                CgFFBoxTurnYRectPositionCh3 = []
            };
        }

        var xItems = XMotionItems;
        var yItems = YMotionItems;
        var push = PushItem;

        return new CalibrationPupilCenterChannelFlexibleAperture
        {
            CgFFBoxTurnXAngleCh3 = [.. xItems.Select(t => t.ActualAngle ?? t.NominalAngle ?? 0d)],
            CgFFBoxTurnXWidthCh3 = [.. xItems.Select(t => t.FirstROI.Width)],
            CgFFBoxTurnXMotorRelationCh3 = [.. xItems.Select(t => Math.Abs(t.PixelSize))],
            CgFFBoxTurnXMotorPositionCh3 = [.. xItems.Select(t => t.FirstPosition)],
            CgFFBoxTurnXRectPositionCh3 = [.. xItems.Select(t => t.FirstROI.ToRectD())],
            CgFFBoxTurnXLightHoleCircleCenterCh3 = XLightHoleItem.Center.ToCgPoint(),
            CgFFBoxTurnXLightHoleCircleRadiusCh3 = XLightHoleItem.Radius,
            CgFFBoxTurnYAngleCh3 = [.. yItems.Select(t => t.ActualAngle ?? t.NominalAngle ?? 0d)],
            CgFFBoxTurnYWidthCh3 = [.. yItems.Select(t => t.FirstROI.Height)],
            CgFFBoxTurnYMotorRelationCh3 = [.. yItems.Select(t => Math.Abs(t.PixelSize))],
            CgFFBoxTurnYMotorPositionCh3 = [.. yItems.Select(t => t.FirstPosition)],
            CgFFBoxTurnYRectPositionCh3 = [.. yItems.Select(t => t.FirstROI.ToRectD())],
            CgFFBoxTurnYLightHoleCircleCenterCh3 = YLightHoleItem.Center.ToCgPoint(),
            CgFFBoxTurnYLightHoleCircleRadiusCh3 = YLightHoleItem.Radius,
            CgFFBoxPushXWidthCh3 = push.FirstROI.Width,
            CgFFBoxPushXMotorRelationCh3 = Math.Abs(push.PixelSize),
            CgFFBoxPushXMotorPositionCh3 = push.FirstPosition,
            CgFFBoxPushXRectPositionCh3 = push.FirstROI.ToRectD(),
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredCalibrate = IsRequiredSelfCheck
        };
    }

    public void Dispose()
    {
        XLightHoleItem.Dispose();
        YLightHoleItem.Dispose();
        foreach (var item in MotionItems) item.Dispose();
    }
}