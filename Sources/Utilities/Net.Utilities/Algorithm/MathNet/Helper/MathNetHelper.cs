using MathNet.Numerics.LinearAlgebra;

namespace Net.Utilities.Algorithm.MathNet.Helper;

public static class MathNetHelper
{
    /// <summary>
    /// 将一维数组转换为 double Vector
    /// Vector只支持double, float, Complex, Complex32
    /// </summary>
    /// <typeparam name="T">数组数据类型</typeparam>
    /// <param name="array">一维数组</param>
    /// <returns>double Vector</returns>
    public static Vector<double> ConvertToDoubleVector<T>(T[] array) where T : unmanaged, IConvertible
    {
        var doubleVector = Vector<double>.Build.Dense(array.Length);

        for (var i = 0; i < array.Length; i++)
        {
            doubleVector[i] = array[i].ToDouble(null);
        }

        return doubleVector;
    }

    /// <summary>
    /// 将二维数组转换为 double Matrix
    /// Matrix只支持double, float, Complex, Complex32
    /// </summary>
    /// <param name="matrix">二维数组</param>
    /// <returns>double Matrix</returns>
    public static Matrix<double> ConvertToDoubleMatrix<T>(T[,] matrix) where T : unmanaged, IConvertible
    {
        var rowCount = matrix.GetLength(0);
        var columnCount = matrix.GetLength(1);

        var doubleMatrix = Matrix<double>.Build.Dense(rowCount, columnCount);

        for (var i = 0; i < rowCount; i++)
        {
            for (var j = 0; j < columnCount; j++)
            {
                doubleMatrix[i, j] = matrix[i, j].ToDouble(null);
            }
        }

        return doubleMatrix;
    }
}