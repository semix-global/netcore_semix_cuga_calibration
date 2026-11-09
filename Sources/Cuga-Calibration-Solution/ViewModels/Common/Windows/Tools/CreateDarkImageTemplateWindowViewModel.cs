using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Algorithm.Models.TemplateMatch;
using Core.Models.Enums.Algorithm;
using Core.Models.Helper;
using Core.Models.Models.Setting;
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
    ReviewViewModel reviewViewModel,
    CalibrationSetting calibrationSetting) : ViewModelBase
{
    [ObservableProperty]
    public partial Rect Rect { get; set; }

    [ObservableProperty]
    public partial string ImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial TemplateMatchSetting TemplateMatchSetting { get; set; } = new();

    [ObservableProperty]
    public partial string TemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateImageFilePath { get; set; } = string.Empty;

    /// <summary>
    /// 步骤标题（仿 OpticsBestFocus 窗口的步骤分区）
    /// </summary>
    public string[] Steps { get; } = ["Select ROI", "Mark Template Center"];

    /// <summary>
    /// ROI 是否显示（首次打开不显示，Step 1 Action 后显示）
    /// </summary>
    [ObservableProperty]
    public partial bool IsRoiVisible { get; set; }

    /// <summary>
    /// 自定义模板中心（原图像素坐标；null = 默认 ROI 中心，Step 2 激活后为 ROI 中心）
    /// </summary>
    [ObservableProperty]
    public partial Point? TemplateCenter { get; set; }

    /// <summary>
    /// 模板中心显示文本（未标记时为默认 ROI 中心）
    /// </summary>
    public string TemplateCenterText => TemplateCenter?.ToString() ?? "ROI Center (Default)";

    partial void OnTemplateCenterChanged(Point? value) => OnPropertyChanged(nameof(TemplateCenterText));

    partial void OnTemplateMatchSettingChanged(TemplateMatchSetting value)
    {
        Rect = new Rect(Rect.X, Rect.Y, Convert.ToInt32(value.TemplateSize), Convert.ToInt32(value.TemplateSize));
    }

    /// <summary>
    /// 单例 ViewModel 每次弹窗时清空上次会话的 ROI / 标记点 / 预览, 调用方在 ShowDialog 前写入的路径和 Setting 保留
    /// </summary>
    [RelayCommand]
    private void Loaded()
    {
        TemplateCenter = null;
        IsRoiVisible = false;
        Rect = default;
        TemplateImageFilePath = string.Empty;
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

                if (TemplateCenter is { } templateCenter)
                {
                    if (new Rect(Point.Origin, new Size(image.Width, image.Height)).Contains(templateCenter) == false)
                    {
                        dialogWindowProvider.ShowDialog("Error: The Template Center is out of the size of the image", DialogButtonsEnum.OK, DialogIconEnum.Error);
                        return;
                    }

                    if (Rect.Contains(templateCenter) == false)
                    {
                        dialogWindowProvider.ShowDialog("Error: The Template Center is out of the ROI", DialogButtonsEnum.OK, DialogIconEnum.Error);
                        return;
                    }
                }

                if (reviewViewModel.TryGenerateTemplate(image, TemplateMatchSetting.ToGenerateParam(calibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum, Rect, TemplateCenter, TemplateFilePath), Guid.NewGuid()) == false)
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

    /// <summary>
    /// Step 1：框选 ROI（显示 ROI；首次以图像中心初始化默认模板尺寸，再次点击保留位置仅重置尺寸；清除标记中心）
    /// </summary>
    [RelayCommand]
    private void Step1()
    {
        TemplateCenter = null;
        IsRoiVisible = true;

        var size = Convert.ToInt32(TemplateMatchSetting.TemplateSize);
        if (Rect.Width > 0 && Rect.Height > 0)
        {
            Rect = new Rect(Rect.X, Rect.Y, size, size);
            return;
        }

        if (System.IO.File.Exists(ImageFilePath) == false) return;

        using var image = new BitmapImage(ImageFilePath);
        Rect = new Rect(image.Width / 2 - size / 2, image.Height / 2 - size / 2, size, size);
    }

    /// <summary>
    /// Step 2：标记模板中心（在 ROI 中心显示 45° 十字架，拖动十字架标记；十字架显示期间 ROI 锁定不可编辑，回到 Step 1 解锁）
    /// </summary>
    [RelayCommand]
    private void Step2()
    {
        if (System.IO.File.Exists(ImageFilePath) == false)
        {
            dialogWindowProvider.ShowDialog("Error: Dark Image is Empty", DialogButtonsEnum.OK, DialogIconEnum.Error);
            return;
        }

        IsRoiVisible = true;
        TemplateCenter = Rect.Center;
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