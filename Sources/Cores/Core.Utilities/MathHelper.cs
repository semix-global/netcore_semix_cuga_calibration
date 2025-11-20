using CommunityToolkit.Diagnostics;

namespace Core.Utilities;

public static class MathHelper
{
    /// <summary>
    /// 计算滑动窗口次数（允许最后一个窗口部分覆盖）
    /// </summary>
    /// <param name="windowLength">窗口长度</param>
    /// <param name="windowStep">窗口步进</param>
    /// <param name="totalLength">总长度</param>
    /// <returns>滑动次数</returns>
    public static int SlideCount(double windowLength, double windowStep, double totalLength)
    {
        Guard.IsGreaterThan(windowLength, 0);
        Guard.IsGreaterThan(windowStep, 0);
        Guard.IsGreaterThan(totalLength, 0);

        if (windowLength >= totalLength)
            return 1; // 窗口大于等于总长度，只需1次

        return (int)Math.Ceiling((totalLength - windowLength) / windowStep) + 1;
    }

    /// <summary>
    /// 计算滑动窗口次数（只计算完整窗口，最后不足的不算）
    /// </summary>
    /// <param name="windowLength">窗口长度</param>
    /// <param name="windowStep">窗口步进</param>
    /// <param name="totalLength">总长度</param>
    /// <returns>完整窗口滑动次数</returns>
    public static int SlideCountFull(double windowLength, double windowStep, double totalLength)
    {
        Guard.IsGreaterThan(windowLength, 0);
        Guard.IsGreaterThan(windowStep, 0);
        Guard.IsGreaterThan(totalLength, 0);

        if (windowLength > totalLength)
            return 0; // 窗口大于总长度，没有完整窗口

        return (int)Math.Floor((totalLength - windowLength) / windowStep) + 1;
    }
}