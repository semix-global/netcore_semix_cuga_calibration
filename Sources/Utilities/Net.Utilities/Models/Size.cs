namespace Net.Utilities.Models;

/// <summary>
/// 双浮点尺寸
/// </summary>
[Serializable]
public struct Size(double width, double height) : IEquatable<Size>, IFormattable
{
    public static readonly Size Empty = new(0, 0);

    private double _width = width;

    private double _height = height;

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

    public readonly bool IsEmpty => _width == 0d && _height == 0d;

    public readonly double DiagonalDistance => Math.Sqrt(_width * _width + _height * _height);

    public Size(Size size) : this(size._width, size._height)
    {
    }

    public Size(Point pt) : this(pt.X, pt.Y)
    {
    }

    #region Object重载

    public readonly bool Equals(Size other)
    {
        return _width.Equals(other._width) && _height.Equals(other._height);
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is Size other && Equals(other);
    }

    public readonly override int GetHashCode()
    {
        return HashCode.Combine(_width, _height);
    }

    public readonly override string ToString() => $"{{Width={_width}, Height={_height}}}";

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToShortString();
    }

    #endregion Object重载

    #region 操作符重载

    #region 操作符重载方法

    public static Size Add(Size sz1, Size sz2) => new(sz1.Width + sz2.Width, sz1.Height + sz2.Height);

    public static Size Subtract(Size sz1, Size sz2) => new(sz1.Width - sz2.Width, sz1.Height - sz2.Height);

    private static Size Multiply(Size size, double multiplier) => new(size._width * multiplier, size._height * multiplier);

    public readonly Point ToPoint() => (Point)this;

    #endregion 操作符重载方法

    public static Size operator +(Size sz1, Size sz2) => Add(sz1, sz2);

    public static Size operator -(Size sz1, Size sz2) => Subtract(sz1, sz2);

    public static Size operator *(double left, Size right) => Multiply(right, left);

    public static Size operator *(Size left, double right) => Multiply(left, right);

    public static Size operator /(Size left, double right) => new(left._width / right, left._height / right);

    public static bool operator ==(Size sz1, Size sz2) => sz1.Width - sz2.Width == 0d && sz1.Height - sz2.Height == 0d;

    public static bool operator !=(Size sz1, Size sz2) => !(sz1 == sz2);

    public static explicit operator Point(Size size) => new(size.Width, size.Height);

    #endregion 操作符重载

    #region 其他

    public readonly string ToShortString() => $"{_width:f3},{_height:f3}";

    #endregion 其他

    #region 解构

    public readonly void Deconstruct(out int intWidth, out int intHeight) => (intWidth, intHeight) = (Convert.ToInt32(Width), Convert.ToInt32(Height));

    public readonly void Deconstruct(out int intWidth, out int intHeight, out float floatWidth, out float floatHeight)
        => (intWidth, intHeight, floatWidth, floatHeight) = (Convert.ToInt32(Width), Convert.ToInt32(Height), Convert.ToSingle(Width), Convert.ToSingle(Height));

    public readonly void Deconstruct(out int intWidth, out int intHeight, out float floatWidth, out float floatHeight, out double doubleWidth, out double doubleHeight)
        => (intWidth, intHeight, floatWidth, floatHeight, doubleWidth, doubleHeight) = (Convert.ToInt32(Width), Convert.ToInt32(Height), Convert.ToSingle(Width), Convert.ToSingle(Height), Width, Height);

    #endregion 解构
}