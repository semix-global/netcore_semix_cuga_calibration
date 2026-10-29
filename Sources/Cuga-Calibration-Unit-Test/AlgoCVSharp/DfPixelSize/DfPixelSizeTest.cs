#define DfPixelSizeTest

using algocv_sharp;
using AwesomeAssertions;
using Xunit.Abstractions;
using AlgoCvImage = algocv_sharp.Image;

#if DfPixelSizeTest
using System.Windows;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using Net.Utilities.ScottPlot.WPF.V2;
using Point = Net.Utilities.Models.Geometries.Point;
#endif

namespace CugaCalibrationUnitTest.AlgoCVSharp.DfPixelSize;

/// <summary>
/// DF PixelSize 单元测试：与 algocv_sharp_test.TestDfPixelSize 同读图口径（create_file_image 原样位深/通道），
/// 断言首末列散点/同行配对/列向步长有效性；打开顶部 define 后可用 ScottPlot 查看 Y 投影曲线与配对行竖线（参考 CIBYPixelSizeDTO 的投影+竖线模式）。
/// </summary>
public class DfPixelSizeTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void TestStructSize() => DfPixelSizeEngine.CheckStructSize();

    /// <summary>常规档代表帧（OI-S10 与 algocv_sharp_test 同帧，另取 OI-S90/NI-S40 覆盖不同 S 档与 NI 模式）。</summary>
    [Theory]
    [InlineData(@"Assets\DFPixelSize\test\OI-S10_20260613.tiff")]
    [InlineData(@"Assets\DFPixelSize\test\OI-S90_20260613.tiff")]
    [InlineData(@"Assets\DFPixelSize\test\NI-S40_20260613.tiff")]
    public void Process_ShouldReturnValidPairsResult(string filePath)
    {
        // 读图口径同 algocv_sharp_test：原样位深/通道（16 位单通道）
        using AlgoCvImage image = new(filePath);
        image.IsEmpty.Should().BeFalse();
        image.Channels.Should().Be(1);
        image.DataType.Should().BeOneOf(ImageDataType.UInt16, ImageDataType.Int16);

        var config = new DfPixelSizeConfig();
        using var engine = new DfPixelSizeEngine(config);
        using var result = engine.Process(image);

        // 首末列散点：格号/匹配位/得分图长度与 n 一致（同 algocv_sharp_test 校验口径）
        result.cells.n.Should().BeGreaterThan(0);
        result.cells.n.Should().Be(result.cells.score.Width * result.cells.score.Height);
        (result.cells.n * 2).Should().Be(result.cells.idx.Width * result.cells.idx.Height);
        (result.cells.n * 2).Should().Be(result.cells.hit.Width * result.cells.hit.Height);

        // 同行配对：每行首末点与列向步长一一对应
        result.pairs.r.Should().BeGreaterThan(0);
        result.pairs.pairs.Count.Should().Be(result.pairs.r);
        result.pairs.steps.Count.Should().Be(result.pairs.r);
        // 列向步长方向一致（首→末列为正），行向分量远小于列向分量
        result.pairs.steps.Should().OnlyContain(t => t.dcol > 0);
        Math.Abs(result.pairs.step_mean_r).Should().BeLessThan(Math.Abs(result.pairs.step_mean_c));
        // 步长合理性：条带内至少 period_x_min_cycles 个周期 → 步长不超过图宽/最小周期数
        result.pairs.step_mean_c.Should().BeInRange(config.period_x_min_px, image.Width / config.period_x_min_cycles);

        testOutputHelper.WriteLine($"img={image.Width}x{image.Height}, cells={result.cells.n}, pairs={result.pairs.r}, step_mean=(drow: {result.pairs.step_mean_r:0.####}, dcol: {result.pairs.step_mean_c:0.####})");

#if DfPixelSizeTest
        // Y 投影曲线（每行均值，估 Ty 的信号）+ 配对行位置竖线
        var rowMean = ComputeRowMean(image);
        var pairedRows = result.pairs.pairs.Select(t => t.r0).ToList();

        var thread = new Thread(() =>
        {
            var window = new Window { Title = $"{nameof(DfPixelSizeTest)}_{System.IO.Path.GetFileNameWithoutExtension(filePath)}", WindowState = WindowState.Maximized };

            var plotControl = new PlotControl();
            window.Content = plotControl;

            var plotDataSource = new PlotDataSource();
            plotControl.DataSource = plotDataSource;
            plotDataSource.Configure(new Columns());
            plotDataSource.SetTitle(0, $"DF PixelSize: step_mean_c={result.pairs.step_mean_c:0.###} (Y: Gray - X: Row Pixel)");

            plotDataSource.GetOrAddScatterLine(0, "Y Projection (Row Mean)", [.. rowMean.Index().Select(t => new Point(t.Index, t.Item))], Constants.Turbo.GetColor(0));

            var xLines = plotDataSource.GetOrAddXLines(pairedRows.Count);
            for (var i = 0; i < pairedRows.Count; i++)
            {
                xLines[i].Update($"Paired Row No.{i + 1}", pairedRows[i], Constants.Category10.GetColor(i));
                xLines[i].LinePattern = LinePattern.Dashed;
            }

            plotDataSource.AutoScaleRefresh();
            window.ShowDialog();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
#endif
    }

    /// <summary>正向图 + XAxis.Row 与转置图 + XAxis.Col 的结果应交叉一致（结果坐标按调用方图像 (行,列) 返回）</summary>
    [Theory]
    [InlineData(@"Assets\DFPixelSize\test\OI-S10_20260613.tiff")]
    [InlineData(@"Assets\DFPixelSize\test\OI-S90_20260613.tiff")]
    [InlineData(@"Assets\DFPixelSize\test\NI-S40_20260613.tiff")]
    public void Process_UprightRow_ShouldMatchTransposedCol(string filePath)
    {
        using AlgoCvImage uprightImage = new(filePath);
        uprightImage.IsEmpty.Should().BeFalse();
        using var transposedImage = algocv_sharp.ImageProc.Transpose(uprightImage);
        transposedImage.Width.Should().Be(uprightImage.Height);
        transposedImage.Height.Should().Be(uprightImage.Width);

        using var engine = new DfPixelSizeEngine(new DfPixelSizeConfig());
        using var uprightResult = engine.Process(uprightImage, XAxis.Row);
        using var transposedResult = engine.Process(transposedImage, XAxis.Col);

        testOutputHelper.WriteLine($"img={uprightImage.Width}x{uprightImage.Height}, upright: pairs={uprightResult.pairs.r}, step_mean=(r: {uprightResult.pairs.step_mean_r:0.####}, c: {uprightResult.pairs.step_mean_c:0.####}); transposed: pairs={transposedResult.pairs.r}, step_mean=(r: {transposedResult.pairs.step_mean_r:0.####}, c: {transposedResult.pairs.step_mean_c:0.####})");

        // 配对数量相等，步长均值交叉一致：正向图 r 对转置图 c，正向图 c 对转置图 r
        uprightResult.pairs.r.Should().BeGreaterThan(0);
        uprightResult.pairs.r.Should().Be(transposedResult.pairs.r);
        uprightResult.pairs.step_mean_r.Should().BeApproximately(transposedResult.pairs.step_mean_c, 1e-6);
        uprightResult.pairs.step_mean_c.Should().BeApproximately(transposedResult.pairs.step_mean_r, 1e-6);

        // 输出方向与图像一致：r0 为正向图行号
        uprightResult.pairs.pairs.Should().OnlyContain(t => t.r0 >= 0d && t.r0 < uprightImage.Height);
    }

#if DfPixelSizeTest
    /// <summary>每行灰度均值（Y 投影曲线，16 位单通道）。</summary>
    private static double[] ComputeRowMean(AlgoCvImage image)
    {
        var span = image.ToSpan<ushort>();
        var width = image.Width;
        var height = image.Height;
        var rowMean = new double[height];

        for (var r = 0; r < height; r++)
        {
            long sum = 0;
            var offset = r * width;
            for (var c = 0; c < width; c++)
                sum += span[offset + c];

            rowMean[r] = (double)sum / width;
        }

        return rowMean;
    }
#endif
}