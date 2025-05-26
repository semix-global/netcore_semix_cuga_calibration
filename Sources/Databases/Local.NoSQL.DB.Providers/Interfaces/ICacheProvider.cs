namespace Local.NoSQL.DB.Providers.Interfaces;

public interface ICacheProvider : IDisposable
{
    T? Get<T>() where T : class, ICacheItem, new();

    T GetOrDefault<T>() where T : class, ICacheItem, new();

    bool TryGetOrDefault<T>(out T item) where T : class, ICacheItem, new();

    (bool IsSuccess, T Item) TryGetOrDefault<T>() where T : class, ICacheItem, new();

    bool Set<T>(T item, CancellationToken cancellationToken) where T : class, ICacheItem, new();

    T[]? GetArray<T>() where T : class, ICacheItem, new();

    T[] GetOrDefaultArray<T>() where T : class, ICacheItem, new();

    bool TryGetOrDefaultArray<T>(out T[] items) where T : class, ICacheItem, new();

    (bool IsSuccess, T[] Items) TryGetOrDefaultArray<T>() where T : class, ICacheItem, new();

    bool SetArray<T>(T[] items, CancellationToken cancellationToken) where T : class, ICacheItem, new();
}