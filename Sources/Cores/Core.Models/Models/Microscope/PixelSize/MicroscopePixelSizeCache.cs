using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Microscope.PixelSize;

public sealed partial class MicroscopePixelSizeCache : CalibrationCacheBase
{
    [ObservableProperty]
    [Comparison(100000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Chuck Radius: ")]
    private double _chuckRadius = 150000;

    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private Size _threshold;

    /// <summary>
    /// 网格水平角度阈值
    /// </summary>
    [ObservableProperty]
    private double _angleThreshold;
}