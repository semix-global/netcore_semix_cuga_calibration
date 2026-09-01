using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using System.Collections.Concurrent;

namespace Core.Models.Models.Microscope.PixelSize;

public sealed partial class MicroscopePixelSizeCache : CalibrationCacheBase<MicroscopePixelSizeCache>
{
    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    /// <summary>
    /// 网格水平角度阈值
    /// </summary>
    [ObservableProperty]
    public partial double AngleThreshold { get; set; }

    [ObservableProperty]
    public partial Size Threshold { get; set; }

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<MicroscopeLensInformation, MicroscopePixelSizeCacheItem>))]
    public ConcurrentDictionary<MicroscopeLensInformation, MicroscopePixelSizeCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public MicroscopePixelSizeCacheItem Item => Items.GetOrAdd(MicroscopeLensInformation, _ => new MicroscopePixelSizeCacheItem());

    public override MicroscopePixelSizeCache Clone() => new()
    {
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        AngleThreshold = AngleThreshold,
        Threshold = Threshold,
        Items = new ConcurrentDictionary<MicroscopeLensInformation, MicroscopePixelSizeCacheItem>(Items.Select(t => new KeyValuePair<MicroscopeLensInformation, MicroscopePixelSizeCacheItem>(t.Key.Clone(), t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class MicroscopePixelSizeCacheItem : CalibrationCacheBase<MicroscopePixelSizeCacheItem>
{
    [ObservableProperty]
    public partial WaferMaskTypeEnum WaferMaskTypeEnum { get; set; } = WaferMaskTypeEnum.Grid_100um;

    [ObservableProperty]
    public partial AlgorithmStandardMaskSquareSizeEnum AlgorithmStandardMaskSquareSizeEnum { get; set; } = AlgorithmStandardMaskSquareSizeEnum.Size10;

    [ObservableProperty]
    public partial Point FindPosition { get; set; }

    public override MicroscopePixelSizeCacheItem Clone() => new()
    {
        WaferMaskTypeEnum = WaferMaskTypeEnum,
        AlgorithmStandardMaskSquareSizeEnum = AlgorithmStandardMaskSquareSizeEnum,
        FindPosition = FindPosition,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}
