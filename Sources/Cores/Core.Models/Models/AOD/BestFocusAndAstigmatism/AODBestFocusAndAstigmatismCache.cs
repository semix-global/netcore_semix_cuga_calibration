using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using System.Collections.Concurrent;

namespace Core.Models.Models.AOD.BestFocusAndAstigmatism;

public partial class AODBestFocusAndAstigmatismCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private OpticsApodizationModeEnum _apodizationModeEnum;

    public ConcurrentBag<KeyValuePair<(ProductivityInformation, OpticsApodizationModeEnum), AODBestFocusAndAstigmatismCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]

    public AODBestFocusAndAstigmatismCacheItem Item => Items.GetOrAdd((ProductivityInformation, ApodizationModeEnum), new AODBestFocusAndAstigmatismCacheItem());

    [ObservableProperty]
    private double _pmtInterval = 320; // Pmt相机采集间隔320um

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private bool _isDarkFieldAlignment;

    [ObservableProperty]
    private IReadOnlyList<int> _pMTIds = [];

    /// <summary>
    /// 采样率 count/ms
    /// </summary>
    [ObservableProperty]
    private double _traceBufferSamplingRate = 1d;
}

public partial class AODBestFocusAndAstigmatismCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private bool _isMultiPMTOnceCollection = true;

    [ObservableProperty]
    private AlgorithmImageQualityTypeEnum _algorithmImageQualityTypeEnum;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private ImageCollectionConfiguration _imageCollectionConfiguration = new() { IsAutoFocus = false, IsForward = true, IsCustomEcs = false };

    [ObservableProperty]
    private double _zMinLimit;

    [ObservableProperty]
    private double _zMaxLimit;

    [ObservableProperty]
    private double _centerPositionAfEcs;

    #region 频率参数

    /// <summary>
    /// Chirp AOD波形频率增长次数 
    /// </summary>
    [ObservableProperty]
    private int _spectralDensityStepCount;

    /// <summary>
    /// Chirp AOD波形频率增长步距 
    /// </summary>
    [ObservableProperty]
    private double _stepSpectralDensity;

    #endregion 频率参数

    #region 波形参数

    /// <summary>
    /// ChirpAod默认波形生成参数
    /// </summary>
    [ObservableProperty]
    private GenerateChirpAODWaveformParam _defaultGenerateChirpAODWaveformParam = new();

    /// <summary>
    /// 起始频率变化率 
    /// </summary>
    [ObservableProperty]
    private double _startSpectralDensity;

    #endregion 波形参数

    [ObservableProperty]
    private double _xQualityThreshold;

    [ObservableProperty]
    private double _yQualityThreshold;

    [ObservableProperty]
    private double _xYBestFocusEcsOffsetThreshold;
}