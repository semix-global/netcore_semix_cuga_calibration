namespace Local.NoSQL.DB.Providers.Interfaces;

public interface ICacheSetting
{
    /// <summary>
    /// 缓存最大存档天数
    /// </summary>
    int CacheMaxArchiveDays { get; init; }
}