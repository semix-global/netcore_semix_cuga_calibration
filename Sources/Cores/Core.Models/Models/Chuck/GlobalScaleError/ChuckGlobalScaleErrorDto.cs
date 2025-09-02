using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Chuck;
using Cuga.Data.DataStruct.Microscope.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.GlobalScaleError;

public sealed partial class ChuckGlobalScaleErrorDto : CalibrationDtoBase, ICloneable<ChuckGlobalScaleErrorDto>, IAdaptTo<CalibrationChuckGlobalScaleError>
{
    [ObservableProperty]
    private MicroscopeLensInformation _lowMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = MicroscopeLensInformation.Default;

    /// <summary>
    /// X轴比例误差系数
    /// </summary>
    [ObservableProperty]
    private double _scaleX = 1.0;

    /// <summary>
    /// Y轴比例误差系数
    /// </summary>
    [ObservableProperty]
    private double _scaleY = 1.0;

    /// <summary>
    /// 误差值um（x轴，Y轴）
    /// </summary>
    [ObservableProperty]
    private Point _scaleErrorValue;

    [ObservableProperty]
    private double _p5Angle;

    #region Real Position

    /// <summary>
    /// wafer左端点实际坐标-低倍镜
    /// </summary>
    [ObservableProperty]
    private Point _leftLowSiteRealPosition;

    /// <summary>
    /// wafer右端点实际坐标-低倍镜
    /// </summary>
    [ObservableProperty]
    private Point _rightLowSiteRealPosition;

    /// <summary>
    /// wafer顶部端点实际坐标-低倍镜
    /// </summary>
    [ObservableProperty]
    private Point _topLowSiteRealPosition;

    /// <summary>
    /// wafer底部端点实际坐标-低倍镜
    /// </summary>
    [ObservableProperty]
    private Point _bottomLowSiteRealPosition;

    /// <summary>
    /// wafer左端点实际坐标-高倍镜
    /// </summary>
    [ObservableProperty]
    private Point _leftHighSiteRealPosition;

    /// <summary>
    /// wafer右端点实际坐标-高倍镜
    /// </summary>
    [ObservableProperty]
    private Point _rightHighSiteRealPosition;

    /// <summary>
    /// wafer顶部端点实际坐标-高倍镜
    /// </summary>
    [ObservableProperty]
    private Point _topHighSiteRealPosition;

    /// <summary>
    /// wafer底部端点实际坐标-高倍镜
    /// </summary>
    [ObservableProperty]
    private Point _bottomHighSiteRealPosition;

    #endregion Real Position

    #region File Path

    [ObservableProperty]
    private string _leftLowSiteFindResultFilePath = string.Empty;

    [ObservableProperty]
    private string _rightLowSiteFindResultFilePath = string.Empty;

    [ObservableProperty]
    private string _topLowSiteFindResultFilePath = string.Empty;

    [ObservableProperty]
    private string _bottomLowSiteFindResultFilePath = string.Empty;

    [ObservableProperty]
    private string _leftHighSiteFindResultFilePath = string.Empty;

    [ObservableProperty]
    private string _rightHighSiteFindResultFilePath = string.Empty;

    [ObservableProperty]
    private string _topHighSiteFindResultFilePath = string.Empty;

    [ObservableProperty]
    private string _bottomHighSiteFindResultFilePath = string.Empty;

    #endregion File Path

    #region Mapper

    public ChuckGlobalScaleErrorDto Clone() => new()
    {
        LowMicroscopeLensInformation = LowMicroscopeLensInformation,
        HighMicroscopeLensInformation = HighMicroscopeLensInformation,
        ScaleX = ScaleX,
        ScaleY = ScaleY,
        ScaleErrorValue = ScaleErrorValue,
        P5Angle = P5Angle,
        LeftLowSiteRealPosition = LeftLowSiteRealPosition,
        RightLowSiteRealPosition = RightLowSiteRealPosition,
        TopLowSiteRealPosition = TopLowSiteRealPosition,
        BottomLowSiteRealPosition = BottomLowSiteRealPosition,
        LeftHighSiteRealPosition = LeftHighSiteRealPosition,
        RightHighSiteRealPosition = RightHighSiteRealPosition,
        TopHighSiteRealPosition = TopHighSiteRealPosition,
        BottomHighSiteRealPosition = BottomHighSiteRealPosition,
        LeftLowSiteFindResultFilePath = LeftLowSiteFindResultFilePath,
        RightLowSiteFindResultFilePath = RightLowSiteFindResultFilePath,
        TopLowSiteFindResultFilePath = TopLowSiteFindResultFilePath,
        BottomLowSiteFindResultFilePath = BottomLowSiteFindResultFilePath,
        LeftHighSiteFindResultFilePath = LeftHighSiteFindResultFilePath,
        RightHighSiteFindResultFilePath = RightHighSiteFindResultFilePath,
        TopHighSiteFindResultFilePath = TopHighSiteFindResultFilePath,
        BottomHighSiteFindResultFilePath = BottomHighSiteFindResultFilePath,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationChuckGlobalScaleError AdaptTo() => new()
    {
        CgMicroscopeLens = HighMicroscopeLensInformation.LensCode == -1 ? 0 : CustomerAdaptToMapper.Mapper<MicroscopeLensInformation, CgMicroscopeLens>(HighMicroscopeLensInformation),
        ScaleX = ScaleX,
        ScaleY = ScaleY,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}