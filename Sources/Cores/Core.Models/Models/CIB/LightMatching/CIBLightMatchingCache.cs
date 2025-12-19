using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.CIB.LightMatching;

public sealed partial class CIBLightMatchingCache : CalibrationCacheBase
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum = CalibrationConstantsHelper.MainOpticsIlluminationModeEnum;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    public ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), CIBLightMatchingCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public CIBLightMatchingCacheItem Item => Items.GetOrAdd((OpticsIlluminationModeEnum, ProductivityInformation), new CIBLightMatchingCacheItem());
}

public sealed partial class CIBLightMatchingCacheItem : ObservableObject
{
    [ObservableProperty]
    private Point _hazeFindBFMachinePosition;

    [ObservableProperty]
    private Point _dSWFindBFMachinePosition;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private int _imageWidth = 1000;
}