using CommunityToolkit.Diagnostics;
using MathNet.Numerics.LinearAlgebra;
using System.Globalization;

namespace Net.Utilities.Algorithms.Modules.CurveFitting;

public static class BoltzmannCurve
{
    /// <summary>
    /// 计算 Boltzmann 函数值: y = A2 + (A1 - A2) / (1 + exp((x - x0) / dx)), 其中 dx > 0
    /// </summary>
    /// <param name="a1">x-> -∞, y -> A1, 其中 dx > 0</param>
    /// <param name="a2">x-> +∞, y -> A2, 其中 dx > 0</param>
    /// <param name="x0">中心点[拐点]</param>
    /// <param name="dx">越小 → 分母变化越快 → 曲线越陡; 越大 → 变化慢 → 曲线越平, 其中 dx > 0</param>
    /// <param name="x">自变量</param>
    /// <returns>计算得到的 Boltzmann 函数值</returns>
    public static double Function(double a1, double a2, double x0, double dx, double x) => a2 + (a1 - a2) / (1d + Math.Exp((x - x0) / dx));

    /// <summary>
    /// 计算 Boltzmann 函数的逆运算:
    /// 已知 y，求 x：
    /// x = x0 + dx * ln( (A1 - A2) / (y - A2) - 1 ), 其中 dx > 0
    /// </summary>
    /// <param name="a1">x-> -∞, y -> A1, 其中 dx > 0</param>
    /// <param name="a2">x-> +∞, y -> A2, 其中 dx > 0</param>
    /// <param name="x0">中心点[拐点]</param>
    /// <param name="dx">越小 → 分母变化越快 → 曲线越陡; 越大 → 变化慢 → 曲线越平, 其中 dx > 0</param>
    /// <param name="y">函数值</param>
    /// <returns>使 Function(a1, a2, x0, dx, x) = y 的 x</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// 当 y 不在可逆范围内（介于 a1 和 a2 之间）时抛出
    /// </exception>
    public static double InverseFunction(double a1, double a2, double x0, double dx, double y)
    {
        // y 必须严格处于 (min(a1, a2), max(a1, a2)) 区间内，否则无法求逆
        var min = Math.Min(a2, a1);
        var max = Math.Max(a2, a1);

        Guard.IsTrue(min < y && y < max, "The value y is out of the valid range for inversion.");

        var ratio = (a1 - a2) / (y - a2) - 1d;

        return x0 + dx * Math.Log(ratio);
    }

