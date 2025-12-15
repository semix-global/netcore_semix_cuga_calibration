using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class PrescanAODWaveformElectrodeInitializeCache : AODWaveformElectrodeInitializeCache<PrescanAODWaveformElectrodeInitializeItem, PrescanAODWaveformElectrodeInitializeResult>
{
    [ObservableProperty]
    private double _chirpFrequency;
}
