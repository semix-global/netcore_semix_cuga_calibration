using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;
using System.Text;

namespace Core.Utilities.WPF.ApplicationAbout.ViewModel;

[IOCAppService(ServiceType = typeof(ShowMarkDownWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public partial class ShowMarkDownWindowViewModel(
    IDialogWindowProvider dialogWindowProvider) : ViewModelBase
{
    [ObservableProperty]
    public partial string MarkdownPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MarkdownContent { get; set; } = string.Empty;

    [RelayCommand]
    private void Loaded()
    {
        if (string.IsNullOrWhiteSpace(MarkdownPath))
        {
            dialogWindowProvider.ShowDialog("Error: Markdown Path is Empty", DialogButtonsEnum.OK, DialogIconEnum.Error);
            return;
        }

        if (Path.GetExtension(MarkdownPath) != ".md")
        {
            dialogWindowProvider.ShowDialog("Error:The file type is not markdown.", DialogButtonsEnum.OK, DialogIconEnum.Error);
            return;
        }

        using StreamReader sr = new(MarkdownPath, Encoding.UTF8);
        MarkdownContent = sr.ReadToEnd();
    }

    [RelayCommand]
    private void Close()
    {
        CloseView(false);
    }
}