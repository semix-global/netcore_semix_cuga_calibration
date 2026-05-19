using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using System.Collections.Concurrent;

namespace Core.Models.Models.Laser.OpticalPowerMeter;

public sealed partial class LaserOpticalPowerMeterCache : CalibrationCacheBase<LaserOpticalPowerMeterCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial int CalibratingRetryTimes { get; set; } = 10;

    [ObservableProperty]
    public partial double Threshold { get; set; } = 0.05;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, LaserOpticalPowerMeterCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, LaserOpticalPowerMeterCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public LaserOpticalPowerMeterCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new LaserOpticalPowerMeterCacheItem());

    public override LaserOpticalPowerMeterCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        CalibratingRetryTimes = CalibratingRetryTimes,
        Threshold = Threshold,
        Items = new ConcurrentDictionary<ProductivityInformation, LaserOpticalPowerMeterCacheItem>(Items.Select(t => new KeyValuePair<ProductivityInformation, LaserOpticalPowerMeterCacheItem>(t.Key.Clone(), t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class LaserOpticalPowerMeterCacheItem : CalibrationCacheBase<LaserOpticalPowerMeterCacheItem>
{
    [ObservableProperty]
    public partial Point FindMachinePosition { get; set; }

    [ObservableProperty]
    public partial double WaitTime { get; set; } = 5;

    [ObservableProperty]
    public partial int RowCount { get; set; } = 5;

    [ObservableProperty]
    public partial int ColumnCount { get; set; } = 5;

    [ObservableProperty]
    public partial double ColumnWidth { get; set; } = 100;

    [ObservableProperty]
    public partial double RowHeight { get; set; } = 100;

    public override LaserOpticalPowerMeterCacheItem Clone() => new()
    {
        FindMachinePosition = FindMachinePosition,
        WaitTime = WaitTime,
        RowCount = RowCount,
        ColumnCount = ColumnCount,
        ColumnWidth = ColumnWidth,
        RowHeight = RowHeight,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}