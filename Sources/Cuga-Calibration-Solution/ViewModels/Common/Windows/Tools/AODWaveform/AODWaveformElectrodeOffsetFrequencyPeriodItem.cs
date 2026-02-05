using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models.Geometries;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem> : ObservableObject
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    private IReadOnlyList<TItem> _frequencyItems = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _frequencyInterpolationPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _frequencyMaximaPoints = [];
}