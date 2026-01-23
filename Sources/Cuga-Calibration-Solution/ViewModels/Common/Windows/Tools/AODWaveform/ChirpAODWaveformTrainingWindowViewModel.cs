using System.IO;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Cookies;
using Core.Services.Interfaces;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Extensions;
using Local.NoSQL.DB.Providers.Interfaces;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Mapper;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
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
    public string Name => "Chirp AOD Waveform Training";

    public ApplicationCookie ApplicationCookie => applicationCookie;

    public string AODWaveformDirectoryPath => Path.Combine(options.Value.AppHomeDirectory, "AODWaveform", GetType().Name, DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public string ImageFileDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", GetType().Name, DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

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
    private async Task ActionC2Async(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                var detectImageDirectory = ImageFileDirectory;
                Cache.Items = [];

                foreach (var c2Coefficient in Generate.LinearRange(Cache.StartC2Coefficient, Cache.StepC2Coefficient, Cache.StopC2Coefficient))
                {
                    await CatchImagesAsync(c2Coefficient, 0, 0, 0, 0, 0, 0, Guid.NewGuid(), cancellationToken);
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

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ActionC3Async(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                var detectImageDirectory = ImageFileDirectory;
                Cache.Items = [];

                foreach (var c3Coefficient in Generate.LinearRange(Cache.StartC3Coefficient, Cache.StepC3Coefficient, Cache.StopC3Coefficient))
                {
                    await CatchImagesAsync(0, c3Coefficient, 0, 0, 0, 0, 0, Guid.NewGuid(), cancellationToken);
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

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ActionC4Async(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                var detectImageDirectory = ImageFileDirectory;
                Cache.Items = [];

                foreach (var c4Coefficient in Generate.LinearRange(Cache.StartC4Coefficient, Cache.StepC4Coefficient, Cache.StopC4Coefficient))
                {
                    await CatchImagesAsync(0, 0, c4Coefficient, 0, 0, 0, 0, Guid.NewGuid(), cancellationToken);
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

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ActionC5Async(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                var detectImageDirectory = ImageFileDirectory;
                Cache.Items = [];

                foreach (var c5Coefficient in Generate.LinearRange(Cache.StartC5Coefficient, Cache.StepC5Coefficient, Cache.StopC5Coefficient))
                {
                    await CatchImagesAsync(0, 0, 0, c5Coefficient, 0, 0, 0, Guid.NewGuid(), cancellationToken);
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

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ActionC6Async(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                var detectImageDirectory = ImageFileDirectory;
                Cache.Items = [];

                foreach (var c6Coefficient in Generate.LinearRange(Cache.StartC6Coefficient, Cache.StepC6Coefficient, Cache.StopC6Coefficient))
                {
                    await CatchImagesAsync(0, 0, 0, 0, c6Coefficient, 0, 0, Guid.NewGuid(), cancellationToken);
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

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ActionC7Async(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                var detectImageDirectory = ImageFileDirectory;
                Cache.Items = [];

                foreach (var c7Coefficient in Generate.LinearRange(Cache.StartC7Coefficient, Cache.StepC7Coefficient, Cache.StopC7Coefficient))
                {
                    await CatchImagesAsync(0, 0, 0, 0, 0, c7Coefficient, 0, Guid.NewGuid(), cancellationToken);
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

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ActionC8Async(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                var detectImageDirectory = ImageFileDirectory;
                Cache.Items = [];

                foreach (var c8Coefficient in Generate.LinearRange(Cache.StartC8Coefficient, Cache.StepC8Coefficient, Cache.StopC8Coefficient))
                {
                    await CatchImagesAsync(0, 0, 0, 0, 0, 0, c8Coefficient, Guid.NewGuid(), cancellationToken);
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

    private async Task CatchImagesAsync(double c2Coefficient, double c3Coefficient, double c4Coefficient, double c5Coefficient, double c6Coefficient, double c7Coefficient, double c8Coefficient, Guid htmlLogUniqueId, CancellationToken cancellationToken)
    {
        var item = new ChirpAODWaveformTrainingItem
        {
            ProductivityInformation = Cache.ProductivityInformation,
            LaserLightInformation = Cache.LaserLightInformation,
            CIBInformation = Cache.CIBInformation,
            C2Coefficient = c2Coefficient,
            C3Coefficient = c3Coefficient,
            C4Coefficient = c4Coefficient,
            C5Coefficient = c5Coefficient,
            C6Coefficient = c6Coefficient,
            C7Coefficient = c7Coefficient,
            C8Coefficient = c8Coefficient
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
        Cache.GenerateChirpAODWaveformParam.C2CompensationCoefficient = item.C2Coefficient;
        Cache.GenerateChirpAODWaveformParam.C3CompensationCoefficient = item.C3Coefficient;
        Cache.GenerateChirpAODWaveformParam.C4CompensationCoefficient = item.C4Coefficient;
        Cache.GenerateChirpAODWaveformParam.C5CompensationCoefficient = item.C5Coefficient;
        Cache.GenerateChirpAODWaveformParam.C6CompensationCoefficient = item.C6Coefficient;
        Cache.GenerateChirpAODWaveformParam.C7CompensationCoefficient = item.C7Coefficient;
        Cache.GenerateChirpAODWaveformParam.C8CompensationCoefficient = item.C8Coefficient;

        (var chirpAODWaveformResult, exception) = AODWaveformGenerator1.GenerateChirpAODWaveform(Cache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);
        if (chirpAODWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));

        item.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(chirpAODWaveformResult);
        item.ChirpAODWaveformResultFilePath = chirpAODWaveformResult.FilePath;

        laserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, item.PrescanAODWaveformProfiles);
        laserViewModel.SetChirpAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, item.ChirpAODWaveformProfiles);

        var startPositon = stageViewModel.MachineToBrightFieldPosition(Cache.DSWMachinePosition);

        var darkFieldImage = await cibViewModel.GetPMTImageAsync(
            item.ProductivityInformation,
            StageCoordinateSystemEnum.Dark,
            startPositon,
            startPositon + new Vector(Cache.ScanLength, startPositon.Y),
            item.CIBInformation,
            (true, null),
            (false, Cache.CIBConfiguration),
            (true, null),
            true,
            Cache.CenterECS - Cache.RangeECS,
            Cache.CenterECS + Cache.RangeECS,
            cancellationToken
        );

        var detectImageDirectory = ImageFileDirectory;
        var imageFilePath = Path.Combine(detectImageDirectory, item.CIBInformation.ToString(), $"{DateTime.Now.ToString(Constants.LongFileDateTimeFormat)}.jpg");
        darkFieldImage.Image.Save(imageFilePath);

        var resultPlots = calibrationAlgorithmService.GetXYStrehlRatio(darkFieldImage.Image);
        item.XStrehlRatioPoints = [.. resultPlots.Select(t => new Point(t.Position.X, t.XStrehlRatio))];
        item.YStrehlRatioPoints = [.. resultPlots.Select(t => new Point(t.Position.X, t.YStrehlRatio))];
        item.GrayPoints = [.. resultPlots.Select(t => new Point(t.Position.X, t.GrayValue))];

        item.XStrehlRatioFitPoints = calibrationAlgorithmService.SmoothStrehlFunction([.. resultPlots.Select(t => t.Position.X)], [.. resultPlots.Select(t => t.XStrehlRatio)]);
        item.YStrehlRatioFitPoints = calibrationAlgorithmService.SmoothStrehlFunction([.. resultPlots.Select(t => t.Position.X)], [.. resultPlots.Select(t => t.YStrehlRatio)]);
        item.GrayFitPoints = calibrationAlgorithmService.SmoothStrehlFunction([.. resultPlots.Select(t => t.Position.X)], [.. resultPlots.Select(t => t.GrayValue)]);

        item.BestXStrehlRatio = item.XStrehlRatioFitPoints.Maxima(t => t.Y).First();
        item.BestYStrehlRatio = item.YStrehlRatioFitPoints.Maxima(t => t.Y).First();
        item.BestGray = item.GrayFitPoints.Maxima(t => t.Y).First();

        item.ImageFilePath = imageFilePath;
        item.RawImageFilePath = darkFieldImage.RawImageFilePath;

        Cache.Items = [.. Cache.Items, item];

        if (htmlLogUniqueId == Guid.Empty) return;

        logger.LogHtmlInformation("AOD Waveform", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            GeneratePrescanAODWaveformParam = new HtmlQuote(Cache.GeneratePrescanAODWaveformParam.ToHtmlAnonymous()),
            item.PrescanAODWaveformResultFilePath,
            PrescanAODWaveformProfiles = new HtmlTable([.. item.PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
            GenerateChirpAODWaveformParam = new HtmlQuote(Cache.GenerateChirpAODWaveformParam.ToHtmlAnonymous()),
            item.ChirpAODWaveformResultFilePath,
            ChirpAODWaveformProfiles = new HtmlTable([.. item.ChirpAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
            item.ImageFilePath,
            item.RawImageFilePath,
            item.BestXStrehlRatio,
            item.BestYStrehlRatio,
            item.BestGray
        }), htmlLogUniqueId.LoggingHtml());
    }


    [RelayCommand]
    private void Close()
    {
        try
        {
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