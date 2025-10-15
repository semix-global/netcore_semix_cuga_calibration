using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Core.Utilities.WPF.ApplicationAbout.Helper;

public static class Platform
{
    private static Regex? _runtimeVersionRegex;

    //
    // 摘要:
    //     Check whether current operating system is Windows of not.
    public static bool IsWindows { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    //
    // 摘要:
    //     Check whether the current operating system is Windows of not.
    public static string WindowsVersion { get; } = RuntimeInformation.OSDescription;

    //
    // 摘要:
    //     Get the latest version of .NET Runtime installed on a device.
    //
    // 参数:
    //   stableVersionOnly:
    //     True to include a stable version only.
    //
    // 返回结果:
    //     Installed .NET Runtime version, or null if .NET Runtime is not installed on device.
    public static Version? GetInstalledRuntimeVersion(bool stableVersionOnly = true)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                Arguments = "--list-runtimes",
                CreateNoWindow = true,
                FileName = "dotnet",
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            if (process != null)
            {
                Version? version = null;
                var text = (IsWindows ? "Microsoft.WindowsDesktop.App" : "Microsoft.NETCore.App");
                var text2 = process.StandardOutput.ReadLine();
                _runtimeVersionRegex ??= new Regex(@"^(?<Runtime>[^\s]+)[\s]+(?<Version>[\d]+(\.[\d]+)*)(?<VersionPostfix>\-.+)?");

                while (text2 != null)
                {
                    try
                    {
                        var match = _runtimeVersionRegex.Match(text2);
                        if (match.Success && match.Groups["Runtime"].Value == text && !(match.Groups["VersionPostfix"].Success && stableVersionOnly) && Version.TryParse(match.Groups["Version"].Value, out Version result) && (version == null || version < result))
                        {
                            version = result;
                        }
                    }
                    finally
                    {
                        text2 = process.StandardOutput.ReadLine();
                    }
                }

                return version;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }
}