using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.Center;

public sealed partial class ChuckCenterCache : CalibrationCacheBase
{
    private double _positiveAngle = 1d;
    private double _negativeAngle = -1d;
    private int _threshold = 50;

    [ObservableProperty]
    private MicroscopeMagnificationEnum _lowMicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification5X;

    [ObservableProperty]
    private MicroscopeMagnificationEnum _highMicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification50X;

    [ObservableProperty]
    private Point _lowTopPosition = new(150, 0);

    [ObservableProperty]
    private Point _lowLeftPosition = new(0, 150);

    [ObservableProperty]
    private Point _lowBottomPosition = new(-150, 0);

    [ObservableProperty]
    private Point _lowRightPosition = new(0, -150);

    [ObservableProperty]
    private Point _highTopPosition = new(150, 0);

    [ObservableProperty]
    private Point _highLeftPosition = new(0, 150);

    [ObservableProperty]
    private Point _highBottomPosition = new(-150, 0);

    [ObservableProperty]
    private Point _highRightPosition = new(0, -150);

    [ObservableProperty]
    private string _lowTopTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _lowTopTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _lowRightTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _lowRightTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _lowBottomTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _lowBottomTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _lowLeftTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _lowLeftTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _highTopTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _highTopTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _highRightTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _highRightTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _highBottomTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _highBottomTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _highLeftTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _highLeftTemplateImageFilePath = string.Empty;

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

    public Point LowToHighPointTop => HighTopPosition - (Vector)LowTopPosition;

    public Point LowToHighPointRight => HighRightPosition - (Vector)LowRightPosition;

    public Point LowToHighPointBottom => HighBottomPosition - (Vector)LowBottomPosition;

    public Point LowToHighPointLeft => HighLeftPosition - (Vector)LowLeftPosition;

    #region Verify

    public (bool IsSuccess, string ErrorMessage) Step1Verify()
    {
        ClearErrors();
        ValidateProperty(PositiveAngle, nameof(PositiveAngle));

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }

    public (bool IsSuccess, string ErrorMessage) Step2Verify()
    {
        ClearErrors();
        ValidateProperty(NegativeAngle, nameof(NegativeAngle));

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }

    #endregion
}