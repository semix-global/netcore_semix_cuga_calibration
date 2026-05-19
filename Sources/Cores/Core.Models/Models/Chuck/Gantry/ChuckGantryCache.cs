using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.Gantry;

public sealed partial class ChuckGantryCache : CalibrationCacheBase<ChuckGantryCache>
{
    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Die Pitch Height must be greater than 0.1.")]
    public double DiePitchHeight
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 16600;

    [Comparison(1, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Reticle Reference Die Row Count must be greater than 1.")]
    public int ReticleDieCountY
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 1;

    [Comparison(1000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Wafer Radius: ")]
    public double WaferRadius
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 150_000;

    [ObservableProperty]
    public partial MicroscopeLensInformation LowMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial MicroscopeLensInformation HighMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial WaferMaskTypeEnum WaferMaskTypeEnum { get; set; } = WaferMaskTypeEnum.DieCorner_LeftBottom;

    [ObservableProperty]
    public partial double VerifyResultOffset { get; set; }

    [ObservableProperty]
    public partial double Threshold { get; set; }

    [ObservableProperty]
    public partial Point BaseLowFindPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point BaseHighFindPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point LowTopPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point LowBottomPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial string LowBaseTemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LowBaseTemplateImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HighBaseTemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HighBaseTemplateImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double P5Angle { get; set; }

    public Point LowToHighPoint => BaseHighFindPosition - (Vector)BaseLowFindPosition;

    public override ChuckGantryCache Clone() => new()
    {
        DiePitchHeight = DiePitchHeight,
        ReticleDieCountY = ReticleDieCountY,
        WaferRadius = WaferRadius,
        LowMicroscopeLensInformation = LowMicroscopeLensInformation.Clone(),
        HighMicroscopeLensInformation = HighMicroscopeLensInformation.Clone(),
        WaferMaskTypeEnum = WaferMaskTypeEnum,
        VerifyResultOffset = VerifyResultOffset,
        Threshold = Threshold,
        BaseLowFindPosition = BaseLowFindPosition,
        BaseHighFindPosition = BaseHighFindPosition,
        LowTopPosition = LowTopPosition,
        LowBottomPosition = LowBottomPosition,
        LowBaseTemplateFilePath = LowBaseTemplateFilePath,
        LowBaseTemplateImageFilePath = LowBaseTemplateImageFilePath,
        HighBaseTemplateFilePath = HighBaseTemplateFilePath,
        HighBaseTemplateImageFilePath = HighBaseTemplateImageFilePath,
        P5Angle = P5Angle,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}