using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using System.Collections.Concurrent;

namespace Core.Models.Models.CIB.AGCDelay;

public sealed partial class CIBAGCDelayCache : CalibrationCacheBase<CIBAGCDelayCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial int CalibratingRetryTimes { get; set; } = 5;

    [ObservableProperty]
    public partial double CalibratingThreshold { get; set; } = 1;

    [ObservableProperty]
    public partial double ReviewThreshold { get; set; } = 2;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, CIBAGCDelayCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, CIBAGCDelayCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public CIBAGCDelayCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new CIBAGCDelayCacheItem());

    public override CIBAGCDelayCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        CalibratingRetryTimes = CalibratingRetryTimes,
        CalibratingThreshold = CalibratingThreshold,
        ReviewThreshold = ReviewThreshold,
        Items = new ConcurrentDictionary<ProductivityInformation, CIBAGCDelayCacheItem>(Items.Select(t => new KeyValuePair<ProductivityInformation, CIBAGCDelayCacheItem>(t.Key.Clone(), t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class CIBAGCDelayCacheItem : CalibrationCacheBase<CIBAGCDelayCacheItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial Point HazeFindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial double StartCoefficient { get; set; } = 0.01;

    [ObservableProperty]
    public partial double StepCoefficient { get; set; } = 0.01;

    [ObservableProperty]
    public partial double StopCoefficient { get; set; } = 1d;

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial double TargetPMTValue { get; set; } = 400d;

    [ObservableProperty]
    public partial int MarkerLengthPixel { get; set; } = 30;

    public override CIBAGCDelayCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        HazeFindBFMachinePosition = HazeFindBFMachinePosition,
        StartCoefficient = StartCoefficient,
        StepCoefficient = StepCoefficient,
        StopCoefficient = StopCoefficient,
        ImageWidth = ImageWidth,
        TargetPMTValue = TargetPMTValue,
        MarkerLengthPixel = MarkerLengthPixel,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}