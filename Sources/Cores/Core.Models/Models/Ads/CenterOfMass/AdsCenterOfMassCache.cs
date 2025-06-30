using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Ads.CenterOfMass;

public sealed partial class AdsCenterOfMassCache : CalibrationCacheBase
{
    /// <summary>
    /// X Speed Value 单位mm/s
    /// </summary>
    public const double XSpeedValue = 300;

    /// <summary>
    /// Y Speed Value 单位mm/s 过大会让轴掉使能
    /// </summary>
    public const double YSpeedValue = 200;

    [ObservableProperty]
    private double _xspeedValue = 300; // 诊断使用

    [ObservableProperty]
    private double _yspeedValue = 300; // 诊断使用

    [ObservableProperty]
    private double _xSpeedForwardValuePositiveX1 = 50;

    [ObservableProperty]
    private double _xSpeedForwardValuePositiveX2 = 50;

    [ObservableProperty]
    private double _xSpeedForwardValueNegativeX3 = 50;

    [ObservableProperty]
    private double _xSpeedForwardValueNegativeX4 = 50;

    [ObservableProperty]
    private double _ySpeedForwardValuePositiveY1 = 50;

    [ObservableProperty]
    private double _ySpeedForwardValuePositiveY2 = 50;

    [ObservableProperty]
    private double _ySpeedForwardValuePositiveY3 = 50;

    [ObservableProperty]
    private double _ySpeedForwardValueNegativeY4 = 50;

    [ObservableProperty]
    private double _ySpeedForwardValueNegativeY5 = 50;

    [ObservableProperty]
    private double _ySpeedForwardValueNegativeY6 = 50;

    [ObservableProperty]
    private StageSpeedEnum _stageSpeedEnum = StageSpeedEnum.High;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.Low;

    [ObservableProperty]
    private int _waitTime = 5;

    [ObservableProperty]
    private int _findCountX = 5;

    [ObservableProperty]
    private int _findCountY = 5;

    [ObservableProperty]
    private bool _isFindX = true;

    [ObservableProperty]
    private Point _centroid = new(0, 0);

    [ObservableProperty]
    private double _threshold;

    [ObservableProperty]
    private Point _startPositionFindX;

    [ObservableProperty]
    private Point _endPositionFindX;

    [ObservableProperty]
    private Point _startPositionFindY;

    [ObservableProperty]
    private Point _endPositionFindY;

    #region Get Position

    public Point GetStartPosition()
    {
        return IsFindX ? StartPositionFindX : StartPositionFindY;
    }

    public Point GetEndPosition()
    {
        return IsFindX ? EndPositionFindX : EndPositionFindY;
    }

    public (double value1, double value2, double value3) GetForwardValue(bool isPositive)
    {
        (double, double, double) forward;
        if (IsFindX)
            forward = isPositive ? (YSpeedForwardValuePositiveY1, YSpeedForwardValuePositiveY2, YSpeedForwardValuePositiveY3) : (YSpeedForwardValueNegativeY4, YSpeedForwardValueNegativeY5, YSpeedForwardValueNegativeY6);
        else
            forward = isPositive ? (XSpeedForwardValuePositiveX1, XSpeedForwardValuePositiveX2, 0) : (XSpeedForwardValueNegativeX3, XSpeedForwardValueNegativeX4, 0);
        return forward;
    }

    #endregion Get Position

    #region Set Position

    public void SetStartPosition(Point point)
    {
        _ = IsFindX
            ? StartPositionFindX = point
            : StartPositionFindY = point;
    }

    public void SetEndPosition(Point point)
    {
        _ = IsFindX
            ? EndPositionFindX = point
            : EndPositionFindY = point;
    }

    #endregion Set Position

    public void SetCentroid(Point point)
    {
        _ = IsFindX
            ? Centroid = new Point(point.X, 0)
            : Centroid = new Point(Centroid.X, point.Y);
    }
}