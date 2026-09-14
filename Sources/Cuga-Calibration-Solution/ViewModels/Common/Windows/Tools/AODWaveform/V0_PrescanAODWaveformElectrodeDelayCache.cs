using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class V0PrescanAODWaveformElectrodeDelayCache : V0AODWaveformElectrodeDelayCache<V0PrescanAODWaveformElectrodeDelayItem, V0PrescanAODWaveformElectrodeDelayResult>
{
    [ObservableProperty]
    public partial double ChirpFrequency { get; set; }
}