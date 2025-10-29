using System.Collections.Concurrent;

namespace Core.Utilities;

public static class ConcurrentBagExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(this ConcurrentBag<KeyValuePair<TKey, TValue>> list, TKey key, TValue value)
        where TKey : notnull
    {
        var item = list.SingleOrDefault(t => Equals(t.Key, key));
        if (Equals(item.Key, key)) return item.Value;

        list.Add(new KeyValuePair<TKey, TValue>(key, value));

        return value;
    }
}