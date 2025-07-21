using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.GlobalScaleError;

public sealed partial class ChuckGlobalScaleErrorCache : CalibrationCacheBase
{
    private double _columnCellWidth = 1;
    private double _rowCellHeight = 1;
    private double _waferDiameter = 300_000;

    [ObservableProperty]
    private MicroscopeMagnificationInfo _lowMicroscopeMagnificationInfo = new();

    [ObservableProperty]
    private MicroscopeMagnificationInfo _highMicroscopeMagnificationInfo = new();

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.DieCorner_LeftBottom;

    [ObservableProperty]
    private Point _threshold;

    [ObservableProperty]
    private double _p5Angle;


    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Column Cell Width must be greater than 0.1.")]
    public double ColumnCellWidth
    {
        get => _columnCellWidth;
        set => SetProperty(ref _columnCellWidth, value, true);
    }

    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Row Cell Height must be greater than 0.1.")]
    public double RowCellHeight
    {
        get => _rowCellHeight;
        set => SetProperty(ref _rowCellHeight, value, true);
    }

    [Comparison(1000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Chuck Diameter: ")]
    public double WaferDiameter
    {
        get => _waferDiameter;
        set => SetProperty(ref _waferDiameter, value, true);
    }

    #region Template

    [ObservableProperty]
    private Point _baseLowSiteFindPosition;

    [ObservableProperty]
    private Point _baseHighSiteFindPosition;

    [ObservableProperty]
    private Point _baseFindResultPosition;

    /// <summary>
    /// 低倍高倍的相对位置误差
    /// </summary>
    public Point LowToHighMagnificationOffset => BaseHighSiteFindPosition - (Vector)BaseLowSiteFindPosition;

    [ObservableProperty]
    private string _lowTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _lowTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _highTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _highTemplateImageFilePath = string.Empty;

    #endregion Template

    #region Idea Position low site

    /// <summary>
    /// wafer左端点理想坐标
    /// </summary>
    [ObservableProperty]
    private Point _leftSideIdeaPosition;

    /// <summary>
    /// wafer右端点理想坐标
    /// </summary>
    [ObservableProperty]
    private Point _rightSideIdeaPosition;

    /// <summary>
    /// wafer顶部端点理想坐标
    /// </summary>
    [ObservableProperty]
    private Point _topSideIdeaPosition;

    /// <summary>
    /// wafer底部端点理想坐标
    /// </summary>
    [ObservableProperty]
    private Point _bottomSideIdeaPosition;

    public double IdeaWidth => Convert.ToInt32(Math.Abs((LeftSideIdeaPosition - RightSideIdeaPosition).X) / ColumnCellWidth) * ColumnCellWidth;

    public double IdeaHeight => Convert.ToInt32(Math.Abs((TopSideIdeaPosition - BottomSideIdeaPosition).Y) / RowCellHeight) * RowCellHeight;

    #endregion Idea Position low site

    public double GetActualWaferDiameter(bool isAxisX)
    {
        return isAxisX ? WaferDiameter - ColumnCellWidth : WaferDiameter - RowCellHeight;
    }
}