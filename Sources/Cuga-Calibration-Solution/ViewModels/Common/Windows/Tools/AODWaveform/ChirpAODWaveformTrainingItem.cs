using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformTrainingItem : ObservableObject
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial string PrescanAODWaveformResultFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<PrescanAODWaveformProfile> PrescanAODWaveformProfiles { get; set; } = [];

    [ObservableProperty]
    public partial string ChirpAODWaveformResultFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<ChirpAODWaveformProfile> ChirpAODWaveformProfiles { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<GenerateAODWaveformSlopeDeltaKConfiguration> SlopeDeltaKConfigurations { get; set; } = [];

    [ObservableProperty]
    public partial BestFocus BestFocus { get; set; } = new();

    public object ToHtmlAnonymous() => new
    {
        ProductivityInformation,
        LaserLightInformation,
        CIBInformation,
        SlopeDeltaKConfigurations = new HtmlTable([..SlopeDeltaKConfigurations.Select(t => t.ToHtmlAnonymous())]),
        BestFocus.RawImageFilePath,
        BestFocus.BestYStrehlRatioPoint
    };
}