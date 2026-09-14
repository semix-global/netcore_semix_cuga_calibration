using Core.Models.Enums.Algorithm;
using Core.Models.Models.Setting;
using Core.Services.Implements;
using Microsoft.Extensions.Logging.Abstractions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using BestFocusModel = Core.Models.Models.Common.DarkField.BestFocus;

namespace CugaCalibrationUnitTest.AlgoCVSharp.BestFocus;

/// <summary>
/// 供单元测试使用的 BestFocus 辅助扩展：直接调用生产，避免测试侧重复维护转换逻辑。
/// </summary>
public static class BestFocusEngineResultExtensions
{
    /// <summary>
    /// 调用服务
    /// </summary>
    /// <param name="image">线性化后的暗场图像。</param>
    /// <param name="startECS">ECS 起始值。</param>
    /// <param name="stopECS">ECS 结束值。</param>
    /// <param name="algorithmBestFocusTypeEnum"></param>
    /// <param name="guid">日志唯一标识。</param>
    /// <returns>转换后的 BestFocus 模型。</returns>
    public static BestFocusModel ToBestFocus(this BitmapImage image, double startECS, double stopECS, AlgorithmBestFocusTypeEnum algorithmBestFocusTypeEnum, Guid guid)
    {
        var service = new CalibrationAlgorithmServiceImpl(
            NullLogger<CalibrationAlgorithmServiceImpl>.Instance,
            new CalibrationSetting(),
            new AffineTransformation(NullLogger<AffineTransformation>.Instance));

        return service.GetBestFocus(image, startECS, stopECS, AlgorithmEngineTypeEnum.AlgoCSharp, algorithmBestFocusTypeEnum, guid);
    }
}