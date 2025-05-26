using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Net.Utilities.Attributes.DataAnnotations;
using Net.Utilities.Enums.Maths;
using Net.Utilities.Models;

namespace Core.Models.Models.Microscope.PixelSize;

public sealed partial class MicroscopePixelSizeCache : CalibrationCacheBase
{
    [ObservableProperty]
    [Comparison(100000d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Chuck Radius: ")]
    private double _chuckRadius = 150000;

    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum;

    [ObservableProperty]
    [PointValidationAttribute(PointEnum.Point2d, ErrorMessage = "Find Position: ")]
    private Point _findPosition;

    [ObservableProperty]
    private Size _threshold;

    /// <summary>
    /// 网格水平角度阈值
    /// </summary>
    [ObservableProperty]
    private double _angleThreshold;
}