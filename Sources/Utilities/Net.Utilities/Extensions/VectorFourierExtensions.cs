using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra;
using Complex = System.Numerics.Complex;

namespace Net.Utilities.Extensions;

/// <summary>
/// 复数向量的傅里叶变换扩展方法
/// </summary>
public static class VectorFourierExtensions
{
    /// <summary>
    /// 执行快速傅里叶变换 (fft)
    /// </summary>
    /// <param name="vector">输入的复数向量</param>
    /// <param name="options">傅里叶变换选项，默认为 Matlab 兼容模式</param>
    /// <returns>傅里叶变换结果</returns>
    public static Vector<Complex> FastFourierTransform(this Vector<Complex> vector, FourierOptions options = FourierOptions.Matlab)
    {
        var complexes = vector.ToArray();
        Fourier.Forward(complexes, options);

        return Vector<Complex>.Build.Dense(complexes);
    }

    /// <summary>
    /// 执行快速傅里叶逆变换 (ifft)
    /// </summary>
    /// <param name="vector">输入的复数向量(频域)</param>
    /// <param name="options">傅里叶变换选项，默认为 Matlab 兼容模式</param>
    /// <returns>傅里叶逆变换结果</returns>
    public static Vector<Complex> InverseFastFourierTransform(this Vector<Complex> vector, FourierOptions options = FourierOptions.Matlab)
    {
        var complexes = vector.ToArray();
        Fourier.Inverse(complexes, options);

        return Vector<Complex>.Build.Dense(complexes);
    }

    /// <summary>
    /// 执行 fft 移位操作，将零频分量移动到数组中心
    /// 对应 MATLAB 的 fftshift 函数
    /// </summary>
    /// <typeparam name="T">向量元素类型</typeparam>
    /// <param name="vector">输入的复数向量</param>
    /// <returns>移位后的向量</returns>
    public static Vector<T> FastFourierTransformShift<T>(this Vector<T> vector)
        where T : struct, IEquatable<T>, IFormattable
    {
        var complexes = vector.ToArray();
        var ceiling = (int)Math.Ceiling(complexes.Length / 2d);

#if NET
        return Vector<T>.Build.Dense([.. complexes[ceiling..], .. complexes[..ceiling]]);
#else
        return Vector<T>.Build.Dense([.. complexes.AsSpan()[ceiling..], .. complexes.AsSpan()[..ceiling]]);
#endif
    }

    /// <summary>
    /// 执行 fft 逆移位操作，撤消 fftshift 的移位
    /// 对应 MATLAB 的 ifftshift 函数
    /// </summary>
    /// <typeparam name="T">向量元素类型</typeparam>
    /// <param name="vector">输入的复数向量(经过 fftshift 的)</param>
    /// <returns>逆移位后的向量</returns>
    public static Vector<T> FastFourierTransformInverseShift<T>(this Vector<T> vector)
        where T : struct, IEquatable<T>, IFormattable
    {
        var complexes = vector.ToArray();
        var floor = (int)Math.Floor(complexes.Length / 2d);

#if NET
        return Vector<T>.Build.Dense([.. complexes[floor..], .. complexes[..floor]]);
#else
        return Vector<T>.Build.Dense([.. complexes.AsSpan()[floor..], .. complexes.AsSpan()[..floor]]);
#endif
    }

    /// <summary>
    /// 获取 FFT 后的频率轴
    /// </summary>
    /// <param name="vector">输入向量（用于确定长度）</param>
    /// <param name="sampleRate">采样率</param>
    /// <returns>频率轴向量</returns>
    public static Vector<double> GetFullFrequencies(this Vector<Complex> vector, double sampleRate)
    {
        var totalLength = vector.Count;

        var frequencies = Vector<double>.Build.Dense(totalLength);

        var firstHalfLength = (totalLength >> 1) + 1;
        var firstHalfIndex = firstHalfLength - 1;

        frequencies.SetSubVectorRange(0, firstHalfIndex,
            Vector<double>.Build.Dense(Generate.LinearRange(0, firstHalfIndex)) * sampleRate / totalLength);

        frequencies.SetSubVectorRange(firstHalfLength, totalLength - 1,
            Vector<double>.Build.Dense(Generate.LinearRange(-firstHalfIndex + 1, -firstHalfIndex + (totalLength - firstHalfLength))) * sampleRate / totalLength);


        return frequencies;
    }

    /// <summary>
    /// 获取 fft 后的正频率轴
    /// </summary>
    /// <param name="vector">输入向量(用于确定长度)</param>
    /// <param name="sampleRate">采样率</param>
    /// <returns>正频率轴向量</returns>
    public static (Vector<double> Frequencies, Vector<double> halfVector) GetPositiveFrequencies(this Vector<Complex> vector, double sampleRate)
    {
        var totalLength = vector.Count;

        var firstHalfLength = (totalLength >> 1) + 1;
        var firstHalfIndex = firstHalfLength - 1;

        var frequencies = Vector<double>.Build.Dense(Generate.LinearRange(0, firstHalfIndex)) * sampleRate / totalLength;

        return (frequencies, vector.SubVectorRange(0, firstHalfIndex).Map(t => t.Magnitude));
    }
}