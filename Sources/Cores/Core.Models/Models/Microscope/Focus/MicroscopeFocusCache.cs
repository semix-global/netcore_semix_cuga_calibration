using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Net.Utilities.Attributes.DataAnnotations;
using Net.Utilities.Enums.Maths;
using Net.Utilities.Models;

namespace Core.Models.Models.Microscope.Focus;

public sealed partial class MicroscopeFocusCache : CalibrationCacheBase
{
    private double _findFocusMin5X = 3000;
    private double _findFocusMax5X = 10000;
    private double _findFocusMin10X = 3000;
    private double _findFocusMax10X = 10000;
    private double _findFocusMin50X = 3000;
    private double _findFocusMax50X = 10000;
    private double _findFocusMin100X = 3000;
    private double _findFocusMax100X = 10000;
    private double _findFocusMin150X = 3000;
    private double _findFocusMax150X = 10000;
    private double _findFocusInterval5X = 5;
    private double _findFocusInterval10X = 5;
    private double _findFocusInterval50X = 5;
    private double _findFocusInterval100X = 5;
    private double _findFocusInterval150X = 5;
    private double _verifyResultQuality = 100;
    private double _verifyResultError = 100;
    private double _threshold = 50;


    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum;

    [ObservableProperty]
    private Point _findFocusPosition5X;

    [ObservableProperty]
    private Point _findFocusPosition10X;

    [ObservableProperty]
    private Point _findFocusPosition50X;

    [ObservableProperty]
    private Point _findFocusPosition100X;

    [ObservableProperty]
    private Point _findFocusPosition150X;

    [Comparison(3000d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMin5X: ")]
    public double FindFocusMin5X
    {
        get => _findFocusMin5X;
        set => SetProperty(ref _findFocusMin5X, value, validate: true);
    }

    [Comparison(10000d, ComparisonTypeEnum.LessThanOrEqual, ErrorMessage = "FindFocusMax5X: ")]
    public double FindFocusMax5X
    {
        get => _findFocusMax5X;
        set => SetProperty(ref _findFocusMax5X, value, validate: true);
    }

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMin10X: ")]
    public double FindFocusMin10X
    {
        get => _findFocusMin10X;
        set => SetProperty(ref _findFocusMin10X, value, validate: true);
    }

    [Comparison(10000d, ComparisonTypeEnum.LessThanOrEqual, ErrorMessage = "FindFocusMax10X: ")]
    public double FindFocusMax10X
    {
        get => _findFocusMax10X;
        set => SetProperty(ref _findFocusMax10X, value, validate: true);
    }

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMin50X: ")]
    public double FindFocusMin50X
    {
        get => _findFocusMin50X;
        set => SetProperty(ref _findFocusMin50X, value, validate: true);
    }

    [Comparison(10000d, ComparisonTypeEnum.LessThanOrEqual, ErrorMessage = "FindFocusMax50X: ")]
    public double FindFocusMax50X
    {
        get => _findFocusMax50X;
        set => SetProperty(ref _findFocusMax50X, value, validate: true);
    }

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMin100X: ")]
    public double FindFocusMin100X
    {
        get => _findFocusMin100X;
        set => SetProperty(ref _findFocusMin100X, value, validate: true);
    }

    [Comparison(10000d, ComparisonTypeEnum.LessThanOrEqual, ErrorMessage = "FindFocusMax100X: ")]
    public double FindFocusMax100X
    {
        get => _findFocusMax100X;
        set => SetProperty(ref _findFocusMax100X, value, validate: true);
    }

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMin150X: ")]
    public double FindFocusMin150X
    {
        get => _findFocusMin150X;
        set => SetProperty(ref _findFocusMin150X, value, validate: true);
    }

    [Comparison(10000d, ComparisonTypeEnum.LessThanOrEqual, ErrorMessage = "FindFocusMax150X: ")]
    public double FindFocusMax150X
    {
        get => _findFocusMax150X;
        set => SetProperty(ref _findFocusMax150X, value, validate: true);
    }

    [Comparison(1, 100, ComparisonTypeEnum.ClosedInterval, ErrorMessage = "FindFocusInterval5X: ")]
    public double FindFocusInterval5X
    {
        get => _findFocusInterval5X;
        set => SetProperty(ref _findFocusInterval5X, value, validate: true);
    }

    [Comparison(1, 100, ComparisonTypeEnum.ClosedInterval, ErrorMessage = "FindFocusInterval10X: ")]
    public double FindFocusInterval10X
    {
        get => _findFocusInterval10X;
        set => SetProperty(ref _findFocusInterval10X, value, validate: true);
    }

    [Comparison(1d, 100, ComparisonTypeEnum.ClosedInterval, ErrorMessage = "FindFocusInterval50X: ")]
    public double FindFocusInterval50X
    {
        get => _findFocusInterval50X;
        set => SetProperty(ref _findFocusInterval50X, value, validate: true);
    }

    [Comparison(1d, 100d, ComparisonTypeEnum.ClosedInterval, ErrorMessage = "FindFocusInterval100X: ")]
    public double FindFocusInterval100X
    {
        get => _findFocusInterval100X;
        set => SetProperty(ref _findFocusInterval100X, value, validate: true);
    }

    [Comparison(1d, 100d, ComparisonTypeEnum.ClosedInterval, ErrorMessage = "FindFocusInterval150X: ")]
    public double FindFocusInterval150X
    {
        get => _findFocusInterval150X;
        set => SetProperty(ref _findFocusInterval150X, value, validate: true);
    }

    [ObservableProperty]
    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "SetVoltageAfErrorThreshold5X: ")]
    private double _setVoltageAfErrorThreshold5X;

