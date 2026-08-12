using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models.Geometries;
using ScottPlot;
using System.ComponentModel;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using Net.Utilities.ScottPlot.Interfaces;
using Range = ScottPlot.Range;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequencyPeriod<TItem> : ObservableObject
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    public partial IReadOnlyList<AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem>> Items { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

    partial void OnItemsChanged(IReadOnlyList<AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem>>? oldValue, IReadOnlyList<AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem>> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlot();
    }

    public AODWaveformElectrodeOffsetFrequencyPeriod()
    {
        PlotDataSource.ToggleLegend(false);
        PlotDataSource.SetTitle("Result (Y: mW - X: MHz)");
    }

    private void RefreshPlot()
    {
        try
        {
            var scatterLines = PlotDataSource.GetOrAddScatterLines(Items.Count);

            foreach (var (index, item) in Items.Index())
            {
                scatterLines[index].Update(
                    $"{index + 1}",
                    [.. item.FrequencyItems.Select(t => new Point(t.Frequency, t.MeasurePower))], Constants.Turbo.GetColor(index, new Range(0, Items.Count - 1)));
            }
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }
}