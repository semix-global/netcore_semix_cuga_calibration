using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

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
    private double _originalSymmetryRatio;

    [ObservableProperty]
    private double _ecsToNmRange;

    [ObservableProperty]
    private double _nscStandard;

    [ObservableProperty]
    private IReadOnlyList<double> _originalEcs = [];

    [ObservableProperty]
    private IReadOnlyList<double> _originalNsc = [];

    [ObservableProperty]
    private IReadOnlyList<double> _originalLvdt = [];

    [ObservableProperty]
    private IReadOnlyList<double> _originalFa = [];

    [ObservableProperty]
    private IReadOnlyList<double> _originalNa = [];

    [ObservableProperty]
    private IReadOnlyList<double> _originalFb = [];

    [ObservableProperty]
    private IReadOnlyList<double> _originalNb = [];

    [ObservableProperty]
    private Point[] _originEcsNscPoints = [];

    [ObservableProperty]
    private Point[] _originEcsNscMaxMins = [];

    [ObservableProperty]
    private double _ecsMotorPositionRelationSlope;

    [ObservableProperty]
    private double _ecsMotorPositionRelationIntercept;

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
    private IReadOnlyList<double> _calibrationEcs = [];

    [ObservableProperty]
    private IReadOnlyList<double> _calibrationNsc = [];

    [ObservableProperty]
    private IReadOnlyList<double> _calibrationLvdt = [];

    [ObservableProperty]
    private IReadOnlyList<double> _calibrationFa = [];

    [ObservableProperty]
    private IReadOnlyList<double> _calibrationNa = [];

    [ObservableProperty]
    private IReadOnlyList<double> _calibrationFb = [];

    [ObservableProperty]
    private IReadOnlyList<double> _calibrationNb = [];

    [ObservableProperty]
    private Point[] _calibrationEcsNscPoints = [];

    [ObservableProperty]
    private Point[] _calibrationEcsNscMaxMins = [];

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
        OriginalSymmetryRatio = OriginalSymmetryRatio,
        EcsToNmRange = EcsToNmRange,
        NscStandard = NscStandard,
        OriginalEcs = [.. OriginalEcs],
        OriginalNsc = [.. OriginalNsc],
        OriginalLvdt = [.. OriginalLvdt],
        NscOffset = NscOffset,
        NscGain = NscGain,
        NscCurrentNscPerNm = NscCurrentNscPerNm,
        NscCurrentSymmetryRatio = NscCurrentSymmetryRatio,
        CalibrationEcs = [.. CalibrationEcs],
        CalibrationNsc = [.. CalibrationNsc],
        CalibrationLvdt = [.. CalibrationLvdt],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        EcsMotorPositionRelationSlope = EcsMotorPositionRelationSlope,
        EcsMotorPositionRelationIntercept = EcsMotorPositionRelationIntercept,
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
        IsRequiredCalibrate = IsRequiredSelfCheck,
        EcsMotorPositionRelationSlope = EcsMotorPositionRelationSlope,
        EcsMotorPositionRelationIntercept = EcsMotorPositionRelationIntercept
    };

    #endregion Mapper
}