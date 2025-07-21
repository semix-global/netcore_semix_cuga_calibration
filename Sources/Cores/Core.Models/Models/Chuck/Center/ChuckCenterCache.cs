using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.Center;

public sealed partial class ChuckCenterCache : CalibrationCacheBase
{
    private double _positiveAngle = 1d;
    private double _negativeAngle = -1d;
    private int _threshold = 50;

    [ObservableProperty]
    private ChuckCenterCacheItem _lowChuckCenterCacheItem = new();

    [ObservableProperty]
    private ChuckCenterCacheItem _highChuckCenterCacheItem = new();

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.DieCorner_LeftBottom;

    [ObservableProperty]
    private double _p5Angle;

    [ComparisonRange(0, 50, NumberComparisonRangeTypeEnum.LeftOpenAndRightClosedInterval, ErrorMessage = "Threshold: ")]
    public int Threshold
    {
        get => _threshold;
        set => SetProperty(ref _threshold, value, true);
    }

    [ComparisonRange(0d, 1d, NumberComparisonRangeTypeEnum.LeftOpenAndRightClosedInterval, ErrorMessage = "Positive Angle: ")]
    public double PositiveAngle
    {
        get => _positiveAngle;
        set => SetProperty(ref _positiveAngle, value, true);
    }

    [ObservableProperty]
    private double _thetaAngle;


    [ComparisonRange(-1d, 0d, NumberComparisonRangeTypeEnum.LeftClosedAndRightOpenInterval, ErrorMessage = "Negative Angle: ")]
    public double NegativeAngle
    {
        get => _negativeAngle;
        set => SetProperty(ref _negativeAngle, value, true);
    }

    public Point LowToHighPointTop => HighChuckCenterCacheItem.TopPosition - (Vector)LowChuckCenterCacheItem.TopPosition;

    public Point LowToHighPointRight => HighChuckCenterCacheItem.RightPosition - (Vector)LowChuckCenterCacheItem.RightPosition;

    public Point LowToHighPointBottom => HighChuckCenterCacheItem.BottomPosition - (Vector)LowChuckCenterCacheItem.BottomPosition;

    public Point LowToHighPointLeft => HighChuckCenterCacheItem.LeftPosition - (Vector)LowChuckCenterCacheItem.LeftPosition;

    #region Verify

    public (bool IsSuccess, string ErrorMessage) Step1Verify()
    {
        ClearErrors();
        ValidateProperty(PositiveAngle, nameof(PositiveAngle));

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }

    public (bool IsSuccess, string ErrorMessage) Step2Verify()
    {
        ClearErrors();
        ValidateProperty(NegativeAngle, nameof(NegativeAngle));

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }

    #endregion
}