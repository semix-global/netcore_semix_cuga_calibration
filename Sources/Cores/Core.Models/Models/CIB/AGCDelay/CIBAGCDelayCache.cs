using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.CIB.AGCDelay;

public sealed partial class CIBAGCDelayCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial int CalibratingRetryTimes { get; set; } = 5;

    [ObservableProperty]
    public partial double CalibratingThreshold { get; set; } = 1;

    [ObservableProperty]
    public partial double ReviewThreshold { get; set; } = 2;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, CIBAGCDelayCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public CIBAGCDelayCacheItem Item => Items.GetOrAdd(ProductivityInformation, new Lazy<CIBAGCDelayCacheItem>(() => new CIBAGCDelayCacheItem()));
}

public sealed partial class CIBAGCDelayCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial Point HazeFindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial double StartCoefficient { get; set; } = 0.01;

    [ObservableProperty]
    public partial double StepCoefficient { get; set; } = 0.01;

    [ObservableProperty]
    public partial double StopCoefficient { get; set; } = 1d;

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial double TargetPMTValue { get; set; } = 400d;

    [ObservableProperty]
    public partial int MarkerLengthPixel { get; set; } = 30;
}