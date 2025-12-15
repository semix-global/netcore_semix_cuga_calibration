using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Setting;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Laser.DOEAngle;

public sealed partial class LaserDOEAngleCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    private ObservableCollection<PmtConfigParam> _pmtConfigList = [];

    [ObservableProperty]
    private double _originDOEAngle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, LaserDOEAngleCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public LaserDOEAngleCacheItem Item => Items.GetOrAdd(ProductivityInformation, new LaserDOEAngleCacheItem());

    /// <summary>
    ///  um
    /// </summary>
    [ObservableProperty]
    private double _pmtInterval = 320;

    /// <summary>
    /// um/ecs
    /// </summary>
    [ObservableProperty]
    private double _umPerEcs = 0.2;
}

public sealed partial class LaserDOEAngleCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private Point _findPosition = Point.Origin;

    /// <summary>
    /// 入射角（°）
    /// </summary>
    [ObservableProperty]
    private double _obliqueAngle = 53;

    /// <summary>
    /// Af Offset(mm)/ecs
    /// </summary>
    [ObservableProperty]
    private double _ecsPerAfOffset = 23;

    [ObservableProperty]
    private double _threshold;

    [ObservableProperty]
    private int _retryCount = 5;

    [ObservableProperty]
    private double _p5Angle;

    [ObservableProperty]
    private bool _isDarkFieldAlignment;
}