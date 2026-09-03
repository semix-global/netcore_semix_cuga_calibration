using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class V0PrescanAODWaveformElectrodeOffsetCache : V0AODWaveformElectrodeOffsetCache<V0PrescanAODWaveformElectrodeOffsetItem, V0PrescanAODWaveformElectrodeOffsetResult>
{
    [ObservableProperty]
    public partial double ChirpFrequency { get; set; }
}