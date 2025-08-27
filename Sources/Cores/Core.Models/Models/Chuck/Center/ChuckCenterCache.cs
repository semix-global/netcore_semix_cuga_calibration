using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.Center;

public sealed partial class ChuckCenterCache : CalibrationCacheBase
{
    private double _columnCellWidth = 1;
    private double _rowCellHeight = 1;
    private double _waferDiameter = 300_000;
    private double _positiveAngle = 1d;
    private double _negativeAngle = -1d;
    private int _threshold = 50;

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

    [ObservableProperty]
    private StageDirectionTypeEnum _siteDirection = StageDirectionTypeEnum.Up;

    [ObservableProperty]
    private Point _baseLowFindPosition = Point.Origin;

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

    public double GetActualWaferDiameter(bool isAxisX)
    {
        return isAxisX ? WaferDiameter - ColumnCellWidth : WaferDiameter - RowCellHeight;
    }

    public void SetPosition(Point position, MicroscopeLensInformation lensInformation)
    {
        var chuckCenterCacheItem = lensInformation == LowChuckCenterCacheItem.LensInformation ? LowChuckCenterCacheItem : HighChuckCenterCacheItem;
        switch (SiteDirection)
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

    public void SetTemplate(string templatePath, string templateImagePath, MicroscopeLensInformation lensInformation)
    {
        var chuckCenterCacheItem = lensInformation == LowChuckCenterCacheItem.LensInformation ? LowChuckCenterCacheItem : HighChuckCenterCacheItem;
        switch (SiteDirection)
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
        var chuckCenterCacheItem = lensInformation == LowChuckCenterCacheItem.LensInformation ? LowChuckCenterCacheItem : HighChuckCenterCacheItem;
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
        var chuckCenterCacheItem = lensInformation == LowChuckCenterCacheItem.LensInformation ? LowChuckCenterCacheItem : HighChuckCenterCacheItem;
        return SiteDirection switch
        {
            StageDirectionTypeEnum.Up => (chuckCenterCacheItem.TopTemplateFilePath, chuckCenterCacheItem.TopTemplateImageFilePath),
            StageDirectionTypeEnum.Down => (chuckCenterCacheItem.BottomTemplateFilePath, chuckCenterCacheItem.BottomTemplateImageFilePath),
            StageDirectionTypeEnum.Left => (chuckCenterCacheItem.LeftTemplateFilePath, chuckCenterCacheItem.LeftTemplateImageFilePath),
            StageDirectionTypeEnum.Right => (chuckCenterCacheItem.RightTemplateFilePath, chuckCenterCacheItem.RightTemplateImageFilePath),
            _ => throw new ArgumentOutOfRangeException(nameof(SiteDirection), SiteDirection, null)
        };
    }

    #region Verify

    public (bool IsSuccess, string ErrorMessage) Step3Verify()
    {
        ClearErrors();
        ValidateProperty(WaferDiameter, nameof(WaferDiameter));
        ValidateProperty(RowCellHeight, nameof(RowCellHeight));
        ValidateProperty(ColumnCellWidth, nameof(ColumnCellWidth));

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }

    public (bool IsSuccess, string ErrorMessage) Step4Verify()
    {
        ClearErrors();
        ValidateProperty(PositiveAngle, nameof(PositiveAngle));

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }

    public (bool IsSuccess, string ErrorMessage) Step5Verify()
    {
        ClearErrors();
        ValidateProperty(NegativeAngle, nameof(NegativeAngle));

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }

    #endregion Verify
}