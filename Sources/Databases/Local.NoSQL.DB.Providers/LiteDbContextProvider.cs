using LiteDB;
using Local.NoSQL.DB.Providers.Implements;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Helpers.Helpers.Files;
using SourceGenerator.InjectHostDI;
using Yitter.IdGenerator;

namespace Local.NoSQL.DB.Providers;

public static class LiteDbContextProvider
{
    public static IServiceCollection AddNoSqlDbContext(
        this IServiceCollection services,
        Func<IServiceProvider, string> nosqlDbDataSourceProvider,
        Func<IServiceProvider, ICacheSetting> cacheSettingProvider,
        IHostEnvironment hostEnvironment)
    {
        services.AddSingleton<ILiteDatabaseProvider>(sp =>
        {
            var nosqlDbDataSource = nosqlDbDataSourceProvider(sp);

            if (YitIdHelper.IdGenInstance is null)
                YitIdHelper.SetIdGenerator(new IdGeneratorOptions
                {
                    WorkerId = 1,
                    WorkerIdBitLength = 6,
                    SeqBitLength = 6
                });

            BsonMapper.Global.EmptyStringToNull = false;
            BsonMapper.Global.SerializeNullValues = true;
            BsonMapper.Global.EnumAsInteger = true;
            BsonMapper.Global.IncludeFields = true;
            // 原生Datetime Truncate了 Truncate DateTime in milliseconds
            var logger = sp.GetRequiredService<ILogger<LiteDbCacheProviderImpl>>();

            DirectoryHelper.CreateFileDirectoryIfNotExists(nosqlDbDataSource);
            var liteDatabase = new LiteDatabase(new ConnectionString(nosqlDbDataSource) { Connection = ConnectionType.Direct, Collation = new Collation("en-US/IgnoreCase") });

            // 将未提交的-log文件写入主数据库
            liteDatabase.Checkpoint();

            return new LiteDatabaseProviderImpl(logger)
            {
                LiteDatabase = liteDatabase
            };
        });

        services.AddSingleton<ICacheProvider>(sp =>
        {
            var cacheSetting = cacheSettingProvider(sp);

            var logger = sp.GetRequiredService<ILogger<LiteDbCacheProviderImpl>>();

            var liteDbProvider = sp.GetRequiredService<ILiteDatabaseProvider>();
            return new LiteDbCacheProviderImpl(liteDbProvider, cacheSetting, logger);
        });

        services.AddLocalNoSQLDBProvidersInjectHostDI(hostEnvironment);

        return services;
    }

    public static IServiceCollection AddKeyedNoSqlDbContext(
        this IServiceCollection services,
        string serviceKey,
        Func<IServiceProvider, ICacheSetting> cacheSettingProvider,
        IHostEnvironment hostEnvironment)
    {
        services.AddKeyedSingleton<ILiteDatabaseProvider>(serviceKey, (sp, _) =>
        {
            if (YitIdHelper.IdGenInstance is null)
                YitIdHelper.SetIdGenerator(new IdGeneratorOptions
                {
                    WorkerId = 1,
                    WorkerIdBitLength = 6,
                    SeqBitLength = 6
                });

            BsonMapper.Global.EmptyStringToNull = false;
            BsonMapper.Global.SerializeNullValues = true;
            BsonMapper.Global.EnumAsInteger = true;
            BsonMapper.Global.IncludeFields = true;
            var logger = sp.GetRequiredService<ILogger<LiteDbCacheProviderImpl>>();

            return new LiteDatabaseProviderImpl(logger);
        });

        services.AddKeyedSingleton<ICacheProvider>(serviceKey, (sp, _) =>
        {
            var cacheSetting = cacheSettingProvider(sp);

            var logger = sp.GetRequiredService<ILogger<LiteDbCacheProviderImpl>>();

            var liteDbProvider = sp.GetRequiredKeyedService<ILiteDatabaseProvider>(serviceKey);

            return new LiteDbCacheProviderImpl(liteDbProvider, cacheSetting, logger);
        });

        return services;
    }
}