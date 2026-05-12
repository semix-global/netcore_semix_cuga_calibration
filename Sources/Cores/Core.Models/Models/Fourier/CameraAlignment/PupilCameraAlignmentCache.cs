using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Fourier.CameraAlignment;

public sealed partial class PupilCameraAlignmentCache : CalibrationCacheBase<PupilCameraAlignmentCache>
{
    [ObservableProperty]
    private Point _hazeWaferPosition;

    [ObservableProperty]
    private string _primaryImageFilePath1 = string.Empty;

    [ObservableProperty]
    private string _primaryImageFilePath2 = string.Empty;

    [ObservableProperty]
    private string _primaryImageFilePath3 = string.Empty;

    [ObservableProperty]
    private string _originImageFilePath1 = string.Empty;

    [ObservableProperty]
    private string _originImageFilePath2 = string.Empty;

    [ObservableProperty]
    private string _originImageFilePath3 = string.Empty;

    [ObservableProperty]
    private RectROIDrawable? _rectROIDrawableCh1;

    [ObservableProperty]
    private RectROIDrawable? _rectROIDrawableCh2;

    [ObservableProperty]
    private RectROIDrawable? _rectROIDrawableCh3;

    [ObservableProperty]
    private CircleROIDrawable? _circleROIDrawableCh1;

    [ObservableProperty]
    private CircleROIDrawable? _circleROIDrawableCh2;

    [ObservableProperty]
    private CircleROIDrawable? _circleROIDrawableCh3;

    [ObservableProperty]
    private ObservableCollection<RectROIDrawable> _rectROIDrawableListCh1 = [];

    [ObservableProperty]
    private ObservableCollection<RectROIDrawable> _rectROIDrawableListCh2 = [];

    [ObservableProperty]
    private ObservableCollection<RectROIDrawable> _rectROIDrawableListCh3 = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImageDrawable? _bitmapImageDrawableCh1;

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImageDrawable? _bitmapImageDrawableCh2;

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImageDrawable? _bitmapImageDrawableCh3;

    [ObservableProperty]
    private bool _isToggleSelectRectROIDrawableCh1;

    [ObservableProperty]
    private bool _isToggleSelectRectROIDrawableCh2;

    [ObservableProperty]
    private bool _isToggleSelectRectROIDrawableCh3;

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
    private BitmapImage? _ch3Image;

    [ObservableProperty]
    private Point _rectCh1Position = Point.Origin;

    [ObservableProperty]
    private int _ch1ImageWidth;

    [ObservableProperty]
    private int _ch1ImageHeight;

    [ObservableProperty]
    private Point _rectCh2Position = Point.Origin;

    [ObservableProperty]
    private int _ch2ImageWidth;

    [ObservableProperty]
    private int _ch2ImageHeight;

    [ObservableProperty]
    private Point _rectCh3Position = Point.Origin;

    [ObservableProperty]
    private int _ch3ImageWidth;

    [ObservableProperty]
    private int _ch3ImageHeight;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    public override PupilCameraAlignmentCache Clone() => new()
    {
        HazeWaferPosition = HazeWaferPosition,
        PrimaryImageFilePath1 = PrimaryImageFilePath1,
        PrimaryImageFilePath2 = PrimaryImageFilePath2,
        PrimaryImageFilePath3 = PrimaryImageFilePath3,
        OriginImageFilePath1 = OriginImageFilePath1,
        OriginImageFilePath2 = OriginImageFilePath2,
        OriginImageFilePath3 = OriginImageFilePath3,
        RectROIDrawableCh1 = RectROIDrawableCh1,
        RectROIDrawableCh2 = RectROIDrawableCh2,
        RectROIDrawableCh3 = RectROIDrawableCh3,
        CircleROIDrawableCh1 = CircleROIDrawableCh1,
        CircleROIDrawableCh2 = CircleROIDrawableCh2,
        CircleROIDrawableCh3 = CircleROIDrawableCh3,
        RectROIDrawableListCh1 = new([.. RectROIDrawableListCh1]),
        RectROIDrawableListCh2 = new([.. RectROIDrawableListCh2]),
        RectROIDrawableListCh3 = new([.. RectROIDrawableListCh3]),
        BitmapImageDrawableCh1 = BitmapImageDrawableCh1,
        BitmapImageDrawableCh2 = BitmapImageDrawableCh2,
        BitmapImageDrawableCh3 = BitmapImageDrawableCh3,
        IsToggleSelectRectROIDrawableCh1 = IsToggleSelectRectROIDrawableCh1,
        IsToggleSelectRectROIDrawableCh2 = IsToggleSelectRectROIDrawableCh2,
        IsToggleSelectRectROIDrawableCh3 = IsToggleSelectRectROIDrawableCh3,
        Ch1Image = Ch1Image,
        Ch2Image = Ch2Image,
        Ch3Image = Ch3Image,
        RectCh1Position = RectCh1Position,
        Ch1ImageWidth = Ch1ImageWidth,
        Ch1ImageHeight = Ch1ImageHeight,
        RectCh2Position = RectCh2Position,
        Ch2ImageWidth = Ch2ImageWidth,
        Ch2ImageHeight = Ch2ImageHeight,
        RectCh3Position = RectCh3Position,
        Ch3ImageWidth = Ch3ImageWidth,
        Ch3ImageHeight = Ch3ImageHeight,
        ProductivityInformation = ProductivityInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}