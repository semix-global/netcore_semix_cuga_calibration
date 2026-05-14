using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.GlobalScaleError;

public sealed partial class ChuckGlobalScaleErrorCache : CalibrationCacheBase<ChuckGlobalScaleErrorCache>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation LowMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial MicroscopeLensInformation HighMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial StageDirectionTypeEnum SiteDirection { get; set; } = StageDirectionTypeEnum.Up;

    [ObservableProperty]
    public partial WaferMaskTypeEnum WaferMaskTypeEnum { get; set; } = WaferMaskTypeEnum.DieCorner_LeftBottom;

    [ObservableProperty]
    public partial Point Threshold { get; set; }

    [ObservableProperty]
    public partial double P5Angle { get; set; }

    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Die Pitch Width must be greater than 0.1.")]
    public double DiePitchWidth
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 5100;

    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Die Pitch Height must be greater than 0.1.")]
    public double DiePitchHeight
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 16600;

    [Comparison(1000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Wafer Radius: ")]
    public double WaferRadius
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 150_000;

    [Comparison(1, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Reticle Reference Die Col Count must be greater than 1.")]
    public int ReticleDieCountX
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 1;

    [Comparison(1, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Reticle Reference Die Row Count must be greater than 1.")]
    public int ReticleDieCountY
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 1;

    #region position

    [ObservableProperty]
    public partial Point BaseLowSiteFindPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point BaseHighSiteFindPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point TopLowSitePosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point LeftLowSitePosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point BottomLowSitePosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point RightLowSitePosition { get; set; } = Point.Origin;

    public Point LowToHighMagnificationOffset => BaseHighSiteFindPosition - (Vector)BaseLowSiteFindPosition;

    public double IdeaWidth => Convert.ToInt32(Math.Abs((LeftLowSitePosition - RightLowSitePosition).X) / DiePitchWidth) * DiePitchWidth;

    public double IdeaHeight => Convert.ToInt32(Math.Abs((TopLowSitePosition - BottomLowSitePosition).Y) / DiePitchHeight) * DiePitchHeight;

    #endregion position

    [ObservableProperty]
    public partial string LowBaseTemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LowBaseTemplateImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HighBaseTemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HighBaseTemplateImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int Times { get; set; } = 3;

    public void SetPosition(Point position, StageDirectionTypeEnum? stageDirection = null)
    {
        switch (stageDirection ?? SiteDirection)
        {
            case StageDirectionTypeEnum.Up:
                TopLowSitePosition = position;
                break;

            case StageDirectionTypeEnum.Down:
                BottomLowSitePosition = position;
                break;

            case StageDirectionTypeEnum.Left:
                LeftLowSitePosition = position;
                break;

            case StageDirectionTypeEnum.Right:
                RightLowSitePosition = position;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(SiteDirection), SiteDirection, null);
        }
    }

    public Point GetPosition()
    {
        return SiteDirection switch
        {
            StageDirectionTypeEnum.Up => TopLowSitePosition,
            StageDirectionTypeEnum.Down => BottomLowSitePosition,
            StageDirectionTypeEnum.Left => LeftLowSitePosition,
            StageDirectionTypeEnum.Right => RightLowSitePosition,
            _ => throw new ArgumentOutOfRangeException(nameof(SiteDirection), SiteDirection, null)
        };
    }

    public override ChuckGlobalScaleErrorCache Clone() => new()
    {
        LowMicroscopeLensInformation = LowMicroscopeLensInformation.Clone(),
        HighMicroscopeLensInformation = HighMicroscopeLensInformation.Clone(),
        SiteDirection = SiteDirection,
        WaferMaskTypeEnum = WaferMaskTypeEnum,
        Threshold = Threshold,
        P5Angle = P5Angle,
        DiePitchWidth = DiePitchWidth,
        DiePitchHeight = DiePitchHeight,
        WaferRadius = WaferRadius,
        ReticleDieCountX = ReticleDieCountX,
        ReticleDieCountY = ReticleDieCountY,
        BaseLowSiteFindPosition = BaseLowSiteFindPosition,
        BaseHighSiteFindPosition = BaseHighSiteFindPosition,
        TopLowSitePosition = TopLowSitePosition,
        LeftLowSitePosition = LeftLowSitePosition,
        BottomLowSitePosition = BottomLowSitePosition,
        RightLowSitePosition = RightLowSitePosition,
        LowBaseTemplateFilePath = LowBaseTemplateFilePath,
        LowBaseTemplateImageFilePath = LowBaseTemplateImageFilePath,
        HighBaseTemplateFilePath = HighBaseTemplateFilePath,
        HighBaseTemplateImageFilePath = HighBaseTemplateImageFilePath,
        Times = Times,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}