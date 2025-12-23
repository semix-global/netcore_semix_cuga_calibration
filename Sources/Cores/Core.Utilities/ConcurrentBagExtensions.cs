using System.Collections.Concurrent;

namespace Core.Utilities;

public static class ConcurrentBagExtensions
{
    public static TValue? Get<TKey, TValue>(
        this ConcurrentBag<KeyValuePair<TKey, TValue>> list,
        TKey key)
        where TKey : notnull
    {
        return list.TryGetSingle((Func<KeyValuePair<TKey, TValue>, bool>)(t => Equals(t.Key, key)), out var keyValuePair)
            ? keyValuePair.Value
            : default;
    }
}