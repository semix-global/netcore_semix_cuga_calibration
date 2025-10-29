using CommunityToolkit.Diagnostics;
using Net.Utilities.Models;

// ReSharper disable once CheckNamespace
namespace System.Linq;

public static class EnumerableExtensions
{
#if NETFRAMEWORK
    /*public static IEnumerable<(int Index, TSource Item)> Index<TSource>(this IEnumerable<TSource> source)
    {
        var index = 0;
        foreach (var item in source)
        {
            yield return (index, item);
            index++;
        }
    }*/

    public static TSource? MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        => MaxBy(source, keySelector, null);

    public static TSource? MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
    {
        comparer ??= Comparer<TKey>.Default;

        using var e = source.GetEnumerator();

        if (e.MoveNext() == false)
        {
            if (default(TSource) is null) return default;

            return ThrowHelper.ThrowInvalidOperationException<TSource?>("Sequence contains no elements");
        }

        var value = e.Current;
        var key = keySelector(value);

        if (default(TKey) is null)
        {
            if (key is null)
            {
                var firstValue = value;

                do
                {
                    if (e.MoveNext() == false)
                    {
                        return firstValue;
                    }

                    value = e.Current;
                    key = keySelector(value);
                } while (key is null);
            }

            while (e.MoveNext())
            {
                var nextValue = e.Current;
                var nextKey = keySelector(nextValue);
                if (nextKey is not null && comparer.Compare(nextKey, key) > 0)
                {
                    key = nextKey;
                    value = nextValue;
                }
            }
        }
        else
        {
            while (e.MoveNext())
            {
                var nextValue = e.Current;
                var nextKey = keySelector(nextValue);
                if (comparer.Compare(nextKey, key) > 0)
                {
                    key = nextKey;
                    value = nextValue;
                }
            }
        }

        return value;
    }

    public static TSource? MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) => MinBy(source, keySelector, null);

    public static TSource? MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
    {
        comparer ??= Comparer<TKey>.Default;

        using var e = source.GetEnumerator();

        if (e.MoveNext() == false)
        {
            if (default(TSource) is null)
            {
                return default;
            }

            return ThrowHelper.ThrowInvalidOperationException<TSource?>("Sequence contains no elements");
        }

        var value = e.Current;
        var key = keySelector(value);

        if (default(TKey) is null)
        {
            if (key is null)
            {
                var firstValue = value;

                do
                {
                    if (e.MoveNext() == false)
                    {
                        return firstValue;
                    }

                    value = e.Current;
                    key = keySelector(value);
                } while (key is null);
            }

            while (e.MoveNext())
            {
                var nextValue = e.Current;
                var nextKey = keySelector(nextValue);
                if (nextKey is not null && comparer.Compare(nextKey, key) < 0)
                {
                    key = nextKey;
                    value = nextValue;
                }
            }
        }
        else
        {
            while (e.MoveNext())
            {
                var nextValue = e.Current;
                var nextKey = keySelector(nextValue);
                if (comparer.Compare(nextKey, key) < 0)
                {
                    key = nextKey;
                    value = nextValue;
                }
            }
        }

        return value;
    }

    public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
    {
        var single = source.TryGetSingle(predicate, out var found);

        return found ? GuardUtils.IsNotNullAndReturn(single) : defaultValue;
    }

    private static TSource? TryGetSingle<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out bool found)
    {
        using var e = source.GetEnumerator();

        while (e.MoveNext())
        {
            var result = e.Current;
            if (predicate(result))
            {
                while (e.MoveNext())
                {
                    if (predicate(e.Current))
                    {
                        found = false;

                        return ThrowHelper.ThrowInvalidOperationException<TSource?>("Sequence contains more than one matching element");
                    }
                }

                found = true;

                return result;
            }
        }

        found = false;

        return default;
    }

    public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
    {
        var single = source.TryGetSingle(out var found);

        return found ? GuardUtils.IsNotNullAndReturn(single) : defaultValue;
    }

    private static TSource? TryGetSingle<TSource>(this IEnumerable<TSource> source, out bool found)
    {
        if (source is IList<TSource> list)
        {
            switch (list.Count)
            {
                case 0:
                    found = false;

                    return default;

                case 1:
                    found = true;

                    return list[0];
            }
        }
        else
        {
            using var e = source.GetEnumerator();
            if (e.MoveNext() == false)
            {
                found = false;

                return default;
            }

            var result = e.Current;
            if (e.MoveNext() == false)
            {
                found = true;

                return result;
            }
        }

        found = false;

        return ThrowHelper.ThrowInvalidOperationException<TSource?>("Sequence contains more than one matching element");
    }

#endif
}