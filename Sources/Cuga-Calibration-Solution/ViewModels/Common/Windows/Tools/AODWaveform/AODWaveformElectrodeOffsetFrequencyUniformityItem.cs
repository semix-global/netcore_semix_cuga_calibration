using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequencyUniformityItem<TItem> : ObservableCacheBase
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    private IReadOnlyList<TItem> _frequencyItems = [];

    [ObservableProperty]
    private TItem? _maxItem;
}