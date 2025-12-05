using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;

namespace Core.Models.Models.CIB.MMD;

public sealed partial class CIBMMDCache : CalibrationCacheBase
{
    [ObservableProperty]
    private IReadOnlyList<CIBInformation> _cIBInformations = [];

    [ObservableProperty]
    private Point _findBFMachinePosition;

    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _aFOffsetMotor;

    [ObservableProperty]
    private double _aFECS;

    [ObservableProperty]
    private bool _isAFEnable;

    [ObservableProperty]
    private double _prescanFrequency;

    [ObservableProperty]
    private GeneratePrescanAODWaveformParam _generatePrescanAODWaveformParam = new() { FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Flatness };

    [ObservableProperty]
    private double _chirpFrequency;

    [ObservableProperty]
    private GenerateChirpAODWaveformParam _generateChirpAODWaveformParam = new() { FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Flatness };

    [ObservableProperty]
    private CIBProfileModeEnum _cIBProfileMode = CIBProfileModeEnum.PMTVoltage;

    [ObservableProperty]
    private double _measurePowerWaitTime = 5;

    [ObservableProperty]
    private double _pMTValueWaitTime = 1;

    [ObservableProperty]
    private double _startCoefficient = 0.01;

    [ObservableProperty]
    private double _stepCoefficient = 0.1;

    [ObservableProperty]
    private double _stopCoefficient = 1;

    [ObservableProperty]
    private double _measurePowerSequenceCommonRatio = 1 / 1.3;

    [ObservableProperty]
    private double _measurePowerNotUseODFilterMinValue = 1;

    [ObservableProperty]
    private double _mMDMeasurePowerRangeRatio = 1000;

    [ObservableProperty]
    private double _startGain = -10;

    [ObservableProperty]
    private double _stepGain = 0.5;

    [ObservableProperty]
    private double _stopGain = 10;

    [ObservableProperty]
    private double _protectedPMTValue = 409.6;

    [ObservableProperty]
    private double _protectedCount = 3;

    [ObservableProperty]
    private int _catchPMTValueCount = 10;

    [ObservableProperty]
    private int _concurrentCount = 2;

    [ObservableProperty]
    private double _darkCurrent;

    [ObservableProperty]
    private double _denominator = 16384d;

    [ObservableProperty]
    private double _scaleFactor = 2000000d;

    [ObservableProperty]
    private double _minValidFraction;

    [ObservableProperty]
    private double _maxValidFraction = 250000d;

    [ObservableProperty]
    private double _minLogGain = 0.1;

    [ObservableProperty]
    private IReadOnlyList<GainConfiguration> _gainConfigurations = [];

    /********** 缓存的结果 **********/

    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];

    [ObservableProperty]
    private string _chirpAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _measurePowerPoints = [];

    [ObservableProperty]
    private double _p0;

    [ObservableProperty]
    private double _p1;

    [ObservableProperty]
    private double _p2;

    [ObservableProperty]
    private double _p3;

    [ObservableProperty]
    private double _rSquared;

    [ObservableProperty]
    private IReadOnlyList<Point> _fitMeasurePowerPoints = [];

    [ObservableProperty]
    private double _oDFilterRatio;

    [ObservableProperty]
    private IReadOnlyList<Point> _notUseODFilterMeasurePowerPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _useODFilterMeasurePowerPoints = [];

    /********** 缓存的结果 **********/

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    public CIBMMDCache()
    {
        ScatterPlotControl.SetTitle("Measure Power(Y: mW X: Coefficient)");
    }

    public void RefreshPlot()
    {
        ScatterPlotControl.Clear();
        ScatterPlotControl.GetOrAddScatterLine($"Origin OD = {ODFilterRatio:0.######}", MeasurePowerPoints);
        ScatterPlotControl.GetOrAddScatterLine($"Fit Curve: y = {P0:0.######} + {P1:0.######}x + {P2:0.######}x^2 + {P3:0.######}x^3 r^2 = {RSquared:0.######}", FitMeasurePowerPoints);
        ScatterPlotControl.GetOrAddScatterMarkers("Not Use OD", NotUseODFilterMeasurePowerPoints, markerShape: MarkerShape.FilledDiamond);
        ScatterPlotControl.GetOrAddScatterMarkers("Use OD", UseODFilterMeasurePowerPoints, markerShape: MarkerShape.OpenDiamond);
    }

    public sealed class GainConfiguration
    {
        public double Gain { get; init; }

        /// <summary>
        /// 14bitSense值, 无符号位
        /// </summary>
        public int SenseU14Bit { get; init; }

        /// <summary>
        /// 16位增益值, 有符号位
        /// </summary>
        public int GainS16Bit { get; init; }
    }
}