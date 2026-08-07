using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using System.Collections.Concurrent;

namespace Core.Models.Models.Optics.INC;

public sealed partial class OpticsINCCache : CalibrationCacheBase<OpticsINCCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial int SmoothWindowSize { get; set; } = 21;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, OpticsINCCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, OpticsINCCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public OpticsINCCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new OpticsINCCacheItem());

    public override OpticsINCCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        SmoothWindowSize = SmoothWindowSize,
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
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial Point HazeFindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial double StartRoughINCMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double StepRoughINCMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double StopRoughINCMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double RangeRefinedINCMotorAbsoluteValue { get; set; } = 0.2;

    [ObservableProperty]
    public partial double StepRefinedINCMotorAbsoluteValue { get; set; } = 0.01;

    public override OpticsINCCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        CIBInformation = CIBInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        HazeFindBFMachinePosition = HazeFindBFMachinePosition,
        ImageWidth = ImageWidth,
        StartRoughINCMotorAbsoluteValue = StartRoughINCMotorAbsoluteValue,
        StepRoughINCMotorAbsoluteValue = StepRoughINCMotorAbsoluteValue,
        StopRoughINCMotorAbsoluteValue = StopRoughINCMotorAbsoluteValue,
        RangeRefinedINCMotorAbsoluteValue = RangeRefinedINCMotorAbsoluteValue,
        StepRefinedINCMotorAbsoluteValue = StepRefinedINCMotorAbsoluteValue,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}
