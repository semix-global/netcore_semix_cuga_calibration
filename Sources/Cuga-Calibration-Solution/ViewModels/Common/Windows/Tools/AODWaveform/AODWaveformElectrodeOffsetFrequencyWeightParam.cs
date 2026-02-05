using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequencyWeightParam : ObservableObject
{
    [ObservableProperty]
    private OpticsAODElectrodeEnum _opticsAODElectrodeEnum;

    [ObservableProperty]
    private double _frequency;

    [ObservableProperty]
    private double _weight;

    public object ToHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        Frequency,
        Weight
    };
}