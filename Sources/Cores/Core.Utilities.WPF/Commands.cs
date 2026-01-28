using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Core.Utilities.WPF;

public static class Commands
{
    public static IRelayCommand<string> OpenFilePathCommand { get; } =
        new RelayCommand<string>(filePath =>
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            if (File.Exists(filePath) == false) return;

            Clipboard.SetText(filePath);

            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        });
}