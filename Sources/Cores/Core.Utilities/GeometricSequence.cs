using CommunityToolkit.Diagnostics;
using MathNet.Numerics;
using MathNet.Numerics.RootFinding;

namespace Core.Utilities;

public static class GeometricSequence
{
    /// <summary>
    /// 生成等比数列
    /// </summary>
    /// <param name="firstValue">第一个值</param>
    /// <param name="ratio">等比数列的比例</param>
    /// <param name="minValue">最小值阈值</param>
    /// <returns>返回等比数列列表</returns>
    public static IReadOnlyList<double> Generate(double firstValue, double ratio, double minValue)
    {
        Guard.IsNotEqualTo(ratio, 0);

        var sequence = new List<double>();

        var current = firstValue;

        switch (ratio)
        {
            // 如果比例大于0且小于1，或者比例小于0，数列递减
            case > 0 and < 1:
            case < 0:
                while (current >= minValue)
                {
                    sequence.Add(current);
                    current *= ratio;
                }

                break;
            // 如果比例大于1，数列递增（需要调整逻辑）
            case > 1:
                // 递增情况下，第一个值应该大于等于最小值
                if (firstValue >= minValue)
                {
                    sequence.Add(firstValue);
                }

                break;
            // 如果比例等于1，只返回第一个值
            case 1:
                if (firstValue >= minValue)
                {
                    sequence.Add(firstValue);
                }

                break;
        }

        return sequence;
    }

    /// <summary>
    /// 生成等比数列（指定项数）
    /// </summary>
    /// <param name="firstValue">第一个值</param>
    /// <param name="ratio">等比数列的比例</param>
    /// <param name="count">生成的项数</param>
    /// <returns>返回等比数列列表</returns>
    public static IReadOnlyList<double> GenerateByCount(double firstValue, double ratio, int count)
    {
        Guard.IsNotEqualTo(ratio, 0);

        var sequence = new List<double>();

        if (count <= 0) return sequence;

        var current = firstValue;

        for (var i = 0; i < count; i++)
        {
            sequence.Add(current);
            current *= ratio;
        }

        return sequence;
    }

    public static double[] SolveForX(double p0, double p1, double p2, double p3, double yTarget)
    {
        // 化简：p3*x^3 + p2*x^2 + p1*x + (p0 - yTarget) = 0
        double a0 = p0 - yTarget;
        double a1 = p1;
        double a2 = p2;
        double a3 = p3;

        // 标准化成：x^3 + b2*x^2 + b1*x + b0 = 0
        if (Math.Abs(a3) < 1e-12)
            throw new Exception("p3 不能为 0，这是三次方程求根器");

        double b0 = a0 / a3;
        double b1 = a1 / a3;
        double b2 = a2 / a3;

        // 解三次方程
        var (x1, x2, x3) = Cubic.RealRoots(b0, b1, b2);

        List<double> roots = new();

        if (!double.IsNaN(x1)) roots.Add(x1);
        if (!double.IsNaN(x2)) roots.Add(x2);
        if (!double.IsNaN(x3)) roots.Add(x3);

        return roots.ToArray();
    }
}