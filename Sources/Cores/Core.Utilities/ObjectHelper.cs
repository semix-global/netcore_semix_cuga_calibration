using Net.Utilities.Models;
using System.Reflection;

namespace Core.Utilities;

public static class ObjectHelper
{
    private const BindingFlags AllBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static object? GetFieldValue(object obj, string fieldName)
    {
        return GuardUtils.IsNotNullAndReturn(obj.GetType().GetField(fieldName, AllBindingFlags)).GetValue(obj);
    }

    public static T? GetFieldValueOrDefault<T>(object obj, string fieldName)
    {
        var value = GetFieldValue(obj, fieldName);

        return value is null ? default : GuardUtils.IsAssignableToType<T>(value);
    }

    public static T GetFieldValue<T>(object obj, string fieldName)
    {
        return GuardUtils.IsNotNullAndAssignableToType<T>(GetFieldValue(obj, fieldName));
    }

    public static void SetFieldValue(object obj, string fieldName, object? value)
    {
        GuardUtils.IsNotNullAndReturn(obj.GetType().GetField(fieldName, AllBindingFlags)).SetValue(obj, value);
    }

    public static object? GetPropertyValue(object obj, string propertyName)
    {
        return GuardUtils.IsNotNullAndReturn(obj.GetType().GetProperty(propertyName, AllBindingFlags)).GetValue(obj);
    }

    public static T? GetPropertyValueOrDefault<T>(object obj, string propertyName)
    {
        var value = GetPropertyValue(obj, propertyName);

        return value is null ? default : GuardUtils.IsAssignableToType<T>(value);
    }

    public static T GetPropertyValue<T>(object obj, string propertyName)
    {
        return GuardUtils.IsNotNullAndAssignableToType<T>(GetPropertyValue(obj, propertyName));
    }

    public static void SetPropertyValue(object obj, string propertyName, object? value)
    {
        GuardUtils.IsNotNullAndReturn(obj.GetType().GetProperty(propertyName, AllBindingFlags)).SetValue(obj, value);
    }

    public static object? InvokeMethod(object obj, string methodName, params object[] args)
    {
        return GuardUtils.IsNotNullAndReturn(obj.GetType().GetMethod(methodName, AllBindingFlags)).Invoke(obj, args);
    }
}