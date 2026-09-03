using CommunityToolkit.Mvvm.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequencyUniformityItem<TItem> : ObservableObject
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    public partial IReadOnlyList<TItem> FrequencyItems { get; set; } = [];

    [ObservableProperty]
    public partial TItem? MaxItem { get; set; }
}