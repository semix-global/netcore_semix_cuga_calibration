using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Optics.Relay;

public sealed partial class OpticsRelayCache : CalibrationCacheBase<OpticsRelayCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum;

    [ObservableProperty]
    private double _threshold = 0.999;

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<OpticsIlluminationModeEnum, OpticsRelayCacheItem>))]
    public ConcurrentDictionary<OpticsIlluminationModeEnum, OpticsRelayCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public OpticsRelayCacheItem Item => Items.GetOrAdd(OpticsIlluminationModeEnum, _ => new OpticsRelayCacheItem());

    public override OpticsRelayCache Clone() => new()
    {
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        Threshold = Threshold,
        Items = new ConcurrentDictionary<OpticsIlluminationModeEnum, OpticsRelayCacheItem>(Items.Select(t => new KeyValuePair<OpticsIlluminationModeEnum, OpticsRelayCacheItem>(t.Key, t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class OpticsRelayCacheItem : CalibrationCacheBase<OpticsRelayCacheItem>
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private OpticsConfiguration _opticsConfiguration = new();

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private AlignmentResultDto _alignmentResult = new();

    [ObservableProperty]
    private Point _dSWFindBFMachinePosition;

    [ObservableProperty]
    private int _imageWidth = 1000;

    [ObservableProperty]
    private double _opticsIlluminationDegreeAngle = 90;

    [ObservableProperty]
    private double _defaultRelayMotorRatio = 100;

    [ObservableProperty]
    private double _startRelayMotorAbsoluteValue;

    [ObservableProperty]
    private double _stepRelayMotorAbsoluteValue;

    [ObservableProperty]
    private double _stopRelayMotorAbsoluteValue;

    [ObservableProperty]
    private double _centerRoughECS;

    [ObservableProperty]
    private double _rangeRoughECS;

    [ObservableProperty]
    private double _stepRoughECS;

    [ObservableProperty]
    private double _rangeRefinedECS;

    [ObservableProperty]
    private double _stepRefinedECS;

    [ObservableProperty]
    private Point _xZDSWFindBFMachinePosition;

    [ObservableProperty]
    private double _xZScanLength;

    [ObservableProperty]
    private double _startXZRelayMotorAbsoluteValue;

    [ObservableProperty]
    private double _stepXZRelayMotorAbsoluteValue;

    [ObservableProperty]
    private double _stopXZRelayMotorAbsoluteValue;

    [ObservableProperty]
    private double _xZCenterECS;

    [ObservableProperty]
    private double _xZRangeECS;

    [ObservableProperty]
    private OpticsStrehlRatioQualityTypeEnum _opticsStrehlRatioQualityTypeEnum = OpticsStrehlRatioQualityTypeEnum.XStrehlRatio;

    public override OpticsRelayCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        ProductivityInformation = ProductivityInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        CIBInformation = CIBInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        AlignmentResult = AlignmentResult.Clone(),
        DSWFindBFMachinePosition = DSWFindBFMachinePosition,
        ImageWidth = ImageWidth,
        OpticsIlluminationDegreeAngle = OpticsIlluminationDegreeAngle,
        DefaultRelayMotorRatio = DefaultRelayMotorRatio,
        StartRelayMotorAbsoluteValue = StartRelayMotorAbsoluteValue,
        StepRelayMotorAbsoluteValue = StepRelayMotorAbsoluteValue,
        StopRelayMotorAbsoluteValue = StopRelayMotorAbsoluteValue,
        CenterRoughECS = CenterRoughECS,
        RangeRoughECS = RangeRoughECS,
        StepRoughECS = StepRoughECS,
        RangeRefinedECS = RangeRefinedECS,
        StepRefinedECS = StepRefinedECS,
        XZDSWFindBFMachinePosition = XZDSWFindBFMachinePosition,
        XZScanLength = XZScanLength,
        StartXZRelayMotorAbsoluteValue = StartXZRelayMotorAbsoluteValue,
        StepXZRelayMotorAbsoluteValue = StepXZRelayMotorAbsoluteValue,
        StopXZRelayMotorAbsoluteValue = StopXZRelayMotorAbsoluteValue,
        XZCenterECS = XZCenterECS,
        XZRangeECS = XZRangeECS,
        OpticsStrehlRatioQualityTypeEnum = OpticsStrehlRatioQualityTypeEnum,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}