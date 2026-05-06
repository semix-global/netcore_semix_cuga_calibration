using CommunityToolkit.Mvvm.ComponentModel;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Helper;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.Collections.Concurrent;
using Range = ScottPlot.Range;

namespace Core.Models.Models.AOD.Uniformity;

public partial class AODUniformityDTO
{
    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public partial IScatterPlotControl IsReverseScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public partial IScatterPlotControl MappingScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public partial IScatterPlotControl InitializeWindowScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public partial IScatterPlotControl ScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public partial ConcurrentDictionary<int, IScatterPlotControl> ScatterPlotControls { get; set; } = [];

    public AODUniformityDTO()
    {
        var customGridIsReverseScatterPlotControl = new CustomGrid();
        IsReverseScatterPlotControl.Configure(customGridIsReverseScatterPlotControl, 5,
            plots =>
            {
                customGridIsReverseScatterPlotControl.Set(plots[0], new GridCell(0, 0, 2, 2));
                customGridIsReverseScatterPlotControl.Set(plots[1], new GridCell(0, 1, 2, 2));
                customGridIsReverseScatterPlotControl.Set(plots[2], new GridCell(1, 0, 2, 2));
                customGridIsReverseScatterPlotControl.Set(plots[3], new GridCell(1, 1, 2, 2));
            });

        IsReverseScatterPlotControl.SetTitle(0, "Window(Y: Coefficient - X: sa)");
        IsReverseScatterPlotControl.SetTitle(1, "Horizontal Projects(Y: PMT Value(Log) - X: px)");
        IsReverseScatterPlotControl.SetTitle(2, "Mapping Window(Y: Coefficient - X: sa)");
        IsReverseScatterPlotControl.SetTitle(3, "Mapping Horizontal Projects(Y: PMT Value(Log) - X: px)");

        MappingScatterPlotControl.SetTitle("Mapping (Y: Prescan Index - X: Prescan Index)");

        InitializeWindowScatterPlotControl.Configure(new Columns(), 2);
        InitializeWindowScatterPlotControl.SetTitle(0, "Horizontal Projects(Y: PMT Value(Log) - X: px)");
        InitializeWindowScatterPlotControl.SetTitle(1, "Window(Y: Coefficient - X: sa)");

        var customGridScatterPlotControl = new CustomGrid();
        ScatterPlotControl.Configure(customGridScatterPlotControl, 3,
            plots =>
            {
                customGridScatterPlotControl.Set(plots[0], new GridCell(0, 0, 2, 3, colSpan: 2));
                customGridScatterPlotControl.Set(plots[1], new GridCell(1, 0, 2, 3, colSpan: 2));
                customGridScatterPlotControl.Set(plots[2], new GridCell(0, 2, 2, 3, rowSpan: 2));
            });

        ScatterPlotControl.SetTitle(0, "Horizontal Projects(Y: PMT Value(Log) - X: px)");
        ScatterPlotControl.SetTitle(1, "Window(Y: Coefficient - X: px)");
        ScatterPlotControl.SetTitle(2, "Result Window(Y: Coefficient - X: sa)");
        ScatterPlotControl.ToggleLegend(1, false);
    }

    public AODUniformityDTO(IReadOnlyList<int> cibInformationChannelIds) : this()
    {
        ScatterPlotControls = new ConcurrentDictionary<int, IScatterPlotControl>(cibInformationChannelIds.Select(t => new KeyValuePair<int, IScatterPlotControl>(t, GetScatterPlotControl())));
    }

