using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Pattern;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.AutoFocus;

public sealed partial class LaserAutoFocusCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeMagnificationInfo _microscopeMagnificationInfo = new();

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private Point _findPosition;

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
    private double _thresholdCurrentMin;

    [ObservableProperty]
    private double _thresholdCurrentMax = 550;

    [ObservableProperty]
    private double _findInterval = 100;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrationThresholdFMax), nameof(CalibrationThresholdFMin), nameof(CalibrationThresholdNMax), nameof(CalibrationThresholdNMin))]
    private double _calibrationThresholdRangeRation = 0.8;

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
    private double _halfEcsLength = 250;

    /// <summary>
    /// Ecs/s
    /// </summary>
    [ObservableProperty]
    private double _speedEcs = 100;

    [ObservableProperty]
    private double _nscStandardValue = 5000;

    [ObservableProperty]
    private double _calibrationNscCenterOffset = 150;

    [ObservableProperty]
    private double _calibrationNscSideOffset = 500;

    [ObservableProperty]
    private double _thresholdNscStandardCenterOffset = 1000;

    [ObservableProperty]
    private double _thresholdNscStandardGain = 1.4;

    [ObservableProperty]
    private int _retryCount = 5;
}