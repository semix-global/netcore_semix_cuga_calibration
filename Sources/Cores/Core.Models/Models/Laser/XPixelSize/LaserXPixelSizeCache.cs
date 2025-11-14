using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Pattern;
using LiteDB;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using Core.Models.Helper;
using Core.Models.Models.Common.Alignment;

namespace Core.Models.Models.Laser.XPixelSize;

public sealed partial class LaserXPixelSizeCache : CalibrationCacheBase
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _threshold = 15;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, LaserXPixelSizeCacheItem>> Items { get; init; } = [];

    [JsonIgnore]
    [BsonIgnore]
    public LaserXPixelSizeCacheItem Item => Items.GetOrAdd(ProductivityInformation, new LaserXPixelSizeCacheItem());
}

public sealed partial class LaserXPixelSizeCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private int _pMTId = CalibrationConstantsHelper.MainPmtId;

    [ObservableProperty]
    private int _channelId = CalibrationConstantsHelper.MainChannelId;

    [ObservableProperty]
    private bool _isDarkFieldAlignment;

    [ObservableProperty]
    private AlignmentResultDto _alignmentResult = new();

    [ObservableProperty]
    private int _widthPixel = 1000;

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.Caliper;

    [ObservableProperty]
    private Point _findBFMachinePosition;

    [ObservableProperty]
    private string _templateFilePath = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath = string.Empty;

    [ObservableProperty]
    private double _waferDiameter = 300_000;

    [ObservableProperty]
    private double _columnCellWidth = 15300;
}