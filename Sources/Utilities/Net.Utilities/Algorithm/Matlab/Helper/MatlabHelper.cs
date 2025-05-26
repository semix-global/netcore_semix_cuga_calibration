using MathWorks.MATLAB.NET.Arrays;

namespace Net.Utilities.Algorithm.Matlab.Helper;

public static class MatlabHelper
{
    /// <summary>
    /// 将 MWNumericArray 转换为二维数组的方法
    /// </summary>
    /// <param name="mwArray">MWNumericArray</param>
    /// <returns>二维数组</returns>
    public static double[,] ConvertMwNumericArrayTo2DArray(MWNumericArray mwArray)
    {
        var rows = mwArray.Dimensions[0];
        var cols = mwArray.Dimensions[1];
        var arrayData = new double[rows, cols];

        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
            {
                arrayData[i, j] = mwArray[i + 1, j + 1].ToScalarDouble(); // MATLAB indices are 1-based
            }
        }

        return arrayData;
    }
}