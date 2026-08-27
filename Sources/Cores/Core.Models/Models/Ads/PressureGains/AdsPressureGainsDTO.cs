using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Ads;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Ads.PressureGains;

[CacheVersion("1.0.0")]
public sealed partial class AdsPressureGainsDTO : CalibrationDTOBase<AdsPressureGainsDTO>, IAdaptTo<CalibrationAdsPressureGains>
{
    [ObservableProperty]
    public partial double PressureValue1 { get; set; }

    [ObservableProperty]
    public partial double PressureValue2 { get; set; }

    [ObservableProperty]
    public partial double PressureValue3 { get; set; }

    [ObservableProperty]
    public partial Point FindPosition { get; set; }

    #region Mapper

    public override AdsPressureGainsDTO Clone() => new()
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