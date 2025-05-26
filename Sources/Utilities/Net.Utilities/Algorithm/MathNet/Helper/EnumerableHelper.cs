using CommunityToolkit.Diagnostics;
using Net.Utilities.Constants;
using System.Collections.ObjectModel;

namespace Net.Utilities.Algorithm.MathNet.Helper;

public static class EnumerableHelper
{
    #region Enumerable

    /// <summary>
    /// 安全的Range用法
    /// </summary>
    /// <param name="count">数量</param>
    /// <returns>list</returns>
    public static IEnumerable<int> Range(int count)
    {
        return count <= 0 ? [] : Enumerable.Range(0, count);
    }

    #endregion Enumerable

    #region List

    /// <summary>
    /// 安全获取List的值
    /// </summary>
    /// <typeparam name="T">列表数据类型</typeparam>
    /// <param name="list">列表</param>
    /// <param name="index">索引</param>
    /// <param name="isCanGetNearbyIndex">超过索引获取最接近一个值</param>
    /// <returns>值</returns>
    public static (bool IsSuccess, T? value) Get<T>(IList<T> list, int index, bool isCanGetNearbyIndex = false)
    {
        if (index < 0)
        {
            return isCanGetNearbyIndex ? (true, list[0]) : (false, default);
        }

        if (index >= list.Count)
        {
            return isCanGetNearbyIndex ? (true, list[^1]) : (false, default);
        }

        return (true, list[index]);
    }

    /// <summary>
    /// 连续5个false返回true
    /// </summary>
    /// <param name="resultList">bool list</param>
    /// <param name="consecutiveCount">连续多少个</param>
    /// <returns>连续5多少个false返回true</returns>
    public static bool HasConsecutiveFalse(List<bool> resultList, int consecutiveCount)
    {
        if (resultList.Count - consecutiveCount < 0) return false;

        for (var i = 0; i <= resultList.Count - consecutiveCount; i++)
        {
            if (resultList[i] == false && resultList.GetRange(i, consecutiveCount).All(v => v == false))
            {
                return true; // 提前返回
            }
        }

        return false;
    }

    /// <summary>
    /// 连续n个true返回true
    /// </summary>
    /// <param name="resultList">bool list</param>
    /// <param name="consecutiveCount">连续多少个</param>
    /// <returns>连续5多少个false返回true</returns>
    public static bool HasConsecutiveTrue(List<bool> resultList, int consecutiveCount)
    {
        if (resultList.Count - consecutiveCount < 0) return false;

        for (var i = 0; i <= resultList.Count - consecutiveCount; i++)
        {
            if (resultList[i] && resultList.GetRange(i, consecutiveCount).All(v => v))
            {
                return true; // 提前返回
            }
        }

        return false;
    }

    /// <summary>
    /// 是否有极大值, 且最大值是极大值
    /// </summary>
    /// <param name="sequence">序列</param>
    /// <returns>是否有极大值</returns>
    public static bool HasMaximumIsMax(List<double> sequence)
    {
        // 序列必须至少有3个元素
        if (sequence.Count < 3) return false;
        // 序列如果全部相等
        if (sequence.All(v => v - sequence[0] == 0)) return false;

        var maxIndex = sequence.IndexOf(sequence.Max());
        // 检查左侧是否递增
        for (var i = 0; i < maxIndex; i++)
        {
            if (sequence[i] >= sequence[i + 1])
            {
                return false;
            }
        }

        // 检查右侧是否递减
        for (var i = maxIndex + 1; i < sequence.Count - 1; i++)
        {
            if (sequence[i] <= sequence[i + 1])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 判断是否有连续count下降的索引
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="count">连续count</param>
    /// <returns>索引, -1表示无</returns>
    public static int FindDescendingSequenceIndex(List<double> data, int count)
    {
        // 遍历数组中的元素
        for (var i = 0; i <= data.Count - count; i++)
        {
            // 判断是否存在连续下降的序列
            if (IsDescending(i)) return i;
        }

        // 如果没有找到，返回 -1
        return -1;

        bool IsDescending(int startIndex) // 判断从某个起始索引开始的元素是否连续下降
        {
            for (var i = startIndex; i < startIndex + count - 1; i++)
            {
                if (data[i] <= data[i + 1])
                {
                    return false;
                }
            }

            return true;
        }
    }

    #endregion List

    #region Generate

    /// <summary>
    /// 生成指定范围的列表
    /// </summary>
    /// <param name="center">中间的数值</param>
    /// <param name="step">步进的长度</param>
    /// <param name="halfCount">左右一半的数量</param>
    /// <returns>列表</returns>
    public static List<double> GenerateList(double center, double step, int halfCount)
    {
        if (step == 0 && halfCount == 0) return [center];

        Guard.IsGreaterThan(step, 0, nameof(step));
        Guard.IsGreaterThan(halfCount, 0, nameof(halfCount));

        var lower = center - halfCount * step;
        var upper = center + halfCount * step;
        var list = new List<double>();

        for (var i = lower; i <= upper; i += step)
        {
            list.Add(i);
        }

        return list;
    }

    /// <summary>
    /// 生成指定范围的列表
    /// </summary>
    /// <param name="startMin">起始最小值</param>
    /// <param name="endMax">结束最大值</param>
    /// <param name="step">步距</param>
    /// <returns>列表</returns>
    public static IEnumerable<double> GenerateList(double startMin, double endMax, double step)
    {
        if (Math.Abs(startMin - endMax) < ConstantHelper.Tolerance && step == 0) return [startMin];

        Guard.IsGreaterThan(step, 0, nameof(step));
        Guard.IsGreaterThan(endMax, startMin, nameof(endMax));

        return ((double[])[startMin, .. Enumerable.Range(1, (int)Math.Floor((endMax - startMin) / step)).Select(x => startMin + x * step), endMax]).Distinct();
    }

    #endregion Generate

    #region Compare

    public static bool ObservableCollectionsCompare<T>(ObservableCollection<T> collection1, ObservableCollection<T> collection2)
    {
        if (collection1.Count != collection2.Count)
            return false;

        for (var i = 0; i < collection1.Count; i++)
        {
            if (!collection1[i]!.Equals(collection2[i])) // 使用自定义的 Equals 方法
                return false;
        }

        return true;
    }

    #endregion Compare
}