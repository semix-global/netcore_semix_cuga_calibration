using Net.Utilities.Algorithm.MathNet.Helper;
using Net.Utilities.Models;

namespace Net.Utilities.Extensions;

public static class PointExtension
{
    /// <summary>
    /// 相加
    /// </summary>
    public static Point Offset(this Point point, double dx, double dy)
    {
        point.X += dx;
        point.Y += dy;
        return point;
    }

    /// <summary>
    /// 相减
    /// </summary>
    public static Point Subtract(this Point point, double dx, double dy)
    {
        point.X -= dx;
        point.Y -= dy;
        return point;
    }

    /// <summary>
    /// 围绕原点的逆时针旋转<br/>
    /// [ cos(angel)  -sin(angel) ]<br/>
    /// [ sin(angel)   cos(angel) ]<br/>
    /// </summary>
    /// <param name="point">坐标</param>
    /// <param name="radianAngle">围绕原点的旋转的弧度</param>
    /// <returns>旋转后的位置</returns>
    public static Point RadianAngleByOrigin(this Point point, double radianAngle)
    {
        return new Point(
            point.X * Math.Cos(radianAngle) - point.Y * Math.Sin(radianAngle),
            point.X * Math.Sin(radianAngle) + point.Y * Math.Cos(radianAngle));
    }

    /// <summary>
    /// 围绕原点的逆时针旋转<br/>
    /// [ cos(angel)  -sin(angel) ]<br/>
    /// [ sin(angel)   cos(angel) ]<br/>
    /// </summary>
    /// <param name="point">坐标</param>
    /// <param name="degreeAngle">围绕原点的旋转的角度</param>
    /// <returns>旋转后的位置</returns>
    public static Point DegreeAngleByOrigin(this Point point, double degreeAngle)
    {
        return RadianAngleByOrigin(point, MathHelper.DegreeAngleToRadianAngle(degreeAngle));
    }

    /// <summary>
    /// 坐标系的逆时针旋转<br/>
    /// [  cos(angel)   sin(angel) ]<br/>
    /// [ -sin(angel)   cos(angel) ]<br/>
    /// </summary>
    /// <param name="point">坐标</param>
    /// <param name="radianAngle">围绕原点的旋转的弧度</param>
    /// <returns>旋转后的位置</returns>
    public static Point RadianAngleByXy(this Point point, double radianAngle)
    {
        return new Point(
            point.X * Math.Cos(radianAngle) + point.Y * Math.Sin(radianAngle),
            -point.X * Math.Sin(radianAngle) + point.Y * Math.Cos(radianAngle));
    }

    /// <summary>
    /// 坐标系的逆时针旋转<br/>
    /// [  cos(angel)   sin(angel) ]<br/>
    /// [ -sin(angel)   cos(angel) ]<br/>
    /// </summary>
    /// <param name="point">坐标</param>
    /// <param name="degreeAngle">围绕原点的旋转的角度</param>
    /// <returns>旋转后的位置</returns>
    public static Point DegreeAngleByXy(this Point point, double degreeAngle)
    {
        return RadianAngleByXy(point, MathHelper.DegreeAngleToRadianAngle(degreeAngle));
    }

    /// <summary>
    /// 关于x轴对称变换<br/>
    /// (x,y) => (x,-y)
    /// </summary>
    /// <returns>旋转后的位置</returns>
    public static Point XAxialSymmetry(this Point point)
    {
        point.Y = -point.Y;
        return point;
    }

    /// <summary>
    /// 关于y轴对称变换<br/>
    /// (x,y) => (-x,y)
    /// </summary>
    /// <returns>旋转后的位置</returns>
    public static Point YAxialSymmetry(this Point point)
    {
        point.X = -point.X;
        return point;
    }

    /// <summary>
    /// 关于原点对称变换<br/>
    /// (x,y) => (-x,-y)
    /// </summary>
    /// <returns>旋转后的位置</returns>
    public static Point OriginSymmetry(this Point point)
    {
        point.X = -point.X;
        point.Y = -point.Y;
        return point;
    }

    /// <summary>
    /// 关于y=x对称变换<br/>
    /// (x,y) => (y,x)
    /// </summary>
    /// <returns>旋转后的位置</returns>
    public static Point InverseXyAxialSymmetry(this Point point)
    {
        point.X = point.Y;
        point.Y = point.X;
        return point;
    }

    /// <summary>
    /// 判断点是否在圆内
    /// </summary>
    /// <param name="point">点</param>
    /// <param name="center">圆心</param>
    /// <param name="diameter">直径</param>
    /// <returns>是否在圆内</returns>
    public static bool IsPointInCircle(this Point point, Point center, double diameter)
    {
        // 计算半径
        var radius = diameter / 2d;

        // 计算点到圆心的距离
        var distance = (point - center).DistanceToZero();

        // 判断点是否在圆内
        return distance < radius;
    }

    /// <summary>
    /// 一维 -> 二维
    /// </summary>
    /// <param name="list">一维</param>
    /// <returns>二维</returns>
    public static Point[] ToPoints(this IEnumerable<double> list)
    {
        return [.. list.Select((t, i) => new Point(i, t))];
    }
}