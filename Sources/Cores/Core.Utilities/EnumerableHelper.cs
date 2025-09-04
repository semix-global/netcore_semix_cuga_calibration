namespace Core.Utilities;

public static class EnumerableHelper
{
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

    public static bool IsStrictlyIncreasing<T>(IEnumerable<T> sequence) where T : IComparable<T>
    {
        using var enumerator = sequence.GetEnumerator();

        if (enumerator.MoveNext() == false) return true;

        var previous = enumerator.Current;
        if (previous is null) return false;

        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            if (current is null) return false;

            if (previous.CompareTo(current) >= 0) // 当前元素 <= 前一个元素
                return false;

            previous = current;
        }

        return true;
    }
}