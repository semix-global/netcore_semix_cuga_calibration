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

    [ObservableProperty]
    private double _nscOffset;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NscGainReciprocal))]
    private double _nscGain;

    public double NscGainReciprocal => NscGain == 0 ? 0 : 1 / NscGain;

    [ObservableProperty]
    private double _nscCurrentMax;

    [ObservableProperty]
    private double _nscCurrentMin;

    [ObservableProperty]
    private double _nscCurrentOffset;

    [ObservableProperty]
    private double _nscCurrentGain;

    [ObservableProperty]
    private List<double> _ecsData = [];

    [ObservableProperty]
    private List<double> _nscData = [];

    [ObservableProperty]
    private List<double> _lvdtData = [];

    #region Mapper

    public LaserAutoFocusDto Clone() => new()
    {
        CurrentA = CurrentA,
        Fa = Fa,
        Na = Na,
        CurrentB = CurrentB,
        Fb = Fb,
        Nb = Nb,
        NscOffset = NscOffset,
        NscGain = NscGain,
        NscCurrentMax = NscCurrentMax,
        NscCurrentMin = NscCurrentMin,
        EcsData = [.. EcsData],
        NscData = [.. NscData],
        LvdtData = [.. LvdtData],
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
        NscOffset = NscOffset,
        NscGain = NscGain,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}