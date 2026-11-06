using algocv_sharp;
using CommunityToolkit.Diagnostics;
using Core.Algorithm.Helper;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.YPixelSize;
using Core.Algorithm.Models;
using Core.Algorithm.Models.YPixelSize;
using Core.Models.Models.Common.DarkField;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.AlgoCSharp;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Implements.AlgoCSharp;

/// <summary>
/// AlgoCSharp Y 像素尺寸引擎（DfPixelSize 暗场网格，native Row 口径 + AMPD 峰位）
/// </summary>
public sealed class AlgoCvYPixelSizeEngine(ILogger<AlgoCvYPixelSizeEngine> logger) : IYPixelSizeEngine
{
    /// <summary>
    /// AlgoCSharp 引擎（DfPixelSize 暗场网格）：raw 底座为 V3 竖图零重排直读（CreateProtocolImage 原形态）。
    /// 计算口径对齐 algolib 金钉子 testdata "DF Y PixelSize"：竖图（线扫原形态：高=运动轴 X、宽=物理 Y）配 XAxis.Row（native，不转置），
    /// 模板精配步长输出在列方向：step_mean_c = 竖图列向节距 = 物理 Y 向周期（像素/格）；
    /// 峰位/画线对齐 HAlgorithm 版（AMPD 在 Y 投影曲线上找峰），
    /// 计算在竖图上完成后 Transpose 回物理正向图投影/画线/输出，显示形态与旧显示图一致。
    /// </summary>
    public AlgorithmResultRet<YPixelSizeResult> Excute(DarkFieldImageDTO darkFieldImage, YPixelSizeEngineParam param, Guid guid)
    {
        if (EngineParams.TryGet<AlgoCvYPixelSizeParams>(param.EngineParams, out var algoCvParams, out var engineParamsError) == false)
        {
            return AlgorithmResultRet.CreateError<YPixelSizeResult>(engineParamsError);
        }

        // AlgoCSharp 引擎 raw 底座（终态）：V3 竖图零重排直读，替代旧“显示图→ToAlgoCVImage”绕道
        using var algoImage = RAWImageFactoryV3.CreateProtocolImage(darkFieldImage.RawImageFilePath);
        if (darkFieldImage.IsToLiner)
        {
            // 竖形态线性化（log 12bit → 线性 16bit，逐像素运算与转置次序无关，同 V3.CreateImage 语义）
            using var linearImage = ImageProc.LogToLinear(algoImage);
            return ExcuteForward(linearImage, darkFieldImage, param, algoCvParams, guid);
        }

        return ExcuteForward(algoImage, darkFieldImage, param, algoCvParams, guid);
    }

