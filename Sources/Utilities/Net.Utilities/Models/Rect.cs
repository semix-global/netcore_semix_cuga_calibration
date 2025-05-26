namespace Net.Utilities.Models;

/// <summary>
/// 双浮点矩形
/// </summary>
[Serializable]
public struct Rect(double x, double y, double width, double height) : IEquatable<Rect>, IFormattable
{
    public static readonly Rect Empty = new(0, 0, 0, 0);

    private double _x = x;
    private double _y = y;
    private double _width = width;
    private double _height = height;

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

    public Point Point
    {
        readonly get => new(X, Y);
        set
        {
            X = value.X;
            Y = value.Y;
        }
    }

    public Size Size
    {
        readonly get => new(Width, Height);
        set
        {
            Width = value.Width;
            Height = value.Height;
        }
    }

    public Rect(Point location, Size size) : this(location.X, location.Y, size.Width, size.Height)
    {
    }

    #region Object重载

    public readonly bool Equals(Rect other)
    {
        return _x.Equals(other._x) && _y.Equals(other._y) && _width.Equals(other._width) && _height.Equals(other._height);
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is Rect other && Equals(other);
    }

    public readonly override int GetHashCode()
    {
        return HashCode.Combine(_x, _y, _width, _height);
    }

    public readonly override string ToString() => $"{{X={_x}, Y={_y}, Width={_width}, Height={_height}}}";

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToShortString();
    }

    #endregion Object重载

    #region 操作符重载

    public static bool operator ==(Rect left, Rect right) => left.X - right.X == 0d && left.Y - right.Y == 0d && left.Width - right.Width == 0d && left.Height - right.Height == 0d;

    public static bool operator !=(Rect left, Rect right) => !(left == right);

    #endregion 操作符重载

    #region 其他

    public readonly bool Contains(double x, double y) => X <= x && x < X + Width && Y <= y && y < Y + Height;

    public readonly bool Contains(Point point) => Contains(point.X, point.Y);

    public readonly bool Contains(Rect rect) => X <= rect.X && rect.X + rect.Width <= X + Width && Y <= rect.Y && rect.Y + rect.Height <= Y + Height;

    public readonly string ToShortString() => $"{_x:f3},{_y:f3},{_width:f3},{_height:f3}";

    #endregion 其他
}