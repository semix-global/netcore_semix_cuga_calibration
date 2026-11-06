using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.PixelSize;
using Core.Algorithm.Models.PixelSize;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Implements.Mock.Halcon;

/// <summary>
/// Halcon BF 像素尺寸引擎 Mock：返回随机数据（原 CalibrationAlgorithmServiceMockImpl.GetPixelSize 行为）
/// </summary>
public sealed class HalconPixelSizeEngineMock : IPixelSizeEngine
{
    public AlgorithmResultRet<PixelSizeResult> Excute(BitmapImage image, PixelSizeEngineParam param, Guid guid)
    {
        var result = new PixelSizeResult
        {
            PixelSize = new Size(Random.Shared.Next(1, 10), Random.Shared.Next(1, 10)),
            DrawingImage = image.Copy(),
            Angle = Random.Shared.NextDouble()
        };

        return AlgorithmResultRet.CreateSuccess(result);
    }
}