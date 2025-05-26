using CommunityToolkit.Diagnostics;
using Net.Utilities.Constants;

namespace Net.Utilities.Algorithm.MathNet.Modules;

public static class BinarySearch
{
    /// <summary>
    /// 递增且不能重复序列中利用二分查找法寻找最接近值的左右index
    /// </summary>
    /// <param name="arr">递增且不能重复序列</param>
    /// <param name="target">目标值</param>
    /// <param name="lowerIndex">左边index</param>
    /// <param name="upperIndex">右边index</param>
    /// <returns>是否成功</returns>
    public static bool TryValueIndexRange(int[] arr, int target, out int lowerIndex, out int upperIndex)
    {
        lowerIndex = -1;
        upperIndex = -1;

        // 检查是否递增(不检查严格递增)
        for (var i = 1; i < arr.Length; i++)
        {
            if (arr[i] < arr[i - 1])
            {
                ThrowHelper.ThrowArgumentException("The array is increasing.");
            }
        }

        var left = 0;
        var right = arr.Length - 1;

        while (left <= right)
        {
            var mid = left + (right - left) / 2;

            if (Math.Abs(arr[mid] - target) < ConstantHelper.Tolerance) // 认为是相等
            {
                if (arr[mid] >= target)
                {
                    if (mid + 1 > arr.Length - 1)
                    {
                        if (arr[mid] - target != 0)
                        {
                            return false;
                        }

                        lowerIndex = mid - 1; // 如果完全相等, 且是最后一个索引, 向下靠拢 包含[mid-1, mid] 之间
                        upperIndex = mid;
                    }
                    else // 如果完全相等,, 且是非最后一个索引, 向上靠拢 包含[mid, mid + 1] 之间
                    {
                        lowerIndex = mid;
                        upperIndex = mid + 1;
                    }
                }
                else
                {
                    if (mid - 1 < 0) return false;
                    lowerIndex = mid - 1;
                    upperIndex = mid;
                }

                return true;
            }

            if (arr[mid] < target)
            {
                left = mid + 1;
                lowerIndex = mid;
            }
            else
            {
                right = mid - 1;
                upperIndex = mid;
            }
        }

        return lowerIndex != -1 && upperIndex != -1;
    }

    /// <summary>
    /// 递增且不能重复序列中利用二分查找法寻找最接近值的左右index
    /// </summary>
    /// <param name="arr">递增且不能重复序列</param>
    /// <param name="target">目标值</param>
    /// <param name="lowerIndex">左边index</param>
    /// <param name="upperIndex">右边index</param>
    /// <returns>是否成功</returns>
    public static bool TryValueIndexRange(float[] arr, float target, out int lowerIndex, out int upperIndex)
    {
        lowerIndex = -1;
        upperIndex = -1;

        // 检查是否递增(不检查严格递增)
        for (var i = 1; i < arr.Length; i++)
        {
            if (arr[i] < arr[i - 1])
            {
                ThrowHelper.ThrowArgumentException("The array is increasing.");
            }
        }

        var left = 0;
        var right = arr.Length - 1;

        while (left <= right)
        {
            var mid = left + (right - left) / 2;

            if (Math.Abs(arr[mid] - target) < ConstantHelper.Tolerance) // 认为是相等
            {
                if (arr[mid] >= target)
                {
                    if (mid + 1 > arr.Length - 1)
                    {
                        if (arr[mid] - target != 0)
                        {
                            return false;
                        }

                        lowerIndex = mid - 1; // 如果完全相等, 且是最后一个索引, 向下靠拢 包含[mid-1, mid] 之间
                        upperIndex = mid;
                    }
                    else // 如果完全相等,, 且是非最后一个索引, 向上靠拢 包含[mid, mid + 1] 之间
                    {
                        lowerIndex = mid;
                        upperIndex = mid + 1;
                    }
                }
                else
                {
                    if (mid - 1 < 0) return false;
                    lowerIndex = mid - 1;
                    upperIndex = mid;
                }

                return true;
            }

            if (arr[mid] < target)
            {
                left = mid + 1;
                lowerIndex = mid;
            }
            else
            {
                right = mid - 1;
                upperIndex = mid;
            }
        }

        return lowerIndex != -1 && upperIndex != -1;
    }

    /// <summary>
    /// 递增且不能重复序列中利用二分查找法寻找最接近值的左右index
    /// </summary>
    /// <param name="arr">递增且不能重复序列</param>
    /// <param name="target">目标值</param>
    /// <param name="lowerIndex">左边index</param>
    /// <param name="upperIndex">右边index</param>
    /// <returns>是否成功</returns>
    public static bool TryValueIndexRange(double[] arr, double target, out int lowerIndex, out int upperIndex)
    {
        lowerIndex = -1;
        upperIndex = -1;

        // 检查是否递增(不检查严格递增)
        for (var i = 1; i < arr.Length; i++)
        {
            if (arr[i] < arr[i - 1])
            {
                ThrowHelper.ThrowArgumentException("The array is increasing.");
            }
        }

        var left = 0;
        var right = arr.Length - 1;

        while (left <= right)
        {
            var mid = left + (right - left) / 2;

            if (Math.Abs(arr[mid] - target) < ConstantHelper.Tolerance) // 认为是相等
            {
                if (arr[mid] >= target)
                {
                    if (mid + 1 > arr.Length - 1)
                    {
                        if (arr[mid] - target != 0)
                        {
                            return false;
                        }

                        lowerIndex = mid - 1; // 如果完全相等, 且是最后一个索引, 向下靠拢 包含[mid-1, mid] 之间
                        upperIndex = mid;
                    }
                    else // 如果完全相等,, 且是非最后一个索引, 向上靠拢 包含[mid, mid + 1] 之间
                    {
                        lowerIndex = mid;
                        upperIndex = mid + 1;
                    }
                }
                else
                {
                    if (mid - 1 < 0) return false;
                    lowerIndex = mid - 1;
                    upperIndex = mid;
                }

                return true;
            }

            if (arr[mid] < target)
            {
                left = mid + 1;
                lowerIndex = mid;
            }
            else
            {
                right = mid - 1;
                upperIndex = mid;
            }
        }

        return lowerIndex != -1 && upperIndex != -1;
    }
}