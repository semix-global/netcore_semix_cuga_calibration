using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.AutoFocus.DarkAutoFocus;

public sealed partial class DarkAutoFocusCache : CalibrationCacheBase<DarkAutoFocusCache>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial Point FindPosition { get; set; }

    #region Current

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingThresholdFMax), nameof(CalibratingThresholdFMin), nameof(ReviewThresholdFMax), nameof(ReviewThresholdFMin))]
    public partial double ThresholdIdealFMin { get; set; } = 6000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingThresholdFMax), nameof(CalibratingThresholdFMin), nameof(ReviewThresholdFMax), nameof(ReviewThresholdFMin))]
    public partial double ThresholdIdealFMax { get; set; } = 14000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingThresholdNMax), nameof(CalibratingThresholdNMin), nameof(ReviewThresholdNMax), nameof(ReviewThresholdNMin))]
    public partial double ThresholdIdealNMin { get; set; } = 18000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingThresholdNMax), nameof(CalibratingThresholdNMin), nameof(ReviewThresholdNMax), nameof(ReviewThresholdNMin))]
    public partial double ThresholdIdealNMax { get; set; } = 22000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingThresholdFMax), nameof(CalibratingThresholdFMin), nameof(CalibratingThresholdNMax), nameof(CalibratingThresholdNMin))]
    public partial double CalibratingThresholdRangeRatio { get; set; } = 0.5;

    public double CalibratingThresholdFMin => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d - (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * CalibratingThresholdRangeRatio;

    public double CalibratingThresholdFMax => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d + (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * CalibratingThresholdRangeRatio;

    public double CalibratingThresholdNMin => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d - (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * CalibratingThresholdRangeRatio;

    public double CalibratingThresholdNMax => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d + (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * CalibratingThresholdRangeRatio;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReviewThresholdFMax), nameof(ReviewThresholdFMin), nameof(ReviewThresholdNMax), nameof(ReviewThresholdNMin))]
    public partial double ReviewThresholdRangeRatio { get; set; } = 0.8;

    public double ReviewThresholdFMin => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d - (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * ReviewThresholdRangeRatio;

    public double ReviewThresholdFMax => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d + (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * ReviewThresholdRangeRatio;

    public double ReviewThresholdNMin => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d - (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * ReviewThresholdRangeRatio;

    public double ReviewThresholdNMax => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d + (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * ReviewThresholdRangeRatio;

    [ObservableProperty]
    public partial double ThresholdCurrentMin { get; set; }

    [ObservableProperty]
    public partial double ThresholdCurrentMax { get; set; } = 5500;

    [ObservableProperty]
    public partial double FindCurrentStart { get; set; }

    [ObservableProperty]
    public partial double FindCurrentStep { get; set; } = 100;

    [ObservableProperty]
    public partial double FindCurrentStop { get; set; } = 5000;

    [ObservableProperty]
    public partial double LowCoefficient { get; set; } = 0.6d;

    [ObservableProperty]
    public partial double HighCoefficient { get; set; } = 1.5d;

    #endregion Current

    #region NSC

    [ObservableProperty]
    public partial double HalfEcsLength { get; set; } = 250;

    [ObservableProperty]
    public partial double SpeedEcsPerSecond { get; set; } = 500;

    [ObservableProperty]
    public partial double NscStandardNscPerNm { get; set; } = 1;

    [ObservableProperty]
    public partial double ThresholdNscStandardSymmetryRatio { get; set; } = 1.5;

    [ObservableProperty]
    public partial double ThresholdNscStandardGain { get; set; } = 1.5;

    [ObservableProperty]
    public partial double CalibrationThresholdNscSymmetryRatio { get; set; } = 1.05;

    [ObservableProperty]
    public partial double CalibrationThresholdNscNscPerNmRange { get; set; } = 0.05;

    [ObservableProperty]
    public partial int RetryCount { get; set; } = 10;

    [ObservableProperty]
    public partial double StartAFMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double StepAFMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double StopAFMotorAbsoluteValue { get; set; }

    #endregion NSC

    public override DarkAutoFocusCache Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
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
        Expiration = Expiration
    };
}