using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using System.Collections.Concurrent;

namespace Core.Models.Models.Optics.GlobalFieldTilt;

public sealed partial class GlobalFieldTiltCache : CalibrationCacheBase<GlobalFieldTiltCache>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial OpticsIlluminationModeEnum OpticsIlluminationModeEnum { get; set; }

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<OpticsIlluminationModeEnum, GlobalFieldTiltCacheItem>))]
    public ConcurrentDictionary<OpticsIlluminationModeEnum, GlobalFieldTiltCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public GlobalFieldTiltCacheItem Item => Items.GetOrAdd(OpticsIlluminationModeEnum, _ => new GlobalFieldTiltCacheItem());

    [ObservableProperty]
    public partial double PmtInterval { get; set; } = 320;

    [ObservableProperty]
    public partial bool IsDarkFieldAlignment { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<int> PMTIds { get; set; } = [];

    [ObservableProperty]
    public partial double UmPerEcs { get; set; } = 0.2;

    [ObservableProperty]
    public partial double OriginDOEPos { get; set; }

    [ObservableProperty]
    public partial double Threshold { get; set; }

    [ObservableProperty]
    public partial double VerifyQualityThreshold { get; set; }

    [ObservableProperty]
    public partial double P5Angle { get; set; }

    public override GlobalFieldTiltCache Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        Items = new ConcurrentDictionary<OpticsIlluminationModeEnum, GlobalFieldTiltCacheItem>(Items.Select(t => new KeyValuePair<OpticsIlluminationModeEnum, GlobalFieldTiltCacheItem>(t.Key, t.Value.Clone()))),
        PmtInterval = PmtInterval,
        IsDarkFieldAlignment = IsDarkFieldAlignment,
        PMTIds = [.. PMTIds],
        UmPerEcs = UmPerEcs,
        OriginDOEPos = OriginDOEPos,
        Threshold = Threshold,
        VerifyQualityThreshold = VerifyQualityThreshold,
        P5Angle = P5Angle,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public partial class GlobalFieldTiltCacheItem : CalibrationCacheBase<GlobalFieldTiltCacheItem>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial bool IsMultiPMTOnceCollection { get; set; } = true;

    [ObservableProperty]
    public partial AlgorithmImageQualityTypeEnum AlgorithmImageQualityTypeEnum { get; set; } = AlgorithmImageQualityTypeEnum.Laplace;

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial ImageCollectionConfiguration ImageCollectionConfiguration { get; set; } = new() { IsAutoFocus = false, IsForward = true, IsCustomEcs = false };

    [ObservableProperty]
    public partial Point FindPosition { get; set; } = Point.Origin;

    /// <summary>
    /// 入射角（°）
    /// </summary>
    [ObservableProperty]
    public partial double ObliqueAngle { get; set; } = 53;

    [ObservableProperty]
    public partial double Threshold { get; set; }

    [ObservableProperty]
    public partial int RetryCount { get; set; } = 5;

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial double CenterECS { get; set; }

    [ObservableProperty]
    public partial double RangeECS { get; set; } = 120;

    [ObservableProperty]
    public partial double StepECS { get; set; } = 20;

    [ObservableProperty]
    public partial double RangeRefinedECS { get; set; } = 50;

    [ObservableProperty]
    public partial double StepRefinedECS { get; set; } = 10;

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    public override GlobalFieldTiltCacheItem Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        IsMultiPMTOnceCollection = IsMultiPMTOnceCollection,
        AlgorithmImageQualityTypeEnum = AlgorithmImageQualityTypeEnum,
        CIBInformation = CIBInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        ImageCollectionConfiguration = ImageCollectionConfiguration.Clone(),
        FindPosition = FindPosition,
        ObliqueAngle = ObliqueAngle,
        Threshold = Threshold,
        RetryCount = RetryCount,
        ImageWidth = ImageWidth,
        CenterECS = CenterECS,
        RangeECS = RangeECS,
        StepECS = StepECS,
        RangeRefinedECS = RangeRefinedECS,
        StepRefinedECS = StepRefinedECS,
        AlignmentResult = AlignmentResult.Clone(),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}