// #define VSharpTest

using AwesomeAssertions;
using CommunityToolkit.Diagnostics;
using Core.Models.Models.AOD.Uniformity;
using Core.Utilities;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
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
using System.Windows.Controls;
using System.Windows.Data;
using Core.Utilities.WPF.Converters;
using ScottPlot.MultiplotLayouts;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.ScottPlot.WPF.Extensions;
#endif

namespace CugaCalibrationUnitTest;

#if !VSharpTest
// ReSharper disable  UnusedVariable
// ReSharper disable UnusedMember.Local
#endif
public class VSharpTest
{
    [Theory]
    [InlineData("test_1_4_true.raw", 11, 4, true, 786)]
    [InlineData("test_1_6_true.raw", 11, 6, true, 435)]
    [InlineData("test_1_1_false.raw", 5, 1, false, 1049)]
    [InlineData("test_1_3_false.raw", 5, 3, false, 347)]
    [InlineData("20260315_1116_0_0_1_short_001000_PMT08-CH3_8.raw", 30, 14, true, 765)]
    [InlineData("20260315_1119_0_0_1_short_001000_PMT08-CH3_8.raw", 30, 15, true, 826)]
    [InlineData("20260315_1107_0_0_1_short_001000_PMT08-CH3_8.raw", 8, 3, true, 685)]
    [InlineData("20260315_1110_0_0_1_short_001000_PMT08-CH3_8.raw", 8, 4, true, 913)]
    [InlineData("20260315_1098_0_0_1_short_001000_PMT08-CH3_8.raw", 20, 9, true, 753)]
    [InlineData("20260315_1101_0_0_1_short_001000_PMT08-CH3_8.raw", 20, 10, true, 845)]
    [InlineData("20260315_780_0_0_1_short_001000_PMT08-CH3_8.raw", 8, 3, true, 950)]
    [InlineData("20260315_783_0_0_1_short_001000_PMT08-CH3_8.raw", 8, 4, true, 737)]
#pragma warning disable IDE0079
#pragma warning disable xUnit1026
    public void TestVSharp1(string filePath, int segmentCount, int segmentIndex, bool isLog, int expectedIndex)
#pragma warning restore xUnit1026
#pragma warning restore IDE0079
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
    [InlineData(5002, "20260315_1116_0_0_1_short_001000_PMT08-CH3_8.raw", "20260315_1119_0_0_1_short_001000_PMT08-CH3_8.raw", 30, 14, 15, "20260315_1122_0_0_1_short_001000_PMT08-CH3_8.raw", true, new[] { 30, 89, 147, 202, 258, 315, 373, 429, 485, 542, 600, 656, 714, 771, 828, 885, 942, 997, 1054, 1110, 1168, 1224, 1282, 1338, 1396, 1454, 1510, 1567, 1624, 1683 })]
    [InlineData(5002, "20260315_1107_0_0_1_short_001000_PMT08-CH3_8.raw", "20260315_1110_0_0_1_short_001000_PMT08-CH3_8.raw", 8, 3, 4, "20260315_1113_0_0_1_short_001000_PMT08-CH3_8.raw", true, new[] { 103, 317, 531, 747, 961, 1175, 1390, 1604 })]
    [InlineData(5002, "20260315_1098_0_0_1_short_001000_PMT08-CH3_8.raw", "20260315_1101_0_0_1_short_001000_PMT08-CH3_8.raw", 20, 9, 10, "20260315_1104_0_0_1_short_001000_PMT08-CH3_8.raw", true, new[] { 45, 132, 216, 302, 387, 473, 558, 644, 729, 815, 901, 985, 1071, 1156, 1241, 1326, 1412, 1498, 1583, 1669 })]
    [InlineData(4669, "20260315_780_0_0_1_short_001000_PMT08-CH3_8.raw", "20260315_783_0_0_1_short_001000_PMT08-CH3_8.raw", 8, 3, 4, "20260315_786_0_0_1_short_001000_PMT08-CH3_8.raw", true, new[] { 95, 289, 484, 681, 876, 1070, 1266, 1461 })]
    public void TestVSharps(
        int prescanAODWaveformCount,
        string startFilePath,
        string stopFilePath,
        int segmentCount,
        int startSegmentIndex,
        int stopSegmentIndex,
        string filePath,
        bool isLog,
        IReadOnlyList<int> expectedSegmentIndexes)
    {
        var segmentIndexes = Generate.LinearRangeInt32(0, segmentCount - 1);

        #region Forward / Reverse VSharp

        var startVSharpResult = GetVSharp(startFilePath, segmentCount, isLog);
        var stopVSharpResult = GetVSharp(stopFilePath, segmentCount, isLog);

        #endregion

        #region Prescan Index to Pixel Index

        var isReverse = startVSharpResult.VSharpIndex > stopVSharpResult.VSharpIndex;

        var linearSplinePrescan = LinearSpline.InterpolateSorted(
            [
                Generate.LinearVShapeWindowBySegments(
                    1d,
                    0d,
                    segmentCount,
                    startSegmentIndex,
                    prescanAODWaveformCount).Region.VMiddleIndex,
                Generate.LinearVShapeWindowBySegments(
                    1d,
                    0d,
                    segmentCount,
                    stopSegmentIndex,
                    prescanAODWaveformCount).Region.VMiddleIndex
            ],
            [
                isReverse ? startVSharpResult.LineHorizontalProjects.Count - 1 - startVSharpResult.VSharpIndex : startVSharpResult.VSharpIndex,
                isReverse ? stopVSharpResult.LineHorizontalProjects.Count - 1 - stopVSharpResult.VSharpIndex : stopVSharpResult.VSharpIndex
            ]);

        var prescanToImageIndexMappings = Generate.LinearRangeInt32(0, prescanAODWaveformCount - 1)
            .Select(t => new Point(t, linearSplinePrescan.Interpolate(t)))
            .ToArray();
        var startPrescanAODWaveformIndex = (int)prescanToImageIndexMappings.First(t => 0 <= t.Y && t.Y <= startVSharpResult.LineHorizontalProjects.Count - 1).X;
        if (startPrescanAODWaveformIndex < 0) startPrescanAODWaveformIndex = 0;
        var stopPrescanAODWaveformIndex = (int)prescanToImageIndexMappings.Last(t => 0 <= t.Y && t.Y <= stopVSharpResult.LineHorizontalProjects.Count - 1).X;
        if (stopPrescanAODWaveformIndex > prescanAODWaveformCount - 1) stopPrescanAODWaveformIndex = prescanAODWaveformCount - 1;

        var window = Generate.Repeat(prescanAODWaveformCount, 1d);

        var prescanAODWaveformIndexes = Generate.LinearRangeInt32(0, prescanAODWaveformCount - 1).AsSpan()[startPrescanAODWaveformIndex..(stopPrescanAODWaveformIndex + 1)].ToArray();

        var (windowTemp, regionTemps) = Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            segmentCount,
            segmentIndexes,
            prescanAODWaveformIndexes.Length);

