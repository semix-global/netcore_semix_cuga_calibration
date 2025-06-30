using LiteDB;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Helpers.Helpers.Files;

namespace Local.NoSQL.DB.Providers.Implements;

public class LiteDatabaseProviderImpl(ILogger<LiteDbCacheProviderImpl> logger) : ILiteDatabaseProvider
{
    public LiteDatabase LiteDatabase { get; set; }

    public bool ModifyLiteDatabase(string liteDbSource)
    {
        try
        {
            Dispose();

            DirectoryHelper.CreateFileDirectoryIfNotExists(liteDbSource);

            LiteDatabase = new LiteDatabase(new ConnectionString(liteDbSource) { Connection = ConnectionType.Direct, Collation = new Collation("en-US/IgnoreCase") });

            // 将未提交的-log文件写入主数据库
            LiteDatabase.Checkpoint();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Set lite database failed");
            return false;
        }
    }

    public LiteDatabase? GetLiteDatabase()
    {
        return LiteDatabase;
    }

    public void Dispose()
    {
        if (LiteDatabase is null)
            return;
        // 将未提交的-log文件写入主数据库
        LiteDatabase.Checkpoint();

        LiteDatabase.Dispose();
    }
}