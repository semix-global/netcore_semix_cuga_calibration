using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Optics.INC;

public sealed partial class OpticsINCCache : CalibrationCacheBase<OpticsINCCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<ProductivityInformation, OpticsINCCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, OpticsINCCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public OpticsINCCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new OpticsINCCacheItem());

    public override OpticsINCCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        Items = new ConcurrentDictionary<ProductivityInformation, OpticsINCCacheItem>(Items.Select(t => new KeyValuePair<ProductivityInformation, OpticsINCCacheItem>(t.Key.Clone(), t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class OpticsINCCacheItem : CalibrationCacheBase<OpticsINCCacheItem>
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

    public override OpticsINCCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        CIBInformation = CIBInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        HazeFindBFMachinePosition = HazeFindBFMachinePosition,
        ImageWidth = ImageWidth,
        StartINCMotorAbsoluteValue = StartINCMotorAbsoluteValue,
        StepINCMotorAbsoluteValue = StepINCMotorAbsoluteValue,
        StopINCMotorAbsoluteValue = StopINCMotorAbsoluteValue,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}