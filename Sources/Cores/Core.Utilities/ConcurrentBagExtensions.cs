using CommunityToolkit.Diagnostics;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

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

        public bool TryGet(TKey key, [NotNullWhen(true)] out TValue? value)
        {
            if (@this.TryGetSingle((Func<KeyValuePair<TKey, TValue>, bool>)(t => Equals(t.Key, key)), out var keyValuePair))
            {
                Guard.IsNotNull(keyValuePair.Value);
                value = keyValuePair.Value;

                return true;
            }

            value = default;

            return false;
        }
    }
}