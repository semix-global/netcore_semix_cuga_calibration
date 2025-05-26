namespace Net.Utilities.Models;

/// <summary>
/// 三浮点尺寸
/// </summary>
[Serializable]
public struct Size3D(double width, double height, double depth) : IEquatable<Size3D>, IFormattable
{
    public static readonly Size3D Empty = new(0, 0, 0);

    private double _width = width;
    private double _height = height;
    private double _depth = depth;

    public double Width
    {
        readonly get => _width;
        set => _width = value;
    }

    public double Height
    {
        readonly get => _height;
        set => _height = value;
    }

    public double Depth
    {
        readonly get => _depth;
        set => _depth = value;
    }

    public readonly bool IsEmpty => _width == 0d && _height == 0d && _depth == 0d;

    public Size3D(Size3D size) : this(size._width, size._height, size._depth)
    {
    }

    public Size3D(Point3D pt) : this(pt.X, pt.Y, pt.Z)
    {
    }

    #region Object重载

    public readonly bool Equals(Size3D other)
    {
        return _width.Equals(other._width) && _height.Equals(other._height) && _depth.Equals(other._depth);
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is Size3D other && Equals(other);
    }

    public readonly override int GetHashCode()
    {
        return HashCode.Combine(_width, _height, _depth);
    }

    public readonly override string ToString() => $"{{Width={_width}, Height={_height}, Depth={_depth}}}";

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToShortString();
    }

    #endregion Object重载

    #region 操作符重载

    #region 操作符重载方法

    public static Size3D Add(Size3D sz1, Size3D sz2) => new(sz1.Width + sz2.Width, sz1.Height + sz2.Height, sz1.Depth + sz2.Depth);

    public static Size3D Subtract(Size3D sz1, Size3D sz2) => new(sz1.Width - sz2.Width, sz1.Height - sz2.Height, sz1.Depth - sz2.Depth);

    private static Size3D Multiply(Size3D size, double multiplier) => new(size._width * multiplier, size._height * multiplier, size._depth * multiplier);

    public readonly Point3D ToPoint3D() => (Point3D)this;

    #endregion 操作符重载方法

    public static Size3D operator +(Size3D sz1, Size3D sz2) => Add(sz1, sz2);

    public static Size3D operator -(Size3D sz1, Size3D sz2) => Subtract(sz1, sz2);

    public static Size3D operator *(double left, Size3D right) => Multiply(right, left);

    public static Size3D operator *(Size3D left, double right) => Multiply(left, right);

    public static Size3D operator /(Size3D left, double right) => new(left._width / right, left._height / right, left._depth / right);

    public static bool operator ==(Size3D sz1, Size3D sz2) => sz1.Width - sz2.Width == 0d && sz1.Height - sz2.Height == 0d && sz1.Depth - sz2.Depth == 0d;

    public static bool operator !=(Size3D sz1, Size3D sz2) => !(sz1 == sz2);

    public static explicit operator Point3D(Size3D size) => new(size.Width, size.Height, size.Depth);

    #endregion 操作符重载

    #region 其他

    public readonly string ToShortString() => $"{_width:f3},{_height:f3},{_depth:f3}";

    public readonly double DiagonalDistance => Math.Sqrt(_width * _width + _height * _height + _depth * _depth);

    #endregion 其他
}