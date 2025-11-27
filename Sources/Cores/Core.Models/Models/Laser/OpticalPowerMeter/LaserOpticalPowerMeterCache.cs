using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Laser.OpticalPowerMeter;

public sealed partial class LaserOpticalPowerMeterCache : CalibrationCacheBase
{
    [ObservableProperty]
    private OpticsIncidentModeEnum _opticsIncidentModeEnum = CalibrationConstantsHelper.MainOpticsIncidentModeEnum;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _threshold = 0.05;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, LaserOpticalPowerMeterCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
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
    private double _waitTime = 5;

    [ObservableProperty]
    private int _repeatCount = 5;
}