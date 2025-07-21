using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Pattern;
using Core.Wcf.Models.Chuck;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.RotateScaleError;

public sealed partial class ChuckRotateScaleErrorDto : CalibrationDtoBase, ICloneable<ChuckRotateScaleErrorDto>, IAdaptTo<CalibrationChuckRotateScaleError>
{
    [ObservableProperty]
    private MicroscopeMagnificationInfo _lowMicroscopeMagnificationInfo = new();

    [ObservableProperty]
    private MicroscopeMagnificationInfo _highMicroscopeMagnificationInfo = new();

    [ObservableProperty]
    private bool _isPositive;

    [ObservableProperty]
    private Point _chuckCenterBrightFieldPosition = Point.Origin;

    /// <summary>
    /// T轴比例误差系数下发值
    /// </summary>
    [ObservableProperty]
    private double _appliedScaleT = 1.0;

    /// <summary>
    /// 下发T轴比例误差系数后的结果值
    /// </summary>
    [ObservableProperty]
    private double _resultScaleT = 1.0;

    /// <summary>
    /// 四个预设点位正反向旋转后的误差值均值(um)
    /// </summary>
    [ObservableProperty]
    private double _scaleErrorValueAverage;

    #region Real Position

    /// <summary>
    /// 四个预设点位正反向旋转后的实际角度差值的均值
    /// </summary>
    [ObservableProperty]
    private double _realAngleErrorsAverage;

    /// <summary>
    /// wafer左端点实际坐标-正向
    /// </summary>
    [ObservableProperty]
    private Point _positiveLeftHighSiteRealPosition;

    /// <summary>
    /// wafer右端点实际坐标-正向
    /// </summary>
    [ObservableProperty]
    private Point _positiveRightHighSiteRealPosition;

    /// <summary>
    /// wafer顶部端点实际坐标-正向
    /// </summary>
    [ObservableProperty]
    private Point _positiveTopHighSiteRealPosition;

    /// <summary>
    /// wafer底部端点实际坐标-正向
    /// </summary>
    [ObservableProperty]
    private Point _positiveBottomHighSiteRealPosition;

    /// <summary>
    /// wafer左端点实际坐标-反向
    /// </summary>
    [ObservableProperty]
    private Point _negativeLeftHighSiteRealPosition;

    /// <summary>
    /// wafer右端点实际坐标-反向
    /// </summary>
    [ObservableProperty]
    private Point _negativeRightHighSiteRealPosition;

    /// <summary>
    /// wafer顶部端点实际坐标-反向
    /// </summary>
    [ObservableProperty]
    private Point _negativeTopHighSiteRealPosition;

    /// <summary>
    /// wafer底部端点实际坐标-反向
    /// </summary>
    [ObservableProperty]
    private Point _negativeBottomHighSiteRealPosition;

    #endregion Real Position

    #region File Path

    [ObservableProperty]
    private string _positiveLeftFindResultFilePath = string.Empty;

    [ObservableProperty]
    private string _positiveRightFindResultFilePath = string.Empty;

    [ObservableProperty]
    private string _positiveTopFindResultFilePath = string.Empty;

    [ObservableProperty]
    private string _positiveBottomFindResultFilePath = string.Empty;

    [ObservableProperty]
    private string _negativeLeftFindResultFilePath = string.Empty;

    [ObservableProperty]
    private string _negativeRightFindResultFilePath = string.Empty;

    [ObservableProperty]
    private string _negativeTopFindResultFilePath = string.Empty;

    [ObservableProperty]
    private string _negativeBottomFindResultFilePath = string.Empty;

    #endregion File Path

    public void SetMatchResultInfo(StageDirectionTypeEnum stageDirectionTypeEnum, Point point, string findResultImageFilePath)
    {
        switch (stageDirectionTypeEnum)
        {
            case StageDirectionTypeEnum.Up:
                {
                    if (IsPositive)
                    {
                        PositiveTopHighSiteRealPosition = point;
                        PositiveTopFindResultFilePath = findResultImageFilePath;
                    }
                    else
                    {
                        NegativeTopHighSiteRealPosition = point;
                        NegativeTopFindResultFilePath = findResultImageFilePath;
                    }
                }
                break;
            case StageDirectionTypeEnum.Down:
                {
                    if (IsPositive)
                    {
                        PositiveBottomHighSiteRealPosition = point;
                        PositiveBottomFindResultFilePath = findResultImageFilePath;
                    }
                    else
                    {
                        NegativeBottomHighSiteRealPosition = point;
                        NegativeBottomFindResultFilePath = findResultImageFilePath;
                    }
                }
                break;
            case StageDirectionTypeEnum.Left:
                {
                    if (IsPositive)
                    {
                        PositiveLeftHighSiteRealPosition = point;
                        PositiveLeftFindResultFilePath = findResultImageFilePath;
                    }
                    else
                    {
                        NegativeLeftHighSiteRealPosition = point;
                        NegativeLeftFindResultFilePath = findResultImageFilePath;
                    }
                }
                break;
            case StageDirectionTypeEnum.Right:
                {
                    if (IsPositive)
                    {
                        PositiveRightHighSiteRealPosition = point;
                        PositiveRightFindResultFilePath = findResultImageFilePath;
                    }
                    else
                    {
                        NegativeRightHighSiteRealPosition = point;
                        NegativeRightFindResultFilePath = findResultImageFilePath;
                    }
                }
                break;
        }
    }

