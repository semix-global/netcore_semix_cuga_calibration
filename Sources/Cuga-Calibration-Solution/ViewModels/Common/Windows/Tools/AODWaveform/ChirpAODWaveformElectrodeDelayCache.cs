using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformElectrodeDelayCache : AODWaveformElectrodeDelayCache<ChirpAODWaveformElectrodeDelayItem, ChirpAODWaveformElectrodeDelayResult>
{
    [ObservableProperty]
    public partial double PrescanFrequency { get; set; }
}