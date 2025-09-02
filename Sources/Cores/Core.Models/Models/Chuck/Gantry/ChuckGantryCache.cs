using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.Gantry;

public sealed partial class ChuckGantryCache : CalibrationCacheBase
{
    private double _rowCellHeight = 1;
    private double _waferDiameter = 300_000;

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
    private MicroscopeLensInformation _lowMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.DieCorner_LeftBottom;

    [ObservableProperty]
    private double _verifyResultOffset;

    [ObservableProperty]
    private double _threshold;

    [ObservableProperty]
    private Point _baseLowFindPosition = Point.Origin;

    [ObservableProperty]
    private Point _lowTopPosition = Point.Origin;

    [ObservableProperty]
    private Point _lowBottomPosition = Point.Origin;

    [ObservableProperty]
    private Point _highTopPosition = Point.Origin;

    [ObservableProperty]
    private Point _highBottomPosition = Point.Origin;

    [ObservableProperty]
    private string _lowTopTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _lowTopTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _highTopTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _highTopTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _lowBottomTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _lowBottomTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _highBottomTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _highBottomTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private double _p5Angle;

    public Point LowToHighPoint => HighTopPosition - (Vector)LowTopPosition;
}