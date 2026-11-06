using Core.Algorithm.Models.YPixelSize;
using Core.Models.Models.Common.DarkField;

namespace Core.Algorithm.Interfaces.YPixelSize;

/// <summary>
/// Y 像素尺寸引擎
/// </summary>
public interface IYPixelSizeEngine
{
    /// <summary>
    /// 传入暗场图 DTO 获取Y像素尺寸um
    /// <para>DTO 双通道（对齐 IBestFocusEngine）：引擎内部各自 raw 底座直读 RawImageFilePath——
    /// HAlgorithm 用 RAWImageFactoryV2（Halcon 原生 HImage）；AlgoCSharp 用 RAWImageFactoryV3（algo Image）。</para>
    /// </summary>
    /// <param name="darkFieldImage">暗场图 DTO（GetImage() 显示图仅用于结果可视化兜底）</param>
    /// <param name="param">参数</param>
    /// <param name="guid">日志Id</param>
    /// <returns>Y 像素尺寸结果</returns>
    AlgorithmResultRet<YPixelSizeResult> Excute(DarkFieldImageDTO darkFieldImage, YPixelSizeEngineParam param, Guid guid);
}
