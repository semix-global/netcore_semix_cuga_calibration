using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.OpticalPowerMeter;

public sealed partial class LaserOpticalPowerDto : CalibrationDtoBase, ICloneable<LaserOpticalPowerDto>, IAdaptTo<CalibrationLaserOpticalPower>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;
    
    [ObservableProperty]
    private double _coefficient;

    [ObservableProperty]
    private int _rowNumber;

    [ObservableProperty]
    private int _columnNumber;

    [ObservableProperty]
    private Point _findCenterPosition;

    [ObservableProperty]
    private List<LaserOpticalPowerItemDto> _map = [];

    [ObservableProperty]
    private double _measureMaxPower;

    [ObservableProperty]
    private Point _measureMaxPowerPosition = Point.Origin;

    #region Mapper

    public LaserOpticalPowerDto Clone()
    {
        return new LaserOpticalPowerDto
        {
            ProductivityInformation = ProductivityInformation.Clone(),
            Coefficient = Coefficient,
            RowNumber = RowNumber,
            ColumnNumber = ColumnNumber,
            FindCenterPosition = FindCenterPosition,
            Map = [.. Map.Select(x => x.Clone())],
            MeasureMaxPower = MeasureMaxPower,
            MeasureMaxPowerPosition = MeasureMaxPowerPosition,
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredSelfCheck = IsRequiredSelfCheck,
            Id = Id,
            Expiration = Expiration
        };
    }

    public CalibrationLaserOpticalPower AdaptTo()
    {
        return new CalibrationLaserOpticalPower
        {
            CgMagTypeEnum = ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum(),
            Coefficient = Coefficient,
            MeasureMaxPower = MeasureMaxPower,
            MeasureMaxPowerPosition = MeasureMaxPowerPosition.ToCgPoint(),
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredSelfCheck = IsRequiredSelfCheck
        };
    }

    #endregion Mapper
}

public sealed partial class LaserOpticalPowerItemDto : ObservableCacheBase, ICloneable<LaserOpticalPowerItemDto>
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

    public LaserOpticalPowerItemDto Clone() => new()
    {
        Row = Row,
        Column = Column,
        MeasurePosition = MeasurePosition,
        MeasurePower = MeasurePower,
        Id = Id,
        Expiration = Expiration
    };

    #endregion Mapper
}