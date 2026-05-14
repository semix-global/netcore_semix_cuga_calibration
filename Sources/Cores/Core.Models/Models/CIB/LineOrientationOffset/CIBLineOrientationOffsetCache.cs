using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;
using Net.Utilities.Models.Serializations;

namespace Core.Models.Models.CIB.LineOrientationOffset;

public sealed partial class CIBLineOrientationOffsetCache : CalibrationCacheBase<CIBLineOrientationOffsetCache>
{
    [ObservableProperty]
    public partial double PmtInterval { get; set; } = 320;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, CIBLineOrientationOffsetCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, CIBLineOrientationOffsetCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public CIBLineOrientationOffsetCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new CIBLineOrientationOffsetCacheItem());

    public override CIBLineOrientationOffsetCache Clone() => new()
    {
        PmtInterval = PmtInterval,
        ProductivityInformation = ProductivityInformation.Clone(),
        Items = new ConcurrentDictionary<ProductivityInformation, CIBLineOrientationOffsetCacheItem>(Items.Select(t => new KeyValuePair<ProductivityInformation, CIBLineOrientationOffsetCacheItem>(t.Key.Clone(), t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class CIBLineOrientationOffsetCacheItem : CalibrationCacheBase<CIBLineOrientationOffsetCacheItem>
{
    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Die Pitch Width must be greater than 0.1.")]
    public double DiePitchWidth
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 5100;

    [Comparison(1000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Wafer Radius: ")]
    public double WaferRadius
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 150_000;

    [Comparison(1, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Reticle Reference Die Col Count must be greater than 1.")]
    public int ReticleDieCountX
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 1;

    [ObservableProperty]
    public partial int ImageCount { get; set; }

    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial WaferMaskTypeEnum WaferMaskTypeEnum { get; set; } = WaferMaskTypeEnum.GridConrner_100um;

    [ObservableProperty]
    public partial int XWidthPixel { get; set; } = 800;

    [ObservableProperty]
    public partial double Threshold { get; set; }

    [ObservableProperty]
    public partial bool IsDarkFieldAlignment { get; set; }

    /// <summary>
    /// 选定特征的明场坐标
    /// </summary>
    [ObservableProperty]
    public partial Point FindPosition { get; set; }

    [ObservableProperty]
    public partial Point StartPosition { get; set; }

    [ObservableProperty]
    public partial Point EndPosition { get; set; }

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    [ObservableProperty]
    public partial string BrightTemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BrightTemplateImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateImageFilePath { get; set; } = string.Empty;

    public override CIBLineOrientationOffsetCacheItem Clone() => new()
    {
        DiePitchWidth = DiePitchWidth,
        WaferRadius = WaferRadius,
        ReticleDieCountX = ReticleDieCountX,
        ImageCount = ImageCount,
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        CIBInformation = CIBInformation.Clone(),
        WaferMaskTypeEnum = WaferMaskTypeEnum,
        XWidthPixel = XWidthPixel,
        Threshold = Threshold,
        IsDarkFieldAlignment = IsDarkFieldAlignment,
        FindPosition = FindPosition,
        StartPosition = StartPosition,
        EndPosition = EndPosition,
        AlignmentResult = AlignmentResult.Clone(),
        BrightTemplateFilePath = BrightTemplateFilePath,
        BrightTemplateImageFilePath = BrightTemplateImageFilePath,
        TemplateFilePath = TemplateFilePath,
        TemplateImageFilePath = TemplateImageFilePath,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}