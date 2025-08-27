using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
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
    private StageDirectionTypeEnum _siteDirection = StageDirectionTypeEnum.Up;

    [ObservableProperty]
    private ChuckGlobalScaleErrorCacheItem _lowGlobalScaleErrorCacheItem = new();

    [ObservableProperty]
    private ChuckGlobalScaleErrorCacheItem _highGlobalScaleErrorCacheItem = new();

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

    #region Idea Position low site

    [ObservableProperty]
    private Point _baseLowSiteFindPosition = Point.Origin;

    /// <summary>
    /// 低倍高倍的相对位置误差
    /// </summary>
    [ObservableProperty]
    private Point _lowToHighMagnificationOffset = Point.Origin;

    public double IdeaWidth => Convert.ToInt32(Math.Abs((HighGlobalScaleErrorCacheItem.LeftPosition - HighGlobalScaleErrorCacheItem.RightPosition).X) / ColumnCellWidth) * ColumnCellWidth;

    public double IdeaHeight => Convert.ToInt32(Math.Abs((HighGlobalScaleErrorCacheItem.TopPosition - HighGlobalScaleErrorCacheItem.BottomPosition).Y) / RowCellHeight) * RowCellHeight;

    #endregion Idea Position low site

    public double GetActualWaferDiameter(bool isAxisX)
    {
        return isAxisX ? WaferDiameter - ColumnCellWidth : WaferDiameter - RowCellHeight;
    }

    public void SetPosition(Point position, MicroscopeLensInformation lensInformation, StageDirectionTypeEnum? stageDirection = null)
    {
        var chuckCenterCacheItem = lensInformation == LowGlobalScaleErrorCacheItem.LensInformation ? LowGlobalScaleErrorCacheItem : HighGlobalScaleErrorCacheItem;
        switch (stageDirection ?? SiteDirection)
        {
            case StageDirectionTypeEnum.Up:
                chuckCenterCacheItem.TopPosition = position;
                break;

            case StageDirectionTypeEnum.Down:
                chuckCenterCacheItem.BottomPosition = position;
                break;

            case StageDirectionTypeEnum.Left:
                chuckCenterCacheItem.LeftPosition = position;
                break;

            case StageDirectionTypeEnum.Right:
                chuckCenterCacheItem.RightPosition = position;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(SiteDirection), SiteDirection, null);
        }
    }

    public void SetTemplate(string templatePath, string templateImagePath, MicroscopeLensInformation lensInformation, StageDirectionTypeEnum? stageDirection = null)
    {
        var chuckCenterCacheItem = lensInformation == LowGlobalScaleErrorCacheItem.LensInformation ? LowGlobalScaleErrorCacheItem : HighGlobalScaleErrorCacheItem;
        switch (stageDirection ?? SiteDirection)
        {
            case StageDirectionTypeEnum.Up:
                chuckCenterCacheItem.TopTemplateFilePath = templatePath;
                chuckCenterCacheItem.TopTemplateImageFilePath = templateImagePath;
                break;

            case StageDirectionTypeEnum.Down:
                chuckCenterCacheItem.BottomTemplateFilePath = templatePath;
                chuckCenterCacheItem.BottomTemplateImageFilePath = templateImagePath;
                break;

            case StageDirectionTypeEnum.Left:
                chuckCenterCacheItem.LeftTemplateFilePath = templatePath;
                chuckCenterCacheItem.LeftTemplateImageFilePath = templateImagePath;
                break;

            case StageDirectionTypeEnum.Right:
                chuckCenterCacheItem.RightTemplateFilePath = templatePath;
                chuckCenterCacheItem.RightTemplateImageFilePath = templateImagePath;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(SiteDirection), SiteDirection, null);
        }
    }

    public Point GetPosition(MicroscopeLensInformation lensInformation)
    {
        var chuckCenterCacheItem = lensInformation == LowGlobalScaleErrorCacheItem.LensInformation ? LowGlobalScaleErrorCacheItem : HighGlobalScaleErrorCacheItem;
        return SiteDirection switch
        {
            StageDirectionTypeEnum.Up => chuckCenterCacheItem.TopPosition,
            StageDirectionTypeEnum.Down => chuckCenterCacheItem.BottomPosition,
            StageDirectionTypeEnum.Left => chuckCenterCacheItem.LeftPosition,
            StageDirectionTypeEnum.Right => chuckCenterCacheItem.RightPosition,
            _ => throw new ArgumentOutOfRangeException(nameof(SiteDirection), SiteDirection, null)
        };
    }

    public (string templatePath, string templateImagePath) GetTemplate(MicroscopeLensInformation lensInformation)
    {
        var chuckCenterCacheItem = lensInformation == LowGlobalScaleErrorCacheItem.LensInformation ? LowGlobalScaleErrorCacheItem : HighGlobalScaleErrorCacheItem;
        return SiteDirection switch
        {
            StageDirectionTypeEnum.Up => (chuckCenterCacheItem.TopTemplateFilePath, chuckCenterCacheItem.TopTemplateImageFilePath),
            StageDirectionTypeEnum.Down => (chuckCenterCacheItem.BottomTemplateFilePath, chuckCenterCacheItem.BottomTemplateImageFilePath),
            StageDirectionTypeEnum.Left => (chuckCenterCacheItem.LeftTemplateFilePath, chuckCenterCacheItem.LeftTemplateImageFilePath),
            StageDirectionTypeEnum.Right => (chuckCenterCacheItem.RightTemplateFilePath, chuckCenterCacheItem.RightTemplateImageFilePath),
            _ => throw new ArgumentOutOfRangeException(nameof(SiteDirection), SiteDirection, null)
        };
    }
}