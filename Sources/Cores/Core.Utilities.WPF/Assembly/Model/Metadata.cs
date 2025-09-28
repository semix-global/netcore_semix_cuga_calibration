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
    private string _generatedAt = string.Empty;

    /// <summary>
    /// 项目名称
    /// </summary>
    [ObservableProperty]
    private string _projectName = string.Empty;

    /// <summary>
    /// 配置模式
    /// </summary>
    [ObservableProperty]
    private string _configuration = string.Empty;

    /// <summary>
    /// 目标框架
    /// </summary>
    [ObservableProperty]
    private string _framework = string.Empty;

    /// <summary>
    /// 目标运行时
    /// </summary>
    [ObservableProperty]
    private string _runtime = string.Empty;

    /// <summary>
    /// 总文件数
    /// </summary>
    [ObservableProperty]
    private int _totalFiles;

    /// <summary>
    /// 程序集总数
    /// </summary>
    [ObservableProperty]
    private int _assemblyCount;

    /// <summary>
    /// DLL文件数
    /// </summary>
    [ObservableProperty]
    private int _dLLCount;

    /// <summary>
    /// 可执行文件数
    /// </summary>
    [ObservableProperty]
    private int _executableCount;

    /// <summary>
    ///  App 版本
    /// </summary>
    [ObservableProperty]
    private string _version = string.Empty;

    /// <summary>
    /// Assembly Metadata: NeutralResourcesLanguage
    /// </summary>
    [ObservableProperty]
    private string _neutralResourcesLanguage = string.Empty;

    /// <summary>
    /// Assembly Metadata: InformationalVersion
    /// </summary>
    [ObservableProperty]
    private string _informationalVersion = string.Empty;

    /// <summary>
    ///  编译器符号
    /// </summary>
    [ObservableProperty]
    private string _defineConstants = string.Empty;

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