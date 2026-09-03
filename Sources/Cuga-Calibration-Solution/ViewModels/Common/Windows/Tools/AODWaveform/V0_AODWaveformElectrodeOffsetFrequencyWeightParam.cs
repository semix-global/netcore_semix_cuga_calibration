using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequencyWeightParam : ObservableObject
{
    [ObservableProperty]
    public partial OpticsAODElectrodeEnum OpticsAODElectrodeEnum { get; set; }

    [ObservableProperty]
    public partial double Frequency { get; set; }

    [ObservableProperty]
    public partial double Weight { get; set; }

    public object ToHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        Frequency,
        Weight
    };
}