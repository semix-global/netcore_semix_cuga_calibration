using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.CIB.LightMatching;

public sealed partial class CIBLightMatchingCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum = CalibrationConstantsHelper.MainOpticsIlluminationModeEnum;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private int _hazeCalibratingRetryTimes = 5;

    [ObservableProperty]
    private int _silicaSphereCalibratingRetryTimes = 5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingHazeThreshold), nameof(ReviewHazeThreshold))]
    private double _hazeThreshold = 16;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingSilicaSphereThreshold), nameof(CalibratingSilicaSphereThreshold))]
    private double _silicaSphereThreshold = 16;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingHazeThreshold), nameof(ReviewSilicaSphereThreshold))]
    private double _calibratingThresholdRangeRatio = 0.5;

    public double CalibratingHazeThreshold => HazeThreshold * CalibratingThresholdRangeRatio;

    public double CalibratingSilicaSphereThreshold => SilicaSphereThreshold * CalibratingThresholdRangeRatio;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReviewHazeThreshold), nameof(ReviewSilicaSphereThreshold))]
    private double _reviewThresholdRangeRatio = 0.8;

    public double ReviewHazeThreshold => HazeThreshold * ReviewThresholdRangeRatio;

    public double ReviewSilicaSphereThreshold => SilicaSphereThreshold * ReviewThresholdRangeRatio;

    public ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), CIBLightMatchingCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public CIBLightMatchingCacheItem Item => Items.GetOrAdd((OpticsIlluminationModeEnum, ProductivityInformation), new CIBLightMatchingCacheItem());
}

public sealed partial class CIBLightMatchingCacheItem : ObservableObject
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private Point _hazeFindBFMachinePosition;

    [ObservableProperty]
    private Point _silicaSphereFindBFMachinePosition;

    [ObservableProperty]
    private int _imageWidth = 1000;
}