using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Utilities.WPF.ApplicationAbout.Model;

public partial class OperatingSystemInfo : ObservableObject
{
    /// <summary>
    /// 设备名称
    /// </summary>
    public static string DeviceName => Environment.MachineName;

    /// <summary>
    /// 操作系统
    /// </summary>
    [ObservableProperty]
    public partial string OperatingSystem { get; set; } = string.Empty;

    /// <summary>
    /// 操作系统版本
    /// </summary>
    [ObservableProperty]
    public partial string OperatingSystemVersion { get; set; } = string.Empty;

    /// <summary>
    /// 物理内存
    /// </summary>
    [ObservableProperty]
    public partial string TotalPhysicalMemory { get; set; } = string.Empty;

    /// <summary>
    /// 处理器个数
    /// </summary>
    public static int ProcessorCount => Environment.ProcessorCount;
}