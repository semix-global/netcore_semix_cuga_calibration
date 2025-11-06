using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Pattern;
using LiteDB;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using Newtonsoft.Json;
using System.Collections.Concurrent;


namespace Core.Models.Models.Laser.XPixelSize;

public sealed partial class LaserXPixelSizeCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.Caliper;

    [ObservableProperty]
    private double _chuckRadius = 150000;

    [ObservableProperty]
    private Point _findStartPosition;

    [ObservableProperty]
    private Point _findEndPosition;

    [ObservableProperty]
    private double _dieWidthUm = 15300;

    [ObservableProperty]
    private int _slideWindowValue = 1000;

    [ObservableProperty]
    private int _slideStepValue = 100;

    [ObservableProperty]
    private int _splitWidthPixel = 1000;

    [ObservableProperty]
    private int _splitImageCount = 18;

    [ObservableProperty]
    private double _nccScoreThreshold = 0.9;

    [ObservableProperty]
    private int _columnNumber = 9;

    [ObservableProperty]
    private double _threshold = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, LaserXPixelSizeCacheItem>> Items { get; init; } = [];

    [JsonIgnore]
    [BsonIgnore]
    public LaserXPixelSizeCacheItem Item => Items.GetOrAdd(ProductivityInformation, new LaserXPixelSizeCacheItem());
}

public sealed partial class LaserXPixelSizeCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private Point _findTemplatePosition;

    [ObservableProperty]
    private string _templateFilePath = String.Empty;

    [ObservableProperty]
    private string _templateImageFilePath = String.Empty;

}