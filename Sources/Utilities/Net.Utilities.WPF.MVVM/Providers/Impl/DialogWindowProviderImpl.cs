using HandyControl.Controls;
using HandyControl.Data;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Extensions;
using Net.Utilities.Helper.Enum;
using Net.Utilities.Helper.IOC.Providers;
using Net.Utilities.Models;
using Net.Utilities.WPF.Behaviors;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels;
using Ookii.Dialogs.Wpf;
using System.Media;
using Colors = System.Windows.Media.Colors;

namespace Net.Utilities.WPF.MVVM.Providers.Impl;

[IOCAppService(ServiceType = typeof(IDialogWindowProvider), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class DialogWindowProviderImpl(
    IWindowManagerService windowManagerService,
    string applicationName,
    ISynchronizationContextProvider contextProvider) : IDialogWindowProvider
{
    public bool? TryShowDialog(string title, string message, out DialogResultEnum dialogResultEnum, DialogButtonsEnum dialogButtonsEnum = DialogButtonsEnum.OK, DialogIconEnum dialogIconEnum = DialogIconEnum.Information)
    {
        var dialogWindowViewModel = ShowDialogViewModel(title, message, dialogButtonsEnum, dialogIconEnum);

        var showDialog = windowManagerService.ShowDialog(dialogWindowViewModel, null, false);
        dialogResultEnum = dialogWindowViewModel.DialogResultEnum;

        return showDialog;
    }

    public bool? TryShowDialog(string message, out DialogResultEnum dialogResultEnum, DialogButtonsEnum dialogButtonsEnum = DialogButtonsEnum.OK, DialogIconEnum dialogIconEnum = DialogIconEnum.Information)
    {
        return TryShowDialog(string.Empty, message, out dialogResultEnum, dialogButtonsEnum, dialogIconEnum);
    }

    public void ShowDialog(string title, string message, DialogButtonsEnum dialogButtonsEnum = DialogButtonsEnum.OK, DialogIconEnum dialogIconEnum = DialogIconEnum.Information)
    {
        TryShowDialog(title, message, out _, dialogButtonsEnum, dialogIconEnum);
    }

    public void ShowDialog(string message, DialogButtonsEnum dialogButtonsEnum = DialogButtonsEnum.OK, DialogIconEnum dialogIconEnum = DialogIconEnum.Information)
    {
        TryShowDialog(string.Empty, message, out _, dialogButtonsEnum, dialogIconEnum);
    }

    public void Show(string title, string message, DialogButtonsEnum dialogButtonsEnum = DialogButtonsEnum.OK, DialogIconEnum dialogIconEnum = DialogIconEnum.Information)
    {
        var dialogWindowViewModel = ShowDialogViewModel(title, message, dialogButtonsEnum, dialogIconEnum);

        windowManagerService.ShowWindow(dialogWindowViewModel);
    }

    public void Show(string message, DialogButtonsEnum dialogButtonsEnum = DialogButtonsEnum.OK, DialogIconEnum dialogIconEnum = DialogIconEnum.Information)
    {
        var dialogWindowViewModel = ShowDialogViewModel(string.Empty, message, dialogButtonsEnum, dialogIconEnum);

        windowManagerService.ShowWindow(dialogWindowViewModel);
    }

    public void ShowNotification(string message, DialogIconEnum dialogIconEnum = DialogIconEnum.Information, int waitSeconds = 3)
    {
        switch (dialogIconEnum)
        {
            case DialogIconEnum.Error:
                Growl.ErrorGlobal(new GrowlInfo { Message = message, WaitTime = waitSeconds });
                break;

            case DialogIconEnum.Question:
                Growl.AskGlobal(new GrowlInfo { Message = message, WaitTime = waitSeconds });
                break;

            case DialogIconEnum.Warning:
                Growl.WarningGlobal(new GrowlInfo { Message = message, WaitTime = waitSeconds });
                break;

            case DialogIconEnum.Information:
                Growl.InfoGlobal(new GrowlInfo { Message = message, WaitTime = waitSeconds });
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(dialogIconEnum), dialogIconEnum, null);
        }
    }

    private DialogWindowViewModel ShowDialogViewModel(string title, string message, DialogButtonsEnum dialogButtonsEnum, DialogIconEnum dialogIconEnum)
    {
        var dialogWindowViewModel = new DialogWindowViewModel
        {
            DialogTitle = string.IsNullOrWhiteSpace(title) ? applicationName : $"{applicationName} {title}",
            DialogMessage = message,
            DialogButtonList = [.. EnumHelper.GetFlagsEnums((DialogResultEnum)dialogButtonsEnum)]
        };

        switch (dialogIconEnum)
        {
            default:
            case DialogIconEnum.None:
            case DialogIconEnum.Information:
                dialogWindowViewModel.DialogTitleColor = Colors.Green.ToString();
                SystemSounds.Asterisk.Play();
                break;

            case DialogIconEnum.Question:
                dialogWindowViewModel.DialogTitleColor = Colors.Orange.ToString();
                SystemSounds.Question.Play();
                break;

            case DialogIconEnum.Warning:
                dialogWindowViewModel.DialogTitleColor = Colors.Yellow.ToString();
                SystemSounds.Exclamation.Play();
                break;

            case DialogIconEnum.Error:
                dialogWindowViewModel.DialogTitleColor = Colors.Red.ToString();
                SystemSounds.Hand.Play();
                break;
        }

        return dialogWindowViewModel;
    }

    public void ShowImage(string filePath)
    {
        var viewerWindowViewModel = new ImageViewerWindowViewModel([(filePath, "")]);
        windowManagerService.ShowWindow(viewerWindowViewModel);
    }

    public void ShowImage(List<(string path, string title)> filePath)
    {
        var viewerWindowViewModel = new ImageViewerWindowViewModel(filePath);
        windowManagerService.ShowWindow(viewerWindowViewModel);
    }

    public void ShowPlot(double[] plots, string? title = null)
    {
        ShowPlot([.. plots.Select((t, i) => new Point(i + 1, t))], title);
    }

    public void ShowPlot(Point[] plots, string? title = null)
    {
        ShowPlot([(string.IsNullOrWhiteSpace(title) ? $"Plot: {plots.Length} Points" : title!, plots)]);
    }

    public void ShowPlot(List<double[]> plots)
    {
        ShowPlot([.. plots.Select(t => t.Select((tt, i) => new Point(i + 1, tt)).ToArray())]);
    }

    public void ShowPlot(List<Point[]> plots)
    {
        ShowPlot([.. plots.Select((t, i) => (Title: $"Plot {i + 1}: {t.Length} Points", t))]);
    }

    public void ShowPlot(List<(string Title, double[] Points)> plots)
    {
        ShowPlot([.. plots.Select(t => (t.Title, t.Points.ToPoints()))]);
    }

    public void ShowPlot(List<(string Title, Point[] Points)> plots)
    {
        var plotWindowViewModel = new PlotWindowViewModel
        {
            PlotList = [.. plots.Select(t => new WpfPlotModel(t.Title, t.Points))]
        };

        windowManagerService.ShowWindow(plotWindowViewModel);
    }

    public bool? TryShowSelectFilePathDialog(string filter, out string filePath)
    {
        filePath = string.Empty;

        // ReSharper disable LocalizableElement
        var openFileDialog = new VistaOpenFileDialog // 创建 VistaOpenFileDialog 对象
        {
            Filter = $"files (*{filter})|*{filter}", // 设置文件类型过滤器
            DefaultExt = $"{filter}", // 设置默认文件类型
            Title = "Select a file" // 设置对话框标题
        };
        bool? result = null;
        contextProvider.Send(() => result = openFileDialog.ShowDialog());

        if (result == true) filePath = openFileDialog.FileName;

        return result;
    }

    public bool? TryShowSelectDirectoryPathDialog(out string directoryPath)
    {
        directoryPath = string.Empty;
        var dialog = new VistaFolderBrowserDialog
        {
            Description = "Please select directory",
            UseDescriptionForTitle = true // 使用 Description 作为窗口标题
        };

        // 打开文件夹选择对话框
        if (dialog.ShowDialog() != true) return false;

        directoryPath = dialog.SelectedPath;

        return true;
    }

    public bool? TryShowSaveFilePathDialog(string filter, out string filePath)
    {
        filePath = string.Empty;

        var saveFileDialog = new VistaSaveFileDialog
        {
            Title = "Save a file",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            OverwritePrompt = true,
            AddExtension = true, // 文档名添加扩展名
            DefaultExt = $"{filter}",
            Filter = $"files (*{filter})|*{filter}",
            FileName = Guid.NewGuid().ToString()
        };

        bool? result = null;
        contextProvider.Send(() => result = saveFileDialog.ShowDialog());

        if (result == true) filePath = saveFileDialog.FileName;

        return result;
    }
}