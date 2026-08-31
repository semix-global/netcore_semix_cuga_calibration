using Local.SQL.Cache.Providers.Services.Interfaces;
using Net.Utilities.Models;

namespace Core.Models;

public sealed record ApplicationSetting : BaseApplicationSetting, ICacheConfiguration
{
    /// <summary>
    /// 软件脚本的主路径
    /// </summary>
    public string ScriptDirectory { get; init; } = string.Empty;

    /// <summary>
    /// Sqlite数据库链接字符串
    /// </summary>
    public string SqlDbDataSource { get; init; } = string.Empty;

    /// <summary>
    /// LiteDB数据库链接字符串
    /// </summary>
    public string NosqlDbDataSource { get; init; } = string.Empty;

    /// <summary>
    /// 配方LiteDB数据库主路径
    /// </summary>
    public string NosqlDbDataSourceDirectory { get; init; } = string.Empty;

    /// <summary>
    /// 默认配方名称
    /// </summary>
    public string DefaultRecipeName { get; init; } = string.Empty;

    /// <summary>
    /// 配方数据库名称
    /// </summary>
    public string RecipeDBName { get; init; } = string.Empty;

    /// <summary>
    /// 缓存最大存档天数
    /// </summary>
    public int CacheMaxArchiveDays { get; init; }

    /// <summary>
    /// 移除过期缓存时，保留的缓存数量
    /// </summary>
    public int RemoveExpirationCacheKeepCount { get; init; }

    /// <summary>
    /// 校准菜单名称
    /// </summary>
    public string CalibrationMenuName { get; init; } = string.Empty;

    /// <summary>
    /// 标题栏菜单名称
    /// </summary>
    public string TitleMenuName { get; init; } = string.Empty;

    /// <summary>
    /// 校准软件更新说明文档名称
    /// </summary>
    public string UpdateDocumentPath { get; init; } = string.Empty;

    /// <summary>
    /// 文档根目录路径
    /// </summary>
    public string DocumentDirectory { get; init; } = string.Empty;
    
    /// <summary>
    /// 算法日志根目录路径
    /// </summary>
    public string AlgorithmLogDirectory { get; init; } = string.Empty;
}