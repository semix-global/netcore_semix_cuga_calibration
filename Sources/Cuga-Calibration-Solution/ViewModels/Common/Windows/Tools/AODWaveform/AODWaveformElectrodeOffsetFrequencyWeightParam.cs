using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequencyWeightParam : ObservableObject
{
    [ObservableProperty]
    public partial double Frequency { get; set; }

    [ObservableProperty]
    public partial double Weight { get; set; }

    public object ToHtmlAnonymous() => new
    {
        Frequency,
        Weight
    };
}