    /// <summary>
    /// 将提供的数据拟合到 Boltzmann S型模型
    /// </summary>
    /// <param name="x">自变量向量</param>
    /// <param name="y">因变量向量</param>
    /// <returns>
    /// 一个元组, 包含以下内容:
    /// <list type="bullet">
    /// <item><description>A1 (x-> -∞, y -> A1, 其中 Dx > 0)</description></item>
    /// <item><description>A2 (x-> +∞, y -> A2, 其中 Dx > 0)</description></item>
    /// <item><description>X0 (中心点[拐点])</description></item>
    /// <item><description>Dx (越小 → 分母变化越快 → 曲线越陡; 越大 → 变化慢 → 曲线越平, 恒 > 0)</description></item>
    /// <item><description>RSquared (R²)</description></item>
    /// <item><description>YPredicted (预测的 Y 值向量)</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentException">当 x 和 y 长度不同时抛出</exception>
    public static (double A1, double A2, double X0, double Dx, double RSquared, Vector<double> YPredicted) Fit(Vector<double> x, Vector<double> y)
    {
        Guard.IsTrue(x.Count == y.Count, "Vectors x and y must have the same length.");

        // 1. 初始渐近线估计 (A1, A2)
        double guessA1, guessA2;
        if (PolynomialLeastSquares.Polynomial1Fit(x, y).Slope > 0) // 递增
        {
            guessA1 = y.Minimum();
            guessA2 = y.Maximum();
        }
        else
        {
            guessA1 = y.Maximum();
            guessA2 = y.Minimum();
        }

        /*
         * // 2. 使用Origin软件的默认方法估计中心点 (X0) 和斜率 (Dx)，不是很准
         * var guessX0 = x[x.Count / 2];
         * var guessDx = (y.Maximum() - y.Minimum()) / 20d;
         */

        // 2. 线性化拟合以获得更准确的中心点 (X0) 和斜率 (Dx) 初始值
        // 公式推导:
        // y = A2 + (A1 - A2) / (1 + exp((x - x0) / dx))
        // => (A1 - A2) / (y - A2) - 1 = exp((x - x0) / dx)
        // => ln((A1 - A2) / (y - A2) - 1) = (1 / dx) * x - (x0 / dx)
        // 这是一个线性形式 Y' = m * x + c，其中 m = 1/dx, c = -x0/dx

        var lineXList = new List<double>();
        var lineYList = new List<double>();

        for (var i = 0; i < x.Count; i++)
        {
            var log = Math.Log((guessA1 - guessA2) / (y[i] - guessA2) - 1.0);
            if (double.IsNaN(log) || double.IsInfinity(log)) continue;

            lineXList.Add(x[i]);
            lineYList.Add(log);
        }

        var (slope, intercept, _, _) = PolynomialLeastSquares.Polynomial1Fit(
            Vector<double>.Build.Dense([.. lineXList]),
            Vector<double>.Build.Dense([.. lineYList])
        );

        var guessDx = 1.0 / slope;
        var guessX0 = -intercept * guessDx;

        Guard.IsTrue(guessDx > 0, "Initial guess for Dx must be positive.");

        // 3. 使用非线性最小二乘法进行最终拟合（优化所有 4 个参数 A1, A2, X0, Dx）
        var (a1Fit, a2Fit, x0Fit, dxFit) = MathNet.Numerics.Fit.Curve(
            x.AsArray() ?? [.. x],
            y.AsArray() ?? [.. y],
            Function,
            guessA1,
            guessA2,
            guessX0,
            guessDx,
            maxIterations: 10_000);

        Guard.IsTrue(dxFit > 0, "Fitted Dx must be positive.");

        // 计算拟合的 Y 值
        var yPredicted = x.Map(t => Function(a1Fit, a2Fit, x0Fit, dxFit, t));

        // 计算 R²
        var rSquared = RSquared(yPredicted, y);

        return (a1Fit, a2Fit, x0Fit, dxFit, rSquared, yPredicted);
    }

    public static string ToString(double a1, double a2, double x0, double dx, double rSquared, string? format = null, IFormatProvider? formatProvider = null)
    {
        format ??= "0.###";
        formatProvider ??= CultureInfo.CurrentCulture;

        return $"Fit Curve: y = {a2.ToString(format, formatProvider)} + ({a1.ToString(format, formatProvider)} - {a2.ToString(format, formatProvider)}) / (1 + exp((x - {x0.ToString(format, formatProvider)}) / {dx.ToString(format, formatProvider)})), r^2 = {rSquared.ToString(format, formatProvider)}";
    }
    
    
    /// <summary>
    /// 决定系数
    /// </summary>
    /// <param name="yPredicted">模型预测值</param>
    /// <param name="y">实际值</param>
    /// <returns>决定系数</returns>
    public static double RSquared(Vector<double> yPredicted, Vector<double> y)
    {
        Guard.IsTrue(y.Count == yPredicted.Count, "Vectors y and yPredicted must have the same length.");

        var yMean = y.Average();
        var sse = 0d;
        var sst = 0d;

        for (var i = 0; i < y.Count; i++)
        {
            var residual = y[i] - yPredicted[i]; // 残差
            sse += Math.Pow(residual, 2);

            var deviation = y[i] - yMean; // 偏差
            sst += Math.Pow(deviation, 2);
        }

        return 1 - sse / sst;
    }
}