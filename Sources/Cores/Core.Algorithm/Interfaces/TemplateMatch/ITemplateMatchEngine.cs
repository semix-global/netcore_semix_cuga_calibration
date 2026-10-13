using Core.Algorithm.Models.TemplateMatch;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;

namespace Core.Algorithm.Interfaces.TemplateMatch;

/// <summary>
/// 模板匹配引擎
/// </summary>
public interface ITemplateMatchEngine
{
    /// <summary>
    /// 创建模板
    /// </summary>
    /// <param name="image">模板原始图片</param>
    /// <param name="param"></param>
    /// <param name="guid"></param>
    /// <returns>模板对象</returns>
    AlgorithmResultRet<ITemplateMatchModel> Generate(BitmapImage image, GenerateTemplateParam param, Guid guid);

    /// <summary>
    /// 读模板
    /// </summary>
    /// <param name="param"></param>
    /// <param name="guid"></param>
    /// <returns>模板对象</returns>
    AlgorithmResultRet<ITemplateMatchModel> Read(ReadTemplateParam param, Guid guid);

    /// <summary>
    /// 模板匹配
    /// </summary>
    /// <param name="image">待匹配图片</param>
    /// <param name="param"></param>
    /// <param name="guid"></param>
    /// <returns>匹配结果</returns>
    AlgorithmResultRet<MatchTemplateResult> Match(BitmapImage image, MatchTemplateParam param, Guid guid);
}