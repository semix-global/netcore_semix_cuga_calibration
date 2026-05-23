using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformElectrodeInitializeCache : AODWaveformElectrodeInitializeCache<ChirpAODWaveformElectrodeInitializeItem, ChirpAODWaveformElectrodeInitializeResult>
{
    [ObservableProperty]
    public partial double PrescanFrequency { get; set; }
}