using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using System.Collections.Concurrent;
using Net.Utilities.Models.Serializations;

namespace Core.Models.Models.Laser.Attenuator;

public sealed partial class LaserAttenuatorCache : CalibrationCacheBase<LaserAttenuatorCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial double Threshold { get; set; } = 0.999;

    [ObservableProperty]
    public partial double RateThreshold { get; set; } = 0.05;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, LaserAttenuatorCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, LaserAttenuatorCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
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
    public partial double WaitTime { get; set; } = 5;

    [ObservableProperty]
    public partial double StartCoefficient { get; set; } = 0.01;

    [ObservableProperty]
    public partial double StepCoefficient { get; set; } = 0.02;

    [ObservableProperty]
    public partial double StopCoefficient { get; set; } = 1;

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