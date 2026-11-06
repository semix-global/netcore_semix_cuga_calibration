using Core.Algorithm.Implements.AlgoCSharp;
using Core.Algorithm.Models.BestFocus;
using Core.Models.Enums.Algorithm;
using Core.Models.Models.Common.DarkField;
using Microsoft.Extensions.Logging.Abstractions;
using BestFocusModel = Core.Models.Models.Common.DarkField.BestFocus;

namespace CugaCalibrationUnitTest.AlgoCVSharp.BestFocus;

/// <summary>
/// 供单元测试使用的 BestFocus 辅助扩展：直接调用 AlgoCvBestFocusEngine 生产路径，避免测试侧重复维护转换逻辑。
/// </summary>
public static class BestFocusEngineResultExtensions
{
    /// <summary>
    /// 调用 AlgoCv BestFocus 引擎
    /// </summary>
    /// <param name="darkFieldImage">暗场图 DTO. AlgoCv 引擎从 RawImageFilePath 读取 log 域原图.</param>
    /// <param name="startECS">ECS 起始值。</param>
    /// <param name="stopECS">ECS 结束值。</param>
    /// <param name="algorithmBestFocusTypeEnum"></param>
    /// <param name="guid">日志唯一标识。</param>
    /// <returns>转换后的 BestFocus 模型。</returns>
    public static BestFocusModel ToBestFocus(this DarkFieldImageDTO darkFieldImage, double startECS, double stopECS, AlgorithmBestFocusTypeEnum algorithmBestFocusTypeEnum, Guid guid)
    {
        var engine = new AlgoCvBestFocusEngine(NullLogger<AlgoCvBestFocusEngine>.Instance);
        var ret = engine.Excute(darkFieldImage, new BestFocusEngineParam { StartEcs = startECS, StopEcs = stopECS, BestFocusType = algorithmBestFocusTypeEnum }, guid);

        return ret.Data ?? throw new InvalidOperationException(ret.ErrorMsg);
    }
}
