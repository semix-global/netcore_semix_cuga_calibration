using CommunityToolkit.Diagnostics;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra;

namespace Core.Utilities;

public static class Interpolator
{
    public static (Vector<double> X, Vector<double> Y) SplineInterpolation(Vector<double> x, Vector<double> y, int interpolationCount, int order = 3)
    {
        Guard.IsEqualTo(x.Count, y.Count, "x and y must have the same length");

        IInterpolation interpolator;

        switch (order)
        {
            case 1:
                interpolator = LinearSpline.InterpolateSorted([.. x], [.. y]);
                break;

            case 3:
                interpolator = CubicSpline.InterpolateNaturalSorted([.. x], [.. y]);
                break;

            default:
                return ThrowHelper.ThrowArgumentOutOfRangeException<(Vector<double> X, Vector<double> Y)>(nameof(order), order, "order must be 1 or 3");
        }

        var interpXList = new List<double>();
        var interpYList = new List<double>();

        for (var i = 0; i < x.Count - 1; i++)
        {
            var x1 = x[i];
            var y1 = y[i];
            var x2 = x[i + 1];

            interpXList.Add(x1);
            interpYList.Add(y1);

            var xs = Generate.LinearSpaced(2 + interpolationCount, x1, x2);

            for (var j = 1; j < xs.Length - 1; j++) // 跳过第一个和最后一个
            {
                var xInterp = xs[j];
                var yInterp = interpolator.Interpolate(xInterp);

                interpXList.Add(xInterp);
                interpYList.Add(yInterp);
            }
        }

        interpXList.Add(x[^1]);
        interpYList.Add(y[^1]);

        return (Vector<double>.Build.DenseOfEnumerable(interpXList), Vector<double>.Build.DenseOfEnumerable(interpYList));
    }
}