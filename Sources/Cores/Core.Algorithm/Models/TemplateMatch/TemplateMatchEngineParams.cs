using Core.Models.Enums.Algorithm;

namespace Core.Algorithm.Models.TemplateMatch;

public sealed class AlgoCvTemplateMatchParams : EngineParams
{
    public TemplateMatchMethodEnum Method { get; init; } = TemplateMatchMethodEnum.TmCcoeffNormed;

    public int PyramidLevels { get; init; } = 3;

    public override object ToHtmlAnonymous() => new
    {
        Method,
        PyramidLevels
    };
}

public sealed class HalconTemplateMatchParams : EngineParams
{
    public AlgorithmTemplateTypeEnum TemplateType { get; init; } = AlgorithmTemplateTypeEnum.Ncc;

    public override object ToHtmlAnonymous() => new
    {
        TemplateType
    };
}