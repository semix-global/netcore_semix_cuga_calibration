using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Optics.Relay;

public sealed partial class OpticsRelayCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum;

    public ConcurrentBag<KeyValuePair<OpticsIlluminationModeEnum, OpticsRelayCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public OpticsRelayCacheItem Item => Items.GetOrAdd(OpticsIlluminationModeEnum, new OpticsRelayCacheItem());
}

public sealed partial class OpticsRelayCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private Point _findBFMachinePosition;

    [ObservableProperty]
    private int _imageWidth = 1000;

    [ObservableProperty]
    private double _opticsIlluminationModeDegreeAngle = 90;

    [ObservableProperty]
    private double _defaultRelayMotorSlope = 100;

    [ObservableProperty]
    private double _currentRelayMotorAbsoluteValue;

    [ObservableProperty]
    private double _startRelayMotorAbsoluteValue;

    [ObservableProperty]
    private double _stepRelayMotorAbsoluteValue;

    [ObservableProperty]
    private double _stopRelayMotorAbsoluteValue;

    [ObservableProperty]
    private double _startRoughECS;

    [ObservableProperty]
    private double _stepRoughECS;

    [ObservableProperty]
    private double _stopRoughECS;

    [ObservableProperty]
    private double _rangeRefinedECS;

    [ObservableProperty]
    private double _stepRefinedECS;
}