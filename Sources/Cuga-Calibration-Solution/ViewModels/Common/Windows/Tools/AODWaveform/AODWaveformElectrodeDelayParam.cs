using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeDelayParam : ObservableObject
{
    [ObservableProperty]
    public partial OpticsAODElectrodeEnum OpticsAODElectrodeEnum { get; set; }

    [ObservableProperty]
    public partial double BoardCardDelay { get; set; }

    [ObservableProperty]
    public partial double MinDelay { get; set; } = 0d;

    [ObservableProperty]
    public partial double MaxDelay { get; set; } = 10d;

    public object ToHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        BoardCardDelay,
        MinDelay,
        MaxDelay
    };
}