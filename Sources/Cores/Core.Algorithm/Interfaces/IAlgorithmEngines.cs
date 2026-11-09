using Core.Algorithm.Interfaces.BestFocus;
using Core.Algorithm.Interfaces.PixelSize;
using Core.Algorithm.Interfaces.TemplateMatch;
using Core.Algorithm.Interfaces.YPixelSize;
using Net.Utilities.Calibration;

namespace Core.Algorithm.Interfaces;

public interface IAlgorithmEngines
{
    /// <summary>
    /// 切换算法库
    /// </summary>
    /// <param name="engine"></param>
    /// <returns></returns>
    ITemplateMatchEngine TemplateMatch(AlgorithmEngineTypeEnum engine);

    /// <summary>
    /// 切换 Best Focus 算法库
    /// </summary>
    /// <param name="engine"></param>
    /// <returns></returns>
    IBestFocusEngine BestFocus(AlgorithmEngineTypeEnum engine);

    /// <summary>
    /// 切换 Y 像素尺寸算法库
    /// </summary>
    /// <param name="engine"></param>
    /// <returns></returns>
    IYPixelSizeEngine YPixelSize(AlgorithmEngineTypeEnum engine);

    /// <summary>
    /// 切换 BF 像素尺寸算法库
    /// </summary>
    /// <param name="engine"></param>
    /// <returns></returns>
    IPixelSizeEngine PixelSize(AlgorithmEngineTypeEnum engine);
}