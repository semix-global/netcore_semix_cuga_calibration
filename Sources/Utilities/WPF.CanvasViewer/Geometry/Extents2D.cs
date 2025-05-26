using System.Runtime.CompilerServices;
using System.Windows;

namespace CanvasViewer.Geometry;

/// <summary>
/// 区域RectangleF
/// </summary>
public sealed class Extents2D
{
    /// <summary>
    /// 空区域
    /// </summary>
    public static Extents2D Empty => new();

    /// <summary>
    /// 无界区域
    /// </summary>
    public static Extents2D Infinity => new(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity);

    #region 属性

    public bool IsEmpty { get; private set; }

    /// <summary>
    /// 区域最小值X
    /// </summary>
    public double XMin { get; private set; }

    /// <summary>
    /// 区域最小值Y
    /// </summary>
    public double YMin { get; private set; }

    /// <summary>
    /// 区域最大值X
    /// </summary>
    public double XMax { get; private set; }

    /// <summary>
    /// 区域最大值Y
    /// </summary>
    public double YMax { get; private set; }

    /// <summary>
    /// 区域宽度
    /// </summary>
    public double Width => Math.Abs(XMax - XMin);

    /// <summary>
    /// 区域高度
    /// </summary>
    public double Height => Math.Abs(YMax - YMin);

    /// <summary>
    /// 区域中心
    /// </summary>
    public Point2D Center => IsEmpty ? Point2D.Zero : new Point2D((XMin + XMax) / 2, (YMin + YMax) / 2);

    /// <summary>
    /// 区域最小点
    /// </summary>
    public Point2D Point2DMin => IsEmpty ? Point2D.Zero : new Point2D(XMin, YMin);

    /// <summary>
    /// 区域最大值
    /// </summary>
    public Point2D Point2DMax => IsEmpty ? Point2D.Zero : new Point2D(XMax, YMax);

    /// <summary>
    /// 区域左上角
    /// </summary>
    public Point2D Point2DTopLeftCorner => IsEmpty ? Point2D.Zero : new Point2D(XMin, YMax);

    /// <summary>
    /// 区域右上角
    /// </summary>
    public Point2D Point2DTopRightCorner => Point2DMax;

    /// <summary>
    /// 区域左下角
    /// </summary>
    public Point2D Point2DBottomLeftCorner => Point2DMin;

    /// <summary>
    /// 区域右下角
    /// </summary>
    public Point2D Point2DBottomRightCorner => IsEmpty ? Point2D.Zero : new Point2D(XMax, YMin);

    /// <summary>
    /// 区域上方中间
    /// </summary>
    public Point2D Point2DTopCenter => IsEmpty ? Point2D.Zero : new Point2D((XMin + XMax) / 2, YMax);

    /// <summary>
    /// 区域下方中间
    /// </summary>
    public Point2D Point2DBottomCenter => IsEmpty ? Point2D.Zero : new Point2D((XMin + XMax) / 2, YMin);

    /// <summary>
    /// 区域左边中间
    /// </summary>
    public Point2D Point2DLeftCenter => IsEmpty ? Point2D.Zero : new Point2D(XMin, (YMin + YMax) / 2);

    /// <summary>
    /// 区域右边中间
    /// </summary>
    public Point2D Point2DRightCenter => IsEmpty ? Point2D.Zero : new Point2D(XMax, (YMin + YMax) / 2);

    #endregion 属性

    #region 构造

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Extents2D()
    {
        IsEmpty = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Extents2D(double xMin, double yMin, double xMax, double yMax)
    {
        IsEmpty = true;
        Add(xMin, yMin);
        Add(xMax, yMax);
    }

    #endregion 构造

    #region 方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        IsEmpty = true;
    }

    /// <summary>
    /// 添加点
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(double x, double y)
    {
        if (IsEmpty || x < XMin) XMin = x;
        if (IsEmpty || y < YMin) YMin = y;
        if (IsEmpty || x > XMax) XMax = x;
        if (IsEmpty || y > YMax) YMax = y;

        IsEmpty = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Point2D pt)
    {
        Add(pt.X, pt.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(IEnumerable<Point2D> points)
    {
        foreach (var pt in points)
        {
            Add(pt.X, pt.Y);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Rect rectangle)
    {
        Add(rectangle.X, rectangle.Y);
        Add(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Extents2D extents)
    {
        if (extents.IsEmpty) return;

        Add(extents.XMin, extents.YMin);
        Add(extents.XMax, extents.YMax);
    }

    /// <summary>
    /// 是否包含点
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Point2D pt)
    {
        return Contains(pt.X, pt.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(double x, double y)
    {
        if (IsEmpty)
            return false;
        else
            return x >= XMin && x <= XMax && y >= YMin && y <= YMax;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Extents2D other)
    {
        return XMin <= other.XMin && XMax >= other.XMax && YMin <= other.YMin && YMax >= other.YMax;
    }

    /// <summary>
    /// 是否相交
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IntersectsWith(Extents2D other)
    {
        return XMax >= other.XMin && XMin <= other.XMax && YMax >= other.YMin && YMin <= other.YMax;
    }

    /// <summary>
    /// 转换
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TransformBy(Matrix2D transformation)
    {
        if (IsEmpty) return;

        var pMin = Point2DMin;
        var pMax = Point2DMax;
        pMin = pMin.Transform(transformation);
        pMax = pMax.Transform(transformation);
        Reset();
        Add(pMin);
        Add(pMax);
    }

    #endregion 方法

    #region 操作符重构

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Rect(Extents2D extents)
    {
        return extents.IsEmpty
            ? Rect.Empty
            : new Rect(extents.XMin, extents.YMin, extents.XMax - extents.XMin, extents.YMax - extents.YMin);
    }

    #endregion 操作符重构
}