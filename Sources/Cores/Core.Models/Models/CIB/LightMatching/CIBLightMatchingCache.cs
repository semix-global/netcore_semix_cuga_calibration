using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.CIB.LightMatching;

public sealed partial class CIBLightMatchingCache : CalibrationCacheBase<CIBLightMatchingCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private int _hazeCalibratingRetryTimes = 10;

    [ObservableProperty]
    private int _silicaSphereCalibratingRetryTimes = 10;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingHazeThreshold), nameof(ReviewHazeThreshold))]
    private double _hazeThreshold = 8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingSilicaSphereThreshold), nameof(CalibratingSilicaSphereThreshold))]
    private double _silicaSphereThreshold = 8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingHazeThreshold), nameof(ReviewSilicaSphereThreshold))]
    private double _calibratingThresholdRangeRatio = 0.5;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public double CalibratingHazeThreshold => HazeThreshold * CalibratingThresholdRangeRatio;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public double CalibratingSilicaSphereThreshold => SilicaSphereThreshold * CalibratingThresholdRangeRatio;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReviewHazeThreshold), nameof(ReviewSilicaSphereThreshold))]
    private double _reviewThresholdRangeRatio = 0.8;

    public double ReviewHazeThreshold => HazeThreshold * ReviewThresholdRangeRatio;

    public double ReviewSilicaSphereThreshold => SilicaSphereThreshold * ReviewThresholdRangeRatio;

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<ProductivityInformation, CIBLightMatchingCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, CIBLightMatchingCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public CIBLightMatchingCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new CIBLightMatchingCacheItem());

    public override CIBLightMatchingCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        HazeCalibratingRetryTimes = HazeCalibratingRetryTimes,
        SilicaSphereCalibratingRetryTimes = SilicaSphereCalibratingRetryTimes,
        HazeThreshold = HazeThreshold,
        SilicaSphereThreshold = SilicaSphereThreshold,
        CalibratingThresholdRangeRatio = CalibratingThresholdRangeRatio,
        ReviewThresholdRangeRatio = ReviewThresholdRangeRatio,
        Items = new ConcurrentDictionary<ProductivityInformation, CIBLightMatchingCacheItem>(Items.Select(t => new KeyValuePair<ProductivityInformation, CIBLightMatchingCacheItem>(t.Key.Clone(), t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}

public sealed partial class CIBLightMatchingCacheItem : CalibrationCacheBase<CIBLightMatchingCacheItem>
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private Point _hazeFindBFMachinePosition;

    [ObservableProperty]
    private Point _silicaSphereFindBFMachinePosition;

    [ObservableProperty]
    private int _imageWidth = 1000;

    public override CIBLightMatchingCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        HazeFindBFMachinePosition = HazeFindBFMachinePosition,
        SilicaSphereFindBFMachinePosition = SilicaSphereFindBFMachinePosition,
        ImageWidth = ImageWidth,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}