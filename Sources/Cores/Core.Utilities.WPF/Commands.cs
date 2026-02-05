using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.IO;

namespace Core.Utilities.WPF;

public static class Commands
{
    public static IRelayCommand<string> OpenFilePathCommand { get; } =
        new RelayCommand<string>(static filePath =>
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            if (File.Exists(filePath) == false) return;

            Guard.IsNotNull(filePath);

            filePath = filePath.Replace("/", "\\");
            // Clipboard.SetText(filePath);

            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        });
}