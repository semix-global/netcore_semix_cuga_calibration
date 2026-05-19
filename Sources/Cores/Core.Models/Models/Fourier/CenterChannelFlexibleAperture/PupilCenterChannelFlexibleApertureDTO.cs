using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Fourier;
using Cuga.Data.DataStruct.DTO.Recipe;
using Cuga.Data.DataStruct.Stage;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using System.Collections.ObjectModel;
using Point = Net.Utilities.Models.Geometries.Point;

namespace Core.Models.Models.Fourier.CenterChannelFlexibleAperture;

[CacheVersion("1.0.0")]
public sealed partial class PupilCenterChannelFlexibleApertureDTO : CalibrationDTOBase<PupilCenterChannelFlexibleApertureDTO>, IAdaptTo<CalibrationPupilCenterChannelFlexibleAperture>
{
    [ObservableProperty]
    public partial ObservableCollection<double> CgFFBoxTurnXAngleCh3 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<double> CgFFBoxTurnXWidthCh3 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<double> CgFFBoxTurnXMotorRelationCh3 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<double> CgFFBoxTurnXMotorPositionCh3 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Rect> CgFFBoxTurnXRectPositionCh3 { get; set; } = [];

    [ObservableProperty]
    public partial Point CgFFBoxTurnXLightHoleCircleCenterCh3 { get; set; } = new();

    [ObservableProperty]
    public partial double CgFFBoxTurnXLightHoleCircleRadiusCh3 { get; set; } = 0;

    [ObservableProperty]
    public partial ObservableCollection<double> CgFFBoxTurnYAngleCh3 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<double> CgFFBoxTurnYWidthCh3 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<double> CgFFBoxTurnYMotorRelationCh3 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<double> CgFFBoxTurnYMotorPositionCh3 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Rect> CgFFBoxTurnYRectPositionCh3 { get; set; } = [];

    [ObservableProperty]
    public partial Point CgFFBoxTurnYLightHoleCircleCenterCh3 { get; set; } = new();

    [ObservableProperty]
    public partial double CgFFBoxTurnYLightHoleCircleRadiusCh3 { get; set; } = 0;

    [ObservableProperty]
    public partial double CgFFBoxPushXWidthCh3 { get; set; } = 0;

    [ObservableProperty]
    public partial double CgFFBoxPushXMotorRelationCh3 { get; set; } = 0;

    [ObservableProperty]
    public partial double CgFFBoxPushXMotorPositionCh3 { get; set; } = 0;

    [ObservableProperty]
    public partial Rect CgFFBoxPushXRectPositionCh3 { get; set; } = new();

    #region Mapper

