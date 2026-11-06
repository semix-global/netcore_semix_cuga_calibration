using Core.Algorithm.Models.BestFocus;
using Core.Models.Models.Common.DarkField;

namespace Core.Algorithm.Interfaces.BestFocus;

/// <summary>
/// Best Focus 引擎
/// </summary>
public interface IBestFocusEngine
{
    /// <summary>
    /// 计算 Best Focus
    /// </summary>
    /// <param name="darkFieldImage">暗场图 DTO（双通道）。引擎内部各自 raw 底座直读 RawImageFilePath：
    /// HAlgorithm 经 RAWImageFactoryV2 产 Halcon 原生 HImage（随 IsToLiner 线性化）；AlgoCSharp 经 RAWImageFactoryV3 读 log 域原图.</param>
    /// <param name="param">参数</param>
    /// <param name="guid">日志Id</param>
    /// <returns>Best Focus 结果</returns>
    AlgorithmResultRet<Core.Models.Models.Common.DarkField.BestFocus> Excute(DarkFieldImageDTO darkFieldImage, BestFocusEngineParam param, Guid guid);
}
