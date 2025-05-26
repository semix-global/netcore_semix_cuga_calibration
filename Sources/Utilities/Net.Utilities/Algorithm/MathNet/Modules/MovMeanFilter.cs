using MathNet.Numerics.LinearAlgebra;

namespace Net.Utilities.Algorithm.MathNet.Modules;

public static class MovMeanFilter
{
    /// <summary>
    /// MovMean滤波
    /// </summary>
    /// <param name="f">窗口</param>
    /// <param name="signal">信号</param>
    /// <returns>滤波信号</returns>
    public static Vector<double> Smooth(int f, Vector<double> signal)
    {
        if ((f & 1) != 1) throw new ArgumentException("The window size, F, must be an odd integer greater than 0.");

        var n = signal.Count;
        var result = Vector<double>.Build.Dense(n, 0);

        for (var i = 0; i < n; i++)
        {
            var start = Math.Max(0, i - f / 2); // 移动平均滤波窗口的起始位置 (当前位置的半窗口的前一个位置)
            var end = Math.Min(n - 1, i + f / 2); // 移动平均滤波窗口的结束位置 (当前位置的半窗口的后一个位置)
            result[i] = signal.Skip(start).Take(end - start + 1).Average();
        }

        return result;
    }
}