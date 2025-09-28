using Net.Utilities.Models;

namespace Core.Utilities;

public static class TypeExtension
{
    public static string ToAssemblyName(this Type type, bool isFullName = true, bool isAssemblyName = true, bool isVersion = true, bool isCulture = true, bool isPublicKeyToken = true)
    {
        var assemblyQualifiedName = type.AssemblyQualifiedName;

        var strs = GuardUtils.IsNotNullAndReturn(assemblyQualifiedName).Split(',');
        var resultStrs = new List<string>();

        if (isFullName)
            resultStrs.Add(strs[0]);
        if (isAssemblyName)
            resultStrs.Add(strs[1]);
        if (isVersion)
            resultStrs.Add(strs[2]);
        if (isCulture)
            resultStrs.Add(strs[3]);
        if (isPublicKeyToken)
            resultStrs.Add(strs[4]);
        var resultStr = string.Join(",", [.. resultStrs]);
        return strs.Length < 1 ? string.Empty : resultStr;

    }
}