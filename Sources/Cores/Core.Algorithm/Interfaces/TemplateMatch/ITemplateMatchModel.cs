using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Interfaces.TemplateMatch;

/// <summary>
/// 模板
/// </summary>
public interface ITemplateMatchModel : IDisposable
{
    /// <summary>
    /// 自定义模板中心相对 ROI 中心的偏移量（sidecar 元数据，Read 时加载；零偏移 = 默认 ROI 中心语义）
    /// </summary>
    Point CenterOffset { get; }
}