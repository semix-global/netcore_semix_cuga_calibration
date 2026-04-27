using Local.SQL.Cache.Providers.Bases;

namespace Core.Models.Models.Common.Version;

public sealed class CalibrationVersionDTO : ObservableCacheBase
{
    /// <summary>
    /// 校准结果文件路径
    /// </summary>
    public required string ResultFilePath { get; set; } = string.Empty;

    /// <summary>
    /// 所有校准DTO版本信息
    /// </summary>
    public IReadOnlyList<VersionInfo> CalibrationVersionInfos { get; set; } = [];

    public VersionInfo? GetVersionInfo(Type type)
    {
        return CalibrationVersionInfos.SingleOrDefault(t => t.GetRuntimeType() == type);
    }

    public sealed class VersionInfo
    {
        public long Id { get; set; }

        public string Version { get; set; }

        public string TypeFullName { get; set; } = string.Empty;

        public Type? GetRuntimeType() => Type.GetType(TypeFullName);
    }
}