using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformElectrodeInitializeCache : AODWaveformElectrodeInitializeCache<ChirpAODWaveformElectrodeInitializeItem, ChirpAODWaveformElectrodeInitializeResult>
{
    [ObservableProperty]
    private double _prescanFrequency;
}