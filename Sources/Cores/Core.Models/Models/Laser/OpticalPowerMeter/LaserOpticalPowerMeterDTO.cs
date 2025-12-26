using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.OpticalPowerMeter;

public sealed partial class LaserOpticalPowerMeterDTO : CalibrationDtoBase, ICloneable<LaserOpticalPowerMeterDTO>, IAdaptTo<CalibrationLaserOpticalPower>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _coefficient;

    [ObservableProperty]
    private Point _findMachinePosition;

    [ObservableProperty]
    private List<LaserOpticalPowerMeterDTOItem> _map = [];

    [ObservableProperty]
    private double _maxMeasurePower;

    [ObservableProperty]
    private Point _maxMeasurePowerPosition = Point.Origin;

    #region Mapper

    public LaserOpticalPowerMeterDTO Clone() => new()
    {
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        ProductivityInformation = ProductivityInformation.Clone(),
        Coefficient = Coefficient,
        FindMachinePosition = FindMachinePosition,
        Map = [.. Map.Select(x => x.Clone())],
        MaxMeasurePower = MaxMeasurePower,
        MaxMeasurePowerPosition = MaxMeasurePowerPosition,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserOpticalPower AdaptTo() => new()
    {
        CgNIOITypeEnum = OpticsIlluminationModeEnum.ToCgNIOITypeEnum(),
        CgMagTypeEnum = ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum(),
        Coefficient = Coefficient,
        MeasureMaxPower = MaxMeasurePower,
        MeasureMaxPowerPosition = MaxMeasurePowerPosition.ToCgPoint(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class LaserOpticalPowerMeterDTOItem : ObservableObject, ICloneable<LaserOpticalPowerMeterDTOItem>
{
    [ObservableProperty]
    private int _row;

    [ObservableProperty]
    private int _column;

    [ObservableProperty]
    private Point _measurePosition;

    [ObservableProperty]
    private double _measurePower;

    #region Mapper

    public LaserOpticalPowerMeterDTOItem Clone() => new()
    {
        Row = Row,
        Column = Column,
        MeasurePosition = MeasurePosition,
        MeasurePower = MeasurePower
    };

    #endregion Mapper
}