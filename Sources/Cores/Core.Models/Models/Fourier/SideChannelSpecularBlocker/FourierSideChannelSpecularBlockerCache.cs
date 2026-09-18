using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using System.Collections.Concurrent;

namespace Core.Models.Models.Fourier.SideChannelSpecularBlocker;

[CacheVersion("2.0.0")]
public sealed partial class FourierSideChannelSpecularBlockerCache : CalibrationCacheBase<FourierSideChannelSpecularBlockerCache>
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

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, FourierSideChannelSpecularBlockerCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, FourierSideChannelSpecularBlockerCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public FourierSideChannelSpecularBlockerCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new FourierSideChannelSpecularBlockerCacheItem());

    public override FourierSideChannelSpecularBlockerCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        VerifyLaserLightInformation = VerifyLaserLightInformation.Clone(),
        VerifyCIBConfiguration = VerifyCIBConfiguration.Clone(),
        VerifyImageWidth = VerifyImageWidth,
        ExtinctionRatioThreshold = ExtinctionRatioThreshold,
        Items = new ConcurrentDictionary<ProductivityInformation, FourierSideChannelSpecularBlockerCacheItem>(Items.Select(x => new KeyValuePair<ProductivityInformation, FourierSideChannelSpecularBlockerCacheItem>(x.Key.Clone(), x.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class FourierSideChannelSpecularBlockerCacheItem : CalibrationCacheBase<FourierSideChannelSpecularBlockerCacheItem>
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

    public override FourierSideChannelSpecularBlockerCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        ScanLength = ScanLength,
        ShinyWaferFindBFMachinePosition = ShinyWaferFindBFMachinePosition,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}