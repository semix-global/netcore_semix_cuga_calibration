using System.Globalization;
using System.Text;

namespace Net.Utilities.Models.Extensions;

public static class JaggedArrayExtensions
{
    /// <summary>
    /// 空二维交错数组。
    /// </summary>
    public static T[][] EmptyMatrix<T>() where T : unmanaged => [];

    /// <summary>
    /// 获取二维交错数组的行数和列数。
    /// </summary>
    public static (int RowCount, int ColumnCount) GetRowCountColCount<T>(this T[][] @this) where T : unmanaged
    {
        var rowCount = @this.Length;
        var columnCount = rowCount == 0 ? 0 : @this[0].Length;

        return (rowCount, columnCount);
    }

    /// <summary>
    /// 获取二维交错数组的一维行数组。
    /// </summary>
    public static T[] Row<T>(this T[][] @this, int rowIndex) where T : unmanaged => [.. @this[rowIndex]];

    /// <summary>
    /// 获取二维交错数组的一维列数组。
    /// </summary>
    public static T[] Column<T>(this T[][] @this, int columnIndex) where T : unmanaged
    {
        var array = new T[@this.Length];

        for (var row = 0; row < @this.Length; row++)
            array[row] = @this[row][columnIndex];

        return array;
    }

    /// <summary>
    /// 二维交错数组转换为一维数组。
    /// </summary>
    public static T[] ToArrayByRow<T>(this T[][] @this) where T : unmanaged
    {
        var (rowCount, columnCount) = @this.GetRowCountColCount();
        var array = new T[rowCount * columnCount];

        var index = 0;
        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                array[index] = @this[row][column];
                index++;
            }
        }

        return array;
    }

    /// <summary>
    /// 将二维交错数组转置。
    /// </summary>
    public static T[][] Transpose<T>(this T[][] @this) where T : unmanaged
    {
        var (rowCount, columnCount) = @this.GetRowCountColCount();
        var transposedMatrix = new T[columnCount][];

        for (var column = 0; column < columnCount; column++)
        {
            transposedMatrix[column] = new T[rowCount];
            for (var row = 0; row < rowCount; row++)
                transposedMatrix[column][row] = @this[row][column];
        }

        return transposedMatrix;
    }

    /// <summary>
    /// 二维交错数组水平翻转。
    /// </summary>
    public static T[][] HorizontalFlip<T>(this T[][] @this) where T : unmanaged
    {
        var (rowCount, columnCount) = @this.GetRowCountColCount();
        var horizontalFlipMatrix = new T[rowCount][];

        for (var row = 0; row < rowCount; row++)
        {
            horizontalFlipMatrix[row] = new T[columnCount];
            for (var column = 0; column < columnCount; column++)
                horizontalFlipMatrix[row][column] = @this[row][columnCount - 1 - column];
        }

        return horizontalFlipMatrix;
    }

    /// <summary>
    /// 二维交错数组垂直翻转。
    /// </summary>
    public static T[][] VerticalFlip<T>(this T[][] @this) where T : unmanaged
    {
        var (rowCount, columnCount) = @this.GetRowCountColCount();
        var verticalFlipMatrix = new T[rowCount][];

        for (var row = 0; row < rowCount; row++)
        {
            verticalFlipMatrix[row] = new T[columnCount];
            for (var column = 0; column < columnCount; column++)
                verticalFlipMatrix[row][column] = @this[rowCount - 1 - row][column];
        }

        return verticalFlipMatrix;
    }

    /// <summary>
    /// 深复制二维交错数组。
    /// </summary>
    public static T[][] Clone<T>(this T[][] @this) where T : unmanaged
    {
        var cloneMatrix = new T[@this.Length][];

        for (var row = 0; row < @this.Length; row++)
            cloneMatrix[row] = [.. @this[row]];

        return cloneMatrix;
    }

    /// <summary>
    /// 格式化二维交错数组。
    /// </summary>
    public static string FormatMatrix<T>(this T[][] @this, string? format, IFormatProvider? formatProvider = null) where T : unmanaged, IFormattable
    {
        format ??= "0.###";
        formatProvider ??= CultureInfo.CurrentCulture;

        var (rowCount, columnCount) = @this.GetRowCountColCount();
        var sb = new StringBuilder();

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                sb.Append(@this[row][column].ToString(format, formatProvider));
                sb.Append('\t');
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}