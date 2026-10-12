using Core.Algorithm.Interfaces.TemplateMatch;

namespace Core.Algorithm.Models.TemplateMatch;

public sealed class MatchTemplateParam
{
    public required ITemplateMatchModel Model { get; init; }

    /// <summary>
    /// 匹配得分下限
    /// </summary>
    public double ScoreThreshold { get; init; }

    public object ToHtmlAnonymous() => new
    {
        ScoreThreshold
    };
}