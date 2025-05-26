using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CanvasViewer.Geometry;

/// <summary>
/// 分割直线
/// </summary>
[TypeConverter(typeof(Segment2DConverter))]
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct Segment2D(Point2D p1, Point2D p2)
{
    #region 属性

    /// <summary>
    /// 线段起点P1
    /// </summary>
    public Point2D P1 { get; } = p1;

    /// <summary>
    /// 线段终点P2
    /// </summary>
    public Point2D P2 { get; } = p2;

    /// <summary>
    /// 线段起点P1 X
    /// </summary>
    public double X1 => P1.X;

    /// <summary>
    /// 线段起点P1 Y
    /// </summary>
    public double Y1 => P1.Y;

    /// <summary>
    /// 线段起点P2 X
    /// </summary>
    public double X2 => P2.X;

    /// <summary>
    /// 线段起点P2 Y
    /// </summary>
    public double Y2 => P2.Y;

    /// <summary>
    /// 线段单位向量(指向终点P2)
    /// </summary>
    public Vector2D Direction => (P2 - P1).Normal;

    /// <summary>
    /// 线段长度
    /// </summary>
    public double Length => (P2 - P1).Length;

    #endregion 属性

    #region 构造

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Segment2D(double x1, double y1, double x2, double y2)
        : this(new Point2D(x1, y1), new Point2D(x2, y2))
    {
    }

    #endregion 构造

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Segment2D Transform(Matrix2D transformation)
    {
        var p1 = P1.Transform(transformation);
        var p2 = P2.Transform(transformation);
        return new Segment2D(p1, p2);
    }

    /// <summary>
    /// 点到线段距离, 是否在tolerance内
    /// </summary>
    /// <param name="pt">点</param>
    /// <param name="tolerance">范围</param>
    /// <param name="t">点到线段距离</param>
    /// <returns>是否在tolerance内</returns>
    public bool Contains(Point2D pt, double tolerance, out double t)
    {
        var w = pt - P1;
        var vL = P2 - P1;
        t = w.DotProduct(vL) / vL.DotProduct(vL); // 获取向量投影比例: https://www.bilibili.com/read/cv22449403/
        var dist = (w - t * vL /*w的向量投影*/).Length; // 两个向量相减获取垂直向量: 从而获得点到直线距离
        return dist < tolerance;
    }

    #region 格式化

    public string ToString(IFormatProvider provider)
    {
        return ToString("{0:F}, {1:F} : {2:F}, {3:F}", provider);
    }

    public string ToString(string format = "{0:F}, {1:F} : {2:F}, {3:F}", IFormatProvider? provider = null)
    {
        return provider is null
            ? string.Format(format, X1, Y1, X2, Y2)
            : string.Format(provider, format, X1, Y1, X2, Y2);
    }

    public static bool TryParse(string s, out Segment2D result)
    {
        var conv = new Segment2DConverter();
        if (conv.IsValid(s))
        {
            if (conv.ConvertFrom(s) is Segment2D temp)
            {
                result = temp;
                return true;
            }

            result = new Segment2D();
            return false;
        }

        result = new Segment2D();
        return false;
    }

    #endregion 格式化

    #region 操作符重构

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Segment2D operator +(Segment2D s, Vector2D v)
    {
        return new Segment2D(s.P1 + v, s.P2 + v);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Segment2D operator -(Segment2D s, Vector2D v)
    {
        return new Segment2D(s.P1 - v, s.P2 - v);
    }

    #endregion 操作符重构
}