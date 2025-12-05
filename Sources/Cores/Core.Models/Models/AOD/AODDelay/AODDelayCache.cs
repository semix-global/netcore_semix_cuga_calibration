using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.AOD.AODDelay;

public sealed partial class AODDelayCache : CalibrationCacheBase
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum = CalibrationConstantsHelper.MainOpticsIlluminationModeEnum;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _threshold;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, AODDelayCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public AODDelayCacheItem Item => Items.GetOrAdd(ProductivityInformation, new AODDelayCacheItem());
}

public sealed partial class AODDelayCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private double _waitTime = 5;

    [ObservableProperty]
    private int _pMTDataCount = 100;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private int _pMTId = CalibrationConstantsHelper.MainPmtId;

    [ObservableProperty]
    private int _channelId = CalibrationConstantsHelper.MainChannelId;

    [ObservableProperty]
    private Point _findBFMachinePosition;

    [ObservableProperty]
    private double _startRoughAODDelay = -1500;

    [ObservableProperty]
    private double _stepRoughAODDelay = 100;

    [ObservableProperty]
    private double _stopRoughAODDelay = 3000;

    [ObservableProperty]
    private double _rangeRefinedAODDelay = 200;

    [ObservableProperty]
    private double _stepRefinedAODDelay = 10;
}