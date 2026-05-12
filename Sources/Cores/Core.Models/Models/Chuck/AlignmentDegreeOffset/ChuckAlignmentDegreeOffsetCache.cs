using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using System.Collections.Concurrent;

namespace Core.Models.Models.Chuck.AlignmentDegreeOffset;

public sealed partial class ChuckAlignmentDegreeOffsetCache : CalibrationCacheBase<ChuckAlignmentDegreeOffsetCache>
{
    [ObservableProperty]
    private MicroscopeLensInformation _lowMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = MicroscopeLensInformation.Default;

    /// <summary>
    /// 晶圆类型
    /// </summary>
    [ObservableProperty]
    private AlgorithmWaferTypeEnum _algorithmWaferTypeEnum = AlgorithmWaferTypeEnum.D300;

    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum = CalibrationConstantsHelper.MainOpticsIlluminationModeEnum;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<(OpticsIlluminationModeEnum, ProductivityInformation), ChuckAlignmentDegreeOffsetCacheItem>))]
    public ConcurrentDictionary<(OpticsIlluminationModeEnum, ProductivityInformation), ChuckAlignmentDegreeOffsetCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public ChuckAlignmentDegreeOffsetCacheItem Item => Items.GetOrAdd((OpticsIlluminationModeEnum, ProductivityInformation), _ => new ChuckAlignmentDegreeOffsetCacheItem());

    [ObservableProperty]
    private double _nccTypeTemplateMatchScoreThreshold = 0.8;

    [ObservableProperty]
    private double _teachingThreshold;

    [ObservableProperty]
    private double _verifyThreshold;

    public override ChuckAlignmentDegreeOffsetCache Clone() => new()
    {
        LowMicroscopeLensInformation = LowMicroscopeLensInformation.Clone(),
        HighMicroscopeLensInformation = HighMicroscopeLensInformation.Clone(),
        AlgorithmWaferTypeEnum = AlgorithmWaferTypeEnum,
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        ProductivityInformation = ProductivityInformation.Clone(),
        Items = new ConcurrentDictionary<(OpticsIlluminationModeEnum, ProductivityInformation), ChuckAlignmentDegreeOffsetCacheItem>(Items.Select(t => new KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), ChuckAlignmentDegreeOffsetCacheItem>((t.Key.Item1, t.Key.Item2.Clone()), t.Value.Clone()))),
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
    private int _xWidthPixel = 800;

    public override ChuckAlignmentDegreeOffsetCacheItem Clone() => new()
    {
        XWidthPixel = XWidthPixel,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}