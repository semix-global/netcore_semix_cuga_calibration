using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Laser.OpticalPowerMeter;

public sealed partial class LaserOpticalPowerMeterCache : CalibrationCacheBase<LaserOpticalPowerMeterCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private int _calibratingRetryTimes = 10;

    [ObservableProperty]
    private double _threshold = 0.05;

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<ProductivityInformation, LaserOpticalPowerMeterCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, LaserOpticalPowerMeterCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
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
    private Point _findMachinePosition;

    [ObservableProperty]
    private double _waitTime = 5;

    [ObservableProperty]
    private int _rowCount = 5;

    [ObservableProperty]
    private int _columnCount = 5;

    [ObservableProperty]
    private double _columnWidth = 100;

    [ObservableProperty]
    private double _rowHeight = 100;

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