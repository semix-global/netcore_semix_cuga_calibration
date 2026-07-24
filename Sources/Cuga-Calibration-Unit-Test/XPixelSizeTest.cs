// #define XPixelSizeTest

using AwesomeAssertions;
using MiniExcelLibs;
using Net.Utilities.Algorithms.Modules;
using Xunit.Abstractions;
using Point = Net.Utilities.Models.Geometries.Point;

#if XPixelSizeTest
using System.Windows;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.WPF.V2;
using Net.Utilities.Helpers.Extensions;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
#endif

namespace CugaCalibrationUnitTest;

public class XPixelSizeTest(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("test1.xlsx", 0.7, 4, new[] { 24947.007, 24943.969, 24941.919 })]
    [InlineData("test2.xlsx", 0.7, 16, new[] { 74706.354, 74706.115, 74708.599, 74707.325, 74705.558, 74707.925, 74706.92, 74707.655, 74707.707, 74706.902, 74705.874, 74706.493, 74706.946, 74707.062, 74706.141 })]
    [InlineData("test3.xlsx", 0.7, 15, new[] { 74944.447, 74947.406, 74944.655, 74943.691, 74946.119, 74946.759, 74946.367, 74944.355, 74946.67, 74943.411, 74947.489, 74943.737, 74947.8, 74944.207 })]
    [InlineData("test4.xlsx", 0.7, 17, new[] { 49964.727, 49964.088, 49963.032, 49962.883, 49964.177, 49963.214, 49964.824, 49962.904, 49963.303, 49963.897, 49963.932, 49964.166, 49963.731, 49963.272, 49964.164, 49963.486 })]
    [InlineData("test5.xlsx", 0.7, 14, new[] { 50325.744, 50325.486, 50326.672, 50325.033, 50327.846, 50328.08, 50327.871, 50328.456, 50329.57, 50329.011, 50328.965, 50330.811 })]
    [InlineData("test5.xlsx", 0.6, 14, new[] { 50325.744, 50325.486, 50326.672, 50325.033, 50327.846, 50327.871, 50328.456, 50329.57, 50329.011, 50328.965, 50330.811 })]
    public void Test(string filePath, double threshold, int count, IReadOnlyList<double> expectedXDifferences)
    {
        var points = MiniExcel.Query<Temp>(@$"Assets\XPixelSize\{filePath}", sheetName: "ALL Points")
            .Select(t => new Point(t.X, t.Y))
            .GroupBy(t => t.X)
            .Select(g => g.MaxBy(t => t.Y))
            .OrderBy(t => t.X)
            .ToArray();

        var (indexes, _) = Extremumor.FindMaxima(points);

        var filterIndexes = indexes.Where(t => points[t].Y >= threshold).ToArray();
        filterIndexes = Filter.NMS([.. filterIndexes.Select(t => points[t])], 402).Indexes.Select(t => filterIndexes[t]).ToArray();

        filterIndexes = filterIndexes
            .Select(t => (Index: t, points[t].Y))
            .OrderByDescending(t => t.Y)
            .Take(count)
            .OrderBy(t => points[t.Index].X)
            .Select(t => t.Index).ToArray();

        var matchPoints = filterIndexes.Select(t => points[t]).ToArray();
        var xDifferences = matchPoints
            .Zip(matchPoints.Skip(1), (prev, next) => next.X - prev.X)
            .ToArray();
        var filterXDifferences = Filter.MAD(xDifferences).Results;

        testOutputHelper.WriteLine(string.Join(", ", filterXDifferences.Select(t => t.ToString("0.###"))));

        filterXDifferences.Should()
            .BeEquivalentTo(expectedXDifferences, options => options
                .WithStrictOrdering()
                .Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, 1e-3))
                .WhenTypeIs<double>());

#if XPixelSizeTest
        var thread = new Thread(() =>
        {
            var window = new Window { Title = $"{nameof(XPixelSizeTest)}_{threshold:0.###}" };

            var plotControl = new PlotControl();
            window.Content = plotControl;

            var plotDataSource = new PlotDataSource();
            plotDataSource.Configure(new Rows(), 3);
            plotControl.DataSource = plotDataSource;

            #region Plot0

            plotDataSource.SetTitle(0, "Template Match: px/score");

            plotDataSource.GetOrAddScatterLine(
                0,
                "Origin",
                [.. points.Select(t => new Point(t.X, t.Y))],
                Colors.Blue);

            plotDataSource.GetOrAddYLine(0, "Threshold", threshold, Colors.LightGreen);

            var scatterMarkers = plotDataSource.GetOrAddScatterMarkers(
                0,
                "Maxima",
                [.. indexes.Select(t => points[t])],
                Colors.DarkMagenta,
                MarkerShape.FilledTriangleDown);
            scatterMarkers.MarkerSize = 20;

            scatterMarkers = plotDataSource.GetOrAddScatterMarkers(
                0,
                "Filter Maxima",
                matchPoints,
                Colors.Red,
                MarkerShape.Asterisk);
            scatterMarkers.MarkerSize = 30;

            #endregion

            plotDataSource.GetOrAddScatterLine(
                1,
                "XDifferences",
                xDifferences.ToPoints(),
                Colors.Blue);

            plotDataSource.GetOrAddScatterLine(
                2,
                "XFilterDifferences",
                filterXDifferences.ToPoints(),
                Colors.Blue);

            window.ShowDialog();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
#endif
    }
}

file sealed record Temp(double X, double Y)
{
    public Temp() : this(0d, 0d)
    {
    }
}