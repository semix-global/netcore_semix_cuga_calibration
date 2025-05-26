using CanvasViewer.Editor.Enum;
using CanvasViewer.Geometry;

namespace CanvasViewer.Editor.Entity;

/// <summary>
/// 编辑时候捕捉的锚点
/// </summary>
public sealed class SnapPoint(string name, SnapPointTypeEnum type, Point2D location)
{
    public string Name { get; private set; } = name;
    public SnapPointTypeEnum Type { get; private set; } = type;
    public Point2D Location { get; private set; } = location;

    public SnapPoint(string name, Point2D location) : this(name, SnapPointTypeEnum.End, location)
    {
    }
}