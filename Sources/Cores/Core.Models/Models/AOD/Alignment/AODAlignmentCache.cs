using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using System.Collections.Concurrent;

namespace Core.Models.Models.AOD.Alignment;

public sealed partial class AODAlignmentCache : CalibrationCacheBase<AODAlignmentCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial double RSquaredThreshold { get; set; } = 0.999;

    [ObservableProperty]
    public partial int MinFitPoints { get; set; } = 3;

    [ObservableProperty]
    public partial int ExcludedEdgePixelCount { get; set; } = 3;

    [ObservableProperty]
    public partial double MaxSubpixelOffset { get; set; } = 0.5;

    [ObservableProperty]
    public partial int DeltaPixels { get; set; }

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, AODAlignmentCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, AODAlignmentCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public AODAlignmentCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new AODAlignmentCacheItem());

    public override AODAlignmentCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        RSquaredThreshold = RSquaredThreshold,
        MinFitPoints = MinFitPoints,
        ExcludedEdgePixelCount = ExcludedEdgePixelCount,
        MaxSubpixelOffset = MaxSubpixelOffset,
        DeltaPixels = DeltaPixels,
        Items = new ConcurrentDictionary<ProductivityInformation, AODAlignmentCacheItem>(Items.Select(x => new KeyValuePair<ProductivityInformation, AODAlignmentCacheItem>(x.Key.Clone(), x.Value.Clone()))),
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
    [NotifyPropertyChangedFor(nameof(CIBInformationsText))]
    public partial IReadOnlyList<CIBInformation> CIBInformations { get; set; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public string CIBInformationsText => string.Join(", ", CIBInformations.OrderBy(t => t.ChannelId));

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
    public partial double StepPrescanFrequency { get; set; } = 10d;

    [ObservableProperty]
    public partial double StopPrescanFrequency { get; set; }

    public override AODAlignmentCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        CIBInformations = [.. CIBInformations.Select(item => item.Clone())],
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        HazeFindBFMachinePosition = HazeFindBFMachinePosition,
        ImageWidth = ImageWidth,
        FlatnessGeneratePrescanAODWaveformParam = FlatnessGeneratePrescanAODWaveformParam.Clone(),
        StartPrescanFrequency = StartPrescanFrequency,
        StepPrescanFrequency = StepPrescanFrequency,
        StopPrescanFrequency = StopPrescanFrequency,
        Id = Id,
        Expiration = Expiration
    };
}