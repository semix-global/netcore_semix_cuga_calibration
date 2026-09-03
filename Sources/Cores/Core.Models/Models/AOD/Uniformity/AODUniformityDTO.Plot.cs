using CommunityToolkit.Mvvm.ComponentModel;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.Collections.Concurrent;
using Range = ScottPlot.Range;

namespace Core.Models.Models.AOD.Uniformity;

public partial class AODUniformityDTO
{
    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource IsReversePlotDataSource { get; set; } = new PlotDataSource();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource MappingPlotDataSource { get; set; } = new PlotDataSource();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource InitializeWindowPlotDataSource { get; set; } = new PlotDataSource();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial ConcurrentDictionary<int, IPlotDataSource> PlotDataSources { get; set; } = [];

    public AODUniformityDTO()
    {
        var customGridIsReversePlotDataSource = new CustomGrid();
        IsReversePlotDataSource.Configure(customGridIsReversePlotDataSource, 4,
            plots =>
            {
                customGridIsReversePlotDataSource.Set(plots[0], new GridCell(0, 0, 2, 2));
                customGridIsReversePlotDataSource.Set(plots[1], new GridCell(0, 1, 2, 2));
                customGridIsReversePlotDataSource.Set(plots[2], new GridCell(1, 0, 2, 2));
                customGridIsReversePlotDataSource.Set(plots[3], new GridCell(1, 1, 2, 2));
            });

        IsReversePlotDataSource.SetTitle(0, "Window(Y: Coefficient - X: sa)");
        IsReversePlotDataSource.SetTitle(1, "Horizontal Projects(Y: PMT Value(Log) - X: px)");
        IsReversePlotDataSource.SetTitle(2, "Mapping Window(Y: Coefficient - X: sa)");
        IsReversePlotDataSource.SetTitle(3, "Mapping Horizontal Projects(Y: PMT Value(Log) - X: px)");

        MappingPlotDataSource.SetTitle("Mapping (Y: Prescan Index - X: Prescan Index)");

        InitializeWindowPlotDataSource.Configure(new Columns(), 2);
        InitializeWindowPlotDataSource.SetTitle(0, "Horizontal Projects(Y: PMT Value(Log) - X: px)");
        InitializeWindowPlotDataSource.SetTitle(1, "Window(Y: Coefficient - X: sa)");

        var customGridPlotDataSource = new CustomGrid();
        PlotDataSource.Configure(customGridPlotDataSource, 3,
            plots =>
            {
                customGridPlotDataSource.Set(plots[0], new GridCell(0, 0, 2, 3, colSpan: 2));
                customGridPlotDataSource.Set(plots[1], new GridCell(1, 0, 2, 3, colSpan: 2));
                customGridPlotDataSource.Set(plots[2], new GridCell(0, 2, 2, 3, rowSpan: 2));
            });

        PlotDataSource.SetTitle(0, "Horizontal Projects(Y: PMT Value(Log) - X: px)");
        PlotDataSource.SetTitle(1, "Window(Y: Coefficient - X: px)");
        PlotDataSource.SetTitle(2, "Result Window(Y: Coefficient - X: sa)");
        PlotDataSource.ToggleLegend(1, false);
    }

    public AODUniformityDTO(IReadOnlyList<int> cibInformationChannelIds) : this()
    {
        PlotDataSources = new ConcurrentDictionary<int, IPlotDataSource>(cibInformationChannelIds.Select(t => new KeyValuePair<int, IPlotDataSource>(t, GetScatterPlotControl())));
    }

