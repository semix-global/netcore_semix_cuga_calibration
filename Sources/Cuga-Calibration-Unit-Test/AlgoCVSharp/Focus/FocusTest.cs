#define FocusTest

using algocv_sharp;
using AwesomeAssertions;
using Core.Models.Enums.Algorithm;
using Core.Models.Models.Setting;
using Core.Services.Implements;
using Microsoft.Extensions.Logging.Abstractions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Xunit.Abstractions;
using AlgoCvImage = algocv_sharp.Image;

#if FocusTest
using System.Windows;
using ScottPlot.MultiplotLayouts;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Helper;
using Net.Utilities.ScottPlot.WPF.V2;
using Point = Net.Utilities.Models.Geometries.Point;
#endif

namespace CugaCalibrationUnitTest.AlgoCVSharp.Focus;

/// <summary>
/// FocusEngine 用法单元测试：FocusEngine.EvaluateFocus 为静态 API（灰度/BGR/RGBA 输入，无需 Dispose），
/// 按倍率目录扫描 ECS 序列帧（文件名 Index(n)_Ecs(value).jpg 解析台架 ECS），逐模式评价清晰度；
/// 注入该倍率参考 Y 像素尺寸（PixelSizeServiceTest 实测：网格尺寸/格距；10X-IR 未校准传 0 显示 N/A）。
/// 打开顶部 define 后弹窗显示各模式 ECS-清晰度曲线。
/// </summary>
public class FocusTest(ITestOutputHelper testOutputHelper)
{
    /// <summary>FocusEngine 支持的全部清晰度评价模式。</summary>
    private static readonly string[] Modes = ["variance", "top_percent_mean", "variance_laplacian", "sum_modified_laplacian", "entropy", "tenengrad", "band_frequency"];

    [Theory]
    [InlineData("5X")]
    [InlineData("10X")]
    [InlineData("10X-IR")]
    [InlineData("50X")]
    [InlineData("100X")]
    public void EvaluateFocus_EcsSweep_ShouldReturnFiniteVaryingScores(string magnification)
    {
        var files = Directory.GetFiles($@"Assets\Focus\{magnification}", "*.jpg");
        files.Length.Should().BeGreaterThan(0);

        // 文件名解析 ECS：Index(n)_Ecs(value).jpg，按 ECS 升序排列
        var regex = new Regex(@"Ecs\((?<ecs>[-\d.]+)\)");
        var frames = files
            .Select(t => (FilePath: t, Ecs: double.Parse(regex.Match(Path.GetFileName(t)).Groups["ecs"].Value, CultureInfo.InvariantCulture)))
            .OrderBy(t => t.Ecs)
            .ToList();
        frames.Should().OnlyContain(t => t.Ecs > 0);
        testOutputHelper.WriteLine($"{magnification}: {frames.Count} frames, Ecs=[{frames[0].Ecs:0.###} ~ {frames[^1].Ecs:0.###}]");

        // 逐模式评价整组帧
        var scores = new Dictionary<string, double[]>();
        foreach (var mode in Modes)
        {
            var modeScores = new double[frames.Count];
            for (var i = 0; i < frames.Count; i++)
            {
                using var image = new AlgoCvImage(frames[i].FilePath);
                modeScores[i] = FocusEngine.EvaluateFocus(image, mode);
            }

            modeScores.Should().OnlyContain(t => double.IsInfinity(t) == false && double.IsNaN(t) == false && t >= 0);
            modeScores.Distinct().Should().HaveCountGreaterThan(1);
            scores[mode] = modeScores;

            var bestEcs = frames[Array.IndexOf(modeScores, modeScores.Max())].Ecs;
            testOutputHelper.WriteLine($"{magnification} {mode}: max={modeScores.Max():0.####} @Ecs={bestEcs:0.###}");
        }

#if FocusTest
        ShowPlot(magnification, [.. frames.Select(t => t.Ecs)], scores);
#endif
    }

