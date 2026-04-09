using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Fourier;
using Cuga.Data.DataStruct.DTO.Recipe;
using Cuga.Data.DataStruct.Stage;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using System.Collections.ObjectModel;
using Point = Net.Utilities.Models.Geometries.Point;

namespace Core.Models.Models.Fourier;

public sealed partial class PupilCenterChannelFlexibleApertureDTO : CalibrationDtoBase, ICloneable<PupilCenterChannelFlexibleApertureDTO>, IAdaptTo<CalibrationPupilCenterChannelFlexibleAperture>
{
    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxTurnXAngleCh3 = new ObservableCollection<double>();

    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxTurnXWidthCh3 = new ObservableCollection<double>();

    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxTurnXMotorRelationCH3 = new ObservableCollection<double>();

    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxTurnXMotorPositionCH3 = new ObservableCollection<double>();

    [ObservableProperty]
    public ObservableCollection<Rect> _cgFFBoxTurnXRectPositionCH3 = new ObservableCollection<Rect>();

    [ObservableProperty]
    public Point _cgFFBoxTurnXLightHoleCircleCenterCh3 = new Point();

    [ObservableProperty]
    public double _cgFFBoxTurnXLightHoleCircleRadiusCh3 = 0;

    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxTurnYAngleCh3 = new ObservableCollection<double>();

    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxTurnYWidthCh3 = new ObservableCollection<double>();

    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxTurnYMotorRelationCH3 = new ObservableCollection<double>();

    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxTurnYMotorPositionCH3 = new ObservableCollection<double>();

    [ObservableProperty]
    public ObservableCollection<Rect> _cgFFBoxTurnYRectPositionCH3 = new ObservableCollection<Rect>();

    [ObservableProperty]
    public Point _cgFFBoxTurnYLightHoleCircleCenterCh3 = new Point();

    [ObservableProperty]
    public double _cgFFBoxTurnYLightHoleCircleRadiusCh3 = 0;

    [ObservableProperty]
    public double _cgFFBoxPushXWidthCh3 = 0;

    [ObservableProperty]
    public double _cgFFBoxPushXMotorRelationCH3 = 0;

    [ObservableProperty]
    public double _cgFFBoxPushXMotorPositionCH3 = 0;

    [ObservableProperty]
    public Rect _cgFFBoxPushXRectPositionCH3 = new Rect();

    #region Mapper  

    public PupilCenterChannelFlexibleApertureDTO Clone()
    {
        return new PupilCenterChannelFlexibleApertureDTO
        {
            CgFFBoxTurnXAngleCh3 = CgFFBoxTurnXAngleCh3,
            CgFFBoxTurnXWidthCh3 = CgFFBoxTurnXWidthCh3,
            CgFFBoxTurnXMotorRelationCH3 = CgFFBoxTurnXMotorRelationCH3,
            CgFFBoxTurnXMotorPositionCH3 = CgFFBoxTurnXMotorPositionCH3,
            CgFFBoxTurnXRectPositionCH3 = CgFFBoxTurnXRectPositionCH3,
            CgFFBoxTurnXLightHoleCircleCenterCh3 = CgFFBoxTurnXLightHoleCircleCenterCh3,
            CgFFBoxTurnXLightHoleCircleRadiusCh3 = CgFFBoxTurnXLightHoleCircleRadiusCh3,
            CgFFBoxTurnYAngleCh3 = CgFFBoxTurnYAngleCh3,
            CgFFBoxTurnYWidthCh3 = CgFFBoxTurnYWidthCh3,
            CgFFBoxTurnYMotorRelationCH3 = CgFFBoxTurnYMotorRelationCH3,
            CgFFBoxTurnYMotorPositionCH3 = CgFFBoxTurnYMotorPositionCH3,
            CgFFBoxTurnYRectPositionCH3 = CgFFBoxTurnYRectPositionCH3,
            CgFFBoxTurnYLightHoleCircleCenterCh3 = CgFFBoxTurnYLightHoleCircleCenterCh3,
            CgFFBoxTurnYLightHoleCircleRadiusCh3 = CgFFBoxTurnYLightHoleCircleRadiusCh3,

            CgFFBoxPushXWidthCh3 = CgFFBoxPushXWidthCh3,
            CgFFBoxPushXMotorRelationCH3 = CgFFBoxPushXMotorRelationCH3,
            CgFFBoxPushXMotorPositionCH3 = CgFFBoxPushXMotorPositionCH3,
            CgFFBoxPushXRectPositionCH3 = CgFFBoxPushXRectPositionCH3,

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
        CgFFBoxTurnXMotorRelationCH3 = CgFFBoxTurnXMotorRelationCH3.ToList(),
        CgFFBoxTurnXMotorPositionCH3 = CgFFBoxTurnXMotorPositionCH3.ToList(),
        CgFFBoxTurnXRectPositionCH3 = CgFFBoxTurnXRectPositionCH3.Select(r => new RectD((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height)).ToList(),
        CgFFBoxTurnXLightHoleCircleCenterCh3 = new CgPoint((int)Math.Round(CgFFBoxTurnXLightHoleCircleCenterCh3.X), (int)Math.Round(CgFFBoxTurnXLightHoleCircleCenterCh3.Y)),
        CgFFBoxTurnXLightHoleCircleRadiusCh3 = CgFFBoxTurnXLightHoleCircleRadiusCh3,
        CgFFBoxTurnYAngleCh3 = CgFFBoxTurnYAngleCh3.ToList(),
        CgFFBoxTurnYWidthCh3 = CgFFBoxTurnYWidthCh3.ToList(),
        CgFFBoxTurnYMotorRelationCH3 = CgFFBoxTurnYMotorRelationCH3.ToList(),
        CgFFBoxTurnYMotorPositionCH3 = CgFFBoxTurnYMotorPositionCH3.ToList(),
        CgFFBoxTurnYRectPositionCH3 = CgFFBoxTurnYRectPositionCH3.Select(r => new RectD((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height)).ToList(),
        CgFFBoxTurnYLightHoleCircleCenterCh3 = new CgPoint((int)Math.Round(CgFFBoxTurnYLightHoleCircleCenterCh3.X), (int)Math.Round(CgFFBoxTurnYLightHoleCircleCenterCh3.Y)),
        CgFFBoxTurnYLightHoleCircleRadiusCh3 = CgFFBoxTurnYLightHoleCircleRadiusCh3,

        CgFFBoxPushXWidthCh3 = CgFFBoxPushXWidthCh3,
        CgFFBoxPushXMotorRelationCH3 = CgFFBoxPushXMotorRelationCH3,
        CgFFBoxPushXMotorPositionCH3 = CgFFBoxPushXMotorPositionCH3,
        CgFFBoxPushXRectPositionCH3 = new RectD((int)Math.Round(CgFFBoxPushXRectPositionCH3.X), (int)Math.Round(CgFFBoxPushXRectPositionCH3.Y), (int)Math.Round(CgFFBoxPushXRectPositionCH3.Width), (int)Math.Round(CgFFBoxPushXRectPositionCH3.Height)),

        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}
