using CugaCalibration.ViewModels;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using System.ComponentModel;

namespace CugaCalibration.Views;

[IOCAppService(ServiceType = typeof(MainWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MainWindow
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

        if (mainWindowViewModel.ActiveItem is not null)
        {
            _dialogWindowProvider.ShowDialog("Calibration, cannot be Cancel.", dialogIconEnum: DialogIconEnum.Warning);
            e.Cancel = true;
            return;
        }

        if (_dialogWindowProvider.TryShowDialog("Confirm to close the current service?", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Warning) == true || dialogResult == DialogResultEnum.Yes) return;

        e.Cancel = true;
    }
}