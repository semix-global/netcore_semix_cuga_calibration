using CommunityToolkit.Mvvm.ComponentModel;
using Core.Utilities.WPF.Assembly.Model;

namespace Core.Utilities.WPF.ApplicationAbout.Model;

public partial class ApplicationInfo : ObservableObject
{
    [ObservableProperty]
    private VersionInfo _versionInfo = new();

    [ObservableProperty]
    private string _outPutPath = string.Empty;

    [ObservableProperty]
    private string _frameworkVersion = string.Empty;

    [ObservableProperty]
    private string _runtimeVersion = string.Empty;

    /// <summary>
    /// Version of .NET currently used by application.
    /// </summary>
    public static Version CLRVersion => Environment.Version;
}