using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Optics.SC;

public sealed partial class OpticsSCCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum;

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<OpticsIlluminationModeEnum, OpticsSCCacheItem>))]
    public ConcurrentDictionary<OpticsIlluminationModeEnum, OpticsSCCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public OpticsSCCacheItem Item => Items.GetOrAdd(OpticsIlluminationModeEnum, _ => new OpticsSCCacheItem());
}

public sealed partial class OpticsSCCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private OpticsConfiguration _opticsConfiguration = new();

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private AlignmentResultDto _alignmentResult = new();

    [ObservableProperty]
    private Point _dSWFindBFMachinePosition;

    [ObservableProperty]
    private double _scanLength;

    [ObservableProperty]
    private double _startLambda;

    [ObservableProperty]
    private double _stepLambda;

    [ObservableProperty]
    private double _stopLambda;

    [ObservableProperty]
    private double _lambdaToL1Coefficient;

    [ObservableProperty]
    private double _lambdaToL3Coefficient;

    [ObservableProperty]
    private double _sCMotorAbsoluteValueL1Center;

    [ObservableProperty]
    private double _sCMotorAbsoluteValueL3Center;

    [ObservableProperty]
    private bool _isL1ToL2Direction;

    [ObservableProperty]
    private bool _isL2ToL3Direction;

    [ObservableProperty]
    private double _centerECS;

    [ObservableProperty]
    private double _rangeECS;
}