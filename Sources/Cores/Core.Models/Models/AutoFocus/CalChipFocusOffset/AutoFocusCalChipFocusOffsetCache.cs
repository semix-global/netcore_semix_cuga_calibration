using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.AutoFocus.CalChipFocusOffset;

public sealed partial class AutoFocusCalChipFocusOffsetCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private Point _chuckRTFCBrightFieldMachinePosition = Point.Origin;

    [ObservableProperty]
    private int _imageWidth = 1000;

    /// <summary>
    /// 校准 chuck、dsw、haze rtfc输出的电机offset和校准结果的差值需小于该阈值 
    /// </summary>
    [ObservableProperty]
    private double _motorOffsetThreshold;

    /// <summary>
    /// 验证时下发校准的结果，暗场采图图像得分和rtfc图像得分差值小于该阈值
    /// </summary>
    [ObservableProperty]
    private double _verifyQualityThreshold;

    /// <summary>
    /// 电机值cuga当前配置位置，防呆用
    /// </summary>
    [ObservableProperty]
    private double _originAFMotor;

    /// <summary>
    /// 电机值cuga当前配置位置，防呆用
    /// </summary>
    [ObservableProperty]
    private double _originRelayMotor;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

    public ConcurrentBag<KeyValuePair<CalChipSiteModelEnum, AutoFocusCalChipFocusOffsetCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public AutoFocusCalChipFocusOffsetCacheItem Item => Items.GetOrAdd(CalChipSiteModelEnum, new AutoFocusCalChipFocusOffsetCacheItem());
}

public sealed partial class AutoFocusCalChipFocusOffsetCacheItem : ObservableValidator
{
    [ObservableProperty]
    private OpticsConfiguration _opticsConfiguration = new();

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private Point _calChipRTFCBrightFieldMachinePosition = Point.Origin;
}