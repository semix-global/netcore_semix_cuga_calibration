using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Helper;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using CugaCalibration.ViewModels.Laser;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.WPF.MVVM.Providers;

namespace CugaCalibrationTest.ViewModels;

[IOCAppService(ServiceType = typeof(SplitImageWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class SplitImageWindowViewModel(
    LaserXPixelSizeCalibrationViewModel laserXPixelSizeCalibrationViewModel,
    CalibrationSetting calibrationSetting,
    [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
    ICacheDatabaseProvider recipeLiteDataBaseProvider,
    IDialogWindowProvider dialogWindowProvider,
    ICalibrationLaserService calibrationLaserService,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ILogger<SplitImageWindowViewModel> logger) : ViewModelBase
{
    private readonly AsyncAutoResetEvent _asyncAutoResetEvent = new(false);

    public LaserXPixelSizeCalibrationViewModel LaserXPixelSizeCalibrationViewModel => laserXPixelSizeCalibrationViewModel;

    [ObservableProperty]
    private string _templateFilePath = @"\\10.10.5.19\d\Nano\Cuga-Calibration\Template\LaserXPixelSizeCalibrationViewModel\S90(H-L)\20251117\5X\5563747b5bb240149f8f78eda98c838f.jpg_Template";

    [ObservableProperty]
    private string _templateImageFilePath = @"\\10.10.5.19\d\Nano\Cuga-Calibration\Template\LaserXPixelSizeCalibrationViewModel\S90(H-L)\20251117\5X\5563747b5bb240149f8f78eda98c838f.jpg_Template.jpg";

    [ObservableProperty]
    private string _slideRawImageFilePath = @"C:\Users\DELL\Pictures\20251117_22327_0_0_1_short_1250481_PMT08-CH3_8.raw";

    [ObservableProperty]
    private string _verifyRawImageFilePath = @"C:\Users\DELL\Pictures\20251117_22330_0_0_1_short_1214201_PMT08-CH3_8.raw";

    public double Threshold
    {
        get => LaserXPixelSizeCalibrationViewModel.Cache.Threshold;
        set => SetProperty(LaserXPixelSizeCalibrationViewModel.Cache.Threshold, value, LaserXPixelSizeCalibrationViewModel.Cache, (m, v) => m.Threshold = v);
    }

    public double NccTypeTemplateMatchScoreThreshold
    {
        get => calibrationSetting.SettingTemplateMatchParam.NccTypeTemplateMatchScoreThreshold;
        set => SetProperty(calibrationSetting.SettingTemplateMatchParam.NccTypeTemplateMatchScoreThreshold, value, calibrationSetting.SettingTemplateMatchParam, (m, v) => m.NccTypeTemplateMatchScoreThreshold = v);
    }

    public double SharpeTypeTemplateMatchScoreThreshold
    {
        get => calibrationSetting.SettingTemplateMatchParam.SharpeTypeTemplateMatchScoreThreshold;
        set => SetProperty(calibrationSetting.SettingTemplateMatchParam.SharpeTypeTemplateMatchScoreThreshold, value, calibrationSetting.SettingTemplateMatchParam, (m, v) => m.SharpeTypeTemplateMatchScoreThreshold = v);
    }

    public double XPixelSize
    {
        get => LaserXPixelSizeCalibrationViewModel.CalibratingItem.XPixelSize;
        set => SetProperty(LaserXPixelSizeCalibrationViewModel.CalibratingItem.XPixelSize, value, LaserXPixelSizeCalibrationViewModel.CalibratingItem, (m, v) => m.XPixelSize = v);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SplitImageAsync(CancellationToken cancellationToken)
    {
        try
        {
            recipeLiteDataBaseProvider.ChangeDatabase("D:\\Nano\\Cuga-Calibration\\Database\\0823\\cache.db", cancellationToken);

            await LaserXPixelSizeCalibrationViewModel.LoadedCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
            await LaserXPixelSizeCalibrationViewModel.CalibrateCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);

            // await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            // await LaserXPixelSizeCalibrationViewModel.Step0CalibrateActionCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
            await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            await LaserXPixelSizeCalibrationViewModel.NextCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);

            // await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            // await LaserXPixelSizeCalibrationViewModel.Step1CalibrateActionCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
            await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            await LaserXPixelSizeCalibrationViewModel.NextCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);

            // await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            // await LaserXPixelSizeCalibrationViewModel.Step2CalibrateActionCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
            await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            await LaserXPixelSizeCalibrationViewModel.NextCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);

            // await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            // await LaserXPixelSizeCalibrationViewModel.Step3CalibrateActionCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
            await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            await LaserXPixelSizeCalibrationViewModel.NextCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);

            LaserXPixelSizeCalibrationViewModel.Cache.Item.TemplateFilePath = TemplateFilePath;
            LaserXPixelSizeCalibrationViewModel.Cache.Item.TemplateImageFilePath = TemplateImageFilePath;

            ObjectHelper.SetFieldValue(calibrationLaserService, "_mockImageFilePath", SlideRawImageFilePath);
            ObjectHelper.SetFieldValue(calibrationAlgorithmService, "_isUseMock", false);

            // await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            // await LaserXPixelSizeCalibrationViewModel.Step4CalibrateActionCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
            await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            await LaserXPixelSizeCalibrationViewModel.NextCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);

            await LaserXPixelSizeCalibrationViewModel.ReviewCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
            ObjectHelper.SetFieldValue(calibrationLaserService, "_mockImageFilePath", VerifyRawImageFilePath);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                dialogWindowProvider.ShowDialog("Operation Cancelled");

                return;
            }

            logger.LogError(ex, "SplitImageAsync Error");
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        try
        {
            recipeLiteDataBaseProvider.ChangeDatabase("D:\\Nano\\Cuga-Calibration\\Database\\0823\\cache.db", cancellationToken);

            await LaserXPixelSizeCalibrationViewModel.LoadedCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
            await LaserXPixelSizeCalibrationViewModel.ReviewCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
            ObjectHelper.SetFieldValue(calibrationLaserService, "_mockImageFilePath", VerifyRawImageFilePath);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                dialogWindowProvider.ShowDialog("Operation Cancelled");

                return;
            }

            logger.LogError(ex, "SplitImageAsync Error");
        }
    }

    [RelayCommand]
    private void Continue()
    {
        if (LaserXPixelSizeCalibrationViewModel.CalibrationStepList[LaserXPixelSizeCalibrationViewModel.CalibrationStepIndex].StepIsNextEnable == false)
        {
            dialogWindowProvider.ShowDialog("No More Step");

            return;
        }

        _asyncAutoResetEvent.Set();
    }
}