        Vector<double>.Build.Dense(window).SetSubVectorRange(
            prescanAODWaveformIndexes[0],
            prescanAODWaveformIndexes[^1],
            Vector<double>.Build.Dense(windowTemp));

        var regions = regionTemps
            .Select(t => (VStartIndex: t.VStartIndex + startPrescanAODWaveformIndex, VMiddleIndex: t.VMiddleIndex + startPrescanAODWaveformIndex, VStopIndex: t.VStopIndex + startPrescanAODWaveformIndex))
            .ToArray();

        #endregion

        #region Mapping VShape

        using var originImage = RawImageFactory.CreateImage(@$"Assets\VSharpTest\{filePath}");
        var originHorizontalProjects = isReverse ? originImage.GetHorizontalProjects().Reverse().ToArray() : originImage.GetHorizontalProjects();

        using var lineImage = isLog ? originImage.RAW12BitsPerPixelLogToLinear() : originImage.Copy();
        var lineHorizontalProjects = isReverse ? lineImage.GetHorizontalProjects().Reverse().ToArray() : lineImage.GetHorizontalProjects();

        var smoothImageHorizontalProjects = SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.Dense([..lineHorizontalProjects])).ToArray();
        var smoothImageHorizontalProjectPoints = smoothImageHorizontalProjects.ToPoints();

        var (indexes, smoothImageHorizontalProjectMinimaPoints) = Extremumor.FindMinima(smoothImageHorizontalProjectPoints);

        var imageRegions = regions
            .Select(t =>
            {
                var (vStartIndex, vMiddleIndex, vStopIndex) = t;

                return (VStartIndex: (int)Math.Clamp(Math.Floor(prescanToImageIndexMappings[vStartIndex].Y), 0, lineHorizontalProjects.Count - 1),
                    VMiddleIndex: (int)Math.Clamp(Math.Round(prescanToImageIndexMappings[vMiddleIndex].Y), 0, lineHorizontalProjects.Count - 1),
                    VStopIndex: (int)Math.Clamp(Math.Ceiling(prescanToImageIndexMappings[vStopIndex].Y), 0, lineHorizontalProjects.Count - 1));
            }).ToArray();
        var vSharps = imageRegions
            .Select(t =>
            {
                var (startIndex, _, stopIndex) = t;

                var vSharpIndex = indexes
                    .Where(tt => startIndex <= tt && tt <= stopIndex)
                    .OrderBy(tt => smoothImageHorizontalProjects[tt])
                    .First();
                var vSharpPoints = smoothImageHorizontalProjectPoints[vSharpIndex];

                return (VSharpIndex: vSharpIndex, VSharpPoints: vSharpPoints);
            })
            .ToArray();

        vSharps.Select(t => t.VSharpIndex).Should()
            .BeEquivalentTo(expectedSegmentIndexes, options => options.WithStrictOrdering());

        var windowItem = new AODUniformityDTO.WindowItem
        {
            ImageHorizontalProjects = lineHorizontalProjects
        };
        windowItem.CalculateHorizontalProjectMinPixels(regions, prescanToImageIndexMappings);

        vSharps.Select(t => t.VSharpIndex).Should()
            .BeEquivalentTo(windowItem.HorizontalProjectMinPixels, options => options.WithStrictOrdering());
        smoothImageHorizontalProjects.Should()
            .BeEquivalentTo(windowItem.SmoothImageHorizontalProjects, options => options.WithStrictOrdering());

        #endregion

        #region Pixel Index to Prescan Index

        var mappingMinIndexes = regions.Select(t => t.VMiddleIndex).ToArray();
        var imageHorizontalProjectMinIndexes = vSharps.Select(t => t.VSharpIndex).ToArray();

        var mappingList = new List<AODUniformityDTO.Mapping>();

        var linearSpline = LinearSpline.InterpolateSorted([.. imageHorizontalProjectMinIndexes], [.. mappingMinIndexes]);
        for (var i = 0; i < lineHorizontalProjects.Count; i++)
        {
            var mappingIndex = linearSpline.Interpolate(i);

            var indexOf = imageHorizontalProjectMinIndexes.IndexOf(i);
            var isNotLinearSpline = indexOf != -1;

            mappingList.Add(new AODUniformityDTO.Mapping
            {
                IsNotLinearSpline = isNotLinearSpline,
                ImageHorizontalProjectIndex = i,
                LinearSplineMappingIndex = mappingIndex
            });
        }

        #endregion

        #region Pixel Index to Prescan Index Group

        var leftMappingMinIndexes = Generate.LinearRangeInt32(0, mappingList[0].MappingIndex - 1);
        for (var i = 0; i < mappingList.Count; i++)
        {
            if (i == mappingList.Count - 1)
                mappingList[i].MappingIndices = [.. leftMappingMinIndexes, .. Generate.LinearRangeInt32(mappingList[^1].MappingIndex, window.Length - 1)];
            else
            {
                var mappingIndexes = Generate.LinearRangeInt32(mappingList[i].MappingIndex, mappingList[i + 1].MappingIndex);
                mappingIndexes = mappingIndexes.Except((int[])[mappingList[i].MappingIndex, mappingList[i + 1].MappingIndex]).ToArray();

                var chunks = mappingIndexes.ChunkSplitEvenly(2).ToArray();

                mappingList[i].MappingIndices = [.. leftMappingMinIndexes, mappingList[i].MappingIndex, .. chunks[0]];

                leftMappingMinIndexes = chunks.ElementAtOrDefault(1) ?? [];
            }
        }

        #endregion

