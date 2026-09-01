using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(CreateRoiWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CreateRoiWindowViewModel(
    IDialogWindowProvider dialogWindowProvider,
    ILogger<CreateRoiWindowViewModel> logger) : ViewModelBase
{
    [ObservableProperty]
    public partial Rect Rect { get; set; }

    [ObservableProperty]
    public partial string ImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial AlgorithmTemplateSizeEnum AlgorithmTemplateSizeEnum { get; set; }

    partial void OnAlgorithmTemplateSizeEnumChanged(AlgorithmTemplateSizeEnum value)
    {
        Rect = new Rect(Rect.X, Rect.Y, Convert.ToInt32(value), Convert.ToInt32(value));
    }

    [RelayCommand]
    private void Loaded()
    {
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum.Size256;
    }

    [RelayCommand]
    private async Task OkAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                if (System.IO.File.Exists(ImageFilePath) == false)
                {
                    dialogWindowProvider.ShowDialog("Error: Template File Path is Empty or Dark Image is Empty", DialogButtonsEnum.OK, DialogIconEnum.Error);
                    return;
                }

                var (_, size) = ImageHelper.GetImageInfo(ImageFilePath);
                if (new Rect(Point.Origin, size).Contains(Rect) == false)
                {
                    dialogWindowProvider.ShowDialog("Error: The ROI is out of the size of the image", DialogButtonsEnum.OK, DialogIconEnum.Error);
                    return;
                }

                dialogWindowProvider.ShowDialog("Get ROI Ok!");

                CloseView(true);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: Get Template Failed", nameof(CreateRoiWindowViewModel));
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseView(false);
    }
}