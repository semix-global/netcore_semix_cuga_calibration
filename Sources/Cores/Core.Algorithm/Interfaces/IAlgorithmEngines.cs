using Core.Algorithm.Interfaces.TemplateMatch;
using Core.Models.Enums.Algorithm;

namespace Core.Algorithm.Interfaces;

public interface IAlgorithmEngines
{
    /// <summary>
    /// 切换算法库
    /// </summary>
    /// <param name="engine"></param>
    /// <returns></returns>
    ITemplateMatchEngine TemplateMatch(AlgorithmEngineTypeEnum engine);
}
