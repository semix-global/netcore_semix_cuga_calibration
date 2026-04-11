using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Fourier;

public sealed partial class PupilSideChannelSpecularBlockerCache : CalibrationCacheBase
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
    private BitmapImage? _ch1Image = null;

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImage? _ch2Image = null;

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
    private ObservableCollection<RectROIDrawable> _rectROIDrawableListCh11 = new();

    public ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), PupilSideChannelSpecularBlockerCacheItem>> Items { get; init; } = [];

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public PupilSideChannelSpecularBlockerCacheItem Item => Items.GetOrAdd((OpticsIlluminationModeEnum, ProductivityInformation), new Lazy<PupilSideChannelSpecularBlockerCacheItem>(() => new PupilSideChannelSpecularBlockerCacheItem()));
}

public sealed partial class PupilSideChannelSpecularBlockerCacheItem : CalibrationCacheBase
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
    private float _imageGrayOldCh1 = 0;

    [ObservableProperty]
    private float _imageGrayOldCh2 = 0;

    [ObservableProperty]
    private float _imageGrayNewCh1 = 0;

    [ObservableProperty]
    private float _imageGrayNewCh2 = 0;

    [ObservableProperty]
    public Point _cgFFBoxBeginPositionCh1 = Point.Origin;

    [ObservableProperty]
    public Point _cgFFBoxBeginPositionCh2 = Point.Origin;

    [ObservableProperty]
    public int _cgFFBoxBeginNumberCh1 = 1;

    [ObservableProperty]
    public int _cgFFBoxBeginNumberCh2 = 1;

    [ObservableProperty]
    public int _cgFFBoxEndNumberCh1 = 3;

    [ObservableProperty]
    public int _cgFFBoxEndNumberCh2 = 3;

    [ObservableProperty]
    public List<double> _cgFFBoxMoveDownPercentListCh1 = [0.2, 0.3, 0.8, 0.9, 0.6, 0.8];

    [ObservableProperty]
    public List<double> _cgFFBoxMoveDownPercentListCh2 = [0.2, 0.3, 0.8, 0.9, 0.6, 0.8];
}