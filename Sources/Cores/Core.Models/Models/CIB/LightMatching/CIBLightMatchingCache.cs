using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;
using Net.Utilities.Models.Serializations;

namespace Core.Models.Models.CIB.LightMatching;

public sealed partial class CIBLightMatchingCache : CalibrationCacheBase<CIBLightMatchingCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial int HazeCalibratingRetryTimes { get; set; } = 10;

    [ObservableProperty]
    public partial int SilicaSphereCalibratingRetryTimes { get; set; } = 10;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingHazeThreshold), nameof(ReviewHazeThreshold))]
    public partial double HazeThreshold { get; set; } = 8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingSilicaSphereThreshold), nameof(CalibratingSilicaSphereThreshold))]
    public partial double SilicaSphereThreshold { get; set; } = 8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingHazeThreshold), nameof(ReviewSilicaSphereThreshold))]
    public partial double CalibratingThresholdRangeRatio { get; set; } = 0.5;

    [Newtonsoft.Json.JsonIgnore]
    public double CalibratingHazeThreshold => HazeThreshold * CalibratingThresholdRangeRatio;

    [Newtonsoft.Json.JsonIgnore]
    public double CalibratingSilicaSphereThreshold => SilicaSphereThreshold * CalibratingThresholdRangeRatio;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReviewHazeThreshold), nameof(ReviewSilicaSphereThreshold))]
    public partial double ReviewThresholdRangeRatio { get; set; } = 0.8;

    public double ReviewHazeThreshold => HazeThreshold * ReviewThresholdRangeRatio;

    public double ReviewSilicaSphereThreshold => SilicaSphereThreshold * ReviewThresholdRangeRatio;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, CIBLightMatchingCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, CIBLightMatchingCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public CIBLightMatchingCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new CIBLightMatchingCacheItem());

    public override CIBLightMatchingCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        HazeCalibratingRetryTimes = HazeCalibratingRetryTimes,
        SilicaSphereCalibratingRetryTimes = SilicaSphereCalibratingRetryTimes,
        HazeThreshold = HazeThreshold,
        SilicaSphereThreshold = SilicaSphereThreshold,
        CalibratingThresholdRangeRatio = CalibratingThresholdRangeRatio,
        ReviewThresholdRangeRatio = ReviewThresholdRangeRatio,
        Items = new ConcurrentDictionary<ProductivityInformation, CIBLightMatchingCacheItem>(Items.Select(t => new KeyValuePair<ProductivityInformation, CIBLightMatchingCacheItem>(t.Key.Clone(), t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}

public sealed partial class CIBLightMatchingCacheItem : CalibrationCacheBase<CIBLightMatchingCacheItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial Point HazeFindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial Point SilicaSphereFindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    public override CIBLightMatchingCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        HazeFindBFMachinePosition = HazeFindBFMachinePosition,
        SilicaSphereFindBFMachinePosition = SilicaSphereFindBFMachinePosition,
        ImageWidth = ImageWidth,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}