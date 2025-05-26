using Net.Utilities.Mapper.Interfaces;

namespace Net.Utilities.Models;

/// <summary>
/// 双浮点类型坐标点
/// </summary>
[Serializable]
public struct Point(double x, double y) : IEquatable<Point>, IFormattable, ICloneable<Point>, ICloneable
{
    public static readonly Point Empty = new(0, 0);

    private double _x = x;

    private double _y = y;

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

    public readonly bool IsEmpty => _x == 0d && _y == 0d;

    public Point(Point p) : this(p.X, p.Y)
    {
    }

    #region Object重载

    public readonly bool Equals(Point other)
    {
        return _x.Equals(other._x) && _y.Equals(other._y);
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is Point other && Equals(other);
    }

    public readonly override int GetHashCode()
    {
        return HashCode.Combine(_x, _y);
    }

    public readonly Point Clone()
    {
        return new Point(_x, _y);
    }

    readonly object ICloneable.Clone()
    {
        return Clone();
    }

    public readonly override string ToString() => $"{nameof(_x)}: {_x}, {nameof(_y)}: {_y}, {nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(IsEmpty)}: {IsEmpty}";

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToShortString();
    }

    #endregion Object重载

    #region 操作符重载

    #region 操作符重载方法

    public static Point Add(Point pt, Size sz) => new(pt.X + sz.Width, pt.Y + sz.Height);

    public static Point Subtract(Point pt, Size sz) => new(pt.X - sz.Width, pt.Y - sz.Height);

    public static Point Add(Point pt1, Point pt2) => new(pt1.X + pt2.X, pt1.Y + pt2.Y);

    public static Point Subtract(Point pt1, Point pt2) => new(pt1.X - pt2.X, pt1.Y - pt2.Y);

    public static Point Multiply(Point pt, int multiple) => new(pt.X * multiple, pt.Y * multiple);

    #endregion 操作符重载方法

    public static Point operator +(Point pt, Size sz) => Add(pt, sz);

    public static Point operator -(Point pt, Size sz) => Subtract(pt, sz);

    public static Point operator +(Point pt1, Point pt2) => Add(pt1, pt2);

    public static Point operator -(Point pt1, Point pt2) => Subtract(pt1, pt2);

    public static bool operator ==(Point left, Point right) => left.X - right.X == 0d && left.Y - right.Y == 0d;

    public static bool operator !=(Point left, Point right) => !(left == right);

    public static explicit operator Size(Point point) => new(point.X, point.Y);

    public static Point operator *(Point pt, int multiple) => Multiply(pt, multiple);

    #endregion 操作符重载

    #region 其他

    public readonly string ToShortString() => $"{_x:f3},{_y:f3}";

    public readonly double DistanceToZero() => Math.Sqrt(_x * _x + _y * _y);

    #endregion 其他

    #region 解构

    public readonly void Deconstruct(out int intX, out int intY) => (intX, intY) = (Convert.ToInt32(X), Convert.ToInt32(Y));

    public readonly void Deconstruct(out int intX, out int intY, out float floatX, out float floatY)
        => (intX, intY, floatX, floatY) = (Convert.ToInt32(X), Convert.ToInt32(Y), Convert.ToSingle(X), Convert.ToSingle(Y));

    public readonly void Deconstruct(out int intX, out int intY, out float floatX, out float floatY, out double doubleX, out double doubleY)
        => (intX, intY, floatX, floatY, doubleX, doubleY) = (Convert.ToInt32(X), Convert.ToInt32(Y), Convert.ToSingle(X), Convert.ToSingle(Y), X, Y);

    #endregion 解构
}