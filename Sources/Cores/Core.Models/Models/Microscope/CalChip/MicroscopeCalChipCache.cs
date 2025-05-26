using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Stage;
using Net.Utilities.Attributes.DataAnnotations;
using Net.Utilities.Enums.Maths;

namespace Core.Models.Models.Microscope.CalChip;

public sealed partial class MicroscopeCalChipCache : CalibrationCacheBase
{
    private double _findFocusMinDsw = 1;
    private double _findFocusMinUndefined = 1;
    private double _findFocusMinHaze = 1;
    private double _findFocusMinShinyWafer = 1;
    private double _findFocusMaxDsw = 1;
    private double _findFocusMaxUndefined = 1;
    private double _findFocusMaxHaze = 1;
    private double _findFocusMaxShinyWafer = 1;
    private double _findFocusIntervalDsw = 1;
    private double _findFocusIntervalUndefined = 1;
    private double _findFocusIntervalHaze = 1;
    private double _findFocusIntervalShinyWafer = 1;
    private double _threshold = 1;

    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification5X;

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;


    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMinDsw: ")]
    public double FindFocusMinDsw
    {
        get => _findFocusMinDsw;
        set => SetProperty(ref _findFocusMinDsw, value, validate: true);
    }

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMinUndefined: ")]
    public double FindFocusMinUndefined
    {
        get => _findFocusMinUndefined;
        set => SetProperty(ref _findFocusMinUndefined, value);
    }

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMinHaze: ")]
    public double FindFocusMinHaze
    {
        get => _findFocusMinHaze;
        set => SetProperty(ref _findFocusMinHaze, value, validate: true);
    }

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMinShinyWafer: ")]
    public double FindFocusMinShinyWafer
    {
        get => _findFocusMinShinyWafer;
        set => SetProperty(ref _findFocusMinShinyWafer, value);
    }

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMaxDsw: ")]
    public double FindFocusMaxDsw
    {
        get => _findFocusMaxDsw;
        set => SetProperty(ref _findFocusMaxDsw, value, validate: true);
    }

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMaxUndefined: ")]
    public double FindFocusMaxUndefined
    {
        get => _findFocusMaxUndefined;
        set => SetProperty(ref _findFocusMaxUndefined, value, validate: true);
    }

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMaxHaze: ")]
    public double FindFocusMaxHaze
    {
        get => _findFocusMaxHaze;
        set => SetProperty(ref _findFocusMaxHaze, value, validate: true);
    }

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMaxShinyWafer: ")]
    public double FindFocusMaxShinyWafer
    {
        get => _findFocusMaxShinyWafer;
        set => SetProperty(ref _findFocusMaxShinyWafer, value, validate: true);
    }

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusIntervalDsw: ")]
    public double FindFocusIntervalDsw
    {
        get => _findFocusIntervalDsw;
        set => SetProperty(ref _findFocusIntervalDsw, value, validate: true);
    }

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusIntervalUndefined: ")]
    public double FindFocusIntervalUndefined
    {
        get => _findFocusIntervalUndefined;
        set => SetProperty(ref _findFocusIntervalUndefined, value, validate: true);
    }

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusIntervalHaze: ")]
    public double FindFocusIntervalHaze
    {
        get => _findFocusIntervalHaze;
        set => SetProperty(ref _findFocusIntervalHaze, value, validate: true);
    }

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusIntervalShinyWafer: ")]
    public double FindFocusIntervalShinyWafer
    {
        get => _findFocusIntervalShinyWafer;
        set => SetProperty(ref _findFocusIntervalShinyWafer, value, validate: true);
    }

    [ObservableProperty]
    private string _verifyResultQuality = string.Empty;

    [ObservableProperty]
    private string _verifyResultError = string.Empty;

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Threshold: ")]
    public double Threshold
    {
        get => _threshold;
        set => SetProperty(ref _threshold, value, validate: true);
    }

    public double GetFindFocusMin() => CalChipSiteModelEnum switch
    {
        CalChipSiteModelEnum.DswModel => FindFocusMinDsw,
        CalChipSiteModelEnum.UndefinedModel => FindFocusMinUndefined,
        CalChipSiteModelEnum.HazeModel => FindFocusMinHaze,
        CalChipSiteModelEnum.ShinyWaferModel => FindFocusMinShinyWafer,
        _ => throw new ArgumentOutOfRangeException()
    };

    public double GetFindFocusMax() => CalChipSiteModelEnum switch
    {
        CalChipSiteModelEnum.DswModel => FindFocusMaxDsw,
        CalChipSiteModelEnum.UndefinedModel => FindFocusMaxUndefined,
        CalChipSiteModelEnum.HazeModel => FindFocusMaxHaze,
        CalChipSiteModelEnum.ShinyWaferModel => FindFocusMaxShinyWafer,
        _ => throw new ArgumentOutOfRangeException()
    };

    public double GetFindFocusInterval() => CalChipSiteModelEnum switch
    {
        CalChipSiteModelEnum.DswModel => FindFocusIntervalDsw,
        CalChipSiteModelEnum.UndefinedModel => FindFocusIntervalUndefined,
        CalChipSiteModelEnum.HazeModel => FindFocusIntervalHaze,
        CalChipSiteModelEnum.ShinyWaferModel => FindFocusIntervalShinyWafer,
        _ => throw new ArgumentOutOfRangeException()
    };
}