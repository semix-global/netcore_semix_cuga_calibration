using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeDelayItem<TItem> : ObservableObject
    where TItem : AODWaveformElectrodeDelayItem, new()
{
    [ObservableProperty]
    public partial double[] Delays { get; set; } = [];

    [ObservableProperty]
    public partial TItem[] FrequencyItems { get; set; } = [];

    [ObservableProperty]
    public partial double Score { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public object ToHtmlAnonymous() => new
    {
        Delays,
        Score,
        IsSelected
    };
}