namespace CanvasViewer.Helper;

internal static class MathHelper
{
    /// <summary>
    /// ε 最小值
    /// </summary>
    public const double Epsilon = 1e-7f;

    /// <summary>
    /// tau 是一个圆周常数, 等于2π
    /// </summary>
    public const double Tau = 6.28318530717958647692528676655900576839433879875021164d;

    /// <summary>
    /// 是不是0
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>是不是0</returns>
    public static bool IsZero(double value)
    {
        return -Epsilon < value && value < Epsilon;
    }

    /// <summary>
    /// 是不是相等
    /// </summary>
    /// <param name="a">值a</param>
    /// <param name="b">值b</param>
    /// <returns>是不是相等</returns>
    public static bool IsEqual(double a, double b)
    {
        return IsZero(a - b);
    }

    /// <summary>
    /// 比较值a和值b大小
    /// </summary>
    /// <param name="a">值a</param>
    /// <param name="b">值b</param>
    /// <returns>a==b返回0; a&lt;b返回-1; a>b返回1</returns>
    public static int Compare(double a, double b)
    {
        return IsEqual(a, b) ? 0 : a < b ? -1 : 1;
    }

    /// <summary>
    /// 判断值v是不是在 a, b 范围内
    /// </summary>
    /// <param name="v">值</param>
    /// <param name="a">范围a</param>
    /// <param name="b">范围b</param>
    /// <param name="includeEnds">true: 闭区间 false: 开区间</param>
    /// <returns>是否在范围内</returns>
    public static bool IsBetween(double v, double a, double b, bool includeEnds)
    {
        if (a > b) Swap(ref a, ref b);

        if (includeEnds && (IsEqual(v, a) || IsEqual(v, b)))
            return true;
        if (includeEnds == false && (IsEqual(v, a) || IsEqual(v, b)))
            return false;

        return v > a && v < b;
    }

    /// <summary>
    /// 返回角度在[0, pi/2]内
    /// </summary>
    /// <param name="a">夹角</param>
    /// <returns>角度[0, pi/2]内</returns>
    public static double NormalizeAngle(double a)
    {
        while (a < 0) a += Tau;
        while (a > Tau) a -= Tau;
        return a;
    }

    /// <summary>
    /// 判断角度v是不是在 a, b 范围内
    /// </summary>
    /// <param name="v">值</param>
    /// <param name="a">范围a</param>
    /// <param name="b">范围b</param>
    /// <param name="includeEnds">true: 闭区间 false: 开区间</param>
    /// <returns>是否在范围内</returns>
    public static bool IsBetweenAngle(double v, double a, double b, bool includeEnds)
    {
        a = NormalizeAngle(a);
        b = NormalizeAngle(b);
        v = NormalizeAngle(v);

        if (includeEnds && (IsEqual(v, a) || IsEqual(v, b)))
            return true;
        if (includeEnds == false && (IsEqual(v, a) || IsEqual(v, b)))
            return false;

        if (b > a)
            return v > a && v < b;
        else
            return v > a && v <= Tau && v >= 0 && v < b;
    }

    /// <summary>
    /// 将值转换到[min, max]之间数, 超过范围取闭区间边界
    /// </summary>
    /// <param name="val">数</param>
    /// <param name="min">最小</param>
    /// <param name="max">最大</param>
    /// <returns>[min, max]之间数</returns>
    public static double Clamp(double val, double min, double max)
    {
        return val < min ? min : val > max ? max : val;
    }

    /// <summary>
    /// 交换 a,b 值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="a">值a</param>
    /// <param name="b">值b</param>
    public static void Swap<T>(ref T a, ref T b)
    {
        (a, b) = (b, a);
    }

    /// <summary>
    /// 二维行列式: Returns the determinant of the given 2x2 matrix.
    /// | a1 b1 |
    /// | a2 b2 |
    /// </summary>
    /// <param name="a1">a11</param>
    /// <param name="b1">a12</param>
    /// <param name="a2">a21</param>
    /// <param name="b2">a22</param>
    /// <returns>行列式值</returns>
    public static double Determinant(double a1, double b1, double a2, double b2)
    {
        return a1 * b2 - a2 * b1;
    }
}