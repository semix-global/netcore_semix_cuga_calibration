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
    [NotifyPropertyChangedFor(nameof(Item))]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum = CalibrationConstantsHelper.MainOpticsIlluminationModeEnum;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private int _calibratingRetryTimes = 5;

    [ObservableProperty]
    private double _threshold = 0.05;

    public ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), LaserOpticalPowerMeterCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public LaserOpticalPowerMeterCacheItem Item => Items.GetOrAdd((OpticsIlluminationModeEnum, ProductivityInformation), new LaserOpticalPowerMeterCacheItem());
}

public sealed partial class LaserOpticalPowerMeterCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private Point _findMachinePosition;
    
    [ObservableProperty]
    private double _waitTime = 5;

    [ObservableProperty]
    private int _rowCount = 11;

    [ObservableProperty]
    private int _columnCount = 11;

    [ObservableProperty]
    private double _columnWidth = 100;

    [ObservableProperty]
    private double _rowHeight = 100;
}