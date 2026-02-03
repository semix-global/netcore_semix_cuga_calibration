using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Local.NoSQL.DB.Providers.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequencyPeriodParam : ObservableObject
{
    [ObservableProperty]
    private OpticsAODElectrodeEnum _opticsAODElectrodeEnum;

    [ObservableProperty]
    private double _startOffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _stepOffsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _stopOffsetFrequencyPeriodCoefficient;

    public object ToHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        StartOffsetFrequencyPeriodCoefficient,
        StepOffsetFrequencyPeriodCoefficient,
        StopOffsetFrequencyPeriodCoefficient
    };
}