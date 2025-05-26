using CommunityToolkit.Diagnostics;
using System.Collections.Concurrent;

namespace Net.Utilities.Mapper;

public static class CustomerAdaptToMapper
{
    private static readonly ConcurrentDictionary<(Type AdaptInType, Type AdaptToType), Func<object, object>> CustomMapper = [];

    public static void RegisterType<TIn, TOut>(Func<TIn, TOut> func1, Func<TOut, TIn> func2)
    {
        CustomMapper[(typeof(TIn), typeof(TOut))] = obj =>
        {
            Guard.IsNotNull(obj, nameof(obj));
            var @out = func1((TIn)obj);
            Guard.IsNotNull(@out, nameof(@out));

            return @out;
        };

        CustomMapper[(typeof(TOut), typeof(TIn))] = obj =>
        {
            Guard.IsNotNull(obj, nameof(obj));
            var @out = func2((TOut)obj);
            Guard.IsNotNull(@out, nameof(@out));

            return @out;
        };
    }

    public static TOut Mapper<TIn, TOut>(TIn obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        if (CustomMapper.TryGetValue((typeof(TIn), typeof(TOut)), out var func))
        {
            return (TOut)func(obj);
        }

        return ThrowHelper.ThrowNotSupportedException<TOut>($"No adapter found for {typeof(TIn).FullName} => {typeof(TOut).FullName}");
    }
}