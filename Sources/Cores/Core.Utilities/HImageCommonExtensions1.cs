using CommunityToolkit.Diagnostics;
using HalconDotNet;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Models.Geometries;
using System;
using System.Runtime.InteropServices;

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

        public short[,] GetMatrix()
        {
            using var gray = @this.ToGray();

            var pointer = gray.GetImagePointer1(out string type, out int width, out int height);
            if (width <= 0 || height <= 0) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(@this));

            var length = checked(width * height);

            short[] pixels = type switch
            {
                "int2" => CopyInt2(pointer, length),
                "uint2" => CopyUInt2(pointer, length),
                "byte" => CopyByte(pointer, length),
                _ => throw new NotSupportedException($"Unsupported image type: {type}")
            };

            var matrix = new short[height, width];
            var index = 0;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    matrix[y, x] = pixels[index++];
                }
            }

            return matrix;

            static short[] CopyInt2(IntPtr pointer, int length)
            {
                var pixels = new short[length];
                Marshal.Copy(pointer, pixels, 0, length);
                return pixels;
            }

            static short[] CopyUInt2(IntPtr pointer, int length)
            {
                var bytes = new byte[checked(length * 2)];
                Marshal.Copy(pointer, bytes, 0, bytes.Length);
                var unsigned = new ushort[length];
                Buffer.BlockCopy(bytes, 0, unsigned, 0, bytes.Length);
                var pixels = new short[length];
                for (var i = 0; i < length; i++) pixels[i] = unchecked((short)unsigned[i]);
                return pixels;
            }

            static short[] CopyByte(IntPtr pointer, int length)
            {
                var bytes = new byte[length];
                Marshal.Copy(pointer, bytes, 0, length);
                var pixels = new short[length];
                for (var i = 0; i < length; i++) pixels[i] = bytes[i];
                return pixels;
            }
        }
    }
}
