using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.AOD.BestFocusAndAstigmatism;

public partial class AODBestFocusAndAstigmatismCache : CalibrationCacheBase
{
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
    public AODBestFocusAndAstigmatismCacheItem Item => Items.GetOrAdd((ProductivityInformation, ApodizationModeEnum), new Lazy<AODBestFocusAndAstigmatismCacheItem>(() => new AODBestFocusAndAstigmatismCacheItem()));

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
}

public partial class AODBestFocusAndAstigmatismCacheItem : CalibrationCacheBase
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

}