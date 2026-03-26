using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformTrainingItem : ObservableObject, IEquatable<ChirpAODWaveformTrainingItem>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];

    [ObservableProperty]
    private string _chirpAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];

    [ObservableProperty]
    private double _p2Coefficient;

    [ObservableProperty]
    private double _p3Coefficient;

    [ObservableProperty]
    private double _p4Coefficient;

    [ObservableProperty]
    private double _p5Coefficient;

    [ObservableProperty]
    private double _p6Coefficient;

    [ObservableProperty]
    private double _p7Coefficient;

    [ObservableProperty]
    private double _p8Coefficient;

    [ObservableProperty]
    private BestFocus _bestFocus = new();

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