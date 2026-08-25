using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using System.Collections.Concurrent;

namespace Core.Models.Models.CIB.XPixelSize;

public sealed partial class CIBXPixelSizeCache : CalibrationCacheBase<CIBXPixelSizeCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.ChuckModel;

    [ObservableProperty]
    public partial double Threshold { get; set; } = 15;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, CIBXPixelSizeCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, CIBXPixelSizeCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public CIBXPixelSizeCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new CIBXPixelSizeCacheItem());

    public override CIBXPixelSizeCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        Threshold = Threshold,
        Items = new ConcurrentDictionary<ProductivityInformation, CIBXPixelSizeCacheItem>(Items.Select(t => new KeyValuePair<ProductivityInformation, CIBXPixelSizeCacheItem>(t.Key.Clone(), t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class CIBXPixelSizeCacheItem : CalibrationCacheBase<CIBXPixelSizeCacheItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial bool IsDarkFieldAlignment { get; set; }

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial WaferMaskTypeEnum WaferMaskTypeEnum { get; set; } = WaferMaskTypeEnum.Caliper;

    [ObservableProperty]
    public partial Point FindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial string TemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double WaferRadius { get; set; } = 140_000;

    [ObservableProperty]
    public partial double DiePitchWidth { get; set; } = 5100;

    [ObservableProperty]
    public partial int ReticleDieCountX { get; set; } = 3;

    public override CIBXPixelSizeCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        CIBInformation = CIBInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        IsDarkFieldAlignment = IsDarkFieldAlignment,
        AlignmentResult = AlignmentResult.Clone(),
        ImageWidth = ImageWidth,
        WaferMaskTypeEnum = WaferMaskTypeEnum,
        FindBFMachinePosition = FindBFMachinePosition,
        TemplateFilePath = TemplateFilePath,
        TemplateImageFilePath = TemplateImageFilePath,
        WaferRadius = WaferRadius,
        DiePitchWidth = DiePitchWidth,
        ReticleDieCountX = ReticleDieCountX,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}