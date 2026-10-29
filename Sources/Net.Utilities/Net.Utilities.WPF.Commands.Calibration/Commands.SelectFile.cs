using CommunityToolkit.Mvvm.Input;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using System.Windows;
using System.Windows.Controls;

namespace Net.Utilities.WPF.Commands.Calibration;

/// <summary>
/// 文件选择命令参数, 通过 Freezable 继承绑定上下文.
/// </summary>
public sealed class SelectFileCommandParameter : Freezable
{
    public static readonly DependencyProperty TextBoxProperty = DependencyProperty.Register(
        nameof(TextBox),
        typeof(TextBox),
        typeof(SelectFileCommandParameter));

    public static readonly DependencyProperty FileExtensionProperty = DependencyProperty.Register(
        nameof(FileExtension),
        typeof(string),
        typeof(SelectFileCommandParameter),
        new PropertyMetadata(string.Empty));

    public TextBox? TextBox
    {
        get => (TextBox?)GetValue(TextBoxProperty);
        set => SetValue(TextBoxProperty, value);
    }

    public string FileExtension
    {
        get => (string)GetValue(FileExtensionProperty);
        set => SetValue(FileExtensionProperty, value);
    }

    protected override Freezable CreateInstanceCore() => new SelectFileCommandParameter();
}

public static partial class Commands
{
    public static IRelayCommand<SelectFileCommandParameter> SelectFileCommand { get; } = new RelayCommand<SelectFileCommandParameter>(static parameter =>
    {
        if (parameter is not { TextBox: { } textBox }) return;

        var dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();

        try
        {
            if (dialogWindowProvider.TryShowSelectFilePathDialog(parameter.FileExtension, out var filePath) != true) return;

            textBox.SetCurrentValue(TextBox.TextProperty, filePath); // 修改文本, 同时保留原有绑定和样式来源.
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource(); // 立即把路径写回绑定源. 因为 TextBox.Text 默认在失去焦点时才更新源.
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Select File Failed!
                                             {ex}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    });
}