using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Models.Common.Pattern;
using System.Collections.Concurrent;

namespace Core.Models.Models.Laser.Attenuator;

public sealed partial class LaserAttenuatorCache : CalibrationCacheBase<LaserAttenuatorCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _threshold = 0.999;

    [ObservableProperty]
    private double _rateThreshold = 0.05;

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<ProductivityInformation, LaserAttenuatorCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, LaserAttenuatorCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public LaserAttenuatorCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new LaserAttenuatorCacheItem());

    public override LaserAttenuatorCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        Threshold = Threshold,
        RateThreshold = RateThreshold,
        Items = new ConcurrentDictionary<ProductivityInformation, LaserAttenuatorCacheItem>(Items.Select(t => new KeyValuePair<ProductivityInformation, LaserAttenuatorCacheItem>(t.Key.Clone(), t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class LaserAttenuatorCacheItem : CalibrationCacheBase<LaserAttenuatorCacheItem>
{
    [ObservableProperty]
    private double _waitTime = 5;

    [ObservableProperty]
    private double _startCoefficient = 0.01;

    [ObservableProperty]
    private double _stepCoefficient = 0.02;

    [ObservableProperty]
    private double _stopCoefficient = 1;

    public override LaserAttenuatorCacheItem Clone() => new()
    {
        WaitTime = WaitTime,
        StartCoefficient = StartCoefficient,
        StepCoefficient = StepCoefficient,
        StopCoefficient = StopCoefficient,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}