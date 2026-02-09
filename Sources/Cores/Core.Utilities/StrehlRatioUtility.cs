using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Models.Geometries;

namespace Core.Utilities;

public static class StrehlRatioUtility
{
    #region fields

    private const double InitialGuess_A = 2000;
    private const double InitialGuess_B = 0;
    private const double InitialGuess_C = 0;
    private const double InitialGuess_Sigma = 1;

    #endregion fields

    #region private methods

    //高斯函数
    private static Func<double, double, double, double, double, double> gaussian_func = (double a, double b, double c, double sigma, double x) => { return 1 / Math.Sqrt(2 * Math.PI) / sigma * a * Math.Exp(-Math.Pow((x - b), 2) / (2 * Math.Pow(sigma, 2))) + c; };

    /// <summary>
    /// 获取高斯函数参数信息
    /// </summary>
    /// <param name="datas"></param>
    /// <param name="A">Amplitude</param>
    /// <param name="B">x0/y0</param>
    /// <param name="C">常数</param>
    /// <param name="Sigma">sigma</param>
    private static void GetGaussianParameters(double[] datas, out double A, out double B, out double C, out double Sigma)
    {
        try
        {
            var xCoords = GetXCoord(datas);
            (double a, double b, double c, double sigma) = Fit.Curve(xCoords, datas, gaussian_func, InitialGuess_A, InitialGuess_B, InitialGuess_C, InitialGuess_Sigma, maxIterations: 9999);
            A = a;
            B = b;
            C = c;
            Sigma = sigma;
        }
        catch
        {
            A = B = C = Sigma = 0;
        }
    }

    /// <summary>
    /// 获取横坐标数组
    /// </summary>
    /// <param name="datas"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static double[] GetXCoord(double[] datas)
    {
        var length = datas.Length;
        if (length == 0)
            throw new Exception("unvalid datas !");
        var maxIndex = MaxInex(datas);
        double start = -maxIndex;
        return [.. Enumerable.Range((int)start, length).Select(i => (double)i)];
    }

    /// <summary>
    /// 获取原始数据最大值索引
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arr"></param>
    /// <returns></returns>
    private static int MaxInex<T>(T[] arr) where T : IComparable<T>
    {
        int i_Pos = 0;
        var value = arr[0];
        for (var i = 1; i < arr.Length; ++i)
        {
            var _value = arr[i];
            if (_value.CompareTo(value) > 0)
            {
                value = _value;
                i_Pos = i;
            }
        }

        return i_Pos;
    }

    #endregion private methods

    #region public methods

    /// <summary>
    /// 获取StrehlRatio
    /// </summary>
    /// <param name="datas">原始数据</param>
    /// <param name="scale">pixelToum</param>
    /// <param name="cylinder_diameter">圆柱直径</param>
    /// <param name="idealSpot_diameter">理想光斑</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static double GetStrehlRatio(double[] datas, double scale, double cylinder_diameter, double idealSpot_diameter)
    {
        var length = datas.Length;
        if (length == 0)
            throw new Exception("unvalid datas !");
        GetGaussianParameters(datas, out _, out _, out _, out double sigma);
        var phy_sigma = sigma * scale;
        var phy_sigma_corrected = phy_sigma - cylinder_diameter / 4;
        var phy_sigma_ideal = idealSpot_diameter / 4;
        var strehl_ratio = phy_sigma_ideal / phy_sigma_corrected;
        return Math.Round(strehl_ratio, 3);
    }

    /// <summary>
    /// 获取拟合数据
    /// </summary>
    /// <param name="datas">原始数据</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static double[] GetFittedDatas(double[] datas)
    {
        var length = datas.Length;
        if (length == 0)
            throw new Exception("unvalid datas !");
        var xCoords = GetXCoord(datas);
        GetGaussianParameters(datas, out double a, out double b, out double c, out double sigma);
        var datas_Fitted = new double[datas.Length];
        for (int i = 0; i < xCoords.Length; i++)
        {
            var fittedData = gaussian_func(a, b, c, sigma, xCoords[i]);
            datas_Fitted[i] = fittedData;
        }

        return datas_Fitted;
    }

    public static ((double StrehlRatio, double[] Line, double[] FitLine) XStrehlRatio, (double StrehlRatio, double[] Line, double[] FitLine) YStrehlRatio) GetStrehlRatio(short[,] image, Rect rect, double xPixelSize, double yPixelSize, double potDiameter,
        double xPointDiameter, double yPointDiameter)
    {
        var (x, y, width, height) = (RectI)rect;
        var subMatrix = Matrix<double>.Build.DenseOfArray(image).SubMatrix(x, width, y, height);

        var xStrehlRatioList = new List<(double StrehlRatio, double[] Line, double[] FitLine)>();
        for (var i = 0; i < subMatrix.RowCount; i++)
        {
            var line = subMatrix.Row(i).ToArray();
            var fitLine = GetFittedDatas(line);
            xStrehlRatioList.Add((GetStrehlRatio(line, xPixelSize, potDiameter, xPointDiameter), line, fitLine));
        }

        var yStrehlRatioList = new List<(double StrehlRatio, double[] Line, double[] FitLine)>();
        for (var j = 0; j < subMatrix.ColumnCount; j++)
        {
            var line = subMatrix.Column(j).ToArray();
            var fitLine = GetFittedDatas(line);
            yStrehlRatioList.Add((GetStrehlRatio(line, yPixelSize, potDiameter, yPointDiameter), line, fitLine));
        }

        return (xStrehlRatioList.OrderByDescending(t => t.StrehlRatio).First(), yStrehlRatioList.OrderByDescending(t => t.StrehlRatio).First());
    }

    #endregion public methods
}