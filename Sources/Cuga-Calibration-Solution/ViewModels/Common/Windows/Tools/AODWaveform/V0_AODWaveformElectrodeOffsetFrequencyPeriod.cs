using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Humanizer;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Models.Geometries;
using ScottPlot;
using System.ComponentModel;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;
using Range = ScottPlot.Range;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class V0AODWaveformElectrodeOffsetFrequencyPeriod<TItem> : ObservableObject
    where TItem : V0AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial IReadOnlyList<OpticsAODElectrodeEnum> Electrodes { get; set; } = [];

    public string Title => string.Join(", ", Electrodes.Select(t => t.Humanize()));

    [ObservableProperty]
    public partial IReadOnlyList<V0AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem>> Items { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Point> ClosestMaximaPoints { get; set; } = [];

    [ObservableProperty]
    public partial double? OffsetFrequencyPeriodCoefficient { get; set; }

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    partial void OnItemsChanged(IReadOnlyList<V0AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem>>? oldValue, IReadOnlyList<V0AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem>> newValue)
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

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnClosestMaximaPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnOffsetFrequencyPeriodCoefficientChanged(double? value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public V0AODWaveformElectrodeOffsetFrequencyPeriod()
    {
        PlotDataSource.ToggleLegend(false);
    }

    private void RefreshPlot()
    {
        try
        {
            PlotDataSource.SetTitle($"Result: {(OffsetFrequencyPeriodCoefficient is null ? "-" : $"{OffsetFrequencyPeriodCoefficient:0.###}(2pi)")} (Y: mW - X: 2pi)");

            foreach (var (index, item) in Items.Index())
            {
                if (item.FrequencyItems.Count <= 0) continue;

                var scatterMarkersOrigin = PlotDataSource.GetOrAddScatterMarkers(
                    $"Origin {item.FrequencyItems[0].Frequency:0.###}(MHz)",
                    [.. item.FrequencyItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))],
                    index,
                    new Range(0, Items.Count - 1),
                    MarkerShape.OpenCircle);
                scatterMarkersOrigin.MarkerSize = 10;

                PlotDataSource.GetOrAddScatterLine(
                    $"Interpolation {item.FrequencyItems[0].Frequency:0.###}(MHz)",
                    item.FrequencyInterpolationPoints,
                    index,
                    new Range(0, Items.Count - 1));

                var scatterMarkersMaxima = PlotDataSource.GetOrAddScatterMarkers(
                    $"Maxima {item.FrequencyItems[0].Frequency:0.###}(MHz)",
                    item.FrequencyMaximaPoints,
                    index,
                    new Range(0, Items.Count - 1),
                    MarkerShape.Asterisk);
                scatterMarkersMaxima.MarkerSize = 20;
            }

            if (ClosestMaximaPoints.Count > 0)
            {
                var scatterMarkersClosestMaxima = PlotDataSource.GetOrAddScatterMarkers(
                    "Closest Maxima",
                    ClosestMaximaPoints,
                    Colors.Blue,
                    MarkerShape.FilledSquare);
                scatterMarkersClosestMaxima.MarkerSize = 20;
            }

            if (OffsetFrequencyPeriodCoefficient is not null)
            {
                PlotDataSource.GetOrAddXLine("Result", OffsetFrequencyPeriodCoefficient.Value, Colors.DarkRed);
            }
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    public void InterpolationMaxima(int densityFactor)
    {
        ClosestMaximaPoints = [];

        foreach (var item in Items)
        {
            item.FrequencyInterpolationPoints = [];
            item.FrequencyMaximaPoints = [];

            var (frequencyInterpolationX, frequencyInterpolationY) = Interpolator.SplineInterpolation(
                Vector<double>.Build.Dense([.. item.FrequencyItems.Select(t => t.OffsetFrequencyPeriodCoefficient)]),
                Vector<double>.Build.Dense([.. item.FrequencyItems.Select(t => t.MeasurePower)]),
                densityFactor);
            item.FrequencyInterpolationPoints = [.. frequencyInterpolationX.Index().Select(t => new Point(t.Item, frequencyInterpolationY[t.Index]))];

            var (_, frequencyMaxima) = Extremumor.FindMaxima(item.FrequencyInterpolationPoints);
            item.FrequencyMaximaPoints =
            [
                .. frequencyMaxima
                    .Where(t => t.Y > frequencyInterpolationY.Average())
                    .Select(t => t)
            ];
        }

        var (results, _) = Extremumor.FindClosestExtremum([.. Items.Select(t => Vector<double>.Build.DenseOfEnumerable(t.FrequencyMaximaPoints.Select(tt => tt.X)))]);

        foreach (var (index, (xIndex, xValue)) in results.Index())
        {
            ClosestMaximaPoints = [.. ClosestMaximaPoints, new Point(xValue, Items[index].FrequencyMaximaPoints[xIndex].Y)];
        }
    }
}