using CommunityToolkit.Diagnostics;
using System.Globalization;
using System.Text;

namespace Net.Utilities.Algorithm.MathNet.Helper;

public static class MatrixHelper
{
    /// <summary>
    /// 空二维数组, <see cref="Array.Empty{T}"/>
    /// </summary>
    /// <typeparam name="T">数组类型</typeparam>
    /// <returns>空二维数组</returns>
    public static T[,] EmptyMatrix<T>() where T : unmanaged => Helper.EmptyMatrix<T>.Value;

    /// <summary>
    /// 获取二维数组的行数和列数
    /// </summary>
    /// <typeparam name="T">数组数据类型</typeparam>
    /// <param name="matrix">二维数组</param>
    /// <returns>(行数, 列数)</returns>
    public static (int RowCount, int ColumnCount) GetRowCountColCount<T>(T[,] matrix) where T : unmanaged
    {
        var rowCount = matrix.GetLength(0);
        var columnCount = matrix.GetLength(1);

        return (rowCount, columnCount);
    }

    /// <summary>
    /// 获取二维数组的一维行数组
    /// </summary>
    /// <typeparam name="T">数组类型</typeparam>
    /// <param name="matrix">二维数组</param>
    /// <param name="rowIndex">行索引, 默认第一行</param>
    /// <returns>一维行数组</returns>
    public static T[] Row<T>(T[,] matrix, int rowIndex) where T : unmanaged
    {
        var (_, columnCount) = GetRowCountColCount(matrix);

        var array = new T[columnCount];

        for (var column = 0; column < columnCount; column++)
            array[column] = matrix[rowIndex, column];

        return array;
    }

    /// <summary>
    /// 获取二维数组的一维列数组
    /// </summary>
    /// <typeparam name="T">数组类型</typeparam>
    /// <param name="matrix">二维数组</param>
    /// <param name="colIndex">列索引, 默认第一列</param>
    /// <returns>一维列数组</returns>
    public static T[] Column<T>(T[,] matrix, int colIndex) where T : unmanaged
    {
        var (rowCount, _) = GetRowCountColCount(matrix);

        var array = new T[rowCount];

        for (var row = 0; row < rowCount; row++)
            array[row] = matrix[row, colIndex];

        return array;
    }

    /// <summary>
    /// 二维数组转换为一维数组
    /// </summary>
    /// <example><pre>
    /// 1, 2, 3
    /// 4, 5, 6  will be returned as 1, 2, 3, 4, 5, 6, 7, 8, 9
    /// 7, 8, 9
    /// </pre></example>
    /// <typeparam name="T">数组数据类型</typeparam>
    /// <param name="matrix">二维数组</param>
    /// <returns>一维数组</returns>
    public static T[] ToArrayByRow<T>(T[,] matrix) where T : unmanaged
    {
        var (rowCount, columnCount) = GetRowCountColCount(matrix);

        var array = new T[rowCount * columnCount];

        var index = 0;
        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                array[index] = matrix[row, column];
                index++;
            }
        }

        return array;
    }

    /// <summary>
    /// 一维数组转换为二维数组
    /// </summary>
    /// <example><pre>
    ///                                                    1, 2, 3
    /// 1, 2, 3, 4, 5, 6, 7, 8, 9 will be returned as(3*3) 4, 5, 6
    ///                                                    7, 8, 9
    /// </pre></example>
    /// <typeparam name="T">数组数据类型</typeparam>
    /// <param name="array">一维数组</param>
    /// <param name="rowCount">行数</param>
    /// <param name="columnCount">列数</param>
    /// <returns>二维数组</returns>
    /// <exception cref="ArgumentException">转换失败</exception>
    public static T[,] ToMatrixByRow<T>(T[] array, int rowCount, int columnCount) where T : unmanaged
    {
        if (array.Length != rowCount * columnCount) ThrowHelper.ThrowArgumentException("Invalid array length");

        var matrix = new T[rowCount, columnCount];

        var index = 0;
        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                matrix[row, column] = array[index];
                index++;
            }
        }

        return matrix;
    }

    /// <summary>
    /// 将二维数组转置
    /// </summary>
    /// <example><pre>
    /// 1, 2, 3                          1, 4, 7
    /// 4, 5, 6 will be returned as(3*3) 2, 5, 8
    /// 7, 8, 9                          3, 6, 9
    /// </pre></example>
    /// <typeparam name="T">数组数据类型</typeparam>
    /// <param name="matrix">二维数组</param>
    /// <returns>转置后的二维数组</returns>
    public static T[,] Transpose<T>(T[,] matrix) where T : unmanaged
    {
        var (rowCount, columnCount) = GetRowCountColCount(matrix);

        var transposedMatrix = new T[columnCount, rowCount];

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                transposedMatrix[column, row] = matrix[row, column];
            }
        }

        return transposedMatrix;
    }

    /// <summary>
    /// 二维数组 水平翻转
    /// </summary>
    /// <typeparam name="T">数组数据类型</typeparam>
    /// <param name="matrix">二维数组</param>
    /// <returns>水平翻转二维数组</returns>
    public static T[,] HorizontalFlip<T>(T[,] matrix) where T : unmanaged
    {
        var (rowCount, columnCount) = GetRowCountColCount(matrix);

        var horizontalFlipMatrix = new T[rowCount, columnCount];

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                horizontalFlipMatrix[row, column] = matrix[row, columnCount - 1 - column];
            }
        }

        return horizontalFlipMatrix;
    }

    /// <summary>
    /// 二维数组 垂直翻转
    /// </summary>
    /// <typeparam name="T">数组数据类型</typeparam>
    /// <param name="matrix">二维数组</param>
    /// <returns>垂直翻转二维数组</returns>
    public static T[,] VerticalFlip<T>(T[,] matrix) where T : unmanaged
    {
        var (rowCount, columnCount) = GetRowCountColCount(matrix);

        var verticalFlipMatrix = new T[rowCount, columnCount];

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                verticalFlipMatrix[row, column] = matrix[rowCount - 1 - row, column];
            }
        }

        return verticalFlipMatrix;
    }

    /// <summary>
    /// 获取二维数组的行数和列数
    /// </summary>
    /// <typeparam name="T">数组数据类型</typeparam>
    /// <param name="matrix">二维数组</param>
    /// <returns>(行数, 列数)</returns>
    public static T[,] Clone<T>(T[,] matrix) where T : unmanaged
    {
        var (rowCount, columnCount) = GetRowCountColCount(matrix);

        var cloneMatrix = new T[rowCount, columnCount];

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                cloneMatrix[row, column] = matrix[row, column];
            }
        }

        return cloneMatrix;
    }

    /// <summary>
    /// 格式化数组的方法
    /// </summary>
    /// <typeparam name="T">数组数据类型</typeparam>
    /// <param name="matrix">二维数组</param>
    /// <param name="format">格式化字符</param>
    /// <returns>字符串</returns>
    public static string FormatMatrix<T>(T[,] matrix, string? format = null) where T : unmanaged, IFormattable
    {
        var (rowCount, columnCount) = GetRowCountColCount(matrix);

        var sb = new StringBuilder();

        // 遍历矩阵并生成包含行和列的字符串
        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                sb.Append(matrix[row, column].ToString(format, CultureInfo.CurrentCulture));
                sb.Append("    ");
            }

            sb.AppendLine();
        }

        // 返回生成的字符串
        return sb.ToString();
    }
}

/// <summary>
/// <see>
///     <cref>EmptyArray{T}</cref>
/// </see>
/// </summary>
internal static class EmptyMatrix<T> where T : unmanaged
{
    public static readonly T[,] Value = new T[0, 0];
}