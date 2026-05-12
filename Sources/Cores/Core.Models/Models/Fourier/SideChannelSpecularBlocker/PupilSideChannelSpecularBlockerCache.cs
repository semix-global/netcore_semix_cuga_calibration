using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Fourier.SideChannelSpecularBlocker;

public sealed partial class PupilSideChannelSpecularBlockerCache : CalibrationCacheBase<PupilSideChannelSpecularBlockerCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum = CalibrationConstantsHelper.MainOpticsIlluminationModeEnum;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImage? _ch1Image;

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImage? _ch2Image;

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImageDrawable _bitmapImageDrawableCh10 = new();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImageDrawable _bitmapImageDrawableCh11 = new();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImageDrawable _bitmapImageDrawableCh20 = new();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImageDrawable _bitmapImageDrawableCh21 = new();

    [ObservableProperty]
    private RectROIDrawable? _rectROIDrawableCh11;

    [ObservableProperty]
    private CircleROIDrawable? _circleROIDrawableCh11;

    [ObservableProperty]
    private ObservableCollection<RectROIDrawable> _rectROIDrawableListCh11 = [];

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<(OpticsIlluminationModeEnum, ProductivityInformation), PupilSideChannelSpecularBlockerCacheItem>))]
    public ConcurrentDictionary<(OpticsIlluminationModeEnum, ProductivityInformation), PupilSideChannelSpecularBlockerCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public PupilSideChannelSpecularBlockerCacheItem Item => Items.GetOrAdd((OpticsIlluminationModeEnum, ProductivityInformation), _ => new PupilSideChannelSpecularBlockerCacheItem());

    public override PupilSideChannelSpecularBlockerCache Clone() => new()
    {
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        ProductivityInformation = ProductivityInformation.Clone(),
        Ch1Image = Ch1Image,
        Ch2Image = Ch2Image,
        BitmapImageDrawableCh10 = BitmapImageDrawableCh10,
        BitmapImageDrawableCh11 = BitmapImageDrawableCh11,
        BitmapImageDrawableCh20 = BitmapImageDrawableCh20,
        BitmapImageDrawableCh21 = BitmapImageDrawableCh21,
        RectROIDrawableCh11 = RectROIDrawableCh11,
        CircleROIDrawableCh11 = CircleROIDrawableCh11,
        RectROIDrawableListCh11 = new([.. RectROIDrawableListCh11]),
        Items = new ConcurrentDictionary<(OpticsIlluminationModeEnum, ProductivityInformation), PupilSideChannelSpecularBlockerCacheItem>(Items.Select(t => new KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), PupilSideChannelSpecularBlockerCacheItem>((t.Key.Item1, t.Key.Item2.Clone()), t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class PupilSideChannelSpecularBlockerCacheItem : CalibrationCacheBase<PupilSideChannelSpecularBlockerCacheItem>
{
    [ObservableProperty]
    private Point _shinyWaferPosition;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private OpticsConfiguration _opticsConfiguration = new();

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private CIBInformation _cIBInformation1 = CIBInformation.Default;

    [ObservableProperty]
    private CIBInformation _cIBInformation2 = CIBInformation.Default;

    [ObservableProperty]
    private string _originImageFilePathOld1 = string.Empty;

    [ObservableProperty]
    private string _originImageFilePathOld2 = string.Empty;

    [ObservableProperty]
    private string _originImageFilePathNew1 = string.Empty;

    [ObservableProperty]
    private string _originImageFilePathNew2 = string.Empty;

    [ObservableProperty]
    private string _imageGrayCompareCh1 = string.Empty;

    [ObservableProperty]
    private string _imageGrayCompareCh2 = string.Empty;

    [ObservableProperty]
    private float _imageGrayOldCh1;

    [ObservableProperty]
    private float _imageGrayOldCh2;

    [ObservableProperty]
    private float _imageGrayNewCh1;

    [ObservableProperty]
    private float _imageGrayNewCh2;

    [ObservableProperty]
    public Point _cgFFBoxBeginPositionCh1 = Point.Origin;

    [ObservableProperty]
    public Point _cgFFBoxBeginPositionCh2 = Point.Origin;

    [ObservableProperty]
    public List<int> _cgFFBoxBeginAndEndNumberCh1 = [];

    [ObservableProperty]
    public List<int> _cgFFBoxBeginAndEndNumberCh2 = [];

    [ObservableProperty]
    public List<double> _cgFFBoxMoveDownPercentListCh1 = [0.2, 0.3, 0.8, 0.9, 0.6, 0.8];

    [ObservableProperty]
    public List<double> _cgFFBoxMoveDownPercentListCh2 = [0.2, 0.3, 0.8, 0.9, 0.6, 0.8];

    public override PupilSideChannelSpecularBlockerCacheItem Clone() => new()
    {
        ShinyWaferPosition = ShinyWaferPosition,
        CIBConfiguration = CIBConfiguration.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        CIBInformation1 = CIBInformation1.Clone(),
        CIBInformation2 = CIBInformation2.Clone(),
        OriginImageFilePathOld1 = OriginImageFilePathOld1,
        OriginImageFilePathOld2 = OriginImageFilePathOld2,
        OriginImageFilePathNew1 = OriginImageFilePathNew1,
        OriginImageFilePathNew2 = OriginImageFilePathNew2,
        ImageGrayCompareCh1 = ImageGrayCompareCh1,
        ImageGrayCompareCh2 = ImageGrayCompareCh2,
        ImageGrayOldCh1 = ImageGrayOldCh1,
        ImageGrayOldCh2 = ImageGrayOldCh2,
        ImageGrayNewCh1 = ImageGrayNewCh1,
        ImageGrayNewCh2 = ImageGrayNewCh2,
        CgFFBoxBeginPositionCh1 = CgFFBoxBeginPositionCh1,
        CgFFBoxBeginPositionCh2 = CgFFBoxBeginPositionCh2,
        CgFFBoxBeginAndEndNumberCh1 = [.. CgFFBoxBeginAndEndNumberCh1],
        CgFFBoxBeginAndEndNumberCh2 = [.. CgFFBoxBeginAndEndNumberCh2],
        CgFFBoxMoveDownPercentListCh1 = [.. CgFFBoxMoveDownPercentListCh1],
        CgFFBoxMoveDownPercentListCh2 = [.. CgFFBoxMoveDownPercentListCh2],
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}