using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Laser.DOEAngle;

public partial class LaserDOEAngleDto : CalibrationDtoBase, ICloneable<LaserDOEAngleDto>, IAdaptTo<CalibrationLaserDOEAngle>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _dOEAngle;

    [ObservableProperty]
    private double _dOEReviseAngle;

    [ObservableProperty]
    private double _multiRtfcFitSlope;

    [ObservableProperty]
    private double _afPosError;

    #region Mapper

    public LaserDOEAngleDto Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        DOEAngle = DOEAngle,
        DOEReviseAngle = DOEReviseAngle,
        MultiRtfcFitSlope = MultiRtfcFitSlope,
        AfPosError = AfPosError,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserDOEAngle AdaptTo()
    {
        return new CalibrationLaserDOEAngle
        {
            DOEAngle = DOEAngle,
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredCalibrate = IsRequiredSelfCheck
        };
    }

    #endregion Mapper
}