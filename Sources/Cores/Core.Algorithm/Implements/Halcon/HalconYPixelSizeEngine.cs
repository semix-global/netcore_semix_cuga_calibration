using Core.Algorithm.Helper;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.YPixelSize;
using Core.Algorithm.Models.YPixelSize;
using Core.Models.Models.Common.DarkField;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Implements.Halcon;

/// <summary>
/// Halcon Y 像素尺寸引擎（行投影 + AMPD 峰检测）
/// </summary>
public sealed class HalconYPixelSizeEngine(ILogger<HalconYPixelSizeEngine> logger) : IYPixelSizeEngine
{
    public AlgorithmResultRet<YPixelSizeResult> Excute(DarkFieldImageDTO darkFieldImage, YPixelSizeEngineParam param, Guid guid)
    {
        // Halcon 引擎 raw 底座：V2 直读 raw 产 Halcon 原生 HImage（世界口径正向图），替代旧“显示图→ToHImage”绕道
        using var hImage = RAWImageFactoryV2.CreateImage(darkFieldImage.RawImageFilePath, darkFieldImage.IsToLiner);
        hImage.GetImageSize(out int width, out int height);

        try
        {
            var data = hImage.RAW16BitsPerPixelToMatrix();
            var matrix = Matrix<double>.Build.DenseOfArray(data, null);
            var projectionYs = matrix.RowSums() / matrix.ColumnCount;

            // 使用AMPD算法找出波峰
            var signal = Vector<double>.Build.DenseOfEnumerable(projectionYs.Select(t => -t));
            var peaks = AutomaticMPeakDetection.Ampd(signal);

            var xDifferences = peaks
                .Zip(peaks.Skip(1), (prev, next) => (double)next - prev)
                .ToArray();
            var (indexes, filterXDifferences) = Filter.MAD(xDifferences);

            // 所有后一个减去前一个，得到差值, 然后取得均值
            var mean = filterXDifferences.Average();

            using var drawHImage = hImage.DrawLines(
                [
                    .. indexes
                        .SelectMany(t => (int[])[t, t + 1])
                        .Distinct()
                        .OrderBy(t => t)
                        .Select(index => (new Point(0, peaks[index]), new Point(width, peaks[index])))
                ],
                5);
            var drawingImage = drawHImage.ToBitmapImage();

            var result = new YPixelSizeResult
            {
                YPixelSize = (double)param.StandardMaskSquareSize / mean,
                DrawingImage = drawingImage,
                YProjects = [.. projectionYs.Select((t, i) => new Point(i, t))],
                ResultIndexes = [.. peaks]
            };

            return AlgorithmResultRet.CreateSuccess(result);
        }
        catch (Exception ex)
        {
            using var errorImage = hImage.ToBitmapImage(darkFieldImage.IsToLiner ? 16 : 12);
            var errorImagePath = AlgorithmLogHelper.SaveErrorImage(errorImage, guid, nameof(Excute));
            AlgorithmLogHelper.LogAlgorithmError(logger, ex, nameof(Excute), guid, errorImagePath);
            throw;
        }
    }

}
