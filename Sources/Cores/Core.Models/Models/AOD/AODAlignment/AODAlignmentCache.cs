using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.AOD.AODAlignment;

public sealed partial class AODAlignmentCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _threshold;

    public ConcurrentBag<KeyValuePair<ProductivityInformation, AODAlignmentCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public AODAlignmentCacheItem Item => Items.GetOrAdd(ProductivityInformation, new AODAlignmentCacheItem());
}

public sealed partial class AODAlignmentCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private Point _findBFMachinePosition;
    
    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private int _pMTId = CalibrationConstantsHelper.MainPmtId;

    [ObservableProperty]
    private int _channelId = CalibrationConstantsHelper.MainChannelId;

    [ObservableProperty]
    private int _imageWidth = 1000;

    [ObservableProperty]
    private GeneratePrescanAODWaveformParam _generatePrescanAODWaveformParam = new();

    [ObservableProperty]
    private double _startPrescanFrequency;

    [ObservableProperty]
    private double _stepPrescanFrequency;

    [ObservableProperty]
    private double _stopPrescanFrequency;
}