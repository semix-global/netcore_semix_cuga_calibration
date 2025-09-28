using CommunityToolkit.Diagnostics;

namespace Core.Utilities;

public static class ObjectHelper
{
    public static Array ObjectToArray(Type targetArrayType, object objectArray)
    {
        if (objectArray is not Array array)
            return ThrowHelper.ThrowArrayTypeMismatchException<Array>("Object is not an array!");

        // 创建目标类型的数组
        var typedArray = Array.CreateInstance(targetArrayType, array.Length);

        for (int i = 0; i < array.Length; i++)
        {
            // 把 objectArray[i] 转成目标元素类型再放进去
            var convertedValue = Convert.ChangeType(array.GetValue(i), targetArrayType);
            typedArray.SetValue(convertedValue, i);
        }

        return typedArray;
    }
}