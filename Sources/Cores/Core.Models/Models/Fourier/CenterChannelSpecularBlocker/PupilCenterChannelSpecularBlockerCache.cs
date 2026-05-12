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

namespace Core.Models.Models.Fourier.CenterChannelSpecularBlocker;

public sealed partial class PupilCenterChannelSpecularBlockerCache : CalibrationCacheBase<PupilCenterChannelSpecularBlockerCache>
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
    private BitmapImage? _ch3Image;

    [ObservableProperty]
    private RectROIDrawable? _rectROIDrawable;

    [ObservableProperty]
    private CircleROIDrawable? _circleROIDrawable;

    [ObservableProperty]
    private ObservableCollection<RectROIDrawable> _rectROIDrawableList = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImageDrawable _bitmapImageDrawableCh30 = new();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImageDrawable _bitmapImageDrawableCh31 = new();

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<(OpticsIlluminationModeEnum, ProductivityInformation), PupilCenterChannelSpecularBlockerCacheItem>))]
    public ConcurrentDictionary<(OpticsIlluminationModeEnum, ProductivityInformation), PupilCenterChannelSpecularBlockerCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public PupilCenterChannelSpecularBlockerCacheItem Item => Items.GetOrAdd((OpticsIlluminationModeEnum, ProductivityInformation), _ => new PupilCenterChannelSpecularBlockerCacheItem());

    public override PupilCenterChannelSpecularBlockerCache Clone() => new()
    {
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        ProductivityInformation = ProductivityInformation.Clone(),
        Ch3Image = Ch3Image,
        RectROIDrawable = RectROIDrawable,
        CircleROIDrawable = CircleROIDrawable,
        RectROIDrawableList = new([.. RectROIDrawableList]),
        BitmapImageDrawableCh30 = BitmapImageDrawableCh30,
        BitmapImageDrawableCh31 = BitmapImageDrawableCh31,
        Items = new ConcurrentDictionary<(OpticsIlluminationModeEnum, ProductivityInformation), PupilCenterChannelSpecularBlockerCacheItem>(Items.Select(t => new KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), PupilCenterChannelSpecularBlockerCacheItem>((t.Key.Item1, t.Key.Item2.Clone()), t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class PupilCenterChannelSpecularBlockerCacheItem : CalibrationCacheBase<PupilCenterChannelSpecularBlockerCacheItem>
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
    private CIBInformation _cIBInformation3 = CIBInformation.Default;

    [ObservableProperty]
    private string _originImageFilePathOld = string.Empty;

    [ObservableProperty]
    private string _originImageFilePathNew = string.Empty;

    [ObservableProperty]
    private string _imageGrayCompareCh3 = string.Empty;

    [ObservableProperty]
    private float _imageGrayOldCh3;

    [ObservableProperty]
    private float _imageGrayNewCh3;

    [ObservableProperty]
    public float _ch3Angle = 1;

    [ObservableProperty]
    public float _ch3TurnX = 0.2f;

    [ObservableProperty]
    public float _ch3TurnY = 0.3f;

    [ObservableProperty]
    public float _ch3Push = 0.3f;

    public override PupilCenterChannelSpecularBlockerCacheItem Clone() => new()
    {
        ShinyWaferPosition = ShinyWaferPosition,
        CIBConfiguration = CIBConfiguration.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        CIBInformation3 = CIBInformation3.Clone(),
        OriginImageFilePathOld = OriginImageFilePathOld,
        OriginImageFilePathNew = OriginImageFilePathNew,
        ImageGrayCompareCh3 = ImageGrayCompareCh3,
        ImageGrayOldCh3 = ImageGrayOldCh3,
        ImageGrayNewCh3 = ImageGrayNewCh3,
        Ch3Angle = Ch3Angle,
        Ch3TurnX = Ch3TurnX,
        Ch3TurnY = Ch3TurnY,
        Ch3Push = Ch3Push,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}