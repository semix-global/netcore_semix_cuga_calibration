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
    [NotifyPropertyChangedFor(nameof(CalibrateThresholdFMax), nameof(CalibrateThresholdFMin), nameof(ReviewThresholdFMax), nameof(ReviewThresholdFMin))]
    private double _thresholdIdealFMin = 6000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrateThresholdFMax), nameof(CalibrateThresholdFMin), nameof(ReviewThresholdFMax), nameof(ReviewThresholdFMin))]
    private double _thresholdIdealFMax = 14000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrateThresholdNMax), nameof(CalibrateThresholdNMin), nameof(ReviewThresholdNMax), nameof(ReviewThresholdNMin))]
    private double _thresholdIdealNMin = 18000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrateThresholdNMax), nameof(CalibrateThresholdNMin), nameof(ReviewThresholdNMax), nameof(ReviewThresholdNMin))]
    private double _thresholdIdealNMax = 22000;

    [ObservableProperty]
    private double _thresholdCurrentMin;

    [ObservableProperty]
    private double _thresholdCurrentMax = 550;

    [ObservableProperty]
    private double _findInterval = 100;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrateThresholdFMax), nameof(CalibrateThresholdFMin), nameof(CalibrateThresholdNMax), nameof(CalibrateThresholdNMin))]
    private double _calibrateThresholdRangeRatio = 0.8;

    public double CalibrateThresholdFMin => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d - (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * CalibrateThresholdRangeRatio;

    public double CalibrateThresholdFMax => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d + (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * CalibrateThresholdRangeRatio;

    public double CalibrateThresholdNMin => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d - (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * CalibrateThresholdRangeRatio;

    public double CalibrateThresholdNMax => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d + (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * CalibrateThresholdRangeRatio;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReviewThresholdFMax), nameof(ReviewThresholdFMin), nameof(ReviewThresholdNMax), nameof(ReviewThresholdNMin))]
    private double _reviewThresholdRangeRatio = 0.8;

    public double ReviewThresholdFMin => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d - (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * ReviewThresholdRangeRatio;

    public double ReviewThresholdFMax => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d + (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * ReviewThresholdRangeRatio;

    public double ReviewThresholdNMin => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d - (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * ReviewThresholdRangeRatio;

    public double ReviewThresholdNMax => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d + (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * ReviewThresholdRangeRatio;

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
    private double _thresholdNscOffset = 150;

    [ObservableProperty]
    private double _thresholdNscGain = 500;

    [ObservableProperty]
    private int _retryCount = 5;
}