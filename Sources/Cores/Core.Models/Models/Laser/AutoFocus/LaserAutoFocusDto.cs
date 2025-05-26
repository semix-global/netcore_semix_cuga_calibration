using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Laser.AutoFocus;

public sealed partial class LaserAutoFocusDto : CalibrationDtoBase, ICloneable<LaserAutoFocusDto>, IAdaptTo<CalibrationLaserAutoFocus>
{
    [ObservableProperty]
    private double _currentA;

    [ObservableProperty]
    private double _fa;

    [ObservableProperty]
    private double _na;

    [ObservableProperty]
    private double _currentB;

    [ObservableProperty]
    private double _fb;

    [ObservableProperty]
    private double _nb;

    #region Mapper

    public LaserAutoFocusDto Clone() => new()
    {
        CurrentA = CurrentA,
        Fa = Fa,
        Na = Na,
        CurrentB = CurrentB,
        Fb = Fb,
        Nb = Nb,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserAutoFocus AdaptTo() => new()
    {
        CurrentA = CurrentA,
        CurrentB = CurrentB,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}