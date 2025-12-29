using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using Cuga.Data.DataStruct.Optics;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.IlluminationProfile;

public sealed partial class LaserIlluminationProfileItemDto : CalibrationDtoBase, ICloneable<LaserIlluminationProfileItemDto>, IAdaptTo<CalibrationLaserIlluminationProfileItem>
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private int _pmtId;

    [ObservableProperty]
    private int _channelId;

    /// <summary>
    /// prescan文件路径
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfileList = [];

    /// <summary>
    /// prescan文件路径
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformResult> _prescanAODWaveformResultList = [];

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
        LaserLightInformation = LaserLightInformation.Clone(),
        MicroscopeLensInformation = MicroscopeLensInformation,
        ProductivityInformation = ProductivityInformation.Clone(),
        PmtId = PmtId,
        ChannelId = ChannelId,
        PrescanAODWaveformProfileList = [.. PrescanAODWaveformProfileList.Select(t => t.Clone())],
        PrescanAODWaveformResultList = [.. PrescanAODWaveformResultList.Select(t => t.Clone())],
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
        Coefficient = LaserLightInformation.Coefficient,
        OpticsMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.Default,
        CalibrationPrescanAODWaveformResults =
        [
            .. PrescanAODWaveformResultList
                .Cast<IAdaptTo<CalibrationPrescanAODWaveformResult>>()
                .Select(t => t.AdaptTo())
        ],
        PolarizationPPower = PolarizationPPower,
        PolarizationSPower = PolarizationSPower,
        PolarizationCPower = PolarizationCPower,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}