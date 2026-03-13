// #define GenerateTest

using AwesomeAssertions;
using Xunit;
using CommunityToolkit.Diagnostics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Generate = MathNet.Numerics.Generate;

#if GenerateTest
using System.Windows;
using Net.Utilities.Helpers.Extensions;
using ScottPlot.MultiplotLayouts;
using Net.Utilities.ScottPlot.WPF.WPF;
using ScottPlot;
#endif

namespace CugaCalibrationUnitTest;

public class GenerateTest
{
    [Theory]
    [InlineData(4792, 4, 1d)]
    [InlineData(4792, 5, 1d)]
    public void TestGenerate(int totalLength, int segmentCount, double coefficient)
    {
        var indexes = Generate.LinearRangeInt32(0, segmentCount - 1);

        var windows =
            (
                from index in indexes
                select Generate.LinearVShapeWindowBySegments(
                    coefficient,
                    coefficient / 1000d,
                    indexes.Length,
                    index,
                    totalLength)
            )
            .ToArray();

        var allWindow = Generate.LinearVShapeWindowBySegments(
            coefficient,
            coefficient / 1000d,
            indexes.Length,
            indexes,
            totalLength);

        foreach (var (index, (window, (vStartIndex, vMiddleIndex, vStopIndex))) in windows.Index())
        {
            vStartIndex.Should().Be(allWindow.Regions[index].VStartIndex);
            vMiddleIndex.Should().Be(allWindow.Regions[index].VMiddleIndex);
            vStopIndex.Should().Be(allWindow.Regions[index].VStopIndex);

            Vector<double>.Build.Dense(window).SubVectorRange(vStartIndex, vStopIndex).Should()
                .HaveCount(vStopIndex - vStartIndex + 1)
                .And.BeEquivalentTo(Vector<double>.Build.Dense(allWindow.Window).SubVectorRange(vStartIndex, vStopIndex),
                    options => options.WithStrictOrdering());
        }

#if GenerateTest
        var thread = new Thread(() =>
        {
            var wpfWindow = new Window { Title = $"{nameof(TestGenerate)}, totalLength{totalLength}, segmentCount{segmentCount}, coefficient{coefficient}", WindowState = WindowState.Maximized };

            var scatterPlotControl = new ScatterPlotControl();
            wpfWindow.Content = scatterPlotControl;

            scatterPlotControl.Configure(new Rows(), windows.Length + 1);

            scatterPlotControl.SetTitle(0, string.Join(", ", indexes));
            var scatterLine = scatterPlotControl.AddScatterLine(0);
            scatterLine.Update(string.Empty, allWindow.Window.ToPoints(), Colors.Blue);
            foreach (var (vStartIndex, vMiddleIndex, vStopIndex) in allWindow.Regions)
            {
                var xLine = scatterPlotControl.AddXLine(0);
                xLine.Update(string.Empty, vStartIndex, Colors.LightGreen);

                xLine = scatterPlotControl.AddXLine(0);
                xLine.Update(string.Empty, vMiddleIndex, Colors.Green);

                xLine = scatterPlotControl.AddXLine(0);
                xLine.Update(string.Empty, vStopIndex, Colors.DarkGreen);
            }

            foreach (var (index, (window, (vStartIndex, vMiddleIndex, vStopIndex))) in windows.Index())
            {
                scatterPlotControl.SetTitle(index + 1, $"{indexes[index]}");

                scatterLine = scatterPlotControl.AddScatterLine(index + 1);
                scatterLine.Update(string.Empty, window.ToPoints(), Colors.Blue);

                var xLine = scatterPlotControl.AddXLine(index + 1);
                xLine.Update(string.Empty, vStartIndex, Colors.LightGreen);

                xLine = scatterPlotControl.AddXLine(index + 1);
                xLine.Update(string.Empty, vMiddleIndex, Colors.Green);

                xLine = scatterPlotControl.AddXLine(index + 1);
                xLine.Update(string.Empty, vStopIndex, Colors.DarkGreen);
            }

            wpfWindow.ShowDialog();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
#endif
    }

    [Fact]
    public void LinearVShapeWindowByIndex_Single_ShouldReturnCorrectWindowAndRegion()
    {
        var (window, region) = Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            5,
            4,
            11);

        region.VStartIndex.Should().Be(1);
        region.VMiddleIndex.Should().Be(5);
        region.VStopIndex.Should().Be(9);

        window.Should()
            .HaveCount(11)
            .And.BeEquivalentTo(
            [
                1d, // 0
                1d, // 1 <- V Start
                0.75, // 2
                0.5, // 3
                0.25, // 4
                0d, // 5 <- V Middle
                0.25, // 6
                0.5, // 7
                0.75, // 8
                1d, // 9 <- V Stop
                1d // 10
            ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void LinearVShapeWindowByIndex_Single_NegativeCoefficient_ShouldReturnCorrectWindowAndRegion()
    {
        var (window, region) = Generate.LinearVShapeWindowByIndex(
            -1d,
            0d,
            5,
            4,
            11);

        region.VStartIndex.Should().Be(1);
        region.VMiddleIndex.Should().Be(5);
        region.VStopIndex.Should().Be(9);

        window.Should()
            .HaveCount(11)
            .And.BeEquivalentTo(
            [
                -1, // 0
                -1, // 1 <- V Start
                -0.75, // 2
                -0.5, // 3
                -0.25, // 4
                0, // 5 <- V Middle
                -0.25, // 6
                -0.5, // 7
                -0.75, // 8
                -1, // 9 <- V Stop
                -1 // 10
            ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void LinearVShapeWindowByIndex_Single_LeftClipped_ShouldReturnAsymmetricWindow()
    {
        var (window, region) = Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            2,
            4,
            10);

        region.VStartIndex.Should().Be(0);
        region.VMiddleIndex.Should().Be(2);
        region.VStopIndex.Should().Be(6);

        window.Should()
            .HaveCount(10)
            .And.BeEquivalentTo(
            [
                0.5, // 0 <- V Start
                0.25, // 1
                0d, // 2 <- V Middle
                0.25, // 3
                0.5, // 4
                0.75, // 5
                1d, // 6 <- V Stop
                1d, // 7
                1d, // 8
                1d // 9
            ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void LinearVShapeWindowByIndex_Single_RightClipped_ShouldReturnAsymmetricWindow()
    {
        var (window, region) = Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            7,
            4,
            10);

        region.VStartIndex.Should().Be(3);
        region.VMiddleIndex.Should().Be(7);
        region.VStopIndex.Should().Be(9);

        window.Should()
            .HaveCount(10)
            .And.BeEquivalentTo(
            [
                1d, // 0
                1d, // 1
                1d, // 2
                1d, // 3 <- V Start
                0.75, // 4
                0.5, // 5
                0.25, // 6
                0d, // 7 <- V Middle
                0.25, // 8
                0.5 // 9 <- V Stop
            ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void LinearVShapeWindowByIndex_Single_Exception()
    {
        ((Action)(() => Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            -1,
            4,
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("vMiddleIndex");
        ((Action)(() => Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            10,
            4,
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("vMiddleIndex");

        ((Action)(() => Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            0,
            0,
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("vHalfWidth");
        ((Action)(() => Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            0,
            -1,
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("vHalfWidth");

        ((Action)(() => Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            0,
            4,
            0))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("totalLength");
        ((Action)(() => Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            0,
            4,
            -1))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("totalLength");
    }

    [Fact]
    public void LinearVShapeWindowByIndex_Multiple_ShouldReturnCorrectWindowAndRegions()
    {
        var (window, regions) = Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            [3, 7],
            2,
            10);

        regions.Should().HaveCount(2);
        regions[0].VStartIndex.Should().Be(1);
        regions[0].VMiddleIndex.Should().Be(3);
        regions[0].VStopIndex.Should().Be(5);
        regions[1].VStartIndex.Should().Be(5);
        regions[1].VMiddleIndex.Should().Be(7);
        regions[1].VStopIndex.Should().Be(9);

        window.Should()
            .HaveCount(10)
            .And.BeEquivalentTo(
            [
                1d, // 0
                1d, // 1 <- V1 Start
                0.5, // 2
                0d, // 3 <- V1 Middle
                0.5, // 4
                1d, // 5 <-  V1 Stop (V2 Start) overlap:  V1 wrote 1d, V2 overwrote 1d (same)
                0.5, // 6
                0d, // 7 <- V2 Middle
                0.5, // 8
                1d // 9 <- V2 Stop
            ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void LinearVShapeWindowByIndex_Multiple_BothEdgesClipped_ShouldReturnCorrectWindowAndRegions()
    {
        var (window, regions) = Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            [1, 8],
            2,
            10);

        regions.Should().HaveCount(2);
        regions[0].VStartIndex.Should().Be(0);
        regions[0].VMiddleIndex.Should().Be(1);
        regions[0].VStopIndex.Should().Be(3);
        regions[1].VStartIndex.Should().Be(6);
        regions[1].VMiddleIndex.Should().Be(8);
        regions[1].VStopIndex.Should().Be(9);

        window.Should()
            .HaveCount(10)
            .And.BeEquivalentTo(
            [
                0.5, // 0 <- V1 Start
                0d, // 1 <- V1 Middle
                0.5, // 2
                1d, // 3 <- V1 Stop
                1d, // 4
                1d, // 5
                1d, // 6 <- V2 Start
                0.5, // 7
                0d, // 8 <- V2 Middle
                0.5 // 9 <- V2 Stop
            ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void LinearVShapeWindowByIndex_Multiple_RegionsTouching_ShouldReturnConsecutiveVShapes()
    {
        var (window, regions) = Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            [2, 6],
            2,
            10);

        regions.Should().HaveCount(2);
        regions[0].VStartIndex.Should().Be(0);
        regions[0].VMiddleIndex.Should().Be(2);
        regions[0].VStopIndex.Should().Be(4);
        regions[1].VStartIndex.Should().Be(4);
        regions[1].VMiddleIndex.Should().Be(6);
        regions[1].VStopIndex.Should().Be(8);

        window.Should()
            .HaveCount(10)
            .And.BeEquivalentTo(
            [
                1d, // 0 <- V1 Start
                0.5, // 1
                0d, // 2 <- V1 Middle
                0.5, // 3
                1d, // 4 <- V1 Stop (V2 Start) overlap:  V1 wrote 1d, V2 overwrote 1d (same)
                0.5, // 5
                0d, // 6 <- V2 Middle
                0.5, // 7
                1d, // 8 <- V2 Stop
                1d // 9
            ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void LinearVShapeWindowByIndex_Multiple_RegionsPartialOverlap_SecondVShapeWinsInOverlapZone()
    {
        var (window, regions) = Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            [3, 5],
            2,
            10);

        regions.Should().HaveCount(2);
        regions[0].VStartIndex.Should().Be(1);
        regions[0].VMiddleIndex.Should().Be(3);
        regions[0].VStopIndex.Should().Be(5);
        regions[1].VStartIndex.Should().Be(3);
        regions[1].VMiddleIndex.Should().Be(5);
        regions[1].VStopIndex.Should().Be(7);

        window.Should()
            .HaveCount(10)
            .And.BeEquivalentTo(
            [
                1d, // 0
                1d, // 1 <- V1 Start
                0.5, // 2
                1d, // 3 <-  V1 Middle (V2 Start) overlap:  V1 wrote 0d, V2 overwrote 1d
                0.5, // 4 <-                      overlap:  V1 wrote 0.5, V2 overwrote 0.5 (same)
                0d, // 5 <- V1 Stop (V2 Middle)   overlap:  V1 wrote 1d, V2 overwrote 0d
                0.5, // 6
                1d, // 7 <- V2 Stop
                1d, // 8
                1d // 9
            ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void LinearVShapeWindowByIndex_Multiple_RegionsHeavyOverlap_SecondVShapeFullyOverwrites()
    {
        var (window, regions) = Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            [3, 4],
            2,
            10);

        regions.Should().HaveCount(2);
        regions[0].VStartIndex.Should().Be(1);
        regions[0].VMiddleIndex.Should().Be(3);
        regions[0].VStopIndex.Should().Be(5);
        regions[1].VStartIndex.Should().Be(2);
        regions[1].VMiddleIndex.Should().Be(4);
        regions[1].VStopIndex.Should().Be(6);

        window.Should()
            .HaveCount(10)
            .And.BeEquivalentTo(
            [
                1d, // 0
                1d, // 1 <- V1 Start
                1d, // 2 <-         V2 Start  overlap:  V1 wrote 0.5, V2 overwrote 1
                0.5, // 3 <- V1 Middle        overlap:  V1 wrote 0d, V2 overwrote 0.5
                0d, // 4 <-         V2 Middle overlap:  V1 wrote 0.5, V2 overwrote 0d
                0.5, // 5 <- V1 Stop          overlap:  V1 wrote 1d, V2 overwrote 0.5
                1d, // 6 <- V2 Stop
                1d, // 7
                1d, // 8
                1d // 9
            ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void LinearVShapeWindowByIndex_Multiple_Empty()
    {
        var (window, regions) = Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            [],
            4,
            10);

        regions.Should().BeEmpty();

        window.Should()
            .HaveCount(10)
            .And.OnlyContain(t => Equals(t, 1d));
    }

    [Fact]
    public void LinearVShapeWindowByIndex_Multiple_Exception()
    {
        ((Action)(() => Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            [-1, 0],
            4,
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("vMiddleIndex");
        ((Action)(() => Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            [0, 10],
            4,
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("vMiddleIndex");

        ((Action)(() => Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            [0],
            0,
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("vHalfWidth");
        ((Action)(() => Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            [0],
            -1,
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("vHalfWidth");
        ((Action)(() => Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            [],
            0,
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("vHalfWidth");
        ((Action)(() => Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            [],
            -1,
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("vHalfWidth");

        ((Action)(() => Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            [0],
            4,
            0))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("totalLength");
        ((Action)(() => Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            [0],
            4,
            -1))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("totalLength");
        ((Action)(() => Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            [],
            4,
            0))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("totalLength");
        ((Action)(() => Generate.LinearVShapeWindowByIndex(
            1d,
            0d,
            [],
            4,
            -1))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("totalLength");
    }

    [Fact]
    public void LinearVShapeWindowBySegments_Single_ShouldReturnCorrectWindowAndRegion()
    {
        var (window, region) = Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            0,
            10);

        region.VStartIndex.Should().Be(0);
        region.VMiddleIndex.Should().Be(2);
        region.VStopIndex.Should().Be(4);

        window.Should()
            .HaveCount(10)
            .And.BeEquivalentTo(
            [
                1d, // 0 <- V Start
                0.5, // 1
                0d, // 2 <- V Middle
                0.5, // 3
                1d, // 4 <- V Stop
                1d, // 5
                1d, // 6
                1d, // 7
                1d, // 8
                1d // 9
            ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void LinearVShapeWindowBySegments_Single_LastSegmentRightEdge_ShouldReturnClippedWindow()
    {
        var (window, region) = Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            1,
            9);

        region.VStartIndex.Should().Be(4);
        region.VMiddleIndex.Should().Be(6);
        region.VStopIndex.Should().Be(8);

        window.Should()
            .HaveCount(9)
            .And.BeEquivalentTo(
            [
                1d, // 0
                1d, // 1
                1d, // 2
                1d, // 3
                1d, // 4 <- V Start
                0.5, // 5
                0d, // 6 <- V Middle
                0.5, // 7
                1d // 8 <- V Stop
            ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void LinearVShapeWindowBySegments_Single_Exception()
    {
        ((Action)(() => Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            -1,
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("vShapeSegmentIndex");
        ((Action)(() => Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            2,
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("vShapeSegmentIndex");

        ((Action)(() => Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            0,
            0,
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("segmentCount");
        ((Action)(() => Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            -1,
            0,
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("segmentCount");

        ((Action)(() => Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            0,
            0))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("totalLength");
        ((Action)(() => Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            0,
            -1))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("totalLength");
    }

    [Fact]
    public void LinearVShapeWindowBySegments_Multiple_ShouldReturnCorrectWindowAndRegions()
    {
        var (window, regions) = Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            [0, 1],
            10);

        regions.Should().HaveCount(2);
        regions[0].VStartIndex.Should().Be(0);
        regions[0].VMiddleIndex.Should().Be(2);
        regions[0].VStopIndex.Should().Be(4);
        regions[1].VStartIndex.Should().Be(5);
        regions[1].VMiddleIndex.Should().Be(7);
        regions[1].VStopIndex.Should().Be(9);

        window.Should()
            .HaveCount(10)
            .And.BeEquivalentTo(
            [
                1d, // 0 <- V1 Start
                0.5, // 1
                0d, // 2 <- V1 Middle
                0.5, // 3
                1d, // 4 <- V1 Stop
                1d, // 5 <- V2 Start
                0.5, // 6
                0d, // 7 <- V2 Middle
                0.5, // 8
                1d // 9 <- V2 Stop
            ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void LinearVShapeWindowBySegments_Multiple_AllThreeSegments_ShouldReturnFullWindowAndRegions()
    {
        var (window, regions) = Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            3,
            [0, 1, 2],
            10);

        regions.Should().HaveCount(3);
        regions[0].VStartIndex.Should().Be(0);
        regions[0].VMiddleIndex.Should().Be(1);
        regions[0].VStopIndex.Should().Be(2);
        regions[1].VStartIndex.Should().Be(3);
        regions[1].VMiddleIndex.Should().Be(4);
        regions[1].VStopIndex.Should().Be(5);
        regions[2].VStartIndex.Should().Be(6);
        regions[2].VMiddleIndex.Should().Be(7);
        regions[2].VStopIndex.Should().Be(8);

        window.Should()
            .HaveCount(10)
            .And.BeEquivalentTo(
            [
                1d, // 0 <- V1 Start
                0d, // 1 <- V1 Middle
                1d, // 2 <- V1 Stop
                1d, // 3 <- V2 Start
                0d, // 4 <- V2 Middle
                1d, // 5 <- V2 Stop
                1d, // 6 <- V3 Start
                0d, // 7 <- V3 Middle
                1d, // 8 <- V3 Stop
                1d // 9
            ], options => options.WithStrictOrdering());
    }

    /*
     * > 8/2/2
     * 2
     * > 9/2/2
     * 2
     * > 10/2/2
     * 2
     * > 11/2/2
     * 2
     * > 12/2/2
     * 3
     * > 7/2/2
     * 1
     */

    [Fact]
    public void LinearVShapeWindowBySegments_Multiple_LastSegmentRightEdge_ShouldReturnCorrectWindowAndRegions8()
    {
        var (window, regions) = Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            [0, 1],
            8);

        regions.Should().HaveCount(2);
        regions[0].VStartIndex.Should().Be(0);
        regions[0].VMiddleIndex.Should().Be(2);
        regions[0].VStopIndex.Should().Be(4);
        regions[1].VStartIndex.Should().Be(4);
        regions[1].VMiddleIndex.Should().Be(6);
        regions[1].VStopIndex.Should().Be(7);

        window.Should()
            .HaveCount(8)
            .And.BeEquivalentTo(
            [
                1d, // 0 <- V1 Start
                0.5, // 1
                0d, // 2 <- V1 Middle
                0.5, // 3
                1d, // 4 <- V1 Stop (V2 Start) overlap:  V1 wrote 1d, V2 overwrote 1d (same)
                0.5, // 5
                0d, // 6 <- V2 Middle
                0.5 // 7 <- V2 Stop
            ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void LinearVShapeWindowBySegments_Multiple_LastSegmentRightEdge_ShouldReturnCorrectWindowAndRegions9()
    {
        var (window, regions) = Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            [0, 1],
            9);

        regions.Should().HaveCount(2);
        regions[0].VStartIndex.Should().Be(0);
        regions[0].VMiddleIndex.Should().Be(2);
        regions[0].VStopIndex.Should().Be(4);
        regions[1].VStartIndex.Should().Be(4);
        regions[1].VMiddleIndex.Should().Be(6);
        regions[1].VStopIndex.Should().Be(8);

        window.Should()
            .HaveCount(9)
            .And.BeEquivalentTo(
            [
                1d, // 0 <- V1 Start
                0.5, // 1
                0d, // 2 <- V1 Middle
                0.5, // 3
                1d, // 4 <- V1 Stop (V2 Start) overlap:  V1 wrote 1d, V2 overwrote 1d (same)
                0.5, // 5
                0d, // 6 <- V2 Middle
                0.5, // 7
                1d // 8 <- V2 Stop
            ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void LinearVShapeWindowBySegments_Multiple_LastSegmentRightEdge_ShouldReturnCorrectWindowAndRegions10()
    {
        var (window, regions) = Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            [0, 1],
            10);

        regions.Should().HaveCount(2);
        regions[0].VStartIndex.Should().Be(0);
        regions[0].VMiddleIndex.Should().Be(2);
        regions[0].VStopIndex.Should().Be(4);
        regions[1].VStartIndex.Should().Be(5);
        regions[1].VMiddleIndex.Should().Be(7);
        regions[1].VStopIndex.Should().Be(9);

        window.Should()
            .HaveCount(10)
            .And.BeEquivalentTo(
            [
                1d, // 0 <- V1 Start
                0.5, // 1
                0d, // 2 <- V1 Middle
                0.5, // 3
                1d, // 4 <- V1 Stop
                1d, // 5 <- V2 Start
                0.5, // 6
                0d, // 7 <- V2 Middle
                0.5, // 8
                1d // 9 <- V2 Stop
            ], options => options.WithStrictOrdering());
    }

    [Fact]
    public void LinearVShapeWindowBySegments_Multiple_LastSegmentRightEdge_ShouldReturnCorrectWindowAndRegions11()
    {
        var (window, regions) = Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            [0, 1],
            11);

        regions.Should().HaveCount(2);
        regions[0].VStartIndex.Should().Be(0);
        regions[0].VMiddleIndex.Should().Be(2);
        regions[0].VStopIndex.Should().Be(4);
        regions[1].VStartIndex.Should().Be(5);
        regions[1].VMiddleIndex.Should().Be(7);
        regions[1].VStopIndex.Should().Be(9);

        window.Should()
            .HaveCount(11)
            .And.BeEquivalentTo(
            [
                1d, // 0 <- V1 Start
                0.5, // 1
                0d, // 2 <- V1 Middle
                0.5, // 3
                1d, // 4 <- V1 Stop
                1d, // 5 <- V2 Start
                0.5, // 6
                0d, // 7 <- V2 Middle
                0.5, // 8
                1d, // 9 <- V2 Stop
                1d // 10
            ], options => options.WithStrictOrdering());
    }


    [Fact]
    public void LinearVShapeWindowBySegments_Multiple_Empty()
    {
        var (window, regions) = Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            [],
            10);

        regions.Should().BeEmpty();

        window.Should()
            .HaveCount(10)
            .And.OnlyContain(t => Equals(t, 1d));
    }

    [Fact]
    public void LinearVShapeWindowBySegments_Multiple_Exception()
    {
        ((Action)(() => Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            [-1, 0],
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("vShapeSegmentIndex");
        ((Action)(() => Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            [0, 2],
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("vShapeSegmentIndex");

        ((Action)(() => Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            0,
            [0],
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("segmentCount");
        ((Action)(() => Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            -1,
            [0],
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("segmentCount");
        ((Action)(() => Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            0,
            [],
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("segmentCount");
        ((Action)(() => Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            -1,
            [],
            10))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("segmentCount");

        ((Action)(() => Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            [0],
            0))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("totalLength");
        ((Action)(() => Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            [0],
            -1))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("totalLength");
        ((Action)(() => Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            [],
            0))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("totalLength");
        ((Action)(() => Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            2,
            [],
            -1))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("totalLength");
    }
}

public static class GenerateExtensions
{
    extension(Generate)
    {
        public static (double[] Window, (int VStartIndex, int VMiddleIndex, int VStopIndex) Region) LinearVShapeWindowByIndex(
            double coefficient,
            double vCoefficient,
            int vMiddleIndex,
            int vHalfWidth,
            int totalLength)
        {
            Guard.IsGreaterThan(totalLength, 0);
            Guard.IsGreaterThanOrEqualTo(vMiddleIndex, 0);
            Guard.IsLessThanOrEqualTo(vMiddleIndex, totalLength - 1);
            Guard.IsGreaterThan(vHalfWidth, 0);

            var window = Generate.Repeat(totalLength, coefficient);

            var vStartIndex = Math.Max(0, vMiddleIndex - vHalfWidth);
            var vStopIndex = Math.Min(totalLength - 1, vMiddleIndex + vHalfWidth);

            var k = (coefficient - vCoefficient) / vHalfWidth;

            for (var i = vMiddleIndex - 1; i >= vStartIndex; i--) window[i] = vCoefficient + (vMiddleIndex - i) * k;

            window[vMiddleIndex] = vCoefficient;

            for (var i = vMiddleIndex + 1; i <= vStopIndex; i++) window[i] = vCoefficient + (i - vMiddleIndex) * k;

            return (window, (vStartIndex, vMiddleIndex, vStopIndex));
        }

        public static (double[] Window, (int VStartIndex, int VMiddleIndex, int VStopIndex)[] Regions) LinearVShapeWindowByIndex(
            double coefficient,
            double vCoefficient,
            IReadOnlyList<int> vMiddleIndexes,
            int vHalfWidth,
            int totalLength)
        {
            Guard.IsGreaterThan(vHalfWidth, 0);
            Guard.IsGreaterThan(totalLength, 0);

            var window = Generate.Repeat(totalLength, coefficient);
            var regions = new (int VStartIndex, int VMiddleIndex, int VStopIndex)[vMiddleIndexes.Count];

            for (var i = 0; i < vMiddleIndexes.Count; i++)
            {
                var (tempWindow, region) = Generate.LinearVShapeWindowByIndex(
                    coefficient,
                    vCoefficient,
                    vMiddleIndexes[i],
                    vHalfWidth,
                    totalLength);

                Vector<double>.Build.Dense(window).SetSubVectorRange(
                    region.VStartIndex,
                    region.VStopIndex,
                    Vector<double>.Build.Dense([..tempWindow.AsSpan()[region.VStartIndex..(region.VStopIndex + 1)]]));

                regions[i] = region;
            }

            return (window, regions);
        }

        public static (double[] Window, (int VStartIndex, int VMiddleIndex, int VStopIndex) Region) LinearVShapeWindowBySegments(
            double coefficient,
            double vCoefficient,
            int segmentCount,
            int vShapeSegmentIndex,
            int totalLength)
        {
            Guard.IsGreaterThan(segmentCount, 0);
            Guard.IsGreaterThanOrEqualTo(vShapeSegmentIndex, 0);
            Guard.IsLessThanOrEqualTo(vShapeSegmentIndex, segmentCount - 1);
            Guard.IsGreaterThan(totalLength, 0);

            var segmentLength = totalLength / segmentCount;
            var vHalfWidth = segmentLength / 2;
            var vMiddleIndex = vShapeSegmentIndex * segmentLength + vHalfWidth;

            return Generate.LinearVShapeWindowByIndex(
                coefficient,
                vCoefficient,
                vMiddleIndex,
                vHalfWidth,
                totalLength);
        }

        public static (double[] Window, (int VStartIndex, int VMiddleIndex, int VStopIndex)[] Regions) LinearVShapeWindowBySegments(
            double coefficient,
            double vCoefficient,
            int segmentCount,
            IReadOnlyList<int> vShapeSegmentIndexes,
            int totalLength)
        {
            Guard.IsGreaterThan(segmentCount, 0);
            Guard.IsGreaterThan(totalLength, 0);

            var window = Generate.Repeat(totalLength, coefficient);
            var regions = new (int VStartIndex, int VMiddleIndex, int VStopIndex)[vShapeSegmentIndexes.Count];

            for (var i = 0; i < vShapeSegmentIndexes.Count; i++)
            {
                var (tempWindow, region) = Generate.LinearVShapeWindowBySegments(
                    coefficient,
                    vCoefficient,
                    segmentCount,
                    vShapeSegmentIndexes[i],
                    totalLength);

                Vector<double>.Build.Dense(window).SetSubVectorRange(
                    region.VStartIndex,
                    region.VStopIndex,
                    Vector<double>.Build.Dense([..tempWindow.AsSpan()[region.VStartIndex..(region.VStopIndex + 1)]]));

                regions[i] = region;
            }

            return (window, regions);
        }
    }
}