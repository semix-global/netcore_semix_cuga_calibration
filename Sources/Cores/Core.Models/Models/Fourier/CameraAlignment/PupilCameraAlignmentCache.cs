using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Fourier.CameraAlignment;

public sealed partial class PupilCameraAlignmentCache : CalibrationCacheBase<PupilCameraAlignmentCache>
{
    [ObservableProperty]
    public partial Point HazeWaferPosition { get; set; }

    [ObservableProperty]
    public partial string PrimaryImageFilePath1 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PrimaryImageFilePath2 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PrimaryImageFilePath3 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OriginImageFilePath1 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OriginImageFilePath2 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OriginImageFilePath3 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial RectROIDrawable? RectROIDrawableCh1 { get; set; }

    [ObservableProperty]
    public partial RectROIDrawable? RectROIDrawableCh2 { get; set; }

    [ObservableProperty]
    public partial RectROIDrawable? RectROIDrawableCh3 { get; set; }

    [ObservableProperty]
    public partial CircleROIDrawable? CircleROIDrawableCh1 { get; set; }

    [ObservableProperty]
    public partial CircleROIDrawable? CircleROIDrawableCh2 { get; set; }

    [ObservableProperty]
    public partial CircleROIDrawable? CircleROIDrawableCh3 { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<RectROIDrawable> RectROIDrawableListCh1 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<RectROIDrawable> RectROIDrawableListCh2 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<RectROIDrawable> RectROIDrawableListCh3 { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImageDrawable? BitmapImageDrawableCh1 { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImageDrawable? BitmapImageDrawableCh2 { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImageDrawable? BitmapImageDrawableCh3 { get; set; }

    [ObservableProperty]
    public partial bool IsToggleSelectRectROIDrawableCh1 { get; set; }

    [ObservableProperty]
    public partial bool IsToggleSelectRectROIDrawableCh2 { get; set; }

    [ObservableProperty]
    public partial bool IsToggleSelectRectROIDrawableCh3 { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImage? Ch1Image { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImage? Ch2Image { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImage? Ch3Image { get; set; }

    [ObservableProperty]
    public partial Point RectCh1Position { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial int Ch1ImageWidth { get; set; }

    [ObservableProperty]
    public partial int Ch1ImageHeight { get; set; }

    [ObservableProperty]
    public partial Point RectCh2Position { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial int Ch2ImageWidth { get; set; }

    [ObservableProperty]
    public partial int Ch2ImageHeight { get; set; }

    [ObservableProperty]
    public partial Point RectCh3Position { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial int Ch3ImageWidth { get; set; }

    [ObservableProperty]
    public partial int Ch3ImageHeight { get; set; }

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

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
        RectROIDrawableListCh1 = new ObservableCollection<RectROIDrawable>([.. RectROIDrawableListCh1]),
        RectROIDrawableListCh2 = new ObservableCollection<RectROIDrawable>([.. RectROIDrawableListCh2]),
        RectROIDrawableListCh3 = new ObservableCollection<RectROIDrawable>([.. RectROIDrawableListCh3]),
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