using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.AutoFocus.GlobalFocusOffset;

public sealed partial class AutoFocusGlobalFocusOffsetCache : CalibrationCacheBase<AutoFocusGlobalFocusOffsetCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<ProductivityInformation, AutoFocusGlobalFocusOffsetCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, AutoFocusGlobalFocusOffsetCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public AutoFocusGlobalFocusOffsetCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new AutoFocusGlobalFocusOffsetCacheItem());

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

    public override AutoFocusGlobalFocusOffsetCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        Items = new ConcurrentDictionary<ProductivityInformation, AutoFocusGlobalFocusOffsetCacheItem>(Items.Select(x => new KeyValuePair<ProductivityInformation, AutoFocusGlobalFocusOffsetCacheItem>(x.Key.Clone(), x.Value.Clone()))),
        OriginAFMotor = OriginAFMotor,
        OriginRelayMotor = OriginRelayMotor,
        QualityThreshold = QualityThreshold,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}

public sealed partial class AutoFocusGlobalFocusOffsetCacheItem : CalibrationCacheBase<AutoFocusGlobalFocusOffsetCacheItem>
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

    public override AutoFocusGlobalFocusOffsetCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        CIBInformation = CIBInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        RTFCBrightFieldMachinePosition = RTFCBrightFieldMachinePosition,
        ImageWidth = ImageWidth,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}