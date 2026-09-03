using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class V0AODWaveformElectrodeOffsetFrequencyUniformityItem<TItem> : ObservableObject
    where TItem : V0AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    public partial IReadOnlyList<TItem> FrequencyItems { get; set; } = [];

    [ObservableProperty]
    public partial TItem? MaxItem { get; set; }
}