using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformElectrodeOffsetCache : AODWaveformElectrodeOffsetCache<ChirpAODWaveformElectrodeOffsetItem, ChirpAODWaveformElectrodeOffsetResult>
{
    [ObservableProperty]
    private double _prescanFrequency;
}