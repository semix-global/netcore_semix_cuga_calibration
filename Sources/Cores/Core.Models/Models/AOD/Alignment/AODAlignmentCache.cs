using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.AOD.Alignment;

public sealed partial class AODAlignmentCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _threshold = 0.999;

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
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private Point _hazeFindBFMachinePosition;

    [ObservableProperty]
    private int _imageWidth = 1000;

    [ObservableProperty]
    private GeneratePrescanAODWaveformParam _flatnessGeneratePrescanAODWaveformParam = new() { FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Flatness };

    [ObservableProperty]
    private double _startPrescanFrequency;

    [ObservableProperty]
    private double _stepPrescanFrequency;

    [ObservableProperty]
    private double _stopPrescanFrequency;

    [ObservableProperty]
    private int _rangeSkipFitCount = 1;
}