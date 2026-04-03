using CommunityToolkit.Diagnostics;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models;
using System.Windows;
using System.Windows.Input;

namespace CugaCalibration.ViewModels.Flourier;

public static class DropCommandAttached
{
    #region 附加属性

    public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
        "Command",
        typeof(ICommand),
        typeof(DropCommandAttached),
        new PropertyMetadata(OnCommandChanged)
    );

    public static readonly DependencyProperty IsAllowMultipleFilesProperty = DependencyProperty.RegisterAttached(
        "IsAllowMultipleFiles",
        typeof(bool),
        typeof(DropCommandAttached),
        new PropertyMetadata(false)
    );

    public static readonly DependencyProperty AllowedExtensionsProperty = DependencyProperty.RegisterAttached(
        "AllowedExtensions",
        typeof(string),
        typeof(DropCommandAttached),
        new PropertyMetadata(Constants.AnyFileExtensionsFilter));

    public static ICommand? GetCommand(DependencyObject target) => (ICommand?)target.GetValue(CommandProperty);

    public static void SetCommand(DependencyObject target, ICommand value) => target.SetValue(CommandProperty, value);

    public static bool GetIsAllowMultipleFiles(DependencyObject target) => (bool)target.GetValue(IsAllowMultipleFilesProperty);

    public static void SetIsAllowMultipleFiles(DependencyObject target, bool value) => target.SetValue(IsAllowMultipleFilesProperty, value);

    public static string GetAllowedExtensions(DependencyObject obj) => (string)obj.GetValue(AllowedExtensionsProperty);

    public static void SetAllowedExtensions(DependencyObject obj, string value) => obj.SetValue(AllowedExtensionsProperty, value);

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement frameworkElement) return;

        switch (e)
        {
            case { NewValue: ICommand, OldValue: null }:
                frameworkElement.AllowDrop = true;

                frameworkElement.PreviewDragOver -= OnPreviewDragOver;
                frameworkElement.PreviewDragOver += OnPreviewDragOver;
                frameworkElement.Drop -= OnDrop;
                frameworkElement.Drop += OnDrop;

                break;

            case { NewValue: null }:
                frameworkElement.AllowDrop = false;

                frameworkElement.PreviewDragOver -= OnPreviewDragOver;
                frameworkElement.Drop -= OnDrop;

                break;
        }
    }

    #endregion 附加属性

    private static void OnPreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;
        e.Handled = true;

        if (sender is not FrameworkElement frameworkElement) return;
        if (e.Data.GetDataPresent(DataFormats.FileDrop) == false) return;

        var isAllowMultipleFiles = GetIsAllowMultipleFiles(frameworkElement);
        Guard.IsNotNull(isAllowMultipleFiles);
        var allowedExtensions = GetAllowedExtensions(frameworkElement);
        Guard.IsNotNullOrWhiteSpace(allowedExtensions);

        var files = (string[]?)e.Data.GetData(DataFormats.FileDrop) ?? ThrowHelper.ThrowArgumentNullException<string[]?>(nameof(e.Data.GetData));
        if (files.Length > 1 && isAllowMultipleFiles == false) return;

        if (files.All(filePath => FileHelper.IsValidateFileExtension(filePath, allowedExtensions)) == false) return;

        e.Effects = DragDropEffects.Copy;
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        if (sender is not FrameworkElement frameworkElement) return;
        if (e.Data.GetDataPresent(DataFormats.FileDrop) == false) return;

        var command = GetCommand(frameworkElement);
        Guard.IsNotNull(command);
        var isAllowMultipleFiles = GetIsAllowMultipleFiles(frameworkElement);
        Guard.IsNotNull(isAllowMultipleFiles);

        var files = (string[]?)e.Data.GetData(DataFormats.FileDrop) ?? ThrowHelper.ThrowArgumentNullException<string[]?>(nameof(e.Data.GetData));

        object parameter = isAllowMultipleFiles ? files : files.First();

        if (command.CanExecute(parameter)) command.Execute(parameter);

        e.Handled = true;
    }
}