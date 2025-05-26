using CommunityToolkit.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Local.NoSQL.DB.Providers.Helper;

public static class LiteDbHelper
{
    public static string RemoveInvalidFileName([NotNull] string? name)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));

        const string pattern = @"[^a-zA-Z$_]";

        // 将所有不符合规则的字符替换为下划线
        return Regex.Replace(name, pattern, "_");
    }
}