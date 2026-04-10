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

    [ObservableProperty]
    private double _threshold = 0.999;

    [ObservableProperty]
    private double _rateThreshold = 0.05;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, LaserAttenuatorCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public LaserAttenuatorCacheItem Item => Items.GetOrAdd(ProductivityInformation, new Lazy<LaserAttenuatorCacheItem>(() => new LaserAttenuatorCacheItem()));
}

public sealed partial class LaserAttenuatorCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private double _waitTime = 5;

    [ObservableProperty]
    private double _startCoefficient = 0.01;

    [ObservableProperty]
    private double _stepCoefficient = 0.02;

    [ObservableProperty]
    private double _stopCoefficient = 1;
}