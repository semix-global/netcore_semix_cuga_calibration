using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.AOD.Uniformity;

public sealed partial class AODUniformityCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private int _calibratingRetryTimes = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrateThresholdMin), nameof(CalibrateThresholdMax))]
    private double _calibrateThreshold = 0.05;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]

    public double CalibrateThresholdMin => 1 - CalibrateThreshold;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]

    public double CalibrateThresholdMax => 1 + CalibrateThreshold;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReviewThresholdMin), nameof(ReviewThresholdMax))]
    private double _reviewThreshold = 0.05;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]

    public double ReviewThresholdMin => 1 - ReviewThreshold;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]

    public double ReviewThresholdMax => 1 + ReviewThreshold;

    public ConcurrentBag<KeyValuePair<(ProductivityInformation ProductivityInformation, LaserLightInformation LaserLightInformation), AODUniformityCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]

    public AODUniformityCacheItem Item => Items.GetOrAdd((ProductivityInformation, LaserLightInformation), new AODUniformityCacheItem());
}

public sealed partial class AODUniformityCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private Point _hazeFindBFMachinePosition;

    [ObservableProperty]
    private int _imageWidth = 1000;

    [ObservableProperty]
    private int _prescanAODWaveformProfileSegmentCount = 20;

    [ObservableProperty]
    private int _imageHorizontalProjectsSegmentCount = 100;

    [ObservableProperty]
    private int _initializeWindowLinearSpacedCount = 11;

    [ObservableProperty]
    private double _initializeWindowLinearSpacedRate = 0.5;

    [ObservableProperty]
    private int _imageHorizontalProjectsSkipCout;

    [ObservableProperty]
    private int _imageHorizontalProjectsSkipLastCout;

    [ObservableProperty]
    private double _windowLimitRate = 0.2;

    [ObservableProperty]
    private double _windowInterval = 0.01;

    [ObservableProperty]
    private double _waitTime = 5;
}