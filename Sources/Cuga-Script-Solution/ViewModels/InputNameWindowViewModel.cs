using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaScript.ViewModels;

public sealed partial class InputNameWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;

    [RelayCommand]
    private void Ok()
    {
        CloseView(string.IsNullOrWhiteSpace(Name) == false);
    }

    [RelayCommand]
    private void Close()
    {
        CloseView(false);
    }
}