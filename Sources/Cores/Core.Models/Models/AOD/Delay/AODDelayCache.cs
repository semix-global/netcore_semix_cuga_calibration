using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.AOD.Delay;

public sealed partial class AODDelayCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, AODDelayCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public AODDelayCacheItem Item => Items.GetOrAdd(ProductivityInformation, new Lazy<AODDelayCacheItem>(() => new AODDelayCacheItem()));
}

public sealed partial class AODDelayCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private OpticsConfiguration _opticsConfiguration = new();

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private Point _hazeFindBFMachinePosition;

    [ObservableProperty]
    private int _imageWidth = 1000;

    [ObservableProperty]
    private double _waitTime = 5;

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