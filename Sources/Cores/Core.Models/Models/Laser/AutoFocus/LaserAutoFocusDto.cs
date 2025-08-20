using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Laser.AutoFocus;

public sealed partial class LaserAutoFocusDto : CalibrationDtoBase, ICloneable<LaserAutoFocusDto>, IAdaptTo<CalibrationLaserAutoFocus>
{
    #region Current

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

    #endregion Current

    #region NSC

    #region NSC Profile

    [ObservableProperty]
    private bool _isNscUseMaxValue;

    [ObservableProperty]
    private bool _isNscUsePositiveSlope;

    [ObservableProperty]
    private double _ecsToNmRange;

    [ObservableProperty]
    private double _nscStandard;

    [ObservableProperty]
    private ImmutableArray<double> _originalEcs = [];

    [ObservableProperty]
    private ImmutableArray<double> _originalNsc = [];

    [ObservableProperty]
    private ImmutableArray<double> _originalLvdt = [];

    #endregion NSC Profile

    #region NscGain

    [ObservableProperty]
    private double _nscOffset;

    [ObservableProperty]
    private double _nscGain;

    [ObservableProperty]
    private double _nscCurrentNscPerNm;

    [ObservableProperty]
    private double _nscCurrentSymmetryRatio;

    [ObservableProperty]
    private ImmutableArray<double> _calibrationEcs = [];

    [ObservableProperty]
    private ImmutableArray<double> _calibrationNsc = [];

    [ObservableProperty]
    private ImmutableArray<double> _calibrationLvdt = [];

    #endregion NscGain

    #endregion NSC

    #region Mapper

    public LaserAutoFocusDto Clone() => new()
    {
        CurrentA = CurrentA,
        Fa = Fa,
        Na = Na,
        CurrentB = CurrentB,
        Fb = Fb,
        Nb = Nb,
        IsNscUseMaxValue = IsNscUseMaxValue,
        IsNscUsePositiveSlope = IsNscUsePositiveSlope,
        EcsToNmRange = EcsToNmRange,
        NscStandard = NscStandard,
        OriginalEcs = [..OriginalEcs],
        OriginalNsc = [..OriginalNsc],
        OriginalLvdt = [..OriginalLvdt],
        NscOffset = NscOffset,
        NscGain = NscGain,
        NscCurrentNscPerNm = NscCurrentNscPerNm,
        NscCurrentSymmetryRatio = NscCurrentSymmetryRatio,
        CalibrationEcs = [..CalibrationEcs],
        CalibrationNsc = [..CalibrationNsc],
        CalibrationLvdt = [..CalibrationLvdt],
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
        NscGain = NscGain,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}