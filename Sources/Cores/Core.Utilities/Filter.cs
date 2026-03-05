using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using Net.Utilities.Models.Geometries;

namespace Core.Utilities;

public static class Filter
{
    public static Vector<double> MAD(Vector<double> vector, double k = 3d)
    {
        var median = vector.Median();
        var mad = vector.Map(x => Math.Abs(x - median)).Median();

        // 标准正态分布 N(0,1) P(Z ≤ 0.6745) = 0.75, mad = 0.6745 × σ
        var sigma = mad / 0.6745;

        var lowerBound = median - k * sigma;
        var upperBound = median + k * sigma;

        return Vector<double>.Build.Dense([..vector.Where(x => lowerBound <= x && x <= upperBound)]);
    }

    public static Vector<double> IQR(Vector<double> vector, double multiplier = 1.5)
    {
        var q1 = vector.Quantile(0.25);
        var q3 = vector.Quantile(0.75);
        var iqr = q3 - q1;

        var lowerBound = q1 - multiplier * iqr;
        var upperBound = q3 + multiplier * iqr;

        return Vector<double>.Build.Dense([..vector.Where(x => lowerBound <= x && x <= upperBound)]);
    }

    public static Vector<double> ZScore(Vector<double> vector, double k = 3d)
    {
        var mean = vector.Mean();
        var standardDeviation = vector.StandardDeviation();

        var lowerBound = mean - k * standardDeviation;
        var upperBound = mean + k * standardDeviation;

        return Vector<double>.Build.Dense([..vector.Where(x => lowerBound <= x && x <= upperBound)]);
    }

    public static Point[] NMS(IReadOnlyList<Point> matches, double distanceThreshold)
    {
        var finalBestMatches = new List<Point>();

        // 局部非极大值抑制 (NMS)
        foreach (var candidate in matches)
        {
            var isSuppressed = finalBestMatches.Any(bestMatch => Math.Abs(candidate.X - bestMatch.X) <= distanceThreshold);

            if (isSuppressed == false) finalBestMatches.Add(candidate);
        }

        return [..finalBestMatches.OrderBy(m => m.X)];
    }
}