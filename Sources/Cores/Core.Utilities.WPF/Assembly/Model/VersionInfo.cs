using Net.Utilities.Mapper.Interfaces;
using System.Text.Json.Serialization;

namespace Core.Utilities.WPF.Assembly.Model;

/// <summary>
/// 版本信息输出模型
/// </summary>
public class VersionInfo : ICloneable<VersionInfo>
{
    /// <summary>
    /// 元数据信息
    /// </summary>
    [JsonPropertyOrder(0)]
    public Metadata Metadata { get; set; } = new();

    /// <summary>
    /// 程序集信息列表
    /// </summary>
    [JsonPropertyOrder(1)]
    public List<AssemblyInfo> Assemblies { get; set; } = [];

    public VersionInfo Clone() => new()
    {
        Metadata = Metadata.Clone(),
        Assemblies = [.. Assemblies.Select(t => t.Clone())]
    };
}