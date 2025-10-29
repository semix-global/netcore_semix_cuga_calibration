using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Core.Utilities;
using LiteDB;
using Net.Utilities.Models.Geometries;
using Newtonsoft.Json;

namespace Core.Models.Models.Laser.OpticalPowerMeter;

public sealed partial class LaserOpticalPowerMeterCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _threshold = 0.05;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, LaserOpticalPowerMeterCacheItem>> Items { get; init; } = [];

    [JsonIgnore]
    [BsonIgnore]
    public LaserOpticalPowerMeterCacheItem Item => Items.GetOrAdd(ProductivityInformation, new LaserOpticalPowerMeterCacheItem());
}

public sealed partial class LaserOpticalPowerMeterCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private int _rowNumber = 11;

    [ObservableProperty]
    private int _columnNumber = 11;

    [ObservableProperty]
    private double _columnCellWidth = 100;

    [ObservableProperty]
    private double _rowCellHeight = 100;

    [ObservableProperty]
    private double _waitTime = 15;

    [ObservableProperty]
    private int _repeatCount = 5;
}