    /// <summary>
    /// 竖图（XAxis.Row native 口径）计算 + 正向图（Transpose 后）投影/画线/输出。
    /// </summary>
    /// <param name="computeImage">参与计算与输出的竖图（log 域=零重排直读；线性域=竖形态线性化后）</param>
    private AlgorithmResultRet<YPixelSizeResult> ExcuteForward(Image computeImage, DarkFieldImageDTO darkFieldImage, YPixelSizeEngineParam param, AlgoCvYPixelSizeParams algoCvParams, Guid guid)
    {
        try
        {
            using var engine = new DfPixelSizeEngine(algoCvParams.ToDfPixelSizeConfig());
            // 竖图配 XAxis.Row（native 口径，不转置）：行(高)=运动轴(X)、列(宽)=物理 Y（线方向），同 algolib 金钉子 "DF Y PixelSize" 口径。
            // native 下模板精配步长输出在列方向：step_mean_c = 竖图列向节距 = 物理 Y 向周期（像素/格）
            using var result = engine.Process(computeImage, XAxis.Row);

            Guard.IsNotEqualTo(result.pairs.step_mean_c, 0);

            // 计算在竖图上完成后转回物理正向图，投影/画线/显示与旧显示图形态一致
            using var forwardImage = ImageProc.Transpose(computeImage);

            // Y 投影曲线 = 正向图每行灰度均值（行=物理 Y，与 HAlgorithm 版 projectionYs 同信号）
            var rowMean = new double[forwardImage.Height];
            var pixels = forwardImage.ToSpan<ushort>();
            for (var row = 0; row < forwardImage.Height; row++)
            {
                long sum = 0;
                var offset = row * forwardImage.Width;
                for (var col = 0; col < forwardImage.Width; col++)
                    sum += pixels[offset + col];

                rowMean[row] = (double)sum / forwardImage.Width;
            }

            // 峰位对齐 HAlgorithm 版语义：AMPD 在 Y 投影曲线上找峰（信号取负 → 定位暗格线谷位）
            var signal = Vector<double>.Build.DenseOfEnumerable(rowMean.Select(t => -t));
            var peaks = AutomaticMPeakDetection.Ampd(signal);

            // 显示域画线：先拉到 8bit 再叠半透明红横线，避免 16bit 白线把整图拉暗
            var drawingImage = DrawResultImage(forwardImage, peaks);

            var ret = new YPixelSizeResult
            {
                // Y pixel size = 物理格高 / 竖图列向节距 step_mean_c（native Row 口径 = 物理 Y 向周期）
                YPixelSize = (double)param.StandardMaskSquareSize / result.pairs.step_mean_c,
                DrawingImage = drawingImage,
                YProjects = [.. rowMean.Select((t, i) => new Point(i, t))],
                ResultIndexes = [.. peaks]
            };

            return AlgorithmResultRet.CreateSuccess(ret);
        }
        catch (Exception ex)
        {
            using var errorForwardImage = ImageProc.Transpose(computeImage);
            using var errorImage = errorForwardImage.ToBitmapImage(darkFieldImage.IsToLiner ? 16 : 12);
            var errorImagePath = AlgorithmLogHelper.SaveErrorImage(errorImage, guid, nameof(Excute));
            AlgorithmLogHelper.LogAlgorithmError(logger, ex, nameof(Excute), guid, errorImagePath);
            throw;
        }
    }

    /// <summary>标记线厚度（对齐 HAlgorithm 版 DrawLines 线宽）</summary>
    private const int LineThickness = 5;

    /// <summary>红色横线透明度（0 全透，1 不透明）</summary>
    private const double LineAlpha = 0.5;

    /// <summary>
    /// 在投影峰行画横跨全宽、厚度 5 的半透明红线。
    /// 大于 8bit 先拉到 UInt8 再转 BGR，输出显示域 Bgr8888，不按 12/16bit 回写。
    /// </summary>
    /// <param name="forwardImage">物理正向图（竖图计算后 Transpose 产物，不被修改）</param>
    /// <param name="peaks">AMPD 峰位列表（正向图行号=物理 Y）</param>
    /// <returns>带半透明红线的 8bit 彩色显示图</returns>
    private static BitmapImage DrawResultImage(Image forwardImage, int[] peaks)
    {
        using var display8 = forwardImage.DataType == ImageDataType.UInt8
            ? null
            : ImageProc.ScaleImageMax(forwardImage, ScaleTargetMax.UInt8, ImageDataType.UInt8);
        var canvas = display8 ?? forwardImage;
        using var bgrImage = canvas.GrayToBgr();

        foreach (var row in peaks.Distinct())
        {
            BlendRedHorizontalLine(bgrImage, row);
        }

        return bgrImage.ToBitmapImage();
    }

    /// <summary>
    /// 在指定起始行按 alpha 混合红色横带，行号越界时夹紧。
    /// </summary>
    private static void BlendRedHorizontalLine(Image bgrImage, int startRow)
    {
        var firstRow = startRow < 0 ? 0 : startRow;
        var endRow = startRow + LineThickness;
        if (endRow > bgrImage.Height) endRow = bgrImage.Height;

        if (firstRow >= endRow) return;

        for (var row = firstRow; row < endRow; row++)
        {
            for (var col = 0; col < bgrImage.Width; col++)
            {
                var srcB = bgrImage.GetValue<byte>(col, row, 0);
                var srcG = bgrImage.GetValue<byte>(col, row, 1);
                var srcR = bgrImage.GetValue<byte>(col, row, 2);
                bgrImage.SetValue(col, row, (byte)Math.Round(srcB * (1d - LineAlpha)), 0);
                bgrImage.SetValue(col, row, (byte)Math.Round(srcG * (1d - LineAlpha)), 1);
                bgrImage.SetValue(col, row, (byte)Math.Round(srcR * (1d - LineAlpha) + byte.MaxValue * LineAlpha), 2);
            }
        }
    }

}
