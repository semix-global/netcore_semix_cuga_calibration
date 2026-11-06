using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.YPixelSize;
using Core.Algorithm.Models.YPixelSize;
using Core.Models.Models.Common.DarkField;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Implements.Mock.AlgoCSharp;

/// <summary>
/// AlgoCSharp Y 像素尺寸引擎 Mock：返回随机数据（原 CalibrationAlgorithmServiceMockImpl.GetYPixelSize 行为）
/// </summary>
public sealed class AlgoCvYPixelSizeEngineMock : IYPixelSizeEngine
{
    public AlgorithmResultRet<YPixelSizeResult> Excute(DarkFieldImageDTO darkFieldImage, YPixelSizeEngineParam param, Guid guid)
    {
        var result = new YPixelSizeResult
        {
            YPixelSize = Random.Shared.NextDouble(),
            DrawingImage = darkFieldImage.GetImage(),
            YProjects = [.. Enumerable.Range(0, 1064).Select(t => new Point(Random.Shared.Next(), Random.Shared.NextDouble()))],
            ResultIndexes = [.. Enumerable.Range(0, 10).Select(t => Random.Shared.Next())]
        };

        return AlgorithmResultRet.CreateSuccess(result);
    }
}
