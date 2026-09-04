using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class V0AODWaveformElectrodeOffsetFrequencyPeriodParam : ObservableObject
{
    [ObservableProperty]
    public partial OpticsAODElectrodeEnum OpticsAODElectrodeEnum { get; set; }

    [ObservableProperty]
    public partial double StartOffsetFrequencyPeriodCoefficient { get; set; }

    [ObservableProperty]
    public partial double StepOffsetFrequencyPeriodCoefficient { get; set; }

    [ObservableProperty]
    public partial double StopOffsetFrequencyPeriodCoefficient { get; set; }

    public object ToHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        StartOffsetFrequencyPeriodCoefficient,
        StepOffsetFrequencyPeriodCoefficient,
        StopOffsetFrequencyPeriodCoefficient
    };
}