#define BfPixelSizeTest

using algocv_sharp;
using AwesomeAssertions;
using Xunit.Abstractions;
using AlgoCvImage = algocv_sharp.Image;

#if BfPixelSizeTest
using System.Windows;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using Net.Utilities.ScottPlot.WPF.V2;
using Point = Net.Utilities.Models.Geometries.Point;
#endif

namespace CugaCalibrationUnitTest.AlgoCVSharp.BfPixelSize;

/// <summary>
/// BF PixelSize 单元测试：与 algocv_sharp_test.TestBfPixelSize 同读图口径（create_file_image 原样位深/通道），
/// 断言散点/稠密阵/格基矢有效性；全倍率用例（5x-150x）按倍率预估扫描窗并与服务档位对齐；
/// 打开顶部 define 后可用 ScottPlot 弹窗查看 X/Y 两条投影曲线与格位竖线（参考 CIBYPixelSizeDTO 的投影+竖线模式）。
/// </summary>
public class BfPixelSizeTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void TestStructSize() => BfPixelSizeEngine.CheckStructSize();

    /// <summary>三张常规档代表帧（与 algocv_sharp_test.DatasetRegression 同帧）。</summary>
    [Theory]
    [InlineData(@"Assets\BFPixelSize\10-10-5-19_100X_20260308.jpg")]
    [InlineData(@"Assets\BFPixelSize\10-10-5-19_10X-IR_20260829.jpg")]
    [InlineData(@"Assets\BFPixelSize\10-10-6-19_5X_20251014.jpg")]
    public void Process_ShouldReturnValidGridResult(string filePath)
    {
        // 读图口径同 algocv_sharp_test：jpg → 8UC3，引擎内按通道均值转灰度
        using AlgoCvImage image = new(filePath);
        image.IsEmpty.Should().BeFalse();

        var config = new BfPixelSizeConfig();
        using var engine = new BfPixelSizeEngine(config);
        using var result = engine.Process(image);

        // 散点：格号/匹配位/得分三列同序等长
        result.cells.n.Should().BeGreaterThan(0);
        result.cells.idx.Count.Should().Be(result.cells.n);
        result.cells.hit.Count.Should().Be(result.cells.n);
        result.cells.score.Count.Should().Be(result.cells.n);
        // BFS 匹配输出的散点得分均应达到匹配阈值（异常点由拟合残差剔除，得分仍达标）
        result.cells.score.Should().OnlyContain(t => t >= config.match_score_thresh);

        // 稠密阵：内点铺成 R×C，缺格为 NaN，有效格数不超过总格数
        result.arr.rows.Should().BeGreaterThan(0);
        result.arr.cols.Should().BeGreaterThan(0);
        result.arr.n.Should().BeInRange(1, result.arr.rows * result.arr.cols);

        // 格基矢：单格位移向量长度 = ty/tx，且格距落在周期扫描窗内
        result.hvc.ty.Should().BeInRange(config.period_n_min, config.period_n_max);
        result.hvc.tx.Should().BeInRange(config.period_n_min, config.period_n_max);
        Math.Sqrt(result.hvc.row_vec[0] * result.hvc.row_vec[0] + result.hvc.row_vec[1] * result.hvc.row_vec[1]).Should().BeApproximately(result.hvc.ty, 1e-6);
        Math.Sqrt(result.hvc.col_vec[0] * result.hvc.col_vec[0] + result.hvc.col_vec[1] * result.hvc.col_vec[1]).Should().BeApproximately(result.hvc.tx, 1e-6);

        testOutputHelper.WriteLine($"img={image.Width}x{image.Height}x{image.Channels}, cells={result.cells.n}, array={result.arr.rows}x{result.arr.cols} n={result.arr.n}, tx={result.hvc.tx:0.####}, ty={result.hvc.ty:0.####}");

#if BfPixelSizeTest
        ShowPlot(filePath, image, result);
#endif
    }

    /// <summary>
    /// 全倍率用例：倍率-网格尺寸对应 5x-100um / 10x-50um / 50x-25um / 100x-10um / 150x-10um（AlgorithmStandardMaskSquareSizeEnum）。
    /// 扫描窗按倍率预估（与 GetAlgoCVSharpPixelSize 服务档位一致）：Size100/Size50→400，Size25→500，Size10→600（150x 格距 431px 超默认 400 会直接失败 code=2）。
    /// 期望格距为全倍率帧实测值（PixelSizeServiceTest 交叉验证：像素尺寸 = 网格尺寸 / 格距）。
    /// </summary>
    [Theory]
    [InlineData(@"Assets\BFPixelSize\10-10-6-19_5X_20251014.jpg", 145.72, 400)]
    [InlineData(@"Assets\BFPixelSize\10-10-6-19_10X_20251014.jpg", 72.88, 400)]
    [InlineData(@"Assets\BFPixelSize\10-10-6-19_50X_20251014.jpg", 365.21, 500)]
    [InlineData(@"Assets\BFPixelSize\10-10-6-19_100X_20251014.jpg", 288.64, 600)]
    [InlineData(@"Assets\BFPixelSize\10-10-6-19_150X_20260402.jpg", 431.01, 600)]
    public void Process_AllMagnifications_WithScanWindow_ShouldMatchMeasuredPitch(string filePath, double expectedTx, int scanMax)
    {
        // 读图口径同 algocv_sharp_test：jpg → 8UC3，引擎内按通道均值转灰度
        using AlgoCvImage image = new(filePath);
        image.IsEmpty.Should().BeFalse();

        var config = new BfPixelSizeConfig { period_half = scanMax, period_n_max = scanMax };
        using var engine = new BfPixelSizeEngine(config);
        using var result = engine.Process(image);

        // 格基矢：格距落在预估扫描窗内，且单格位移向量长度 = ty/tx
        result.hvc.tx.Should().BeInRange(expectedTx * 0.95, expectedTx * 1.05);
        result.hvc.ty.Should().BeInRange(expectedTx * 0.95, expectedTx * 1.05);
        Math.Sqrt(result.hvc.row_vec[0] * result.hvc.row_vec[0] + result.hvc.row_vec[1] * result.hvc.row_vec[1]).Should().BeApproximately(result.hvc.ty, 1e-6);
        Math.Sqrt(result.hvc.col_vec[0] * result.hvc.col_vec[0] + result.hvc.col_vec[1] * result.hvc.col_vec[1]).Should().BeApproximately(result.hvc.tx, 1e-6);

        // 散点与稠密阵有效性
        result.cells.n.Should().BeGreaterThan(0);
        result.arr.rows.Should().BeGreaterThan(0);
        result.arr.cols.Should().BeGreaterThan(0);
        result.arr.n.Should().BeInRange(1, result.arr.rows * result.arr.cols);

        testOutputHelper.WriteLine($"img={image.Width}x{image.Height}x{image.Channels}, scanMax={scanMax}: cells={result.cells.n}, array={result.arr.rows}x{result.arr.cols} n={result.arr.n}, tx={result.hvc.tx:0.####}, ty={result.hvc.ty:0.####}");

#if BfPixelSizeTest
        ShowPlot(filePath, image, result);
#endif
    }

