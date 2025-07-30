using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

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
    private double _afEcsErrorThreshold = 1;
    private double _afMotorErrorThreshold = 0.1;

    [ObservableProperty]
    private MicroscopeMagnificationInfo _microscopeMagnificationInfo = new();

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;


    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMinDsw: ")]
    public double FindFocusMinDsw
    {
        get => _findFocusMinDsw;
        set => SetProperty(ref _findFocusMinDsw, value, validate: true);
    }

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMinUndefined: ")]
    public double FindFocusMinUndefined
    {
        get => _findFocusMinUndefined;
        set => SetProperty(ref _findFocusMinUndefined, value);
    }

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMinHaze: ")]
    public double FindFocusMinHaze
    {
        get => _findFocusMinHaze;
        set => SetProperty(ref _findFocusMinHaze, value, validate: true);
    }

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMinShinyWafer: ")]
    public double FindFocusMinShinyWafer
    {
        get => _findFocusMinShinyWafer;
        set => SetProperty(ref _findFocusMinShinyWafer, value);
    }

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMaxDsw: ")]
    public double FindFocusMaxDsw
    {
        get => _findFocusMaxDsw;
        set => SetProperty(ref _findFocusMaxDsw, value, validate: true);
    }

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMaxUndefined: ")]
    public double FindFocusMaxUndefined
    {
        get => _findFocusMaxUndefined;
        set => SetProperty(ref _findFocusMaxUndefined, value, validate: true);
    }

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMaxHaze: ")]
    public double FindFocusMaxHaze
    {
        get => _findFocusMaxHaze;
        set => SetProperty(ref _findFocusMaxHaze, value, validate: true);
    }

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMaxShinyWafer: ")]
    public double FindFocusMaxShinyWafer
    {
        get => _findFocusMaxShinyWafer;
        set => SetProperty(ref _findFocusMaxShinyWafer, value, validate: true);
    }

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusIntervalDsw: ")]
    public double FindFocusIntervalDsw
    {
        get => _findFocusIntervalDsw;
        set => SetProperty(ref _findFocusIntervalDsw, value, validate: true);
    }

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusIntervalUndefined: ")]
    public double FindFocusIntervalUndefined
    {
        get => _findFocusIntervalUndefined;
        set => SetProperty(ref _findFocusIntervalUndefined, value, validate: true);
    }

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusIntervalHaze: ")]
    public double FindFocusIntervalHaze
    {
        get => _findFocusIntervalHaze;
        set => SetProperty(ref _findFocusIntervalHaze, value, validate: true);
    }

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusIntervalShinyWafer: ")]
    public double FindFocusIntervalShinyWafer
    {
        get => _findFocusIntervalShinyWafer;
        set => SetProperty(ref _findFocusIntervalShinyWafer, value, validate: true);
    }

    [ObservableProperty]
    private string _verifyResultQuality = string.Empty;

    [ObservableProperty]
    private string _verifyResultError = string.Empty;

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Threshold: ")]
    public double Threshold
    {
        get => _threshold;
        set => SetProperty(ref _threshold, value, validate: true);
    }

    [Comparison(20d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Af Ecs Error Threshold: ")]
    public double AfEcsErrorThreshold
    {
        get => _afEcsErrorThreshold;
        set => SetProperty(ref _afEcsErrorThreshold, value, validate: true);
    }

    [Comparison(5d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Af Motor Error Threshold: ")]
    public double AfMotorErrorThreshold
    {
        get => _afMotorErrorThreshold;
        set => SetProperty(ref _afMotorErrorThreshold, value, validate: true);
    }

    #region Position

    [ObservableProperty]
    private Point _chuckPosition = Point.Origin;

    #region DSW

    [ObservableProperty]
    private Point _dswLeftTopPosition = new(122000, 128000);

    [ObservableProperty]
    private Point _dswRightBottomPosition = new Point(122000, 128000);

    public Point DswPosition => (DswLeftTopPosition + (Vector)DswRightBottomPosition) / 2;

    #endregion

    #region Undefined

    [ObservableProperty]
    private Point _undefinedLeftTopPosition = new(-122000, 128000);

    [ObservableProperty]
    private Point _undefinedRightBottomPosition = new(-122000, 128000);

    public Point UndefinedPosition => (UndefinedLeftTopPosition + (Vector)UndefinedRightBottomPosition) / 2;

    #endregion

    #region Haze

    [ObservableProperty]
    private Point _hazeLeftTopPosition = new(-122000, -128000);

    [ObservableProperty]
    private Point _hazeRightBottomPosition = new(-122000, -128000);

    public Point HazePosition => (HazeLeftTopPosition + (Vector)HazeRightBottomPosition) / 2;

    #endregion

    #region ShinyWafer

    [ObservableProperty]
    private Point _shinyWaferLeftTopPosition = new(122000, -128000);

    [ObservableProperty]
    private Point _shinyWaferRightBottomPosition = new(122000, -128000);

    public Point ShinyWaferPosition => (ShinyWaferLeftTopPosition + (Vector)ShinyWaferRightBottomPosition) / 2;
    #endregion

    #endregion

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

    public Point GetFindPosition() => CalChipSiteModelEnum switch
    {
        CalChipSiteModelEnum.DswModel => DswPosition,
        CalChipSiteModelEnum.UndefinedModel => UndefinedPosition,
        CalChipSiteModelEnum.HazeModel => HazePosition,
        CalChipSiteModelEnum.ShinyWaferModel => ShinyWaferPosition,
        _ => throw new ArgumentOutOfRangeException()
    };

    public Point GetLeftTopPosition() => CalChipSiteModelEnum switch
    {
        CalChipSiteModelEnum.DswModel => DswLeftTopPosition,
        CalChipSiteModelEnum.UndefinedModel => UndefinedLeftTopPosition,
        CalChipSiteModelEnum.HazeModel => HazeLeftTopPosition,
        CalChipSiteModelEnum.ShinyWaferModel => ShinyWaferLeftTopPosition,
        _ => throw new ArgumentOutOfRangeException()
    };

    public Point GetRightBottomPosition() => CalChipSiteModelEnum switch
    {
        CalChipSiteModelEnum.DswModel => DswRightBottomPosition,
        CalChipSiteModelEnum.UndefinedModel => UndefinedRightBottomPosition,
        CalChipSiteModelEnum.HazeModel => HazeRightBottomPosition,
        CalChipSiteModelEnum.ShinyWaferModel => ShinyWaferRightBottomPosition,
        _ => throw new ArgumentOutOfRangeException()
    };
}