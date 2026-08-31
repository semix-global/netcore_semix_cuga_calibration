using algocv_sharp;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Models.Geometries;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using BestFocusModel = Core.Models.Models.Common.DarkField.BestFocus;

namespace CugaCalibrationUnitTest.AlgoCVSharp.BestFocus;

/// <summary>
/// 将 algocv_sharp.BestFocusEngineResult 转换为 Core.Models.Models.Common.DarkField.BestFocus。
/// 仅供单元测试使用；后续若要在生产代码中使用，可迁移到 Core.Services。
/// </summary>
public static class BestFocusEngineResultExtensions
{
    /// <summary>
    /// 把 <see cref="BestFocusEngineResult"/> 映射为 <see cref="BestFocusModel"/>。
    /// </summary>
    /// <param name="result">BestFocusEngine 计算结果。</param>
    /// <param name="imageSize">原始图像尺寸。</param>
    /// <param name="startECS">ECS 起始值。</param>
    /// <param name="stopECS">ECS 结束值。</param>
    /// <returns>转换后的 BestFocus 模型。</returns>
    public static BestFocusModel ToBestFocus(this BestFocusEngineResult result, Size imageSize, double startECS, double stopECS)
    {
        // BestFocus 构造函数会请求 IScatterPlotControl，单元测试环境未初始化 HostApplication，
        // 因此使用未初始化对象，再由本方法填充所有数据属性。
#if NETFRAMEWORK
        var bestFocus = (BestFocusModel)FormatterServices.GetUninitializedObject(typeof(BestFocusModel));
#else
        var bestFocus = (BestFocusModel)RuntimeHelpers.GetUninitializedObject(typeof(BestFocusModel));
#endif
        bestFocus.IsAlgorithmOk = true;
        bestFocus.XStrehlRatioPoints = BuildStrehlRatioPoints(result.strehl_array_info_x);
        bestFocus.XStrehlRatioFitPoints = BuildStrehlRatioFitPoints(bestFocus.XStrehlRatioPoints);
        bestFocus.XStrehlRatioColumnPoints = BuildStrehlRatioColumnPoints(result.strehl_array_info_x);
        bestFocus.XIntraRibbonFieldsPoints = BuildIntraRibbonFieldsPoints(result.strehl_array_info_x);
        bestFocus.YStrehlRatioPoints = BuildStrehlRatioPoints(result.strehl_array_info_y);
        bestFocus.YStrehlRatioFitPoints = BuildStrehlRatioFitPoints(bestFocus.YStrehlRatioPoints);
        bestFocus.YStrehlRatioColumnPoints = BuildStrehlRatioColumnPoints(result.strehl_array_info_y);
        bestFocus.YIntraRibbonFieldsPoints = BuildIntraRibbonFieldsPoints(result.strehl_array_info_y);

        // X 方向：最佳点、拟合曲线、场倾斜
        BuildDirectionData(
            result.strehl_array_info_x,
            bestFocus,
            isX: true);

        // Y 方向：最佳点、拟合曲线、场倾斜
        BuildDirectionData(
            result.strehl_array_info_y,
            bestFocus,
            isX: false);

        bestFocus.BestXStrehlRatioECS = startECS + bestFocus.BestXStrehlRatioPoint.X / imageSize.Width * (stopECS - startECS);
        bestFocus.BestYStrehlRatioECS = startECS + bestFocus.BestYStrehlRatioPoint.X / imageSize.Width * (stopECS - startECS);

        return bestFocus;
    }

    #region 图1

    /// <summary>
    /// 构建所有Post x位置- Strehl 
    /// </summary>
    private static IReadOnlyList<Point> BuildStrehlRatioPoints(StrehlRatioArrayInfo resultInfo)
    {
        var xs = Generate.LinearRangeInt32(0, resultInfo.strehl_array_cols - 1);
        IReadOnlyList<Point> strehlRatioPoints = [.. xs.Select(t => new Point(resultInfo.pixel_pos_per_col[t], resultInfo.median_strehl_per_col[t]))];
        return strehlRatioPoints;
    }

    /// <summary>
    /// 构建每列最小/最大 Strehl 误差柱。
    /// </summary>
    private static IReadOnlyList<IReadOnlyList<Point>> BuildStrehlRatioColumnPoints(StrehlRatioArrayInfo resultInfo)
    {
        var xs = Generate.LinearRangeInt32(0, resultInfo.strehl_array_cols - 1);
        IReadOnlyList<IReadOnlyList<Point>> strehlRatioColumnPoints = [.. xs.Select<int, IReadOnlyList<Point>>(t => [new Point(resultInfo.pixel_pos_per_col[t], resultInfo.min_strehl_per_col[t]), new Point(resultInfo.pixel_pos_per_col[t], resultInfo.max_strehl_per_col[t])])];

        return strehlRatioColumnPoints;
    }

    /// <summary>
    /// 对 median Strehl 点做二次拟合，生成拟合曲线。
    /// </summary>
    private static IReadOnlyList<Point> BuildStrehlRatioFitPoints(IReadOnlyList<Point> strehlRatioPoints)
    {
        var (_, _, _, _, fitYPredicted) = PolynomialCurve.Fit2(
            Vector<double>.Build.Dense([.. strehlRatioPoints.Select(t => t.X)]),
            Vector<double>.Build.Dense([.. strehlRatioPoints.Select(t => t.Y)]));

        return [.. strehlRatioPoints.Index().Select(t => new Point(t.Item.X, fitYPredicted[t.Index]))];
    }

