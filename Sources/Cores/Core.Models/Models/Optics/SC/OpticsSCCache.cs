using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using System.Collections.Concurrent;

namespace Core.Models.Models.Optics.SC;

public sealed partial class OpticsSCCache : CalibrationCacheBase<OpticsSCCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial OpticsIlluminationModeEnum OpticsIlluminationModeEnum { get; set; }

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<OpticsIlluminationModeEnum, OpticsSCCacheItem>))]
    public ConcurrentDictionary<OpticsIlluminationModeEnum, OpticsSCCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public OpticsSCCacheItem Item => Items.GetOrAdd(OpticsIlluminationModeEnum, _ => new OpticsSCCacheItem());

    public override OpticsSCCache Clone() => new()
    {
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        Items = new ConcurrentDictionary<OpticsIlluminationModeEnum, OpticsSCCacheItem>(Items.Select(t => new KeyValuePair<OpticsIlluminationModeEnum, OpticsSCCacheItem>(t.Key, t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class OpticsSCCacheItem : CalibrationCacheBase<OpticsSCCacheItem>
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
    public partial double ScanLength { get; set; }

    [ObservableProperty]
    public partial double StartLambda { get; set; }

    [ObservableProperty]
    public partial double StepLambda { get; set; }

    [ObservableProperty]
    public partial double StopLambda { get; set; }

    [ObservableProperty]
    public partial double LambdaToL1Coefficient { get; set; }

    [ObservableProperty]
    public partial double LambdaToL3Coefficient { get; set; }

    [ObservableProperty]
    public partial double SCMotorAbsoluteValueL1Center { get; set; }

    [ObservableProperty]
    public partial double SCMotorAbsoluteValueL3Center { get; set; }

    [ObservableProperty]
    public partial bool IsL1ToL2Direction { get; set; }

    [ObservableProperty]
    public partial bool IsL2ToL3Direction { get; set; }

    [ObservableProperty]
    public partial double CenterECS { get; set; }

    [ObservableProperty]
    public partial double RangeECS { get; set; }

    public override OpticsSCCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        ProductivityInformation = ProductivityInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        CIBInformation = CIBInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        AlignmentResult = AlignmentResult.Clone(),
        DSWFindBFMachinePosition = DSWFindBFMachinePosition,
        ScanLength = ScanLength,
        StartLambda = StartLambda,
        StepLambda = StepLambda,
        StopLambda = StopLambda,
        LambdaToL1Coefficient = LambdaToL1Coefficient,
        LambdaToL3Coefficient = LambdaToL3Coefficient,
        SCMotorAbsoluteValueL1Center = SCMotorAbsoluteValueL1Center,
        SCMotorAbsoluteValueL3Center = SCMotorAbsoluteValueL3Center,
        IsL1ToL2Direction = IsL1ToL2Direction,
        IsL2ToL3Direction = IsL2ToL3Direction,
        CenterECS = CenterECS,
        RangeECS = RangeECS,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}