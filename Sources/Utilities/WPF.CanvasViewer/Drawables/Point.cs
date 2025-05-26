using CanvasViewer.Editor.Entity;
using CanvasViewer.Editor.Enum;
using CanvasViewer.Geometry;
using CanvasViewer.Media;
using Newtonsoft.Json;
using System.ComponentModel;

namespace CanvasViewer.Drawables;

public sealed class Point : AbstractDrawable
{
    #region 属性

    private Point2D _p;

    /// <summary>
    /// 点
    /// </summary>
    public Point2D Location
    {
        get => _p;
        set
        {
            if (SetField(ref _p, value) == false) return;
            OnPropertyChanged(nameof(X));
            OnPropertyChanged(nameof(Y));
        }
    }

    /// <summary>
    /// 点x
    /// </summary>
    [Browsable(false)]
    [JsonIgnore]
    public double X => Location.X;

    /// <summary>
    /// 点y
    /// </summary>
    [Browsable(false)]
    [JsonIgnore]
    public double Y => Location.Y;

    #endregion 属性

    #region 构造

    public Point()
    {
    }

    public Point(Point2D location)
    {
        Location = location;
    }

    public Point(double x, double y)
        : this(new Point2D(x, y))
    {
    }

    #endregion 构造

    #region AbstractDrawable

    #region 绘画

    public override void Draw(Renderer renderer)
    {
        var size = renderer.View.ScreenToWorld(new Vector2D(renderer.View.Document.Settings.PointSize, 0)).X / 2;
        renderer.DrawCircle(Style.ApplyLayer(Layer), Location, size);
    }

    public override Extents2D GetExtents()
    {
        var extents = new Extents2D();
        extents.Add(X, Y);
        return extents;
    }

    public override void TransformBy(Matrix2D transformation)
    {
        Location = Location.Transform(transformation);
    }

    public override void TransformControlPoints(int[] indices, Matrix2D transformation)
    {
        foreach (var index in indices)
        {
            if (index == 0)
                Location = Location.Transform(transformation);
        }
    }

    #endregion 绘画

    public override bool Contains(Point2D pt, double pickBoxSize)
    {
        var dist = (pt - Location).Length;
        return dist <= pickBoxSize / 2;
    }

    public override ControlPoint[] GetControlPoints()
    {
        return
        [
            new ControlPoint("Location", Location)
        ];
    }

    public override SnapPoint[] GetSnapPoints()
    {
        return
        [
            new SnapPoint("Start point", SnapPointTypeEnum.Point, Location)
        ];
    }

    #endregion AbstractDrawable
}