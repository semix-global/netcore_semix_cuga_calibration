using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;

namespace Net.Utilities.Algorithms.Modules;

/// <summary>
/// Savitzky-Golay 二维曲线平滑扩展
/// </summary>
public static class SavitzkyGolayFilter2D
{
    /// <summary>
    /// 对二维曲线进行 Savitzky-Golay 平滑处理，考虑X坐标的分布
    /// 在滑动窗口内，基于X坐标拟合多项式来平滑Y坐标
    /// </summary>
    /// <param name="k">多项式的阶数（degree），即在滑动窗口内拟合多项式的最高次项</param>
    /// <param name="f">滑动窗口的大小（window size），必须是一个奇数</param>
    /// <param name="xPoints">X坐标向量</param>
    /// <param name="yPoints">Y坐标向量（将被平滑）</param>
    /// <returns>平滑后的曲线，X坐标不变，Y坐标被平滑（考虑X的分布）</returns>
    /// <exception cref="ArgumentException">当X和Y向量长度不一致时抛出异常</exception>
    public static (Vector<double> X, Vector<double> Y) SmoothCurve(int k, int f, Vector<double> xPoints, Vector<double> yPoints)
    {
        if (xPoints.Count != yPoints.Count)
            throw new ArgumentException("X and Y vectors must have the same length.");

        if (k < 0 || f <= k)
            throw new ArgumentException("The polynomial order, K, must be a non-negative integer less than the window size, F.");

        if ((f & 1) != 1)
            throw new ArgumentException("The window size, F, must be an odd integer greater than 0.");

        var n = xPoints.Count;
        var halfWindow = (f - 1) / 2;
        var smoothedY = Vector<double>.Build.Dense(n);

        for (int i = 0; i < n; i++)
        {
            // 确定窗口范围
            var start = Math.Max(0, i - halfWindow);
            var end = Math.Min(n - 1, i + halfWindow);
            var windowSize = end - start + 1;

            // 提取窗口内的X和Y值
            var windowX = Vector<double>.Build.Dense(windowSize, j => xPoints[start + j]);
            var windowY = Vector<double>.Build.Dense(windowSize, j => yPoints[start + j]);

            // 获取当前点的X坐标
            var currentX = xPoints[i];

            // 构建Vandermonde矩阵，基于窗口内的X坐标
            var actualK = Math.Min(k, windowSize - 1);
            var vandermondeMatrix = Matrix<double>.Build.Dense(windowSize, actualK + 1,
                (row, col) => Math.Pow(windowX[row], col));

            try
            {
                // 使用QR分解求解最小二乘问题: V * coeffs = windowY
                var qr = vandermondeMatrix.QR(QRMethod.Full);
                var coeffs = qr.Solve(windowY);

                // 使用拟合的多项式计算当前点的平滑Y值
                smoothedY[i] = 0;
                for (int j = 0; j <= actualK; j++)
                {
                    smoothedY[i] += coeffs[j] * Math.Pow(currentX, j);
                }
            }
            catch
            {
                // 如果拟合失败，使用原始值
                smoothedY[i] = yPoints[i];
            }
        }

        return (xPoints, smoothedY);
    }
}