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

namespace Core.Models.Models.Fourier.CenterChannelSpecularBlocker;

public sealed partial class PupilCenterChannelSpecularBlockerCache : CalibrationCacheBase<PupilCenterChannelSpecularBlockerCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial OpticsIlluminationModeEnum OpticsIlluminationModeEnum { get; set; } = CalibrationConstantsHelper.MainOpticsIlluminationModeEnum;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImage? Ch3Image { get; set; }

    [ObservableProperty]
    public partial RectROIDrawable? RectROIDrawable { get; set; }

    [ObservableProperty]
    public partial CircleROIDrawable? CircleROIDrawable { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<RectROIDrawable> RectROIDrawableList { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImageDrawable BitmapImageDrawableCh30 { get; set; } = new();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial BitmapImageDrawable BitmapImageDrawableCh31 { get; set; } = new();

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<(OpticsIlluminationModeEnum, ProductivityInformation), PupilCenterChannelSpecularBlockerCacheItem>))]
    public ConcurrentDictionary<(OpticsIlluminationModeEnum, ProductivityInformation), PupilCenterChannelSpecularBlockerCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public PupilCenterChannelSpecularBlockerCacheItem Item => Items.GetOrAdd((OpticsIlluminationModeEnum, ProductivityInformation), _ => new PupilCenterChannelSpecularBlockerCacheItem());

    public override PupilCenterChannelSpecularBlockerCache Clone() => new()
    {
        OpticsIlluminationModeEnum = OpticsIlluminationModeEnum,
        ProductivityInformation = ProductivityInformation.Clone(),
        Ch3Image = Ch3Image,
        RectROIDrawable = RectROIDrawable,
        CircleROIDrawable = CircleROIDrawable,
        RectROIDrawableList = new ObservableCollection<RectROIDrawable>([.. RectROIDrawableList]),
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
    public partial Point ShinyWaferPosition { get; set; }

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial CIBInformation CIBInformation3 { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial string OriginImageFilePathOld { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OriginImageFilePathNew { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ImageGrayCompareCh3 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial float ImageGrayOldCh3 { get; set; }

    [ObservableProperty]
    public partial float ImageGrayNewCh3 { get; set; }

    [ObservableProperty]
    public partial float Ch3Angle { get; set; } = 1;

    [ObservableProperty]
    public partial float Ch3TurnX { get; set; } = 0.2f;

    [ObservableProperty]
    public partial float Ch3TurnY { get; set; } = 0.3f;

    [ObservableProperty]
    public partial float Ch3Push { get; set; } = 0.3f;

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