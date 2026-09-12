using CommunityToolkit.Mvvm.ComponentModel;
using MathNet.Numerics.Statistics;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using Net.Utilities.ScottPlot.Interfaces;
using ScottPlot;
using System.ComponentModel;
using Range = ScottPlot.Range;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeDelay<TItem> : ObservableObject
    where TItem : AODWaveformElectrodeDelayItem, new()
{
    [ObservableProperty]
    public partial AODWaveformElectrodeDelayItem<TItem>[] Items { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource StabilityPlotDataSource { get; set; } = new PlotDataSource();

    [ObservableProperty]
    public partial int? StabilityStartIndex { get; set; }

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnStabilityStartIndexChanged(int? value) => RefreshStabilityPlot();

    partial void OnItemsChanged(AODWaveformElectrodeDelayItem<TItem>[] oldValue, AODWaveformElectrodeDelayItem<TItem>[] newValue)
    {
        foreach (var item in oldValue) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlot();
        RefreshStabilityPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RefreshPlot();
            RefreshStabilityPlot();
        }
    }

    // ReSharper restore UnusedParameterInPartialMethod

    public AODWaveformElectrodeDelay()
    {
        PlotDataSource.ToggleLegend(false);
        PlotDataSource.SetTitle("Result (Y: mW - X: MHz)");

        StabilityPlotDataSource.SetTitle("Repeat Score (Y: Score - X: Count)");
    }

    private void RefreshPlot()
    {
        try
        {
            var scatterLines = PlotDataSource.GetOrAddScatterLines(Items.Length);

            foreach (var (index, item) in Items.Index())
            {
                scatterLines[index].Update(
                    $"{index + 1}: {item.Score:0.###}",
                    [.. item.FrequencyItems.Select(t => new Point(t.Frequency, t.MeasurePower))], Constants.Turbo.GetColor(index, new Range(0, Items.Length - 1)));
            }
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    private void RefreshStabilityPlot()
    {
        try
        {
            double[] scores = StabilityStartIndex is not null
                ?
                [
                    .. Items
                        .Skip(StabilityStartIndex.Value)
                        .Select(t => t.Score)
                ]
                : [];

            var scatterLines = StabilityPlotDataSource.GetOrAddScatterLines(Items.Length > 0 ? 1 : 0);
            scatterLines.ElementAtOrDefault(0)?.Update("Score", [.. Items.Index().Select(t => new Point(t.Index + 1, t.Item.Score))], Colors.Blue);

            var yLines = StabilityPlotDataSource.GetOrAddYLines(scores.Length > 0 ? 1 : 0);
            yLines.ElementAtOrDefault(0)?.Update($"Stability std: {scores.StandardDeviation():0.######}", scores.Average(), Colors.DarkRed);

            var xLines = StabilityPlotDataSource.GetOrAddXLines(StabilityStartIndex is not null ? 1 : 0);
            xLines.ElementAtOrDefault(0)?.Update("Stability Start", (StabilityStartIndex ?? 0) + 1, Colors.Green);
        }
        finally
        {
            StabilityPlotDataSource.AutoScaleRefresh();
        }
    }
}