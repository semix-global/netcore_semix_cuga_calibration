using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.CIB.LineOrientationOffset;

public sealed partial class CIBLineOrientationOffsetCache : CalibrationCacheBase
{
    [ObservableProperty]
    private double _pmtInterval = 320; // Pmt相机采集间隔320um

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, CIBLineOrientationOffsetCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public CIBLineOrientationOffsetCacheItem Item => Items.GetOrAdd(ProductivityInformation, new CIBLineOrientationOffsetCacheItem());
}

public sealed partial class CIBLineOrientationOffsetCacheItem : CalibrationCacheBase
{
    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Die Pitch Width must be greater than 0.1.")]
    public double DiePitchWidth
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 5100;

    [Comparison(1000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Wafer Radius: ")]
    public double WaferRadius
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 150_000;

    [Comparison(1, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Reticle Reference Die Col Count must be greater than 1.")]
    public int ReticleDieCountX
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 1;

    [ObservableProperty]
    private int _imageCount;

    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private OpticsConfiguration _opticsConfiguration = new();

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.GridConrner_100um;

    [ObservableProperty]
    private int _xWidthPixel = 800;

    [ObservableProperty]
    private double _threshold;

    [ObservableProperty]
    private bool _isDarkFieldAlignment;

    /// <summary>
    /// 选定特征的明场坐标
    /// </summary>
    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private Point _startPosition;

    [ObservableProperty]
    private Point _endPosition;

    [ObservableProperty]
    private AlignmentResultDto _alignmentResult = new();


    [ObservableProperty]
    private string _brightTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _brightTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _templateFilePath = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath = string.Empty;
}