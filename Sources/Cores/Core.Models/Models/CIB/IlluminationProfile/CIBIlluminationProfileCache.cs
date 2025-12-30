using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
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
    [NotifyPropertyChangedFor(nameof(CalibratingThreshold), nameof(ReviewThreshold))]
    private double _threshold = 16;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingThreshold))]
    private double _calibratingThresholdRangeRatio = 0.5;

    public double CalibratingThreshold => Threshold * CalibratingThresholdRangeRatio;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReviewThreshold))]
    private double _reviewThresholdRangeRatio = 0.8;

    public double ReviewThreshold => Threshold * ReviewThresholdRangeRatio;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, CIBIlluminationProfileCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public CIBIlluminationProfileCacheItem Item => Items.GetOrAdd(ProductivityInformation, new CIBIlluminationProfileCacheItem());
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