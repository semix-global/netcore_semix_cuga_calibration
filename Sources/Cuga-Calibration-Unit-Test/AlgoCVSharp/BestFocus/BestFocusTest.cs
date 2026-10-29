#define BestFocusTest

using AwesomeAssertions;
using Core.Models.Enums.Algorithm;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Xunit.Abstractions;
using AlgoCvImage = algocv_sharp.Image;
using BestFocusConfig = algocv_sharp.BestFocusConfig;
using BestFocusEngine = algocv_sharp.BestFocusEngine;
using FocusMetricArrayInfo = algocv_sharp.FocusMetricArrayInfo;
using XAxis = algocv_sharp.XAxis;

#if BestFocusTest
using System.Windows;
using System.Windows.Controls;
using ScottPlot.MultiplotLayouts;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.WPF.V2;
#endif

namespace CugaCalibrationUnitTest.AlgoCVSharp.BestFocus;

public class BestFocusTest(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData(@"Assets\20260508_214_0_0_1_short_011375_PMT08-CH2_8.raw", AlgorithmBestFocusTypeEnum.DSW065)]
    [InlineData(@"Assets\BestFocus16.3.raw", AlgorithmBestFocusTypeEnum.DSW)]
    public void GetBestFocus_ShouldReturnValidBestFocus(string filePath, AlgorithmBestFocusTypeEnum bestFocusType)
    {
        // 按生产路径加载 raw 文件并线性化（同 CalibrationAlgorithmServiceMockImpl.GetBestFocus）
        using var hImage = RAWImageFactory.CreateImage(filePath, true);
        using var image = hImage.ToBitmapImage();

        const double startEcs = 0d;
        const double stopEcs = 100d;

        var bestFocus = image.ToBestFocus(startEcs, stopEcs, bestFocusType, Guid.NewGuid());

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

        // X/YStrehlRatioFitPoints 生产端暂未实现（见 CalibrationAlgorithmServiceImpl.ConvertToBestFocus 注释），不再断言

        bestFocus.XStrehlRatioColumnPoints.Should().NotBeEmpty();
        bestFocus.YStrehlRatioColumnPoints.Should().NotBeEmpty();

        // DSW16.3输出的是清晰度分数，不能用strehl的评价方式
        // bestFocus.BestXStrehlRatioPoint.Y.Should().BeInRange(0, 1);
        // bestFocus.BestYStrehlRatioPoint.Y.Should().BeInRange(0, 1);

        bestFocus.BestXStrehlRatioECS.Should().BeInRange(startEcs, stopEcs);
        bestFocus.BestYStrehlRatioECS.Should().BeInRange(startEcs, stopEcs);

        bestFocus.XIntraRibbonFieldsPoints.Should().NotBeEmpty();
        bestFocus.YIntraRibbonFieldsPoints.Should().NotBeEmpty();

        bestFocus.XFieldTiltPoints.Should().NotBeEmpty();
        bestFocus.YFieldTiltPoints.Should().NotBeEmpty();

        // bestFocus.XFieldTiltFitRSquared.Should().BeInRange(0, 1);
        // bestFocus.YFieldTiltFitRSquared.Should().BeInRange(0, 1);

#if BestFocusTest
        var thread = new Thread(() =>
        {
            var window = new Window { Title = nameof(GetBestFocus_ShouldReturnValidBestFocus), WindowState = WindowState.Maximized };

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

    /// <summary>
    /// 校验 1.3.0 原生结构体内存布局与托管侧一致.
    /// </summary>
    [Fact]
    public void CheckStructSize_ShouldNotThrow()
    {
        var act = () => BestFocusEngine.CheckStructSize();

        act.Should().NotThrow();
    }

    /// <summary>
    /// Process 不传 xAxis 时应与显式传 XAxis.Row 结果一致(默认值即既有口径).
    /// </summary>
    [Theory]
    [InlineData(@"Assets\20260508_214_0_0_1_short_011375_PMT08-CH2_8.raw")]
    public void Process_DefaultXAxis_ShouldEqualRow(string filePath)
    {
        using var image = LoadAlgoCvImage(filePath, true);
        using var defaultEngine = new BestFocusEngine(new BestFocusConfig());
        using var rowEngine = new BestFocusEngine(new BestFocusConfig());
        using var defaultResult = defaultEngine.Process(image);
        using var rowResult = rowEngine.Process(image, XAxis.Row);

        AssertSameResult(defaultResult.metric_array_info_y, rowResult.metric_array_info_y);
        AssertSameResult(defaultResult.metric_array_info_x, rowResult.metric_array_info_x);
    }

    /// <summary>
    /// ProcessDSW163 不传 xAxis 时应与显式传 XAxis.Row 结果一致.
    /// </summary>
    [Theory]
    [InlineData(@"Assets\BestFocus16.3.raw")]
    public void ProcessDSW163_DefaultXAxis_ShouldEqualRow(string filePath)
    {
        using var image = LoadAlgoCvImage(filePath, true);
        using var defaultResult = BestFocusEngine.ProcessDSW163(image);
        using var rowResult = BestFocusEngine.ProcessDSW163(image, XAxis.Row);

        AssertSameResult(defaultResult.metric_array_info_y, rowResult.metric_array_info_y);
        AssertSameResult(defaultResult.metric_array_info_x, rowResult.metric_array_info_x);
    }

    /// <summary>
    /// Process: 转置竖图配 XAxis.Row 与正向图配 XAxis.Col 应等价(形状一致, ribbon 拟合 k/b 近似相等).
    /// </summary>
    [Theory]
    [InlineData(@"Assets\20260508_214_0_0_1_short_011375_PMT08-CH2_8.raw")]
    public void Process_TransposedRowAndUprightCol_ShouldBeEquivalent(string filePath)
    {
        using var transposedImage = LoadAlgoCvImage(filePath, true);
        using var uprightImage = LoadAlgoCvImage(filePath, false);
        using var rowEngine = new BestFocusEngine(new BestFocusConfig());
        using var colEngine = new BestFocusEngine(new BestFocusConfig());
        using var rowResult = rowEngine.Process(transposedImage, XAxis.Row);
        using var colResult = colEngine.Process(uprightImage, XAxis.Col);

        DumpImageSize(filePath);

        Dump("Row.y", rowResult.metric_array_info_y);
        Dump("Col.y", colResult.metric_array_info_y);
        Dump("Row.x", rowResult.metric_array_info_x);
        Dump("Col.x", colResult.metric_array_info_x);

        AssertEquivalentResult(rowResult.metric_array_info_y, colResult.metric_array_info_y);
        AssertEquivalentResult(rowResult.metric_array_info_x, colResult.metric_array_info_x);
    }

    /// <summary>
    /// ProcessDSW163: 转置竖图配 XAxis.Row 与正向图配 XAxis.Col 应等价.
    /// </summary>
    [Theory]
    [InlineData(@"Assets\BestFocus16.3.raw")]
    public void ProcessDSW163_TransposedRowAndUprightCol_ShouldBeEquivalent(string filePath)
    {
        using var transposedImage = LoadAlgoCvImage(filePath, true);
        using var uprightImage = LoadAlgoCvImage(filePath, false);
        using var rowResult = BestFocusEngine.ProcessDSW163(transposedImage, XAxis.Row);
        using var colResult = BestFocusEngine.ProcessDSW163(uprightImage, XAxis.Col);

        DumpImageSize(filePath);

        Dump("Row.y", rowResult.metric_array_info_y);
        Dump("Col.y", colResult.metric_array_info_y);
        Dump("Row.x", rowResult.metric_array_info_x);
        Dump("Col.x", colResult.metric_array_info_x);

        AssertEquivalentResult(rowResult.metric_array_info_y, colResult.metric_array_info_y);
        AssertEquivalentResult(rowResult.metric_array_info_x, colResult.metric_array_info_x);
    }

    /// <summary>
    /// ProcessDSW163 传入 null 图像时应抛 ArgumentNullException.
    /// </summary>
    [Fact]
    public void ProcessDSW163_NullImage_ShouldThrowArgumentNullException()
    {
        var act = () => BestFocusEngine.ProcessDSW163(null!, XAxis.Col);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// 引擎 Dispose 之后调用 Process 应抛 ObjectDisposedException.
    /// </summary>
    [Theory]
    [InlineData(@"Assets\20260508_214_0_0_1_short_011375_PMT08-CH2_8.raw")]
    public void Process_AfterDispose_ShouldThrowObjectDisposedException(string filePath)
    {
        using var image = LoadAlgoCvImage(filePath, false);
        var engine = new BestFocusEngine(new BestFocusConfig());
        engine.Dispose();

        var act = () => engine.Process(image, XAxis.Col);

        act.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// 按生产路径加载 raw 并转换为 algocv 图像.
    /// </summary>
    /// <param name="filePath">raw 文件路径.</param>
    /// <param name="isRawSource">true 转置为竖图, false 保持正向.</param>
    private static AlgoCvImage LoadAlgoCvImage(string filePath, bool isRawSource)
    {
        using var hImage = RAWImageFactory.CreateImage(filePath, true);
        using var bitmapImage = hImage.ToBitmapImage();

        var uprightImage = bitmapImage.ToAlgoCVImage();
        if (isRawSource == false)
        {
            return uprightImage;
        }

        using (uprightImage)
        {
            return algocv_sharp.ImageProc.Transpose(uprightImage);
        }
    }

    /// <summary>
    /// 断言两个方向的结果完全一致.
    /// </summary>
    private static void AssertSameResult(FocusMetricArrayInfo expected, FocusMetricArrayInfo actual)
    {
        actual.metric_array_rows.Should().Be(expected.metric_array_rows);
        actual.metric_array_cols.Should().Be(expected.metric_array_cols);
        actual.pixel_pos_per_col.Should().Equal(expected.pixel_pos_per_col);
        actual.pixel_pos_per_row.Should().Equal(expected.pixel_pos_per_row);
        actual.median_metric_per_col.Should().Equal(expected.median_metric_per_col);
        actual.max_pixel_pos_per_row.Should().Equal(expected.max_pixel_pos_per_row);
        ((double)actual.max_metric_in_median).Should().Be((double)expected.max_metric_in_median);
        ((double)actual.mmetric_v_ribbon_k).Should().Be((double)expected.mmetric_v_ribbon_k);
        ((double)actual.mmetric_v_ribbon_b).Should().Be((double)expected.mmetric_v_ribbon_b);
    }

    /// <summary>
    /// 断言两个方向的结果等价: 形状一致, ribbon 拟合 k/b 近似相等.
    /// </summary>
    private static void AssertEquivalentResult(FocusMetricArrayInfo expected, FocusMetricArrayInfo actual)
    {
        // 容差为经验值, 实测不通过时按输出日志调整
        const double tolerance = 1e-3;

        actual.metric_array_rows.Should().Be(expected.metric_array_rows);
        actual.metric_array_cols.Should().Be(expected.metric_array_cols);
        AssertListApproximately(nameof(expected.pixel_pos_per_col), expected.pixel_pos_per_col, actual.pixel_pos_per_col, tolerance);
        AssertListApproximately(nameof(expected.pixel_pos_per_row), expected.pixel_pos_per_row, actual.pixel_pos_per_row, tolerance);
        AssertListApproximately(nameof(expected.median_metric_per_col), expected.median_metric_per_col, actual.median_metric_per_col, tolerance);
        AssertListApproximately(nameof(expected.max_pixel_pos_per_row), expected.max_pixel_pos_per_row, actual.max_pixel_pos_per_row, tolerance);
        ((double)actual.mmetric_v_ribbon_k).Should().BeApproximately((double)expected.mmetric_v_ribbon_k, tolerance);
        ((double)actual.mmetric_v_ribbon_b).Should().BeApproximately((double)expected.mmetric_v_ribbon_b, tolerance);
    }

    /// <summary>
    /// 逐项断言两个列表在容差内一致.
    /// </summary>
    private static void AssertListApproximately<T>(string name, System.Collections.Generic.IReadOnlyList<T> expected, System.Collections.Generic.IReadOnlyList<T> actual, double tolerance)
    {
        actual.Count.Should().Be(expected.Count, name);

        for (var i = 0; i < expected.Count; i++)
        {
            Convert.ToDouble(actual[i]).Should().BeApproximately(Convert.ToDouble(expected[i]), tolerance, $"{name}[{i}]");
        }
    }

    /// <summary>
    /// 输出原始正向图的宽高, 用于确认结果矩阵方向.
    /// </summary>
    private void DumpImageSize(string filePath)
    {
        using var hImage = RAWImageFactory.CreateImage(filePath, true);
        using var bitmapImage = hImage.ToBitmapImage();
        testOutputHelper.WriteLine($"upright image: width={bitmapImage.Width}, height={bitmapImage.Height}");
    }

    private void Dump(string tag, FocusMetricArrayInfo info)
    {
        testOutputHelper.WriteLine($"{tag}: rows={info.metric_array_rows}, cols={info.metric_array_cols}, per_col={info.pixel_pos_per_col.Count}, per_row={info.pixel_pos_per_row.Count}, ribbon k={info.mmetric_v_ribbon_k:0.######}, b={info.mmetric_v_ribbon_b:0.######}");
    }
}