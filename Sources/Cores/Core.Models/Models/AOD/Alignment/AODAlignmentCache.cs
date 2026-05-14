using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.AOD.Alignment;

public sealed partial class AODAlignmentCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _threshold = 0.999;

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<ProductivityInformation, AODAlignmentCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, AODAlignmentCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public AODAlignmentCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new AODAlignmentCacheItem());
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
    private OpticsConfiguration _opticsConfiguration = new();

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
    private double _stepPrescanFrequency = 10;

    [ObservableProperty]
    private double _stopPrescanFrequency;

    [ObservableProperty]
    private int _rangeSkipFitCount = 1;
}