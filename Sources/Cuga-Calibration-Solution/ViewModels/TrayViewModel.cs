using CommunityToolkit.Mvvm.Input;
using Core.Utilities.WPF.Tray.Model;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels;

[IOCAppService(ServiceType = typeof(TrayViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class TrayViewModel : ViewModelBase
{
    private readonly IDialogWindowProvider _dialogWindowProvider;
    public ObservableCollection<TrayMenuItem> Items { get; } = [];

    public TrayViewModel(IDialogWindowProvider dialogWindowProvider)
    {
        _dialogWindowProvider = dialogWindowProvider;
        Items.Add(new TrayMenuItem
        {
            Header = "Exit",
            Command = new RelayCommand(Exit)
        });
    }

    [RelayCommand]
    private void Exit()
    {
        _dialogWindowProvider.TryShowDialog("Do you want to exit the program?", out var dialogResultEnum, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
        if (dialogResultEnum == DialogResultEnum.No) return;

        Environment.Exit(0);
    }
}