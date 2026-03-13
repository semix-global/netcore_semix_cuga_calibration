// #define VSharpTest

using AwesomeAssertions;
using CommunityToolkit.Diagnostics;
using Core.Utilities;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra;
using Xunit;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.ScottPlot.WPF.WPF;
using ScottPlot;
using Generate = MathNet.Numerics.Generate;
using Point = Net.Utilities.Models.Geometries.Point;

#if VSharpTest
using System.Windows;
using ScottPlot.MultiplotLayouts;
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
    [InlineData(4792, "test_1_4_true.raw", "test_1_6_true.raw", 11, 4, 6, "test_9_true.raw", new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, true, new[] { 71, 246, 420, 596, 773, 950, 1123, 1299, 1472 })]
    [InlineData(4792, "test_1_1_false.raw", "test_1_3_false.raw", 5, 1, 3, "test_4_false.raw", new[] { 0, 1, 2, 3 }, false, new[] { 160, 509, 863, 1213 })]
    public void TestVSharps(
        int prescanCount,
        string startFilePath,
        string stopFilePath,
        int segmentCount,
        int startSegmentIndex,
        int stopSegmentIndex,
        string filePath,
        IReadOnlyList<int> segmentIndexes,
        bool isLog,
        IReadOnlyList<int> expectedSegmentIndexes)
    {
        var startVSharpResult = GetVSharp(startFilePath, segmentCount, isLog);
        var stopVSharpResult = GetVSharp(stopFilePath, segmentCount, isLog);

        var isReverse = startVSharpResult.VSharpIndex > stopVSharpResult.VSharpIndex;

        var linearSpline = LinearSpline.InterpolateSorted(
            [
                Generate.LinearVShapeWindowBySegments(
                    1d,
                    0d,
                    segmentCount,
                    startSegmentIndex,
                    prescanCount).Region.VMiddleIndex,
                Generate.LinearVShapeWindowBySegments(
                    1d,
                    0d,
                    segmentCount,
                    stopSegmentIndex,
                    prescanCount).Region.VMiddleIndex
            ],
            [
                isReverse ? startVSharpResult.OriginHorizontalProjects.Count - 1 - startVSharpResult.VSharpIndex : startVSharpResult.VSharpIndex,
                isReverse ? stopVSharpResult.OriginHorizontalProjects.Count - 1 - stopVSharpResult.VSharpIndex : stopVSharpResult.VSharpIndex
            ]);

        var mappingPointList = new List<Point>();
        for (var i = 0; i < prescanCount; i++)
        {
            var mappingIndex = linearSpline.Interpolate(i);

            mappingPointList.Add(new Point(i, mappingIndex));
        }

        var vShapePrescanWindowBySegments = Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            segmentCount,
            segmentIndexes,
            4792
        );

        var regions = vShapePrescanWindowBySegments.Regions
            .Select(t =>
            {
                var (vStartIndex, vMiddleIndex, vStopIndex) = t;

                return (VStartIndex: (int)Math.Clamp(Math.Floor(mappingPointList[vStartIndex].Y), 0, startVSharpResult.OriginHorizontalProjects.Count - 1),
                    VMiddleIndex: (int)Math.Clamp(Math.Round(mappingPointList[vMiddleIndex].Y), 0, startVSharpResult.OriginHorizontalProjects.Count - 1),
                    VStopIndex: (int)Math.Clamp(Math.Ceiling(mappingPointList[vStopIndex].Y), 0, startVSharpResult.OriginHorizontalProjects.Count - 1));
            })
            .ToArray();

        using var originImage = RawImageFactory.CreateImage(@$"Assets\VSharpTest\{filePath}");
        var originHorizontalProjects = isReverse ? originImage.GetHorizontalProjects().Reverse().ToArray() : originImage.GetHorizontalProjects();

        using var lineImage = isLog ? originImage.RAW12BitsPerPixelLogToLinear() : originImage.Copy();
        var lineHorizontalProjects = isReverse ? lineImage.GetHorizontalProjects().Reverse().ToArray() : lineImage.GetHorizontalProjects();

        var smoothImageHorizontalProjects = SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.DenseOfEnumerable(lineHorizontalProjects)).ToArray();
        var smoothImageHorizontalProjectPoints = smoothImageHorizontalProjects.ToPoints();

        var (indexes, smoothImageHorizontalProjectMinimaPoints) = Extremumor.FindMinima(smoothImageHorizontalProjectPoints);

        var vSharps = regions.Select(t =>
        {
            var (startIndex, _, stopIndex) = t;
            var vSharpIndex = indexes
                .Where(tt => startIndex <= tt && tt <= stopIndex)
                .OrderBy(tt => smoothImageHorizontalProjectPoints[tt].Y)
                .First();
            var vSharpPoints = smoothImageHorizontalProjectPoints[vSharpIndex];

            return (VSharpIndex: vSharpIndex, VSharpPoints: vSharpPoints);
        }).ToArray();

        vSharps.Select(t => t.VSharpIndex).Should()
            .BeEquivalentTo(expectedSegmentIndexes, options => options.WithStrictOrdering());

