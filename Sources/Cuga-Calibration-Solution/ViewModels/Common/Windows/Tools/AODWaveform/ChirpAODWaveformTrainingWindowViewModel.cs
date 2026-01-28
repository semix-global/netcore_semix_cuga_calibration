using System.IO;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Cookies;
using Core.Services.Interfaces;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Extensions;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Threading;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(ChirpAODWaveformTrainingWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChirpAODWaveformTrainingWindowViewModel(
    CIBViewModel cibViewModel,
    LaserViewModel laserViewModel,
    StageViewModel stageViewModel,
    ICacheProvider cacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    IOptions<ApplicationSetting> options,
    ApplicationCookie applicationCookie,
    ILogger<ChirpAODWaveformTrainingWindowViewModel> logger) : ViewModelBase
{
    private readonly AsyncAutoResetEvent _asyncAutoResetEvent = new(false);

    public string Name => "Chirp AOD Waveform Training";

    public ApplicationCookie ApplicationCookie => applicationCookie;

    public string AODWaveformDirectoryPath => Path.Combine(options.Value.AppHomeDirectory, "AODWaveform", GetType().Name, DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    [ObservableProperty]
    private ChirpAODWaveformTrainingCache _cache = new();

    [RelayCommand]
    private async Task LoadedAsync() => await Task.Run(() => Cache = cacheProvider.GetOrDefault<ChirpAODWaveformTrainingCache>());

    [RelayCommand]
    private void ImportPrescanAODWaveformParam()
    {
        try
        {
            var prescanCache = cacheProvider.GetOrDefault<PrescanAODWaveformElectrodeOffsetCache>();

            var prescanResult = prescanCache.Results.FirstOrDefault(t => t.GeneratePrescanAODWaveformParam.ProductivityInformation.Equals(Cache.ProductivityInformation));

            if (prescanResult is null)
            {
                dialogWindowProvider.ShowDialog("No matched found for current Productivity Information!", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return;
            }

            Cache.GeneratePrescanAODWaveformParam = prescanResult.GeneratePrescanAODWaveformParam;

            dialogWindowProvider.ShowDialog("Import Success!");
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Import Parameters Failed
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Import Parameters Failed");
        }
    }

    [RelayCommand]
    private void ImportChirpAODWaveformParam()
    {
        try
        {
            var chirpCache = cacheProvider.GetOrDefault<ChirpAODWaveformElectrodeOffsetCache>();

            var chirpResult = chirpCache.Results.FirstOrDefault(t => t.GenerateChirpAODWaveformParam.ProductivityInformation.Equals(Cache.ProductivityInformation));

            if (chirpResult is null)
            {
                dialogWindowProvider.ShowDialog("No matched found for current Productivity Information!", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return;
            }

            Cache.GenerateChirpAODWaveformParam = chirpResult.GenerateChirpAODWaveformParam;

            dialogWindowProvider.ShowDialog("Import Parameters Success!");
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Import Parameters Failed
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Import Parameters Failed");
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task TrainingAsync(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                Cache.Items = [];

                cancellationToken.ThrowIfCancellationRequested();

                Cache.Item = await CatchImagesAsync(
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    cancellationToken);

                for (var i = 0; i < Cache.RetryTimes; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await OptimizeCoefficientAsync(3);
                    await OptimizeCoefficientAsync(4);
                    await OptimizeCoefficientAsync(5);
                    await OptimizeCoefficientAsync(6);
                    await OptimizeCoefficientAsync(7);
                    await OptimizeCoefficientAsync(8);
                }

                async Task OptimizeCoefficientAsync(int p)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var currentPCoefficient = p switch
                    {
                        3 => Cache.Item.P3Coefficient,
                        4 => Cache.Item.P4Coefficient,
                        5 => Cache.Item.P5Coefficient,
                        6 => Cache.Item.P6Coefficient,
                        7 => Cache.Item.P7Coefficient,
                        8 => Cache.Item.P8Coefficient,
                        _ => ThrowHelper.ThrowArgumentException<double>(nameof(p))
                    };

                    var plusPCoefficient = currentPCoefficient + Cache.StepPCoefficient;
                    var minusPCoefficient = currentPCoefficient - Cache.StepPCoefficient;

                    await RunCatchImagesAsync(p, plusPCoefficient);

                    dialogWindowProvider.ShowDialog("Please review the result and click Continue to proceed.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    await _asyncAutoResetEvent.WaitAsync(cancellationToken);

                    /*if (plusItem.BestYStrehlRatio.Y > Cache.Item.BestYStrehlRatio.Y)
                    {
                        Cache.Item = plusItem;

                        return;
                    }*/

                    await RunCatchImagesAsync(p, minusPCoefficient);

                    dialogWindowProvider.ShowDialog("Please review the result and click Continue to proceed.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    await _asyncAutoResetEvent.WaitAsync(cancellationToken);

                    /*if (minusItem.BestYStrehlRatio.Y > Cache.Item.BestYStrehlRatio.Y) Cache.Item = minusItem;*/
                }

                async Task RunCatchImagesAsync(int index, double val)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await CatchImagesAsync(
                        index == 3 ? val : Cache.Item.P3Coefficient,
                        index == 4 ? val : Cache.Item.P4Coefficient,
                        index == 5 ? val : Cache.Item.P5Coefficient,
                        index == 6 ? val : Cache.Item.P6Coefficient,
                        index == 7 ? val : Cache.Item.P7Coefficient,
                        index == 8 ? val : Cache.Item.P8Coefficient,
                        cancellationToken);
                }

                dialogWindowProvider.ShowDialog("Training Success");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog($"Training Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                dialogWindowProvider.ShowDialog($"""
                                                 Training Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogError(ex, "Training Failed");
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand]
    private void Continue()
    {
        _asyncAutoResetEvent.Set();
    }

    private async Task<ChirpAODWaveformTrainingItem> CatchImagesAsync(
        double p3Coefficient,
        double p4Coefficient,
        double p5Coefficient,
        double p6Coefficient,
        double p7Coefficient,
        double p8Coefficient,
        CancellationToken cancellationToken)
    {
        var item = new ChirpAODWaveformTrainingItem
        {
            ProductivityInformation = Cache.ProductivityInformation,
            LaserLightInformation = Cache.LaserLightInformation,
            CIBInformation = Cache.CIBInformation,
            P3Coefficient = p3Coefficient,
            P4Coefficient = p4Coefficient,
            P5Coefficient = p5Coefficient,
            P6Coefficient = p6Coefficient,
            P7Coefficient = p7Coefficient,
            P8Coefficient = p8Coefficient
        };

        Cache.GeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        foreach (var configuration in Cache.GeneratePrescanAODWaveformParam.ElectrodeConfigurations) configuration.WithAmplitude(item.LaserLightInformation.Coefficient);
        Cache.GeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

        var (prescanAODWaveformResult, exception) = AODWaveformGenerator1.GeneratePrescanAODWaveform(Cache.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);
        if (prescanAODWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));

        item.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(prescanAODWaveformResult);
        item.PrescanAODWaveformResultFilePath = prescanAODWaveformResult.FilePath;

        item.ChirpAODWaveformProfiles = [];
        item.ChirpAODWaveformResultFilePath = string.Empty;

        Cache.GenerateChirpAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        Cache.GenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
        Cache.GenerateChirpAODWaveformParam.P3CompensationCoefficient = item.P3Coefficient;
        Cache.GenerateChirpAODWaveformParam.P4CompensationCoefficient = item.P4Coefficient;
        Cache.GenerateChirpAODWaveformParam.P5CompensationCoefficient = item.P5Coefficient;
        Cache.GenerateChirpAODWaveformParam.P6CompensationCoefficient = item.P6Coefficient;
        Cache.GenerateChirpAODWaveformParam.P7CompensationCoefficient = item.P7Coefficient;
        Cache.GenerateChirpAODWaveformParam.P8CompensationCoefficient = item.P8Coefficient;

        (var chirpAODWaveformResult, exception) = AODWaveformGenerator1.GenerateChirpAODWaveform(Cache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);
        if (chirpAODWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));

        item.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(chirpAODWaveformResult);
        item.ChirpAODWaveformResultFilePath = chirpAODWaveformResult.FilePath;

        laserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, item.PrescanAODWaveformProfiles);
        laserViewModel.SetChirpAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, item.ChirpAODWaveformProfiles);

        cancellationToken.ThrowIfCancellationRequested();

        var startPositon = stageViewModel.MachineToBrightFieldPosition(Cache.DSWMachinePosition);

        using var darkFieldImage = await cibViewModel.GetPMTImageAsync(
            item.ProductivityInformation,
            StageCoordinateSystemEnum.Dark,
            startPositon,
            startPositon + new Vector(Cache.ScanLength, 0),
            item.CIBInformation,
            (true, null),
            (false, Cache.CIBConfiguration),
            (true, null),
            true,
            Cache.CenterECS - Cache.RangeECS,
            Cache.CenterECS + Cache.RangeECS,
            cancellationToken
        );

        using var image = Cache.CIBConfiguration.CIBProfileMode == CIBProfileModeEnum.PMTLog
            ? calibrationAlgorithmService.DarkFieldRawImageToLinearImage(darkFieldImage.Image)
            : darkFieldImage.Image.Copy();

        var resultPlots = calibrationAlgorithmService.GetXYStrehlRatio(image);
        item.XStrehlRatioPoints = [.. resultPlots.Select(t => new Point(t.Position.X, t.XStrehlRatio))];
        item.YStrehlRatioPoints = [.. resultPlots.Select(t => new Point(t.Position.X, t.YStrehlRatio))];
        item.GrayPoints = [.. resultPlots.Select(t => new Point(t.Position.X, t.GrayValue))];

        item.XStrehlRatioFitPoints = calibrationAlgorithmService.SmoothStrehlFunction([.. resultPlots.Select(t => t.Position.X)], [.. resultPlots.Select(t => t.XStrehlRatio)]);
        item.YStrehlRatioFitPoints = calibrationAlgorithmService.SmoothStrehlFunction([.. resultPlots.Select(t => t.Position.X)], [.. resultPlots.Select(t => t.YStrehlRatio)]);
        item.GrayFitPoints = calibrationAlgorithmService.SmoothStrehlFunction([.. resultPlots.Select(t => t.Position.X)], [.. resultPlots.Select(t => t.GrayValue)]);

        item.BestXStrehlRatio = item.XStrehlRatioFitPoints.Maxima(t => t.Y).First();
        item.BestYStrehlRatio = item.YStrehlRatioFitPoints.Maxima(t => t.Y).First();
        item.BestGray = item.GrayFitPoints.Maxima(t => t.Y).First();

        item.RawImageFilePath = darkFieldImage.RawImageFilePath;

        Cache.Items = [.. Cache.Items, item];

        return item;
    }

    [RelayCommand]
    private void Close()
    {
        try
        {
            TrainingCancelCommand.Execute(null);

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            cacheProvider.Set(Cache, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save cache");
        }

        CloseView(null);
    }
}