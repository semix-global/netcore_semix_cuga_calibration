using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using System.Collections.Concurrent;

namespace Core.Models.Models.AOD.Delay;

public sealed partial class AODDelayCache : CalibrationCacheBase<AODDelayCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, AODDelayCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, AODDelayCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public AODDelayCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new AODDelayCacheItem());

    public override AODDelayCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        Items = new ConcurrentDictionary<ProductivityInformation, AODDelayCacheItem>(Items.Select(x => new KeyValuePair<ProductivityInformation, AODDelayCacheItem>(x.Key.Clone(), x.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class AODDelayCacheItem : CalibrationCacheBase<AODDelayCacheItem>
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
    public partial double WaitTime { get; set; } = 5;

    [ObservableProperty]
    public partial int SmoothWindow { get; set; } = 21;

    [ObservableProperty]
    public partial double StartRoughAODDelay { get; set; } = -1500;

    [ObservableProperty]
    public partial double StepRoughAODDelay { get; set; } = 100;

    [ObservableProperty]
    public partial double StopRoughAODDelay { get; set; } = 3000;

    [ObservableProperty]
    public partial double RangeRefinedAODDelay { get; set; } = 200;

    [ObservableProperty]
    public partial double StepRefinedAODDelay { get; set; } = 10;

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
        SmoothWindow = SmoothWindow,
        StartRoughAODDelay = StartRoughAODDelay,
        StepRoughAODDelay = StepRoughAODDelay,
        StopRoughAODDelay = StopRoughAODDelay,
        RangeRefinedAODDelay = RangeRefinedAODDelay,
        StepRefinedAODDelay = StepRefinedAODDelay,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}