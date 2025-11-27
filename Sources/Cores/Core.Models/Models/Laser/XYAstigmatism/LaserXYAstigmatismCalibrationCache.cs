using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Laser.XYAstigmatism;

// ReSharper disable once InconsistentNaming
public sealed partial class LaserXYAstigmatismCalibrationCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private OpticsIncidentModeEnum _opticsIncidentModeEnum = CalibrationConstantsHelper.MainOpticsIncidentModeEnum;

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, LaserXYAstigmatismCalibrationCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public LaserXYAstigmatismCalibrationCacheItem Item => Items.GetOrAdd(ProductivityInformation, new LaserXYAstigmatismCalibrationCacheItem());

    public void SetEcsYParams()
    {
        Item.EcsYPositionUpper = Item.EcsPositionUpper;
        Item.EcsYPositionLower = Item.EcsPositionLower;
        Item.EcsInterval = Item.EcsYInterval;
        Item.EcsYInitial = Item.EcsXInitial;
    }
}

public sealed partial class LaserXYAstigmatismCalibrationCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private Point _findPosition;

    /// <summary>
    /// XY焦距差值允许范围 
    /// </summary>
    [ObservableProperty]
    private double _threshold;

    #region find EcsX Z轴参数

    /// <summary>
    /// 暗场ECS X 默认波形下最佳ECS
    /// </summary>
    [ObservableProperty]
    private double _ecsXInitial;

    /// <summary>
    /// 暗场ECS upper(中心往上递增次数) 
    /// </summary>
    [ObservableProperty]
    private double _ecsPositionUpper;

    /// <summary>
    /// 暗场ECS lower(中心往下递增次数) 
    /// </summary>
    [ObservableProperty]
    private double _ecsPositionLower;

    /// <summary>
    /// 暗场ECS Z轴步距
    /// </summary>
    [ObservableProperty]
    private double _ecsInterval;

    #endregion find EcsX Z轴参数

    #region find EcsY Z轴参数

    /// <summary>
    /// 暗场ECS Y 默认波形下最佳ECS
    /// </summary>
    [ObservableProperty]
    private double _ecsYInitial;

    /// <summary>
    /// 暗场ECS Y upper(中心往上递增次数) 
    /// </summary>
    [ObservableProperty]
    private double _ecsYPositionUpper;

    /// <summary>
    /// 暗场ECS Y lower(中心往下递增次数) 
    /// </summary>
    [ObservableProperty]
    private double _ecsYPositionLower;

    /// <summary>
    /// 暗场ECS Y  Z轴步距
    /// </summary>
    [ObservableProperty]
    private double _ecsYInterval;

    #endregion find EcsY Z轴参数

    #region 频率参数

    /// <summary>
    /// Chirp AOD波形频率增长次数 
    /// </summary>
    [ObservableProperty]
    private int _increaseCount;

    /// <summary>
    /// Chirp AOD波形频率增长步距 
    /// </summary>
    [ObservableProperty]
    private double _increaseInterval;

    #endregion 频率参数

    #region 波形参数

    /// <summary>
    /// ChirpAod默认波形生成参数
    /// </summary>
    [ObservableProperty]
    private GenerateChirpAODWaveformParam _chirpAodDefaultDto = new();

    /// <summary>
    /// 起始频率变化率 
    /// </summary>
    [ObservableProperty]
    private double _startSpectralDensity;

    #endregion 波形参数
}