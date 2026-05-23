using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class PrescanAODWaveformElectrodeInitializeCache : AODWaveformElectrodeInitializeCache<PrescanAODWaveformElectrodeInitializeItem, PrescanAODWaveformElectrodeInitializeResult>
{
    [ObservableProperty]
    public partial double ChirpFrequency { get; set; }
}