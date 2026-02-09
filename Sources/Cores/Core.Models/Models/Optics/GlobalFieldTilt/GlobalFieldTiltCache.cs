using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Setting;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Optics.GlobalFieldTilt;

public sealed partial class GlobalFieldTiltCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum;

    public ConcurrentBag<KeyValuePair<OpticsIlluminationModeEnum, GlobalFieldTiltCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public GlobalFieldTiltCacheItem Item => Items.GetOrAdd(OpticsIlluminationModeEnum, new GlobalFieldTiltCacheItem());

    [ObservableProperty]
    private double _pmtInterval = 320; // Pmt相机采集间隔320um

    [ObservableProperty]
    private bool _isDarkFieldAlignment;

    [ObservableProperty]
    private ObservableCollection<PmtConfigParam> _pmtConfigList = [];

    /// <summary>
    /// um/ecs
    /// </summary>
    [ObservableProperty]
    private double _umPerEcs = 0.2;

    [ObservableProperty]
    private double _originDOEPos;

    [ObservableProperty]
    private double _threshold;

    [ObservableProperty]
    private double _verifyQualityThreshold;

    [ObservableProperty]
    private double _p5Angle;
}

public partial class GlobalFieldTiltCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private bool _isMultiPMTOnceCollection = true;

    [ObservableProperty]
    private AlgorithmImageQualityTypeEnum _algorithmImageQualityTypeEnum = AlgorithmImageQualityTypeEnum.Laplace;

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private ImageCollectionConfiguration _imageCollectionConfiguration = new() { IsAutoFocus = false, IsForward = true, IsCustomEcs = false };

    [ObservableProperty]
    private Point _findPosition = Point.Origin;

    /// <summary>
    /// 入射角（°）
    /// </summary>
    [ObservableProperty]
    private double _obliqueAngle = 53;

    [ObservableProperty]
    private double _threshold;

    [ObservableProperty]
    private int _retryCount = 5;

    [ObservableProperty]
    private int _imageWidth = 1000;

    [ObservableProperty]
    private double _centerECS;

    [ObservableProperty]
    private double _rangeECS = 120;

    [ObservableProperty]
    private double _stepECS = 20;

    [ObservableProperty]
    private double _rangeRefinedECS = 50;

    [ObservableProperty]
    private double _stepRefinedECS = 10;

    [ObservableProperty]
    private AlignmentResultDto _alignmentResult = new();
}