using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class PrescanAODWaveformElectrodeOffsetResult : AODWaveformElectrodeOffsetResult
{
    [ObservableProperty]
    public partial GeneratePrescanAODWaveformParam GeneratePrescanAODWaveformParam { get; set; } = new();

    [ObservableProperty]
    public partial string PrescanAODWaveformResultFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<PrescanAODWaveformProfile> PrescanAODWaveformProfiles { get; set; } = [];
}