namespace Net.Utilities.Algorithm.MathNet.Helper;

public static class MathHelper
{
    /// <summary>
    /// 角度转弧度
    /// </summary>
    /// <param name="degreesAngle">角度</param>
    /// <returns>弧度</returns>
    public static double DegreeAngleToRadianAngle(double degreesAngle)
    {
        return degreesAngle * (Math.PI / 180d);
    }

    /// <summary>
    /// 弧度转角度
    /// </summary>
    /// <param name="radiansAngle">弧度</param>
    /// <returns>角度</returns>
    public static double RadianAngleToDegreeAngle(double radiansAngle)
    {
        return radiansAngle * (180d / Math.PI);
    }

    /// <summary>
    /// 计算已知斜率的两条直线的夹角(k2到顺时针旋转到k1的夹角)
    /// Math.Atan(k2) - Math.Atan(k1) = Math.Atan((k2 - k1) / (1 + k2 * k1))
    /// </summary>
    /// <param name="k2">始边斜率</param>
    /// <param name="k1">终边斜率</param>
    /// <returns>两条直线的夹角(弧度), 范围 (-pi/2, pi/2)</returns>
    public static double TwoLineToIncludedRadianAngle(double k2, double k1)
    {
        // 检查是否有任一斜率是无限大
        if (double.IsInfinity(k2) == false && double.IsInfinity(k1) == false) return Math.Atan((k2 - k1) / (1 + k2 * k1));

        // 如果两条直线斜率都是无限（垂直）
        if (double.IsInfinity(k2) && double.IsInfinity(k1)) return 0;

        // 如果其中一条直线斜率是无限, 算出另一条直线的角度
        var finiteAngle = double.IsInfinity(k2) ? Math.Atan(k1) : Math.Atan(k2);

        // 与π/2的差值给出夹角, IsInfinity只能是, pi/2角度
        var angle = double.IsInfinity(k2) ? Math.PI / 2 - finiteAngle : finiteAngle - Math.PI / 2;

        // 角度不在(-pi/2, pi/2)范围内
        switch (angle)
        {
            case > Math.PI / 2:
                angle -= Math.PI;
                break;

            case < -Math.PI / 2:
                angle += Math.PI;
                break;
        }

        return angle;
    }

    /// <summary>
    /// 计算内接正方形的边长
    /// </summary>
    /// <param name="radius">半径</param>
    /// <returns>内接正方形的边长</returns>
    public static double CalculateInscribedSquareSide(double radius)
    {
        return radius * Math.Sqrt(2);
    }

    /// <summary>
    /// 计算外接正方形的边长
    /// </summary>
    /// <param name="radius">半径</param>
    /// <returns>外接正方形的边长</returns>
    public static double CalculateCircumscribedSquareSide(double radius)
    {
        return 2 * radius;
    }
}