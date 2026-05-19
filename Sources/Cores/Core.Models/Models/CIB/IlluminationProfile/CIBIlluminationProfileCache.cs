using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;
using Net.Utilities.Models.Serializations;

namespace Core.Models.Models.CIB.IlluminationProfile;

public sealed partial class CIBIlluminationProfileCache : CalibrationCacheBase<CIBIlluminationProfileCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial int CalibratingRetryTimes { get; set; } = 5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrateThresholdMin), nameof(CalibrateThresholdMax))]
    public partial double CalibrateThreshold { get; set; } = 0.05;

    [Newtonsoft.Json.JsonIgnore]
    public double CalibrateThresholdMin => 1 - CalibrateThreshold;

    [Newtonsoft.Json.JsonIgnore]
    public double CalibrateThresholdMax => 1 + CalibrateThreshold;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReviewThresholdMin), nameof(ReviewThresholdMax))]
    public partial double ReviewThreshold { get; set; } = 0.05;

    [Newtonsoft.Json.JsonIgnore]
    public double ReviewThresholdMin => 1 - ReviewThreshold;

    [Newtonsoft.Json.JsonIgnore]
    public double ReviewThresholdMax => 1 + ReviewThreshold;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, CIBIlluminationProfileCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, CIBIlluminationProfileCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public CIBIlluminationProfileCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new CIBIlluminationProfileCacheItem());

    public override CIBIlluminationProfileCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        CalibratingRetryTimes = CalibratingRetryTimes,
        CalibrateThreshold = CalibrateThreshold,
        ReviewThreshold = ReviewThreshold,
        Items = new ConcurrentDictionary<ProductivityInformation, CIBIlluminationProfileCacheItem>(Items.Select(t => new KeyValuePair<ProductivityInformation, CIBIlluminationProfileCacheItem>(t.Key.Clone(), t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class CIBIlluminationProfileCacheItem : CalibrationCacheBase<CIBIlluminationProfileCacheItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial Point HazeFindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    public override CIBIlluminationProfileCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        HazeFindBFMachinePosition = HazeFindBFMachinePosition,
        ImageWidth = ImageWidth,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}