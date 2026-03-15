using CommunityToolkit.Diagnostics;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;

namespace Core.Utilities;

public static class GenerateExtensions
{
    extension(Generate)
    {
        public static (double[] Window, (int VStartIndex, int VMiddleIndex, int VStopIndex) Region) LinearVShapeWindowByIndex(
            double coefficient,
            double vCoefficient,
            int vMiddleIndex,
            int vHalfWidth,
            int totalLength)
        {
            Guard.IsGreaterThan(totalLength, 0);
            Guard.IsGreaterThanOrEqualTo(vMiddleIndex, 0);
            Guard.IsLessThanOrEqualTo(vMiddleIndex, totalLength - 1);
            Guard.IsGreaterThan(vHalfWidth, 0);

            var window = Generate.Repeat(totalLength, coefficient);

            var vStartIndex = Math.Max(0, vMiddleIndex - vHalfWidth);
            var vStopIndex = Math.Min(totalLength - 1, vMiddleIndex + vHalfWidth);

            var k = (coefficient - vCoefficient) / vHalfWidth;

            for (var i = vMiddleIndex - 1; i >= vStartIndex; i--) window[i] = vCoefficient + (vMiddleIndex - i) * k;

            window[vMiddleIndex] = vCoefficient;

            for (var i = vMiddleIndex + 1; i <= vStopIndex; i++) window[i] = vCoefficient + (i - vMiddleIndex) * k;

            return (window, (vStartIndex, vMiddleIndex, vStopIndex));
        }

        public static (double[] Window, (int VStartIndex, int VMiddleIndex, int VStopIndex)[] Regions) LinearVShapeWindowByIndex(
            double coefficient,
            double vCoefficient,
            IReadOnlyList<int> vMiddleIndexes,
            int vHalfWidth,
            int totalLength)
        {
            Guard.IsGreaterThan(vHalfWidth, 0);
            Guard.IsGreaterThan(totalLength, 0);

            var window = Generate.Repeat(totalLength, coefficient);
            var regions = new (int VStartIndex, int VMiddleIndex, int VStopIndex)[vMiddleIndexes.Count];

            for (var i = 0; i < vMiddleIndexes.Count; i++)
            {
                var (tempWindow, region) = Generate.LinearVShapeWindowByIndex(
                    coefficient,
                    vCoefficient,
                    vMiddleIndexes[i],
                    vHalfWidth,
                    totalLength);

                Vector<double>.Build.Dense(window).SetSubVectorRange(
                    region.VStartIndex,
                    region.VStopIndex,
                    Vector<double>.Build.Dense([..tempWindow.AsSpan()[region.VStartIndex..(region.VStopIndex + 1)]]));

                regions[i] = region;
            }

            return (window, regions);
        }

        public static (double[] Window, (int VStartIndex, int VMiddleIndex, int VStopIndex) Region) LinearVShapeWindowBySegments(
            double coefficient,
            double vCoefficient,
            int segmentCount,
            int vShapeSegmentIndex,
            int totalLength)
        {
            Guard.IsGreaterThan(segmentCount, 0);
            Guard.IsGreaterThanOrEqualTo(vShapeSegmentIndex, 0);
            Guard.IsLessThanOrEqualTo(vShapeSegmentIndex, segmentCount - 1);
            Guard.IsGreaterThan(totalLength, 0);

            var segmentLength = totalLength / segmentCount;
            var vHalfWidth = segmentLength / 2;
            var vMiddleIndex = vShapeSegmentIndex * segmentLength + vHalfWidth;

            return Generate.LinearVShapeWindowByIndex(
                coefficient,
                vCoefficient,
                vMiddleIndex,
                vHalfWidth,
                totalLength);
        }

        public static (double[] Window, (int VStartIndex, int VMiddleIndex, int VStopIndex)[] Regions) LinearVShapeWindowBySegments(
            double coefficient,
            double vCoefficient,
            int segmentCount,
            IReadOnlyList<int> vShapeSegmentIndexes,
            int totalLength)
        {
            Guard.IsGreaterThan(segmentCount, 0);
            Guard.IsGreaterThan(totalLength, 0);

            var window = Generate.Repeat(totalLength, coefficient);
            var regions = new (int VStartIndex, int VMiddleIndex, int VStopIndex)[vShapeSegmentIndexes.Count];

            for (var i = 0; i < vShapeSegmentIndexes.Count; i++)
            {
                var (tempWindow, region) = Generate.LinearVShapeWindowBySegments(
                    coefficient,
                    vCoefficient,
                    segmentCount,
                    vShapeSegmentIndexes[i],
                    totalLength);

                Vector<double>.Build.Dense(window).SetSubVectorRange(
                    region.VStartIndex,
                    region.VStopIndex,
                    Vector<double>.Build.Dense([..tempWindow.AsSpan()[region.VStartIndex..(region.VStopIndex + 1)]]));

                regions[i] = region;
            }

            return (window, regions);
        }
    }
}