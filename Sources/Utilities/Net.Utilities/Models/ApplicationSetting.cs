namespace Net.Utilities.Models;

public sealed record ApplicationSetting
{
    public const string AppSetting = nameof(ApplicationSetting);

    /// <summary>
    /// 应用名称
    /// </summary>
    public string AppName { get; init; } = string.Empty;

    /// <summary>
    /// 软件的主路径
    /// </summary>
    public string AppHomeDirectory { get; init; } = string.Empty;

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
    /// 配方LiteDB数据库链接字符串
    /// </summary>
    public string NosqlRecipeDbDataSource { get; init; } = string.Empty;

    /// <summary>
    /// 缓存最大存档天数
    /// </summary>
    public int CacheMaxArchiveDays { get; init; }

    /// <summary>
    /// 校准菜单名称
    /// </summary>
    public string CalibrationMenuName { get; init; } = string.Empty;

    /// <summary>
    /// 标题栏菜单名称
    /// </summary>
    public string TitleMenuName { get; init; } = string.Empty;
}