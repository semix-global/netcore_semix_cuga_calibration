using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Humanizer;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeInitializeStep0Item<TItem> : ObservableObject
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    public partial IReadOnlyList<OpticsAODElectrodeEnum> Electrodes { get; set; } = [];

    public string Title => string.Join(", ", Electrodes.Select(t => t.Humanize()));

    [ObservableProperty]
    public partial IReadOnlyList<TItem> Items { get; set; } = [];
}