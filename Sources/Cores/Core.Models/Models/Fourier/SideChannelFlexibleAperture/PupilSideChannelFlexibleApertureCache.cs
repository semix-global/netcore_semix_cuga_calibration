using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

public sealed partial class PupilSideChannelFlexibleApertureCache : CalibrationCacheBase
{
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
    private Point _hazeWaferPosition;

    [ObservableProperty]
    private string _originImageFilePath1 = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _originImageFilePathList1 = [];

    [ObservableProperty]
    private string _originImageFilePath2 = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _originImageFilePathList2 = [];

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
    private BitmapImageDrawable _bitmapImageDrawableCh1 = new();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImageDrawable _bitmapImageDrawableCh2 = new();

    [ObservableProperty]
    private bool _isToggleSelectRectROIDrawableCh1;

    [ObservableProperty]
    private bool _isToggleSelectRectROIDrawableCh2;

    [ObservableProperty]
    private Point _cgFFBoxBeginPositionCh1 = Point.Origin;

    [ObservableProperty]
    private Point _cgFFBoxEndPositionCh1 = Point.Origin;

    [ObservableProperty]
    private Point _cgFFBoxBeginPositionCh2 = Point.Origin;

    [ObservableProperty]
    private Point _cgFFBoxEndPositionCh2 = Point.Origin;

    [ObservableProperty]
    private int _cgFFBoxBeginNumber1Ch1;

    [ObservableProperty]
    private int _cgFFBoxBeginNumber2Ch1;

    [ObservableProperty]
    private int _cgFFBoxBeginNumber1Ch2;

    [ObservableProperty]
    private int _cgFFBoxBeginNumber2Ch2;

    [ObservableProperty]
    private int _cgFFBoxEndNumber1Ch1;

    [ObservableProperty]
    private int _cgFFBoxEndNumber2Ch1;

    [ObservableProperty]
    private int _cgFFBoxEndNumber1Ch2;

    [ObservableProperty]
    private int _cgFFBoxEndNumber2Ch2;

    [ObservableProperty]
    private ObservableCollection<int> _cgFFBoxRodWidthListCh1 = [];

    [ObservableProperty]
    private ObservableCollection<int> _cgFFBoxRodWidthListCh2 = [];

    [ObservableProperty]
    private ObservableCollection<double> _cgFFBoxHeightRelationPercentListCh1 = [];

    [ObservableProperty]
    private ObservableCollection<double> _cgFFBoxHeightRelationPercentListCh2 = [];

    [ObservableProperty]
    private ObservableCollection<Rect> _currentImageRectListFirstCh1 = [];

    [ObservableProperty]
    private ObservableCollection<Rect> _currentImageRectListFirstCh2 = [];

    [ObservableProperty]
    private double _cgFFBoxAllRodsBeginPercentCh1;

    [ObservableProperty]
    private double _cgFFBoxAllRodsBeginPercentCh2;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;
}