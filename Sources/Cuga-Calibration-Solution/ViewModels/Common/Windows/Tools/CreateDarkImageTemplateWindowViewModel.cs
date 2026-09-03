using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Helper;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(CreateDarkImageTemplateWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CreateDarkImageTemplateWindowViewModel(
    IDialogWindowProvider dialogWindowProvider,
    ILogger<CreateDarkImageTemplateWindowViewModel> logger,
    ReviewViewModel reviewViewModel) : ViewModelBase
{
    [ObservableProperty]
    public partial Rect Rect { get; set; }

    [ObservableProperty]
    public partial string ImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial AlgorithmTemplateTypeEnum AlgorithmTemplateTypeEnum { get; set; }

    [ObservableProperty]
    public partial AlgorithmTemplateSizeEnum AlgorithmTemplateSizeEnum { get; set; }

    [ObservableProperty]
    public partial string TemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateImageFilePath { get; set; } = string.Empty;

    partial void OnAlgorithmTemplateSizeEnumChanged(AlgorithmTemplateSizeEnum value)
    {
        Rect = new Rect(Rect.X, Rect.Y, Convert.ToInt32(value), Convert.ToInt32(value));
    }

    [RelayCommand]
    private async Task OkAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(TemplateFilePath) || System.IO.File.Exists(ImageFilePath) == false)
                {
                    dialogWindowProvider.ShowDialog("Error: Template File Path is Empty or Dark Image is Empty", DialogButtonsEnum.OK, DialogIconEnum.Error);
                    return;
                }

                if (Rect.Width < Convert.ToInt32(AlgorithmTemplateSizeEnum.Size32) || Rect.Height < Convert.ToInt32(AlgorithmTemplateSizeEnum.Size32))
                {
                    dialogWindowProvider.ShowDialog($"Error: Rect Width and Height >= {Convert.ToInt32(AlgorithmTemplateSizeEnum.Size32)}", DialogButtonsEnum.OK, DialogIconEnum.Error);
                    return;
                }

                using var image = new BitmapImage(ImageFilePath);
                if (new Rect(Point.Origin, new Size(image.Width, image.Height)).Contains(Rect) == false)
                {
                    dialogWindowProvider.ShowDialog("Error: The ROI is out of the size of the image", DialogButtonsEnum.OK, DialogIconEnum.Error);
                    return;
                }

                if (reviewViewModel.TryGenerateTemplate(image, AlgorithmTemplateTypeEnum, TemplateFilePath, Rect, Guid.NewGuid()) == false)
                {
                    dialogWindowProvider.ShowDialog("Error: Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Error);
                    return;
                }

                TemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(TemplateFilePath);
                OnPropertyChanged(nameof(TemplateImageFilePath));

                dialogWindowProvider.ShowDialog("Get Template Ok!");

                CloseView(true);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: Get Template Failed", nameof(CreateDarkImageTemplateWindowViewModel));
        }
    }

    [RelayCommand]
    private void Close()
    {
        if (System.IO.File.Exists(TemplateFilePath))
        {
            CloseView(true);
            return;
        }

        var showDialog = dialogWindowProvider.TryShowDialog("Error: Cancel Create Template?", out var dialogResultEnum, DialogButtonsEnum.OKCancel, DialogIconEnum.Error);
        if (showDialog == false || dialogResultEnum == DialogResultEnum.Cancel) return;

        CloseView(false);
    }
}