#if VSharpTest
        var thread = new Thread(() =>
        {
            #region Plot

            var scatterPlotControl = new ScatterPlotControl();
            scatterPlotControl.Configure(new Rows(), 8);

            #endregion

            #region Pixel Index to Prescan Index Group

            var dataGrid = new DataGrid
            {
                ItemsSource = mappingList,
                AutoGenerateColumns = false,
                IsReadOnly = true
            };

            dataGrid.Columns.Add(new DataGridCheckBoxColumn { Header = nameof(AODUniformityDTO.Mapping.IsNotLinearSpline), Binding = new Binding(nameof(AODUniformityDTO.Mapping.IsNotLinearSpline)) });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = nameof(AODUniformityDTO.Mapping.ImageHorizontalProjectIndex), Binding = new Binding(nameof(AODUniformityDTO.Mapping.ImageHorizontalProjectIndex)) });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = nameof(AODUniformityDTO.Mapping.LinearSplineMappingIndex), Binding = new Binding(nameof(AODUniformityDTO.Mapping.LinearSplineMappingIndex)) { StringFormat = "0.###" } });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = nameof(AODUniformityDTO.Mapping.MappingIndex), Binding = new Binding(nameof(AODUniformityDTO.Mapping.MappingIndex)) });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = nameof(AODUniformityDTO.Mapping.MappingIndices), Binding = new Binding(nameof(AODUniformityDTO.Mapping.MappingIndices)) { Converter = new IntsToStringConverter() } });

            #endregion

            #region WPF Window

            var wpfWindow = new Window { Title = nameof(TestVSharp1), WindowState = WindowState.Maximized };

            var tabControl = new TabControl();
            tabControl.Items.Add(new TabItem
            {
                Header = "Plot",
                Content = scatterPlotControl
            });
            tabControl.Items.Add(new TabItem
            {
                Header = "Mapping Data",
                Content = dataGrid
            });
            wpfWindow.Content = tabControl;

            #endregion

            #region Forward / Reverse VSharp

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

            #region Mapping VShape

            scatterPlotControl.SetTitle(4, $"{nameof(segmentCount)}: {segmentCount}, {nameof(isLog)}: {isLog}, {nameof(filePath)}: {filePath}, {nameof(segmentIndexes)}: {string.Join(",", segmentIndexes)}");

            var scatterLine = scatterPlotControl.AddScatterLine(4);
            scatterLine.Update(string.Empty, originHorizontalProjects.ToPoints(), Colors.Gray);
            scatterLine = scatterPlotControl.AddScatterLine(4);
            scatterLine.Update(string.Empty, lineHorizontalProjects.ToPoints(), Colors.Aqua);
            scatterLine = scatterPlotControl.AddScatterLine(4);
            scatterLine.Update(string.Empty, smoothImageHorizontalProjectPoints, Colors.Brown);

            foreach (var (_, (vStartIndexTemp, vMiddleIndexTemp, vStopIndexTemp)) in imageRegions.Index())
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

            #region Window

            scatterPlotControl.SetTitle(5, nameof(window));

            scatterLine = scatterPlotControl.AddScatterLine(5);
            scatterLine.Update(string.Empty, window.ToPoints(), Colors.Gray);

            #endregion

            #endregion

            #region Prescan Index to Pixel Index

            scatterPlotControl.SetTitle(6, "pixel/prescan");

            scatterLine = scatterPlotControl.AddScatterLine(6);
            scatterLine.Update(string.Empty, prescanToImageIndexMappings, Colors.Gray);

            #endregion

            #region Pixel Index to Prescan Index

            scatterPlotControl.SetTitle(7, "prescan/pixel");

            var isNotLinearSplineImageHorizontalProjectIndexes = mappingList.Where(t => t.IsNotLinearSpline).Select(t => t.ImageHorizontalProjectIndex).ToArray();
            var isNotLinearSplineMappingIndexes = mappingList.Where(t => t.IsNotLinearSpline).Select(t => t.MappingIndex).ToArray();
            var (slope, intercept, rSquared, yPredicted) = PolynomialCurve.Fit1(
                Vector<double>.Build.Dense([.. isNotLinearSplineImageHorizontalProjectIndexes]),
                Vector<double>.Build.Dense([.. isNotLinearSplineMappingIndexes]));

            var scatterLineMapping = scatterPlotControl.GetOrAddScatterLine(7, "Origin", [.. isNotLinearSplineImageHorizontalProjectIndexes.Index().Select(t => new Point(t.Item, isNotLinearSplineMappingIndexes[t.Index]))]);
            scatterLineMapping.MarkerSize = 10;

            scatterLineMapping = scatterPlotControl.GetOrAddScatterLine(7, $"Fit Curve: y = {slope:0.######}x + {intercept:0.######} r^2 = {rSquared:0.######}",
                [.. isNotLinearSplineImageHorizontalProjectIndexes.Index().Select(t => new Point(t.Item, yPredicted[t.Index]))]);
            scatterLineMapping.MarkerSize = 10;

            var isLinearSplineImageHorizontalProjectIndexes = mappingList.Where(t => t.IsNotLinearSpline == false).Select(t => t.ImageHorizontalProjectIndex).ToArray();
            var isLinearSplineLinearSplineMappingIndexes = mappingList.Where(t => t.IsNotLinearSpline == false).Select(t => t.LinearSplineMappingIndex).ToArray();
            var isLinearSplineMappingIndexes = mappingList.Where(t => t.IsNotLinearSpline == false).Select(t => t.MappingIndex).ToArray();

            var scatterMarkersMapping = scatterPlotControl.GetOrAddScatterMarkers(7, "Linear Spline", [.. isLinearSplineImageHorizontalProjectIndexes.Index().Select(t => new Point(t.Item, isLinearSplineLinearSplineMappingIndexes[t.Index]))], Colors.DarkRed,
                MarkerShape.FilledSquare);
            scatterMarkersMapping.MarkerSize = 5;
            scatterMarkersMapping = scatterPlotControl.GetOrAddScatterMarkers(7, "Round Linear Spline", [.. isLinearSplineImageHorizontalProjectIndexes.Index().Select(t => new Point(t.Item, isLinearSplineMappingIndexes[t.Index]))], Colors.Red,
                MarkerShape.FilledDiamond);
            scatterMarkersMapping.MarkerSize = 5;

            #endregion

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

        var smoothImageHorizontalProjects = SavitzkyGolayFilter.Smooth(3, 51, Vector<double>.Build.Dense([..lineHorizontalProjects])).ToArray();
        var smoothImageHorizontalProjectPoints = smoothImageHorizontalProjects.ToPoints();

        var vShapeWindowBySegments = Generate.LinearVShapeWindowBySegments(
            1d,
            0d,
            segmentCount,
            Generate.LinearRangeInt32(0, segmentCount - 1),
            lineHorizontalProjects.Count);
        var startIndex = vShapeWindowBySegments.Regions[0].VMiddleIndex;
        var stopIndex = vShapeWindowBySegments.Regions[^1].VMiddleIndex;

        var (indexes, smoothImageHorizontalProjectMinimaPoints) = Extremumor.FindMinima(smoothImageHorizontalProjectPoints);
        var vSharpIndex = indexes
            .Where(t => startIndex <= t && t <= stopIndex)
            .OrderBy(t => smoothImageHorizontalProjects[t])
            .First();
        var vSharpPoints = smoothImageHorizontalProjectPoints[vSharpIndex];

        var windowItem = new AODUniformityDTO.WindowItem { ImageHorizontalProjects = lineHorizontalProjects };
        windowItem.CalculateHorizontalProjectMinPixel(segmentCount);

        vSharpIndex.Should().Be(windowItem.HorizontalProjectMinPixel);
        smoothImageHorizontalProjects.Should()
            .BeEquivalentTo(windowItem.SmoothImageHorizontalProjects, options => options.WithStrictOrdering());

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

#if !VSharpTest
// ReSharper restore UnusedVariable
// ReSharper restore UnusedMember.Local
#endif