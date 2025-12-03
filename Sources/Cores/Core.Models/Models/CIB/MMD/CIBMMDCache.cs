using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.CIB;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.CIB.MMD;

public sealed partial class CIBMMDCache : CalibrationCacheBase
{
    [ObservableProperty]
    private IReadOnlyList<CIBInformation> _cIBInformations = [];

    [ObservableProperty]
    private Point _findBFMachinePosition;

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
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];

    [ObservableProperty]
    private GenerateChirpAODWaveformParam _generateChirpAODWaveformParam = new() { FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum.Flatness };

    [ObservableProperty]
    private string _chirpAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];

    [ObservableProperty]
    private CIBProfileModeEnum _cIBProfileMode = CIBProfileModeEnum.PMTVoltage;

    [ObservableProperty]
    private double _measurePowerWaitTime = 5;

    [ObservableProperty]
    private double _pMTValueWaitTime = 1;

    [ObservableProperty]
    private IReadOnlyList<double> _coefficients = [0.01, 0.013, 0.0169, 0.02197, 0.028561, 0.0371293, 0.04826809, 0.062748517, 0.081573072, 0.106044994, 0.137858492, 0.179216039, 0.232980851, 0.302875107, 0.393737639, 0.51185893, 0.665416609, 0.865041592, 1];

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