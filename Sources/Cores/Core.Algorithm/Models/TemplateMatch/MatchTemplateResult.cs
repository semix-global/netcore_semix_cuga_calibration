using Core.Models.Models.Common.Algorithm.TemplateMatch;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Models.TemplateMatch;

public sealed class MatchTemplateResult : IAdaptTo<TemplateMatchSnapshot>
{
    public Point MarkPoint { get; init; }

    public Point Offset { get; init; }

    public double Score { get; init; }

    /// <summary>
    /// 匹配角度（度；中立口径：图像像素坐标系（Y 向下）下屏幕逆时针为正；双引擎统一口径，Halcon 引擎由弧度换算）
    /// </summary>
    public double Angle { get; init; }

    /// <summary>
    /// 算法库独有的参数
    /// </summary>
    public TemplateMatchEngineResult? EngineResult { get; init; }

    public object ToHtmlAnonymous() => new
    {
        MarkPoint,
        Offset,
        Score,
        Angle,
        EngineResult = EngineResult?.ToHtmlAnonymous()
    };

    public TemplateMatchSnapshot AdaptTo() => new()
    {
        Score = Score,
        Angle = Angle,
        MarkPoint = MarkPoint,
        Offset = Offset
    };
}