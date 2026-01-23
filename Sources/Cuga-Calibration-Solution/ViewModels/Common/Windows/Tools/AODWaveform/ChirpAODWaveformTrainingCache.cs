using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Models.Geometries;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformTrainingCache : ObservableCacheBase
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private GeneratePrescanAODWaveformParam _generatePrescanAODWaveformParam = new();

    [ObservableProperty]
    private GenerateChirpAODWaveformParam _generateChirpAODWaveformParam = new();

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private Point _dSWMachinePosition = Point.Origin;

    [ObservableProperty]
    private double _scanLength;

    [ObservableProperty]
    private double _centerECS;

    [ObservableProperty]
    private double _rangeECS;

    [ObservableProperty]
    private double _startC2Coefficient;

    [ObservableProperty]
    private double _stepC2Coefficient;

    [ObservableProperty]
    private double _stopC2Coefficient;

    [ObservableProperty]
    private double _startC3Coefficient;

    [ObservableProperty]
    private double _stepC3Coefficient;

    [ObservableProperty]
    private double _stopC3Coefficient;

    [ObservableProperty]
    private double _startC4Coefficient;

    [ObservableProperty]
    private double _stepC4Coefficient;

    [ObservableProperty]
    private double _stopC4Coefficient;

    [ObservableProperty]
    private double _startC5Coefficient;

    [ObservableProperty]
    private double _stepC5Coefficient;

    [ObservableProperty]
    private double _stopC5Coefficient;

    [ObservableProperty]
    private double _startC6Coefficient;

    [ObservableProperty]
    private double _stepC6Coefficient;

    [ObservableProperty]
    private double _stopC6Coefficient;

    [ObservableProperty]
    private double _startC7Coefficient;

    [ObservableProperty]
    private double _stepC7Coefficient;

    [ObservableProperty]
    private double _stopC7Coefficient;

    [ObservableProperty]
    private double _startC8Coefficient;

    [ObservableProperty]
    private double _stepC8Coefficient;

    [ObservableProperty]
    private double _stopC8Coefficient;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformTrainingItem> _items = [];
}