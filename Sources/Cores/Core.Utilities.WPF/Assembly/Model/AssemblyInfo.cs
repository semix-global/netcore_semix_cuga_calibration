using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Utilities.WPF.Assembly.Model;

/// <summary>
/// 程序集信息模型
/// </summary>
public partial class AssemblyInfo : ObservableObject, ICloneable<AssemblyInfo>
{
    /// <summary>
    /// 文件名
    /// </summary>
    [ObservableProperty]
    public partial string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件完整路径
    /// </summary>
    [ObservableProperty]
    public partial string FullPath { get; set; } = string.Empty;

    /// <summary>
    /// 程序集版本
    /// </summary>
    [ObservableProperty]
    public partial string AssemblyVersion { get; set; } = string.Empty;

    /// <summary>
    /// 产品版本
    /// </summary>
    [ObservableProperty]
    public partial string ProductVersion { get; set; } = string.Empty;

    /// <summary>
    /// 公司名称
    /// </summary>
    [ObservableProperty]
    public partial string Company { get; set; } = string.Empty;

    /// <summary>
    /// 文件描述
    /// </summary>
    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [ObservableProperty]
    public partial long SizeBytes { get; set; }

    /// <summary>
    /// 文件大小（KB）
    /// </summary>
    [ObservableProperty]
    public partial double SizeKB { get; set; }

    /// <summary>
    /// 文件大小（MB）
    /// </summary>
    [ObservableProperty]
    public partial double SizeMB { get; set; }

    /// <summary>
    /// 最后修改时间
    /// </summary>
    [ObservableProperty]
    public partial string LastModified { get; set; } = string.Empty;

    /// <summary>
    /// 最后修改时间（UTC）
    /// </summary>
    [ObservableProperty]
    public partial string LastModifiedUTC { get; set; } = string.Empty;

    public AssemblyInfo Clone() => new()
    {
        FileName = FileName,
        FullPath = FullPath,
        AssemblyVersion = AssemblyVersion,
        ProductVersion = ProductVersion,
        Company = Company,
        Description = Description,
        SizeBytes = SizeBytes,
        SizeKB = SizeKB,
        SizeMB = SizeMB,
        LastModified = LastModified,
        LastModifiedUTC = LastModifiedUTC
    };
}