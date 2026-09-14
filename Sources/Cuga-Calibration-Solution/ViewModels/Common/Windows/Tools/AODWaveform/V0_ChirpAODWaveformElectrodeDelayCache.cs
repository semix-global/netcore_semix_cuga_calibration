using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class V0ChirpAODWaveformElectrodeDelayCache : V0AODWaveformElectrodeDelayCache<V0ChirpAODWaveformElectrodeDelayItem, V0ChirpAODWaveformElectrodeDelayResult>
{
    [ObservableProperty]
    public partial double PrescanFrequency { get; set; }
}