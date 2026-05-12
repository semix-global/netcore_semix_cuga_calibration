using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.CIB.LineCentricity;

public sealed partial class CIBLineCentricityCache : CalibrationCacheBase<CIBLineCentricityCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;

    [ObservableProperty]
    private double _pmtInterval = 320; // Pmt相机采集间隔320um

    [ObservableProperty]
    private Point _threshold;

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<ProductivityInformation, CIBLineCentricityCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, CIBLineCentricityCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
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
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private OpticsConfiguration _opticsConfiguration = new();

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private Point _findBFMachinePosition;

    [ObservableProperty]
    private int _imageWidth = 1000;

    [ObservableProperty]
    private string _brightTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _brightTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _templateFilePath = string.Empty;

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