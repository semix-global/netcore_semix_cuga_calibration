using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;
using Net.Utilities.Models.Serializations;

namespace Core.Models.Models.AutoFocus.CalChipFocusOffset;

public sealed partial class AutoFocusCalChipFocusOffsetCache : CalibrationCacheBase<AutoFocusCalChipFocusOffsetCache>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial double SpeedEcsPerSecond { get; set; } = 500;

    [ObservableProperty]
    public partial double HalfEcsLength { get; set; } = 250;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.DswModel;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<CalChipSiteModelEnum, AutoFocusCalChipFocusOffsetCacheItem>))]
    public ConcurrentDictionary<CalChipSiteModelEnum, AutoFocusCalChipFocusOffsetCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public AutoFocusCalChipFocusOffsetCacheItem Item => Items.GetOrAdd(CalChipSiteModelEnum, _ => new AutoFocusCalChipFocusOffsetCacheItem());

    public override AutoFocusCalChipFocusOffsetCache Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        ProductivityInformation = ProductivityInformation.Clone(),
        SpeedEcsPerSecond = SpeedEcsPerSecond,
        HalfEcsLength = HalfEcsLength,
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        Items = new ConcurrentDictionary<CalChipSiteModelEnum, AutoFocusCalChipFocusOffsetCacheItem>(Items.Select(x => new KeyValuePair<CalChipSiteModelEnum, AutoFocusCalChipFocusOffsetCacheItem>(x.Key, x.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class AutoFocusCalChipFocusOffsetCacheItem : CalibrationCacheBase<AutoFocusCalChipFocusOffsetCacheItem>
{
    [ObservableProperty]
    public partial Point FindBrightMachinePosition { get; set; } = Point.Origin;

    public override AutoFocusCalChipFocusOffsetCacheItem Clone() => new()
    {
        FindBrightMachinePosition = FindBrightMachinePosition,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}