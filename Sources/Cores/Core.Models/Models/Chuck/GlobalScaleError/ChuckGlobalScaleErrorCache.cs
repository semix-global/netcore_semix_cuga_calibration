using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.GlobalScaleError;

public sealed partial class ChuckGlobalScaleErrorCache : CalibrationCacheBase<ChuckGlobalScaleErrorCache>
{
    private double _diePitchWidth = 5100;
    private double _diePitchHeight = 16600;
    private int _reticleDieCountX = 1;
    private int _reticleDieCountY = 1;
    private double _waferRadius = 150_000;

    [ObservableProperty]
    private MicroscopeLensInformation _lowMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private StageDirectionTypeEnum _siteDirection = StageDirectionTypeEnum.Up;

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.DieCorner_LeftBottom;

    [ObservableProperty]
    private Point _threshold;

    [ObservableProperty]
    private double _p5Angle;

    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Die Pitch Width must be greater than 0.1.")]
    public double DiePitchWidth
    {
        get => _diePitchWidth;
        set => SetProperty(ref _diePitchWidth, value, true);
    }

    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Die Pitch Height must be greater than 0.1.")]
    public double DiePitchHeight
    {
        get => _diePitchHeight;
        set => SetProperty(ref _diePitchHeight, value, true);
    }

    [Comparison(1000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Wafer Radius: ")]
    public double WaferRadius
    {
        get => _waferRadius;
        set => SetProperty(ref _waferRadius, value, true);
    }

    [Comparison(1, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Reticle Reference Die Col Count must be greater than 1.")]
    public int ReticleDieCountX
    {
        get => _reticleDieCountX;
        set => SetProperty(ref _reticleDieCountX, value, true);
    }

    [Comparison(1, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Reticle Reference Die Row Count must be greater than 1.")]
    public int ReticleDieCountY
    {
        get => _reticleDieCountY;
        set => SetProperty(ref _reticleDieCountY, value, true);
    }

    #region position

    [ObservableProperty]
    private Point _baseLowSiteFindPosition = Point.Origin;

    [ObservableProperty]
    private Point _baseHighSiteFindPosition = Point.Origin;

    [ObservableProperty]
    private Point _topLowSitePosition = Point.Origin;

    [ObservableProperty]
    private Point _leftLowSitePosition = Point.Origin;

    [ObservableProperty]
    private Point _bottomLowSitePosition = Point.Origin;

    [ObservableProperty]
    private Point _rightLowSitePosition = Point.Origin;

    public Point LowToHighMagnificationOffset => BaseHighSiteFindPosition - (Vector)BaseLowSiteFindPosition;

    public double IdeaWidth => Convert.ToInt32(Math.Abs((LeftLowSitePosition - RightLowSitePosition).X) / DiePitchWidth) * DiePitchWidth;

    public double IdeaHeight => Convert.ToInt32(Math.Abs((TopLowSitePosition - BottomLowSitePosition).Y) / DiePitchHeight) * DiePitchHeight;

    #endregion position

    [ObservableProperty]
    private string _lowBaseTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _lowBaseTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _highBaseTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _highBaseTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private int _times = 3;

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
        LowMicroscopeLensInformation = LowMicroscopeLensInformation,
        HighMicroscopeLensInformation = HighMicroscopeLensInformation,
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