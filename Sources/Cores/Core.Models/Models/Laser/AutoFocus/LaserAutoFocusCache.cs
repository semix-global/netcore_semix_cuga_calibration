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
    [NotifyPropertyChangedFor(nameof(ThresholdFMax), nameof(ThresholdFMin), nameof(ThresholdNMax), nameof(ThresholdNMin))]
    private double _thresholdRangeRatio = 0.8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThresholdFMax), nameof(ThresholdFMin))]
    private double _thresholdIdealFMin = 6000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThresholdFMax), nameof(ThresholdFMin))]
    private double _thresholdIdealFMax = 14000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThresholdNMax), nameof(ThresholdNMin))]
    private double _thresholdIdealNMin = 18000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThresholdNMax), nameof(ThresholdNMin))]
    private double _thresholdIdealNMax = 22000;

    public double ThresholdFMin => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d - (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * ThresholdRangeRatio;

    public double ThresholdFMax => (ThresholdIdealFMax + ThresholdIdealFMin) / 2d + (ThresholdIdealFMax - ThresholdIdealFMin) / 2d * ThresholdRangeRatio;

    public double ThresholdNMin => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d - (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * ThresholdRangeRatio;

    public double ThresholdNMax => (ThresholdIdealNMax + ThresholdIdealNMin) / 2d + (ThresholdIdealNMax - ThresholdIdealNMin) / 2d * ThresholdRangeRatio;

    [ObservableProperty]
    private double _thresholdCurrentMin;

    [ObservableProperty]
    private double _thresholdCurrentMax = 550;

    [ObservableProperty]
    private double _findInterval = 100;

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