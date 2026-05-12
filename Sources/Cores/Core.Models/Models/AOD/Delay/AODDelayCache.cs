using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.AOD.Delay;

public sealed partial class AODDelayCache : CalibrationCacheBase<AODDelayCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<ProductivityInformation, AODDelayCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, AODDelayCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public AODDelayCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new AODDelayCacheItem());

    public override AODDelayCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        Items = new ConcurrentDictionary<ProductivityInformation, AODDelayCacheItem>(Items.Select(x => new KeyValuePair<ProductivityInformation, AODDelayCacheItem>(x.Key.Clone(), x.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}

public sealed partial class AODDelayCacheItem : CalibrationCacheBase<AODDelayCacheItem>
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
    private Point _hazeFindBFMachinePosition;

    [ObservableProperty]
    private int _imageWidth = 1000;

    [ObservableProperty]
    private double _waitTime = 5;

    [ObservableProperty]
    private double _startRoughAODDelay = -1500;

    [ObservableProperty]
    private double _stepRoughAODDelay = 100;

    [ObservableProperty]
    private double _stopRoughAODDelay = 3000;

    [ObservableProperty]
    private double _rangeRefinedAODDelay = 200;

    [ObservableProperty]
    private double _stepRefinedAODDelay = 10;

    public override AODDelayCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        CIBInformation = CIBInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        HazeFindBFMachinePosition = HazeFindBFMachinePosition,
        ImageWidth = ImageWidth,
        WaitTime = WaitTime,
        StartRoughAODDelay = StartRoughAODDelay,
        StepRoughAODDelay = StepRoughAODDelay,
        StopRoughAODDelay = StopRoughAODDelay,
        RangeRefinedAODDelay = RangeRefinedAODDelay,
        StepRefinedAODDelay = StepRefinedAODDelay,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}