    /// <summary>
    /// GetQuality 服务级双引擎对比：HAlgorithm（LaplaceDefinition）vs AlgoCSharp（FocusEngine tenengrad 默认模式），
    /// 同组 ECS 序列帧分别评分；输出两算法最清晰 ECS 的差值，打开顶部 define 后弹窗双曲线并以十字星标注各自最大点。
    /// </summary>
    [Theory]
    [InlineData("5X")]
    [InlineData("10X")]
    [InlineData("10X-IR")]
    [InlineData("50X")]
    [InlineData("100X")]
    public void GetQuality_HalconVsAlgo_EcsSweep_ShouldOutputBestEcsDifference(string magnification)
    {
        var service = new CalibrationAlgorithmServiceImpl(
            NullLogger<CalibrationAlgorithmServiceImpl>.Instance,
            new CalibrationSetting(),
            new AffineTransformation(NullLogger<AffineTransformation>.Instance));

        var files = Directory.GetFiles($@"Assets\Focus\{magnification}", "*.jpg");
        files.Length.Should().BeGreaterThan(0);

        var regex = new Regex(@"Ecs\((?<ecs>[-\d.]+)\)");
        var frames = files
            .Select(t => (FilePath: t, Ecs: double.Parse(regex.Match(Path.GetFileName(t)).Groups["ecs"].Value, CultureInfo.InvariantCulture)))
            .OrderBy(t => t.Ecs)
            .ToList();

        var halconScores = new double[frames.Count];
        var algoScores = new double[frames.Count];
        for (var i = 0; i < frames.Count; i++)
        {
            using var image = new BitmapImage(frames[i].FilePath);
            halconScores[i] = service.GetQuality(image, Guid.NewGuid(), AlgorithmEngineTypeEnum.HAlgorithm);
            algoScores[i] = service.GetQuality(image, Guid.NewGuid(), AlgorithmEngineTypeEnum.AlgoCSharp);
        }

        halconScores.Should().OnlyContain(t => double.IsInfinity(t) == false && double.IsNaN(t) == false);
        algoScores.Should().OnlyContain(t => double.IsInfinity(t) == false && double.IsNaN(t) == false);

        var bestEcsHalcon = frames[Array.IndexOf(halconScores, halconScores.Max())].Ecs;
        var bestEcsAlgo = frames[Array.IndexOf(algoScores, algoScores.Max())].Ecs;
        testOutputHelper.WriteLine($"{magnification}: Halcon(Laplace) best Ecs={bestEcsHalcon:0.###} (quality {halconScores.Max():0.####}), AlgoCSharp(Tenengrad) best Ecs={bestEcsAlgo:0.###} (quality {algoScores.Max():0.####}), difference={bestEcsAlgo - bestEcsHalcon:0.###}");

#if FocusTest
        ShowComparePlot(magnification, [.. frames.Select(t => t.Ecs)], halconScores, algoScores, bestEcsHalcon, bestEcsAlgo);
#endif
    }

#if FocusTest
    /// <summary>
    /// 弹窗可视化：各清晰度模式的 ECS-得分曲线，标题注入并显示该倍率参考 Y 像素尺寸。
    /// </summary>
    private void ShowPlot(string magnification, double[] ecs, Dictionary<string, double[]> scores)
    {
        var thread = new Thread(() =>
        {
            var window = new Window { Title = $"{nameof(FocusTest)}_{magnification}", WindowState = WindowState.Maximized };

            var plotControl = new PlotControl();
            window.Content = plotControl;

            var plotDataSource = new PlotDataSource();
            plotControl.DataSource = plotDataSource;
            plotDataSource.Configure(new Columns());
            plotDataSource.SetTitle(0, $"Focus {magnification}: (Y: Score - X: ECS)");

            for (var i = 0; i < Modes.Length; i++)
            {
                var modeScores = scores[Modes[i]];
                plotDataSource.GetOrAddScatterLine(0, $"{Modes[i]} (max {modeScores.Max():0.##})", [.. ecs.Select((t, j) => new Point(t, modeScores[j]))], Constants.Category10.GetColor(i));
            }

            plotDataSource.AutoScaleRefresh();
            window.ShowDialog();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    /// <summary>
    /// 弹窗可视化：Halcon 与 AlgoCSharp 双引擎 ECS-清晰度曲线，十字星（XLine 竖线 + YLine 横线）标注各自最大点，标题显示最清晰 ECS 差值。
    /// </summary>
    private void ShowComparePlot(string magnification, double[] ecs, double[] halconScores, double[] algoScores, double bestEcsHalcon, double bestEcsAlgo)
    {
        var thread = new Thread(() =>
        {
            var window = new Window { Title = $"{nameof(FocusTest)}_{magnification}_HalconVsAlgo", WindowState = WindowState.Maximized };

            var plotControl = new PlotControl();
            window.Content = plotControl;

            var plotDataSource = new PlotDataSource();
            plotControl.DataSource = plotDataSource;
            plotDataSource.Configure(new Columns());
            plotDataSource.SetTitle(0, $"Focus {magnification}: BestEcsDiff = {bestEcsAlgo - bestEcsHalcon:0.###} (Y: Score - X: ECS)");

            plotDataSource.GetOrAddScatterLine(0, $"Halcon Laplace (best {bestEcsHalcon:0.###})", [.. ecs.Select((t, i) => new Point(t, halconScores[i]))], Constants.Category10.GetColor(0));
            plotDataSource.GetOrAddScatterLine(0, $"AlgoCSharp Tenengrad (best {bestEcsAlgo:0.###})", [.. ecs.Select((t, i) => new Point(t, algoScores[i]))], Constants.Category10.GetColor(1));

            // 十字星标注最大点：竖线（Best ECS）与横线（Max Score）交点即曲线最大点
            var xLines = plotDataSource.GetOrAddXLines(2);
            xLines[0].Update($"Halcon Best Ecs {bestEcsHalcon:0.###}", bestEcsHalcon, Constants.Category10.GetColor(0));
            xLines[1].Update($"Algo Best Ecs {bestEcsAlgo:0.###}", bestEcsAlgo, Constants.Category10.GetColor(1));

            var yLines = plotDataSource.GetOrAddYLines(2);
            yLines[0].Update($"Halcon Max {halconScores.Max():0.##}", halconScores.Max(), Constants.Category10.GetColor(0));
            yLines[1].Update($"Algo Max {algoScores.Max():0.##}", algoScores.Max(), Constants.Category10.GetColor(1));

            plotDataSource.AutoScaleRefresh();
            window.ShowDialog();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }
#endif
}
