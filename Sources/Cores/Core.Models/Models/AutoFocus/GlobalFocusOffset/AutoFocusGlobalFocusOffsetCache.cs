using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.AutoFocus.GlobalFocusOffset;

public sealed partial class AutoFocusGlobalFocusOffsetCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, AutoFocusGlobalFocusOffsetCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public AutoFocusGlobalFocusOffsetCacheItem Item => Items.GetOrAdd(ProductivityInformation, new Lazy<AutoFocusGlobalFocusOffsetCacheItem>(() => new AutoFocusGlobalFocusOffsetCacheItem()));

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

    /// <summary>
    /// DF verify清晰度得分和校准结果的清晰度差值需小于该阈值
    /// </summary>
    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Quality Threshold: ")]
    public double QualityThreshold
    {
        get;
        set => SetProperty(ref field, value, validate: true);
    } = 1;
}

public sealed partial class AutoFocusGlobalFocusOffsetCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private OpticsConfiguration _opticsConfiguration = new();

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private Point _rTFCBrightFieldMachinePosition = Point.Origin;

    [ObservableProperty]
    private int _imageWidth = 1000;
}