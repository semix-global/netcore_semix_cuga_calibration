using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.CIB.IlluminationProfile;

public sealed partial class CIBIlluminationProfileCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private int _calibratingRetryTimes = 5;

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

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<ProductivityInformation, CIBIlluminationProfileCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, CIBIlluminationProfileCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public CIBIlluminationProfileCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new CIBIlluminationProfileCacheItem());
}

public sealed partial class CIBIlluminationProfileCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private Point _hazeFindBFMachinePosition;

    [ObservableProperty]
    private int _imageWidth = 1000;
}