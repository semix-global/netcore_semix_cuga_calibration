using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequencyPeriodParam : ObservableObject
{
    [ObservableProperty]
    public partial OpticsAODElectrodeEnum OpticsAODElectrodeEnum { get; set; }

    [ObservableProperty]
    public partial double BoardCardOffsetFrequencyPeriodCoefficient { get; set; }

    public object ToHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        BoardCardOffsetFrequencyPeriodCoefficient
    };
}