using System.Collections;
using CommunityToolkit.Diagnostics;

namespace Core.Utilities;

public static class ObjectHelper1
{
    /// <summary>
    /// 将对象转换为数组
    /// </summary>
    /// <param name="values">对象</param>
    /// <param name="conversionArrayType">数组类型</param>
    /// <returns>数组</returns>
    public static Array ConvertToArray(object values, Type conversionArrayType)
    {
        if (values is not IEnumerable enumerable) return ThrowHelper.ThrowArrayTypeMismatchException<Array>("Object is not an IEnumerable!");

        var array = enumerable as object[] ?? enumerable.Cast<object>().ToArray();

        var results = Array.CreateInstance(conversionArrayType, array.Length);

        for (var i = 0; i < array.Length; i++)
        {
            var convertedValue = Convert.ChangeType(array.GetValue(i), conversionArrayType);

            results.SetValue(convertedValue, i);
        }

        return results;
    }
}