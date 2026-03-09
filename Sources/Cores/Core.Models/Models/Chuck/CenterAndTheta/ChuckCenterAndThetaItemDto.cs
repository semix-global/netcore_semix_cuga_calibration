using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Chuck;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.CenterAndTheta;

public sealed partial class ChuckCenterAndThetaItemDto : CalibrationDtoBase, ICloneable<ChuckCenterAndThetaItemDto>, IAdaptTo<CalibrationChuckCenterAndThetaObj>
{
    [ObservableProperty]
    private MicroscopeLensInformation _lowMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private bool _isPositive;

    [ObservableProperty]
    private StageDirectionTypeEnum _siteDirection = StageDirectionTypeEnum.Up;

    [ObservableProperty]
    private ChuckGlobalTemplateMatchDtoItem _positiveMatchResult = new();

    [ObservableProperty]
    private ChuckGlobalTemplateMatchDtoItem _negativeMatchResult = new();

    #region Center

    [ObservableProperty]
    private Point _chuckCenterPosition;

    [ObservableProperty]
    private Point _bFCenterStagePosition;

    [ObservableProperty]
    private Point _newBFCenterStagePosition;

    [ObservableProperty]
    private string _filePath = string.Empty;

    #endregion

    #region Scale

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
    private double _scaleErrorUmAverage;

    /// <summary>
    /// 四个预设点位正反向旋转后的实际角度差值的均值
    /// </summary>
    [ObservableProperty]
    private double _realAngleOffsetAverage;

    #endregion

    public void SetMatchResultInfo(Point point, string findResultImageFilePath)
    {
        switch (SiteDirection)
        {
            case StageDirectionTypeEnum.Up:
                {
                    if (IsPositive)
                    {
                        PositiveMatchResult.TopPosition = point;
                        PositiveMatchResult.TopFindResultImageFilePath = findResultImageFilePath;
                    }
                    else
                    {
                        NegativeMatchResult.TopPosition = point;
                        NegativeMatchResult.TopFindResultImageFilePath = findResultImageFilePath;
                    }
                }
                break;

            case StageDirectionTypeEnum.Down:
                {
                    if (IsPositive)
                    {
                        PositiveMatchResult.BottomPosition = point;
                        PositiveMatchResult.BottomFindResultImageFilePath = findResultImageFilePath;
                    }
                    else
                    {
                        NegativeMatchResult.BottomPosition = point;
                        NegativeMatchResult.BottomFindResultImageFilePath = findResultImageFilePath;
                    }
                }
                break;

            case StageDirectionTypeEnum.Left:
                {
                    if (IsPositive)
                    {
                        PositiveMatchResult.LeftPosition = point;
                        PositiveMatchResult.LeftFindResultImageFilePath = findResultImageFilePath;
                    }
                    else
                    {
                        NegativeMatchResult.LeftPosition = point;
                        NegativeMatchResult.LeftFindResultImageFilePath = findResultImageFilePath;
                    }
                }
                break;

            case StageDirectionTypeEnum.Right:
                {
                    if (IsPositive)
                    {
                        PositiveMatchResult.RightPosition = point;
                        PositiveMatchResult.RightFindResultImageFilePath = findResultImageFilePath;
                    }
                    else
                    {
                        NegativeMatchResult.RightPosition = point;
                        NegativeMatchResult.RightFindResultImageFilePath = findResultImageFilePath;
                    }
                }
                break;
        }
    }

    public Point GetPosition()
    {
        return SiteDirection switch
        {
            StageDirectionTypeEnum.Up => IsPositive ? PositiveMatchResult.TopPosition : NegativeMatchResult.TopPosition,
            StageDirectionTypeEnum.Down => IsPositive ? PositiveMatchResult.BottomPosition : NegativeMatchResult.BottomPosition,
            StageDirectionTypeEnum.Left => IsPositive ? PositiveMatchResult.LeftPosition : NegativeMatchResult.LeftPosition,
            StageDirectionTypeEnum.Right => IsPositive ? PositiveMatchResult.RightPosition : NegativeMatchResult.RightPosition,
            _ => Point.Origin
        };
    }

    public (Point positiveRealPosition, Point negativeRealPosition) GetCoupleRealPosition(StageDirectionTypeEnum siteDirection)
    {
        return siteDirection switch
        {
            StageDirectionTypeEnum.Up => (PositiveMatchResult.TopPosition, NegativeMatchResult.TopPosition),
            StageDirectionTypeEnum.Down => (PositiveMatchResult.BottomPosition, NegativeMatchResult.BottomPosition),
            StageDirectionTypeEnum.Left => (PositiveMatchResult.LeftPosition, NegativeMatchResult.LeftPosition),
            StageDirectionTypeEnum.Right => (PositiveMatchResult.RightPosition, NegativeMatchResult.RightPosition),
            _ => (Point.Origin, Point.Origin)
        };
    }

    #region Mapper

    public ChuckCenterAndThetaItemDto Clone() => new()
    {
        LowMicroscopeLensInformation = LowMicroscopeLensInformation,
        HighMicroscopeLensInformation = HighMicroscopeLensInformation,
        IsPositive = IsPositive,
        PositiveMatchResult = PositiveMatchResult.Clone(),
        NegativeMatchResult = NegativeMatchResult.Clone(),
        ChuckCenterPosition = ChuckCenterPosition,
        BFCenterStagePosition = BFCenterStagePosition,
        NewBFCenterStagePosition = NewBFCenterStagePosition,
        FilePath = FilePath,
        AppliedScaleT = AppliedScaleT,
        ResultScaleT = ResultScaleT,
        ScaleErrorUmAverage = ScaleErrorUmAverage,
        RealAngleOffsetAverage = RealAngleOffsetAverage,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationChuckCenterAndThetaObj AdaptTo() => new()
    {
        CgMicroscopeLens = HighMicroscopeLensInformation != MicroscopeLensInformation.Default ? HighMicroscopeLensInformation.AdaptTo().LensCode : CgMicroscopeLens.None,
        NewBFCenterStagePosition = NewBFCenterStagePosition.ToCgPoint(),
        ScaleT = AppliedScaleT,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}