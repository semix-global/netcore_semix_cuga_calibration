using CommunityToolkit.Diagnostics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Optimization;

namespace Core.Utilities;

/// <summary>
/// 提供 Boltzmann S型函数的数据拟合方法。
/// </summary>
public static class Boltzmann
{
    /// <summary>
    /// 计算 Boltzmann 函数值: y = A2 + (A1 - A2) / (1 + exp((x - x0) / dx))
    /// </summary>
    /// <param name="x">自变量。</param>
    /// <param name="a1">上渐近线。</param>
    /// <param name="a2">下渐近线。</param>
    /// <param name="x0">中心点（拐点）。</param>
    /// <param name="dx">斜率参数。</param>
    /// <returns>计算得到的 Boltzmann 函数值。</returns>
    public static double BoltzmannFunction(double x, double a1, double a2, double x0, double dx) => a2 + (a1 - a2) / (1 + Math.Exp((x - x0) / dx));

    /// <summary>
    /// 将提供的数据拟合到 Boltzmann S型模型。
    /// </summary>
    /// <param name="x">自变量向量。</param>
    /// <param name="y">因变量向量。</param>
    /// <returns>
    /// 包含以下内容的元组: A1 (上渐近线), A2 (下渐近线), X0 (中心点), 
    /// Dx (斜率), RSquared (R²), YPredicted (预测的 Y 值向量)。
    /// </returns>
    /// <exception cref="ArgumentException">当 x 和 y 长度不同时抛出。</exception>
    public static (double A1, double A2, double X0, double Dx, double RSquared, Vector<double> YPredicted) BoltzmannFit(Vector<double> x, Vector<double> y)
    {
        if (x.Count != y.Count) return ThrowHelper.ThrowArgumentException<(double A1, double A2, double X0, double Dx, double RSquared, Vector<double> YPredicted)>("Vectors x and y must have the same length.");

        // 初始参数估计
        var a1Init = y.Maximum();
        var a2Init = y.Minimum();
        var x0Init = x[x.Count / 2];
        var dxInit = 1d;

        var initialGuess = Vector<double>.Build.Dense([a1Init, a2Init, x0Init, dxInit]);

        // 使用 BFGS 优化算法
        var objective = ObjectiveFunction.Gradient(ComputeObjective, ComputeGradient);
        var solver = new BfgsMinimizer(1e-8, 1e-8, 1e-8, 1000);
        var result = solver.FindMinimum(objective, initialGuess);

        // 提取拟合参数
        var a1Fit = result.MinimizingPoint[0];
        var a2Fit = result.MinimizingPoint[1];
        var x0Fit = result.MinimizingPoint[2];
        var dxFit = result.MinimizingPoint[3];

        // 计算拟合的 Y 值
        var yPredicted = Vector<double>.Build.Dense(x.Count);
        for (var i = 0; i < x.Count; i++)
        {
            yPredicted[i] = BoltzmannFunction(x[i], a1Fit, a2Fit, x0Fit, dxFit);
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


        double ComputeObjective(Vector<double> parameters) // 目标函数的梯度（数值梯度）
        {
            var a1 = parameters[0];
            var a2 = parameters[1];
            var x0 = parameters[2];
            var dx = parameters[3];

            return x
                .Select(t => BoltzmannFunction(t, a1, a2, x0, dx))
                .Select((predicted, i) => y[i] - predicted)
                .Sum(residual => residual * residual);
        }

        Vector<double> ComputeGradient(Vector<double> parameters) // 目标函数：最小化残差平方和
        {
            var gradient = Vector<double>.Build.Dense(4);
            const double epsilon = 1e-8;

            for (var i = 0; i < 4; i++)
            {
                var paramPlus = parameters.Clone();
                paramPlus[i] += epsilon;
                var paramMinus = parameters.Clone();
                paramMinus[i] -= epsilon;

                gradient[i] = (ComputeObjective(paramPlus) - ComputeObjective(paramMinus)) / (2 * epsilon);
            }

            return gradient;
        }
    }
}