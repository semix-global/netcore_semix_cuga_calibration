using Core.Models.Models.Common.Algorithm.TemplateMatch;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Models.TemplateMatch;

public sealed class MatchTemplateResult : IAdaptTo<TemplateMatchSnapshot>
{
    public Point MarkPoint { get; init; }

    public Point Offset { get; init; }

    public double Score { get; init; }

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