    public override PupilCenterChannelFlexibleApertureDTO Clone() => new()
    {
        CgFFBoxTurnXAngleCh3 = new ObservableCollection<double>([.. CgFFBoxTurnXAngleCh3]),
        CgFFBoxTurnXWidthCh3 = new ObservableCollection<double>([.. CgFFBoxTurnXWidthCh3]),
        CgFFBoxTurnXMotorRelationCh3 = new ObservableCollection<double>([.. CgFFBoxTurnXMotorRelationCh3]),
        CgFFBoxTurnXMotorPositionCh3 = new ObservableCollection<double>([.. CgFFBoxTurnXMotorPositionCh3]),
        CgFFBoxTurnXRectPositionCh3 = new ObservableCollection<Rect>([.. CgFFBoxTurnXRectPositionCh3]),
        CgFFBoxTurnXLightHoleCircleCenterCh3 = CgFFBoxTurnXLightHoleCircleCenterCh3,
        CgFFBoxTurnXLightHoleCircleRadiusCh3 = CgFFBoxTurnXLightHoleCircleRadiusCh3,
        CgFFBoxTurnYAngleCh3 = new ObservableCollection<double>([.. CgFFBoxTurnYAngleCh3]),
        CgFFBoxTurnYWidthCh3 = new ObservableCollection<double>([.. CgFFBoxTurnYWidthCh3]),
        CgFFBoxTurnYMotorRelationCh3 = new ObservableCollection<double>([.. CgFFBoxTurnYMotorRelationCh3]),
        CgFFBoxTurnYMotorPositionCh3 = new ObservableCollection<double>([.. CgFFBoxTurnYMotorPositionCh3]),
        CgFFBoxTurnYRectPositionCh3 = new ObservableCollection<Rect>([.. CgFFBoxTurnYRectPositionCh3]),
        CgFFBoxTurnYLightHoleCircleCenterCh3 = CgFFBoxTurnYLightHoleCircleCenterCh3,
        CgFFBoxTurnYLightHoleCircleRadiusCh3 = CgFFBoxTurnYLightHoleCircleRadiusCh3,

        CgFFBoxPushXWidthCh3 = CgFFBoxPushXWidthCh3,
        CgFFBoxPushXMotorRelationCh3 = CgFFBoxPushXMotorRelationCh3,
        CgFFBoxPushXMotorPositionCh3 = CgFFBoxPushXMotorPositionCh3,
        CgFFBoxPushXRectPositionCh3 = CgFFBoxPushXRectPositionCh3,

        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationPupilCenterChannelFlexibleAperture AdaptTo() => new()
    {
        CgFFBoxTurnXAngleCh3 = CgFFBoxTurnXAngleCh3.ToList(),
        CgFFBoxTurnXWidthCh3 = CgFFBoxTurnXWidthCh3.ToList(),
        CgFFBoxTurnXMotorRelationCh3 = CgFFBoxTurnXMotorRelationCh3.ToList(),
        CgFFBoxTurnXMotorPositionCh3 = CgFFBoxTurnXMotorPositionCh3.ToList(),
        CgFFBoxTurnXRectPositionCh3 = CgFFBoxTurnXRectPositionCh3.Select(r => new RectD((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height)).ToList(),
        CgFFBoxTurnXLightHoleCircleCenterCh3 = new CgPoint((int)Math.Round(CgFFBoxTurnXLightHoleCircleCenterCh3.X), (int)Math.Round(CgFFBoxTurnXLightHoleCircleCenterCh3.Y)),
        CgFFBoxTurnXLightHoleCircleRadiusCh3 = CgFFBoxTurnXLightHoleCircleRadiusCh3,
        CgFFBoxTurnYAngleCh3 = CgFFBoxTurnYAngleCh3.ToList(),
        CgFFBoxTurnYWidthCh3 = CgFFBoxTurnYWidthCh3.ToList(),
        CgFFBoxTurnYMotorRelationCh3 = CgFFBoxTurnYMotorRelationCh3.ToList(),
        CgFFBoxTurnYMotorPositionCh3 = CgFFBoxTurnYMotorPositionCh3.ToList(),
        CgFFBoxTurnYRectPositionCh3 = CgFFBoxTurnYRectPositionCh3.Select(r => new RectD((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height)).ToList(),
        CgFFBoxTurnYLightHoleCircleCenterCh3 = new CgPoint((int)Math.Round(CgFFBoxTurnYLightHoleCircleCenterCh3.X), (int)Math.Round(CgFFBoxTurnYLightHoleCircleCenterCh3.Y)),
        CgFFBoxTurnYLightHoleCircleRadiusCh3 = CgFFBoxTurnYLightHoleCircleRadiusCh3,

        CgFFBoxPushXWidthCh3 = CgFFBoxPushXWidthCh3,
        CgFFBoxPushXMotorRelationCh3 = CgFFBoxPushXMotorRelationCh3,
        CgFFBoxPushXMotorPositionCh3 = CgFFBoxPushXMotorPositionCh3,
        CgFFBoxPushXRectPositionCh3 = new RectD((int)Math.Round(CgFFBoxPushXRectPositionCh3.X), (int)Math.Round(CgFFBoxPushXRectPositionCh3.Y), (int)Math.Round(CgFFBoxPushXRectPositionCh3.Width), (int)Math.Round(CgFFBoxPushXRectPositionCh3.Height)),

        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}