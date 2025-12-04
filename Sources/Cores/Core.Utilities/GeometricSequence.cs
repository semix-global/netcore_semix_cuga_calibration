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
            {
                if (firstValue >= minValue)
                {
                    sequence.Add(firstValue);
                }

                break;
            }
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
    
    public static double[] SolveForX(double P0, double P1, double P2, double P3, double yTarget)
    {
        var roots = Cubic.Roots(P0-yTarget, P1, P2, P3);
        
        var resultList = new List<double>();
        
        if(roots.Item1.IsReal()) resultList.Add(roots.Item1.Real);
        if(roots.Item2.IsReal()) resultList.Add(roots.Item2.Real);
        if(roots.Item3.IsReal()) resultList.Add(roots.Item3.Real);
        
        return resultList.ToArray();
    }
}