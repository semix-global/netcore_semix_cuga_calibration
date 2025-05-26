using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Net.Utilities.WPF.Behaviors;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace Net.Utilities.WPF.MVVM.ViewModels;

public sealed partial class PlotWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private List<WpfPlotModel> _plotList = [];

    [RelayCommand]
    private void Close()
    {
        CloseView(null);
    }
}