using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.AOD.Uniformity;

public sealed partial class AODUniformityCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial int CalibratingRetryTimes { get; set; } = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrateThresholdMin), nameof(CalibrateThresholdMax))]
    public partial double CalibrateThreshold { get; set; } = 0.05;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public double CalibrateThresholdMin => 1 - CalibrateThreshold;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public double CalibrateThresholdMax => 1 + CalibrateThreshold;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReviewThresholdMin), nameof(ReviewThresholdMax))]
    public partial double ReviewThreshold { get; set; } = 0.05;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public double ReviewThresholdMin => 1 - ReviewThreshold;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public double ReviewThresholdMax => 1 + ReviewThreshold;

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<(ProductivityInformation ProductivityInformation, LaserLightInformation LaserLightInformation), AODUniformityCacheItem>))]
    public ConcurrentDictionary<(ProductivityInformation ProductivityInformation, LaserLightInformation LaserLightInformation), AODUniformityCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public AODUniformityCacheItem Item => Items.GetOrAdd((ProductivityInformation, LaserLightInformation), _ => new AODUniformityCacheItem());
}

public sealed partial class AODUniformityCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial Point HazeFindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial int PrescanAODWaveformProfileSegmentCount { get; set; } = 20;

    [ObservableProperty]
    public partial int ImageHorizontalProjectsSegmentCount { get; set; } = 100;

    [ObservableProperty]
    public partial int InitializeWindowLinearSpacedCount { get; set; } = 11;

    [ObservableProperty]
    public partial double InitializeWindowLinearSpacedRate { get; set; } = 0.5;

    [ObservableProperty]
    public partial int ImageHorizontalProjectsSkipCout { get; set; }

    [ObservableProperty]
    public partial int ImageHorizontalProjectsSkipLastCout { get; set; }

    [ObservableProperty]
    public partial double WindowLimitRate { get; set; } = 0.2;

    [ObservableProperty]
    public partial double WindowInterval { get; set; } = 0.01;

    [ObservableProperty]
    public partial double WaitTime { get; set; } = 5;
}