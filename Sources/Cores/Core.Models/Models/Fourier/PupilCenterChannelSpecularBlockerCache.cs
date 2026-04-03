using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Fourier;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models.Models.Fourier;


public sealed partial class PupilCenterChannelSpecularBlockerCache : CalibrationCacheBase
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
    private BitmapImage? _ch3Image = null;

    [ObservableProperty]
    private RectROIDrawable? _rectROIDrawable;

    [ObservableProperty]
    private CircleROIDrawable? _circleROIDrawable;

    [ObservableProperty]
    private ObservableCollection<RectROIDrawable> _rectROIDrawableList = new();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImageDrawable _bitmapImageDrawableCh30 = new();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private BitmapImageDrawable _bitmapImageDrawableCh31 = new();

    public ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), PupilCenterChannelSpecularBlockerCacheItem>> Items { get; init; } = [];

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]

    public PupilCenterChannelSpecularBlockerCacheItem Item => Items.GetOrAdd((OpticsIlluminationModeEnum, ProductivityInformation), new PupilCenterChannelSpecularBlockerCacheItem());
}

public sealed partial class PupilCenterChannelSpecularBlockerCacheItem : CalibrationCacheBase
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
    private CIBInformation _cIBInformation3 = CIBInformation.Default;

    [ObservableProperty]
    private string _originImageFilePathOld = string.Empty;

    [ObservableProperty]
    private string _originImageFilePathNew = string.Empty;

    [ObservableProperty]
    private string _imageGrayCompareCh3 = string.Empty;

    [ObservableProperty]
    private float _imageGrayOldCh3 = 0;

    [ObservableProperty]
    private float _imageGrayNewCh3 = 0;

    [ObservableProperty]
    public int _ch3Angle = 1;

    [ObservableProperty]
    public float _ch3TurnX = 0.2f;

    [ObservableProperty]
    public float _ch3TurnY = 0.3f;

    [ObservableProperty]
    public float _ch3Push = 0.3f;
}

