using CommunityToolkit.Diagnostics;
using MathNet.Numerics;

namespace Core.Utilities;

public static class GenerateExtensions
{
    extension(Generate)
    {
        /// <summary>
        /// 生成线性V形窗口
        /// </summary>
        /// <param name="coefficient">基础系数</param>
        /// <param name="vCoefficient">V形底部系数</param>
        /// <param name="vMiddleIndex">V形中心位置索引</param>
        /// <param name="vHalfWidth">V形半宽度</param>
        /// <param name="totalLength">窗口总长度</param>
        /// <returns>窗口数组和V形区域信息（起始索引、中心索引、结束索引）</returns>
        public static (double[] Window, (int VStartIndex, int VMiddleIndex, int VStopIndex) Region) LinearVShapeWindow(
            double coefficient,
            double vCoefficient,
            int vMiddleIndex,
            int vHalfWidth,
            int totalLength)
        {
            Guard.IsGreaterThan(totalLength, 0);
            Guard.IsGreaterThanOrEqualTo(vMiddleIndex, 0);
            Guard.IsLessThan(vMiddleIndex, totalLength);
            Guard.IsGreaterThan(vHalfWidth, 0);

            var window = Enumerable.Repeat(coefficient, totalLength).ToArray();

            var vStartIndex = Math.Max(0, vMiddleIndex - vHalfWidth);
            var vStopIndex = Math.Min(totalLength - 1, vMiddleIndex + vHalfWidth);

            var k = (coefficient - vCoefficient) / vHalfWidth;

            for (var i = vMiddleIndex - 1; i >= vStartIndex; i--)
            {
                var rate = vCoefficient + (vMiddleIndex - i) * k;
                window[i] = rate;
            }

            window[vMiddleIndex] = vCoefficient;

            for (var i = vMiddleIndex + 1; i <= vStopIndex; i++)
            {
                var rate = vCoefficient + (i - vMiddleIndex) * k;
                window[i] = rate;
            }

            return (window, (vStartIndex, vMiddleIndex, vStopIndex));
        }

        /// <summary>
        /// 根据段数生成线性V形窗口
        /// </summary>
        /// <param name="coefficient">基础系数</param>
        /// <param name="vCoefficient">V形底部系数</param>
        /// <param name="segmentCount">总段数</param>
        /// <param name="vShapeSegmentIndex">V形所在段的索引（从0开始）</param>
        /// <param name="totalLength">窗口总长度</param>
        /// <returns>窗口数组和V形区域信息（起始索引、中心索引、结束索引）</returns>
        public static (double[] Window, (int VStartIndex, int VMiddleIndex, int VStopIndex) Region) LinearVShapeWindowBySegments(
            double coefficient,
            double vCoefficient,
            int segmentCount,
            int vShapeSegmentIndex,
            int totalLength)
        {
            Guard.IsGreaterThan(segmentCount, 0);
            Guard.IsLessThan(vShapeSegmentIndex, segmentCount);
            Guard.IsGreaterThanOrEqualTo(vShapeSegmentIndex, 0);
            Guard.IsGreaterThan(totalLength, 0);

            var segmentLength = totalLength / segmentCount;
            var vHalfWidth = segmentLength / 2;
            var vMiddleIndex = vShapeSegmentIndex * segmentLength + vHalfWidth;

            return Generate.LinearVShapeWindow(
                coefficient,
                vCoefficient,
                vMiddleIndex,
                vHalfWidth,
                totalLength);
        }

        /// <summary>
        /// 生成包含多个线性V形的窗口
        /// </summary>
        /// <param name="coefficient">基础系数</param>
        /// <param name="vCoefficient">V形底部系数</param>
        /// <param name="vMiddleIndexes">多个V形中心位置索引数组</param>
        /// <param name="vHalfWidth">V形半宽度</param>
        /// <param name="totalLength">窗口总长度</param>
        /// <returns>窗口数组和多个V形区域信息数组（每个包含起始索引、中心索引、结束索引）</returns>
        public static (double[] Window, (int VStartIndex, int VMiddleIndex, int VStopIndex)[] Regions) LinearVShapeWindow(
            double coefficient,
            double vCoefficient,
            int[] vMiddleIndexes,
            int vHalfWidth,
            int totalLength)
        {
            Guard.IsGreaterThan(vHalfWidth, 0);
            Guard.IsGreaterThan(totalLength, 0);

            var window = Enumerable.Repeat(coefficient, totalLength).ToArray();
            var vShapeInfos = new (int VStartIndex, int VMiddleIndex, int VStopIndex)[vMiddleIndexes.Length];

            var k = (coefficient - vCoefficient) / vHalfWidth;

            for (var j = 0; j < vMiddleIndexes.Length; j++)
            {
                var vMiddleIndex = vMiddleIndexes[j];

                Guard.IsGreaterThanOrEqualTo(vMiddleIndex, 0);
                Guard.IsLessThan(vMiddleIndex, totalLength);

                var vStartIndex = Math.Max(0, vMiddleIndex - vHalfWidth);
                var vStopIndex = Math.Min(totalLength - 1, vMiddleIndex + vHalfWidth);

                for (var i = vMiddleIndex - 1; i >= vStartIndex; i--)
                {
                    var rate = vCoefficient + (vMiddleIndex - i) * k;
                    window[i] = rate;
                }

                window[vMiddleIndex] = vCoefficient;

                for (var i = vMiddleIndex + 1; i <= vStopIndex; i++)
                {
                    var rate = vCoefficient + (i - vMiddleIndex) * k;
                    window[i] = rate;
                }

                vShapeInfos[j] = (vStartIndex, vMiddleIndex, vStopIndex);
            }

            return (window, vShapeInfos);
        }

        /// <summary>
        /// 根据多个段索引生成包含多个线性V形的窗口
        /// </summary>
        /// <param name="coefficient">基础系数</param>
        /// <param name="vCoefficient">V形底部系数</param>
        /// <param name="segmentCount">总段数</param>
        /// <param name="vShapeSegmentIndexes">多个V形所在段的索引数组（从0开始）</param>
        /// <param name="totalLength">窗口总长度</param>
        /// <returns>窗口数组和多个V形区域信息数组（每个包含起始索引、中心索引、结束索引）</returns>
        public static (double[] Window, (int VStartIndex, int VMiddleIndex, int VStopIndex)[] Regions) LinearVShapeWindowBySegments(
            double coefficient,
            double vCoefficient,
            int segmentCount,
            int[] vShapeSegmentIndexes,
            int totalLength)
        {
            Guard.IsGreaterThan(segmentCount, 0);
            Guard.IsGreaterThan(totalLength, 0);

            var segmentLength = totalLength / segmentCount;
            var vHalfWidth = segmentLength / 2;

            var vMiddleIndexes = vShapeSegmentIndexes.Select(vShapeSegmentIndex =>
            {
                Guard.IsLessThan(vShapeSegmentIndex, segmentCount);
                Guard.IsGreaterThanOrEqualTo(vShapeSegmentIndex, 0);

                return vShapeSegmentIndex * segmentLength + vHalfWidth;
            }).ToArray();

            return Generate.LinearVShapeWindow(
                coefficient,
                vCoefficient,
                vMiddleIndexes,
                vHalfWidth,
                totalLength);
        }
    }
}