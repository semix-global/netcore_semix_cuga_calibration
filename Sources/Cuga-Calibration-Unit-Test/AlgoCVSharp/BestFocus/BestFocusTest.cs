#define BestFocusTest

using algocv_sharp;
using AwesomeAssertions;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.WPF.V2;
using Xunit.Abstractions;

#if BestFocusTest
using System.Windows;
using System.Windows.Controls;
using ScottPlot.MultiplotLayouts;
#endif

using BestFocusModel = Core.Models.Models.Common.DarkField.BestFocus;

namespace CugaCalibrationUnitTest.AlgoCVSharp.BestFocus;

public class BestFocusTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void BestFocusEngineResult_ShouldMapToValidBestFocus()
    {
        const string filePath = @"Assets\20260508_214_0_0_1_short_011375_PMT08-CH2_8.raw";

        using var algoImage = new algocv_sharp.Image(filePath);
        using var algoLinearImage = algocv_sharp.ImageProc.LogToLinear(algoImage);

        using var engine = new BestFocusEngine(new BestFocusConfig());
        var bestFocusEngineResult = engine.Process(algoLinearImage);

        const double startEcs = 0d;
        const double stopEcs = 100d;
        var imageSize = new Net.Utilities.Models.Geometries.Size(algoLinearImage.Width, algoLinearImage.Height);

        BestFocusModel bestFocus = bestFocusEngineResult.ToBestFocus(imageSize, startEcs, stopEcs);

        testOutputHelper.WriteLine($"BestXStrehlRatioPoint: ({bestFocus.BestXStrehlRatioPoint.X:0.####}, {bestFocus.BestXStrehlRatioPoint.Y:0.####})");
        testOutputHelper.WriteLine($"BestYStrehlRatioPoint: ({bestFocus.BestYStrehlRatioPoint.X:0.####}, {bestFocus.BestYStrehlRatioPoint.Y:0.####})");
        testOutputHelper.WriteLine($"BestXStrehlRatioECS: {bestFocus.BestXStrehlRatioECS:0.####}");
        testOutputHelper.WriteLine($"BestYStrehlRatioECS: {bestFocus.BestYStrehlRatioECS:0.####}");
        testOutputHelper.WriteLine($"XFieldTiltFit: slope={bestFocus.XFieldTiltFitSlope:0.######}, intercept={bestFocus.XFieldTiltFitIntercept:0.######}, r²={bestFocus.XFieldTiltFitRSquared:0.######}");
        testOutputHelper.WriteLine($"YFieldTiltFit: slope={bestFocus.YFieldTiltFitSlope:0.######}, intercept={bestFocus.YFieldTiltFitIntercept:0.######}, r²={bestFocus.YFieldTiltFitRSquared:0.######}");

        bestFocus.IsAlgorithmOk.Should().BeTrue();

        bestFocus.XStrehlRatioPoints.Should().NotBeEmpty();
        bestFocus.YStrehlRatioPoints.Should().NotBeEmpty();
        bestFocus.XStrehlRatioPoints.Count.Should().Be(bestFocus.YStrehlRatioPoints.Count);

        bestFocus.XStrehlRatioFitPoints.Should().NotBeEmpty();
        bestFocus.YStrehlRatioFitPoints.Should().NotBeEmpty();

        bestFocus.XStrehlRatioColumnPoints.Should().NotBeEmpty();
        bestFocus.YStrehlRatioColumnPoints.Should().NotBeEmpty();

        bestFocus.BestXStrehlRatioPoint.Y.Should().BeInRange(0, 1);
        bestFocus.BestYStrehlRatioPoint.Y.Should().BeInRange(0, 1);

        bestFocus.BestXStrehlRatioECS.Should().BeInRange(startEcs, stopEcs);
        bestFocus.BestYStrehlRatioECS.Should().BeInRange(startEcs, stopEcs);

        bestFocus.XIntraRibbonFieldsPoints.Should().NotBeEmpty();
        bestFocus.YIntraRibbonFieldsPoints.Should().NotBeEmpty();

        bestFocus.XFieldTiltPoints.Should().NotBeEmpty();
        bestFocus.YFieldTiltPoints.Should().NotBeEmpty();

        bestFocus.XFieldTiltFitRSquared.Should().BeInRange(0, 1);
        bestFocus.YFieldTiltFitRSquared.Should().BeInRange(0, 1);

#if BestFocusTest
        var thread = new Thread(() =>
        {
            var window = new Window { Title = nameof(BestFocusEngineResult_ShouldMapToValidBestFocus), WindowState = WindowState.Maximized };

            var grid = new System.Windows.Controls.Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var xScatterPlotControl = new PlotControl();
            var yScatterPlotControl = new PlotControl();
            System.Windows.Controls.Grid.SetRow(xScatterPlotControl, 0);
            System.Windows.Controls.Grid.SetRow(yScatterPlotControl, 1);
            grid.Children.Add(xScatterPlotControl);
            grid.Children.Add(yScatterPlotControl);

            window.Content = grid;

            var xPlotDataSource = new PlotDataSource();
            xScatterPlotControl.DataSource = xPlotDataSource;
            xPlotDataSource.Configure(new Columns(), 3);
            xPlotDataSource.SetTitle(0, "Peek X Strehl Ratio(Y: Strehl Ratio - X: px)");
            xPlotDataSource.SetTitle(1, "X Intra-Ribbon Fields(Y: Strehl Ratio - X: px)");
            xPlotDataSource.SetTitle(2, "X Field Tilt(Y: px - X: Intra-Ribbon)");

            var yPlotDataSource = new PlotDataSource();
            yScatterPlotControl.DataSource = yPlotDataSource;
            yPlotDataSource.Configure(new Columns(), 3);
            yPlotDataSource.SetTitle(0, "Peek Y Strehl Ratio(Y: Strehl Ratio - X: px)");
            yPlotDataSource.SetTitle(1, "Y Intra-Ribbon Fields(Y: Strehl Ratio - X: px)");
            yPlotDataSource.SetTitle(2, "Y Field Tilt(Y: px - X: Intra-Ribbon)");

            bestFocus.XStrehlRatioPlotDataSource = xPlotDataSource;
            bestFocus.YStrehlRatioPlotDataSource = yPlotDataSource;

            window.ShowDialog();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
#endif
    }
}