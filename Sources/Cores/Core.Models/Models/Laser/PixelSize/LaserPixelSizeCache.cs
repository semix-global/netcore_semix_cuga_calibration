using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using LiteDB;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Core.Models.Models.Laser.PixelSize;

public sealed partial class LaserPixelSizeCache : CalibrationCacheBase
{
    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private OpticsIncidentModeEnum _opticsIncidentMode = OpticsIncidentModeEnum.OI;

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, LaserPixelSizeCacheItem>> Items { get; init; } = [];

    [JsonIgnore]
    [BsonIgnore]
    public LaserPixelSizeCacheItem Item => Items.GetOrAdd(ProductivityInformation, new LaserPixelSizeCacheItem());

    [ObservableProperty]
    private double _chuckRadius = 150000;

    [ObservableProperty]
    private double _pmtInterval = 320; // Pmt相机采集间隔320um
}

public sealed partial class LaserPixelSizeCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.Grid_10um;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private int _xWidthPixel = 800;

    [ObservableProperty]
    private double _verifyResultYPixelSize;

    [ObservableProperty]
    private double _threshold;
}