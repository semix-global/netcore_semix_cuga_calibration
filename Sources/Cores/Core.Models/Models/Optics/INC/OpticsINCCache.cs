using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Optics.INC;

public sealed partial class OpticsINCCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    public ConcurrentDictionary<ProductivityInformation, OpticsINCCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public OpticsINCCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new OpticsINCCacheItem());
}

public sealed partial class OpticsINCCacheItem : CalibrationCacheBase
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
    private double _startINCMotorAbsoluteValue;

    [ObservableProperty]
    private double _stepINCMotorAbsoluteValue;

    [ObservableProperty]
    private double _stopINCMotorAbsoluteValue;
}