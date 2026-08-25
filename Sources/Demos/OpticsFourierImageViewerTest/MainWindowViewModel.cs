using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Net.Utilities.Graphics;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Models;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Editors;

namespace OpticsFourierImageViewerTest;

public sealed partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    public partial BitmapImageDrawable BitmapImageDrawable { get; set; }

    [ObservableProperty]
    public partial string ImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial CanvasDocument Document { get; set; }

    public MainWindowViewModel()
    {
        BitmapImageDrawable = new BitmapImageDrawable();
        Document = new CanvasDocument();

        Document.DefaultModel.Add(BitmapImageDrawable);
    }

    [RelayCommand]
    private async Task OpenImageFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select an image file",
            Filter = Constants.ImageFileExtensionsFilter,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        await Task.Run(() =>
        {
            ImageFilePath = dialog.FileName;
            BitmapImageDrawable.BitmapImage = BitmapHelper.OpenImage(dialog.FileName);

            Document.View.ZoomToFit();
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task EditROIAsync(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            var options = new AddOrModifyRectROIDrawableInputOptions { IsMultiple = true, CancellationToken = cancellationToken };

            await AddOrModifyRectROIDrawableGetterEditor.RunAsync<AddOrModifyRectROIDrawableGetterEditor>(Document.Edit, options);
        }, cancellationToken).ConfigureAwait(false);
    }
}