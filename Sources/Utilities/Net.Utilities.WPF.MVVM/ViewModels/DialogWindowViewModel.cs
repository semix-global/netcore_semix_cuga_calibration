using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.ComponentModel;

namespace Net.Utilities.WPF.MVVM.ViewModels;

public sealed partial class DialogWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private DialogResultEnum _dialogResultEnum;

    [ObservableProperty]
    private string? _dialogTitle;

    [ObservableProperty]
    private string? _dialogTitleColor;

    [ObservableProperty]
    private string? _dialogMessage;

    [ObservableProperty]
    private BindingList<DialogResultEnum>? _dialogButtonList;

    [RelayCommand]
    private void ConfirmDialogResult(DialogResultEnum option)
    {
        DialogResultEnum = option;
        CloseView(option switch
        {
            DialogResultEnum.Yes or DialogResultEnum.OK => true,
            not DialogResultEnum.None => false,
            _ => null
        });
    }
}