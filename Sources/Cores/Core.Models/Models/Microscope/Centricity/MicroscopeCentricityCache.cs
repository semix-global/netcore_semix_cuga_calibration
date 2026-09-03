using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using System.Collections.Concurrent;

namespace Core.Models.Models.Microscope.Centricity;

public sealed partial class MicroscopeCentricityCache : CalibrationCacheBase<MicroscopeCentricityCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.DswModel;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<MicroscopeLensInformation, MicroscopeCentricityCacheItem>))]
    public ConcurrentDictionary<MicroscopeLensInformation, MicroscopeCentricityCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public MicroscopeCentricityCacheItem Item => Items.GetOrAdd(MicroscopeLensInformation, _ => new MicroscopeCentricityCacheItem { LensInformation = MicroscopeLensInformation.Clone() });

    [ObservableProperty]
    public partial Point VerifyResultPosition { get; set; }

    [ObservableProperty]
    public partial Point VerifyResultError { get; set; }

    [ObservableProperty]
    public partial Point Threshold { get; set; }

    [ObservableProperty]
    public partial double ConcentricThreshold { get; set; }


    public override MicroscopeCentricityCache Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        Items = new ConcurrentDictionary<MicroscopeLensInformation, MicroscopeCentricityCacheItem>(Items.Select(t => new KeyValuePair<MicroscopeLensInformation, MicroscopeCentricityCacheItem>(t.Key.Clone(), t.Value.Clone()))),
        VerifyResultPosition = VerifyResultPosition,
        VerifyResultError = VerifyResultError,
        Threshold = Threshold,
        ConcentricThreshold = ConcentricThreshold,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class MicroscopeCentricityCacheItem : CalibrationCacheBase<MicroscopeCentricityCacheItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation LensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial WaferMaskTypeEnum WaferMaskTypeEnum { get; set; } = WaferMaskTypeEnum.DieCorner;

    [ObservableProperty]
    public partial Point FindPosition { get; set; }

    [ObservableProperty]
    public partial string TemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateImageFilePath { get; set; } = string.Empty;

    public override MicroscopeCentricityCacheItem Clone() => new()
    {
        LensInformation = LensInformation.Clone(),
        WaferMaskTypeEnum = WaferMaskTypeEnum,
        FindPosition = FindPosition,
        TemplateFilePath = TemplateFilePath,
        TemplateImageFilePath = TemplateImageFilePath,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}