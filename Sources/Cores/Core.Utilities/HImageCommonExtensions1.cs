using CommunityToolkit.Diagnostics;
using HalconDotNet;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Models.Geometries;

namespace Core.Utilities;

public static class HImageCommonExtensions1
{
    /// <param name="this">图片</param>
    extension(HImage @this)
    {
        /// <summary>
        /// 获取图片统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public (double Average, double Deviation) GetIntensity()
        {
            using var gray = @this.ToGray();
            using var region = gray.GetDomain();

            var mean = @this.Intensity(region, out double deviation);

            return (mean, deviation);
        }

        /// <summary>
        /// 获取图片的直方图
        /// </summary>
        /// <param name="min">直方图的范围最小值</param>
        /// <param name="max">直方图的范围最大值</param>
        /// <returns>直方图</returns>
        public IReadOnlyList<Point> GetHistogram(int min, int max)
        {
            Guard.IsGreaterThanOrEqualTo(min, 0);

            using var gray = @this.ToGray();
            using var region = gray.GetDomain();

            using var minHTuple = new HTuple(min);
            using var maxHTuple = new HTuple(max);

            var length = max - min + 1;

            using var histogramHTuple = @this.GrayHistoRange(region, minHTuple, maxHTuple, length, out _);

            Guard.IsEqualTo(histogramHTuple.Length, length);

            var results = new Point[length];

            foreach (var (index, value) in Enumerable.Range(min, length).Index()) results[index] = new Point(value, histogramHTuple[index]);

            return results;
        }

        /// <summary>
        /// 获取图片的水平方向投影
        /// </summary>
        /// <returns>投影</returns>
        public IReadOnlyList<double> GetHorizontalProjects()
        {
            using var gray = @this.ToGray();
            using var region = gray.GetDomain();

            using var horizontalProjections = @this.GrayProjections(region, "simple", out var verticalProjections);
            using var _ = verticalProjections;

            var results = new double[horizontalProjections.Length];

            for (var i = 0; i < horizontalProjections.Length; i++)
            {
                results[i] = horizontalProjections[i];
            }

            return results;
        }

        /// <summary>
        /// 获取图片的垂直方向投影
        /// </summary>
        /// <returns>投影</returns>
        public IReadOnlyList<double> GetVerticalProjects()
        {
            using var gray = @this.ToGray();
            using var region = gray.GetDomain();

            using var horizontalProjections = @this.GrayProjections(region, "simple", out var verticalProjections);
            using var _ = verticalProjections;

            var results = new double[verticalProjections.Length];

            for (var i = 0; i < verticalProjections.Length; i++)
            {
                results[i] = verticalProjections[i];
            }

            return results;
        }
    }
}