namespace Net.Utilities.Models;

/// <summary>
/// 三浮点类型坐标点
/// </summary>
[Serializable]
public struct Point3D(double x, double y, double z) : IEquatable<Point3D>, IFormattable
{
    public static readonly Point3D Empty = new(0, 0, 0);

    private double _x = x;
    private double _y = y;
    private double _z = z;

    public double X
    {
        readonly get => _x;
        set => _x = value;
    }

    public double Y
    {
        readonly get => _y;
        set => _y = value;
    }

    public double Z
    {
        readonly get => _z;
        set => _z = value;
    }

    public readonly bool IsEmpty => _x == 0d && _y == 0d && _z == 0d;

    public Point3D(Point3D p) : this(p.X, p.Y, p.Z)
    {
    }

    #region Object重载

    public readonly bool Equals(Point3D other)
    {
        return _x.Equals(other._x) && _y.Equals(other._y) && _z.Equals(other._z);
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is Point3D other && Equals(other);
    }

    public readonly override int GetHashCode()
    {
        return HashCode.Combine(_x, _y, _z);
    }

    public readonly override string ToString() =>
        $"{nameof(_x)}: {_x}, {nameof(_y)}: {_y}, {nameof(_z)}: {_z}, {nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Z)}: {Z}, {nameof(IsEmpty)}: {IsEmpty}";

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToShortString();
    }

    #endregion Object重载

    #region 操作符重载

    #region 操作符重载方法

    public static Point3D Add(Point3D pt, Size3D sz) => new(pt.X + sz.Width, pt.Y + sz.Height, pt.Z + sz.Depth);

    public static Point3D Subtract(Point3D pt, Size3D sz) => new(pt.X - sz.Width, pt.Y - sz.Height, pt.Z - sz.Depth);

    public static Point3D Add(Point3D pt1, Point3D pt2) => new(pt1.X + pt2.X, pt1.Y + pt2.Y, pt1.Z + pt2.Z);

    public static Point3D Subtract(Point3D pt1, Point3D pt2) => new(pt1.X - pt2.X, pt1.Y - pt2.Y, pt1.Z - pt2.Z);

    public static Point3D Multiply(Point3D pt, int multiple) => new(pt.X * multiple, pt.Y * multiple, pt.Z * multiple);

    #endregion 操作符重载方法

    public static Point3D operator +(Point3D pt, Size3D sz) => Add(pt, sz);

    public static Point3D operator -(Point3D pt, Size3D sz) => Subtract(pt, sz);

    public static Point3D operator +(Point3D pt1, Point3D pt2) => Add(pt1, pt2);

    public static Point3D operator -(Point3D pt1, Point3D pt2) => Subtract(pt1, pt2);

    public static bool operator ==(Point3D left, Point3D right) => left.X - right.X == 0d && left.Y - right.Y == 0d && left.Z - right.Z == 0d;

    public static bool operator !=(Point3D left, Point3D right) => !(left == right);

    public static explicit operator Size3D(Point3D point) => new(point.X, point.Y, point.Z);

    public static Point3D operator *(Point3D pt, int multiple) => Multiply(pt, multiple);

    #endregion 操作符重载

    #region 其他

    public readonly string ToShortString() => $"{_x:f3},{_y:f3},{_z:f3}";

    public readonly double DistanceToZero() => Math.Sqrt(_x * _x + _y * _y + _z * _z);

    #endregion 其他
}