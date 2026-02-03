using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;

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