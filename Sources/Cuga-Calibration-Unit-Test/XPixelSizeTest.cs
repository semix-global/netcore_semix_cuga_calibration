using System.Windows;
using Core.Utilities;
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
    [InlineData(@"Assets\XPixelSize\test1.xlsx", 0.7, 4)]
    [InlineData(@"Assets\XPixelSize\test2.xlsx", 0.7, 16)]
    [InlineData(@"Assets\XPixelSize\test3.xlsx", 0.7, 15)]
    [InlineData(@"Assets\XPixelSize\test4.xlsx", 0.7, 17)]
    public void TestXPixelSize(string filePath, double threshold, int count)
    {
        var points = MiniExcel.Query<Temp>(filePath, sheetName: "ALL Points")
            .Select(t => new Point(t.X, t.Y))
            .Distinct()
            .OrderBy(t => t.X)
            .ToArray();

        var (indexes, _) = Extremumor.FindMaxima(points);

        var filterIndexes = indexes.Where(t => points[t].Y >= threshold).ToArray();
        filterIndexes = Filter.NMS([..filterIndexes.Select(t => points[t])], 402).Indexes.Select(t => filterIndexes[t]).ToArray();

        filterIndexes = filterIndexes.Select(t => (Index: t, points[t].Y)).OrderByDescending(t => t.Y).Take(count).Select(t => t.Index).ToArray();

        var thread = new Thread(() =>
        {
            var window = new Window { Title = $"{nameof(TestXPixelSize)}_{threshold:0.###}" };

            var scatterPlotControl = new ScatterPlotControl();
            window.Content = scatterPlotControl;

            var plot = scatterPlotControl.Plot;

            plot.Title("Template Match: px/score");
            var scatterLine = ScatterLine.Empty;
            scatterLine.Update(
                "Origin",
                [.. points.Select(t => new Point(t.X, t.Y))],
                Colors.Blue);
            plot.PlottableList.Add(scatterLine);

            var yLine = YLine.Empty;
            yLine.Update("Threshold", threshold, Colors.LightGreen);
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
        thread.Join();
    }
}

file record Temp(double X, double Y)
{
    public Temp() : this(0d, 0d)
    {
    }
}