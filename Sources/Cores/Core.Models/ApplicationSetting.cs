using Local.SQL.Cache.Providers.Services.Interfaces;
using Net.Utilities.Models;

namespace Core.Models;

public sealed record ApplicationSetting : BaseApplicationSetting, ICacheConfiguration
{
    #region DB

    public string DBDataSource { get; init; } = string.Empty;

    #endregion

    #region Cache

    public string CacheDataSource { get; init; } = string.Empty;

    public string RecipeCacheDirectory { get; init; } = string.Empty;

    public int CacheMaxArchiveDays { get; init; }

    public int RemoveExpirationCacheKeepCount { get; init; }

    #endregion

    #region 菜单

    public string CalibrationMenuName { get; init; } = string.Empty;

    public string TitleMenuName { get; init; } = string.Empty;

    public string ManagementMenuName { get; init; } = string.Empty;

    #endregion

    #region 路径

    public string DocumentDirectory { get; init; } = string.Empty;

    public string SimulatorDirectory { get; init; } = string.Empty;

    public string AlgorithmLogDirectory { get; init; } = string.Empty;

    #endregion

    public int AlgorithmLogLevel { get; init; } = 2;
}