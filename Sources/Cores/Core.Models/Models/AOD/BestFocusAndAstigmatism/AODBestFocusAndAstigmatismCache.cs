using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.AOD.BestFocusAndAstigmatism;

public partial class AODBestFocusAndAstigmatismCache : CalibrationCacheBase<AODBestFocusAndAstigmatismCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private OpticsApodizationModeEnum _apodizationModeEnum;

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<(ProductivityInformation, OpticsApodizationModeEnum), AODBestFocusAndAstigmatismCacheItem>))]
    public ConcurrentDictionary<(ProductivityInformation, OpticsApodizationModeEnum), AODBestFocusAndAstigmatismCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public AODBestFocusAndAstigmatismCacheItem Item => Items.GetOrAdd((ProductivityInformation, ApodizationModeEnum), _ => new AODBestFocusAndAstigmatismCacheItem());

    [ObservableProperty]
    private double _pmtInterval = 320; // Pmt相机采集间隔320um

    /// <summary>
    /// 迭代次数
    /// </summary>
    [ObservableProperty]
    private int _times = 5;

    [ObservableProperty]
    private double _xQualityThreshold;

    [ObservableProperty]
    private double _yQualityThreshold;

    [ObservableProperty]
    private double _xYBestFocusEcsOffsetThreshold;

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
        Expiration = Expiration,
    };
}

public partial class AODBestFocusAndAstigmatismCacheItem : CalibrationCacheBase<AODBestFocusAndAstigmatismCacheItem>
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum;

    [ObservableProperty]
    private bool _isDarkFieldAlignment;

    [ObservableProperty]
    private AlgorithmImageQualityTypeEnum _algorithmImageQualityTypeEnum;

    [ObservableProperty]
    private OpticsConfiguration _opticsConfiguration = new();

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private Point _startPosition;

    [ObservableProperty]
    private double _scanLength;

    [ObservableProperty]
    private double _centerECS;

    [ObservableProperty]
    private double _rangeECS;

    #region 频率参数

    /// <summary>
    /// Chirp AOD波形频率增长次数 
    /// </summary>
    [ObservableProperty]
    private int _spectralDensityStepCount;

    /// <summary>
    /// 起始频率变化率
    /// </summary>
    [ObservableProperty]
    private double _startSpectralDensity;

    /// <summary>
    /// Chirp AOD波形频率增长步距 
    /// </summary>
    [ObservableProperty]
    private double _stepSpectralDensity;

    #endregion 频率参数

    /// <summary>
    /// ChirpAod默认波形生成参数
    /// </summary>
    [ObservableProperty]
    private GenerateChirpAODWaveformParam _defaultGenerateChirpAODWaveformParam = new();

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
        Expiration = Expiration,
    };
}