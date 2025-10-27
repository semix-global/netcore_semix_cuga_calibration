using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Utilities;
using LiteDB;
using Net.Utilities.Models.Geometries;
using Newtonsoft.Json;

namespace Core.Models.Models.AOD.AODDelay;

public sealed partial class AODDelayCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _threshold;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, AODDelayCacheItem>> Items { get; init; } = [];

    [JsonIgnore]
    [BsonIgnore]
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
    private double _roughStartAODDelay = -1500;

    [ObservableProperty]
    private double _roughStepAODDelay = 100;

    [ObservableProperty]
    private double _roughStopAODDelay = 1500;

    [ObservableProperty]
    private double _refinedRangeAODDelay = 200;

    [ObservableProperty]
    private double _refinedStepAODDelay = 10;
}