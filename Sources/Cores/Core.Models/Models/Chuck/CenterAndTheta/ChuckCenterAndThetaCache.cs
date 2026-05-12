using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.CenterAndTheta;

public sealed partial class ChuckCenterAndThetaCache : CalibrationCacheBase<ChuckCenterAndThetaCache>
{
    private double _diePitchWidth = 5100;
    private double _diePitchHeight = 16600;
    private int _reticleDieCountX = 1;
    private int _reticleDieCountY = 1;
    private double _waferRadius = 150_000;
    private double _rotateAngle = 1d;
    private int _centerCalibrationThreshold = 200;
    private int _centerVerifyThreshold = 50;

    [ObservableProperty]
    private MicroscopeLensInformation _lowMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ComparisonRange(0d, 1d, NumberComparisonRangeTypeEnum.LeftOpenAndRightClosedInterval, ErrorMessage = "Rotate Angle: ")]
    public double RotateAngle
    {
        get => _rotateAngle;
        set => SetProperty(ref _rotateAngle, value, true);
    }

    [ObservableProperty]
    private double _p5Angle;

    [ObservableProperty]
    private double _thetaAngle;

    [ObservableProperty]
    private int _times = 3;

    #region Wafer Parameters

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.DieCorner_LeftBottom;

    [ObservableProperty]
    private StageDirectionTypeEnum _siteDirection = StageDirectionTypeEnum.Up;

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

    #endregion

    #region Position

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

    #endregion

    #region Threshold

    [ComparisonRange(0, 500, NumberComparisonRangeTypeEnum.LeftOpenAndRightClosedInterval, ErrorMessage = "Center Calibration Threshold: ")]
    public int CenterCalibrationThreshold
    {
        get => _centerCalibrationThreshold;
        set => SetProperty(ref _centerCalibrationThreshold, value, true);
    }

    [ComparisonRange(0, 50, NumberComparisonRangeTypeEnum.LeftOpenAndRightClosedInterval, ErrorMessage = "Center Verify Threshold: ")]
    public int CenterVerifyThreshold
    {
        get => _centerVerifyThreshold;
        set => SetProperty(ref _centerVerifyThreshold, value, true);
    }

    /// <summary>
    /// 角度阈值默认0.00028°,转成半径300mm对应的弧长
    /// </summary>
    [ObservableProperty]
    private double _rotateScaleThreshold = 1.4661;

    #endregion

    [ObservableProperty]
    private string _lowBaseTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _lowBaseTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _highBaseTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _highBaseTemplateImageFilePath = string.Empty;

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

    #region Verify

    public (bool IsSuccess, string ErrorMessage) Step1Verify()
    {
        ClearErrors();
        ValidateProperty(WaferRadius, nameof(WaferRadius));
        ValidateProperty(DiePitchHeight, nameof(DiePitchHeight));
        ValidateProperty(DiePitchWidth, nameof(DiePitchWidth));

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }

    public (bool IsSuccess, string ErrorMessage) Step4Verify()
    {
        ClearErrors();
        ValidateProperty(RotateAngle, nameof(RotateAngle));

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }

    #endregion Verify

    public override ChuckCenterAndThetaCache Clone() => new()
    {
        DiePitchWidth = DiePitchWidth,
        DiePitchHeight = DiePitchHeight,
        ReticleDieCountX = ReticleDieCountX,
        ReticleDieCountY = ReticleDieCountY,
        WaferRadius = WaferRadius,
        RotateAngle = RotateAngle,
        CenterCalibrationThreshold = CenterCalibrationThreshold,
        CenterVerifyThreshold = CenterVerifyThreshold,
        LowMicroscopeLensInformation = LowMicroscopeLensInformation.Clone(),
        HighMicroscopeLensInformation = HighMicroscopeLensInformation.Clone(),
        P5Angle = P5Angle,
        ThetaAngle = ThetaAngle,
        Times = Times,
        WaferMaskTypeEnum = WaferMaskTypeEnum,
        SiteDirection = SiteDirection,
        BaseLowSiteFindPosition = BaseLowSiteFindPosition,
        BaseHighSiteFindPosition = BaseHighSiteFindPosition,
        TopLowSitePosition = TopLowSitePosition,
        LeftLowSitePosition = LeftLowSitePosition,
        BottomLowSitePosition = BottomLowSitePosition,
        RightLowSitePosition = RightLowSitePosition,
        RotateScaleThreshold = RotateScaleThreshold,
        LowBaseTemplateFilePath = LowBaseTemplateFilePath,
        LowBaseTemplateImageFilePath = LowBaseTemplateImageFilePath,
        HighBaseTemplateFilePath = HighBaseTemplateFilePath,
        HighBaseTemplateImageFilePath = HighBaseTemplateImageFilePath,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}