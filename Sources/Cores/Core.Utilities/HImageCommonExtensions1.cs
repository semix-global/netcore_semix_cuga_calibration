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

        /// <summary>
        /// 将 RAW Log图像转换为线性化图像. 去1500底噪，转int后通过反向指数变换将灰度值映射到 12bit 范围
        /// </summary>
        /// <returns>线性化后的HImage</returns>
        public HImage RAW12BitsPerPixelLogToLinear()
        {
            Guard.IsEqualTo(@this.GetBitsPerPixel(), 16);

            var (width, height) = (SizeI)@this.GetSize();

            using var region = @this.GetDomain();
            region.GetRegionPoints(out var rowsHTuple, out var columnsHTuple);

            using var _01 = rowsHTuple;
            using var _11 = columnsHTuple;

            using var grayValHTuple = @this.GetGrayval(rowsHTuple, columnsHTuple);

            // 2 ^ ((gray - 1500) / 128) -> [0, 4095]
            using var subHTuple = grayValHTuple - 1500d;
            using var divHTuple = subHTuple / 128d;
            using var exp2HTuple = divHTuple.TupleExp2();
            using var exp2MaxHTuple = exp2HTuple.TupleMax();
            using var scaleHTuple = exp2MaxHTuple / 4095d;
            using var exp2DivHTuple = exp2HTuple / scaleHTuple;
            using var intTuple = exp2DivHTuple.TupleInt();

            var result = new HImage("uint2", width, height);
            result.SetGrayval(rowsHTuple, columnsHTuple, intTuple);

            return result;
        }

        /// <summary>
        /// 获取 16位HImage 指定行的灰度值数组
        /// </summary>
        /// <param name="row">行索引（从 0 开始）</param>
        /// <returns>该行所有像素的灰度值 ushort[width]</returns>
        public unsafe ushort[] RAW16BitsPerPixelGetRow(int row)
        {
            Guard.IsEqualTo(@this.GetBitsPerPixel(), 16);

            var (width, height) = (SizeI)@this.GetSize();
            Guard.IsInRange(row, 0, height - 1);

            var totalBytes = width * sizeof(ushort);

            var result = new ushort[width];
            fixed (ushort* dst = result) Buffer.MemoryCopy((ushort*)@this.GetImagePointer().ToPointer() + row * width, dst, totalBytes, totalBytes);

            return result;
        }

        /// <summary>
        /// 获取 16位HImage 指定列的灰度值数组
        /// </summary>
        /// <param name="col">列索引（从 0 开始）</param>
        /// <returns>该列所有像素的灰度值 ushort[height]</returns>
        public unsafe ushort[] RAW16BitsPerPixelGetColumn(int col)
        {
            Guard.IsEqualTo(@this.GetBitsPerPixel(), 16);

            var (width, height) = (SizeI)@this.GetSize();

            Guard.IsInRange(col, 0, width - 1);

            var src = (ushort*)@this.GetImagePointer() + col;

            var result = new ushort[height];

            for (var row = 0; row < height; row++)
            {
                result[row] = *src;
                src += width;
            }

            return result;
        }

        /// <summary>
        /// 将 16位HImage 转换为 ushort[,] 矩阵
        /// </summary>
        /// <returns>灰度值矩阵 ushort[height, width]</returns>
        public unsafe ushort[,] RAW16BitsPerPixelToMatrix()
        {
            Guard.IsEqualTo(@this.GetBitsPerPixel(), 16);

            var (width, height) = (SizeI)@this.GetSize();

            var totalBytes = width * height * sizeof(ushort);

            var matrix = new ushort[height, width];
            fixed (ushort* dst = matrix) Buffer.MemoryCopy((ushort*)@this.GetImagePointer().ToPointer(), dst, totalBytes, totalBytes);

            return matrix;
        }
    }
}