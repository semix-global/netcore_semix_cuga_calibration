using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;
using Net.Utilities.Models.Serializations;

namespace Core.Models.Models.AOD.Alignment;

public sealed partial class AODAlignmentCache : CalibrationCacheBase<AODAlignmentCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial double Threshold { get; set; } = 0.999;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, AODAlignmentCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, AODAlignmentCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public AODAlignmentCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new AODAlignmentCacheItem());

    public override AODAlignmentCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        Threshold = Threshold,
        Items = new ConcurrentDictionary<ProductivityInformation, AODAlignmentCacheItem>(Items.Select(x => new KeyValuePair<ProductivityInformation, AODAlignmentCacheItem>(x.Key.Clone(), x.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class AODAlignmentCacheItem : CalibrationCacheBase<AODAlignmentCacheItem>
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
    public partial GeneratePrescanAODWaveformParam FlatnessGeneratePrescanAODWaveformParam { get; set; } = new() { FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Flatness };

    [ObservableProperty]
    public partial double StartPrescanFrequency { get; set; }

    [ObservableProperty]
    public partial double StepPrescanFrequency { get; set; } = 10;

    [ObservableProperty]
    public partial double StopPrescanFrequency { get; set; }

    [ObservableProperty]
    public partial int RangeSkipFitCount { get; set; } = 1;

    public override AODAlignmentCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        CIBInformation = CIBInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        HazeFindBFMachinePosition = HazeFindBFMachinePosition,
        ImageWidth = ImageWidth,
        FlatnessGeneratePrescanAODWaveformParam = FlatnessGeneratePrescanAODWaveformParam.Clone(),
        StartPrescanFrequency = StartPrescanFrequency,
        StepPrescanFrequency = StepPrescanFrequency,
        StopPrescanFrequency = StopPrescanFrequency,
        RangeSkipFitCount = RangeSkipFitCount,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}