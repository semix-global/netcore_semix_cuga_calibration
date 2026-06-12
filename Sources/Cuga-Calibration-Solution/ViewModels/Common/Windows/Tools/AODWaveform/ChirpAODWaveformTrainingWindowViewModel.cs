using System.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Cookies;
using Core.Services.Interfaces;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
using Local.SQL.Cache.Providers.Services.Interfaces;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Threading;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;
using System.Text;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Algorithms.Extensions;
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

    public string AODWaveformDirectoryPath => Path.Combine(options.Value.AppHomeDirectory, "AODWaveform", nameof(ChirpAODWaveformTrainingWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    [DefaultCache]
    [ObservableProperty]
    public partial ChirpAODWaveformTrainingCache Cache { get; set; } = new();

    [RelayCommand]
    private async Task LoadedAsync() => await Task.Run(() => Cache = cacheProvider.GetOrDefault<ChirpAODWaveformTrainingCache>());

    [RelayCommand]
    private void AddSlopeDeltaKConfiguration()
    {
        var configurationList = Cache.SlopeDeltaKConfigurations.ToList();
        configurationList.Add(new GenerateAODWaveformSlopeDeltaKConfiguration());

        Cache.SlopeDeltaKConfigurations = configurationList;
    }

    [RelayCommand]
    private void RemoveSlopeDeltaKConfiguration(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var configurationList = Cache.SlopeDeltaKConfigurations.ToList();
        foreach (GenerateAODWaveformSlopeDeltaKConfiguration selectItem in selectItems) configurationList.Remove(selectItem);

        Cache.SlopeDeltaKConfigurations = configurationList;
    }

    [RelayCommand]
    private void AddChirpAODWaveformTrainingDeltaK()
    {
        var trainingList = Cache.ChirpAODWaveformTrainingDeltaKs.ToList();
        trainingList.Add(new ChirpAODWaveformTrainingDeltaK());

        Cache.ChirpAODWaveformTrainingDeltaKs = trainingList;
    }

    [RelayCommand]
    private void RemoveChirpAODWaveformTrainingDeltaK(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var trainingList = Cache.ChirpAODWaveformTrainingDeltaKs.ToList();
        foreach (ChirpAODWaveformTrainingDeltaK selectItem in selectItems) trainingList.Remove(selectItem);

        Cache.ChirpAODWaveformTrainingDeltaKs = trainingList;
    }

    [RelayCommand]
    private void ImportAODWaveformParams()
    {
        try
        {
            var isSuccess = true;

            var stringBuilder = new StringBuilder();

            var prescanCache = cacheProvider.GetOrDefault<PrescanAODWaveformElectrodeOffsetCache>();

            var prescanResult = prescanCache.Results.SingleOrDefault(t => t.GeneratePrescanAODWaveformParam.ProductivityInformation.OpticsIlluminationModeEnum == Cache.ProductivityInformation.OpticsIlluminationModeEnum
                                                                          && t.GeneratePrescanAODWaveformParam.ProductivityInformation.OpticsMagType == Cache.ProductivityInformation.OpticsMagType);

            if (prescanResult is null)
            {
                stringBuilder.AppendLine("Warning: Prescan AOD Waveform Param No matched found for current Productivity Information!");
                isSuccess = false;
            }
            else
            {
                Cache.GeneratePrescanAODWaveformParam = prescanResult.GeneratePrescanAODWaveformParam.Clone();
                Cache.GeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation.Clone();
                stringBuilder.AppendLine("Ok: Prescan AOD Waveform Param Import Success!");
            }

            var chirpCache = cacheProvider.GetOrDefault<ChirpAODWaveformElectrodeOffsetCache>();

            var chirpResult = chirpCache.Results.SingleOrDefault(t => t.GenerateChirpAODWaveformParam.ProductivityInformation.OpticsIlluminationModeEnum == Cache.ProductivityInformation.OpticsIlluminationModeEnum
                                                                      && t.GenerateChirpAODWaveformParam.ProductivityInformation.OpticsMagType == Cache.ProductivityInformation.OpticsMagType);

            if (chirpResult is null)
            {
                stringBuilder.AppendLine("Warning: Chirp AOD Waveform Param No matched found for current Productivity Information!");
                isSuccess = false;
            }
            else
            {
                Cache.GenerateChirpAODWaveformParam = chirpResult.GenerateChirpAODWaveformParam.Clone();
                Cache.GenerateChirpAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation.Clone();
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
    private async Task TrainingAllAsync(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            if (Cache.ChirpAODWaveformTrainingDeltaKs.Count <= 0)
            {
                dialogWindowProvider.ShowDialog("DeltaKs is Empty", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return;
            }

            if ((Cache.ChirpAODWaveformTrainingDeltaKs.Count & 1) == 0)
            {
                dialogWindowProvider.ShowDialog("DeltaKs count must be odd.", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return;
            }

            Cache.SlopeDeltaKConfigurations =
            [
                ..Cache.ChirpAODWaveformTrainingDeltaKs.Select(_ => new GenerateAODWaveformSlopeDeltaKConfiguration
                {
                    DeltaKRate = 0d,
                    Coefficient = 0d
                })
            ];

            foreach (var index in EnumerateFromCenter(Cache.ChirpAODWaveformTrainingDeltaKs.Count))
            {
                if (await TrainingAsync(Cache.ChirpAODWaveformTrainingDeltaKs[index], cancellationToken) == false) return;
            }

            return;

            static IEnumerable<int> EnumerateFromCenter(int length)
            {
                if (length <= 0) yield break;

                var center = length / 2;
                yield return center;

                for (var offset = 1;; offset++)
                {
                    var hasValue = false;

                    var left = center - offset;
                    if (left >= 0)
                    {
                        yield return left;

                        hasValue = true;
                    }

                    var right = center + offset;
                    if (right < length)
                    {
                        yield return right;

                        hasValue = true;
                    }

                    if (!hasValue) yield break;
                }
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> TrainingAsync(ChirpAODWaveformTrainingDeltaK chirpAODWaveformTrainingDeltaK, CancellationToken cancellationToken)
    {
        return await Task.Run(async () =>
        {
            var htmlLogUniqueId = Guid.NewGuid();

            var index = Cache.ChirpAODWaveformTrainingDeltaKs.Index().Single(t => ReferenceEquals(t.Item, chirpAODWaveformTrainingDeltaK)).Index;

            logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, htmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation($"Training: {index + 1}", HtmlHeaderLevelEnum.Header2, htmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Cache.ToHtmlAnonymous()), htmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                Cache.Items = [];

                cancellationToken.ThrowIfCancellationRequested();
                isSuccess = true;
                var deltaKRates = Generate.LinearRangeContainsEdge(chirpAODWaveformTrainingDeltaK.StartDeltaKRate, chirpAODWaveformTrainingDeltaK.StepDeltaKRate, chirpAODWaveformTrainingDeltaK.StopDeltaKRate);
                if (deltaKRates.Length <= 0)
                {
                    Cache.SlopeDeltaKConfigurations[index].DeltaKRate = 0d;
                    Cache.SlopeDeltaKConfigurations[index].Coefficient = 1d;
                }
                else
                {
                    foreach (var deltaKRate in deltaKRates)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        logger.LogHtmlInformation($"{index + 1}: {deltaKRate:0.######}", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

                        var temp = Cache.SlopeDeltaKConfigurations.ToArray();
                        temp[index] = new GenerateAODWaveformSlopeDeltaKConfiguration
                        {
                            DeltaKRate = deltaKRate,
                            Coefficient = 1d
                        };

                        var currentItem = await CatchImagesAsync(temp, htmlLogUniqueId, cancellationToken);

                        if (Cache.IsConfirmBestYStrehlRatioResult)
                        {
                            dialogWindowProvider.ShowDialog("Please review the result and click Continue to proceed.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            await _asyncAutoResetEvent.WaitAsync(cancellationToken);

                            Cache.Item = Cache.SelectedItem;
                        }
                        else
                        {
                            if (currentItem.BestFocus.BestYStrehlRatioPoint.Y > Cache.Item.BestFocus.BestYStrehlRatioPoint.Y) Cache.Item = currentItem;
                        }
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
                    XStrehlRatioScatterPlotControl = new HtmlContainer([.. Cache.Item.BestFocus.XStrehlRatioScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]),
                    YStrehlRatioScatterPlotControl = new HtmlContainer([.. Cache.Item.BestFocus.YStrehlRatioScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                }), htmlLogUniqueId.LoggingHtml());

                dialogWindowProvider.ShowDialog($"Training {index + 1} Success");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog($"{Name}: Training {index + 1} Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());

                    return false;
                }

                dialogWindowProvider.ShowDialog($"""
                                                 {Name}: Training {index + 1} Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogHtmlError(ex, Name, HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());
            }
            finally
            {
                logger.LogHtmlInformation(htmlLogUniqueId.LoggedEndHtml($"{Name.Replace(" ", string.Empty)}_{index + 1}_{(isSuccess ? "OK" : "Failed")}"));
            }

            return isSuccess;
        }, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand]
    private void Continue()
    {
        _asyncAutoResetEvent.Set();
    }

    private async Task<ChirpAODWaveformTrainingItem> CatchImagesAsync(
        IReadOnlyList<GenerateAODWaveformSlopeDeltaKConfiguration> slopeDeltaKConfigurations,
        Guid htmlLogUniqueId,
        CancellationToken cancellationToken)
    {
        var item = new ChirpAODWaveformTrainingItem
        {
            ProductivityInformation = Cache.ProductivityInformation,
            LaserLightInformation = Cache.LaserLightInformation,
            CIBInformation = Cache.CIBInformation,
            SlopeDeltaKConfigurations = [..slopeDeltaKConfigurations.Select(t => t.Clone())]
        };

        try
        {
            Cache.GeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
            foreach (var configuration in Cache.GeneratePrescanAODWaveformParam.ElectrodeConfigurations) configuration.WithAmplitude(item.LaserLightInformation.Coefficient);
            Cache.GeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

            var prescanAODWaveformResult = AODWaveformGenerator1.GeneratePrescanAODWaveform(Cache.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);

            item.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(prescanAODWaveformResult);
            item.PrescanAODWaveformResultFilePath = prescanAODWaveformResult.FilePath;

            item.ChirpAODWaveformProfiles = [];
            item.ChirpAODWaveformResultFilePath = string.Empty;

            Cache.GenerateChirpAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
            Cache.GenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
            Cache.GenerateChirpAODWaveformParam.SlopeDeltaKConfigurations = [..item.SlopeDeltaKConfigurations.Select(t => t.Clone())];

            var chirpAODWaveformResult = AODWaveformGenerator1.GenerateChirpAODWaveform(Cache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);

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
                (false, Cache.OpticsConfiguration),
                (false, Cache.CIBConfiguration),
                (true, null),
                true,
                cancellationToken);

            var bestFocus = calibrationAlgorithmService.GetBestFocus(darkFieldImage.Image, startECS, stopECS);

            item.BestFocus = bestFocus;
            item.BestFocus.RawImageFilePath = darkFieldImage.RawImageFilePath;

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
                XStrehlRatioScatterPlotControl = new HtmlContainer([.. Cache.Item.BestFocus.XStrehlRatioScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]),
                YStrehlRatioScatterPlotControl = new HtmlContainer([.. Cache.Item.BestFocus.YStrehlRatioScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
            }), htmlLogUniqueId.LoggingHtml());
        }
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