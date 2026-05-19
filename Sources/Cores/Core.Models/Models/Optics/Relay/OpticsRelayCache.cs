using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using System.Collections.Concurrent;

namespace Core.Models.Models.Optics.Relay;

public sealed partial class OpticsRelayCache : CalibrationCacheBase<OpticsRelayCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial OpticsIlluminationModeEnum OpticsIlluminationModeEnum { get; set; }

    [ObservableProperty]
    public partial double Threshold { get; set; } = 0.999;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<OpticsIlluminationModeEnum, OpticsRelayCacheItem>))]
    public ConcurrentDictionary<OpticsIlluminationModeEnum, OpticsRelayCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
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
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    [ObservableProperty]
    public partial Point DSWFindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial double OpticsIlluminationDegreeAngle { get; set; } = 90;

    [ObservableProperty]
    public partial double DefaultRelayMotorRatio { get; set; } = 100;

    [ObservableProperty]
    public partial double StartRelayMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double StepRelayMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double StopRelayMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double CenterRoughECS { get; set; }

    [ObservableProperty]
    public partial double RangeRoughECS { get; set; }

    [ObservableProperty]
    public partial double StepRoughECS { get; set; }

    [ObservableProperty]
    public partial double RangeRefinedECS { get; set; }

    [ObservableProperty]
    public partial double StepRefinedECS { get; set; }

    [ObservableProperty]
    public partial Point XZDSWFindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial double XZScanLength { get; set; }

    [ObservableProperty]
    public partial double StartXZRelayMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double StepXZRelayMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double StopXZRelayMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double XZCenterECS { get; set; }

    [ObservableProperty]
    public partial double XZRangeECS { get; set; }

    [ObservableProperty]
    public partial OpticsStrehlRatioQualityTypeEnum OpticsStrehlRatioQualityTypeEnum { get; set; } = OpticsStrehlRatioQualityTypeEnum.XStrehlRatio;

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