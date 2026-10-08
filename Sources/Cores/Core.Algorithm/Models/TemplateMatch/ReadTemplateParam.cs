namespace Core.Algorithm.Models.TemplateMatch;

public sealed class ReadTemplateParam
{
    public required string TemplateFilePath { get; init; }

    /// <summary>
    /// 算法库独有的参数
    /// </summary>
    public TemplateMatchEngineParams? EngineParams { get; init; }

    public object ToHtmlAnonymous() => new
    {
        TemplateFilePath,
        EngineParams = EngineParams?.ToHtmlAnonymous()
    };
}
