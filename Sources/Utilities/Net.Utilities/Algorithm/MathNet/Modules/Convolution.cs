using MathNet.Numerics.LinearAlgebra;

namespace Net.Utilities.Algorithm.MathNet.Modules;

/// <summary>
/// 卷积: https://github.com/accord-net/framework.git (Matrix.Common.cs: Matrix.Convolve)<br/>
/// https://en.wikipedia.org/wiki/Convolution
/// </summary>
public static class Convolution
{
    /// <summary>
    /// 使用给定的内核对数组进行卷积
    /// </summary>
    /// <param name="signal">信号数组</param>
    /// <param name="kernel">卷积核</param>
    /// <param name="trim">如果<c>true</c>则生成的数组将被修剪为与输入数组的长度相同, 默认值为 false</param>
    /// <returns>卷积结果</returns>
    public static Vector<double> Convolve(Vector<double> signal, Vector<double> kernel, bool trim)
    {
        Vector<double> result;
        var m = (int)Math.Ceiling(kernel.Count / 2d); // 天花板函数

        if (trim)
        {
            result = Vector<double>.Build.Dense(signal.Count);

            for (var i = 0; i < result.Count; i++)
            {
                result[i] = 0;
                for (var j = 0; j < kernel.Count; j++)
                {
                    var k = i - j + m - 1;
                    if (k >= 0 && k < signal.Count)
                        result[i] += signal[k] * kernel[j];
                }
            }
        }
        else
        {
            result = Vector<double>.Build.Dense(signal.Count + m);

            for (var i = 0; i < result.Count; i++)
            {
                result[i] = 0;
                for (var j = 0; j < kernel.Count; j++)
                {
                    var k = i - j;
                    if (k >= 0 && k < signal.Count)
                        result[i] += signal[k] * kernel[j];
                }
            }
        }

        return result;
    }
}