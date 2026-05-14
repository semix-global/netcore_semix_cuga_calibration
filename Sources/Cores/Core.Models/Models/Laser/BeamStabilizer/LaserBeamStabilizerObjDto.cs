using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.BeamStabilizer;

[CacheVersion("1.0.0")]
public sealed partial class LaserBeamStabilizerObjDto : CalibrationDTOBase<LaserBeamStabilizerObjDto>
{
    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    public partial Point CurrentPDPosition1 { get; set; }

    [ObservableProperty]
    public partial Point CurrentPDPosition2 { get; set; }

    [ObservableProperty]
    public partial Point OriginPosition1 { get; set; }

    [ObservableProperty]
    public partial Point OriginPosition2 { get; set; }

    [ObservableProperty]
    public partial int Interval { get; set; }

    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;

    #region Mapper

    public override LaserBeamStabilizerObjDto Clone() => new()
    {
        Index = Index,
        CurrentPDPosition1 = CurrentPDPosition1,
        CurrentPDPosition2 = CurrentPDPosition2,
        OriginPosition1 = OriginPosition1,
        OriginPosition2 = OriginPosition2,
        Interval = Interval,
        FilePath = FilePath,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    #endregion Mapper
}