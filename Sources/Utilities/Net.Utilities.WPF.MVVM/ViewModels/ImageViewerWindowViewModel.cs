using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;

namespace Net.Utilities.WPF.MVVM.ViewModels;

public sealed partial class ImageViewerWindowViewModel : ViewModelBase
{
    private readonly IDialogWindowProvider _dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();
    private readonly ILogger<ImageViewerWindowViewModel> _logger = HostApplication.GetRequiredService<ILogger<ImageViewerWindowViewModel>>();

    [ObservableProperty]
    private int _imageCount;

    [ObservableProperty]
    private List<DarkImage> _imageList = [];

    public ImageViewerWindowViewModel(List<(string filePath, string imageName)> imageList)
    {
        ImageList.Clear();
        foreach (var (filePath, imageName) in imageList)
        {
            ImageList.Add(new DarkImage { FilePath = filePath, ImageName = imageName });
        }

        ImageCount = ImageList.Count >= 3 ? 3 : ImageList.Count;
    }

    [RelayCommand]
    private async Task SavePictureAsync(DarkImage darkImage)
    {
        try
        {
            await Task.Run(() =>
            {
                var tryShowSaveFilePathDialog = _dialogWindowProvider.TryShowSaveFilePathDialog(".jpg", out var saveFilePath);
                if (tryShowSaveFilePathDialog == false) return;
                if (File.Exists(darkImage.FilePath) == false)
                {
                    _dialogWindowProvider.ShowDialog("The image file does not exist.", "Error", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                File.Copy(darkImage.FilePath, saveFilePath, true);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{@Name}: Grabbing Image Failed", nameof(ImageViewerWindowViewModel));
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseView(null);
    }

    public sealed partial class DarkImage : ObservableObject
    {
        [ObservableProperty]
        private string _imageName = string.Empty;

        [ObservableProperty]
        private string _filePath = string.Empty;
    }
}