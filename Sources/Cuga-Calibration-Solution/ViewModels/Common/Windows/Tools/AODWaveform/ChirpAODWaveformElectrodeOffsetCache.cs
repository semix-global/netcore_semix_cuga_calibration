using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformElectrodeOffsetCache : AODWaveformElectrodeOffsetCache<ChirpAODWaveformElectrodeOffsetItem, ChirpAODWaveformElectrodeOffsetResult>
{
    [ObservableProperty]
    public partial double PrescanFrequency { get; set; }
}