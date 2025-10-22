namespace Core.Utilities;

public static class EnumerableExtensions
{
    public static IEnumerable<(int Index, TSource Item)> Index<TSource>(this IEnumerable<TSource> source)
    {
        var index = -1;
        foreach (var element in source)
        {
            checked
            {
                index++;
            }

            yield return (index, element);
        }
    }
}