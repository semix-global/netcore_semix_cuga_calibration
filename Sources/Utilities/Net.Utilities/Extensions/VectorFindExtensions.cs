using CommunityToolkit.Diagnostics;
using MathNet.Numerics.LinearAlgebra;

namespace Net.Utilities.Extensions;

/// <summary>
/// 向量查找扩展方法
/// </summary>
public static class VectorFindExtensions
{
    /// <summary>
    /// 从向量末尾开始查找第一个满足条件的元素，返回索引和值的元组，如果没找到返回null
    /// （类似 MATLAB 的 find(condition, 1, 'last')）
    /// </summary>
    /// <typeparam name="T">向量元素类型</typeparam>
    /// <param name="vector">源向量</param>
    /// <param name="predicate">判断条件</param>
    /// <param name="zeros">是否包含零值</param>
    /// <returns>满足条件的最后一个元素的索引和值，如果没找到返回null</returns>
    public static Tuple<int, T>? FindLast<T>(this Vector<T> vector, Func<T, bool> predicate, Zeros zeros = Zeros.AllowSkip)
        where T : struct, IEquatable<T>, IFormattable
    {
        foreach (var (index, value) in vector.EnumerateIndexed(zeros).Reverse())
        {
            if (predicate(value))
            {
                return new Tuple<int, T>(index, value);
            }
        }

        return null;
    }

    /// <summary>
    /// 查找满足条件的所有元素索引
    /// </summary>
    /// <typeparam name="T">向量元素类型</typeparam>
    /// <param name="vector">源向量</param>
    /// <param name="predicate">判断条件</param>
    /// <param name="zeros">在稀疏数据结构中, 零元素可以被跳过(默认)</param>
    /// <returns>满足条件的所有索引数组</returns>
    public static int[] FindAll<T>(this Vector<T> vector, Func<T, bool> predicate, Zeros zeros = Zeros.AllowSkip)
        where T : struct, IEquatable<T>, IFormattable
    {
        var indices = new List<int>();

        foreach (var (index, value) in vector.EnumerateIndexed(zeros))
        {
            if (predicate(value))
            {
                indices.Add(index);
            }
        }

        return [.. indices];
    }

    /// <summary>
    /// 查找绝对值大于等于阈值的所有元素索引（实数版本）
    /// </summary>
    /// <typeparam name="T">实数类型，必须实现 IComparable</typeparam>
    /// <param name="vector">源向量</param>
    /// <param name="threshold">阈值</param>
    /// <param name="zeros">在稀疏数据结构中, 零元素可以被跳过(默认)</param>
    /// <returns>满足条件的所有索引数组</returns>
    public static int[] FindAbsAbove<T>(this Vector<T> vector, T threshold, Zeros zeros = Zeros.AllowSkip)
        where T : struct, IEquatable<T>, IFormattable
    {
        var indices = new List<int>();

        foreach (var (index, absValue) in vector.PointwiseAbs().EnumerateIndexed(zeros))
        {
            if (Comparer<T>.Default.Compare(absValue, threshold) >= 0)
            {
                indices.Add(index);
            }
        }

        return [.. indices];
    }

    /// <summary>
    /// 根据索引数组获取向量的子区间
    /// </summary>
    /// <param name="vector">源向量</param>
    /// <param name="indices">索引数组</param>
    /// <returns>对应索引位置的值组成的新向量</returns>
    public static Vector<T> GetByIndices<T>(this Vector<T> vector, int[] indices)
        where T : struct, IEquatable<T>, IFormattable
    {
        var result = Vector<T>.Build.Dense(indices.Length);

        for (var i = 0; i < indices.Length; i++)
        {
            result[i] = vector[indices[i]];
        }

        return result;
    }

    /// <summary>
    /// 根据索引数组设置向量的子区间值
    /// </summary>
    /// <param name="vector">目标向量</param>
    /// <param name="indices">索引数组</param>
    /// <param name="values">要设置的值向量</param>
    public static void SetByIndices<T>(this Vector<T> vector, int[] indices, Vector<T> values)
        where T : struct, IEquatable<T>, IFormattable
    {
        if (indices.Length != values.Count) ThrowHelper.ThrowArgumentException("The length of indices must be equal to the length of values.");

        for (var i = 0; i < indices.Length; i++)
        {
            vector[indices[i]] = values[i];
        }
    }
}