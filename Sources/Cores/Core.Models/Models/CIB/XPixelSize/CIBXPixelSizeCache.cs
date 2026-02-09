using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.CIB.XPixelSize;

public sealed partial class CIBXPixelSizeCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;

    [ObservableProperty]
    private double _threshold = 15;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, CIBXPixelSizeCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]

    public CIBXPixelSizeCacheItem Item => Items.GetOrAdd(ProductivityInformation, new CIBXPixelSizeCacheItem());
}

public sealed partial class CIBXPixelSizeCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private bool _isDarkFieldAlignment;

    [ObservableProperty]
    private AlignmentResultDto _alignmentResult = new();

    [ObservableProperty]
    private int _imageWidth = 1000;

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.Caliper;

    [ObservableProperty]
    private Point _findBFMachinePosition;

    [ObservableProperty]
    private string _templateFilePath = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath = string.Empty;

    [ObservableProperty]
    private double _waferRadius = 140_000;

    [ObservableProperty]
    private double _diePitchWith = 5100;

    [ObservableProperty]
    private int _reticleDieCountX = 3;
}