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
    private string _fileName = string.Empty;

    /// <summary>
    /// 文件完整路径
    /// </summary>
    [ObservableProperty]
    private string _fullPath = string.Empty;

    /// <summary>
    /// 程序集版本
    /// </summary>
    [ObservableProperty]
    private string _assemblyVersion = string.Empty;

    /// <summary>
    /// 产品版本
    /// </summary>
    [ObservableProperty]
    private string _productVersion = string.Empty;

    /// <summary>
    /// 公司名称
    /// </summary>
    [ObservableProperty]
    private string _company = string.Empty;

    /// <summary>
    /// 文件描述
    /// </summary>
    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [ObservableProperty]
    private long _sizeBytes;

    /// <summary>
    /// 文件大小（KB）
    /// </summary>
    [ObservableProperty]
    private double _sizeKB;

    /// <summary>
    /// 文件大小（MB）
    /// </summary>
    [ObservableProperty]
    private double _sizeMB;

    /// <summary>
    /// 最后修改时间
    /// </summary>
    [ObservableProperty]
    private string _lastModified = string.Empty;

    /// <summary>
    /// 最后修改时间（UTC）
    /// </summary>
    [ObservableProperty]
    private string _lastModifiedUTC = string.Empty;

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