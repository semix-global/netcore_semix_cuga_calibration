using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearRegression;

namespace Net.Utilities.Algorithm.MathNet.Modules;

public static class PolyFit
{
    /// <summary>
    /// 最小二乘曲线拟合进程, 使用Q-R分解
    /// 最小二乘法将点 (x,y) 拟合到一条直线
    /// 最小二乘法 y : x -> a + b * x
    /// 以下两种实现方式等价, 但是第一种不适用, 因为斜率pi/2不是Infinity
    /// /*
    ///  *   /// <summary>
    ///  *   /// Least-Squares fitting the points (x,y) to a line y : x ->; a+b*x,
    ///  *   /// returning its best fitting parameters as (a, b) tuple,
    ///  *   /// where an is the intercept and b the slope.
    ///  *   /// </summary>
    ///  *   public static (double A, double B) Line(double[] x, double[] y) => SimpleRegression.Fit(x, y); // 不适用这个, 因为斜率pi/2不是Infinity
    ///  *
    ///  *   /// <summary>
    ///  *   /// Least-Squares fitting the points (x,y) to a k-order polynomial y : x ->; p0 + p1*x + p2*x^2 + ... + pk*x^k,
    ///  *   /// returning its best fitting parameters as [p0, p1, p2, ..., pk] array, compatible with Polynomial.Evaluate.
    ///  *   /// A polynomial with order/degree k has (k+1) coefficients and thus requires at least (k+1) samples.
    ///  *   /// </summary>
    ///  *   public static double[] Polynomial(
    ///  *     double[] x,
    ///  *     double[] y,
    ///  *     int order,
    ///  *     DirectRegressionMethod method = DirectRegressionMethod.QR)
    ///  *
    ///  */
    /// </summary>
    /// <param name="x">预测变量</param>
    /// <param name="y">实际值</param>
    /// <returns>(斜率, 截距，相关系数的平方, 拟合值)</returns>
    // ReSharper disable UnusedTupleComponentInReturnValue
    public static (double Slope, double Intercept, double RSquared, Vector<double> YPredicted) Poly1Fit(Vector<double> x, Vector<double> y)
    {
        if (x.Count != y.Count) throw new ArgumentException("Vectors x and y must have the same length.");

        // 最小二乘法 y : x -> p0 + p1*x + p2*x^2 + p3*x^3 + p4*x^4 + p5*x^5 + p6*x^6 + ... + pn*x^n
        var polynomial = Fit.Polynomial([.. x], [.. y], 1, method: DirectRegressionMethod.QR);
        var slope = polynomial[1];
        var intercept = polynomial[0];

        // 计算预测值
        var yPredicted = x.Map(t => Polynomial.Evaluate(t, polynomial));

        // 计算 r^2，即观察到的结果和观察到的预测值之间的样本相关系数的平方. 参数： 建模/预测值，观察/实际值
        var rSquared = GoodnessOfFit.RSquared(yPredicted, y);

        return (slope, intercept, rSquared, yPredicted);
    }

    /// <summary>
    /// 最小二乘曲线拟合进程, 使用Q-R分解
    /// y : x -> p0 + p1*x + p2*x^2
    /// </summary>
    /// <param name="x">预测变量</param>
    /// <param name="y">实际值</param>
    /// <returns>2次多项式的所有系数, 相关系数的平方, 拟合值</returns>
    public static (double P0, double P1, double P2, double RSquared, Vector<double> YPredicted) Poly2Fit(Vector<double> x, Vector<double> y)
    {
        if (x.Count != y.Count) throw new ArgumentException("Vectors x and y must have the same length.");

        var polynomial = Fit.Polynomial([.. x], [.. y], 2, method: DirectRegressionMethod.QR);
        var yPredicted = x.Map(t => Polynomial.Evaluate(t, polynomial));
        var rSquared = GoodnessOfFit.RSquared(yPredicted, y);

        return (polynomial[0], polynomial[1], polynomial[2], rSquared, yPredicted);
    }

