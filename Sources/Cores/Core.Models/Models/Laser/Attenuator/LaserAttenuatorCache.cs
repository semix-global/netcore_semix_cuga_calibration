using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using System.Collections.Concurrent;

namespace Core.Models.Models.Laser.Attenuator;

public sealed partial class LaserAttenuatorCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, LaserAttenuatorCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public LaserAttenuatorCacheItem Item => Items.GetOrAdd(ProductivityInformation, new LaserAttenuatorCacheItem());
}

public sealed partial class LaserAttenuatorCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private double _coefficientStep = 0.02;

    [ObservableProperty]
    private double _waitTime = 5;
}