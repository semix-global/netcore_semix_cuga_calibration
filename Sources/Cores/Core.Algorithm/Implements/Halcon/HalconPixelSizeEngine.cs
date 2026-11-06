using Core.Algorithm.Helper;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.PixelSize;
using Core.Algorithm.Models.PixelSize;
using HalconDotNet;
using Microsoft.Extensions.Logging;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Implements.Halcon;

/// <summary>
/// Halcon BF 像素尺寸引擎（CalculatePixSize）
/// </summary>
public sealed class HalconPixelSizeEngine(ILogger<HalconPixelSizeEngine> logger) : IPixelSizeEngine
{
    private readonly HAlgorithm.Algorithm _algorithm = new();

    public AlgorithmResultRet<PixelSizeResult> Excute(BitmapImage image, PixelSizeEngineParam param, Guid guid)
    {
        try
        {
            using var hImage = image.ToHImage();
            var maskSize = (double)param.StandardMaskSquareSize;
            _algorithm.CalculatePixSize(hImage, out var drawingImageObj, maskSize, maskSize, out var yTuple, out var xTuple, out var angleX);
            using var _1 = xTuple;
            using var _2 = yTuple;
            using var _3 = angleX;

            using var drawingHImage = new HImage(drawingImageObj);
            var result = new PixelSizeResult
            {
                PixelSize = new Size(xTuple.D, yTuple.D),
                DrawingImage = drawingHImage.ToBitmapImage(),
                Angle = angleX.D
            };

            return AlgorithmResultRet.CreateSuccess(result);
        }
        catch (Exception ex)
        {
            var errorImagePath = AlgorithmLogHelper.SaveErrorImage(image, guid, nameof(Excute));
            AlgorithmLogHelper.LogAlgorithmError(logger, ex, nameof(Excute), guid, errorImagePath);
            throw;
        }
    }
}