using MathNet.Numerics.LinearAlgebra;

namespace Net.Utilities.Algorithm.MathNet.Modules;

/// <summary>
/// 一种在嘈杂的周期性和准周期信号中自动检测峰值的高效算法 AMPD<br/>
/// https://zhuanlan.zhihu.com/p/549588865<br/>
/// https://www.mdpi.com/1999-4893/5/4/588<br/>
/// <remarks>
/// 1. 构造尺度矩阵：对每个尺度计算局部极小值
/// 2. 计算局部极小值矩阵：标记局部极小值
/// 3. 找到最优尺度：通过统计每个尺度上的局部极小值数量来找到最优尺度
/// 4. 检测峰值：在最优尺度上检测局部极大值作为信号的峰值
///  </remarks>
/// </summary>
public static class AutomaticMPeakDetection
{
    /// <summary>
    /// 在数字信号处理中，经常涉及到波峰（或波谷，需要将信号反向后再用此方法查找）查找算法,自动多尺度峰值查找算法
    /// <remarks>
    /// （1）算法本身（几乎）没有超参数，无需调参，对信号具有良好的自适应性，唯一的假设是信号是周期的或者准周期的；
    /// （2）抗噪能力强，后面可以看到，对周期性的要求也不是很高。
    /// </remarks>
    /// </summary>
    /// <param name="signal">信号数组</param>
    /// <returns>波峰列表</returns>
    public static int[] Ampd(Vector<double> signal)
    {
        var signalWindow = Vector<double>.Build.Dense(signal.Count, 0);

        var arrRowSum = new List<double>();
        var l = (int)Math.Ceiling(signal.Count / 2d);
        for (var k = 1; k < l; k++)
        {
            var rowSum = 0d;
            for (var i = k; i < signal.Count - k; i++)
            {
                if (signal[i] > signal[i - k] && signal[i] > signal[i + k])
                    rowSum -= 1;
            }

            arrRowSum.Add(rowSum);
        }

        var maxWindowLength = Vector<double>.Build.DenseOfEnumerable(arrRowSum).MinimumIndex();

        for (var k = 1; k < maxWindowLength + 1; k++)
        {
            for (var i = k; i < signal.Count - k; i++)
            {
                if (signal[i] > signal[i - k] && signal[i] > signal[i + k])
                    signalWindow[i] += 1;
            }
        }

        return
        [
            .. signalWindow
                .Select((value, index) => (Value: value, Index: index))
                .Where(x => x.Value - maxWindowLength == 0)
                .Select(x => x.Index)
        ];
    }
}