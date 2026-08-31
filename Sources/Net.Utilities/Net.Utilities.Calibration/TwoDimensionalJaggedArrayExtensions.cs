using System.Globalization;
using System.Text;

namespace Net.Utilities.Models.Extensions;

public static class TwoDimensionalJaggedArrayExtensions
{
    extension<T>(T[][] @this) where T : unmanaged
    {
        /// <summary>
        /// 获取二维交错数组的行数和列数
        /// </summary>
        public (int YLength, int XLength) GetYXLength()
        {
            var yLength = @this.Length;
            var xLength = @this[0].Length;

            return (yLength, xLength);
        }

        /// <summary>
        /// 二维交错数组转换为一维数组
        /// </summary>
        public T[] ToArrayByRow()
        {
            var (yLength, xLength) = @this.GetYXLength();

            var array = new T[yLength * xLength];

            var index = 0;
            for (var y = 0; y < yLength; y++)
            {
                for (var x = 0; x < xLength; x++)
                {
                    array[index] = @this[y][x];

                    index++;
                }
            }

            return array;
        }

        /// <summary>
        /// 将二维交错数组转置
        /// </summary>
        public T[][] Transpose()
        {
            var (yLength, xLength) = @this.GetYXLength();

            var transposedMatrix = new T[xLength][];

            for (var x = 0; x < xLength; x++)
            {
                transposedMatrix[x] = new T[yLength];

                for (var y = 0; y < yLength; y++)
                {
                    transposedMatrix[x][y] = @this[y][x];
                }
            }

            return transposedMatrix;
        }

        /// <summary>
        /// 二维交错数组水平翻转
        /// </summary>
        public T[][] HorizontalFlip()
        {
            var (yLength, xLength) = @this.GetYXLength();

            var horizontalFlipMatrix = new T[yLength][];

            for (var y = 0; y < yLength; y++)
            {
                horizontalFlipMatrix[y] = new T[xLength];

                for (var x = 0; x < xLength; x++)
                {
                    horizontalFlipMatrix[y][x] = @this[y][xLength - 1 - x];
                }
            }

            return horizontalFlipMatrix;
        }

        /// <summary>
        /// 二维交错数组垂直翻转
        /// </summary>
        public T[][] VerticalFlip()
        {
            var (yLength, xLength) = @this.GetYXLength();

            var verticalFlipMatrix = new T[yLength][];

            for (var y = 0; y < yLength; y++)
            {
                verticalFlipMatrix[y] = new T[xLength];

                for (var x = 0; x < xLength; x++)
                {
                    verticalFlipMatrix[y][x] = @this[yLength - 1 - y][x];
                }
            }

            return verticalFlipMatrix;
        }
    }

    /// <summary>
    /// 格式化二维交错数组
    /// </summary>
    public static string FormatMatrix<T>(this T[][] @this, string? format, IFormatProvider? formatProvider = null) where T : unmanaged, IFormattable
    {
        format ??= "0.###";
        formatProvider ??= CultureInfo.CurrentCulture;

        var (yLength, xLength) = @this.GetYXLength();
        var sb = new StringBuilder();

        for (var y = 0; y < yLength; y++)
        {
            for (var x = 0; x < xLength; x++)
            {
                sb.Append(@this[y][x].ToString(format, formatProvider));
                sb.Append('\t');
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}