using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using System.Collections.Concurrent;

namespace Core.Models.Models.Fourier.V0CenterChannelSpecularBlocker;

[CacheVersion("2.0.0")]
public sealed partial class V0FourierCenterChannelSpecularBlockerCache : CalibrationCacheBase<V0FourierCenterChannelSpecularBlockerCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation VerifyLaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial CIBConfiguration VerifyCIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial int VerifyImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial double ExtinctionRatioThreshold { get; set; } = 0.2;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, V0FourierCenterChannelSpecularBlockerCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, V0FourierCenterChannelSpecularBlockerCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public V0FourierCenterChannelSpecularBlockerCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new V0FourierCenterChannelSpecularBlockerCacheItem());

    public override V0FourierCenterChannelSpecularBlockerCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        VerifyLaserLightInformation = VerifyLaserLightInformation.Clone(),
        VerifyCIBConfiguration = VerifyCIBConfiguration.Clone(),
        VerifyImageWidth = VerifyImageWidth,
        ExtinctionRatioThreshold = ExtinctionRatioThreshold,
        Items = new ConcurrentDictionary<ProductivityInformation, V0FourierCenterChannelSpecularBlockerCacheItem>(Items.Select(x => new KeyValuePair<ProductivityInformation, V0FourierCenterChannelSpecularBlockerCacheItem>(x.Key.Clone(), x.Value.Clone()))),
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class V0FourierCenterChannelSpecularBlockerCacheItem : CalibrationCacheBase<V0FourierCenterChannelSpecularBlockerCacheItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial double ScanLength { get; set; } = 20d;

    [ObservableProperty]
    public partial Point ShinyWaferFindBFMachinePosition { get; set; }

    public override V0FourierCenterChannelSpecularBlockerCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        ScanLength = ScanLength,
        ShinyWaferFindBFMachinePosition = ShinyWaferFindBFMachinePosition,
        Id = Id,
        Expiration = Expiration
    };
}