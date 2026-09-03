using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformElectrodeOffsetResult : AODWaveformElectrodeOffsetResult
{
    [ObservableProperty]
    public partial GenerateChirpAODWaveformParam GenerateChirpAODWaveformParam { get; set; } = new();

    [ObservableProperty]
    public partial string ChirpAODWaveformResultFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<ChirpAODWaveformProfile> ChirpAODWaveformProfiles { get; set; } = [];
}