    [ObservableProperty]
    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "SetVoltageAfErrorThreshold10X: ")]
    private double _setVoltageAfErrorThreshold10X;

    [ObservableProperty]
    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "SetVoltageAfErrorThreshold50X: ")]
    private double _setVoltageAfErrorThreshold50X;

    [ObservableProperty]
    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "SetVoltageAfErrorThreshold100X: ")]
    private double _setVoltageAfErrorThreshold100X;

    [ObservableProperty]
    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "SetVoltageAfErrorThreshold150X: ")]
    private double _setVoltageAfErrorThreshold150X;


    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "VerifyResultQuality: ")]
    public double VerifyResultQuality
    {
        get => _verifyResultQuality;
        set => SetProperty(ref _verifyResultQuality, value, validate: true);
    }

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "VerifyResultError: ")]
    public double VerifyResultError
    {
        get => _verifyResultError;
        set => SetProperty(ref _verifyResultError, value, validate: true);
    }

    [Comparison(1d, ComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Threshold: ")]
    public double Threshold
    {
        get => _threshold;
        set => SetProperty(ref _threshold, value, validate: true);
    }

    [ObservableProperty]
    private int _parfocalThreshold;

    public Point GetFindFocusPosition() =>
        MicroscopeMagnificationEnum switch
        {
            MicroscopeMagnificationEnum.Magnification5X => FindFocusPosition5X,
            MicroscopeMagnificationEnum.Magnification10X => FindFocusPosition10X,
            MicroscopeMagnificationEnum.Magnification50X => FindFocusPosition50X,
            MicroscopeMagnificationEnum.Magnification100X => FindFocusPosition100X,
            MicroscopeMagnificationEnum.Magnification150X => FindFocusPosition150X,
            _ => throw new ArgumentOutOfRangeException()
        };

    public double GetFindFocusMin() => MicroscopeMagnificationEnum switch
    {
        MicroscopeMagnificationEnum.Magnification5X => FindFocusMin5X,
        MicroscopeMagnificationEnum.Magnification10X => FindFocusMin10X,
        MicroscopeMagnificationEnum.Magnification50X => FindFocusMin50X,
        MicroscopeMagnificationEnum.Magnification100X => FindFocusMin100X,
        MicroscopeMagnificationEnum.Magnification150X => FindFocusMin150X,
        _ => throw new ArgumentOutOfRangeException()
    };

    public double GetFindFocusMax() => MicroscopeMagnificationEnum switch
    {
        MicroscopeMagnificationEnum.Magnification5X => FindFocusMax5X,
        MicroscopeMagnificationEnum.Magnification10X => FindFocusMax10X,
        MicroscopeMagnificationEnum.Magnification50X => FindFocusMax50X,
        MicroscopeMagnificationEnum.Magnification100X => FindFocusMax100X,
        MicroscopeMagnificationEnum.Magnification150X => FindFocusMax150X,
        _ => throw new ArgumentOutOfRangeException()
    };

    public double GetFindFocusInterval() => MicroscopeMagnificationEnum switch
    {
        MicroscopeMagnificationEnum.Magnification5X => FindFocusInterval5X,
        MicroscopeMagnificationEnum.Magnification10X => FindFocusInterval10X,
        MicroscopeMagnificationEnum.Magnification50X => FindFocusInterval50X,
        MicroscopeMagnificationEnum.Magnification100X => FindFocusInterval100X,
        MicroscopeMagnificationEnum.Magnification150X => FindFocusInterval150X,
        _ => throw new ArgumentOutOfRangeException()
    };

    public double GetSetVoltageAfErrorThreshold() => MicroscopeMagnificationEnum switch
    {
        MicroscopeMagnificationEnum.Magnification5X => SetVoltageAfErrorThreshold5X,
        MicroscopeMagnificationEnum.Magnification10X => SetVoltageAfErrorThreshold10X,
        MicroscopeMagnificationEnum.Magnification50X => SetVoltageAfErrorThreshold50X,
        MicroscopeMagnificationEnum.Magnification100X => SetVoltageAfErrorThreshold100X,
        MicroscopeMagnificationEnum.Magnification150X => SetVoltageAfErrorThreshold150X,
        _ => throw new ArgumentOutOfRangeException()
    };

    public void SetFindFocusPosition(Point position)
    {
        _ = MicroscopeMagnificationEnum switch
        {
            MicroscopeMagnificationEnum.Magnification5X => FindFocusPosition5X = position,
            MicroscopeMagnificationEnum.Magnification10X => FindFocusPosition10X = position,
            MicroscopeMagnificationEnum.Magnification50X => FindFocusPosition50X = position,
            MicroscopeMagnificationEnum.Magnification100X => FindFocusPosition100X = position,
            MicroscopeMagnificationEnum.Magnification150X => FindFocusPosition150X = position,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public (bool IsSuccess, string ErrorMessage) CalibrationVerify()
    {
        ClearErrors();
        switch (MicroscopeMagnificationEnum)
        {
            case MicroscopeMagnificationEnum.Magnification5X:
                {
                    ValidateProperty(FindFocusMin5X, nameof(FindFocusMin5X));
                    ValidateProperty(FindFocusMax5X, nameof(FindFocusMax5X));
                    ValidateProperty(FindFocusInterval5X, nameof(FindFocusInterval5X));
                    ValidateProperty(SetVoltageAfErrorThreshold5X, nameof(SetVoltageAfErrorThreshold5X));
                }
                break;
            case MicroscopeMagnificationEnum.Magnification10X:
                {
                    ValidateProperty(FindFocusMin10X, nameof(FindFocusMin10X));
                    ValidateProperty(FindFocusMax10X, nameof(FindFocusMax10X));
                    ValidateProperty(FindFocusInterval10X, nameof(FindFocusInterval10X));
                    ValidateProperty(SetVoltageAfErrorThreshold10X, nameof(SetVoltageAfErrorThreshold10X));
                }
                break;
            case MicroscopeMagnificationEnum.Magnification50X:
                {
                    ValidateProperty(FindFocusMin50X, nameof(FindFocusMin50X));
                    ValidateProperty(FindFocusMax50X, nameof(FindFocusMax50X));
                    ValidateProperty(FindFocusInterval50X, nameof(FindFocusInterval50X));
                    ValidateProperty(SetVoltageAfErrorThreshold50X, nameof(SetVoltageAfErrorThreshold50X));
                }
                break;
            case MicroscopeMagnificationEnum.Magnification100X:
                {
                    ValidateProperty(FindFocusMin100X, nameof(FindFocusMin100X));
                    ValidateProperty(FindFocusMax100X, nameof(FindFocusMax100X));
                    ValidateProperty(FindFocusInterval100X, nameof(FindFocusInterval100X));
                    ValidateProperty(SetVoltageAfErrorThreshold100X, nameof(SetVoltageAfErrorThreshold100X));
                }
                break;
            case MicroscopeMagnificationEnum.Magnification150X:
                {
                    ValidateProperty(FindFocusMin150X, nameof(FindFocusMin150X));
                    ValidateProperty(FindFocusMax150X, nameof(FindFocusMax150X));
                    ValidateProperty(FindFocusInterval150X, nameof(FindFocusInterval150X));
                    ValidateProperty(SetVoltageAfErrorThreshold150X, nameof(SetVoltageAfErrorThreshold150X));
                }
                break;
            default:
                break;
        }
        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }

}