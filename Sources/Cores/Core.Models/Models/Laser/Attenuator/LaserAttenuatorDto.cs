using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.Attenuator;

public sealed partial class LaserAttenuatorDto : CalibrationDtoBase, ICloneable<LaserAttenuatorDto>
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private double _maxCoefficientAverageMeasurePower;

    [ObservableProperty]
    private IReadOnlyList<Point> _coefficientMeasurePowerPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _coefficientMeasurePowerRatePoints = [];

    [ObservableProperty]
    private double _p0;

    [ObservableProperty]
    private double _p1;

    [ObservableProperty]
    private double _p2;

    [ObservableProperty]
    private double _p3;

    [ObservableProperty]
    private double _rSquared;

    [ObservableProperty]
    private IReadOnlyList<Point> _coefficientFitMeasurePowerRatePoints = [];

    [ObservableProperty]
    private double _waitTime;

    /// <summary>
    /// 最小功率
    /// </summary>
    public double MinCoefficient => CoefficientMeasurePowerPoints.Min(t => t.X);

    /// <summary>
    /// 最大功率
    /// </summary>
    public double MaxCoefficient => CoefficientMeasurePowerPoints.Max(t => t.X);

    #region Mapper

    public LaserAttenuatorDto Clone() => new()
    {
        OpticsMagTypeEnum = OpticsMagTypeEnum,
        MaxCoefficientAverageMeasurePower = MaxCoefficientAverageMeasurePower,
        CoefficientMeasurePowerPoints = [.. CoefficientMeasurePowerPoints],
        CoefficientMeasurePowerRatePoints = [.. CoefficientMeasurePowerRatePoints],
        P0 = P0,
        P1 = P1,
        P2 = P2,
        P3 = P3,
        RSquared = RSquared,
        CoefficientFitMeasurePowerRatePoints = [.. CoefficientFitMeasurePowerRatePoints],
        WaitTime = WaitTime,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationAttenuatorObj AdaptTo() => new()
    {
        CgMagTypeEnum = OpticsMagTypeEnum.ToCgMagTypeEnum(),
        MaxCoefficientAverageMeasurePower = MaxCoefficientAverageMeasurePower,
        CoefficientMeasurePowerPoints = [.. CoefficientMeasurePowerPoints.Select(t => t.ToCgPoint())],
        CoefficientMeasurePowerRatePoints = [.. CoefficientMeasurePowerRatePoints.Select(t => t.ToCgPoint())],
        P0 = P0,
        P1 = P1,
        P2 = P2,
        P3 = P3,
        RSquared = RSquared,
        CoefficientFitMeasurePowerRatePoints = [.. CoefficientFitMeasurePowerRatePoints.Select(t => t.ToCgPoint())],
        WaitTime = WaitTime,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified
    };

    #endregion Mapper
}