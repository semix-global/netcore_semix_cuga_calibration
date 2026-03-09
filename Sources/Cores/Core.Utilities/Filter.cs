using MathNet.Numerics.Statistics;
using Net.Utilities.Models.Geometries;

namespace Core.Utilities;

public static class Filter
{
    public static (int[] Indexes, double[] Result) MAD(IReadOnlyList<double> filters, double k = 3d)
    {
        var median = filters.Median();
        var mad = filters.Select(x => Math.Abs(x - median)).Median();

        // 标准正态分布 N(0,1) P(Z ≤ 0.6745) = 0.75, mad = 0.6745 × σ
        var sigma = mad / 0.6745;

        var lowerBound = median - k * sigma;
        var upperBound = median + k * sigma;

        var tuples = filters.Index().Where(t => lowerBound <= t.Item && t.Item <= upperBound).ToArray();

        return ([.. tuples.Select(t => t.Index)], [.. tuples.Select(t => t.Item)]);
    }

    public static (int[] Indexes, double[] Result) IQR(IReadOnlyList<double> filters, double multiplier = 1.5)
    {
        var q1 = filters.Quantile(0.25);
        var q3 = filters.Quantile(0.75);
        var iqr = q3 - q1;

        var lowerBound = q1 - multiplier * iqr;
        var upperBound = q3 + multiplier * iqr;

        var tuples = filters.Index().Where(t => lowerBound <= t.Item && t.Item <= upperBound).ToArray();

        return ([.. tuples.Select(t => t.Index)], [.. tuples.Select(t => t.Item)]);
    }

    public static (int[] Indexes, double[] Result) ZScore(IReadOnlyList<double> filters, double k = 3d)
    {
        var mean = filters.Mean();
        var standardDeviation = filters.StandardDeviation();

        var lowerBound = mean - k * standardDeviation;
        var upperBound = mean + k * standardDeviation;

        var tuples = filters.Index().Where(t => lowerBound <= t.Item && t.Item <= upperBound).ToArray();

        return ([.. tuples.Select(t => t.Index)], [.. tuples.Select(t => t.Item)]);
    }

    public static (int[] Indexes, Point[] Result) NMS(IReadOnlyList<Point> filters, double distanceThreshold)
    {
        var tupleList = new List<(int Index, Point Item)>();

        foreach (var (index, point) in filters.Index())
        {
            var existingIndex = tupleList.FindIndex(p => Math.Abs(point.X - p.Item.X) <= distanceThreshold);

            if (existingIndex != -1)
            {
                if (point.Y > tupleList[existingIndex].Item.Y)
                {
                    tupleList[existingIndex] = (index, point);
                }
            }
            else
            {
                tupleList.Add((index, point));
            }
        }

        tupleList = tupleList.OrderBy(t => t.Item.X).ToList();

        return ([.. tupleList.Select(t => t.Index)], [.. tupleList.Select(t => t.Item)]);
    }
}