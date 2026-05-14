using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;
using Net.Utilities.Models.Serializations;

namespace Core.Models.Models.CIB.LineCentricity;

public sealed partial class CIBLineCentricityCache : CalibrationCacheBase<CIBLineCentricityCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.ChuckModel;

    [ObservableProperty]
    public partial double PmtInterval { get; set; } = 320;

    [ObservableProperty]
    public partial Point Threshold { get; set; }

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, CIBLineCentricityCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, CIBLineCentricityCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public CIBLineCentricityCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new CIBLineCentricityCacheItem());

    public override CIBLineCentricityCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        PmtInterval = PmtInterval,
        Threshold = Threshold,
        Items = new ConcurrentDictionary<ProductivityInformation, CIBLineCentricityCacheItem>(Items.Select(t => new KeyValuePair<ProductivityInformation, CIBLineCentricityCacheItem>(t.Key.Clone(), t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}

public sealed partial class CIBLineCentricityCacheItem : CalibrationCacheBase<CIBLineCentricityCacheItem>
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
    public partial Point FindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    private string _brightTemplateFilePath = string.Empty;

    [ObservableProperty]
    public partial string BrightTemplateImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath = string.Empty;

    [ObservableProperty]
    private bool _isDarkFieldAlignment;

    public override CIBLineCentricityCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        CIBInformation = CIBInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        FindBFMachinePosition = FindBFMachinePosition,
        ImageWidth = ImageWidth,
        IsDarkFieldAlignment = IsDarkFieldAlignment,
        AlignmentResult = AlignmentResult.Clone(),
        BrightTemplateFilePath = BrightTemplateFilePath,
        BrightTemplateImageFilePath = BrightTemplateImageFilePath,
        TemplateFilePath = TemplateFilePath,
        TemplateImageFilePath = TemplateImageFilePath,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}