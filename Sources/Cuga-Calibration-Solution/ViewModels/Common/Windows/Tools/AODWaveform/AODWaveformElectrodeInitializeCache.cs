using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform.Generates;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformElectrodeInitializeCache<TItem, TResult> : AODWaveformCommonCache
    where TItem : AODWaveformElectrodeInitializeItem, new()
    where TResult : AODWaveformCommonResult, new()
{
    [ObservableProperty]
    private int _interpolationCount = 3;

    [ObservableProperty]
    private IReadOnlyList<double> _electrode2OffsetFrequencyPeriodCoefficients = [];

    [ObservableProperty]
    private IReadOnlyList<double> _electrode4OffsetFrequencyPeriodCoefficients = [];

    [ObservableProperty]
    private IReadOnlyList<double> _frequencies = [];

    partial void OnFrequenciesChanged(IReadOnlyList<double> value) => Weights = [.. value.Select(_ => 1)];

    [ObservableProperty]
    private IReadOnlyList<double> _weights = [];

    [ObservableProperty]
    private double _startElectrode3OffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _stepElectrode3OffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _stopElectrode3OffsetFrequencyPeriodCoefficient;

    #region Items

    [ObservableProperty]
    private AODWaveformInitializeStep0<TItem> _step0 = new();

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeInitializeStep1<TItem>> _step1Items = [];

    #endregion

    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> _electrodeConfigurationResults = [];

    [ObservableProperty]
    private IReadOnlyList<TResult> _results = [];
}