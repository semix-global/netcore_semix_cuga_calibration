using algocv_sharp;
using Core.Models.Enums.Algorithm;

namespace Core.Algorithm.Models.TemplateMatch;

/// <summary>
/// 模板匹配库专属参数
/// </summary>
public abstract class TemplateMatchEngineParams
{
    public static bool TryGet<T>(TemplateMatchEngineParams? engineParams, out T value, out string errorMsg)
        where T : TemplateMatchEngineParams, new()
    {
        switch (engineParams)
        {
            case null:
                value = new T();
                errorMsg = string.Empty;
                return true;
            case T typed:
                value = typed;
                errorMsg = string.Empty;
                return true;
            default:
                value = new T();
                errorMsg = $"EngineParams must be {typeof(T).Name}";
                return false;
        }
    }

    public abstract object ToHtmlAnonymous();
}

public sealed class AlgoCvTemplateMatchParams : TemplateMatchEngineParams
{
    public TemplateMatchMethod Method { get; init; } = TemplateMatchMethod.TmCcoeffNormed;

    public int PyramidLevels { get; init; }

    internal TemplateMatchConfig ToTemplateMatchConfig() => new(Method, PyramidLevels);

    public override object ToHtmlAnonymous() => new
    {
        Method,
        PyramidLevels
    };
}

public sealed class HalconTemplateMatchParams : TemplateMatchEngineParams
{
    public AlgorithmTemplateTypeEnum TemplateType { get; init; } = AlgorithmTemplateTypeEnum.Ncc;

    public override object ToHtmlAnonymous() => new
    {
        TemplateType
    };
}

