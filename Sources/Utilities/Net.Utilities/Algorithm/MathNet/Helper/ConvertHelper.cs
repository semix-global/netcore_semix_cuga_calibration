namespace Net.Utilities.Algorithm.MathNet.Helper;

public static class ConvertHelper
{
    /// <summary>
    /// 解决double转short溢出问题
    /// </summary>
    /// <param name="value">double</param>
    /// <returns>short</returns>
    public static short ToInt16NotOverflowException(double value)
    {
        return value switch
        {
            > short.MaxValue => short.MaxValue,
            < short.MinValue => short.MinValue,
            _ => Convert.ToInt16(value)
        };
    }
}