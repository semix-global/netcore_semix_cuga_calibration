using LiteDB;
using Local.NoSQL.DB.Providers.Helper;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Yitter.IdGenerator;

namespace Local.NoSQL.DB.Providers.Implements;

[IOCAppService(ServiceType = typeof(ICacheProvider), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class LiteDbCacheProviderImpl(
    ILiteDatabaseProvider databaseProvider,
    IOptions<ApplicationSetting> options,
    ILogger<LiteDbCacheProviderImpl> logger) : ICacheProvider
{
    /// <summary>
    /// 保留最近的10条数据
    /// </summary>
    private const int RemoveExpirationCacheKeepCount = 10;

    /// <summary>
    /// 限制同时只能有一个线程访问
    /// </summary>
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// 使用LiteDb的次数
    /// </summary>
    private int _useLiteDbCount;

    public T? Get<T>() where T : class, ICacheItem, new()
    {
        var name = typeof(T).FullName;
        var collectionName = LiteDbHelper.RemoveInvalidFileName(name);

        try
        {
            Interlocked.Add(ref _useLiteDbCount, 1);
            var liteDatabase = databaseProvider.GetLiteDatabase() ?? throw new ArgumentNullException("Modify lite database failed!");
            var liteCollection = liteDatabase.GetCollection<T>(collectionName, BsonAutoId.Int64);

            var result = liteCollection
                .Query()
                .Where(t => t.IsDeleted == false)
                .OrderByDescending(t => t.Id)
                .FirstOrDefault();

            return result;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "{@Name}: Get cache failed", name);

            return null;
        }
        finally
        {
            Interlocked.Add(ref _useLiteDbCount, -1);
        }
    }

    public T GetOrDefault<T>() where T : class, ICacheItem, new()
    {
        var result = Get<T>();
        if (result is not null) return result;

        result = new T();

        return result;
    }

    public bool TryGetOrDefault<T>(out T obj) where T : class, ICacheItem, new()
    {
        var result = Get<T>();
        if (result is null)
        {
            obj = new T();

            return false;
        }

        obj = result;

        return true;
    }

    public (bool IsSuccess, T Item) TryGetOrDefault<T>() where T : class, ICacheItem, new()
    {
        var result = Get<T>();
        if (result is not null) return (true, result);

        result = new T();

        return (false, result);
    }

    public bool Set<T>(T item, CancellationToken cancellationToken) where T : class, ICacheItem, new()
    {
        var name = typeof(T).FullName;
        var collectionName = LiteDbHelper.RemoveInvalidFileName(name);

        try
        {
            var isRelease = false;
            var liteDatabase = databaseProvider.GetLiteDatabase() ?? throw new ArgumentNullException("Modify lite database failed!");
            try
            {
                Interlocked.Add(ref _useLiteDbCount, 1);
                isRelease = _semaphore.Wait(TimeSpan.FromSeconds(5), cancellationToken);

                liteDatabase.BeginTrans();

                var liteCollection = liteDatabase.GetCollection<T>(collectionName, BsonAutoId.Int64);

                if (item.Id == 0)
                {
                    item.Id = YitIdHelper.NextId();
                }

                item.Expiration = DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(options.Value.CacheMaxArchiveDays)).ToUnixTimeSeconds();
                item.CreatedTime = DateTime.Now;

                var result = liteCollection.Update([item]) == 1 || liteCollection.Insert([item]) == 1;

                result = result && RemoveExpirationCache(liteCollection);

                if (result)
                {
                    liteDatabase.Commit();
                    return true;
                }

                logger.LogCritical("{@Name}: Set cache failed, so Roll back", name);
                liteDatabase.Rollback();

                return false;
            }
            catch (Exception)
            {
                liteDatabase.Rollback();

                throw;
            }
            finally
            {
                if (isRelease) _semaphore.Release();
                Interlocked.Add(ref _useLiteDbCount, -1);
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "{@Name}: Set cache failed", name);

            return false;
        }
    }

    public T[]? GetArray<T>() where T : class, ICacheItem, new()
    {
        var name = typeof(T).FullName;
        var collectionName = LiteDbHelper.RemoveInvalidFileName(name);
        var liteDatabase = databaseProvider.GetLiteDatabase() ?? throw new ArgumentNullException("Modify lite database failed!");

        try
        {
            Interlocked.Add(ref _useLiteDbCount, 1);

            var liteCollection = liteDatabase.GetCollection<T>(collectionName, BsonAutoId.Int64);
            var liteCollectionArray = liteDatabase.GetCollection<IdsCache>($"{collectionName}_{nameof(IdsCache)}", BsonAutoId.Int64);

            var idResult = liteCollectionArray
                .Query()
                .Where(t => t.IsDeleted == false)
                .OrderByDescending(t => t.Id)
                .FirstOrDefault();

            var result = idResult is not null
                ? liteCollection
                    .Query()
                    .Where(t => t.IsDeleted == false)
                    .Where(Query.In("_id", idResult.Ids.Select(t => new BsonValue(t))))
                    .OrderBy(t => t.Id)
                    .ToArray()
                : null;

            return result;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "{@Name}: Get cache failed", name);

            return null;
        }
        finally
        {
            Interlocked.Add(ref _useLiteDbCount, -1);
        }
    }

    public T[] GetOrDefaultArray<T>() where T : class, ICacheItem, new()
    {
        var result = GetArray<T>();
        if (result is not null) return result;

        result = [];

        return result;
    }

    public bool TryGetOrDefaultArray<T>(out T[] items) where T : class, ICacheItem, new()
    {
        var result = GetArray<T>();
        if (result is null)
        {
            items = [];

            return false;
        }

        items = result;

        return true;
    }

    public (bool IsSuccess, T[] Items) TryGetOrDefaultArray<T>() where T : class, ICacheItem, new()
    {
        var result = GetArray<T>();
        if (result is not null) return (true, result);

        result = [];

        return (false, result);
    }

    public bool SetArray<T>(T[] items, CancellationToken cancellationToken) where T : class, ICacheItem, new()
    {
        var name = typeof(T).FullName;
        var collectionName = LiteDbHelper.RemoveInvalidFileName(name);

        try
        {
            var isRelease = false;
            var liteDatabase = databaseProvider.GetLiteDatabase() ?? throw new ArgumentNullException("Get lite database failed!");
            try
            {
                Interlocked.Add(ref _useLiteDbCount, 1);
                isRelease = _semaphore.Wait(TimeSpan.FromSeconds(5), cancellationToken);

                liteDatabase.BeginTrans();

                var liteCollection = liteDatabase.GetCollection<T>(collectionName, BsonAutoId.Int64);
                var liteCollectionArray = liteDatabase.GetCollection<IdsCache>($"{collectionName}_{nameof(IdsCache)}", BsonAutoId.Int64);

                var unixTimeSeconds = DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(options.Value.CacheMaxArchiveDays)).ToUnixTimeSeconds();

                var resultList = new List<bool>();

                foreach (var item in items)
                {
                    if (item.Id == 0) item.Id = YitIdHelper.NextId();
                    item.Expiration = unixTimeSeconds;
                    item.CreatedTime = DateTime.Now;
                    item.IsDeleted = false;

                    resultList.Add(liteCollection.Update([item]) == 1 || liteCollection.Insert([item]) == 1);
                }

                var idListCacheItem = new IdsCache
                {
                    Id = YitIdHelper.NextId(),
                    Expiration = unixTimeSeconds,
                    Ids = [.. items.Select(t => t.Id)],
                    CreatedTime = DateTime.Now,
                    IsDeleted = false
                };

                resultList.Add(liteCollectionArray.Insert([idListCacheItem]) == 1);

                resultList.Add(RemoveExpirationCache(liteCollection));
                resultList.Add(RemoveExpirationCache(liteCollectionArray));

                if (resultList.All(b => b))
                {
                    liteDatabase.Commit();
                    return true;
                }

                logger.LogCritical("{@Name}: Set cache failed, so Roll back", name);
                liteDatabase.Rollback();

                return false;
            }
            catch (Exception)
            {
                liteDatabase.Rollback();

                throw;
            }
            finally
            {
                if (isRelease) _semaphore.Release();
                Interlocked.Add(ref _useLiteDbCount, -1);
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "{@Name}: Set cache failed", name);

            return false;
        }
    }

    public void Dispose()
    {
        var liteDatabase = databaseProvider.GetLiteDatabase() ?? throw new ArgumentNullException("Get lite database failed!");

        // 将未提交的-log文件写入主数据库
        liteDatabase.Checkpoint();

        // 将已经删除的内存页从数据库中清除, 释放空间
        //var rebuildOptions = new RebuildOptions { Collation = new Collation("en-US/IgnoreCase") };
        //liteDatabase.Rebuild(rebuildOptions);
        //if (rebuildOptions.GetErrorReport().Any()) logger.LogCritical("Rebuild error: {@ErrorReport}", string.Join(Environment.NewLine, rebuildOptions.GetErrorReport().Select(t => t.ToString())));

        liteDatabase.Dispose();
        //_semaphore.Dispose();
    }

    private static bool RemoveExpirationCache<T>(ILiteCollection<T> liteCollection) where T : class, ICacheItem, new()
    {
        return true;
        var totalCount = liteCollection
            .Query()
            .Where(t => t.IsDeleted == false)
            .Where(c => c.Expiration < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            .Count();

        var deleteCount = totalCount - RemoveExpirationCacheKeepCount;
        if (deleteCount <= 0) return true;

        var toDelete = liteCollection
            .Query()
            .Where(t => t.IsDeleted == false)
            .Where(c => c.Expiration < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            .OrderBy(t => t.Id)
            .Limit(deleteCount) // 限制最多删除的数量
            .ToList();

        toDelete.AddRange(liteCollection
            .Query()
            .Where(t => t.IsDeleted)
            .Where(c => c.Expiration < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            .ToList()); // 将已删除的数据超过缓存最大的时间也加入删除列表

        var resultList = toDelete.Select(item => liteCollection.Delete(item.Id)).ToList();

        return resultList.All(b => b);
    }
}

internal sealed class IdsCache : ICacheItem
{
    public long Id { get; set; }

    public long Expiration { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.Now;

    public bool IsDeleted { get; set; }

    public long[] Ids { get; init; } = [];
}