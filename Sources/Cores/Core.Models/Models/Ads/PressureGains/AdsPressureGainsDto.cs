using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Ads;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Ads.PressureGains;

public sealed partial class AdsPressureGainsDto : CalibrationDtoBase, ICloneable<AdsPressureGainsDto>, IAdaptTo<CalibrationAdsPressureGains>
{
    [ObservableProperty]
    private double _pressureValue1;

    [ObservableProperty]
    private double _pressureValue2;

    [ObservableProperty]
    private double _pressureValue3;

    [ObservableProperty]
    private Point _findPosition;

    #region Mapper

    public AdsPressureGainsDto Clone() => new()
    {
        PressureValue1 = PressureValue1,
        PressureValue2 = PressureValue2,
        PressureValue3 = PressureValue3,
        FindPosition = FindPosition,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationAdsPressureGains AdaptTo() => new()
    {
        PressureValue1 = PressureValue1,
        PressureValue2 = PressureValue2,
        PressureValue3 = PressureValue3,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}