using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;
using Net.Utilities.Models.Serializations;

namespace Core.Models.Models.CIB.YPixelSize;

public sealed partial class CIBYPixelSizeCache : CalibrationCacheBase<CIBYPixelSizeCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.ChuckModel;

    [ObservableProperty]
    public partial double PmtInterval { get; set; } = 320;

    [ObservableProperty]
    public partial double Threshold { get; set; }

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, CIBYPixelSizeCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, CIBYPixelSizeCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public CIBYPixelSizeCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new CIBYPixelSizeCacheItem());

    public override CIBYPixelSizeCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        PmtInterval = PmtInterval,
        Threshold = Threshold,
        Items = new ConcurrentDictionary<ProductivityInformation, CIBYPixelSizeCacheItem>(Items.Select(t => new KeyValuePair<ProductivityInformation, CIBYPixelSizeCacheItem>(t.Key.Clone(), t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}

public sealed partial class CIBYPixelSizeCacheItem : CalibrationCacheBase<CIBYPixelSizeCacheItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial int CIBChannelId { get; set; } = 3;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial WaferMaskTypeEnum WaferMaskTypeEnum { get; set; } = WaferMaskTypeEnum.Grid_10um;

    [ObservableProperty]
    public partial Point FindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    private bool _isDarkFieldAlignment;

    public override CIBYPixelSizeCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        CIBChannelId = CIBChannelId,
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        WaferMaskTypeEnum = WaferMaskTypeEnum,
        FindBFMachinePosition = FindBFMachinePosition,
        ImageWidth = ImageWidth,
        IsDarkFieldAlignment = IsDarkFieldAlignment,
        AlignmentResult = AlignmentResult.Clone(),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}