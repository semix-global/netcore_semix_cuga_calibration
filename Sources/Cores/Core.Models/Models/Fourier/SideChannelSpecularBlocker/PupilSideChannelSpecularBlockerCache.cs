using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Fourier.SideChannelSpecularBlocker;

public sealed partial class PupilSideChannelSpecularBlockerCache : CalibrationCacheBase<PupilSideChannelSpecularBlockerCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial OpticsIlluminationModeEnum OpticsIlluminationModeEnum { get; set; } = CalibrationConstantsHelper.MainOpticsIlluminationModeEnum;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImage? Ch1Image { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImage? Ch2Image { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImageDrawable BitmapImageDrawableCh10 { get; set; } = new();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImageDrawable BitmapImageDrawableCh11 { get; set; } = new();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImageDrawable BitmapImageDrawableCh20 { get; set; } = new();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImageDrawable BitmapImageDrawableCh21 { get; set; } = new();

    [ObservableProperty]
    public partial RectROIDrawable? RectROIDrawableCh11 { get; set; }

    [ObservableProperty]
    public partial CircleROIDrawable? CircleROIDrawableCh11 { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<RectROIDrawable> RectROIDrawableListCh11 { get; set; } = [];

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<(OpticsIlluminationModeEnum, ProductivityInformation), PupilSideChannelSpecularBlockerCacheItem>))]
    public ConcurrentDictionary<(OpticsIlluminationModeEnum, ProductivityInformation), PupilSideChannelSpecularBlockerCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
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
        RectROIDrawableListCh11 = new ObservableCollection<RectROIDrawable>([.. RectROIDrawableListCh11]),
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
    public partial Point ShinyWaferPosition { get; set; }

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial CIBInformation CIBInformation1 { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial CIBInformation CIBInformation2 { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial string OriginImageFilePathOld1 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OriginImageFilePathOld2 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OriginImageFilePathNew1 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OriginImageFilePathNew2 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ImageGrayCompareCh1 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ImageGrayCompareCh2 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial float ImageGrayOldCh1 { get; set; }

    [ObservableProperty]
    public partial float ImageGrayOldCh2 { get; set; }

    [ObservableProperty]
    public partial float ImageGrayNewCh1 { get; set; }

    [ObservableProperty]
    public partial float ImageGrayNewCh2 { get; set; }

    [ObservableProperty]
    public partial Point CgFFBoxBeginPositionCh1 { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point CgFFBoxBeginPositionCh2 { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial List<int> CgFFBoxBeginAndEndNumberCh1 { get; set; } = [];

    [ObservableProperty]
    public partial List<int> CgFFBoxBeginAndEndNumberCh2 { get; set; } = [];

    [ObservableProperty]
    public partial List<double> CgFFBoxMoveDownPercentListCh1 { get; set; } = [0.2, 0.3, 0.8, 0.9, 0.6, 0.8];

    [ObservableProperty]
    public partial List<double> CgFFBoxMoveDownPercentListCh2 { get; set; } = [0.2, 0.3, 0.8, 0.9, 0.6, 0.8];

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