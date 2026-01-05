using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.AOD.Uniformity;

public sealed partial class AODUniformityCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private int _calibratingRetryTimes = 5;

    [ObservableProperty]
    private double _calibratingThreshold = 1;

    [ObservableProperty]
    private double _reviewThreshold = 1;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, AODUniformityCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public AODUniformityCacheItem Item => Items.GetOrAdd(ProductivityInformation, new AODUniformityCacheItem());
}

public sealed partial class AODUniformityCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private Point _hazeFindBFMachinePosition;

    [ObservableProperty]
    private int _imageWidth = 1000;
}