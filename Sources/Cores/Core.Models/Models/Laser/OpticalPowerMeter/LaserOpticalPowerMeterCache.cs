using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Laser.OpticalPowerMeter;

public sealed partial class LaserOpticalPowerMeterCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private int _calibratingRetryTimes = 10;

    [ObservableProperty]
    private double _threshold = 0.05;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, LaserOpticalPowerMeterCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public LaserOpticalPowerMeterCacheItem Item => Items.GetOrAdd(ProductivityInformation, new Lazy<LaserOpticalPowerMeterCacheItem>(() => new LaserOpticalPowerMeterCacheItem()));
}

public sealed partial class LaserOpticalPowerMeterCacheItem : CalibrationCacheBase
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
}