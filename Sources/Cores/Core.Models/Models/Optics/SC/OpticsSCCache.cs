using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Optics.SC;

public sealed partial class OpticsSCCache : CalibrationCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum;

    public ConcurrentBag<KeyValuePair<OpticsIlluminationModeEnum, OpticsSCCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public OpticsSCCacheItem Item => Items.GetOrAdd(OpticsIlluminationModeEnum, new OpticsSCCacheItem());
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
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private AlignmentResultDto _alignmentResult = new();

    [ObservableProperty]
    private Point _dSWFindBFMachinePosition;

    [ObservableProperty]
    private double _scanLength;

    [ObservableProperty]
    private double _startSCMotorAbsoluteValue;

    [ObservableProperty]
    private double _stepSCMotorAbsoluteValue;

    [ObservableProperty]
    private double _stopSCMotorAbsoluteValue;

    [ObservableProperty]
    private double _xZCenterECS;

    [ObservableProperty]
    private double _xZRangeECS;
}