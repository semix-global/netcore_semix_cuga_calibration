using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.BeamStabilizer;

[CacheVersion("1.0.0")]
public sealed partial class LaserBeamStabilizerObjDto : CalibrationDTOBase<LaserBeamStabilizerObjDto>
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private Point _currentPDPosition1;

    [ObservableProperty]
    private Point _currentPDPosition2;

    [ObservableProperty]
    private Point _originPosition1;

    [ObservableProperty]
    private Point _originPosition2;

    [ObservableProperty]
    private int _interval;

    [ObservableProperty]
    private string _filePath = string.Empty;

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