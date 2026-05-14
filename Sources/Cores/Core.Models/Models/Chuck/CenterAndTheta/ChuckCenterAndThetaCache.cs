using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.CenterAndTheta;

public sealed partial class ChuckCenterAndThetaCache : CalibrationCacheBase<ChuckCenterAndThetaCache>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation LowMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial MicroscopeLensInformation HighMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ComparisonRange(0d, 1d, NumberComparisonRangeTypeEnum.LeftOpenAndRightClosedInterval, ErrorMessage = "Rotate Angle: ")]
    public double RotateAngle
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 1d;

    [ObservableProperty]
    public partial double P5Angle { get; set; }

    [ObservableProperty]
    public partial double ThetaAngle { get; set; }

    [ObservableProperty]
    public partial int Times { get; set; } = 3;

    #region Wafer Parameters

    [ObservableProperty]
    public partial WaferMaskTypeEnum WaferMaskTypeEnum { get; set; } = WaferMaskTypeEnum.DieCorner_LeftBottom;

    [ObservableProperty]
    public partial StageDirectionTypeEnum SiteDirection { get; set; } = StageDirectionTypeEnum.Up;

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

    #endregion

    #region Position

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

    #endregion

    #region Threshold

    [ComparisonRange(0, 500, NumberComparisonRangeTypeEnum.LeftOpenAndRightClosedInterval, ErrorMessage = "Center Calibration Threshold: ")]
    public int CenterCalibrationThreshold
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 200;

    [ComparisonRange(0, 50, NumberComparisonRangeTypeEnum.LeftOpenAndRightClosedInterval, ErrorMessage = "Center Verify Threshold: ")]
    public int CenterVerifyThreshold
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 50;

    /// <summary>
    /// 角度阈值默认0.00028°,转成半径300mm对应的弧长
    /// </summary>
    [ObservableProperty]
    public partial double RotateScaleThreshold { get; set; } = 1.4661;

    #endregion

    [ObservableProperty]
    public partial string LowBaseTemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LowBaseTemplateImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HighBaseTemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HighBaseTemplateImageFilePath { get; set; } = string.Empty;

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
        LowMicroscopeLensInformation = LowMicroscopeLensInformation.Clone(),
        HighMicroscopeLensInformation = HighMicroscopeLensInformation.Clone(),
        RotateAngle = RotateAngle,
        P5Angle = P5Angle,
        ThetaAngle = ThetaAngle,
        Times = Times,
        WaferMaskTypeEnum = WaferMaskTypeEnum,
        SiteDirection = SiteDirection,
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
        CenterCalibrationThreshold = CenterCalibrationThreshold,
        CenterVerifyThreshold = CenterVerifyThreshold,
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