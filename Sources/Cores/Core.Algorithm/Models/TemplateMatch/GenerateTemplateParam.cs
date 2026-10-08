using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Models.TemplateMatch;

public sealed class GenerateTemplateParam
{
    public Rect Roi { get; init; }

    public required string TemplateFilePath { get; init; }

    /// <summary>
    /// 算法库独有的参数
    /// </summary>
    public TemplateMatchEngineParams? EngineParams { get; init; }

    public object ToHtmlAnonymous() => new
    {
        Roi,
        TemplateFilePath,
        EngineParams = EngineParams?.ToHtmlAnonymous()
    };
}
