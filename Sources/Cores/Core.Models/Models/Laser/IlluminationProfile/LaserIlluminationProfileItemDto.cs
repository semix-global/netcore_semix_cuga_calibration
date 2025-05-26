using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

namespace Core.Models.Models.Laser.IlluminationProfile;

public sealed partial class LaserIlluminationProfileItemDto : CalibrationDtoBase, ICloneable<LaserIlluminationProfileItemDto>, IAdaptTo<CalibrationLaserIlluminationProfileItem>
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private double _coefficient;

    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum;

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
    /// 图片
    /// </summary>
    [ObservableProperty]
    private string _channel1ImageFilePath = string.Empty;

    /// <summary>
    /// 暗场图片向y方向投影的列表
    /// </summary>
    [ObservableProperty]
    private List<double> _channel1DarkFieldImageList = [];

    /// <summary>
    /// 图片
    /// </summary>
    [ObservableProperty]
    private string _channel2ImageFilePath = string.Empty;

    /// <summary>
    /// 暗场图片向y方向投影的列表
    /// </summary>
    [ObservableProperty]
    private List<double> _channel2DarkFieldImageList = [];

    /// <summary>
    /// 图片
    /// </summary>
    [ObservableProperty]
    private string _channel3ImageFilePath = string.Empty;

    /// <summary>
    /// 图片
    /// </summary>
    [ObservableProperty]
    private List<string> _channelImageFilePathList = [];

    /// <summary>
    /// 暗场图片向y方向投影的列表
    /// </summary>
    [ObservableProperty]
    private List<double> _channel3DarkFieldImageList = [];

    /// <summary>
    /// 暗场图片向y方向投影的列表
    /// </summary>
    [ObservableProperty]
    private List<double> _channelDarkFieldImageList = [];

    /// <summary>
    /// 暗场图片向y方向投影的列表
    /// </summary>
    [ObservableProperty]
    private List<List<double>> _channelDarkFieldPmtList = [];

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
        MicroscopeMagnificationEnum = MicroscopeMagnificationEnum,
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