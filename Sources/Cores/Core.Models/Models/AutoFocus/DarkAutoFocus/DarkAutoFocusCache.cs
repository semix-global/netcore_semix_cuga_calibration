using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.AutoFocus.DarkAutoFocus;

public sealed partial class DarkAutoFocusCache : CalibrationCacheBase<DarkAutoFocusCache>
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private Point _findPosition;

    #region Current

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingThresholdFMax), nameof(CalibratingThresholdFMin), nameof(ReviewThresholdFMax), nameof(ReviewThresholdFMin))]
    private double _thresholdIdealFMin = 6000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingThresholdFMax), nameof(CalibratingThresholdFMin), nameof(ReviewThresholdFMax), nameof(ReviewThresholdFMin))]
    private double _thresholdIdealFMax = 14000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingThresholdNMax), nameof(CalibratingThresholdNMin), nameof(ReviewThresholdNMax), nameof(ReviewThresholdNMin))]
    private double _thresholdIdealNMin = 18000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingThresholdNMax), nameof(CalibratingThresholdNMin), nameof(ReviewThresholdNMax), nameof(ReviewThresholdNMin))]
    private double _thresholdIdealNMax = 22000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingThresholdFMax), nameof(CalibratingThresholdFMin), nameof(CalibratingThresholdNMax), nameof(CalibratingThresholdNMin))]
    private double _calibratingThresholdRangeRatio = 0.5;

    public double CalibratingThresholdFMin => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d - (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * CalibratingThresholdRangeRatio;

    public double CalibratingThresholdFMax => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d + (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * CalibratingThresholdRangeRatio;

    public double CalibratingThresholdNMin => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d - (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * CalibratingThresholdRangeRatio;

    public double CalibratingThresholdNMax => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d + (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * CalibratingThresholdRangeRatio;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReviewThresholdFMax), nameof(ReviewThresholdFMin), nameof(ReviewThresholdNMax), nameof(ReviewThresholdNMin))]
    private double _reviewThresholdRangeRatio = 0.8;

    public double ReviewThresholdFMin => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d - (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * ReviewThresholdRangeRatio;

    public double ReviewThresholdFMax => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d + (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * ReviewThresholdRangeRatio;

    public double ReviewThresholdNMin => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d - (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * ReviewThresholdRangeRatio;

    public double ReviewThresholdNMax => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d + (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * ReviewThresholdRangeRatio;

    [ObservableProperty]
    private double _thresholdCurrentMin;

    [ObservableProperty]
    private double _thresholdCurrentMax = 5500;

    [ObservableProperty]
    private double _findCurrentStart;

    [ObservableProperty]
    private double _findCurrentStep = 100;

    [ObservableProperty]
    private double _findCurrentStop = 5000;

    [ObservableProperty]
    private double _lowCoefficient = 0.6d;

    [ObservableProperty]
    private double _highCoefficient = 1.5d;

    #endregion Current

    #region NSC

    [ObservableProperty]
    private double _halfEcsLength = 250;

    [ObservableProperty]
    private double _speedEcsPerSecond = 500;

    [ObservableProperty]
    private double _nscStandardNscPerNm = 1;

    [ObservableProperty]
    private double _thresholdNscStandardSymmetryRatio = 1.5;

    [ObservableProperty]
    private double _thresholdNscStandardGain = 1.5;

    [ObservableProperty]
    private double _calibrationThresholdNscSymmetryRatio = 1.05;

    [ObservableProperty]
    private double _calibrationThresholdNscNscPerNmRange = 0.05;

    [ObservableProperty]
    private int _retryCount = 10;

    [ObservableProperty]
    private double _startAFMotorAbsoluteValue;

    [ObservableProperty]
    private double _stepAFMotorAbsoluteValue;

    [ObservableProperty]
    private double _stopAFMotorAbsoluteValue;

    #endregion NSC

    public override DarkAutoFocusCache Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation,
        FindPosition = FindPosition,
        ThresholdIdealFMin = ThresholdIdealFMin,
        ThresholdIdealFMax = ThresholdIdealFMax,
        ThresholdIdealNMin = ThresholdIdealNMin,
        ThresholdIdealNMax = ThresholdIdealNMax,
        CalibratingThresholdRangeRatio = CalibratingThresholdRangeRatio,
        ReviewThresholdRangeRatio = ReviewThresholdRangeRatio,
        ThresholdCurrentMin = ThresholdCurrentMin,
        ThresholdCurrentMax = ThresholdCurrentMax,
        FindCurrentStart = FindCurrentStart,
        FindCurrentStep = FindCurrentStep,
        FindCurrentStop = FindCurrentStop,
        LowCoefficient = LowCoefficient,
        HighCoefficient = HighCoefficient,
        HalfEcsLength = HalfEcsLength,
        SpeedEcsPerSecond = SpeedEcsPerSecond,
        NscStandardNscPerNm = NscStandardNscPerNm,
        ThresholdNscStandardSymmetryRatio = ThresholdNscStandardSymmetryRatio,
        ThresholdNscStandardGain = ThresholdNscStandardGain,
        CalibrationThresholdNscSymmetryRatio = CalibrationThresholdNscSymmetryRatio,
        CalibrationThresholdNscNscPerNmRange = CalibrationThresholdNscNscPerNmRange,
        RetryCount = RetryCount,
        StartAFMotorAbsoluteValue = StartAFMotorAbsoluteValue,
        StepAFMotorAbsoluteValue = StepAFMotorAbsoluteValue,
        StopAFMotorAbsoluteValue = StopAFMotorAbsoluteValue,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}