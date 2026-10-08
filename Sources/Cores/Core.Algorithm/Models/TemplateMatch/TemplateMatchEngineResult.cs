namespace Core.Algorithm.Models.TemplateMatch;

/// <summary>
/// 模板匹配库专属结果参数 （预留，暂不派生）
/// </summary>
public abstract class TemplateMatchEngineResult
{
    public static bool TryGet<T>(TemplateMatchEngineResult? engineResult, out T value, out string errorMsg)
        where T : TemplateMatchEngineResult
    {
        switch (engineResult)
        {
            case T typed:
                value = typed;
                errorMsg = string.Empty;
                return true;
            default:
                value = null!;
                errorMsg = engineResult is null
                    ? $"EngineResult is null, expected {typeof(T).Name}"
                    : $"EngineResult must be {typeof(T).Name}";
                return false;
        }
    }

    public abstract object ToHtmlAnonymous();
}