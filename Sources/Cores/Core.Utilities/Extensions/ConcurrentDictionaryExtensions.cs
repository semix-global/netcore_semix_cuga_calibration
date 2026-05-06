using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Net.Utilities.Helpers.Extensions;

public static class ConcurrentDictionaryExtensions
{
    public static TValue Get<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
    {
        return dictionary[key];
    }

    public static bool TryGet<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dictionary,
        TKey key,
        [NotNullWhen(true)] out TValue? value)
        where TKey : notnull
    {
        return dictionary.TryGetValue(key, out value);
    }
}