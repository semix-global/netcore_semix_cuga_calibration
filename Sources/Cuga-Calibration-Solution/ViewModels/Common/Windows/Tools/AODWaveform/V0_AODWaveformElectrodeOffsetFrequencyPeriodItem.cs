using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models.Geometries;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class V0AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem> : ObservableObject
    where TItem : V0AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    public partial IReadOnlyList<TItem> FrequencyItems { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Point> FrequencyInterpolationPoints { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Point> FrequencyMaximaPoints { get; set; } = [];
}