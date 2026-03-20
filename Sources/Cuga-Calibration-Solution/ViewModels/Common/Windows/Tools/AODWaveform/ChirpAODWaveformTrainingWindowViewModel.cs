using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Cookies;
using Core.Services.Interfaces;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
using Local.SQL.Cache.Providers.Extensions;
using Local.SQL.Cache.Providers.Interfaces;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Threading;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;
using System.Text;
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

    [DefaultCache]
    [ObservableProperty]
    private ChirpAODWaveformTrainingCache _cache = new();

    [RelayCommand]
    private async Task LoadedAsync() => await Task.Run(() => Cache = cacheProvider.GetOrDefault<ChirpAODWaveformTrainingCache>());

    [RelayCommand]
    private void ImportAODWaveformParams()
    {
        try
        {
            var isSuccess = true;

            var stringBuilder = new StringBuilder();

            var prescanCache = cacheProvider.GetOrDefault<PrescanAODWaveformElectrodeOffsetCache>();

            var prescanResult = prescanCache.Results.FirstOrDefault(t => t.GeneratePrescanAODWaveformParam.ProductivityInformation.Equals(Cache.ProductivityInformation));

            if (prescanResult is null)
            {
                stringBuilder.AppendLine("Warning: Prescan AOD Waveform Param No matched found for current Productivity Information!");
                isSuccess = false;
            }
            else
            {
                Cache.GeneratePrescanAODWaveformParam = prescanResult.GeneratePrescanAODWaveformParam;
                stringBuilder.AppendLine("Ok: Prescan AOD Waveform Param Import Success!");
            }

            var chirpCache = cacheProvider.GetOrDefault<ChirpAODWaveformElectrodeOffsetCache>();

            var chirpResult = chirpCache.Results.FirstOrDefault(t => t.GenerateChirpAODWaveformParam.ProductivityInformation.Equals(Cache.ProductivityInformation));

            if (chirpResult is null)
            {
                stringBuilder.AppendLine("Warning: Chirp AOD Waveform Param No matched found for current Productivity Information!");
                isSuccess = false;
            }
            else
            {
                Cache.GenerateChirpAODWaveformParam = chirpResult.GenerateChirpAODWaveformParam;
                stringBuilder.AppendLine("Ok: Chirp AOD Waveform Param Import Success!");
            }

            dialogWindowProvider.ShowDialog(stringBuilder.ToString(), DialogButtonsEnum.OK, isSuccess ? DialogIconEnum.Information : DialogIconEnum.Warning);
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
    private async Task TrainingP2Async(CancellationToken cancellationToken) => await TrainingPAsync(2, Cache.StartP2Coefficient, Cache.StepP2Coefficient, Cache.StopP2Coefficient, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task TrainingP3Async(CancellationToken cancellationToken) => await TrainingPAsync(3, Cache.StartP3Coefficient, Cache.StepP3Coefficient, Cache.StopP3Coefficient, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task TrainingP4Async(CancellationToken cancellationToken) => await TrainingPAsync(4, Cache.StartP4Coefficient, Cache.StepP4Coefficient, Cache.StopP4Coefficient, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task TrainingP5Async(CancellationToken cancellationToken) => await TrainingPAsync(5, Cache.StartP5Coefficient, Cache.StepP5Coefficient, Cache.StopP5Coefficient, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task TrainingP6Async(CancellationToken cancellationToken) => await TrainingPAsync(6, Cache.StartP6Coefficient, Cache.StepP6Coefficient, Cache.StopP6Coefficient, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task TrainingP7Async(CancellationToken cancellationToken) => await TrainingPAsync(7, Cache.StartP7Coefficient, Cache.StepP7Coefficient, Cache.StopP7Coefficient, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task TrainingP8Async(CancellationToken cancellationToken) => await TrainingPAsync(8, Cache.StartP8Coefficient, Cache.StepP8Coefficient, Cache.StopP8Coefficient, cancellationToken);

    private async Task TrainingPAsync(int p, double start, double step, double stop, CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            var htmlLogUniqueId = Guid.NewGuid();

            logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, htmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation($"Training P{p}", HtmlHeaderLevelEnum.Header2, htmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Cache.ToHtmlAnonymous()), htmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                Cache.Items = [];

                cancellationToken.ThrowIfCancellationRequested();

                foreach (var val in Generate.LinearRange(start, step, stop))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    logger.LogHtmlInformation($"P{p}: {val:0.######}", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

                    var currentItem = await CatchImagesAsync(
                        p == 2 ? val : Cache.P2Coefficient,
                        p == 3 ? val : Cache.P3Coefficient,
                        p == 4 ? val : Cache.P4Coefficient,
                        p == 5 ? val : Cache.P5Coefficient,
                        p == 6 ? val : Cache.P6Coefficient,
                        p == 7 ? val : Cache.P7Coefficient,
                        p == 8 ? val : Cache.P8Coefficient,
                        htmlLogUniqueId,
                        cancellationToken);

                    if (Cache.IsConfirmBestYStrehlRatioResult)
                    {
                        dialogWindowProvider.ShowDialog("Please review the result and click Continue to proceed.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        await _asyncAutoResetEvent.WaitAsync(cancellationToken);

                        Cache.Item = Cache.SelectedItem;
                    }
                    else
                    {
                        if (currentItem.BestYStrehlRatioPoint.Y > Cache.Item.BestYStrehlRatioPoint.Y) Cache.Item = currentItem;
                    }
                }

                logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Base = new HtmlQuote(Cache.Item.ToHtmlAnonymous()),
                    GeneratePrescanAODWaveformParam = new HtmlQuote(Cache.GeneratePrescanAODWaveformParam.ToHtmlAnonymous()),
                    Cache.Item.PrescanAODWaveformResultFilePath,
                    PrescanAODWaveformProfiles = new HtmlTable([.. Cache.Item.PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
                    GenerateChirpAODWaveformParam = new HtmlQuote(Cache.GenerateChirpAODWaveformParam.ToHtmlAnonymous()),
                    Cache.Item.ChirpAODWaveformResultFilePath,
                    ChirpAODWaveformProfiles = new HtmlTable([.. Cache.Item.ChirpAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
                    XStrehlRatioScatterPlotControl = new HtmlContainer([.. Cache.Item.XStrehlRatioScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]),
                    YStrehlRatioScatterPlotControl = new HtmlContainer([.. Cache.Item.YStrehlRatioScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]),
                    GrayScatterPlotControl = new HtmlContainer([.. Cache.Item.GrayScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                }), htmlLogUniqueId.LoggingHtml());

                isSuccess = true;

                dialogWindowProvider.ShowDialog($"Training P{p} Success");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog($"{Name}: Training P{p} Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());

                    return;
                }

                dialogWindowProvider.ShowDialog($"""
                                                 {Name}: Training P{p} Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogHtmlError(ex, Name, HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());
            }
            finally
            {
                logger.LogHtmlInformation(htmlLogUniqueId.LoggedEndHtml($"{Name.Replace(" ", string.Empty)}_P{p}_{(isSuccess ? "OK" : "Failed")}"));
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand]
    private void Continue()
    {
        _asyncAutoResetEvent.Set();
    }

    private async Task<ChirpAODWaveformTrainingItem> CatchImagesAsync(
        double p2Coefficient,
        double p3Coefficient,
        double p4Coefficient,
        double p5Coefficient,
        double p6Coefficient,
        double p7Coefficient,
        double p8Coefficient,
        Guid htmlLogUniqueId,
        CancellationToken cancellationToken)
    {
        var item = new ChirpAODWaveformTrainingItem
        {
            ProductivityInformation = Cache.ProductivityInformation,
            LaserLightInformation = Cache.LaserLightInformation,
            CIBInformation = Cache.CIBInformation,
            P2Coefficient = p2Coefficient,
            P3Coefficient = p3Coefficient,
            P4Coefficient = p4Coefficient,
            P5Coefficient = p5Coefficient,
            P6Coefficient = p6Coefficient,
            P7Coefficient = p7Coefficient,
            P8Coefficient = p8Coefficient
        };

        try
        {
            Cache.GeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
            foreach (var configuration in Cache.GeneratePrescanAODWaveformParam.ElectrodeConfigurations) configuration.WithAmplitude(item.LaserLightInformation.Coefficient);
            Cache.GeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

            var prescanAODWaveformResult = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);

            item.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(prescanAODWaveformResult);
            item.PrescanAODWaveformResultFilePath = prescanAODWaveformResult.FilePath;

            item.ChirpAODWaveformProfiles = [];
            item.ChirpAODWaveformResultFilePath = string.Empty;

            Cache.GenerateChirpAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
            Cache.GenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
            Cache.GenerateChirpAODWaveformParam.P2CompensationCoefficient = item.P2Coefficient;
            Cache.GenerateChirpAODWaveformParam.P3CompensationCoefficient = item.P3Coefficient;
            Cache.GenerateChirpAODWaveformParam.P4CompensationCoefficient = item.P4Coefficient;
            Cache.GenerateChirpAODWaveformParam.P5CompensationCoefficient = item.P5Coefficient;
            Cache.GenerateChirpAODWaveformParam.P6CompensationCoefficient = item.P6Coefficient;
            Cache.GenerateChirpAODWaveformParam.P7CompensationCoefficient = item.P7Coefficient;
            Cache.GenerateChirpAODWaveformParam.P8CompensationCoefficient = item.P8Coefficient;

            var chirpAODWaveformResult = AODWaveformGenerator.GenerateChirpAODWaveform(Cache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);

            item.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(chirpAODWaveformResult);
            item.ChirpAODWaveformResultFilePath = chirpAODWaveformResult.FilePath;

            laserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, item.PrescanAODWaveformProfiles);
            laserViewModel.SetChirpAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, item.ChirpAODWaveformProfiles);

            cancellationToken.ThrowIfCancellationRequested();

            var startPositon = stageViewModel.MachineToBrightFieldPosition(Cache.DSWMachinePosition);

            var startECS = Cache.CenterECS - Cache.RangeECS;
            var stopECS = Cache.CenterECS + Cache.RangeECS;
            using var darkFieldImage = await cibViewModel.GetPMTImageAsync(
                item.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                startPositon,
                startPositon + new Vector(Cache.ScanLength, 0),
                startECS,
                stopECS,
                item.CIBInformation,
                (true, null),
                (false, Cache.CIBConfiguration),
                (true, null),
                true,
                cancellationToken);

            item.RawImageFilePath = darkFieldImage.RawImageFilePath;

            var (
                xStrehlRatioPoints,
                yStrehlRatioPoints,
                grayPoints,
                bestXStrehlRatioPoint,
                bestXStrehlRatioXPSFPoints,
                bestXStrehlRatioYPSFPoints,
                bestYStrehlRatioPoint,
                bestYStrehlRatioXPSFPoints,
                bestYStrehlRatioYPSFPoints) = calibrationAlgorithmService.GetXYStrehlRatios(
                darkFieldImage.Image,
                out var xStrehlRatioFitPoints,
                out var yStrehlRatioFitPoints,
                out var grayFitPoints,
                out var bestXStrehlRatioXPSFFitPoints,
                out var bestXStrehlRatioYPSFFitPoints,
                out var bestYStrehlRatioXPSFFitPoints,
                out var bestYStrehlRatioYPSFFitPoints);

            item.XStrehlRatioPoints = xStrehlRatioPoints;
            item.YStrehlRatioPoints = yStrehlRatioPoints;
            item.GrayPoints = grayPoints;
            item.BestXStrehlRatioPoint = bestXStrehlRatioPoint;
            item.BestXStrehlRatioXPSFPoints = bestXStrehlRatioXPSFPoints;
            item.BestXStrehlRatioYPSFPoints = bestXStrehlRatioYPSFPoints;
            item.BestYStrehlRatioPoint = bestYStrehlRatioPoint;
            item.BestYStrehlRatioXPSFPoints = bestYStrehlRatioXPSFPoints;
            item.BestYStrehlRatioYPSFPoints = bestYStrehlRatioYPSFPoints;

            item.XStrehlRatioFitPoints = xStrehlRatioFitPoints;
            item.YStrehlRatioFitPoints = yStrehlRatioFitPoints;
            item.GrayFitPoints = grayFitPoints;
            item.BestXStrehlRatioXPSFFitPoints = bestXStrehlRatioXPSFFitPoints;
            item.BestXStrehlRatioYPSFFitPoints = bestXStrehlRatioYPSFFitPoints;
            item.BestYStrehlRatioXPSFFitPoints = bestYStrehlRatioXPSFFitPoints;
            item.BestYStrehlRatioYPSFFitPoints = bestYStrehlRatioYPSFFitPoints;

            item.BestGrayPoint = item.GrayFitPoints.Maxima(t => t.Y).First();

            item.BestXStrehlRatioECS = startECS + item.BestXStrehlRatioPoint.X / darkFieldImage.Size.Width * (stopECS - startECS);
            item.BestYStrehlRatioECS = startECS + item.BestYStrehlRatioPoint.X / darkFieldImage.Size.Width * (stopECS - startECS);
            item.BestGrayECS = startECS + item.BestGrayPoint.X / darkFieldImage.Size.Width * (stopECS - startECS);

            Cache.Items = [.. Cache.Items, item];

            Cache.SelectedItem = item;

            return item;
        }
        finally
        {
            logger.LogHtmlInformation("Details", HtmlHeaderLevelEnum.Header6, new HtmlQuote(new
            {
                Base = new HtmlQuote(item.ToHtmlAnonymous()),
                GeneratePrescanAODWaveformParam = new HtmlQuote(Cache.GeneratePrescanAODWaveformParam.ToHtmlAnonymous()),
                item.PrescanAODWaveformResultFilePath,
                PrescanAODWaveformProfiles = new HtmlTable([.. item.PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
                GenerateChirpAODWaveformParam = new HtmlQuote(Cache.GenerateChirpAODWaveformParam.ToHtmlAnonymous()),
                item.ChirpAODWaveformResultFilePath,
                ChirpAODWaveformProfiles = new HtmlTable([.. item.ChirpAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
                XStrehlRatioScatterPlotControl = new HtmlContainer([.. Cache.Item.XStrehlRatioScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]),
                YStrehlRatioScatterPlotControl = new HtmlContainer([.. Cache.Item.YStrehlRatioScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]),
                GrayScatterPlotControl = new HtmlContainer([.. Cache.Item.GrayScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
            }), htmlLogUniqueId.LoggingHtml());
        }
    }

    [RelayCommand]
    private void Close()
    {
        try
        {
            TrainingP2CancelCommand.Execute(null);
            TrainingP3CancelCommand.Execute(null);
            TrainingP4CancelCommand.Execute(null);
            TrainingP5CancelCommand.Execute(null);
            TrainingP6CancelCommand.Execute(null);
            TrainingP7CancelCommand.Execute(null);
            TrainingP8CancelCommand.Execute(null);

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