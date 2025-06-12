using CommunityToolkit.Diagnostics;
using MathNet.Numerics.LinearAlgebra;

namespace Net.Utilities.Extensions;

/// <summary>
/// 向量微积分扩展方法
/// </summary>
public static class VectorCalculusExtensions
{
    /// <summary>
    /// 计算向量的差分
    /// 计算相邻元素的差值: v'[i] = v[i+1] - v[i]
    /// </summary>
    /// <param name="vector">源向量</param>
    /// <returns>差分结果向量, 长度比原向量少1</returns>
    public static Vector<double> Differentiate(this Vector<double> vector)
    {
        if (vector.Count < 2) ThrowHelper.ThrowArgumentException("The vector length must be greater than 1.");

        /*var subVector1 = vector.SubVector(1, vector.Count - 1); // v[1], v[2], ..., v[n-1]
        var subVector0 = vector.SubVector(0, vector.Count - 1); // v[0], v[1], ..., v[n-2]

        return subVector1 - subVector0; // x[i+1] - x[i]*/

        var result = Vector<double>.Build.SameAs(vector, vector.Count - 1);

        var sourceStorage = vector.Storage;
        var resultStorage = result.Storage;

        for (var i = 0; i < result.Count; i++)
        {
            resultStorage.At(i, sourceStorage.At(i + 1) - sourceStorage.At(i));
        }

        return result;
    }

    /// <summary>
    /// 计算向量的累积和
    /// 计算从第一个元素开始的累积和: ∫v[i]dx = v[0] + v[1] + ... + v[i]
    /// </summary>
    /// <param name="vector">源向量</param>
    /// <returns>累积和向量, 与原向量长度相同</returns>
    public static Vector<double> IntegrateCumulative(this Vector<double> vector)
    {
        var result = Vector<double>.Build.SameAs(vector);
        if (vector.Count == 0) return result;

        /*for (var i = 0; i < vector.Count; i++)
        {
            var subVector = vector.SubVector(0, i + 1); // 计算前 i+1 个元素的和 v[0], v[1], ..., v[i]
            result[i] = subVector.Sum(); // 使用现有的 Sum 方法
        }*/

        result[0] = vector[0];

        for (var i = 1; i < vector.Count; i++)
        {
            result[i] = result[i - 1] + vector[i];
        }

        return result;
    }

    /// <summary>
    /// 计算向量的差分
    /// 计算相邻元素的差值: v'[i] = v[i+1] - v[i]
    /// </summary>
    /// <param name="vector">源向量</param>
    /// <returns>差分结果向量, 长度比原向量少1</returns>
    public static Vector<float> Differentiate(this Vector<float> vector)
    {
        if (vector.Count < 2) ThrowHelper.ThrowArgumentException("The vector length must be greater than 1.");

        /*var subVector1 = vector.SubVector(1, vector.Count - 1); // v[1], v[2], ..., v[n-1]
        var subVector0 = vector.SubVector(0, vector.Count - 1); // v[0], v[1], ..., v[n-2]

        return subVector1 - subVector0; // x[i+1] - x[i]*/

        var result = Vector<float>.Build.SameAs(vector, vector.Count - 1);

        var sourceStorage = vector.Storage;
        var resultStorage = result.Storage;

        for (var i = 0; i < result.Count; i++)
        {
            resultStorage.At(i, sourceStorage.At(i + 1) - sourceStorage.At(i));
        }

        return result;
    }

    /// <summary>
    /// 计算向量的累积和
    /// 计算从第一个元素开始的累积和: ∫v[i]dx = v[0] + v[1] + ... + v[i]
    /// </summary>
    /// <param name="vector">源向量</param>
    /// <returns>累积和向量, 与原向量长度相同</returns>
    public static Vector<float> IntegrateCumulative(this Vector<float> vector)
    {
        var result = Vector<float>.Build.SameAs(vector);

        /*for (var i = 0; i < vector.Count; i++)
        {
            var subVector = vector.SubVector(0, i + 1); // 计算前 i+1 个元素的和 v[0], v[1], ..., v[i]
            result[i] = subVector.Sum(); // 使用现有的 Sum 方法
        }*/

        result[0] = vector[0];

        for (var i = 1; i < vector.Count; i++)
        {
            result[i] = result[i - 1] + vector[i];
        }

        return result;
    }
}