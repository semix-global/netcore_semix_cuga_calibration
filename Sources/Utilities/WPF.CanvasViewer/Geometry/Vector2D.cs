using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CanvasViewer.Geometry;

/// <summary>
/// 数学向量(x, y): SizeF
/// </summary>
[TypeConverter(typeof(Vector2DConverter))]
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct Vector2D(double x, double y)
{
    /// <summary>
    /// 空向量
    /// </summary>
    public static Vector2D Zero => new(0, 0);

    /// <summary>
    /// y=x方向矢量向量
    /// </summary>
    public static Vector2D One => new(1, 1);

    /// <summary>
    /// x轴单位向量
    /// </summary>
    public static Vector2D XAxis => new(1, 0);

    /// <summary>
    /// y轴单位向量
    /// </summary>
    public static Vector2D YAxis => new(0, 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D FromAngle(double angle) => new(Math.Cos(angle), Math.Sin(angle));

    #region 属性

    /// <summary>
    /// 向量x
    /// </summary>
    public double X { get; } = x;

    /// <summary>
    /// 向量y
    /// </summary>
    public double Y { get; } = y;

    /// <summary>
    /// 向量长度
    /// </summary>
    public double Length => Math.Sqrt(X * X + Y * Y);

    /// <summary>
    /// 与x轴的夹角: + 逆时针, - 顺时针
    /// </summary>
    public double Angle => AngleTo(XAxis);

    /// <summary>
    /// 单位化向量
    /// </summary>
    public Vector2D Normal => new Vector2D(X, Y) / Length;

    /// <summary>
    /// 垂直的向量
    /// </summary>
    public Vector2D Perpendicular => new(-Y, X);

    #endregion 属性

    #region 方法

    /// <summary>
    /// 向量变换
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2D Transform(Matrix2D transformation)
    {
        var x = transformation.M11 * X + transformation.M12 * Y;
        var y = transformation.M21 * X + transformation.M22 * Y;
        return new Vector2D(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point2D AsPoint2D()
    {
        return new Point2D(X, Y);
    }

    /// <summary>
    /// 向量内积
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double DotProduct(Vector2D v)
    {
        return X * v.X + Y * v.Y;
    }

    /// <summary>
    /// 两个向量组成的行列式值: 表示两个向量围成的平行四边形面积
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double CrossProduct(Vector2D v)
    {
        return X * v.Y - Y * v.X;
    }

    /// <summary>
    /// 获取两个向量夹角
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double AngleTo(Vector2D v)
    {
        return ClampAngle(SignedAngleTo(v));
    }

    /// <summary>
    /// 在两个向量之间
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBetween(Vector2D a, Vector2D b)
    {
        var ang = ClampAngle(b.SignedAngleTo(a), true, false);
        var ang1 = ClampAngle(SignedAngleTo(a), true, false);
        var ang2 = ClampAngle(b.SignedAngleTo(this), true, false);

        return Math.Abs(ang2 + ang1 - ang) < 0.0001f;
    }

    /// <summary>
    /// v向量 to this向量 向量需要转的角度: + 逆时针, - 顺时针
    /// 原理: https://file.notion.so/f/f/9dd568af-6047-421c-87c0-fda84db7515d/6e88dc24-78c1-463f-8241-2c860ce191d0/Untitled.png?id=67490900-5325-43d1-a105-b0d45371a966&amp;table=block&amp;spaceId=9dd568af-6047-421c-87c0-fda84db7515d&amp;expirationTimestamp=1694044800000&amp;signature=UjdmMD22coz8FOaE2cJ3XAIgv0BFTckdBDqgZNouCP0&amp;downloadName=Untitled.png
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double SignedAngleTo(Vector2D v)
    {
        var dot = DotProduct(v); // 内积
        var det = X * v.Y - v.X * Y; // 表示两个向量围成的平行四边形面积
        var ang = -Math.Atan2(det, dot); // 反正切
        return ang;
    }

    #endregion 方法

    #region 格式化

    public static bool TryParse(string s, out Vector2D result)
    {
        var conv = new Vector2DConverter();
        if (conv.IsValid(s))
        {
            if (conv.ConvertFrom(s) is Vector2D temp)
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

    public string ToString(IFormatProvider provider)
    {
        return ToString("{0:F}, {1:F}", provider);
    }

    public string ToString(string format = "{0:F}, {1:F}", IFormatProvider? provider = null)
    {
        return provider is null ? string.Format(format, X, Y) : string.Format(provider, format, X, Y);
    }

    #endregion 格式化

    #region 符号重载

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D operator +(Vector2D a, Vector2D b)
    {
        return new Vector2D(a.X + b.X, a.Y + b.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D operator -(Vector2D a, Vector2D b)
    {
        return new Vector2D(a.X - b.X, a.Y - b.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D operator *(Vector2D p, double f)
    {
        return new Vector2D(p.X * f, p.Y * f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D operator *(double f, Vector2D p)
    {
        return new Vector2D(p.X * f, p.Y * f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D operator /(Vector2D p, double f)
    {
        return new Vector2D(p.X / f, p.Y / f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Size(Vector2D a)
    {
        return new Size(a.X, a.Y);
    }

    #endregion 符号重载

    #region 静态方法

    /// <summary>
    /// 夹角 [0, pi/2]
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double ClampAngle(double ang)
    {
        // ReSharper disable once IntroduceOptionalParameters.Local
        return ClampAngle(ang, true, true);
    }

    /// <summary>
    /// 夹角
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double ClampAngle(double ang, bool low, bool high)
    {
        if (low)
        {
            while (ang < 0) ang += 2 * Math.PI;
        }

        if (high)
        {
            while (ang > 2 * Math.PI) ang -= 2 * Math.PI;
        }

        return ang;
    }

    #endregion 静态方法
}