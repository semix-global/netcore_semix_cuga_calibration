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
    public partial MicroscopeLensInformation LowMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial MicroscopeLensInformation HighMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial StageDirectionTypeEnum SiteDirection { get; set; } = StageDirectionTypeEnum.Up;

    /// <summary>
    /// 当前应用的X/Y轴比例误差系数
    /// </summary>
    [ObservableProperty]
    public partial System.Windows.Point AppliedScaleXY { get; set; } = new(1.0, 1.0);

    /// <summary>
    /// 应用X/Y轴比例误差系数的结果比例
    /// </summary>
    [ObservableProperty]
    public partial System.Windows.Point ResultScaleXY { get; set; }

    /// <summary>
    /// 误差值um（x轴，Y轴）
    /// </summary>
    [ObservableProperty]
    public partial Point ScaleErrorValue { get; set; }

    [ObservableProperty]
    public partial double P5Angle { get; set; }

    [ObservableProperty]
    public partial ChuckGlobalTemplateMatchDtoItem HighSiteMatchResult { get; set; } = new();

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
        LowMicroscopeLensInformation = LowMicroscopeLensInformation.Clone(),
        HighMicroscopeLensInformation = HighMicroscopeLensInformation.Clone(),
        SiteDirection = SiteDirection,
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
    public partial MicroscopeLensInformation LensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial Point TopPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point LeftPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point BottomPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point RightPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial string TopFindResultImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BottomFindResultImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LeftFindResultImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RightFindResultImageFilePath { get; set; } = string.Empty;

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