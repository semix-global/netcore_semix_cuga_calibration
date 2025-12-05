using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.Gantry;

public sealed partial class ChuckGantryCache : CalibrationCacheBase
{
    private double _diePitchHeight = 16600;
    private int _reticleDieCountY = 1;
    private double _waferRadius = 150_000;

    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Die Pitch Height must be greater than 0.1.")]
    public double DiePitchHeight
    {
        get => _diePitchHeight;
        set => SetProperty(ref _diePitchHeight, value, true);
    }

    [Comparison(1, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Reticle Reference Die Row Count must be greater than 1.")]
    public int ReticleDieCountY
    {
        get => _reticleDieCountY;
        set => SetProperty(ref _reticleDieCountY, value, true);
    }

    [Comparison(1000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Wafer Radius: ")]
    public double WaferRadius
    {
        get => _waferRadius;
        set => SetProperty(ref _waferRadius, value, true);
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
    private Point _baseHighFindPosition = Point.Origin;

    [ObservableProperty]
    private Point _lowTopPosition = Point.Origin;

    [ObservableProperty]
    private Point _lowBottomPosition = Point.Origin;

    [ObservableProperty]
    private string _lowBaseTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _lowBaseTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _highBaseTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _highBaseTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private double _p5Angle;

    public Point LowToHighPoint => BaseHighFindPosition - (Vector)BaseLowFindPosition;
}