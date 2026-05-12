using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

public sealed partial class PupilSideChannelFlexibleApertureCache : CalibrationCacheBase<PupilSideChannelFlexibleApertureCache>
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

    public override PupilSideChannelFlexibleApertureCache Clone() => new()
    {
        Ch1Image = Ch1Image,
        Ch2Image = Ch2Image,
        HazeWaferPosition = HazeWaferPosition,
        OriginImageFilePath1 = OriginImageFilePath1,
        OriginImageFilePathList1 = new([.. OriginImageFilePathList1]),
        OriginImageFilePath2 = OriginImageFilePath2,
        OriginImageFilePathList2 = new([.. OriginImageFilePathList2]),
        RectROIDrawable = RectROIDrawable,
        CircleROIDrawable = CircleROIDrawable,
        RectROIDrawableList = new([.. RectROIDrawableList]),
        BitmapImageDrawableCh1 = BitmapImageDrawableCh1,
        BitmapImageDrawableCh2 = BitmapImageDrawableCh2,
        IsToggleSelectRectROIDrawableCh1 = IsToggleSelectRectROIDrawableCh1,
        IsToggleSelectRectROIDrawableCh2 = IsToggleSelectRectROIDrawableCh2,
        CgFFBoxBeginPositionCh1 = CgFFBoxBeginPositionCh1,
        CgFFBoxEndPositionCh1 = CgFFBoxEndPositionCh1,
        CgFFBoxBeginPositionCh2 = CgFFBoxBeginPositionCh2,
        CgFFBoxEndPositionCh2 = CgFFBoxEndPositionCh2,
        CgFFBoxBeginNumber1Ch1 = CgFFBoxBeginNumber1Ch1,
        CgFFBoxBeginNumber2Ch1 = CgFFBoxBeginNumber2Ch1,
        CgFFBoxBeginNumber1Ch2 = CgFFBoxBeginNumber1Ch2,
        CgFFBoxBeginNumber2Ch2 = CgFFBoxBeginNumber2Ch2,
        CgFFBoxEndNumber1Ch1 = CgFFBoxEndNumber1Ch1,
        CgFFBoxEndNumber2Ch1 = CgFFBoxEndNumber2Ch1,
        CgFFBoxEndNumber1Ch2 = CgFFBoxEndNumber1Ch2,
        CgFFBoxEndNumber2Ch2 = CgFFBoxEndNumber2Ch2,
        CgFFBoxRodWidthListCh1 = new([.. CgFFBoxRodWidthListCh1]),
        CgFFBoxRodWidthListCh2 = new([.. CgFFBoxRodWidthListCh2]),
        CgFFBoxHeightRelationPercentListCh1 = new([.. CgFFBoxHeightRelationPercentListCh1]),
        CgFFBoxHeightRelationPercentListCh2 = new([.. CgFFBoxHeightRelationPercentListCh2]),
        CurrentImageRectListFirstCh1 = new([.. CurrentImageRectListFirstCh1]),
        CurrentImageRectListFirstCh2 = new([.. CurrentImageRectListFirstCh2]),
        CgFFBoxAllRodsBeginPercentCh1 = CgFFBoxAllRodsBeginPercentCh1,
        CgFFBoxAllRodsBeginPercentCh2 = CgFFBoxAllRodsBeginPercentCh2,
        LaserLightInformation = LaserLightInformation.Clone(),
        ProductivityInformation = ProductivityInformation.Clone(),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}