// #define XPixelSizeTest

using AwesomeAssertions;
using MiniExcelLibs;
using Net.Utilities.Algorithms.Modules;
using Xunit;
using Point = Net.Utilities.Models.Geometries.Point;

#if XPixelSizeTest
using System.Windows;
using Net.Utilities.ScottPlot.WPF.Plottables;
using Net.Utilities.ScottPlot.WPF.WPF;
using ScottPlot;
#endif

namespace CugaCalibrationUnitTest;

public class XPixelSizeTest
{
    [Theory]
    [InlineData(@"test1.xlsx", 0.7, 4, "24941.9192456002,-49885.8884509336,-24947.006616482104")]
    [InlineData(@"test2.xlsx", 0.7, 16, "373532.516904925,-597654.7819178579,-298827.728081162,-149414.714467882,597657.805112544,-672364.1594020331,1045897.4378628212,-224120.50205459807,-373535.058576985,522948.498187383,-821777.9057561241,149415.92401187203,298828.058422383,298826.97699630505,-522949.47728833207")]
    [InlineData(@"test3.xlsx", 0.7, 15, "-149892.878208227,599564.907676722,149892.007562089,-224835.74489835906,-449675.051612956,599566.589106955,-974292.9070480883,599563.7990014113,149890.08134654,-674509.4330802751,74947.406041805,374727.590840155,149891.02480566502,-449673.961032034")]
    [InlineData(@"test4.xlsx", 0.7, 17, "499637.04912352137,-49963.89651114796,-149891.030893863,99927.72758188,199855.29751987197,49963.730633420986,-499637.02965237596,-99927.1205225103,49964.088086548305,449672.165853312,-299782.073871425,449673.241709705,99927.649865187,-599565.068541604,549601.58250509,-399709.36756228504")]
    public void TestXPixelSize(string filePath, double threshold, int count, string expected)
    {
        var points = MiniExcel.Query<Temp>(@$"Assets\XPixelSize\{filePath}", sheetName: "ALL Points")
            .Select(t => new Point(t.X, t.Y))
            .Distinct()
            .OrderBy(t => t.X)
            .ToArray();

        var (indexes, _) = Extremumor.FindMaxima(points);

        var filterIndexes = indexes.Where(t => points[t].Y >= threshold).ToArray();
        filterIndexes = Filter.NMS([.. filterIndexes.Select(t => points[t])], 402).Indexes.Select(t => filterIndexes[t]).ToArray();

        filterIndexes = filterIndexes.Select(t => (Index: t, points[t].Y)).OrderByDescending(t => t.Y).Take(count).Select(t => t.Index).ToArray();

        var matchPoints = filterIndexes.Select(t => points[t]).ToArray();
        var xDifferences = matchPoints
            .Zip(matchPoints.Skip(1), (prev, next) => next.X - prev.X)
            .ToArray();
        var filterXDifferences = Filter.MAD(xDifferences).Results;

        filterXDifferences.Should().HaveCount(xDifferences.Length);
        string.Join(",", filterXDifferences).Should().Be(expected);

#if XPixelSizeTest
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
                matchPoints,
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
#endif
    }
}

file sealed record Temp(double X, double Y)
{
    public Temp() : this(0d, 0d)
    {
    }
}