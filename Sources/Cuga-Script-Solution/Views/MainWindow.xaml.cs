using CugaScript.ViewModels;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using System.ComponentModel;
using System.Windows.Input;

namespace CugaScript.Views;

[IOCAppService(ServiceType = typeof(MainWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class MainWindow
{
    private readonly IDialogWindowProvider _dialogWindowProvider;

    public MainWindow(IDialogWindowProvider dialogWindowProvider)
    {
        InitializeComponent();

        _dialogWindowProvider = dialogWindowProvider;

        ContentRendered -= OnContentRendered;
        ContentRendered += OnContentRendered;

        Closing -= OnClosing;
        Closing += OnClosing;

        TextEditor.PreviewMouseWheel -= TextEditorOnPreviewMouseWheel;
        TextEditor.PreviewMouseWheel += TextEditorOnPreviewMouseWheel;
    }

    private void OnContentRendered(object sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        if (mainWindowViewModel.IsLoadingOk == false) Close();
    }

    private void OnClosing(object sender, CancelEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        if (mainWindowViewModel.IsLoadingOk == false) return;

        if (mainWindowViewModel.IsRunningScript)
        {
            _dialogWindowProvider.ShowDialog("Script is Running, cannot be Cancel.", dialogIconEnum: DialogIconEnum.Warning);
            e.Cancel = true;
            return;
        }

        if (mainWindowViewModel.IsEditDocument)
        {
            _dialogWindowProvider.ShowDialog("Script is has been modified, cannot be Cancel.", dialogIconEnum: DialogIconEnum.Warning);
            e.Cancel = true;
            return;
        }

        if (_dialogWindowProvider.TryShowDialog("Confirm to close the current service?", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Warning) == true || dialogResult == DialogResultEnum.Yes) return;

        e.Cancel = true;
    }

    private void TextEditorOnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        try
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) == false) return;

            if (e.Delta > 0)
            {
                if (TextEditor.FontSize >= 50) return;
                TextEditor.FontSize += 1;
            }
            else
            {
                if (TextEditor.FontSize <= 5) return;
                TextEditor.FontSize -= 1;
            }
        }
        finally
        {
            e.Handled = true;
        }
    }
}