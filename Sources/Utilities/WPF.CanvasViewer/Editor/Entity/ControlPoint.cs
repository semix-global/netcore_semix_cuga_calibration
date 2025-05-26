using CanvasViewer.Editor.Enum;
using CanvasViewer.Geometry;

namespace CanvasViewer.Editor.Entity;

/// <summary>
/// 控制锚点
/// </summary>
public sealed class ControlPoint(string name, ControlPointTypeEnum typeEnum, Point2D basePoint, Point2D location)
{
    /// <summary>
    /// 控制锚点名称
    /// </summary>
    public string Name { get; private set; } = name;

    /// <summary>
    /// 控制锚点类型
    /// </summary>
    public ControlPointTypeEnum TypeEnum { get; private set; } = typeEnum;

    /// <summary>
    /// 基础点
    /// </summary>
    public Point2D BasePoint { get; private set; } = basePoint;

    /// <summary>
    /// 锚点位置
    /// </summary>
    public Point2D Location { get; private set; } = location;

    /// <summary>
    /// 锚点序号(唯一性)
    /// </summary>
    internal int Index { get; set; }

    public ControlPoint(string name, Point2D basePoint)
        : this(name, ControlPointTypeEnum.Point, basePoint, basePoint)
    {
    }
}