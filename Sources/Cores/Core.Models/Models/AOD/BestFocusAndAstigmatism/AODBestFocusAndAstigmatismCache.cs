using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;
using Net.Utilities.Models.Serializations;

namespace Core.Models.Models.AOD.BestFocusAndAstigmatism;

public partial class AODBestFocusAndAstigmatismCache : CalibrationCacheBase<AODBestFocusAndAstigmatismCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial OpticsApodizationModeEnum ApodizationModeEnum { get; set; }

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<(ProductivityInformation, OpticsApodizationModeEnum), AODBestFocusAndAstigmatismCacheItem>))]
    public ConcurrentDictionary<(ProductivityInformation, OpticsApodizationModeEnum), AODBestFocusAndAstigmatismCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public AODBestFocusAndAstigmatismCacheItem Item => Items.GetOrAdd((ProductivityInformation, ApodizationModeEnum), _ => new AODBestFocusAndAstigmatismCacheItem());

    [ObservableProperty]
    public partial double PmtInterval { get; set; } = 320;

    /// <summary>
    /// 迭代次数
    /// </summary>
    [ObservableProperty]
    public partial int Times { get; set; } = 5;

    [ObservableProperty]
    public partial double XQualityThreshold { get; set; }

    [ObservableProperty]
    public partial double YQualityThreshold { get; set; }

    [ObservableProperty]
    public partial double XYBestFocusEcsOffsetThreshold { get; set; }

    public override AODBestFocusAndAstigmatismCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        ApodizationModeEnum = ApodizationModeEnum,
        Items = new ConcurrentDictionary<(ProductivityInformation, OpticsApodizationModeEnum), AODBestFocusAndAstigmatismCacheItem>(Items.Select(x => new KeyValuePair<(ProductivityInformation, OpticsApodizationModeEnum), AODBestFocusAndAstigmatismCacheItem>((x.Key.Item1.Clone(), x.Key.Item2), x.Value.Clone()))),
        PmtInterval = PmtInterval,
        Times = Times,
        XQualityThreshold = XQualityThreshold,
        YQualityThreshold = YQualityThreshold,
        XYBestFocusEcsOffsetThreshold = XYBestFocusEcsOffsetThreshold,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public partial class AODBestFocusAndAstigmatismCacheItem : CalibrationCacheBase<AODBestFocusAndAstigmatismCacheItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; }

    [ObservableProperty]
    public partial bool IsDarkFieldAlignment { get; set; }

    [ObservableProperty]
    public partial AlgorithmImageQualityTypeEnum AlgorithmImageQualityTypeEnum { get; set; }

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial Point StartPosition { get; set; }

    [ObservableProperty]
    public partial double ScanLength { get; set; }

    [ObservableProperty]
    public partial double CenterECS { get; set; }

    [ObservableProperty]
    public partial double RangeECS { get; set; }

    #region 频率参数

    /// <summary>
    /// Chirp AOD波形频率增长次数 
    /// </summary>
    [ObservableProperty]
    public partial int SpectralDensityStepCount { get; set; }

    /// <summary>
    /// 起始频率变化率
    /// </summary>
    [ObservableProperty]
    public partial double StartSpectralDensity { get; set; }

    /// <summary>
    /// Chirp AOD波形频率增长步距 
    /// </summary>
    [ObservableProperty]
    public partial double StepSpectralDensity { get; set; }

    #endregion 频率参数

    /// <summary>
    /// ChirpAod默认波形生成参数
    /// </summary>
    [ObservableProperty]
    public partial GenerateChirpAODWaveformParam DefaultGenerateChirpAODWaveformParam { get; set; } = new();

    public override AODBestFocusAndAstigmatismCacheItem Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        IsDarkFieldAlignment = IsDarkFieldAlignment,
        AlgorithmImageQualityTypeEnum = AlgorithmImageQualityTypeEnum,
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        CIBInformation = CIBInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        StartPosition = StartPosition,
        ScanLength = ScanLength,
        CenterECS = CenterECS,
        RangeECS = RangeECS,
        SpectralDensityStepCount = SpectralDensityStepCount,
        StartSpectralDensity = StartSpectralDensity,
        StepSpectralDensity = StepSpectralDensity,
        DefaultGenerateChirpAODWaveformParam = DefaultGenerateChirpAODWaveformParam.Clone(),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}