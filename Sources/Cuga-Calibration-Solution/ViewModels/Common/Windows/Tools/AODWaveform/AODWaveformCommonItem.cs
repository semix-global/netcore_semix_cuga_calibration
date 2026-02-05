using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformCommonItem : ObservableObject
{
    [ObservableProperty]
    private double _measurePower;

    public virtual object ToHtmlAnonymous() => new
    {
        MeasurePower
    };
}