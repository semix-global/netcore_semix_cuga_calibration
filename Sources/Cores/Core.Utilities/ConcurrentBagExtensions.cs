using CommunityToolkit.Diagnostics;
using System.Collections.Concurrent;

namespace Core.Utilities;

public static class ConcurrentBagExtensions
{
    extension<TKey, TValue>(ConcurrentBag<KeyValuePair<TKey, TValue>> @this) where TKey : notnull
    {
        public TValue Get(TKey key)
        {
            return @this.TryGetSingle((Func<KeyValuePair<TKey, TValue>, bool>)(t => Equals(t.Key, key)), out var keyValuePair)
                ? keyValuePair.Value
                : ThrowHelper.ThrowArgumentNullException<TValue>(nameof(key));
        }
    }
}