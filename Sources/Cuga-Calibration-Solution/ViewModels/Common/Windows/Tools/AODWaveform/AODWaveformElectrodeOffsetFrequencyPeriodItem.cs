using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models.Geometries;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem> : ObservableObject
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    public partial IReadOnlyList<TItem> FrequencyItems { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Point> FrequencyInterpolationPoints { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Point> FrequencyMaximaPoints { get; set; } = [];
}