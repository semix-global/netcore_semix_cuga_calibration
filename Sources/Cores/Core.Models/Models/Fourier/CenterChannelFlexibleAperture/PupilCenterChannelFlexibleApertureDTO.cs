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
public sealed partial class PupilCenterChannelFlexibleApertureDTO : CalibrationDtoBase, ICloneable<PupilCenterChannelFlexibleApertureDTO>, IAdaptTo<CalibrationPupilCenterChannelFlexibleAperture>
{
    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxTurnXAngleCh3 = new ObservableCollection<double>();

    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxTurnXWidthCh3 = new ObservableCollection<double>();

    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxTurnXMotorRelationCh3 = new ObservableCollection<double>();

    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxTurnXMotorPositionCh3 = new ObservableCollection<double>();

    [ObservableProperty]
    public ObservableCollection<Rect> _cgFFBoxTurnXRectPositionCh3 = new ObservableCollection<Rect>();

    [ObservableProperty]
    public Point _cgFFBoxTurnXLightHoleCircleCenterCh3 = new Point();

    [ObservableProperty]
    public double _cgFFBoxTurnXLightHoleCircleRadiusCh3 = 0;

    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxTurnYAngleCh3 = new ObservableCollection<double>();

    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxTurnYWidthCh3 = new ObservableCollection<double>();

    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxTurnYMotorRelationCh3 = new ObservableCollection<double>();

    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxTurnYMotorPositionCh3 = new ObservableCollection<double>();

    [ObservableProperty]
    public ObservableCollection<Rect> _cgFFBoxTurnYRectPositionCh3 = new ObservableCollection<Rect>();

    [ObservableProperty]
    public Point _cgFFBoxTurnYLightHoleCircleCenterCh3 = new Point();

    [ObservableProperty]
    public double _cgFFBoxTurnYLightHoleCircleRadiusCh3 = 0;

    [ObservableProperty]
    public double _cgFFBoxPushXWidthCh3 = 0;

    [ObservableProperty]
    public double _cgFFBoxPushXMotorRelationCh3 = 0;

    [ObservableProperty]
    public double _cgFFBoxPushXMotorPositionCh3 = 0;

    [ObservableProperty]
    public Rect _cgFFBoxPushXRectPositionCh3 = new Rect();

    #region Mapper

    public PupilCenterChannelFlexibleApertureDTO Clone()
    {
        return new PupilCenterChannelFlexibleApertureDTO
        {
            CgFFBoxTurnXAngleCh3 = CgFFBoxTurnXAngleCh3,
            CgFFBoxTurnXWidthCh3 = CgFFBoxTurnXWidthCh3,
            CgFFBoxTurnXMotorRelationCh3 = CgFFBoxTurnXMotorRelationCh3,
            CgFFBoxTurnXMotorPositionCh3 = CgFFBoxTurnXMotorPositionCh3,
            CgFFBoxTurnXRectPositionCh3 = CgFFBoxTurnXRectPositionCh3,
            CgFFBoxTurnXLightHoleCircleCenterCh3 = CgFFBoxTurnXLightHoleCircleCenterCh3,
            CgFFBoxTurnXLightHoleCircleRadiusCh3 = CgFFBoxTurnXLightHoleCircleRadiusCh3,
            CgFFBoxTurnYAngleCh3 = CgFFBoxTurnYAngleCh3,
            CgFFBoxTurnYWidthCh3 = CgFFBoxTurnYWidthCh3,
            CgFFBoxTurnYMotorRelationCh3 = CgFFBoxTurnYMotorRelationCh3,
            CgFFBoxTurnYMotorPositionCh3 = CgFFBoxTurnYMotorPositionCh3,
            CgFFBoxTurnYRectPositionCh3 = CgFFBoxTurnYRectPositionCh3,
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
    }

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