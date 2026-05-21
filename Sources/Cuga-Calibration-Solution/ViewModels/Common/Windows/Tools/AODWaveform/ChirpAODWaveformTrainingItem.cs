using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformTrainingItem : ObservableObject, IEquatable<ChirpAODWaveformTrainingItem>
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
    public partial double P2Coefficient { get; set; }

    [ObservableProperty]
    public partial double P3Coefficient { get; set; }

    [ObservableProperty]
    public partial double P4Coefficient { get; set; }

    [ObservableProperty]
    public partial double P5Coefficient { get; set; }

    [ObservableProperty]
    public partial double P6Coefficient { get; set; }

    [ObservableProperty]
    public partial double P7Coefficient { get; set; }

    [ObservableProperty]
    public partial double P8Coefficient { get; set; }

    [ObservableProperty]
    public partial BestFocus BestFocus { get; set; } = new();

    public object ToHtmlAnonymous() => new
    {
        ProductivityInformation,
        LaserLightInformation,
        CIBInformation,
        P2Coefficient,
        P3Coefficient,
        P4Coefficient,
        P5Coefficient,
        P6Coefficient,
        P7Coefficient,
        P8Coefficient,
        BestFocus.RawImageFilePath,
        BestFocus.BestYStrehlRatioPoint
    };

    public bool Equals(ChirpAODWaveformTrainingItem? other) => ReferenceEquals(this, other) || (P2Coefficient.Equals(other?.P2Coefficient)
                                                                                                && P3Coefficient.Equals(other.P3Coefficient)
                                                                                                && P4Coefficient.Equals(other.P4Coefficient)
                                                                                                && P5Coefficient.Equals(other.P5Coefficient)
                                                                                                && P6Coefficient.Equals(other.P6Coefficient)
                                                                                                && P7Coefficient.Equals(other.P7Coefficient)
                                                                                                && P8Coefficient.Equals(other.P8Coefficient)
                                                                                                && BestFocus.RawImageFilePath.Equals(other.BestFocus.RawImageFilePath)
                                                                                                && BestFocus.BestYStrehlRatioPoint.Equals(other.BestFocus.BestYStrehlRatioPoint));

    public override bool Equals(object? obj) => obj is ChirpAODWaveformTrainingItem other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(P2Coefficient, P3Coefficient, P4Coefficient, P5Coefficient, P6Coefficient, P7Coefficient, P8Coefficient, BestFocus.BestYStrehlRatioPoint);
}