    private void RefreshIsReversePlot()
    {
        try
        {
            IsReverseScatterPlotControl.Clear(0);
            IsReverseScatterPlotControl.Clear(1);
            IsReverseScatterPlotControl.Clear(2);
            IsReverseScatterPlotControl.Clear(3);

            Refresh(StartWindowItem, 0, 1, "Start", Colors.Blue, Colors.DarkBlue);
            Refresh(StopWindowItem, 0, 1, "Stop", Colors.Red, Colors.DarkRed);
            Refresh(MappingWindowItem, 2, 3, "Mapping", Colors.Green, Colors.DarkGreen);
        }
        finally
        {
            IsReverseScatterPlotControl.AutoScaleRefresh();
        }

        return;

        void Refresh(WindowItem windowItem, int windowIndex, int imageIndex, string title, Color primaryColor, Color secondaryColor)
        {
            if (windowItem.Window.Count > 0)
                IsReverseScatterPlotControl.GetOrAddScatterLine(
                    windowIndex,
                    title,
                    windowItem.Window.ToPoints(),
                    primaryColor);

            if (windowItem.ImageHorizontalProjects.Count > 0)
                IsReverseScatterPlotControl.GetOrAddScatterLine(
                    imageIndex,
                    title,
                    windowItem.ImageHorizontalProjects.ToPoints(),
                    primaryColor);

            if (windowItem.SmoothImageHorizontalProjects.Count > 0)
                IsReverseScatterPlotControl.GetOrAddScatterLine(
                    imageIndex,
                    $"{title} Smooth",
                    windowItem.SmoothImageHorizontalProjects.ToPoints(),
                    secondaryColor);

            if (windowItem.HorizontalProjectMinPixels.Count > 0)
            {
                foreach (var (index, horizontalProjectMinPixel) in windowItem.HorizontalProjectMinPixels.Index())
                {
                    IsReverseScatterPlotControl.GetOrAddXLine(
                        imageIndex,
                        $"{title} Smooth Min Pixel: {index + 1}",
                        horizontalProjectMinPixel,
                        secondaryColor);
                }
            }
            else
            {
                if (windowItem.SmoothImageHorizontalProjects.Count > 0)
                    IsReverseScatterPlotControl.GetOrAddXLine(
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
            MappingScatterPlotControl.Clear();

            if (Mappings.Count <= 0) return;

            var isNotLinearSplineImageHorizontalProjectIndexes = Mappings.Where(t => t.IsNotLinearSpline).Select(t => t.ImageHorizontalProjectIndex).ToArray();
            var isNotLinearSplineMappingIndexes = Mappings.Where(t => t.IsNotLinearSpline).Select(t => t.MappingIndex).ToArray();
            var (slope, intercept, rSquared, yPredicted) = PolynomialCurve.Fit1(
                Vector<double>.Build.Dense([.. isNotLinearSplineImageHorizontalProjectIndexes]),
                Vector<double>.Build.Dense([.. isNotLinearSplineMappingIndexes]));

            var scatterLine = MappingScatterPlotControl.GetOrAddScatterLine("Origin", [.. isNotLinearSplineImageHorizontalProjectIndexes.Index().Select(t => new Point(t.Item, isNotLinearSplineMappingIndexes[t.Index]))]);
            scatterLine.MarkerSize = 10;

            scatterLine = MappingScatterPlotControl.GetOrAddScatterLine($"Fit Curve: y = {slope:0.######}x + {intercept:0.######} r^2 = {rSquared:0.######}",
                [.. isNotLinearSplineImageHorizontalProjectIndexes.Index().Select(t => new Point(t.Item, yPredicted[t.Index]))]);
            scatterLine.MarkerSize = 10;

            var isLinearSplineImageHorizontalProjectIndexes = Mappings.Where(t => t.IsNotLinearSpline == false).Select(t => t.ImageHorizontalProjectIndex).ToArray();
            var isLinearSplineLinearSplineMappingIndexes = Mappings.Where(t => t.IsNotLinearSpline == false).Select(t => t.LinearSplineMappingIndex).ToArray();
            var isLinearSplineMappingIndexes = Mappings.Where(t => t.IsNotLinearSpline == false).Select(t => t.MappingIndex).ToArray();

            var scatterMarkers = MappingScatterPlotControl.GetOrAddScatterMarkers("Linear Spline", [.. isLinearSplineImageHorizontalProjectIndexes.Index().Select(t => new Point(t.Item, isLinearSplineLinearSplineMappingIndexes[t.Index]))], Colors.DarkRed,
                MarkerShape.FilledSquare);
            scatterMarkers.MarkerSize = 5;
            scatterMarkers = MappingScatterPlotControl.GetOrAddScatterMarkers("Round Linear Spline", [.. isLinearSplineImageHorizontalProjectIndexes.Index().Select(t => new Point(t.Item, isLinearSplineMappingIndexes[t.Index]))], Colors.Red,
                MarkerShape.FilledDiamond);
            scatterMarkers.MarkerSize = 5;
        }
        finally
        {
            MappingScatterPlotControl.AutoScaleRefresh();
        }
    }

    private void RefreshInitializeWindowPlot()
    {
        try
        {
            InitializeWindowScatterPlotControl.Clear(0);
            InitializeWindowScatterPlotControl.Clear(1);

            foreach (var (index, itemItemData) in InitializeWindowItem.Items.Index())
            {
                InitializeWindowScatterPlotControl.GetOrAddScatterLine(
                    0,
                    $"{itemItemData.Window[0]:0.###}",
                    itemItemData.ImageHorizontalProjects.ToPoints(),
                    index,
                    new Range(0, InitializeWindowItem.Items.Count - 1));
            }

            if (InitializeWindowItem.Window.Count > 0)
                InitializeWindowScatterPlotControl.GetOrAddScatterLine(
                    1,
                    "Window",
                    InitializeWindowItem.Window.ToPoints(),
                    Colors.Green);
        }
        finally
        {
            InitializeWindowScatterPlotControl.AutoScaleRefresh();
        }
    }

    private void RefreshPlot()
    {
        try
        {
            var scatterLines0 = ScatterPlotControl.GetOrAddScatterLines(0, Item.Items.Count);
            var scatterLines1 = ScatterPlotControl.GetOrAddScatterLines(1, Item.Items.Count);
            var yLines = ScatterPlotControl.GetOrAddYLines(1, 2);

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

            ScatterPlotControl.GetOrAddYLines(0, 1)[0].Update(
                TargetPMTValues.TryGetSingle(t => t.Key == Item.CIBInformation, out var targetPMTValueKvp)
                    ? "Target"
                    : string.Empty,
                targetPMTValueKvp.Value,
                Colors.Red);

            var scatterLine = ScatterPlotControl.GetOrAddScatterLines(2, 1)[0];
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
            ScatterPlotControl.AutoScaleRefresh();
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
            var scatterPlotControl = ScatterPlotControls.GetOrAdd(channelId, _ => GetScatterPlotControl());

            try
            {
                if (itemItems.Any(t => t.Items.Count <= 0))
                {
                    scatterPlotControl.Clear();

                    continue;
                }

                var itemItemsData = itemItems
                    .Select(t => (t.CIBInformation.PMTId, Item: t.Items[^1]))
                    .ToArray();
                var minPMTId = itemItemsData.Min(t => t.PMTId);
                var maxPMTId = itemItemsData.Max(t => t.PMTId);

                scatterPlotControl.Clear();
                foreach (var (pmtId, itemItemData) in itemItemsData)
                {
                    scatterPlotControl.GetOrAddScatterLine(
                        $"{pmtId} Error: [{itemItemData.MinRate:0.###}, {itemItemData.MaxRate:0.###}]",
                        itemItemData.ImageHorizontalProjects.ToPoints(),
                        pmtId,
                        new Range(minPMTId, maxPMTId));
                }
            }
            finally
            {
                scatterPlotControl.AutoScaleRefresh();
            }
        }
    }

    private static IScatterPlotControl GetScatterPlotControl()
    {
        var scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

        scatterPlotControl.SetTitle("Horizontal Projects(Y: PMT Value(Log) - X: px)");
        scatterPlotControl.ToggleInvisibleLegendItem(false);

        return scatterPlotControl;
    }
}