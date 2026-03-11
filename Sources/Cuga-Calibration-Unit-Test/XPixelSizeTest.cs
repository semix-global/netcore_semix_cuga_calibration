using System.Windows;
using Core.Utilities;
using MathNet.Numerics.Statistics;
using MiniExcelLibs;
using Net.Utilities.ScottPlot.WPF.Plottables;
using Net.Utilities.ScottPlot.WPF.WPF;
using ScottPlot;
using Xunit;
using Point = Net.Utilities.Models.Geometries.Point;

namespace CugaCalibrationUnitTest;

public class XPixelSizeTest
{
    [Theory]
    [InlineData(0.5)]
    [InlineData(0.6)]
    [InlineData(0.7)]
    public void TestXPixelSize(double threshold)
    {
        var points = MiniExcel.Query<Temp>(@"Assets\XPixelSize\test1.xlsx", sheetName: "ALL Points").Select(t => new Point(t.X, t.Y))
            .OrderBy(t => t.X)
            .ToArray();

        var (indexes, _) = Extremumor.FindMaxima(points);

        var filterIndexes = indexes.Where(t => points[t].Y >= threshold).ToArray();
        filterIndexes = Filter.NMS([..filterIndexes.Select(t => points[t])], 402).Indexes.Select(t => filterIndexes[t]).ToArray();

        var doubles = filterIndexes.Select(t => points[t].Y - threshold).ToArray();

        var thread = new Thread(() =>
        {
            var window = new Window { Title = $"{nameof(TestXPixelSize)}_{threshold:0.###}" };

            var scatterPlotControl = new ScatterPlotControl();
            window.Content = scatterPlotControl;

            var plot = scatterPlotControl.Plot;

            plot.Title($"Template Match: px/quality {doubles.Aggregate(string.Empty, (t1, t2) => $"{t1}, {t2:0.###}")} \r {doubles.Median()} \r {doubles.Average()}");
            var scatterLine = ScatterLine.Empty;
            scatterLine.Update(
                "Origin",
                [.. points.Select(t => new Point(t.X, t.Y))],
                Colors.Blue);
            plot.PlottableList.Add(scatterLine);

            var yLine = YLine.Empty;
            yLine.Update("Threshold", threshold, Colors.Green);
            plot.PlottableList.Add(yLine);

            var scatterMarkers = ScatterMarkers.Empty;
            scatterMarkers.Update(
                "Maxima",
                [.. indexes.Select(t => points[t])],
                Colors.DarkMagenta,
                MarkerShape.FilledTriangleDown);
            scatterMarkers.MarkerSize = 20;
            plot.PlottableList.Add(scatterMarkers);

            scatterMarkers = ScatterMarkers.Empty;
            scatterMarkers.Update(
                "Filter Maxima",
                [.. filterIndexes.Select(t => points[t])],
                Colors.Red,
                MarkerShape.Asterisk);
            scatterMarkers.MarkerSize = 30;
            plot.PlottableList.Add(scatterMarkers);

            plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);

            window.ShowDialog();

            /*var imageFullPath = Path.GetFullPath($"{nameof(TestXPixelSize)}_{threshold:0.###}.png");
            FileHelper.DeleteFileIfExists(imageFullPath);
            plot.SavePng(imageFullPath, 1920, 1080);

            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = imageFullPath,
                UseShellExecute = true
            });*/
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        // thread.Join();
    }
}

file record Temp(double X, double Y)
{
    public Temp() : this(0d, 0d)
    {
    }
}