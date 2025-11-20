using CommunityToolkit.Diagnostics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;

namespace Core.Utilities;

public static class Extremumor
{
    public static (Vector<double> X, Vector<double> Y) FindMaxima(
        Vector<double> x,
        Vector<double> y,
        double threshold = 0,
        bool isContainsEdge = false) => FindExtrema(x, y, ExtremumTypeEnum.Maximum, threshold, isContainsEdge);

    private enum ExtremumTypeEnum
    {
        Maximum,
        Minimum
    }

    public static (Vector<double> X, Vector<double> Y) FindMinima(
        Vector<double> x,
        Vector<double> y,
        double threshold = 0,
        bool isContainsEdge = false) => FindExtrema(x, y, ExtremumTypeEnum.Minimum, threshold, isContainsEdge);

    /// <summary>
    /// 从每组极值中各选一个点，使得所有选中点的值最接近（跨度最小）
    /// </summary>
    /// <param name="extremums">多组极值列表，每组代表一条曲线的极值点</param>
    /// <returns>最优组合（索引和值）以及最小跨度</returns>
    public static (IReadOnlyList<(int Index, double Value)> Results, double Span) FindClosestExtremum(IReadOnlyList<Vector<double>> extremums)
    {
        Guard.IsNotEmpty(extremums);
        Guard.IsTrue(extremums.All(e => e.Count > 0), nameof(extremums));

        var minSpan = double.MaxValue; // 记录最小跨度
        var bestResults = new (int Index, double Value)[extremums.Count]; // 最优解

        var currentValues = new double[extremums.Count]; // 当前选择的值
        var currentIndices = new int[extremums.Count]; // 当前选择的索引

        // 开始递归搜索
        Search(0);

        return (bestResults, minSpan);

        // 递归搜索函数（局部函数）
        void Search(int depth)
        {
            // 递归终止条件：已经为每组都选择了一个点
            if (depth == extremums.Count)
            {
                // 计算当前组合的跨度
                var minValue = currentValues.Min();
                var maxValue = currentValues.Max();
                var span = maxValue - minValue;

                // 如果找到更小的跨度，更新最优解
                if (span < minSpan)
                {
                    minSpan = span;
                    for (var i = 0; i < extremums.Count; i++)
                    {
                        bestResults[i] = (currentIndices[i], currentValues[i]);
                    }
                }

                return;
            }

            // 遍历当前组的所有极值点
            for (var i = 0; i < extremums[depth].Count; i++)
            {
                currentIndices[depth] = i; // 记录索引
                currentValues[depth] = extremums[depth][i]; // 记录值
                Search(depth + 1); // 递归处理下一组
            }
        }
    }

    private static (Vector<double> X, Vector<double> Y) FindExtrema(
        Vector<double> x,
        Vector<double> y,
        ExtremumTypeEnum extremumTypeEnum,
        double threshold,
        bool isContainsEdge)
    {
        var derivativeY = y.Differentiate() / x.Differentiate();

        var indices = derivativeY.FindAbsAbove(threshold);

        var derivativeSign = Vector<double>.Build.SameAs(derivativeY);
        derivativeSign.SetByIndices(indices, derivativeY.GetByIndices(indices).PointwiseSign());

        var derivativeChange = derivativeSign.Differentiate(); // 符号变化量

        var filteredIndices = new List<int>();
        for (var i = 0; i < derivativeChange.Count; i++)
        {
            var change = derivativeChange[i];
            if (change == 0) continue;

            // change < 0: 符号从正变负 (+1 → -1 or 0)，变化量 = -2 or -1, 极大值
            // change > 0: 符号从负变正 (-1 → +1 or 0)，变化量 = +2 or 1, 极小值
            var isMaximumPoint = change < 0;
            var isMinimumPoint = change > 0;

            if ((extremumTypeEnum == ExtremumTypeEnum.Maximum && isMaximumPoint) || // 极大值 且 极大值点
                (extremumTypeEnum == ExtremumTypeEnum.Minimum && isMinimumPoint)) // 极小值 且 极小值点
            {
                filteredIndices.Add(i + 1); // i + 1 是原始数据中的极值点索引
            }
        }

        if (isContainsEdge)
        {
            if ((extremumTypeEnum == ExtremumTypeEnum.Maximum && derivativeSign[0] < 0) || // 极大值 且 第一个点递减
                (extremumTypeEnum == ExtremumTypeEnum.Minimum && derivativeSign[0] > 0)) // 极小值 且 最后一个点递曾
                filteredIndices.Add(0);

            if ((extremumTypeEnum == ExtremumTypeEnum.Maximum && derivativeSign[^1] > 0) || // 极大值 且 最后一个点递增
                (extremumTypeEnum == ExtremumTypeEnum.Minimum && derivativeSign[^1] < 0)) // 极小值 且 最后一个点递减
                filteredIndices.Add(x.Count - 1);
        }


        if (filteredIndices.Count == 0)
            return (Vector<double>.Build.Dense(0), Vector<double>.Build.Dense(0));

        var extremaX = x.GetByIndices([.. filteredIndices]);
        var extremaY = y.GetByIndices([.. filteredIndices]);

        return (extremaX, extremaY);
    }
}