using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.AutoFocus.CalChipFocusOffset;

public sealed partial class AutoFocusCalChipFocusOffsetCache : CalibrationCacheBase<AutoFocusCalChipFocusOffsetCache>
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _speedEcsPerSecond = 500;

    [ObservableProperty]
    private double _halfEcsLength = 250;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<CalChipSiteModelEnum, AutoFocusCalChipFocusOffsetCacheItem>))]
    public ConcurrentDictionary<CalChipSiteModelEnum, AutoFocusCalChipFocusOffsetCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public AutoFocusCalChipFocusOffsetCacheItem Item => Items.GetOrAdd(CalChipSiteModelEnum, _ => new AutoFocusCalChipFocusOffsetCacheItem());

    public override AutoFocusCalChipFocusOffsetCache Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation,
        ProductivityInformation = ProductivityInformation,
        SpeedEcsPerSecond = SpeedEcsPerSecond,
        HalfEcsLength = HalfEcsLength,
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        Items = new ConcurrentDictionary<CalChipSiteModelEnum, AutoFocusCalChipFocusOffsetCacheItem>(Items),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}

public sealed partial class AutoFocusCalChipFocusOffsetCacheItem : ObservableValidator
{
    [ObservableProperty]
    private Point _findBrightMachinePosition = Point.Origin;
}