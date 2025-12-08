using CommunityToolkit.Diagnostics;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using Net.Utilities.Algorithms.Extensions;

namespace Core.Utilities;

/// <summary>
/// 提供 Boltzmann S型函数的数据拟合方法。
/// </summary>
public static class Boltzmann
{
    /// <summary>
    /// 计算 Boltzmann 函数值: y = A2 + (A1 - A2) / (1 + exp((x - x0) / dx))
    /// </summary>
    /// <param name="a1">上渐近线</param>
    /// <param name="a2">下渐近线</param>
    /// <param name="x0">中心点（拐点）</param>
    /// <param name="dx">斜率参数</param>
    /// <param name="x">自变量</param>
    /// <returns>计算得到的 Boltzmann 函数值</returns>
    public static double BoltzmannFunction(double a1, double a2, double x0, double dx, double x) => a2 + (a1 - a2) / (1 + Math.Exp((x - x0) / dx));

    /// <summary>
    /// 计算 Boltzmann 函数的逆运算:
    /// 已知 y，求 x：
    /// x = x0 + dx * ln( (A1 - A2) / (y - A2) - 1 )
    /// </summary>
    /// <param name="a1">上渐近线</param>
    /// <param name="a2">下渐近线</param>
    /// <param name="x0">中心点（拐点）</param>
    /// <param name="dx">斜率参数</param>
    /// <param name="y">函数值</param>
    /// <returns>使 BoltzmannFunction(a1, a2, x0, dx, x) = y 的 x</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// 当 y 不在可逆范围内（介于 a1 和 a2 之间）时抛出
    /// </exception>
    public static double BoltzmannInverse(double a1, double a2, double x0, double dx, double y)
    {
        // y 必须严格处于 (min(a1, a2), max(a1, a2)) 区间内，否则无法求逆
        var min = Math.Min(a2, a1);
        var max = Math.Max(a2, a1);

        if (y <= min || y >= max) return ThrowHelper.ThrowArgumentOutOfRangeException<double>("The value y is out of the valid range for inversion.");

        double ratio = (a1 - a2) / (y - a2) - 1.0;

        return x0 + dx * Math.Log(ratio);
    }

    /// <summary>
    /// 将提供的数据拟合到 Boltzmann S型模型
    /// </summary>
    /// <param name="x">自变量向量</param>
    /// <param name="y">因变量向量</param>
    /// <returns>
    /// 包含以下内容的元组: A1 (上渐近线), A2 (下渐近线), X0 (中心点), Dx (斜率), RSquared (R²), YPredicted (预测的 Y 值向量)
    /// </returns>
    /// <exception cref="ArgumentException">当 x 和 y 长度不同时抛出</exception>
    public static (double A1, double A2, double X0, double Dx, double RSquared, Vector<double> YPredicted) BoltzmannFit(Vector<double> x, Vector<double> y)
    {
        if (x.Count != y.Count) return ThrowHelper.ThrowArgumentException<(double A1, double A2, double X0, double Dx, double RSquared, Vector<double> YPredicted)>("Vectors x and y must have the same length.");

        // 初始参数估计
        var guessA1 = y.Maximum();
        var guessA2 = y.Minimum();
        var guessX0 = x[(y - (guessA1 + guessA2) / 2d).AbsoluteMinimumIndex()];
        var guessDx = (y.Differentiate() / x.Differentiate()).PointwiseAbs().Median();
        // guessX0 = x[x.Count / 2];
        // guessDx = 1d;

        if (guessDx < 0.1) guessDx = 0.5;

        // 使用非线性最小二乘法拟合
        var (a1Fit, a2Fit, x0Fit, dxFit) = Fit.Curve(
            x.AsArray() ?? x.ToArray(),
            y.AsArray() ?? y.ToArray(),
            BoltzmannFunction,
            guessA1,
            guessA2,
            guessX0,
            guessDx,
            maxIterations: 1000);

        // 计算拟合的 Y 值
        var yPredicted = Vector<double>.Build.Dense(x.Count);
        for (var i = 0; i < x.Count; i++)
        {
            yPredicted[i] = BoltzmannFunction(a1Fit, a2Fit, x0Fit, dxFit, x[i]);
        }

        // 计算 R²
        var yMean = y.Average();
        var sse = 0d;
        var sst = 0d;

        for (var i = 0; i < x.Count; i++)
        {
            var residual = y[i] - yPredicted[i];
            sse += residual * residual;
            sst += Math.Pow(y[i] - yMean, 2);
        }

        var rSquared = 1 - (sse / sst);

        return (a1Fit, a2Fit, x0Fit, dxFit, rSquared, yPredicted);
    }
}