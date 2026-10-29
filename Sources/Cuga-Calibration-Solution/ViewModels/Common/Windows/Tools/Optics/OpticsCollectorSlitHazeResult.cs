using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.ComponentModel;
using PlotConstants = Net.Utilities.ScottPlot.Helper.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Optics;

public sealed partial class OpticsCollectorSlitSample : ObservableObject
{
    [ObservableProperty]
    public partial double ECS { get; set; }

    [ObservableProperty]
    public partial int ChannelId { get; set; }

    [ObservableProperty]
    public partial string ImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RawImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int[] Ny { get; set; } = [];

    [ObservableProperty]
    public partial double TopAverageGray { get; set; }

    [ObservableProperty]
    public partial double MiddleAverageGray { get; set; }

    [ObservableProperty]
    public partial double BottomAverageGray { get; set; }

    [ObservableProperty]
    public partial double StandardDeviation { get; set; }

    public object ToHtmlAnonymous() => new
    {
        ECS,
        ChannelId,
        NY = Ny,
        TopAverageGray,
        MiddleAverageGray,
        BottomAverageGray,
        StandardDeviation,
        RawImageFilePath
    };
}

public sealed partial class OpticsCollectorSlitChannelResult : ObservableObject
{
    [ObservableProperty]
    public partial int ChannelId { get; set; }

    [ObservableProperty]
    public partial double[] TraceBufferX { get; set; } = [];

    [ObservableProperty]
    public partial double[] TraceBufferECS { get; set; } = [];

    [ObservableProperty]
    public partial int? StartIndex { get; set; }

    [ObservableProperty]
    public partial int? EndIndex { get; set; }

    [ObservableProperty]
    public partial bool HasBestFocus { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<OpticsCollectorSlitSample> Samples { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial OpticsCollectorSlitSample SelectedItem { get; set; } = new();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

    [ObservableProperty]
    public partial Point CollectionBestFocusPoint { get; set; }

    [ObservableProperty]
    public partial double PeakRegionLeftEcs { get; set; }

    [ObservableProperty]
    public partial double PeakRegionRightEcs { get; set; }

    [ObservableProperty]
    public partial double PeakRegionWidth { get; set; }

    [ObservableProperty]
    public partial bool PeakRegionReachedEndpoint { get; set; }

    [ObservableProperty]
    public partial Point IlluminationBestFocusPoint { get; set; }

    [ObservableProperty]
    public partial double Offset { get; set; }

    public object ToHtmlAnonymous() => new
    {
        ChannelId,
        PeakRegionLeftEcs,
        PeakRegionRightEcs,
        PeakRegionWidth,
        PeakRegionReachedEndpoint,
        CollectionBestFocusPoint,
        IlluminationBestFocusPoint,
        Offset
    };

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnSamplesChanged(IReadOnlyList<OpticsCollectorSlitSample> oldValue, IReadOnlyList<OpticsCollectorSlitSample> newValue)
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

    partial void OnHasBestFocusChanged(bool value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    public OpticsCollectorSlitChannelResult() => ConfigurePlots(1);

    private void ConfigurePlots(int plotCount)
    {
        PlotDataSource.Configure(new Rows(), plotCount);
        PlotDataSource.SetTitle(0, "Top Mid Bottom GrayPlot(Y:Mean / Std - X: ECS)");
        PlotDataSource.Plot.Axes.Left.Label.Text = "Gray Mean";
        PlotDataSource.Plot.Axes.Right.Label.Text = "Std";
        if (plotCount > 1)
            PlotDataSource.SetTitle(1, "TraceBuffer(Y:ECS / X - X: Index)");
    }

    private void RefreshPlot()
    {
        try
        {
            var samples = Samples.OrderBy(item => item.ECS).ToArray();
            var hasSamples = samples.Length > 0;
            var hasTrace = hasSamples && StartIndex is not null && EndIndex is not null;
            var hasBestFocus = hasSamples && HasBestFocus;
            var plotCount = hasTrace ? 2 : 1;

            if (PlotDataSource.GetPlotCount() != plotCount)
                ConfigurePlots(plotCount);

            var scatterLines = PlotDataSource.GetOrAddScatterLines(hasSamples ? 4 : 0);
            var scatterMarkers = PlotDataSource.GetOrAddScatterMarkerses(hasBestFocus ? 2 : 0);
            var xLines = PlotDataSource.GetOrAddXLines(hasBestFocus ? 2 : 0);

            scatterLines.ElementAtOrDefault(0)?.Update(
                "Top",
                [.. samples.Select(t => new Point(t.ECS, t.TopAverageGray))],
                PlotConstants.Category10.GetColor(0));
            scatterLines.ElementAtOrDefault(1)?.Update(
                "Mid",
                [.. samples.Select(t => new Point(t.ECS, t.MiddleAverageGray))],
                PlotConstants.Category10.GetColor(1));
            scatterLines.ElementAtOrDefault(2)?.Update(
                "Bottom",
                [.. samples.Select(t => new Point(t.ECS, t.BottomAverageGray))],
                PlotConstants.Category10.GetColor(2));

            if (scatterLines.ElementAtOrDefault(3) is { } stdLine)
            {
                stdLine.Update(
                    "Std",
                    [.. samples.Select(t => new Point(t.ECS, t.StandardDeviation))],
                    PlotConstants.Category10.GetColor(3));
                stdLine.Axes.YAxis = PlotDataSource.Plot.Axes.Right;
            }

            if (hasTrace)
            {
                var traceScatterLines = PlotDataSource.GetOrAddScatterLines(1, 2);
                var traceXLines = PlotDataSource.GetOrAddXLines(1, 2);

                traceScatterLines.ElementAtOrDefault(0)?.Update(
                    "ECS",
                    [.. TraceBufferECS.Index().Select(t => new Point(t.Index, t.Item))],
                    PlotConstants.Category10.GetColor(3));
                traceScatterLines.ElementAtOrDefault(1)?.Update(
                    "X",
                    [.. TraceBufferX.Index().Select(t => new Point(t.Index, t.Item))],
                    PlotConstants.Category10.GetColor(4));
                traceXLines.ElementAtOrDefault(0)?.Update("Start", StartIndex.GetValueOrDefault(), Colors.Red);
                traceXLines.ElementAtOrDefault(1)?.Update("End", EndIndex.GetValueOrDefault(), Colors.Red);
            }

            scatterMarkers.ElementAtOrDefault(0)?.Update("Collection BF", [CollectionBestFocusPoint], PlotConstants.Category10.GetColor(4));
            xLines.ElementAtOrDefault(0)?.Update(PeakRegionReachedEndpoint ? "95% left (ReachedEnd)" : "95% left", PeakRegionLeftEcs, Colors.Red);
            xLines.ElementAtOrDefault(1)?.Update(PeakRegionReachedEndpoint ? "95% right (ReachedEnd)" : "95% right", PeakRegionRightEcs, Colors.Red);

            if (scatterMarkers.ElementAtOrDefault(1) is { } illuminationMarkers)
            {
                illuminationMarkers.Update("Illumination BF", [IlluminationBestFocusPoint], PlotConstants.Category10.GetColor(4));
                illuminationMarkers.Axes.YAxis = PlotDataSource.Plot.Axes.Right;
            }
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }
}