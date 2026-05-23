using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformCommonItem : ObservableObject
{
    [ObservableProperty]
    public partial double MeasurePower { get; set; }

    public virtual object ToHtmlAnonymous() => new
    {
        MeasurePower
    };
}