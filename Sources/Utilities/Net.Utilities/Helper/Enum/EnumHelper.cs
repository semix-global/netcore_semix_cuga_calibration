using Net.Utilities.Attributes;
using System.ComponentModel;
using System.Reflection;

namespace Net.Utilities.Helper.Enum;

public static class EnumHelper
{
    /// <summary>
    /// 获取 enum list
    /// </summary>
    /// <typeparam name="TEnum">enum类</typeparam>
    /// <returns>enum list</returns>
    public static TEnum[] Enums<TEnum>() where TEnum : struct, System.Enum
    {
        return [.. System.Enum.GetValues(typeof(TEnum)).Cast<TEnum>()];
    }

    /// <summary>
    /// 获取 enum flag list
    /// </summary>
    /// <typeparam name="TEnum">enum类</typeparam>
    /// <returns>flag enum list</returns>
    public static TEnum[] GetFlagsEnums<TEnum>(TEnum combinedFlags) where TEnum : struct, System.Enum, IConvertible
    {
        if (Attribute.IsDefined(typeof(TEnum), typeof(FlagsAttribute)) == false) throw new ArgumentException("The type must be an enum with the Flags attribute.");

        return [.. Enums<TEnum>().Where(value => (Convert.ToInt32(combinedFlags) & Convert.ToInt32(value)) != 0)];
    }

    /// <summary>
    /// private enum PublishStatusValue
    /// {
    ///     [Description("Not Completed")]
    ///     NotCompleted,
    /// };
    /// 不包含DescriptionAttribute特性，返回默认ToString， 否则拿到DescriptionAttribute描述
    /// </summary>
    /// <param name="enumValue">枚举值</param>
    /// <param name="catchDescription">是否抓取DescriptionAttribute</param>
    /// <returns>枚举值描述</returns>
    public static string ToDescriptionString(object enumValue, bool catchDescription = true)
    {
#if NET
        if (catchDescription == false) return enumValue.ToString() ?? string.Empty;
#else
        if (catchDescription == false) return enumValue.ToString();
#endif

#if NET
        var fi = enumValue.GetType().GetField(enumValue.ToString() ?? string.Empty);
#else
        var fi = enumValue.GetType().GetField(enumValue.ToString());
#endif

#if NET
        if (fi is null) return enumValue.ToString() ?? string.Empty;
#else
        if (fi is null) return enumValue.ToString();
#endif

        var attribute = fi.GetCustomAttribute<DescriptionAttribute>(false);

#if NET
        return attribute?.Description ?? enumValue.ToString() ?? string.Empty;
#else
        return attribute?.Description ?? enumValue.ToString();
#endif
    }

    /// <summary>
    /// private enum PublishStatusValue
    /// {
    ///     [Description("Not Completed")]
    ///     NotCompleted,
    /// };
    /// 不包含DescriptionAttribute特性，返回默认ToString， 否则拿到DescriptionAttribute描述
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="enumValue">枚举值</param>
    /// <param name="catchDescription">是否抓取DescriptionAttribute</param>
    /// <returns>枚举值描述</returns>
    public static string ToDescriptionString<TEnum>(TEnum enumValue, bool catchDescription = true) where TEnum : struct, System.Enum
    {
        return ToDescriptionString((object)enumValue, catchDescription);
    }

    /// <summary>
    /// 获取Enum的资源值
    /// </summary>
    /// <param name="enumValue">枚举值</param>
    /// <returns>资源值</returns>
    public static string ToLocalizationResourceValue(object enumValue)
    {
#if NET
        var fi = enumValue.GetType().GetField(enumValue.ToString() ?? string.Empty);
#else
        var fi = enumValue.GetType().GetField(enumValue.ToString());
#endif

#if NET
        if (fi is null) return enumValue.ToString() ?? string.Empty;
#else
        if (fi is null) return enumValue.ToString();
#endif

        var attr = fi.GetCustomAttribute<LocalizationAttribute>(false);

#if NET
        if (attr is null) return enumValue.ToString() ?? string.Empty;
#else
        if (attr is null) return enumValue.ToString();
#endif

        return attr.ResourceType.GetProperty(attr.ResourceName, BindingFlags.Static | BindingFlags.Public)?.GetValue(null) as string
               ?? attr.ResourceType.GetProperty(attr.ResourceName, BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as string
               ?? string.Empty;
    }

    /// <summary>
    /// 获取Enum的资源值
    /// </summary>
    /// <param name="enumValue">枚举值</param>
    /// <returns>资源值</returns>
    public static string ToLocalizationResourceValue<TEnum>(TEnum enumValue) where TEnum : struct, System.Enum
    {
        return ToLocalizationResourceValue((object)enumValue);
    }
}