#define VSharpTest

using AwesomeAssertions;
using CommunityToolkit.Diagnostics;
using Core.Utilities;
using MathNet.Numerics.LinearAlgebra;
using Xunit;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Generate = MathNet.Numerics.Generate;
using Point = Net.Utilities.Models.Geometries.Point;

#if VSharpTest
using System.Windows;
using Net.Utilities.Helpers.Extensions;
using ScottPlot.MultiplotLayouts;
using Net.Utilities.ScottPlot.WPF.WPF;
using ScottPlot;
#endif

namespace CugaCalibrationUnitTest;

public class VSharpTest
{
    [Theory]
    [InlineData("test_1_4_true.raw", 11, 4, true, 786)]
    [InlineData("test_1_6_true.raw", 11, 6, true, 435)]
    [InlineData("test_1_1_false.raw", 5, 1, false, 1049)]
    [InlineData("test_1_3_false.raw", 5, 3, false, 347)]
    public void TestVSharp1(string filePath, int segmentCount, int segmentIndex, bool isLog, int expectedIndex)
    {
        var vSharpResult = GetVSharp(filePath, segmentCount, isLog);

        vSharpResult.VSharpIndex.Should().Be(expectedIndex);

#if VSharpTest
        var thread = new Thread(() =>
        {
            var wpfWindow = new Window { Title = nameof(TestVSharp1), WindowState = WindowState.Maximized };

            var scatterPlotControl = new ScatterPlotControl();
            wpfWindow.Content = scatterPlotControl;

            scatterPlotControl.Configure(new Rows(), 2);

            RefreshVSharpPlot(
                filePath,
                segmentCount,
                segmentIndex,
                isLog,
                scatterPlotControl,
                vSharpResult,
                0,
                1);

            wpfWindow.ShowDialog();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
#endif
    }


    [Theory]
    [InlineData("test_1_4_true.raw", "test_1_6_true.raw", 11, 4, 6, true)]
    [InlineData("test_1_1_false.raw", "test_1_3_false.raw", 5, 1, 3, false)]
    public void TestVSharps(string filePath1, string filePath2, int segmentCount, int segmentIndex1, int segmentIndex2, bool isLog)
    {
        var vSharpResult1 = GetVSharp(filePath1, segmentCount, isLog);
        var vSharpResult2 = GetVSharp(filePath2, segmentCount, isLog);


#if VSharpTest
        var thread = new Thread(() =>
        {
            var wpfWindow = new Window { Title = nameof(TestVSharp1), WindowState = WindowState.Maximized };

            var scatterPlotControl = new ScatterPlotControl();
            wpfWindow.Content = scatterPlotControl;

            scatterPlotControl.Configure(new Rows(), 6);

            RefreshVSharpPlot(
                filePath1,
                segmentCount,
                segmentIndex1,
                isLog,
                scatterPlotControl,
                vSharpResult1,
                0,
                1);

            RefreshVSharpPlot(
                filePath2,
                segmentCount,
                segmentIndex2,
                isLog,
                scatterPlotControl,
                vSharpResult2,
                2,
                3);

            wpfWindow.ShowDialog();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
#endif
    }

    private static VSharpResult GetVSharp(string filePath, int segmentCount, bool isLog)
    {
        Guard.IsGreaterThan(segmentCount, 4);

        using var originImage = RawImageFactory.CreateImage(@$"Assets\VSharpTest\{filePath}");
        var originHorizontalProjects = originImage.GetHorizontalProjects();

        using var lineImage = isLog ? originImage.RAW12BitsPerPixelLogToLinear() : originImage.Copy();
        var lineHorizontalProjects = lineImage.GetHorizontalProjects();

        var vShapeWindowBySegments = Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            segmentCount,
            Generate.LinearRangeInt32(0, segmentCount - 1),
            originHorizontalProjects.Count);
        var startIndex = vShapeWindowBySegments.Regions[0].VMiddleIndex;
        var stopIndex = vShapeWindowBySegments.Regions[^1].VMiddleIndex;

        var smoothImageHorizontalProjects = SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.DenseOfEnumerable(lineHorizontalProjects)).ToArray();
        var smoothImageHorizontalProjectPoints = smoothImageHorizontalProjects.ToPoints();
        var (indexes, smoothImageHorizontalProjectMinimaPoints) = Extremumor.FindMinima(smoothImageHorizontalProjectPoints);
        var vSharpIndex = indexes
            .Where(t => startIndex <= t && t <= stopIndex)
            .OrderBy(t => smoothImageHorizontalProjectPoints[t].Y)
            .First();
        var vSharpPoints = smoothImageHorizontalProjectPoints[vSharpIndex];

        return new VSharpResult(
            originHorizontalProjects,
            lineHorizontalProjects,
            vShapeWindowBySegments,
            startIndex,
            stopIndex,
            smoothImageHorizontalProjectPoints,
            smoothImageHorizontalProjectMinimaPoints,
            vSharpIndex,
            vSharpPoints);
    }

    private static void RefreshVSharpPlot(
        string filePath,
        int segmentCount,
        int segmentIndex,
        bool isLog,
        ScatterPlotControl scatterPlotControl,
        VSharpResult vSharpResult,
        int imagePlotIndex,
        int windowPlotIndex)
    {
        var (originHorizontalProjects,
            lineHorizontalProjects,
            vShapeWindowBySegments,
            startIndex,
            stopIndex,
            smoothImageHorizontalProjectPoints,
            smoothImageHorizontalProjectM,
            vSharpIndex,
            vSharpPoints) = vSharpResult;

        scatterPlotControl.SetTitle(imagePlotIndex, $"{nameof(filePath)}: {filePath}, {nameof(segmentCount)}: {segmentCount}, {nameof(segmentIndex)}: {segmentIndex}, {nameof(isLog)}: {isLog}");

        var scatterLine = scatterPlotControl.AddScatterLine(imagePlotIndex);
        scatterLine.Update(string.Empty, originHorizontalProjects.ToPoints(), Colors.Gray);
        scatterLine = scatterPlotControl.AddScatterLine(imagePlotIndex);
        scatterLine.Update(string.Empty, lineHorizontalProjects.ToPoints(), Colors.Aqua);
        scatterLine = scatterPlotControl.AddScatterLine(imagePlotIndex);
        scatterLine.Update(string.Empty, smoothImageHorizontalProjectPoints, Colors.Brown);

        foreach (var (_, (vStartIndexTemp, vMiddleIndexTemp, vStopIndexTemp)) in vShapeWindowBySegments.Regions.Index())
        {
            var xLineTemp = scatterPlotControl.AddXLine(imagePlotIndex);
            xLineTemp.Update(string.Empty, vStartIndexTemp, Colors.LightSalmon);
            xLineTemp = scatterPlotControl.AddXLine(imagePlotIndex);
            xLineTemp.Update(string.Empty, vMiddleIndexTemp, Colors.Salmon);
            xLineTemp = scatterPlotControl.AddXLine(imagePlotIndex);
            xLineTemp.Update(string.Empty, vStopIndexTemp, Colors.DarkSalmon);
        }

        var (vStartIndex, vMiddleIndex, vStopIndex) = vShapeWindowBySegments.Regions[segmentIndex];
        var xLine = scatterPlotControl.AddXLine(imagePlotIndex);
        xLine.Update(string.Empty, vStartIndex, Colors.DarkOrange);
        xLine.LineWidth = 5;
        xLine = scatterPlotControl.AddXLine(imagePlotIndex);
        xLine.Update(string.Empty, vMiddleIndex, Colors.DarkOrange);
        xLine.LineWidth = 5;
        xLine = scatterPlotControl.AddXLine(imagePlotIndex);
        xLine.Update(string.Empty, vStopIndex, Colors.DarkOrange);
        xLine.LineWidth = 5;

        xLine = scatterPlotControl.AddXLine(imagePlotIndex);
        xLine.Update(string.Empty, startIndex, Colors.Red);
        xLine.LineWidth = 5;
        xLine.LinePattern = LinePattern.Solid;
        xLine = scatterPlotControl.AddXLine(imagePlotIndex);
        xLine.Update(string.Empty, stopIndex, Colors.Red);
        xLine.LineWidth = 5;
        xLine.LinePattern = LinePattern.Solid;

        xLine = scatterPlotControl.AddXLine(imagePlotIndex);
        xLine.Update(string.Empty, vSharpIndex, Colors.DarkRed);
        xLine.LineWidth = 5;
        xLine.LinePattern = LinePattern.Solid;
        var scatterMarkers = scatterPlotControl.AddScatterMarkers(imagePlotIndex);
        scatterMarkers.Update(string.Empty, smoothImageHorizontalProjectM, Colors.Blue, MarkerShape.FilledTriangleDown);
        scatterMarkers.MarkerSize = 20;
        scatterMarkers = scatterPlotControl.AddScatterMarkers(imagePlotIndex);
        scatterMarkers.Update(vSharpPoints.ToString("0.######"), [vSharpPoints], Colors.DarkBlue, MarkerShape.FilledSquare);
        scatterMarkers.MarkerSize = 40;

        scatterPlotControl.SetTitle(windowPlotIndex, nameof(vShapeWindowBySegments.Window));

        scatterLine = scatterPlotControl.AddScatterLine(windowPlotIndex);
        scatterLine.Update(string.Empty, vShapeWindowBySegments.Window.ToPoints(), Colors.Gray);
    }

    private sealed record VSharpResult(
        IReadOnlyList<double> OriginHorizontalProjects,
        IReadOnlyList<double> LineHorizontalProjects,
        (double[] Window, (int VStartIndex, int VMiddleIndex, int VStopIndex)[] Regions) VShapeWindowBySegments,
        int StartIndex,
        int StopIndex,
        IReadOnlyList<Point> SmoothImageHorizontalProjectPoints,
        IReadOnlyList<Point> SmoothImageHorizontalProjectMinimaPoints,
        int VSharpIndex,
        Point VSharpPoints);
}