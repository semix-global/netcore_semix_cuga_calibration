using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem> : ObservableObject
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    public partial double[] OffsetFrequencyPeriodCoefficients { get; set; } = [];

    [ObservableProperty]
    public partial TItem[] FrequencyItems { get; set; } = [];

    [ObservableProperty]
    public partial double Score { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public object ToHtmlAnonymous() => new
    {
        OffsetFrequencyPeriodCoefficients,
        Score,
        IsSelected
    };
}