using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformElectrodeInitializeItem : AODWaveformElectrodeInitializeItem
{
    [ObservableProperty]
    public partial string PrescanAODWaveformResultFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<PrescanAODWaveformProfile> PrescanAODWaveformProfiles { get; set; } = [];

    [ObservableProperty]
    public partial string ChirpAODWaveformResultFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<ChirpAODWaveformProfile> ChirpAODWaveformProfiles { get; set; } = [];
}