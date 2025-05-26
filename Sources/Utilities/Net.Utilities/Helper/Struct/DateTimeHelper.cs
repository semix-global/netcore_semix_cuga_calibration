using Net.Utilities.Constants;
using System.Globalization;

namespace Net.Utilities.Helper.Struct;

/// <summary>
/// 日期类型转换帮助类
/// </summary>
public static class DateTimeHelper
{
    /// <summary>
    /// 字符串格式化为DateTime
    /// </summary>
    /// <param name="value">字符串</param>
    /// <param name="format">格式化</param>
    /// <returns>DateTime</returns>
    public static DateTime? String2DateTime(string? value, string format = ConstantHelper.DateTimeFormat)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        try
        {
            return DateTime.ParseExact(value, format, CultureInfo.CurrentCulture);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// DateTime格式化为字符串
    /// </summary>
    public static string DateTime2String(DateTime date, string format = ConstantHelper.DateTimeFormat)
    {
        return date.ToString(format, CultureInfo.CurrentCulture);
    }
}