using CommunityToolkit.Mvvm.ComponentModel;
using Core.Utilities.WPF.Assembly.Model;

namespace Core.Utilities.WPF.ApplicationAbout.Model;

public partial class ApplicationInfo : ObservableObject
{
    [ObservableProperty]
    public partial VersionInfo VersionInfo { get; set; } = new();

    [ObservableProperty]
    public partial string OutPutPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FrameworkVersion { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RuntimeVersion { get; set; } = string.Empty;

    /// <summary>
    /// Version of .NET currently used by application.
    /// </summary>
    public static Version CLRVersion => Environment.Version;
}