using System.Reflection;
using System.Runtime.CompilerServices;

namespace Net.Utilities.Helper.Object;

public static class ObjectHelper
{
    /// <summary>
    /// 将属性值设置到对象中
    /// </summary>
    /// <param name="target">对象</param>
    /// <param name="properties">属性值</param>
    /// <returns>是否成功</returns>
    public static bool ApplyProperties(object target, IEnumerable<KeyValuePair<string, object?>>? properties)
    {
        if (properties is null) return false;

        var type = target.GetType();

        foreach (var pair in properties)
        {
            type.GetProperty(pair.Key)?.SetValue(target, pair.Value);
        }

        return true;
    }

    /// <summary>
    /// 将源对象的属性复制到目标对象中
    /// </summary>
    /// <param name="source">源对象</param>
    /// <param name="target">目标对象</param>
    public static void CopyProperties(object source, object target)
    {
        var sourceType = source.GetType();
        var targetType = target.GetType();

        foreach (var sourceProperty in sourceType.GetProperties())
        {
            if (sourceProperty.CanRead == false) continue;

            var targetProperty = targetType.GetProperty(sourceProperty.Name);

            if (targetProperty is null || targetProperty.CanWrite == false) continue;

            var value = GetPropertyValue(source, sourceProperty.Name);
            SetPropertyValue(target, targetProperty.Name, value);
        }
    }

    /// <summary>
    /// 获取对象的属性值
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="propertyName">属性</param>
    /// <returns>属性值</returns>
    public static object? GetPropertyValue(object obj, string propertyName)
    {
        return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
    }

    /// <summary>
    /// 设置对象的对象值
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="propertyName">属性</param>
    /// <param name="value">对象值</param>
    public static void SetPropertyValue(object obj, string propertyName, object? value)
    {
        obj.GetType().GetProperty(propertyName)?.SetValue(obj, value);
    }

    /// <summary>
    /// 是否匿名类型
    /// </summary>
    /// <param name="type">匿名类型</param>
    /// <returns>是否匿名类型</returns>
    public static bool CheckIfAnonymousType(Type type)
    {
        // HACK: The only way to detect anonymous types right now.
        return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
               && type.IsGenericType && type.Name.Contains("AnonymousType")
               && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
               && type.Attributes.HasFlag(TypeAttributes.NotPublic);
    }
}