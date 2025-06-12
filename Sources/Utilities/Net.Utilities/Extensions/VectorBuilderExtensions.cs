using MathNet.Numerics.LinearAlgebra;

namespace Net.Utilities.Extensions;

public static class VectorBuilderExtensions
{
    /// <summary>
    /// 从任意数值类型数组创建指定类型的密集向量
    /// </summary>
    /// <param name="builder">向量构建器</param>
    /// <param name="array">源数组</param>
    /// <returns>指定类型的密集向量</returns>
    public static Vector<double> DenseOfArray(this VectorBuilder<double> builder, int[] array)
    {
        var targetArray = new double[array.Length];
        for (var i = 0; i < array.Length; i++)
        {
            targetArray[i] = array[i];
        }

        return builder.Dense(targetArray);
    }
}