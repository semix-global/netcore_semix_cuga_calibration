using MathNet.Numerics.IntegralTransforms;
using System.Numerics;
using LinearAlgebra = MathNet.Numerics.LinearAlgebra;

namespace Net.Utilities.Algorithm.MathNet.Modules;

public static class FourierTransform
{
    /// <summary>
    /// Matlab: 快速傅里叶变换 (FFT) 算法计算 X 的离散傅里叶变换 (DFT)
    /// </summary>
    /// <param name="signal">信号量</param>
    /// <returns>该信号量的傅里叶变换</returns>
    public static LinearAlgebra.Vector<Complex> MatlabFastFourierTransform(LinearAlgebra.Vector<double> signal)
    {
        var complexes = signal.Map(temp => new Complex(temp, 0)).ToArray();
        Fourier.Forward(complexes, FourierOptions.Matlab);

        return LinearAlgebra.Vector<Complex>.Build.DenseOfArray(complexes);
    }

    /// <summary>
    /// Matlab: 将零频分量移动到数组中心，重新排列傅里叶变换X<br/>
    /// 如果一个向量的元素数为奇数，则中间的元素被视为属于向量的左半部分
    /// </summary>
    /// <param name="signal">信号量</param>
    /// <returns>将零频分量移动到数组中心，重新排列傅里叶变换X</returns>
    public static LinearAlgebra.Vector<double> MatlabFastFourierTransformShift(LinearAlgebra.Vector<double> signal)
    {
        var signals = signal.ToArray();

        var ceiling = (int)Math.Ceiling(signals.Length / 2d);

#if NET
        return LinearAlgebra.Vector<double>.Build.DenseOfArray([.. signals[ceiling..], .. signals[..ceiling]]);
#else
        return LinearAlgebra.Vector<double>.Build.DenseOfArray([.. signals.AsSpan()[ceiling..], .. signals.AsSpan()[..ceiling]]);
#endif
    }
}