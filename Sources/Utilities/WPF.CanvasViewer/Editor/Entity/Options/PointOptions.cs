using CanvasViewer.Geometry;

namespace CanvasViewer.Editor.Entity.Options;

public sealed class PointOptions : AbstractInputOptions<Point2D>
{
    /// <summary>
    /// 是否显示基础点(Jigged用于抖动显示)
    /// </summary>
    public bool HasBasePoint { get; private set; }

    /// <summary>
    /// 用于显示的基础点
    /// </summary>
    public Point2D BasePoint { get; private set; }

    public PointOptions(string message, Point2D basePoint, Action<Point2D> jig) : base(message, jig)
    {
        HasBasePoint = true;
        BasePoint = basePoint;
    }

    public PointOptions(string message, Point2D basePoint) : this(message, basePoint, _ => { })
    {
    }

    public PointOptions(string message, Action<Point2D> jig) : base(message, jig)
    {
        HasBasePoint = false;
        BasePoint = Point2D.Zero;
    }

    public PointOptions(string message) : this(message, _ => { })
    {
    }
}