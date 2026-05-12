using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;

namespace Core.Utilities.WPF;

public static class Commands
{
    public static IRelayCommand<string> OpenFilePathCommand { get; } = new RelayCommand<string>(static filePath =>
    {
        var dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();

        try
        {
            if (string.IsNullOrWhiteSpace(filePath) || File.Exists(filePath) == false)
            {
                dialogWindowProvider.ShowDialog($"{filePath}: File does not exist.", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return;
            }

            filePath = Path.GetFullPath(filePath);
            filePath = filePath.Replace("/", "\\");

            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });

            try
            {
                Clipboard.SetText(filePath);
            }
            catch (Exception)
            {
                // ignored
            }
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Open File Path Failed!
                                             {ex}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    });

    public static IRelayCommand<string> OpenDirectoryCommand { get; } = new RelayCommand<string>(static directoryPath =>
    {
        var dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();

        try
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || Directory.Exists(directoryPath) == false)
            {
                dialogWindowProvider.ShowDialog($"{directoryPath}: Directory does not exist.", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return;
            }

            directoryPath = Path.GetFullPath(directoryPath);
            directoryPath = directoryPath.Replace("/", "\\");

            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = directoryPath,
                UseShellExecute = true
            });

            try
            {
                Clipboard.SetText(directoryPath);
            }
            catch (Exception)
            {
                // ignored
            }
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Open Directory Failed!
                                             {ex}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    });

    public static IRelayCommand<string> OpenDirectoryAndSelectCommand { get; } = new RelayCommand<string>(static directoryPath =>
    {
        var dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();

        try
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || Directory.Exists(directoryPath) == false)
            {
                dialogWindowProvider.ShowDialog($"{directoryPath}: Directory does not exist.", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return;
            }

            directoryPath = Path.GetFullPath(directoryPath);
            directoryPath = directoryPath.Replace("/", "\\");

            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = directoryPath,
                Arguments = $"""
                             /select,"{directoryPath}"
                             """,
                UseShellExecute = true
            });

            try
            {
                Clipboard.SetText(directoryPath);
            }
            catch (Exception)
            {
                // ignored
            }
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Open Directory And Select Failed!
                                             {ex}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    });

    public static IRelayCommand<string> OpenDirectoryAndSelectFileCommand { get; } = new RelayCommand<string>(static filePath =>
    {
        var dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();

        try
        {
            if (string.IsNullOrWhiteSpace(filePath) || File.Exists(filePath) == false)
            {
                dialogWindowProvider.ShowDialog($"{filePath}: File does not exist.", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return;
            }

            filePath = Path.GetFullPath(filePath);
            filePath = filePath.Replace("/", "\\");

            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"""
                             /select,"{filePath}"
                             """,
                UseShellExecute = true
            });

            try
            {
                Clipboard.SetText(filePath);
            }
            catch (Exception)
            {
                // ignored
            }
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Open Directory And Select File Failed!
                                             {ex}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    });

    public static IRelayCommand<string> SaveFileAsCommand { get; } = new RelayCommand<string>(static filePath =>
    {
        var dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();

        try
        {
            if (string.IsNullOrWhiteSpace(filePath) || File.Exists(filePath) == false)
            {
                dialogWindowProvider.ShowDialog($"{filePath}: File does not exist.", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return;
            }

            filePath = Path.GetFullPath(filePath);
            filePath = filePath.Replace("/", "\\");

            if (dialogWindowProvider.TryShowSaveFilePathDialog(Path.GetExtension(filePath), out var saveFilePath) == false)
            {
                dialogWindowProvider.ShowDialog("Failed to get save file path.", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return;
            }

            File.Copy(filePath, saveFilePath, overwrite: true);

            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"""
                             /select,"{saveFilePath}"
                             """,
                UseShellExecute = true
            });

            try
            {
                Clipboard.SetText(filePath);
            }
            catch (Exception)
            {
                // ignored
            }
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Save File As Failed!
                                             {ex}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    });

    public static IRelayCommand<string> SaveDirectoryAsCommand { get; } = new RelayCommand<string>(static directoryPath =>
    {
        var dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();

        try
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || Directory.Exists(directoryPath) == false)
            {
                dialogWindowProvider.ShowDialog($"{directoryPath}: Directory does not exist.", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return;
            }

            directoryPath = Path.GetFullPath(directoryPath);
            directoryPath = directoryPath.Replace("/", "\\");

            if (dialogWindowProvider.TryShowSelectDirectoryPathDialog(out var saveDirectoryPath) == false)
            {
                dialogWindowProvider.ShowDialog("Failed to get save file path.", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return;
            }

            saveDirectoryPath = Path.Combine(saveDirectoryPath, DateTime.Now.ToString(Constants.LongFileDateTimeFormat));
            
            DirectoryHelper.CopyDirectory(directoryPath, saveDirectoryPath);

            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = saveDirectoryPath,
                UseShellExecute = true
            });

            try
            {
                Clipboard.SetText(directoryPath);
            }
            catch (Exception)
            {
                // ignored
            }
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Save Directory As Failed!
                                             {ex}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    });
}