    private void RefreshIsReversePlot()
    {
        try
        {
            IsReversePlotDataSource.Clear(0);
            IsReversePlotDataSource.Clear(1);
            IsReversePlotDataSource.Clear(2);
            IsReversePlotDataSource.Clear(3);

            Refresh(StartWindowItem, 0, 1, "Start", Colors.Blue, Colors.DarkBlue);
            Refresh(StopWindowItem, 0, 1, "Stop", Colors.Red, Colors.DarkRed);
            Refresh(MappingWindowItem, 2, 3, "Mapping", Colors.Green, Colors.DarkGreen);
        }
        finally
        {
            IsReversePlotDataSource.AutoScaleRefresh();
        }

        return;

        void Refresh(WindowItem windowItem, int windowIndex, int imageIndex, string title, Color primaryColor, Color secondaryColor)
        {
            if (windowItem.Window.Count > 0)
                IsReversePlotDataSource.GetOrAddScatterLine(
                    windowIndex,
                    title,
                    windowItem.Window.ToPoints(),
                    primaryColor);

            if (windowItem.ImageHorizontalProjects.Count > 0)
                IsReversePlotDataSource.GetOrAddScatterLine(
                    imageIndex,
                    title,
                    windowItem.ImageHorizontalProjects.ToPoints(),
                    primaryColor);

            if (windowItem.SmoothImageHorizontalProjects.Count > 0)
                IsReversePlotDataSource.GetOrAddScatterLine(
                    imageIndex,
                    $"{title} Smooth",
                    windowItem.SmoothImageHorizontalProjects.ToPoints(),
                    secondaryColor);

            if (windowItem.HorizontalProjectMinPixels.Count > 0)
            {
                foreach (var (index, horizontalProjectMinPixel) in windowItem.HorizontalProjectMinPixels.Index())
                {
                    IsReversePlotDataSource.GetOrAddXLine(
                        imageIndex,
                        $"{title} Smooth Min Pixel: {index + 1}",
                        horizontalProjectMinPixel,
                        secondaryColor);
                }
            }
            else
            {
                if (windowItem.SmoothImageHorizontalProjects.Count > 0)
                    IsReversePlotDataSource.GetOrAddXLine(
                        imageIndex,
                        $"{title} Smooth Min Pixel",
                        windowItem.HorizontalProjectMinPixel,
                        secondaryColor);
            }
        }
    }

    private void RefreshMappingPlot()
    {
        try
        {
            MappingPlotDataSource.Clear();

            if (Mappings.Count <= 0) return;

            var isNotLinearSplineImageHorizontalProjectIndexes = Mappings.Where(t => t.IsNotLinearSpline).Select(t => t.ImageHorizontalProjectIndex).ToArray();
            var isNotLinearSplineMappingIndexes = Mappings.Where(t => t.IsNotLinearSpline).Select(t => t.MappingIndex).ToArray();
            var (slope, intercept, rSquared, yPredicted) = PolynomialCurve.Fit1(
                Vector<double>.Build.Dense([.. isNotLinearSplineImageHorizontalProjectIndexes]),
                Vector<double>.Build.Dense([.. isNotLinearSplineMappingIndexes]));

            var scatterLine = MappingPlotDataSource.GetOrAddScatterLine("Origin", [.. isNotLinearSplineImageHorizontalProjectIndexes.Index().Select(t => new Point(t.Item, isNotLinearSplineMappingIndexes[t.Index]))]);
            scatterLine.MarkerSize = 10;

            scatterLine = MappingPlotDataSource.GetOrAddScatterLine($"Fit Curve: y = {slope:0.######}x + {intercept:0.######} r^2 = {rSquared:0.######}",
                [.. isNotLinearSplineImageHorizontalProjectIndexes.Index().Select(t => new Point(t.Item, yPredicted[t.Index]))]);
            scatterLine.MarkerSize = 10;

            var isLinearSplineImageHorizontalProjectIndexes = Mappings.Where(t => t.IsNotLinearSpline == false).Select(t => t.ImageHorizontalProjectIndex).ToArray();
            var isLinearSplineLinearSplineMappingIndexes = Mappings.Where(t => t.IsNotLinearSpline == false).Select(t => t.LinearSplineMappingIndex).ToArray();
            var isLinearSplineMappingIndexes = Mappings.Where(t => t.IsNotLinearSpline == false).Select(t => t.MappingIndex).ToArray();

            var scatterMarkers = MappingPlotDataSource.GetOrAddScatterMarkers("Linear Spline", [.. isLinearSplineImageHorizontalProjectIndexes.Index().Select(t => new Point(t.Item, isLinearSplineLinearSplineMappingIndexes[t.Index]))], Colors.DarkRed,
                MarkerShape.FilledSquare);
            scatterMarkers.MarkerSize = 5;
            scatterMarkers = MappingPlotDataSource.GetOrAddScatterMarkers("Round Linear Spline", [.. isLinearSplineImageHorizontalProjectIndexes.Index().Select(t => new Point(t.Item, isLinearSplineMappingIndexes[t.Index]))], Colors.Red,
                MarkerShape.FilledDiamond);
            scatterMarkers.MarkerSize = 5;
        }
        finally
        {
            MappingPlotDataSource.AutoScaleRefresh();
        }
    }

