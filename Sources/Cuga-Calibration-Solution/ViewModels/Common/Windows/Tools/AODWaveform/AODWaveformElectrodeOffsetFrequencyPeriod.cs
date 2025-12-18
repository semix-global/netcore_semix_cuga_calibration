using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Utilities;
using Humanizer;
using Local.NoSQL.DB.Providers.Bases;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using System.ComponentModel;
using Range = ScottPlot.Range;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequencyPeriod<TItem> : ObservableCacheBase
    where TItem : AODWaveformElectrodeOffsetItem, new()
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private IReadOnlyList<OpticsAODElectrodeEnum> _electrodes = [];

    public string Title => string.Join(", ", Electrodes.Select(t => t.Humanize()));

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetFrequencyPeriodItem<TItem>> _items = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _closestMaximaPoints = [];

    [ObservableProperty]
    private double? _offsetFrequencyPeriodCoefficient;

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

#pragma warning restore CS0657
#pragma warning restore IDE0079

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

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnClosestMaximaPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnOffsetFrequencyPeriodCoefficientChanged(double? value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public AODWaveformElectrodeOffsetFrequencyPeriod()
    {
        ScatterPlotControl.ToggleLegend(false);
    }

    private void RefreshPlot()
    {
        ScatterPlotControl.SetTitle($"Result: {(OffsetFrequencyPeriodCoefficient is null ? "-" : $"{OffsetFrequencyPeriodCoefficient:0.###}(2pi)")} (Y: mW - X: 2pi)");

        foreach (var (index, item) in Items.Index())
        {
            if (item.FrequencyItems.Count <= 0) continue;

            var scatterMarkersOrigin = ScatterPlotControl.GetOrAddScatterMarkers(
                $"Origin {item.FrequencyItems[0].Frequency:0.###}(MHz)",
                [.. item.FrequencyItems.Select(t => new Point(t.OffsetFrequencyPeriodCoefficient, t.MeasurePower))],
                index,
                new Range(0, Items.Count - 1),
                markerShape: MarkerShape.OpenCircle);
            scatterMarkersOrigin.MarkerSize = 10;

            ScatterPlotControl.GetOrAddScatterLine(
                $"Interpolation {item.FrequencyItems[0].Frequency:0.###}(MHz)",
                item.FrequencyInterpolationPoints,
                index,
                new Range(0, Items.Count - 1));

            var scatterMarkersMaxima = ScatterPlotControl.GetOrAddScatterMarkers(
                $"Maxima {item.FrequencyItems[0].Frequency:0.###}(MHz)",
                item.FrequencyMaximaPoints,
                index,
                new Range(0, Items.Count - 1),
                markerShape: MarkerShape.Asterisk);
            scatterMarkersMaxima.MarkerSize = 20;
        }

        if (ClosestMaximaPoints.Count > 0)
        {
            var scatterMarkersClosestMaxima = ScatterPlotControl.GetOrAddScatterMarkers(
                "Closest Maxima",
                ClosestMaximaPoints,
                Colors.Blue,
                markerShape: MarkerShape.FilledSquare);
            scatterMarkersClosestMaxima.MarkerSize = 20;
        }

        if (OffsetFrequencyPeriodCoefficient is not null)
        {
            ScatterPlotControl.GetOrAddXLine("Result", OffsetFrequencyPeriodCoefficient.Value, Colors.DarkRed);
        }

        ScatterPlotControl.AutoScaleRefresh();
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

            var (frequencyMaximaX, frequencyMaximaY) = Extremumor.FindMaxima(
                Vector<double>.Build.Dense([.. item.FrequencyInterpolationPoints.Select(t => t.X)]),
                Vector<double>.Build.Dense([.. item.FrequencyInterpolationPoints.Select(t => t.Y)]));
            item.FrequencyMaximaPoints =
            [
                .. frequencyMaximaY
                    .Index()
                    .Where(t => t.Item > frequencyInterpolationY.Average())
                    .Select(t => new Point(frequencyMaximaX[t.Index], t.Item))
            ];
        }

        var (results, _) = Extremumor.FindClosestExtremum([.. Items.Select(t => Vector<double>.Build.DenseOfEnumerable(t.FrequencyMaximaPoints.Select(tt => tt.X)))]);

        foreach (var (index, (xIndex, xValue)) in results.Index())
        {
            ClosestMaximaPoints = [.. ClosestMaximaPoints, new Point(xValue, Items[index].FrequencyMaximaPoints[xIndex].Y)];
        }
    }
}