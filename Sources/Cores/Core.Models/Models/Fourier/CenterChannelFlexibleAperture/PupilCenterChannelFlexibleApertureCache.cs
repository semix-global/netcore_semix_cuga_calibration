using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Fourier.CenterChannelFlexibleAperture;

public sealed partial class PupilCenterChannelFlexibleApertureCache : CalibrationCacheBase
{
    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImage? _ch3Image1;

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImage? _ch3Image2;

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImage? _ch3Image3;

    [ObservableProperty]
    private Point _hazeWaferPosition;

    [ObservableProperty]
    private ObservableCollection<string> _originImageFilePathList11 = new(Enumerable.Repeat("", 4));

    [ObservableProperty]
    private ObservableCollection<double> _originImageAngleList11 = new(Enumerable.Repeat(0.0, 4));

    [ObservableProperty]
    private ObservableCollection<string> _originImageFilePathList12 = new(Enumerable.Repeat("", 8));

    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxWidthList1 = new(Enumerable.Repeat(0.0, 8));

    [ObservableProperty]
    private ObservableCollection<string> _originImageFilePathList13 = [];

    [ObservableProperty]
    private ObservableCollection<double> _originImageAngleList13 = [];

    [ObservableProperty]
    private ObservableCollection<string> _originImageFilePathList21 = new(Enumerable.Repeat("", 4));

    [ObservableProperty]
    private ObservableCollection<double> _originImageAngleList21 = new(Enumerable.Repeat(0.0, 4));

    [ObservableProperty]
    private ObservableCollection<string> _originImageFilePathList22 = new(Enumerable.Repeat("", 8));

    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxHeightList2 = new(Enumerable.Repeat(0.0, 8));

    [ObservableProperty]
    private ObservableCollection<string> _originImageFilePathList23 = [];

    [ObservableProperty]
    private ObservableCollection<double> _originImageAngleList23 = [];

    [ObservableProperty]
    private ObservableCollection<string> _originImageFilePathList31 = [];

    [ObservableProperty]
    private ObservableCollection<string> _originImageFilePathList32 = new(Enumerable.Repeat("", 2));

    [ObservableProperty]
    public ObservableCollection<double> _cgFFBoxWidthList3 = new(Enumerable.Repeat(0.0, 2));

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImageDrawable _bitmapImageDrawable1 = new();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImageDrawable _bitmapImageDrawable2 = new();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImageDrawable _bitmapImageDrawable3 = new();

    [ObservableProperty]
    private CircleROIDrawable? _circleROIDrawable;

    [ObservableProperty]
    private ObservableCollection<RectROIDrawable> _rectROIDrawableList = [];

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;
}