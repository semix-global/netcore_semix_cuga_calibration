using System.Collections.Concurrent;

namespace Core.Utilities;

public static class ConcurrentBagExtensions
{
    public static TValue? Get<TKey, TValue>(
        this ConcurrentBag<KeyValuePair<TKey, TValue>> list,
        TKey key)
        where TKey : notnull
    {
        KeyValuePair<TKey, TValue> keyValuePair;
        return list.TryGetSingle<KeyValuePair<TKey, TValue>>((Func<KeyValuePair<TKey, TValue>, bool>)(t => object.Equals((object)t.Key, (object)(TKey)key)), out keyValuePair)
            ? keyValuePair.Value
            : default;
    }
}