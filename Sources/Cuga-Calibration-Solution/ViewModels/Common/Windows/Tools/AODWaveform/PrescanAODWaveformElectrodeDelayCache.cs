using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class PrescanAODWaveformElectrodeDelayCache : AODWaveformElectrodeDelayCache<PrescanAODWaveformElectrodeDelayItem, PrescanAODWaveformElectrodeDelayResult>
{
    [ObservableProperty]
    public partial double ChirpFrequency { get; set; }
}