    #endregion

    #region 图2

    /// <summary>
    /// 构建各 ribbon/行的 Strehl 曲线族。
    /// </summary>
    private static IReadOnlyList<IReadOnlyList<Point>> BuildIntraRibbonFieldsPoints(StrehlRatioArrayInfo resultInfo)
    {
        ImageCsvExporter.ExportUshortToCsv(
            resultInfo.strehl_array_filtered.ToSpan<float>(),
            resultInfo.strehl_array_filtered.Width,
            resultInfo.strehl_array_filtered.Height,
            outputPath: "depth_raw.csv",
            includeHeaders: true,
            normalizeValues: false
        );

        var ys = Generate.LinearRangeInt32(0, resultInfo.strehl_array_rows - 1);

        IReadOnlyList<IReadOnlyList<Point>> results = [..ys.Select(rowIndex => resultInfo.strehl_array_filtered.GetRow<float>(rowIndex).ToArray().Select(t => t).ToArray())
            .Select<float[], IReadOnlyList<Point>>(cols => [..cols.Zip(resultInfo.pixel_pos_per_col, (col, px) => new Point(px, col))])];

        return results;
    }

    #endregion

    #region 图3

    /// <summary>
    /// 构建单个方向的最佳点、拟合曲线、场倾斜。
    /// </summary>
    private static void BuildDirectionData(StrehlRatioArrayInfo info, BestFocusModel bestFocus, bool isX)
    {
        Point[] fieldTiltPoints = [.. info.max_pixel_pos_per_row.Select((t, i) => new Point(i, t))];

        var (slope, intercept, rSquared, fieldTiltFitYPredicted) = PolynomialCurve.Fit1(Vector<double>.Build.Dense([.. fieldTiltPoints.Select(t => t.X)]), Vector<double>.Build.Dense([.. fieldTiltPoints.Select(t => t.Y)]));
        var fieldTiltFitPoints = fieldTiltPoints.Index().Select(t => new Point(t.Item.X, fieldTiltFitYPredicted[t.Index])).ToArray();
        var bestPoint = new Point(info.max_strehl_in_median_pixel_pos, info.max_strehl_in_median);

        // StrehlRatioFitPoints暂不实现
        if (isX)
        {
            bestFocus.BestXStrehlRatioPoint = bestPoint;
            bestFocus.XFieldTiltPoints = fieldTiltPoints;
            bestFocus.XFieldTiltFitSlope = slope;
            bestFocus.XFieldTiltFitIntercept = intercept;
            bestFocus.XFieldTiltFitRSquared = rSquared;
            bestFocus.XFieldTiltFitPoints = fieldTiltFitPoints;
        }
        else
        {
            bestFocus.BestYStrehlRatioPoint = bestPoint;
            bestFocus.YFieldTiltPoints = fieldTiltPoints;
            bestFocus.YFieldTiltFitSlope = slope;
            bestFocus.YFieldTiltFitIntercept = intercept;
            bestFocus.YFieldTiltFitRSquared = rSquared;
            bestFocus.YFieldTiltFitPoints = fieldTiltFitPoints;
        }
    }

    #endregion
}

public class ImageCsvExporter
{
    /// <summary>
    /// 导出 byte 图像数据到 CSV
    /// </summary>
    public static void ExportByteToCsv(
        Span<byte> imageData,
        int width,
        int height,
        string outputPath,
        bool includeHeaders = false,
        bool normalizeValues = false)
    {
        ExportToCsvInternal(imageData, width, height, outputPath, includeHeaders,
            normalizeValues ? 255.0 : 1.0);
    }

    /// <summary>
    /// 导出 ushort 图像数据到 CSV
    /// </summary>
    public static void ExportUshortToCsv(
        Span<float> imageData,
        int width,
        int height,
        string outputPath,
        bool includeHeaders = false,
        bool normalizeValues = false)
    {
        ExportToCsvInternal(imageData, width, height, outputPath, includeHeaders,
            normalizeValues ? float.MaxValue : 1.0);
    }

    /// <summary>
    /// 内部通用导出方法
    /// </summary>
    private static void ExportToCsvInternal<T>(
        Span<T> imageData,
        int width,
        int height,
        string outputPath,
        bool includeHeaders,
        double normalizeDivisor) where T : struct, IConvertible, IFormattable
    {
        if (imageData.Length != width * height)
            throw new ArgumentException($"数据长度 {imageData.Length} 与图像尺寸 {width}x{height} 不匹配");

        using var writer = new StreamWriter(outputPath);

        // 写入表头
        if (includeHeaders)
        {
            for (int col = 0; col < width; col++)
            {
                if (col > 0) writer.Write(',');
                writer.Write($"Pixel{col}");
            }
            writer.WriteLine();
        }

        // 写入数据
        bool shouldNormalize = normalizeDivisor > 1.0;

        for (int row = 0; row < height; row++)
        {
            int startIdx = row * width;

            for (int col = 0; col < width; col++)
            {
                if (col > 0) writer.Write(',');

                T value = imageData[startIdx + col];

                if (shouldNormalize)
                {
                    double normalized = Convert.ToDouble(value) / normalizeDivisor;
                    writer.Write(normalized.ToString("F6", CultureInfo.InvariantCulture));
                }
                else
                {
                    writer.Write(value.ToString(null, CultureInfo.InvariantCulture));
                }
            }
            writer.WriteLine();
        }
    }
}