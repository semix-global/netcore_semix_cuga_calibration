using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

public sealed partial class PupilSideChannelFlexibleApertureCache : CalibrationCacheBase<PupilSideChannelFlexibleApertureCache>
{
    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImage? Ch1Image { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImage? Ch2Image { get; set; }

    [ObservableProperty]
    public partial Point HazeWaferPosition { get; set; }

    [ObservableProperty]
    public partial string OriginImageFilePath1 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<string> OriginImageFilePathList1 { get; set; } = [];

    [ObservableProperty]
    public partial string OriginImageFilePath2 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<string> OriginImageFilePathList2 { get; set; } = [];

    [ObservableProperty]
    public partial RectROIDrawable? RectROIDrawable { get; set; }

    [ObservableProperty]
    public partial CircleROIDrawable? CircleROIDrawable { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<RectROIDrawable> RectROIDrawableList { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImageDrawable BitmapImageDrawableCh1 { get; set; } = new();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImageDrawable BitmapImageDrawableCh2 { get; set; } = new();

    [ObservableProperty]
    public partial bool IsToggleSelectRectROIDrawableCh1 { get; set; }

    [ObservableProperty]
    public partial bool IsToggleSelectRectROIDrawableCh2 { get; set; }

    [ObservableProperty]
    public partial Point CgFFBoxBeginPositionCh1 { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point CgFFBoxEndPositionCh1 { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point CgFFBoxBeginPositionCh2 { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point CgFFBoxEndPositionCh2 { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial int CgFFBoxBeginNumber1Ch1 { get; set; }

    [ObservableProperty]
    public partial int CgFFBoxBeginNumber2Ch1 { get; set; }

    [ObservableProperty]
    public partial int CgFFBoxBeginNumber1Ch2 { get; set; }

    [ObservableProperty]
    public partial int CgFFBoxBeginNumber2Ch2 { get; set; }

    [ObservableProperty]
    public partial int CgFFBoxEndNumber1Ch1 { get; set; }

    [ObservableProperty]
    public partial int CgFFBoxEndNumber2Ch1 { get; set; }

    [ObservableProperty]
    public partial int CgFFBoxEndNumber1Ch2 { get; set; }

    [ObservableProperty]
    public partial int CgFFBoxEndNumber2Ch2 { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<int> CgFFBoxRodWidthListCh1 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<int> CgFFBoxRodWidthListCh2 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<double> CgFFBoxHeightRelationPercentListCh1 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<double> CgFFBoxHeightRelationPercentListCh2 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Rect> CurrentImageRectListFirstCh1 { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Rect> CurrentImageRectListFirstCh2 { get; set; } = [];

    [ObservableProperty]
    public partial double CgFFBoxAllRodsBeginPercentCh1 { get; set; }

    [ObservableProperty]
    public partial double CgFFBoxAllRodsBeginPercentCh2 { get; set; }

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    public override PupilSideChannelFlexibleApertureCache Clone() => new()
    {
        Ch1Image = Ch1Image,
        Ch2Image = Ch2Image,
        HazeWaferPosition = HazeWaferPosition,
        OriginImageFilePath1 = OriginImageFilePath1,
        OriginImageFilePathList1 = new ObservableCollection<string>([.. OriginImageFilePathList1]),
        OriginImageFilePath2 = OriginImageFilePath2,
        OriginImageFilePathList2 = new ObservableCollection<string>([.. OriginImageFilePathList2]),
        RectROIDrawable = RectROIDrawable,
        CircleROIDrawable = CircleROIDrawable,
        RectROIDrawableList = new ObservableCollection<RectROIDrawable>([.. RectROIDrawableList]),
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
        CgFFBoxRodWidthListCh1 = new ObservableCollection<int>([.. CgFFBoxRodWidthListCh1]),
        CgFFBoxRodWidthListCh2 = new ObservableCollection<int>([.. CgFFBoxRodWidthListCh2]),
        CgFFBoxHeightRelationPercentListCh1 = new ObservableCollection<double>([.. CgFFBoxHeightRelationPercentListCh1]),
        CgFFBoxHeightRelationPercentListCh2 = new ObservableCollection<double>([.. CgFFBoxHeightRelationPercentListCh2]),
        CurrentImageRectListFirstCh1 = new ObservableCollection<Rect>([.. CurrentImageRectListFirstCh1]),
        CurrentImageRectListFirstCh2 = new ObservableCollection<Rect>([.. CurrentImageRectListFirstCh2]),
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