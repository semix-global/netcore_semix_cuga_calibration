using System.Collections.ObjectModel;

namespace Net.Utilities.Extensions;

public static class CollectionExtensions
{
    public static Collection<T> AddRange<T>(this Collection<T> collection, IEnumerable<T> items)
    {
        foreach (var each in items)
        {
            collection.Add(each);
        }

        return collection;
    }
}