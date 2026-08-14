using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Humanizer;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using ScottPlot;
using System.ComponentModel;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using Net.Utilities.ScottPlot.Interfaces;
using Generate = MathNet.Numerics.Generate;
using Range = ScottPlot.Range;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequencyUniformity<TItem> : ObservableObject
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial OpticsAODElectrodeEnum[] Electrodes { get; set; } = [];

    public string Title => string.Join(", ", Electrodes.Select(t => t.Humanize()));

    [ObservableProperty]
    public partial AODWaveformElectrodeOffsetFrequencyUniformityItem<TItem>[] Items { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

    partial void OnItemsChanged(AODWaveformElectrodeOffsetFrequencyUniformityItem<TItem>[] oldValue, AODWaveformElectrodeOffsetFrequencyUniformityItem<TItem>[] newValue)
    {
        foreach (var item in oldValue) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlot();
    }

    public AODWaveformElectrodeOffsetFrequencyUniformity()
    {
        PlotDataSource.Configure(totalPlotCount: 3);
        PlotDataSource.SetTitle(0, "Uniformity Items(Y: mW - X: AMP)");
        PlotDataSource.SetTitle(1, "Uniformity Amplitude Result(Y: AMP - X: MHz)");
        PlotDataSource.SetTitle(2, "Uniformity Measure Power Result(Y: mW - X: MHz)");
    }

    private void RefreshPlot()
    {
        try
        {
            var isNeedRefreshes = Generate.Repeat(Items.Length, false);

            var scatterLines = PlotDataSource.GetOrAddScatterLines(0, Items.Length);

            foreach (var (index, item) in Items.Index())
            {
                scatterLines[index].Update(
                    $"{item.FrequencyItems[0].Frequency}(MHz)",
                    [.. item.FrequencyItems.Select(t => new Point(t.Amplitude, t.MeasurePower))],
                    Constants.Turbo.GetColor(index, new Range(0, Items.Length - 1)));

                if (item.FrequencyItems.Length <= 0) continue;

                item.MaxItem = item.FrequencyItems.Maxima(t => t.MeasurePower).First();
                isNeedRefreshes[index] = true;
            }

            scatterLines = PlotDataSource.GetOrAddScatterLines(1, isNeedRefreshes.All(b => b) ? 1 : 0);
            scatterLines.ElementAtOrDefault(0)?.Update(
                "Amplitude",
                [.. Items.Select(t => new Point(t.FrequencyItems[0].Frequency, Guard.IsNotNullAndReturn(t.MaxItem).Amplitude))],
                Colors.Blue);

            scatterLines = PlotDataSource.GetOrAddScatterLines(2, isNeedRefreshes.All(b => b) ? 1 : 0);
            scatterLines.ElementAtOrDefault(0)?.Update(
                "Measure Power",
                [.. Items.Select(t => new Point(t.FrequencyItems[0].Frequency, Guard.IsNotNullAndReturn(t.MaxItem).MeasurePower))],
                Colors.Blue);
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }
}