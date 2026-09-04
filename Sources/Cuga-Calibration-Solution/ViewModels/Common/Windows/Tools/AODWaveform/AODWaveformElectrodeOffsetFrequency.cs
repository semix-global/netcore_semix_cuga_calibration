using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequency : ObservableObject
{
    [ObservableProperty]
    public partial double Frequency { get; set; } = 100d;

    [ObservableProperty]
    public partial double Amplitude { get; set; } = 0.1d;

    public object ToHtmlAnonymous() => new
    {
        Frequency,
        Amplitude
    };
}