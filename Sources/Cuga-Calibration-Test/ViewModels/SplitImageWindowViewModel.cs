using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Helper;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using CugaCalibration.ViewModels.CIB;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibrationTest.ViewModels;

[IOCAppService(ServiceType = typeof(SplitImageWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class SplitImageWindowViewModel(
    CIBXPixelSizeViewModel cibxPixelSizeViewModel,
    CalibrationSetting calibrationSetting,
    [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
    ICacheDatabaseProvider recipeLiteDataBaseProvider,
    IDialogWindowProvider dialogWindowProvider,
    ICalibrationLaserService calibrationLaserService,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ILogger<SplitImageWindowViewModel> logger) : ViewModelBase
{
    private readonly AsyncAutoResetEvent _asyncAutoResetEvent = new(false);

    public CIBXPixelSizeViewModel CIBXPixelSizeViewModel => cibxPixelSizeViewModel;

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
        get => CIBXPixelSizeViewModel.Cache.Threshold;
        set => SetProperty(CIBXPixelSizeViewModel.Cache.Threshold, value, CIBXPixelSizeViewModel.Cache, (m, v) => m.Threshold = v);
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
        get => CIBXPixelSizeViewModel.CalibratingItem.XPixelSize;
        set => SetProperty(CIBXPixelSizeViewModel.CalibratingItem.XPixelSize, value, CIBXPixelSizeViewModel.CalibratingItem, (m, v) => m.XPixelSize = v);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SplitImageAsync(CancellationToken cancellationToken)
    {
        try
        {
            recipeLiteDataBaseProvider.ChangeDatabase("D:\\Nano\\Cuga-Calibration\\Database\\0823\\cache.db", cancellationToken);

            await CIBXPixelSizeViewModel.LoadedCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
            await CIBXPixelSizeViewModel.CalibrateCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);

            // await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            // await LaserXPixelSizeCalibrationViewModel.Step0CalibrateActionCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
            await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            await CIBXPixelSizeViewModel.NextCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);

            // await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            // await LaserXPixelSizeCalibrationViewModel.Step1CalibrateActionCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
            await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            await CIBXPixelSizeViewModel.NextCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);

            // await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            // await LaserXPixelSizeCalibrationViewModel.Step2CalibrateActionCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
            await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            await CIBXPixelSizeViewModel.NextCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);

            // await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            // await LaserXPixelSizeCalibrationViewModel.Step3CalibrateActionCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
            await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            await CIBXPixelSizeViewModel.NextCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);

            CIBXPixelSizeViewModel.Cache.Item.TemplateFilePath = TemplateFilePath;
            CIBXPixelSizeViewModel.Cache.Item.TemplateImageFilePath = TemplateImageFilePath;

            ObjectHelper.SetFieldValue(calibrationLaserService, "_mockImageFilePath", SlideRawImageFilePath);
            ObjectHelper.SetFieldValue(calibrationAlgorithmService, "_isUseMock", false);

            // await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            // await LaserXPixelSizeCalibrationViewModel.Step4CalibrateActionCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
            await _asyncAutoResetEvent.WaitAsync(cancellationToken);
            await CIBXPixelSizeViewModel.NextCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);

            await CIBXPixelSizeViewModel.ReviewCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
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

            await CIBXPixelSizeViewModel.LoadedCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
            await CIBXPixelSizeViewModel.ReviewCommand.ExecuteAsync(cancellationToken).ConfigureAwait(true);
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
        if (CIBXPixelSizeViewModel.CalibrationStepList[CIBXPixelSizeViewModel.CalibrationStepIndex].StepIsNextEnable == false)
        {
            dialogWindowProvider.ShowDialog("No More Step");

            return;
        }

        _asyncAutoResetEvent.Set();
    }
}