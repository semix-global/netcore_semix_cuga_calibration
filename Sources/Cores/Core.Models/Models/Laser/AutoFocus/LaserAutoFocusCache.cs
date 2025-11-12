using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.AutoFocus;

public sealed partial class LaserAutoFocusCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private Point _findPosition;

    #region Current

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrationThresholdFMax), nameof(CalibrationThresholdFMin), nameof(ReviewThresholdFMax), nameof(ReviewThresholdFMin))]
    private double _thresholdIdealFMin = 6000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrationThresholdFMax), nameof(CalibrationThresholdFMin), nameof(ReviewThresholdFMax), nameof(ReviewThresholdFMin))]
    private double _thresholdIdealFMax = 14000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrationThresholdNMax), nameof(CalibrationThresholdNMin), nameof(ReviewThresholdNMax), nameof(ReviewThresholdNMin))]
    private double _thresholdIdealNMin = 18000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrationThresholdNMax), nameof(CalibrationThresholdNMin), nameof(ReviewThresholdNMax), nameof(ReviewThresholdNMin))]
    private double _thresholdIdealNMax = 22000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrationThresholdFMax), nameof(CalibrationThresholdFMin), nameof(CalibrationThresholdNMax), nameof(CalibrationThresholdNMin))]
    private double _calibrationThresholdRangeRation = 0.5;

    public double CalibrationThresholdFMin => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d - (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * CalibrationThresholdRangeRation;

    public double CalibrationThresholdFMax => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d + (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * CalibrationThresholdRangeRation;

    public double CalibrationThresholdNMin => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d - (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * CalibrationThresholdRangeRation;

    public double CalibrationThresholdNMax => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d + (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * CalibrationThresholdRangeRation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReviewThresholdFMax), nameof(ReviewThresholdFMin), nameof(ReviewThresholdNMax), nameof(ReviewThresholdNMin))]
    private double _reviewThresholdRangeRation = 0.8;

    public double ReviewThresholdFMin => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d - (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * ReviewThresholdRangeRation;

    public double ReviewThresholdFMax => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d + (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * ReviewThresholdRangeRation;

    public double ReviewThresholdNMin => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d - (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * ReviewThresholdRangeRation;

    public double ReviewThresholdNMax => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d + (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * ReviewThresholdRangeRation;

    [ObservableProperty]
    private double _thresholdCurrentMin;

    [ObservableProperty]
    private double _thresholdCurrentMax = 5500;

    [ObservableProperty]
    private double _findCurrentStep = 100;

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
    private double _currentMotorPosition = 0;

    [ObservableProperty]
    private double _increateMotorPosition;

    [ObservableProperty]
    private double[] _increateEcsAverage = [];

    [ObservableProperty]
    public Point[] _ecsMotorOriginPositionList = [];

    [ObservableProperty]
    public Point[] _ecsMotorSmoothPositionList = [];

    #endregion NSC
}