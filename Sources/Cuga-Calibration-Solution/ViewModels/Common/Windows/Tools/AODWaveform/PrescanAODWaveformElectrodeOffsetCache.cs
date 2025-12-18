using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class PrescanAODWaveformElectrodeOffsetCache : AODWaveformElectrodeOffsetCache<PrescanAODWaveformElectrodeOffsetItem, PrescanAODWaveformElectrodeOffsetResult>
{
    [ObservableProperty]
    private double _chirpFrequency;
}