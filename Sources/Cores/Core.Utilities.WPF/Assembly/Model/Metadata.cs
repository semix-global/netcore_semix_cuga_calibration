using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Utilities.WPF.Assembly.Model;

/// <summary>
/// 元数据模型
/// </summary>
public partial class Metadata : ObservableObject, ICloneable<Metadata>
{
    /// <summary>
    /// 生成时间
    /// </summary>
    [ObservableProperty]
    public partial string GeneratedAt { get; set; } = string.Empty;

    /// <summary>
    /// 项目名称
    /// </summary>
    [ObservableProperty]
    public partial string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// 配置模式
    /// </summary>
    [ObservableProperty]
    public partial string Configuration { get; set; } = string.Empty;

    /// <summary>
    /// 目标框架
    /// </summary>
    [ObservableProperty]
    public partial string Framework { get; set; } = string.Empty;

    /// <summary>
    /// 目标运行时
    /// </summary>
    [ObservableProperty]
    public partial string Runtime { get; set; } = string.Empty;

    /// <summary>
    /// 总文件数
    /// </summary>
    [ObservableProperty]
    public partial int TotalFiles { get; set; }

    /// <summary>
    /// 程序集总数
    /// </summary>
    [ObservableProperty]
    public partial int AssemblyCount { get; set; }

    /// <summary>
    /// DLL文件数
    /// </summary>
    [ObservableProperty]
    public partial int DLLCount { get; set; }

    /// <summary>
    /// 可执行文件数
    /// </summary>
    [ObservableProperty]
    public partial int ExecutableCount { get; set; }

    /// <summary>
    ///  App 版本
    /// </summary>
    [ObservableProperty]
    public partial string Version { get; set; } = string.Empty;

    /// <summary>
    /// Assembly Metadata: NeutralResourcesLanguage
    /// </summary>
    [ObservableProperty]
    public partial string NeutralResourcesLanguage { get; set; } = string.Empty;

    /// <summary>
    /// Assembly Metadata: InformationalVersion
    /// </summary>
    [ObservableProperty]
    public partial string InformationalVersion { get; set; } = string.Empty;

    /// <summary>
    ///  编译器符号
    /// </summary>
    [ObservableProperty]
    public partial string DefineConstants { get; set; } = string.Empty;

    public Metadata Clone() => new()
    {
        GeneratedAt = GeneratedAt,
        ProjectName = ProjectName,
        Configuration = Configuration,
        Framework = Framework,
        Runtime = Runtime,
        TotalFiles = TotalFiles,
        AssemblyCount = AssemblyCount,
        DLLCount = DLLCount,
        ExecutableCount = ExecutableCount,
        Version = Version,
        NeutralResourcesLanguage = NeutralResourcesLanguage,
        InformationalVersion = InformationalVersion,
        DefineConstants = DefineConstants
    };
}