    public Point GetRealPosition(StageDirectionTypeEnum stageDirectionTypeEnum)
    {
        return stageDirectionTypeEnum switch
        {
            StageDirectionTypeEnum.Up => IsPositive ? PositiveTopHighSiteRealPosition : NegativeTopHighSiteRealPosition,
            StageDirectionTypeEnum.Down => IsPositive ? PositiveBottomHighSiteRealPosition : NegativeBottomHighSiteRealPosition,
            StageDirectionTypeEnum.Left => IsPositive ? PositiveLeftHighSiteRealPosition : NegativeLeftHighSiteRealPosition,
            StageDirectionTypeEnum.Right => IsPositive ? PositiveRightHighSiteRealPosition : NegativeRightHighSiteRealPosition,
            _ => Point.Origin
        };
    }

    public (Point positiveRealPosition, Point negativeRealPosition) GetCoupleRealPosition(StageDirectionTypeEnum stageDirectionTypeEnum)
    {
        return stageDirectionTypeEnum switch
        {
            StageDirectionTypeEnum.Up => (PositiveTopHighSiteRealPosition, NegativeTopHighSiteRealPosition),
            StageDirectionTypeEnum.Down => (PositiveBottomHighSiteRealPosition, NegativeBottomHighSiteRealPosition),
            StageDirectionTypeEnum.Left => (PositiveLeftHighSiteRealPosition, NegativeLeftHighSiteRealPosition),
            StageDirectionTypeEnum.Right => (PositiveRightHighSiteRealPosition, NegativeRightHighSiteRealPosition),
            _ => (Point.Origin, Point.Origin)
        };
    }

    public CalibrationChuckRotateScaleError AdaptTo() => new()
    {
        CgMicroscopeLens = CustomerAdaptToMapper.Mapper<MicroscopeMagnificationInfo, CgMicroscopeLens>(HighMicroscopeMagnificationInfo),
        ScaleT = AppliedScaleT,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    public ChuckRotateScaleErrorDto Clone() => new()
    {
        LowMicroscopeMagnificationInfo = LowMicroscopeMagnificationInfo,
        HighMicroscopeMagnificationInfo = HighMicroscopeMagnificationInfo,
        IsPositive = IsPositive,
        ChuckCenterBrightFieldPosition = ChuckCenterBrightFieldPosition,
        AppliedScaleT = AppliedScaleT,
        ResultScaleT = ResultScaleT,
        ScaleErrorValueAverage = ScaleErrorValueAverage,
        RealAngleErrorsAverage = RealAngleErrorsAverage,
        PositiveLeftHighSiteRealPosition = PositiveLeftHighSiteRealPosition,
        PositiveRightHighSiteRealPosition = PositiveRightHighSiteRealPosition,
        PositiveTopHighSiteRealPosition = PositiveTopHighSiteRealPosition,
        PositiveBottomHighSiteRealPosition = PositiveBottomHighSiteRealPosition,
        NegativeLeftHighSiteRealPosition = NegativeLeftHighSiteRealPosition,
        NegativeRightHighSiteRealPosition = NegativeRightHighSiteRealPosition,
        NegativeTopHighSiteRealPosition = NegativeTopHighSiteRealPosition,
        NegativeBottomHighSiteRealPosition = NegativeBottomHighSiteRealPosition,
        PositiveLeftFindResultFilePath = PositiveLeftFindResultFilePath,
        PositiveRightFindResultFilePath = PositiveRightFindResultFilePath,
        PositiveTopFindResultFilePath = PositiveTopFindResultFilePath,
        PositiveBottomFindResultFilePath = PositiveBottomFindResultFilePath,
        NegativeLeftFindResultFilePath = NegativeLeftFindResultFilePath,
        NegativeRightFindResultFilePath = NegativeRightFindResultFilePath,
        NegativeTopFindResultFilePath = NegativeTopFindResultFilePath,
        NegativeBottomFindResultFilePath = NegativeBottomFindResultFilePath,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };
}