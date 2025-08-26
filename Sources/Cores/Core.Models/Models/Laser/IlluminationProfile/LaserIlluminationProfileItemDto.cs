using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.IlluminationProfile;

public sealed partial class LaserIlluminationProfileItemDto : CalibrationDtoBase, ICloneable<LaserIlluminationProfileItemDto>, IAdaptTo<CalibrationLaserIlluminationProfileItem>
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private double _coefficient;

    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = new();

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private int _pmtId;

    [ObservableProperty]
    private int _channelId;

    /// <summary>
    /// prescan文件路径
    /// </summary>
    [ObservableProperty]
    private string _prescanFilePath = string.Empty;

    /// <summary>
    /// prescan文件路径
    /// </summary>
    [ObservableProperty]
    private string _resultPrescanFilePath = string.Empty;

    [ObservableProperty]
    private Point _findPosition;

    /// <summary>
    /// ch1图片
    /// </summary>
    [ObservableProperty]
    private string _channel1ImageFilePath = string.Empty;

    /// <summary>
    /// ch1暗场图片向y方向投影的列表
    /// </summary>
    [ObservableProperty]
    private List<double> _channel1DarkFieldImageList = [];

    /// <summary>
    /// ch2图片
    /// </summary>
    [ObservableProperty]
    private string _channel2ImageFilePath = string.Empty;

    /// <summary>
    /// ch2暗场图片向y方向投影的列表
    /// </summary>
    [ObservableProperty]
    private List<double> _channel2DarkFieldImageList = [];

    /// <summary>
    /// ch3图片
    /// </summary>
    [ObservableProperty]
    private string _channel3ImageFilePath = string.Empty;

    /// <summary>
    /// ch3暗场图片向y方向投影的列表
    /// </summary>
    [ObservableProperty]
    private List<double> _channel3DarkFieldImageList = [];

    /// <summary>
    /// 指定通道暗场图片
    /// </summary>
    [ObservableProperty]
    private string _channelImageFilePath = string.Empty;

    /// <summary>
    /// 指定通道暗场图片向y方向投影的列表 [原始组成：所有光斑的指定通道投影，所有光斑通道像素对应的投影均值]
    /// </summary>
    [ObservableProperty]
    private List<double> _channelDarkFieldProjectYsList = [];

    /// <summary>
    /// 比值最小值
    /// </summary>
    [ObservableProperty]
    private double _darkFieldImageListRateMin;

    /// <summary>
    /// 比值最大值
    /// </summary>
    [ObservableProperty]
    private double _darkFieldImageListRateMax;

    /// <summary>
    /// prescan计算后的幅值比例
    /// </summary>
    [ObservableProperty]
    private List<double> _prescanRateList = [];

    /// <summary>
    /// P偏振功率
    /// </summary>
    [ObservableProperty]
    private double _polarizationPPower;

    /// <summary>
    /// S偏振功率
    /// </summary>
    [ObservableProperty]
    private double _polarizationSPower;

    /// <summary>
    /// C偏振功率
    /// </summary>
    [ObservableProperty]
    private double _polarizationCPower;

    #region Mapper

    public LaserIlluminationProfileItemDto Clone() => new()
    {
        Index = Index,
        Coefficient = Coefficient,
        MicroscopeLensInformation = MicroscopeLensInformation,
        OpticsMagTypeEnum = OpticsMagTypeEnum,
        PmtId = PmtId,
        ChannelId = ChannelId,
        PrescanFilePath = PrescanFilePath,
        ResultPrescanFilePath = ResultPrescanFilePath,
        FindPosition = FindPosition,
        Channel1ImageFilePath = Channel1ImageFilePath,
        Channel1DarkFieldImageList = [.. Channel1DarkFieldImageList],
        Channel2ImageFilePath = Channel2ImageFilePath,
        Channel2DarkFieldImageList = [.. Channel2DarkFieldImageList],
        Channel3ImageFilePath = Channel3ImageFilePath,
        Channel3DarkFieldImageList = [.. Channel3DarkFieldImageList],
        DarkFieldImageListRateMin = DarkFieldImageListRateMin,
        DarkFieldImageListRateMax = DarkFieldImageListRateMax,
        PrescanRateList = [.. PrescanRateList],
        PolarizationPPower = PolarizationPPower,
        PolarizationSPower = PolarizationSPower,
        PolarizationCPower = PolarizationCPower,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserIlluminationProfileItem AdaptTo() => new()
    {
        Coefficient = Coefficient,
        OpticsMagTypeEnum = OpticsMagTypeEnum.ToCgMagTypeEnum(),
        ResultPrescanFilePath = ResultPrescanFilePath,
        PolarizationPPower = PolarizationPPower,
        PolarizationSPower = PolarizationSPower,
        PolarizationCPower = PolarizationCPower,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}