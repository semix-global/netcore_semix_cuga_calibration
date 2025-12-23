using HalconDotNet;
using Net.Utilities.Algorithms.Halcon.Extensions;

namespace Core.Utilities;

public static class HImageCommonExtensions1
{
    /// <summary>
    /// 获取图片统计信息
    /// </summary>
    /// <param name="this">图片</param>
    /// <returns>尺寸</returns>
    public static (double Average, double Deviation ) GetIntensity(this HImage @this)
    {
        using var gray = @this.ToGray();
        using var region = gray.GetDomain();

        var mean = @this.Intensity(region, out double deviation);

        return (mean, deviation);
    }
}