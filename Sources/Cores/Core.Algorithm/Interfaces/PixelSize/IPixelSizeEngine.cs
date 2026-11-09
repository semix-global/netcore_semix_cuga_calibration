using Core.Algorithm.Models.PixelSize;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;

namespace Core.Algorithm.Interfaces.PixelSize;

/// <summary>
/// BF 像素尺寸引擎
/// </summary>
public interface IPixelSizeEngine
{
    /// <summary>
    /// 传入明场图获取像素尺寸 um/px
    /// <para>入参为显示域 BitmapImage（显微 BF jpg/相机图），对齐 ICalibrationAlgorithmService.GetPixelSize 旧链路。</para>
    /// </summary>
    /// <param name="image">明场图</param>
    /// <param name="param">参数</param>
    /// <param name="guid">日志Id</param>
    /// <returns>像素尺寸结果</returns>
    AlgorithmResultRet<PixelSizeResult> Excute(BitmapImage image, PixelSizeEngineParam param, Guid guid);
}