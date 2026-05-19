using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using System.Collections.Concurrent;

namespace Core.Models.Models.Microscope.CalChip;

public sealed partial class MicroscopeCalChipCache : CalibrationCacheBase<MicroscopeCalChipCache>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation LowMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial MicroscopeLensInformation HighMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.DswModel;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<CalChipSiteModelEnum, MicroscopeCalChipCacheItem>))]
    public ConcurrentDictionary<CalChipSiteModelEnum, MicroscopeCalChipCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public MicroscopeCalChipCacheItem Item => Items.GetOrAdd(CalChipSiteModelEnum, _ => new MicroscopeCalChipCacheItem { CalChipSiteModelEnum = CalChipSiteModelEnum });

    [ObservableProperty]
    public partial string VerifyQualityError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double SpeedEcsPerSecond { get; set; } = 500;

    [ObservableProperty]
    public partial double HalfEcsLength { get; set; } = 250;

    /// <summary>
    /// BF verify清晰度得分和校准结果的清晰度差值需小于该阈值
    /// </summary>
    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = $"{nameof(QualityThreshold)}: ")]
    public double QualityThreshold
    {
        get;
        set => SetProperty(ref field, value, validate: true);
    } = 1;

    public override MicroscopeCalChipCache Clone() => new()
    {
        LowMicroscopeLensInformation = LowMicroscopeLensInformation.Clone(),
        HighMicroscopeLensInformation = HighMicroscopeLensInformation.Clone(),
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        Items = new ConcurrentDictionary<CalChipSiteModelEnum, MicroscopeCalChipCacheItem>(Items.Select(t => new KeyValuePair<CalChipSiteModelEnum, MicroscopeCalChipCacheItem>(t.Key, t.Value.Clone()))),
        VerifyQualityError = VerifyQualityError,
        SpeedEcsPerSecond = SpeedEcsPerSecond,
        HalfEcsLength = HalfEcsLength,
        QualityThreshold = QualityThreshold,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class MicroscopeCalChipCacheItem : CalibrationCacheBase<MicroscopeCalChipCacheItem>
{
    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CenterMachinePosition))]
    public partial Point LeftTopMachinePosition { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CenterMachinePosition))]
    public partial Point RightBottomMachinePosition { get; set; }

    public Point CenterMachinePosition => (LeftTopMachinePosition + (Vector)RightBottomMachinePosition) / 2;

    public override MicroscopeCalChipCacheItem Clone() => new()
    {
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        LeftTopMachinePosition = LeftTopMachinePosition,
        RightBottomMachinePosition = RightBottomMachinePosition,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}