using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequencyUniformityParam : ObservableObject
{
    [ObservableProperty]
    private OpticsAODElectrodeEnum _opticsAODElectrodeEnum;

    [ObservableProperty]
    private double _startAmplitude = 1;

    [ObservableProperty]
    private double _stepAmplitude = 1;

    [ObservableProperty]
    private double _stopAmplitude = 1;

    public object ToHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        StartAmplitude,
        StepAmplitude,
        StopAmplitude
    };
}