    private void RefreshInitializeWindowPlot()
    {
        try
        {
            InitializeWindowPlotDataSource.Clear(0);
            InitializeWindowPlotDataSource.Clear(1);

            foreach (var (index, itemItemData) in InitializeWindowItem.Items.Index())
            {
                InitializeWindowPlotDataSource.GetOrAddScatterLine(
                    0,
                    $"{itemItemData.Window[0]:0.###}",
                    itemItemData.ImageHorizontalProjects.ToPoints(),
                    index,
                    new Range(0, InitializeWindowItem.Items.Count - 1));
            }

            if (InitializeWindowItem.Window.Count > 0)
                InitializeWindowPlotDataSource.GetOrAddScatterLine(
                    1,
                    "Window",
                    InitializeWindowItem.Window.ToPoints(),
                    Colors.Green);
        }
        finally
        {
            InitializeWindowPlotDataSource.AutoScaleRefresh();
        }
    }

    private void RefreshPlot()
    {
        try
        {
            var scatterLines0 = PlotDataSource.GetOrAddScatterLines(0, Item.Items.Count);
            var scatterLines1 = PlotDataSource.GetOrAddScatterLines(1, Item.Items.Count);
            var yLines = PlotDataSource.GetOrAddYLines(1, 2);

            foreach (var (i, itemItemData) in Item.Items.Index())
            {
                scatterLines0[i].Update(
                    $"{i + 1}: {Item.CIBInformation} Error: [{itemItemData.MinRate:0.###}, {itemItemData.MaxRate:0.###}]",
                    itemItemData.ImageHorizontalProjects.ToPoints(),
                    Constants.Turbo.GetColor(i, new Range(0, Item.Items.Count - 1)));

                var window = Mappings
                    .OrderBy(t => t.ImageHorizontalProjectIndex)
                    .Select(t => Vector<double>.Build.Dense([.. itemItemData.Window]).SubVectorIndexes([.. t.MappingIndices]).Distinct().Single())
                    .ToArray();

                scatterLines1[i].Update(
                    $"{i + 1}: {Item.CIBInformation}",
                    window.ToPoints(),
                    Constants.Turbo.GetColor(i, new Range(0, Item.Items.Count - 1)));
            }

            yLines[0].Update(string.Empty, Item.WindowLimitMin, Colors.DarkRed);
            yLines[0].LineWidth = 5;
            yLines[0].LinePattern = LinePattern.Solid;
            yLines[1].Update(string.Empty, Item.WindowLimitMax, Colors.DarkRed);
            yLines[1].LineWidth = 5;
            yLines[1].LinePattern = LinePattern.Solid;

            PlotDataSource.GetOrAddYLines(0, 1)[0].Update(
                TargetPMTValues.TryGetSingle(t => t.Key == Item.CIBInformation, out var targetPMTValueKvp)
                    ? "Target"
                    : string.Empty,
                targetPMTValueKvp.Value,
                Colors.Red);

            var scatterLine = PlotDataSource.GetOrAddScatterLines(2, 1)[0];
            scatterLine.Update(
                Item.Window.Count > 0
                    ? "Target"
                    : string.Empty,
                Item.Window.ToPoints(),
                Colors.Red);
            scatterLine.LinePattern = LinePattern.Dotted;
            scatterLine.LineWidth = 5;
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    private void RefreshPlots()
    {
        var results = (
            from itemItem in Items
            group itemItem by itemItem.CIBInformation.ChannelId
            into g
            orderby g.Key
            select (
                ChannelId: g.Key,
                ItemItems: g.OrderBy(t => t.CIBInformation.PMTId).ToArray()
            )).ToArray();

        foreach (var (channelId, itemItems) in results)
        {
            var plotDataSource = PlotDataSources.GetOrAdd(channelId, _ => GetScatterPlotControl());

            try
            {
                if (itemItems.Any(t => t.Items.Count <= 0))
                {
                    plotDataSource.Clear();

                    continue;
                }

                var itemItemsData = itemItems
                    .Select(t => (t.CIBInformation.PMTId, Item: t.Items[^1]))
                    .ToArray();
                var minPMTId = itemItemsData.Min(t => t.PMTId);
                var maxPMTId = itemItemsData.Max(t => t.PMTId);

                plotDataSource.Clear();
                foreach (var (pmtId, itemItemData) in itemItemsData)
                {
                    plotDataSource.GetOrAddScatterLine(
                        $"{pmtId} Error: [{itemItemData.MinRate:0.###}, {itemItemData.MaxRate:0.###}]",
                        itemItemData.ImageHorizontalProjects.ToPoints(),
                        pmtId,
                        new Range(minPMTId, maxPMTId));
                }
            }
            finally
            {
                plotDataSource.AutoScaleRefresh();
            }
        }
    }

    private static IPlotDataSource GetScatterPlotControl()
    {
        var plotDataSource = new PlotDataSource();

        plotDataSource.SetTitle("Horizontal Projects(Y: PMT Value(Log) - X: px)");
        plotDataSource.ToggleInvisibleLegendItem(false);

        return plotDataSource;
    }
}