using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;

namespace Core.Utilities;

public static class Extremumor
{
    public static (Vector<double> X, Vector<double> Y) FindLocalMaxima(
        Vector<double> x,
        Vector<double> y,
        double threshold = 0,
        bool isContainsEdge = false) => FindLocalExtrema(x, y, ExtremumTypeEnum.Maximum, threshold, isContainsEdge);

    private enum ExtremumTypeEnum
    {
        Maximum,
        Minimum
    }

    public static (Vector<double> X, Vector<double> Y) FindLocalMinima(
        Vector<double> x,
        Vector<double> y,
        double threshold = 0,
        bool isContainsEdge = false) => FindLocalExtrema(x, y, ExtremumTypeEnum.Minimum, threshold, isContainsEdge);

    public static (double x1, double y1, double x2, double y2, double x3, double y3, double span) FindClosestTriplet(
        Vector<double> extremum1X,
        Vector<double> extremum1Y,
        Vector<double> extremum2X,
        Vector<double> extremum2Y,
        Vector<double> extremum3X,
        Vector<double> extremum3Y)
    {
        var minSpan = double.MaxValue;
        var bestX1 = 0.0;
        var bestY1 = 0.0;
        var bestX2 = 0.0;
        var bestY2 = 0.0;
        var bestX3 = 0.0;
        var bestY3 = 0.0;

        for (var i = 0; i < extremum1X.Count; i++)
        {
            for (var j = 0; j < extremum2X.Count; j++)
            {
                for (var k = 0; k < extremum3X.Count; k++)
                {
                    var x1 = extremum1X[i];
                    var x2 = extremum2X[j];
                    var x3 = extremum3X[k];

                    // 计算三个 x 坐标的跨度（最大值 - 最小值）
                    var minX = Math.Min(Math.Min(x1, x2), x3);
                    var maxX = Math.Max(Math.Max(x1, x2), x3);
                    var span = maxX - minX;

                    // 更新最小跨度的三点组合
                    if (span < minSpan)
                    {
                        minSpan = span;
                        bestX1 = x1;
                        bestY1 = extremum1Y[i];
                        bestX2 = x2;
                        bestY2 = extremum2Y[j];
                        bestX3 = x3;
                        bestY3 = extremum3Y[k];
                    }
                }
            }
        }

        return (bestX1, bestY1, bestX2, bestY2, bestX3, bestY3, minSpan);
    }

    private static (Vector<double> X, Vector<double> Y) FindLocalExtrema(
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

        var extremaX = x.GetByIndices([..filteredIndices]);
        var extremaY = y.GetByIndices([..filteredIndices]);

        return (extremaX, extremaY);
    }
}