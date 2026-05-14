using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;
using Net.Utilities.Models.Serializations;

namespace Core.Models.Models.CIB.XTC;

public sealed partial class CIBXTCCache : CalibrationCacheBase<CIBXTCCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial int CalibratingRetryTimes { get; set; } = 5;

    [ObservableProperty]
    public partial double CalibratingThreshold { get; set; } = 1;

    [ObservableProperty]
    public partial double ReviewThreshold { get; set; } = 1;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, CIBXTCCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, CIBXTCCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public CIBXTCCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new CIBXTCCacheItem());

    public override CIBXTCCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        CalibratingRetryTimes = CalibratingRetryTimes,
        CalibratingThreshold = CalibratingThreshold,
        ReviewThreshold = ReviewThreshold,
        Items = new ConcurrentDictionary<ProductivityInformation, CIBXTCCacheItem>(Items.Select(t => new KeyValuePair<ProductivityInformation, CIBXTCCacheItem>(t.Key.Clone(), t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}

public sealed partial class CIBXTCCacheItem : CalibrationCacheBase<CIBXTCCacheItem>
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
    public partial Point HazeFindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial int PrescanAODWaveformProfileSegmentCount { get; set; } = 10;

    public override CIBXTCCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        CIBInformation = CIBInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        HazeFindBFMachinePosition = HazeFindBFMachinePosition,
        ImageWidth = ImageWidth,
        PrescanAODWaveformProfileSegmentCount = PrescanAODWaveformProfileSegmentCount,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}