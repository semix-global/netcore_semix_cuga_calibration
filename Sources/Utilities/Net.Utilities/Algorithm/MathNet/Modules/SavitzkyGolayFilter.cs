using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;

namespace Net.Utilities.Algorithm.MathNet.Modules;

/// <summary>
/// Savitzky-Golay 滤波器是一种数字滤波器，可应用于一组数字数据点，以平滑数据，即在不扭曲信号趋势的情况下提高数据的精度<br/>
/// https://github.com/horchler/sgolayfilt/blob/master/sgolayfilt.m<br/>
/// https://en.wikipedia.org/wiki/Savitzky%E2%80%93Golay_filter<br/>
/// <remarks>
/// Savitzky-Golay滤波器是一种数字滤波器，使用局部多项式拟合的方法来平滑数据.
///     - 与传统的移动平均滤波器相比，Savitzky-Golay滤波器能够更好地保持信号的特性（如峰值和宽度）
///     - 它通过在一个滑动窗口内对数据进行多项式拟合，然后用拟合多项式的值替代原始数据，从而实现平滑
/// </remarks>
/// </summary>
public static class SavitzkyGolayFilter
{
    /// <summary>
    /// 生成Savitzky-Golay滤波器的卷积核，常用于平滑信号和计算信号的导数。Savitzky-Golay滤波器通过多项式拟合局部数据窗口，实现平滑效果
    /// </summary>
    /// <param name="k">多项式的阶数（degree），即在滑动窗口内拟合多项式的最高次项</param>
    /// <param name="f">滑动窗口的大小（window size），必须是一个奇数</param>
    /// <returns>构建一个Vandermonde矩阵，其行表示窗口内数据点的x值，列表示多项式的各阶</returns>
    /// <exception cref="ArgumentException">确保k和f是有效的整数，其中f是奇数，并且k小于f</exception>
    public static Matrix<double> Sgolayfilt(int k, int f)
    {
        // f > k 且 f 为奇数
        if (k < 0 || f <= k) throw new ArgumentException("The polynomial order, K, must be a non-negative integer less than the window size, F.");
        if ((f & 1) != 1) throw new ArgumentException("The window size, F, must be an odd integer greater than 0.");

        // Calculate half window size
        var half = (f - 1) / 2.0;

        // Create x vector from -half to half
        var x = Vector<double>.Build.Dense(f, i => i - half);

        // Create Vandermonde matrix: Vandermonde矩阵: 创建一个矩阵S，其行表示滑动窗口内的数据点，列表示多项式的各阶
        var s = Matrix<double>.Build.Dense(f, k + 1, (i, j) => Math.Pow(x[i], j));

        // Perform QR decomposition: QR分解: 对矩阵S进行QR分解，得到正交矩阵Q和上三角矩阵R。滤波器系数矩阵G: 通过矩阵运算得到滤波器的系数矩阵G
        var qr = s.QR(QRMethod.Full);
        var r = qr.R.SubMatrix(0, k + 1, 0, k + 1);
        var g = s * r.Inverse() * r.Transpose().Inverse();

        return g;
    }

    /// <summary>
    ///Savitzky-Golay 滤波
    /// </summary>
    /// <param name="k">多项式的阶数（degree），即在滑动窗口内拟合多项式的最高次项</param>
    /// <param name="f">滑动窗口的大小（window size），必须是一个奇数</param>
    /// <param name="signal">信号</param>
    /// <returns>滤波信号</returns>
    public static Vector<double> Smooth(int k, int f, Vector<double> signal)
    {
        var g = Sgolayfilt(k, f);
        // 在Savitzky-Golay滤波器实现中，Convolve函数用于将信号与滤波器系数矩阵进行卷积，以实现平滑和求导
        var sgolayfiltList = Convolution.Convolve(signal, g.Column(0), true);

        return sgolayfiltList;
    }
}