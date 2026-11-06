using algocv_sharp;
using Core.Algorithm.Helper;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.PixelSize;
using Core.Algorithm.Models;
using Core.Algorithm.Models.PixelSize;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.AlgoCSharp;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Implements.AlgoCSharp;

/// <summary>
/// AlgoCSharp BF 像素尺寸引擎（BfPixelSize 明场网格拟合）
/// </summary>
public sealed class AlgoCvPixelSizeEngine(ILogger<AlgoCvPixelSizeEngine> logger) : IPixelSizeEngine
{
    /// <summary>
    /// AlgoCSharp 引擎（BfPixelSize 明场网格拟合）：显微 BF 图为常规格式（8bit 灰/彩色，jpg 直读），
    /// 桥直搬保持物理正向。
    /// </summary>
    public AlgorithmResultRet<PixelSizeResult> Excute(BitmapImage image, PixelSizeEngineParam param, Guid guid)
    {
        if (EngineParams.TryGet<AlgoCvPixelSizeParams>(param.EngineParams, out var algoCvParams, out var engineParamsError) == false)
        {
            return AlgorithmResultRet.CreateError<PixelSizeResult>(engineParamsError);
        }

        try
        {
            using var algoImage = image.ToAlgoCVImage();
            using var engine = new BfPixelSizeEngine(algoCvParams.ToBfPixelSizeConfig(param.StandardMaskSquareSize));
            using var result = engine.Process(algoImage);

            // hvc 为拟合格基矢：tx/ty = x/y 向格距(像素/格)；col_vec = j 向单格位移 (drow, dcol)。
            // pixel size = 物理格尺寸 / 格距；angle = 格向量相对 x 轴倾角(度)。
            var maskSize = (double)param.StandardMaskSquareSize;
            var ret = new PixelSizeResult
            {
                PixelSize = new Size(maskSize / result.hvc.tx, maskSize / result.hvc.ty),
                DrawingImage = image.Copy(),
                Angle = Math.Atan2(result.hvc.col_vec[0], result.hvc.col_vec[1]) * 180d / Math.PI
            };

            return AlgorithmResultRet.CreateSuccess(ret);
        }
        catch (Exception ex)
        {
            var errorImagePath = AlgorithmLogHelper.SaveErrorImage(image, guid, nameof(Excute));
            AlgorithmLogHelper.LogAlgorithmError(logger, ex, nameof(Excute), guid, errorImagePath);
            throw;
        }
    }
}