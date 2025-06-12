using MathNet.Numerics.LinearAlgebra;

namespace Net.Utilities.Extensions;

public static class VectorExtensions
{
    /// <summary>
    /// 创建包含指定范围元素的子向量（使用结束索引）
    /// </summary>
    /// <typeparam name="T">向量元素类型</typeparam>
    /// <param name="vector">源向量</param>
    /// <param name="startIndex">起始索引（包含）</param>
    /// <param name="endIndex">结束索引（包含）</param>
    /// <returns>包含指定范围元素的新向量</returns>
    public static Vector<T> SubVectorRange<T>(this Vector<T> vector, int startIndex, int endIndex)
        where T : struct, IEquatable<T>, IFormattable
    {
        return vector.SubVector(startIndex, endIndex - startIndex + 1);
    }

    /// <summary>
    /// 将给定向量的值复制到当前向量的指定范围（使用结束索引）
    /// </summary>
    /// <typeparam name="T">向量元素类型</typeparam>
    /// <param name="vector">目标向量</param>
    /// <param name="startIndex">起始索引（包含）</param>
    /// <param name="endIndex">结束索引（包含）</param>
    /// <param name="subVector">要复制的源向量</param>
    public static void SetSubVectorRange<T>(this Vector<T> vector, int startIndex, int endIndex, Vector<T> subVector)
        where T : struct, IEquatable<T>, IFormattable
    {
        vector.SetSubVector(startIndex, endIndex - startIndex + 1, subVector);
    }
}