#if BfPixelSizeTest
    /// <summary>
    /// 弹窗可视化：X/Y 两条投影曲线（列均值/行均值）+ 稠密阵首行/首列格位竖线（参考 CIBYPixelSizeDTO 的投影+竖线模式）。
    /// </summary>
    private void ShowPlot(string filePath, AlgoCvImage image, BfPixelSizeResult result)
    {
        var columnMean = ComputeColumnMean(image);
        var rowMean = ComputeRowMean(image);
        var (cellCols, cellRows) = GetFirstRowAndColumnCellPositions(result.arr);

        var thread = new Thread(() =>
        {
            var window = new Window { Title = $"{nameof(BfPixelSizeTest)}_{System.IO.Path.GetFileNameWithoutExtension(filePath)}", WindowState = WindowState.Maximized };

            var plotControl = new PlotControl();
            window.Content = plotControl;

            var plotDataSource = new PlotDataSource();
            plotControl.DataSource = plotDataSource;
            plotDataSource.Configure(new Columns());
            plotDataSource.SetTitle(0, $"BF PixelSize: tx={result.hvc.tx:0.###} ty={result.hvc.ty:0.###} (Y: Gray - X: Pixel)");

            plotDataSource.GetOrAddScatterLine(0, "X Projection (Col Mean)", [.. columnMean.Index().Select(t => new Point(t.Index, t.Item))], Constants.Turbo.GetColor(0));
            plotDataSource.GetOrAddScatterLine(0, "Y Projection (Row Mean)", [.. rowMean.Index().Select(t => new Point(t.Index, t.Item))], Constants.Turbo.GetColor(1));

            var xLines = plotDataSource.GetOrAddXLines(cellCols.Count + cellRows.Count);
            for (var i = 0; i < cellCols.Count; i++)
            {
                xLines[i].Update($"X Cell No.{i + 1}", cellCols[i], Constants.Category10.GetColor(0));
                xLines[i].LinePattern = LinePattern.Dashed;
            }

            for (var i = 0; i < cellRows.Count; i++)
            {
                xLines[cellCols.Count + i].Update($"Y Cell No.{i + 1}", cellRows[i], Constants.Category10.GetColor(1));
                xLines[cellCols.Count + i].LinePattern = LinePattern.Dashed;
            }

            plotDataSource.AutoScaleRefresh();
            window.ShowDialog();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    /// <summary>每列灰度均值（X 投影曲线）。</summary>
    private static double[] ComputeColumnMean(AlgoCvImage image)
    {
        var span = image.ToSpan<byte>();
        var width = image.Width;
        var height = image.Height;
        var channels = image.Channels;
        var columnMean = new double[width];

        for (var c = 0; c < width; c++)
        {
            long sum = 0;
            for (var r = 0; r < height; r++)
                for (var k = 0; k < channels; k++)
                    sum += span[(r * width + c) * channels + k];

            columnMean[c] = (double)sum / height;
        }

        return columnMean;
    }

    /// <summary>每行灰度均值（Y 投影曲线）。</summary>
    private static double[] ComputeRowMean(AlgoCvImage image)
    {
        var span = image.ToSpan<byte>();
        var width = image.Width;
        var height = image.Height;
        var channels = image.Channels;
        var rowMean = new double[height];
        var rowLength = width * channels;

        for (var r = 0; r < height; r++)
        {
            long sum = 0;
            for (var k = 0; k < rowLength; k++)
                sum += span[r * rowLength + k];

            rowMean[r] = (double)sum / rowLength;
        }

        return rowMean;
    }

    /// <summary>稠密阵首行各格 col 位置与首列各格 row 位置（缺格 NaN 剔除）。</summary>
    private static (List<double> CellCols, List<double> CellRows) GetFirstRowAndColumnCellPositions(BfPixelSizeArray arr)
    {
        var cellCols = new List<double>();
        var cellRows = new List<double>();
        if (arr.hit is not { } hit) return (cellCols, cellRows);

        var span = hit.ToSpan<double>();
        for (var c = 0; c < arr.cols; c++)
        {
            var col = span[c * 2 + 1];
            if (double.IsNaN(col) == false) cellCols.Add(col);
        }

        for (var r = 0; r < arr.rows; r++)
        {
            var row = span[r * arr.cols * 2];
            if (double.IsNaN(row) == false) cellRows.Add(row);
        }

        return (cellCols, cellRows);
    }
#endif
}
