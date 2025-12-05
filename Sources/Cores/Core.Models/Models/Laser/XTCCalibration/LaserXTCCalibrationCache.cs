using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Laser.IlluminationProfile;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace Core.Models.Models.Laser.XTCCalibration;

public sealed partial class LaserXTCCalibrationCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum = CalibrationConstantsHelper.MainOpticsIlluminationModeEnum;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, LaserXTCCalibrationCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public LaserXTCCalibrationCacheItem Item => Items.GetOrAdd(ProductivityInformation, new LaserXTCCalibrationCacheItem());

    [ObservableProperty]
    private double _chuckRadius = 150000;

    [ObservableProperty]
    private int _channelId = 3;

    [ObservableProperty]
    private double _pmtInterval = 320; // Pmt相机采集间隔100um

    [ObservableProperty]
    private ConcurrentDictionary<string, LaserIlluminationProfileDarkFieldImageListToPrescanListCacheItem> _darkFieldImageListToPrescanListCacheItemDic = [];

    [JsonIgnore]
    public LaserIlluminationProfileDarkFieldImageListToPrescanListCacheItem CurrentDarkFieldImageListToPrescanListCacheItem =>
        DarkFieldImageListToPrescanListCacheItemDic.GetOrAdd($"{ProductivityInformation}", new LaserIlluminationProfileDarkFieldImageListToPrescanListCacheItem());
}

public sealed partial class LaserXTCCalibrationCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private int _widthPixel = 800;

    [ObservableProperty]
    private int _prescanStartIndex = 2000;

    [ObservableProperty]
    private int _prescanInterval = 200;

    [ObservableProperty]
    private int _prescanSkipCount = 50;

    [ObservableProperty]
    private double _xShifting = 0.5; //X偏移系数0.5um

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private double _threshold = 1.0d;
}