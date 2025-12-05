using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Laser.LineCentricity;

public sealed partial class LaserLineCentricityCache : CalibrationCacheBase
{
    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.GridConrner_100um;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, LaserLineCentricityCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public LaserLineCentricityCacheItem Item => Items.GetOrAdd(ProductivityInformation, new LaserLineCentricityCacheItem());

    [ObservableProperty]
    private double _chuckRadius = 150000;

    [ObservableProperty]
    private double _pmtInterval = 320; // Pmt相机采集间隔320um

}

public sealed partial class LaserLineCentricityCacheItem : CalibrationCacheBase
{
    /// <summary>
    /// 选定特征的明场坐标
    /// </summary>
    [ObservableProperty]
    private Point _findPosition;

    /// <summary>
    /// 明场选定特征对应的stage机械坐标
    /// </summary>
    [ObservableProperty]
    private Point _findBrightMachinePosition;

    [ObservableProperty]
    private int _xWidthPixel = 800;

    [ObservableProperty]
    private string _brightTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _brightTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _templateFilePath = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath = string.Empty;

    [ObservableProperty]
    private Point _threshold;
}