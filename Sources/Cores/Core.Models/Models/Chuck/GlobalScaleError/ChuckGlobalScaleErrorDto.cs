using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Chuck;
using Cuga.Data.DataStruct.Microscope.Enums;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.GlobalScaleError;

[CacheVersion("1.0.0")]
public sealed partial class ChuckGlobalScaleErrorDto : CalibrationDTOBase<ChuckGlobalScaleErrorDto>, IAdaptTo<CalibrationChuckGlobalScaleError>
{
    [ObservableProperty]
    private MicroscopeLensInformation _lowMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private StageDirectionTypeEnum _siteDirection = StageDirectionTypeEnum.Up;

    /// <summary>
    /// 当前应用的X/Y轴比例误差系数
    /// </summary>
    [ObservableProperty]
    private System.Windows.Point _appliedScaleXY = new(1.0, 1.0);

    /// <summary>
    /// 应用X/Y轴比例误差系数的结果比例
    /// </summary>
    [ObservableProperty]
    private System.Windows.Point _resultScaleXY;

    /// <summary>
    /// 误差值um（x轴，Y轴）
    /// </summary>
    [ObservableProperty]
    private Point _scaleErrorValue;

    [ObservableProperty]
    private double _p5Angle;

    [ObservableProperty]
    private ChuckGlobalTemplateMatchDtoItem _highSiteMatchResult = new();

    public void SetMatchResultInfo(Point point, string findResultImageFilePath)
    {
        switch (SiteDirection)
        {
            case StageDirectionTypeEnum.Up:
                {
                    HighSiteMatchResult.TopPosition = point;
                    HighSiteMatchResult.TopFindResultImageFilePath = findResultImageFilePath;
                }
                break;

            case StageDirectionTypeEnum.Down:
                {
                    HighSiteMatchResult.BottomPosition = point;
                    HighSiteMatchResult.BottomFindResultImageFilePath = findResultImageFilePath;
                }
                break;

            case StageDirectionTypeEnum.Left:
                {
                    HighSiteMatchResult.LeftPosition = point;
                    HighSiteMatchResult.LeftFindResultImageFilePath = findResultImageFilePath;
                }
                break;

            case StageDirectionTypeEnum.Right:
                {
                    HighSiteMatchResult.RightPosition = point;
                    HighSiteMatchResult.RightFindResultImageFilePath = findResultImageFilePath;
                }
                break;
        }
    }

    public Point GetPosition(StageDirectionTypeEnum siteDirection)
    {
        return siteDirection switch
        {
            StageDirectionTypeEnum.Up => HighSiteMatchResult.TopPosition,
            StageDirectionTypeEnum.Down => HighSiteMatchResult.BottomPosition,
            StageDirectionTypeEnum.Left => HighSiteMatchResult.LeftPosition,
            StageDirectionTypeEnum.Right => HighSiteMatchResult.RightPosition,
            _ => Point.Origin
        };
    }

    #region Mapper

    public override ChuckGlobalScaleErrorDto Clone() => new()
    {
        LowMicroscopeLensInformation = LowMicroscopeLensInformation,
        HighMicroscopeLensInformation = HighMicroscopeLensInformation,
        AppliedScaleXY = AppliedScaleXY,
        ResultScaleXY = ResultScaleXY,
        ScaleErrorValue = ScaleErrorValue,
        P5Angle = P5Angle,
        HighSiteMatchResult = HighSiteMatchResult.Clone(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationChuckGlobalScaleError AdaptTo() => new()
    {
        CgMicroscopeLens = HighMicroscopeLensInformation != MicroscopeLensInformation.Default ? HighMicroscopeLensInformation.AdaptTo().LensCode : CgMicroscopeLens.None,
        ScaleX = AppliedScaleXY.X,
        ScaleY = AppliedScaleXY.Y,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}

public sealed partial class ChuckGlobalTemplateMatchDtoItem : ObservableObject, ICloneable<ChuckGlobalTemplateMatchDtoItem>
{
    [ObservableProperty]
    private MicroscopeLensInformation _lensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private Point _topPosition = Point.Origin;

    [ObservableProperty]
    private Point _leftPosition = Point.Origin;

    [ObservableProperty]
    private Point _bottomPosition = Point.Origin;

    [ObservableProperty]
    private Point _rightPosition = Point.Origin;

    [ObservableProperty]
    private string _topFindResultImageFilePath = string.Empty;

    [ObservableProperty]
    private string _bottomFindResultImageFilePath = string.Empty;

    [ObservableProperty]
    private string _leftFindResultImageFilePath = string.Empty;

    [ObservableProperty]
    private string _rightFindResultImageFilePath = string.Empty;

    public ChuckGlobalTemplateMatchDtoItem Clone() => new()
    {
        LensInformation = LensInformation.Clone(),
        TopPosition = TopPosition,
        BottomPosition = BottomPosition,
        LeftPosition = LeftPosition,
        RightPosition = RightPosition,
        TopFindResultImageFilePath = TopFindResultImageFilePath,
        BottomFindResultImageFilePath = BottomFindResultImageFilePath,
        LeftFindResultImageFilePath = LeftFindResultImageFilePath,
        RightFindResultImageFilePath = RightFindResultImageFilePath
    };
}