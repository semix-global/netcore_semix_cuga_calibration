using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class V0AODWaveformElectrodeOffsetFrequencyUniformityParam : ObservableObject
{
    [ObservableProperty]
    public partial OpticsAODElectrodeEnum OpticsAODElectrodeEnum { get; set; }

    [ObservableProperty]
    public partial double StartAmplitude { get; set; } = 1;

    [ObservableProperty]
    public partial double StepAmplitude { get; set; } = 1;

    [ObservableProperty]
    public partial double StopAmplitude { get; set; } = 1;

    public object ToHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        StartAmplitude,
        StepAmplitude,
        StopAmplitude
    };
}