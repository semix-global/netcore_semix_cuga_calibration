using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Pattern;
using System.Collections.Concurrent;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Models.Serializations;

namespace Core.Models.Models.Chuck.AlignmentDegreeOffset;

[CacheVersion("1.0.0")]
public sealed partial class ChuckAlignmentDegreeOffsetCache : CalibrationCacheBase<ChuckAlignmentDegreeOffsetCache>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation LowMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial MicroscopeLensInformation HighMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    /// <summary>
    /// 晶圆类型
    /// </summary>
    [ObservableProperty]
    public partial AlgorithmWaferTypeEnum AlgorithmWaferTypeEnum { get; set; } = AlgorithmWaferTypeEnum.D300;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<(OpticsIlluminationModeEnum, ProductivityInformation), ChuckAlignmentDegreeOffsetCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, ChuckAlignmentDegreeOffsetCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public ChuckAlignmentDegreeOffsetCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new ChuckAlignmentDegreeOffsetCacheItem());

    [ObservableProperty]
    public partial double NccTypeTemplateMatchScoreThreshold { get; set; } = 0.8;

    [ObservableProperty]
    public partial double TeachingThreshold { get; set; }

    [ObservableProperty]
    public partial double VerifyThreshold { get; set; }

    public override ChuckAlignmentDegreeOffsetCache Clone() => new()
    {
        LowMicroscopeLensInformation = LowMicroscopeLensInformation.Clone(),
        HighMicroscopeLensInformation = HighMicroscopeLensInformation.Clone(),
        AlgorithmWaferTypeEnum = AlgorithmWaferTypeEnum,
        ProductivityInformation = ProductivityInformation.Clone(),
        Items = new ConcurrentDictionary<ProductivityInformation, ChuckAlignmentDegreeOffsetCacheItem>(Items.Select(t => new KeyValuePair<ProductivityInformation, ChuckAlignmentDegreeOffsetCacheItem>(t.Key.Clone(), t.Value.Clone()))),
        NccTypeTemplateMatchScoreThreshold = NccTypeTemplateMatchScoreThreshold,
        TeachingThreshold = TeachingThreshold,
        VerifyThreshold = VerifyThreshold,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}

public sealed partial class ChuckAlignmentDegreeOffsetCacheItem : CalibrationCacheBase<ChuckAlignmentDegreeOffsetCacheItem>
{
    [ObservableProperty]
    public partial int XWidthPixel { get; set; } = 800;

    public override ChuckAlignmentDegreeOffsetCacheItem Clone() => new()
    {
        XWidthPixel = XWidthPixel,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}