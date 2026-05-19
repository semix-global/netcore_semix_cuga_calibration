using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;
using Net.Utilities.Models.Serializations;

namespace Core.Models.Models.AutoFocus.GlobalFocusOffset;

public sealed partial class AutoFocusGlobalFocusOffsetCache : CalibrationCacheBase<AutoFocusGlobalFocusOffsetCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.DswModel;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, AutoFocusGlobalFocusOffsetCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, AutoFocusGlobalFocusOffsetCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public AutoFocusGlobalFocusOffsetCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new AutoFocusGlobalFocusOffsetCacheItem());

    /// <summary>
    /// 电机值cuga当前配置位置，防呆用
    /// </summary>
    [ObservableProperty]
    public partial double OriginAFMotor { get; set; }

    /// <summary>
    /// 电机值cuga当前配置位置，防呆用
    /// </summary>
    [ObservableProperty]
    public partial double OriginRelayMotor { get; set; }

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
        Expiration = Expiration
    };
}

public sealed partial class AutoFocusGlobalFocusOffsetCacheItem : CalibrationCacheBase<AutoFocusGlobalFocusOffsetCacheItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial Point RTFCBrightFieldMachinePosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

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
        Expiration = Expiration
    };
}