#if VSharpTest
        var thread = new Thread(() =>
        {
            var wpfWindow = new Window { Title = nameof(TestVSharp1), WindowState = WindowState.Maximized };

            var scatterPlotControl = new ScatterPlotControl();
            wpfWindow.Content = scatterPlotControl;

            scatterPlotControl.Configure(new Rows(), 7);

            #region forward / reverse

            RefreshVSharpPlot(
                startFilePath,
                segmentCount,
                startSegmentIndex,
                isLog,
                scatterPlotControl,
                startVSharpResult,
                0,
                1);

            RefreshVSharpPlot(
                stopFilePath,
                segmentCount,
                stopSegmentIndex,
                isLog,
                scatterPlotControl,
                stopVSharpResult,
                2,
                3);

            #endregion

            scatterPlotControl.SetTitle(4, $"{nameof(segmentCount)}: {segmentCount}, {nameof(isLog)}: {isLog}, {nameof(filePath)}: {filePath}, {nameof(segmentIndexes)}: {string.Join(",", segmentIndexes)}");

            var scatterLine = scatterPlotControl.AddScatterLine(4);
            scatterLine.Update(string.Empty, originHorizontalProjects.ToPoints(), Colors.Gray);
            scatterLine = scatterPlotControl.AddScatterLine(4);
            scatterLine.Update(string.Empty, lineHorizontalProjects.ToPoints(), Colors.Aqua);
            scatterLine = scatterPlotControl.AddScatterLine(4);
            scatterLine.Update(string.Empty, smoothImageHorizontalProjectPoints, Colors.Brown);

            foreach (var (_, (vStartIndexTemp, vMiddleIndexTemp, vStopIndexTemp)) in regions.Index())
            {
                var xLineTemp = scatterPlotControl.AddXLine(4);
                xLineTemp.Update(string.Empty, vStartIndexTemp, Colors.LightSalmon);
                xLineTemp = scatterPlotControl.AddXLine(4);
                xLineTemp.Update(string.Empty, vMiddleIndexTemp, Colors.Salmon);
                xLineTemp = scatterPlotControl.AddXLine(4);
                xLineTemp.Update(string.Empty, vStopIndexTemp, Colors.DarkSalmon);
            }

            var scatterMarkers = scatterPlotControl.AddScatterMarkers(4);
            scatterMarkers.Update(string.Empty, smoothImageHorizontalProjectMinimaPoints, Colors.Blue, MarkerShape.FilledTriangleDown);
            scatterMarkers.MarkerSize = 20;

            foreach (var (vSharpIndex, vSharpPoints) in vSharps)
            {
                var xLine = scatterPlotControl.AddXLine(4);
                xLine.Update(string.Empty, vSharpIndex, Colors.DarkRed);
                xLine.LineWidth = 5;
                xLine.LinePattern = LinePattern.Solid;

                scatterMarkers = scatterPlotControl.AddScatterMarkers(4);
                scatterMarkers.Update(string.Empty, [vSharpPoints], Colors.DarkBlue, MarkerShape.FilledSquare);
                scatterMarkers.MarkerSize = 40;
            }

            scatterPlotControl.SetTitle(5, nameof(vShapePrescanWindowBySegments.Window));

            scatterLine = scatterPlotControl.AddScatterLine(5);
            scatterLine.Update(string.Empty, vShapePrescanWindowBySegments.Window.ToPoints(), Colors.Gray);

            scatterPlotControl.SetTitle(6, nameof(vShapePrescanWindowBySegments.Window));

            scatterLine = scatterPlotControl.AddScatterLine(6);
            scatterLine.Update(string.Empty, mappingPointList, Colors.Gray);

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

        var smoothImageHorizontalProjects = SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.DenseOfEnumerable(lineHorizontalProjects)).ToArray();
        var smoothImageHorizontalProjectPoints = smoothImageHorizontalProjects.ToPoints();

        var vShapeWindowBySegments = Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            segmentCount,
            Generate.LinearRangeInt32(0, segmentCount - 1),
            originHorizontalProjects.Count);
        var startIndex = vShapeWindowBySegments.Regions[0].VMiddleIndex;
        var stopIndex = vShapeWindowBySegments.Regions[^1].VMiddleIndex;

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
            smoothImageHorizontalProjectMinimaPoints,
            vSharpIndex,
            vSharpPoints) = vSharpResult;

        scatterPlotControl.SetTitle(imagePlotIndex, $"{nameof(filePath)}: {filePath}, {nameof(segmentCount)}: {segmentCount}, {nameof(segmentIndex)}: {segmentIndex}, {nameof(isLog)}: {isLog}, {vSharpPoints.ToString("0.######")}");

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
        scatterMarkers.Update(string.Empty, smoothImageHorizontalProjectMinimaPoints, Colors.Blue, MarkerShape.FilledTriangleDown);
        scatterMarkers.MarkerSize = 20;
        scatterMarkers = scatterPlotControl.AddScatterMarkers(imagePlotIndex);
        scatterMarkers.Update(string.Empty, [vSharpPoints], Colors.DarkBlue, MarkerShape.FilledSquare);
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