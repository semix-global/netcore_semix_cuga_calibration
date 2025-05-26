using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace CanvasViewer.Geometry;

/// <summary>
/// 笛卡尔坐标系仿射矩阵: Matrix2D类对应矩阵Matrix:<br/>
/// [M11, M12, Dx]<br/>
/// [M21, M22, Dy]<br/>
/// [0,   0,   1]<br/>
/// <code>
/// 缩放矩阵
/// [M11, 0,   0]
/// [0,   M22, 0]
/// [0,   0,   1]
/// 旋转矩阵
/// [cosθ, -sinθ, 0]
/// [sinθ,  cosθ, 0]
/// [0,     0,    1]
/// 平移矩阵
/// [1, 0, Dx]
/// [0, 1, Dy]
/// [0, 0,  1]
/// </code>
/// <code>
/// 点[x, y]进行变换:<br/>
/// [M11, M12, Dx]   [x]    [M11 * x + M12 * y + Dx]<br/>
/// [M21, M22, Dy] * [y] =  [M21 * x + M22 * y + Dy]<br/>
/// [0,   0,   1 ]   [1]    [1                     ]<br/>
/// 多次变换:<br/>
///                                     [x]        [x']<br/>
/// M1(3*3) * M2(3*3) * ... * Mn(3*3) * [y]      = [y']<br/>
///                                     [1](3*1)   [1 ]<br/>
/// 矩阵乘法结合律, 可以M1(3*3) * M2(3*3) * ... * Mn(3*3) = M(3*3)<br/>
/// 之后再和坐标相乘提高计算速度<br/>
/// </code>
/// </summary>
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct Matrix2D(double m11, double m12, double m21, double m22, double dx, double dy)
{
    public static Matrix2D Identity => new(1, 0, 0, 1, 0, 0);

    #region 属性

    /// <summary>
    /// 三维矩阵a11: 表示 x 缩放因子. 这个值决定了沿 x 轴方向的缩放变换
    /// </summary>
    public double M11 { get; } = m11;

    /// <summary>
    /// 三维矩阵a12: 通常为零, 表示无剪切
    /// </summary>
    public double M12 { get; } = m12;

    /// <summary>
    /// 三维矩阵a21: 通常也为零, 表示无剪切
    /// </summary>
    public double M21 { get; } = m21;

    /// <summary>
    /// 三维矩阵a22: 表示 y 缩放因子. 这个值决定了沿 y 轴方向的缩放变换
    /// </summary>
    public double M22 { get; } = m22;

    /// <summary>
    /// 三维矩阵a13: 表示 x 平移量. 这个值决定了沿 x 轴方向的平移变换
    /// </summary>
    public double Dx { get; } = dx;

    /// <summary>
    /// 三维矩阵a23: 表示 y 平移量. 这个值决定了沿 y 轴方向的平移变换
    /// </summary>
    public double Dy { get; } = dy;

    /// <summary>
    /// 旋转角度
    /// </summary>
    public double RotationAngle => Vector2D.XAxis.Transform(this).Angle;

    /// <summary>
    /// 获取一个值，该值指示此 Matrix 是否是单位矩阵
    /// </summary>
    // ReSharper disable CompareOfFloatsByEqualityOperator
    public bool IsIdentity => M11 == 1 && M12 == 0 && M21 == 0 && M22 == 1 && Dx == 0 && Dy == 0;

    /// <summary>
    /// <code>
    /// [M11, M12, Dx]<br/>
    /// [M21, M22, Dy]<br/>
    /// [0,   0,   1]<br/>
    /// </code>
    /// 三维矩阵的逆, 伴随矩阵求法
    /// </summary>
    public Matrix2D Inverse
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var det = M11 * M22 - M12 * M21; // 行列式
            var invDet = 1 / det;

            var m11 = M22 * invDet;
            var m12 = -M12 * invDet;
            var dx = (M12 * Dy - Dx * M22) * invDet;
            var m21 = -M21 * invDet;
            var m22 = M11 * invDet;
            var dy = (M21 * Dx - M11 * Dy) * invDet;

            return new Matrix2D(m11, m12, m21, m22, dx, dy);
        }
    }

    #endregion 属性

    #region 构造

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix2D(Matrix m) : this(m.M11, m.M12, m.M21, m.M22, m.OffsetX,
        m.OffsetY)
    {
    }

    #endregion 构造

    #region 操作符重构

    /// <summary>
    /// <code>
    /// [M11, M12, Dx]<br/>
    /// [M21, M22, Dy]<br/>
    /// [0,   0,   1]<br/>
    /// </code>
    /// 矩阵乘法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2D operator *(Matrix2D a, Matrix2D b)
    {
        var m11 = a.M11 * b.M11 + a.M12 * b.M21 + a.Dx * 0;
        var m12 = a.M11 * b.M12 + a.M12 * b.M22 + a.Dx * 0;
        var dx = a.M11 * b.Dx + a.M12 * b.Dy + a.Dx * 1;
        var m21 = a.M21 * b.M11 + a.M22 * b.M21 + a.Dy * 0;
        var m22 = a.M21 * b.M12 + a.M22 * b.M22 + a.Dy * 0;
        var dy = a.M21 * b.Dx + a.M22 * b.Dy + a.Dy * 1;
        return new Matrix2D(m11, m12, m21, m22, dx, dy);
    }

    /// <summary>
    /// 旋转变换矩阵Matrix: https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.matrix?view=windowsdesktop-8.0
    /// </summary>
    public static explicit operator Matrix(Matrix2D t)
    {
        return new Matrix(t.M11, t.M12, t.M21, t.M22, t.Dx, t.Dy);
    }

    #endregion 操作符重构

    #region 静态方法

    /// <summary>
    /// 变换
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2D Transformation(double xScale, double yScale, double rotation, double dx, double dy)
    {
        var m11 = xScale * Math.Cos(rotation);
        var m12 = -Math.Sin(rotation);
        var m21 = Math.Sin(rotation);
        var m22 = yScale * Math.Cos(rotation);

        return new Matrix2D(m11, m12, m21, m22, dx, dy);
    }

    /// <summary>
    /// 缩放
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2D Scale(double xScale, double yScale)
    {
        return new Matrix2D(xScale, 0, 0, yScale, 0, 0);
    }

    /// <summary>
    /// 缩放
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2D Scale(double uniformScale)
    {
        return Scale(uniformScale, uniformScale);
    }

    /// <summary>
    /// 缩放
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2D Scale(Point2D basePoint, double xScale, double yScale)
    {
        return Translation(basePoint.X, basePoint.Y) *
               Scale(xScale, yScale) *
               Translation(-basePoint.X, -basePoint.Y);
    }

    /// <summary>
    /// 缩放
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2D Scale(Point2D basePoint, double uniformScale)
    {
        return Scale(basePoint, uniformScale, uniformScale);
    }

    /// <summary>
    /// 镜像
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2D Mirror(Point2D basePoint, Vector2D direction)
    {
        return Translation(basePoint.X, basePoint.Y) *
               Rotation(direction.Angle) *
               Scale(1, -1) *
               Rotation(-direction.Angle) *
               Translation(-basePoint.X, -basePoint.Y);
    }

    /// <summary>
    /// 旋转
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2D Rotation(double rotation)
    {
        var m11 = Math.Cos(-rotation);
        var m12 = Math.Sin(-rotation);
        var m21 = -Math.Sin(-rotation);
        var m22 = Math.Cos(-rotation);

        return new Matrix2D(m11, m12, m21, m22, 0, 0);
    }

    /// <summary>
    /// 旋转
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2D Rotation(Point2D basePoint, double rotation)
    {
        return Translation(basePoint.X, basePoint.Y) *
               Rotation(rotation) *
               Translation(-basePoint.X, -basePoint.Y);
    }

    /// <summary>
    /// 平移
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2D Translation(Vector2D delta)
    {
        return Translation(delta.X, delta.Y);
    }

    /// <summary>
    /// 平移
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2D Translation(double dx, double dy)
    {
        return new Matrix2D(1, 0, 0, 1, dx, dy);
    }

    #endregion 静态方法

    public override string ToString()
    {
        var str =
            $"|{M11}, {M12}, {Dx}|{Environment.NewLine}" +
            $"|{M21}, {M22}, {Dy}|{Environment.NewLine}" +
            "|0, 0, 1|";
        return str;
    }
}