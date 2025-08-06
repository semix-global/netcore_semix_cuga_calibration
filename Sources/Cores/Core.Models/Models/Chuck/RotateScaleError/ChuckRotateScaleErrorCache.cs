using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;

namespace Core.Models.Models.Chuck.RotateScaleError;

public sealed partial class ChuckRotateScaleErrorCache : CalibrationCacheBase
{
    private double _rotateAngle = 1;

    /// <summary>
    /// 角度阈值默认0.00028°,转成半径300mm对应的弧长
    /// </summary>
    [ObservableProperty]
    private double _threshold = 1.4661;

    /// <summary>
    /// 对准后chuck此时的角度
    /// </summary>
    [ObservableProperty]
    private double _p5ResultAngle;

    [ComparisonRange(0d, 1d, NumberComparisonRangeTypeEnum.LeftOpenAndRightClosedInterval, ErrorMessage = "Rotate Angle: ")]
    public double RotateAngle
    {
        get => _rotateAngle;
        set => SetProperty(ref _rotateAngle, value, true);
    }
}