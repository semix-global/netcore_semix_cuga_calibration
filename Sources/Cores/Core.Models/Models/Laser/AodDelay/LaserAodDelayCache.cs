using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Net.Utilities.Models;

namespace Core.Models.Models.Laser.AodDelay;

public sealed partial class LaserAodDelayCache : CalibrationCacheBase
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private double _waitTime = 5;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private double _roughAodDelayMinLow = -1500;

    [ObservableProperty]
    private double _roughAodDelayMinMiddle = -1500;

    [ObservableProperty]
    private double _roughAodDelayMinHigh = -1500;

    [ObservableProperty]
    private double _roughAodDelayMaxLow = 1500;

    [ObservableProperty]
    private double _roughAodDelayMaxMiddle = 1500;

    [ObservableProperty]
    private double _roughAodDelayMaxHigh = 1500;

    [ObservableProperty]
    private double _roughFindIntervalLow = 100;

    [ObservableProperty]
    private double _roughFindIntervalMiddle = 100;

    [ObservableProperty]
    private double _roughFindIntervalHigh = 100;

    [ObservableProperty]
    private double _refinedRangeLow = 200;

    [ObservableProperty]
    private double _refinedRangeMiddle = 200;

    [ObservableProperty]
    private double _refinedRangeHigh = 200;

    [ObservableProperty]
    private double _refinedFindIntervalLow = 10;

    [ObservableProperty]
    private double _refinedFindIntervalMiddle = 10;

    [ObservableProperty]
    private double _refinedFindIntervalHigh = 10;

    [ObservableProperty]
    private double _threshold;

    public double GetRoughAodDelayMin() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => RoughAodDelayMinLow,
        OpticsMagTypeEnum.Middle => RoughAodDelayMinMiddle,
        OpticsMagTypeEnum.High => RoughAodDelayMinHigh,
        _ => throw new ArgumentOutOfRangeException()
    };

    public double GetRoughAodDelayMax() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => RoughAodDelayMaxLow,
        OpticsMagTypeEnum.Middle => RoughAodDelayMaxMiddle,
        OpticsMagTypeEnum.High => RoughAodDelayMaxHigh,
        _ => throw new ArgumentOutOfRangeException()
    };

    public double GetRoughFindInterval() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => RoughFindIntervalLow,
        OpticsMagTypeEnum.Middle => RoughFindIntervalMiddle,
        OpticsMagTypeEnum.High => RoughFindIntervalHigh,
        _ => throw new ArgumentOutOfRangeException()
    };

    public double GetRefinedRange() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => RefinedRangeLow,
        OpticsMagTypeEnum.Middle => RefinedRangeMiddle,
        OpticsMagTypeEnum.High => RefinedRangeHigh,
        _ => throw new ArgumentOutOfRangeException()
    };

    public double GetRefinedFindInterval() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => RefinedFindIntervalLow,
        OpticsMagTypeEnum.Middle => RefinedFindIntervalMiddle,
        OpticsMagTypeEnum.High => RefinedFindIntervalHigh,
        _ => throw new ArgumentOutOfRangeException()
    };
}