    /// <summary>
    /// 最小二乘曲线拟合进程, 使用Q-R分解
    /// y : x -> p0 + p1*x + p2*x^2 + p3*x^3
    /// </summary>
    /// <param name="x">预测变量</param>
    /// <param name="y">实际值</param>
    /// <returns>3次多项式的所有系数, 相关系数的平方, 拟合值</returns>
    public static (double P0, double P1, double P2, double P3, double RSquared, Vector<double> YPredicted) Poly3Fit(Vector<double> x, Vector<double> y)
    {
        if (x.Count != y.Count) throw new ArgumentException("Vectors x and y must have the same length.");

        var polynomial = Fit.Polynomial([.. x], [.. y], 3, method: DirectRegressionMethod.QR);
        var yPredicted = x.Map(t => Polynomial.Evaluate(t, polynomial));
        var rSquared = GoodnessOfFit.RSquared(yPredicted, y);

        return (polynomial[0], polynomial[1], polynomial[2], polynomial[3], rSquared, yPredicted);
    }

    /// <summary>
    /// 最小二乘曲线拟合进程, 使用Q-R分解
    /// y : x -> p0 + p1*x + p2*x^2 + p3*x^3 + p4*x^4 + p5*x^5
    /// </summary>
    /// <param name="x">预测变量</param>
    /// <param name="y">实际值</param>
    /// <returns>5次多项式的所有系数, 相关系数的平方, 拟合值</returns>
    public static (double P0, double P1, double P2, double P3, double P4, double P5, double RSquared, Vector<double> YPredicted) Poly5Fit(Vector<double> x, Vector<double> y)
    {
        if (x.Count != y.Count) throw new ArgumentException("Vectors x and y must have the same length.");

        var polynomial = Fit.Polynomial([.. x], [.. y], 5, method: DirectRegressionMethod.QR);
        var yPredicted = x.Map(t => Polynomial.Evaluate(t, polynomial));
        var rSquared = GoodnessOfFit.RSquared(yPredicted, y);

        return (polynomial[0], polynomial[1], polynomial[2], polynomial[3], polynomial[4], polynomial[5], rSquared, yPredicted);
    }

    /// <summary>
    /// 最小二乘曲线拟合进程, 使用Q-R分解
    /// y : x -> p0 + p1*x + p2*x^2 + p3*x^3 + p4*x^4 + p5*x^5 + p6*x^6 + ... + pn*x^n
    /// </summary>
    /// <param name="x">预测变量</param>
    /// <param name="y">实际值</param>
    /// <param name="order">多项式最高次数n</param>
    /// <returns>预测值</returns>
    public static (double[] polynomial, double RSquared, Vector<double> YPredicted) PolyFitFunc(Vector<double> x, Vector<double> y, int order)
    {
        if (x.Count != y.Count) throw new ArgumentException("Vectors x and y must have the same length.");

        // 最小二乘法 y : x -> p0 + p1*x + p2*x^2 + p3*x^3 + p4*x^4 + p5*x^5 + p6*x^6 + ... + pn*x^n
        var polynomial = Fit.Polynomial([.. x], [.. y], order, method: DirectRegressionMethod.QR);

        var yPredicted = x.Map(t => Polynomial.Evaluate(t, polynomial));
        var rSquared = GoodnessOfFit.RSquared(yPredicted, y);

        return (polynomial, rSquared, yPredicted);
    }

    // 定义五次多项式函数
    public static double FifthDegreePolynomial(double x, double[] coefficients)
    {
        // 多项式形式: a5*x^5 + a4*x^4 + a3*x^3 + a2*x^2 + a1*x + a0
        return coefficients[5] * Math.Pow(x, 5) +
               coefficients[4] * Math.Pow(x, 4) +
               coefficients[3] * Math.Pow(x, 3) +
               coefficients[2] * Math.Pow(x, 2) +
               coefficients[1] * x +
               coefficients[0];
    }

    // 定义梯形法积分函数
    public static double TrapezoidalRule(Func<double, double> f, double a, double b, int n)
    {
        // 计算步长
        var h = (b - a) / n;

        // 计算积分值
        var sum = 0.5 * (f(a) + f(b)); // 首尾两项
        for (var i = 1; i < n; i++)
        {
            var x = a + i * h; // 当前节点
            sum += f(x); // 中间项
        }

        return sum * h; // 乘以步长
    }
}