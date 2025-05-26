using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CanvasViewer.Geometry;

/// <summary>
/// 点PointF
/// </summary>
[TypeConverter(typeof(Point2DConverter))]
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct Point2D(double x, double y)
{
    public static Point2D Zero => new(0, 0);

    #region 属性

    /// <summary>
    /// 点x
    /// </summary>
    public double X { get; } = x;

    /// <summary>
    /// 点y
    /// </summary>
    public double Y { get; } = y;

    #endregion 属性

    #region 构造

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point2D(Point pt) : this(pt.X, pt.Y)
    {
    }

    #endregion 构造

    #region 方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point2D Transform(Matrix2D transformation)
    {
        var x = transformation.M11 * X + transformation.M12 * Y + transformation.Dx;
        var y = transformation.M21 * X + transformation.M22 * Y + transformation.Dy;
        return new Point2D(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2D AsVector2D()
    {
        return new Vector2D(X, Y);
    }

    #endregion 方法

    #region 符号重载

    /// <summary>
    /// 点p沿着v向量方向加上v向量值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point2D operator +(Point2D p, Vector2D v)
    {
        return new Point2D(p.X + v.X, p.Y + v.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point2D operator -(Point2D p, Vector2D v)
    {
        return new Point2D(p.X - v.X, p.Y - v.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D operator -(Point2D p1, Point2D p2)
    {
        return new Vector2D(p1.X - p2.X, p1.Y - p2.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point2D operator *(Point2D p, double f)
    {
        return new Point2D(p.X * f, p.Y * f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point2D operator *(double f, Point2D p)
    {
        return new Point2D(p.X * f, p.Y * f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point2D operator /(Point2D p, double f)
    {
        return new Point2D(p.X / f, p.Y / f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Point(Point2D a)
    {
        return new Point(a.X, a.Y);
    }

    #endregion 符号重载

    #region 格式化

    public string ToString(IFormatProvider provider)
    {
        return ToString("{0:F}, {1:F}", provider);
    }

    public string ToString(string format = "{0:F}, {1:F}", IFormatProvider? provider = null)
    {
        return provider is null ? string.Format(format, X, Y) : string.Format(provider, format, X, Y);
    }

    public static bool TryParse(string s, out Point2D result)
    {
        var conv = new Point2DConverter();
        if (conv.IsValid(s))
        {
            if (conv.ConvertFrom(s) is Point2D temp)
            {
                result = temp;
                return true;
            }

            result = Zero;
            return false;
        }

        result = Zero;
        return false;
    }

    #endregion 格式化

    #region 静态方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Distance(Point2D p1, Point2D p2)
    {
        return (p1 - p2).Length;
    }

    public static Point2D Average(params Point2D[] points)
    {
        var n = points.Length;
        double x = 0;
        double y = 0;
        foreach (var pt in points)
        {
            x += pt.X / n;
            y += pt.Y / n;
        }

        return new Point2D(x, y);
    }

    public static Point2D Sum(params Point2D[] points)
    {
        double x = 0;
        double y = 0;
        foreach (var pt in points)
        {
            x += pt.X;
            y += pt.Y;
        }

        return new Point2D(x, y);
    }

    #endregion 静态方法
}