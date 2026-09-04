using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class V0ChirpAODWaveformElectrodeOffsetCache : V0AODWaveformElectrodeOffsetCache<V0ChirpAODWaveformElectrodeOffsetItem, V0ChirpAODWaveformElectrodeOffsetResult>
{
    [ObservableProperty]
    public partial double PrescanFrequency { get; set; }
}