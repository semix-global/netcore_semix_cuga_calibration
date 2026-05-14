using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Fourier.CenterChannelFlexibleAperture;

public sealed partial class PupilCenterChannelFlexibleApertureCache : CalibrationCacheBase<PupilCenterChannelFlexibleApertureCache>
{
    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImage? Ch3Image1 { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImage? Ch3Image2 { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImage? Ch3Image3 { get; set; }

    [ObservableProperty]
    public partial Point HazeWaferPosition { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> OriginImageFilePathList11 { get; set; } = new(Enumerable.Repeat("", 4));

    [ObservableProperty]
    public partial ObservableCollection<double> OriginImageAngleList11 { get; set; } = new(Enumerable.Repeat(0.0, 4));

    [ObservableProperty]
    public partial ObservableCollection<string> OriginImageFilePathList12 { get; set; } = new(Enumerable.Repeat("", 8));

    [ObservableProperty]
    public partial ObservableCollection<double> CgFFBoxWidthList1 { get; set; } = new(Enumerable.Repeat(0.0, 8));

    [ObservableProperty]
    public partial ObservableCollection<string> OriginImageFilePathList13 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<double> OriginImageAngleList13 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<string> OriginImageFilePathList21 { get; set; } = new(Enumerable.Repeat("", 4));

    [ObservableProperty]
    public partial ObservableCollection<double> OriginImageAngleList21 { get; set; } = new(Enumerable.Repeat(0.0, 4));

    [ObservableProperty]
    public partial ObservableCollection<string> OriginImageFilePathList22 { get; set; } = new(Enumerable.Repeat("", 8));

    [ObservableProperty]
    public partial ObservableCollection<double> CgFFBoxHeightList2 { get; set; } = new(Enumerable.Repeat(0.0, 8));

    [ObservableProperty]
    public partial ObservableCollection<string> OriginImageFilePathList23 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<double> OriginImageAngleList23 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<string> OriginImageFilePathList31 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<string> OriginImageFilePathList32 { get; set; } = new(Enumerable.Repeat("", 2));

    [ObservableProperty]
    public partial ObservableCollection<double> CgFFBoxWidthList3 { get; set; } = new(Enumerable.Repeat(0.0, 2));

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImageDrawable BitmapImageDrawable1 { get; set; } = new();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImageDrawable BitmapImageDrawable2 { get; set; } = new();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImageDrawable BitmapImageDrawable3 { get; set; } = new();

    [ObservableProperty]
    public partial CircleROIDrawable? CircleROIDrawable { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<RectROIDrawable> RectROIDrawableList { get; set; } = [];

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    public override PupilCenterChannelFlexibleApertureCache Clone() => new()
    {
        Ch3Image1 = Ch3Image1,
        Ch3Image2 = Ch3Image2,
        Ch3Image3 = Ch3Image3,
        HazeWaferPosition = HazeWaferPosition,
        OriginImageFilePathList11 = new ObservableCollection<string>([.. OriginImageFilePathList11]),
        OriginImageAngleList11 = new ObservableCollection<double>([.. OriginImageAngleList11]),
        OriginImageFilePathList12 = new ObservableCollection<string>([.. OriginImageFilePathList12]),
        CgFFBoxWidthList1 = new ObservableCollection<double>([.. CgFFBoxWidthList1]),
        OriginImageFilePathList13 = new ObservableCollection<string>([.. OriginImageFilePathList13]),
        OriginImageAngleList13 = new ObservableCollection<double>([.. OriginImageAngleList13]),
        OriginImageFilePathList21 = new ObservableCollection<string>([.. OriginImageFilePathList21]),
        OriginImageAngleList21 = new ObservableCollection<double>([.. OriginImageAngleList21]),
        OriginImageFilePathList22 = new ObservableCollection<string>([.. OriginImageFilePathList22]),
        CgFFBoxHeightList2 = new ObservableCollection<double>([.. CgFFBoxHeightList2]),
        OriginImageFilePathList23 = new ObservableCollection<string>([.. OriginImageFilePathList23]),
        OriginImageAngleList23 = new ObservableCollection<double>([.. OriginImageAngleList23]),
        OriginImageFilePathList31 = new ObservableCollection<string>([.. OriginImageFilePathList31]),
        OriginImageFilePathList32 = new ObservableCollection<string>([.. OriginImageFilePathList32]),
        CgFFBoxWidthList3 = new ObservableCollection<double>([.. CgFFBoxWidthList3]),
        BitmapImageDrawable1 = BitmapImageDrawable1,
        BitmapImageDrawable2 = BitmapImageDrawable2,
        BitmapImageDrawable3 = BitmapImageDrawable3,
        CircleROIDrawable = CircleROIDrawable,
        RectROIDrawableList = new ObservableCollection<RectROIDrawable>([.. RectROIDrawableList]),
        ProductivityInformation = ProductivityInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}