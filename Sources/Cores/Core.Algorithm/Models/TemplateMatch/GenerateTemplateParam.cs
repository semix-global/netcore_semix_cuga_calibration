using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Models.TemplateMatch;

public sealed class GenerateTemplateParam
{
    public Rect Roi { get; init; }

    /// <summary>
    /// 自定义模板中心（原图像素坐标；null = 默认 ROI 中心）
    /// </summary>
    public Point? TemplateCenter { get; init; }

    public required string TemplateFilePath { get; init; }

    /// <summary>
    /// 算法库独有的参数
    /// </summary>
    public EngineParams? EngineParams { get; init; }

    /// <summary>
    /// 生成模板侧元数据：自定义中心相对 ROI 中心的偏移量（模板侧静态事实，随模板文件持久化）
    /// </summary>
    public TemplateMatchMeta ToTemplateMatchMeta()
    {
        if (TemplateCenter is not { } templateCenter) return new TemplateMatchMeta();

        var offset = templateCenter - Roi.Center;
        return new TemplateMatchMeta { CenterOffset = new Point(offset.X, offset.Y) };
    }

    public object ToHtmlAnonymous() => new
    {
        Roi,
        TemplateCenter,
        TemplateFilePath,
        EngineParams = EngineParams?.ToHtmlAnonymous()
    };
}