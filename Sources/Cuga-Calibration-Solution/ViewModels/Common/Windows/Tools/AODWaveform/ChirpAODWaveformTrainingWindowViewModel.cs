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
using CommunityToolkit.Diagnostics;
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
    private void AddChirpAODWaveformTrainingSlope()
    {
        var trainingList = Cache.ChirpAODWaveformTrainingSlopes.ToList();
        trainingList.Add(new ChirpAODWaveformTrainingSlope());

        Cache.ChirpAODWaveformTrainingSlopes = trainingList;
    }

    [RelayCommand]
    private void RemoveChirpAODWaveformTrainingSlope(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var trainingList = Cache.ChirpAODWaveformTrainingSlopes.ToList();
        foreach (ChirpAODWaveformTrainingSlope selectItem in selectItems) trainingList.Remove(selectItem);

        Cache.ChirpAODWaveformTrainingSlopes = trainingList;
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
            if (Cache.ChirpAODWaveformTrainingSlopes.Count <= 0)
            {
                dialogWindowProvider.ShowDialog("DeltaKs is Empty", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return;
            }

            if ((Cache.ChirpAODWaveformTrainingSlopes.Count & 1) == 0)
            {
                dialogWindowProvider.ShowDialog("DeltaKs count must be odd.", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return;
            }

            Cache.Item = new ChirpAODWaveformTrainingItem();
            Cache.SlopeConfigurations =
            [
                ..Cache.ChirpAODWaveformTrainingSlopes.Select(_ => new GenerateAODWaveformSlopeConfiguration
                {
                    DeltaKRate = 0d,
                    Coefficient = 0d
                })
            ];

            foreach (var index in EnumerateFromCenter(Cache.ChirpAODWaveformTrainingSlopes.Count))
            {
                if (await TrainingAsync((Cache.ChirpAODWaveformTrainingSlopes[index], true), cancellationToken) == false) return;
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
    private async Task<bool> TrainingAsync((ChirpAODWaveformTrainingSlope ChirpAODWaveformTrainingSlope, bool isSilent)? valueTuple, CancellationToken cancellationToken)
    {
        return await Task.Run(async () =>
        {
            Guard.IsNotNull(valueTuple);
            var (chirpAODWaveformTrainingSlope, isSilent) = valueTuple.Value;

            var htmlLogUniqueId = Guid.NewGuid();

            var index = Cache.ChirpAODWaveformTrainingSlopes.Index().Single(t => ReferenceEquals(t.Item, chirpAODWaveformTrainingSlope)).Index;

            logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, htmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation($"Training: {index + 1}", HtmlHeaderLevelEnum.Header2, htmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Cache.ToHtmlAnonymous()), htmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                Cache.Items = [];

                cancellationToken.ThrowIfCancellationRequested();

                var pointXStrehlRatioList = new List<Point>();
                var pointXECSList = new List<Point>();
                var pointYStrehlRatioList = new List<Point>();
                var pointYECSList = new List<Point>();
                foreach (var deltaKRate in Generate.LinearRangeContainsEdge(chirpAODWaveformTrainingSlope.StartDeltaKRate, chirpAODWaveformTrainingSlope.StepDeltaKRate, chirpAODWaveformTrainingSlope.StopDeltaKRate))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    logger.LogHtmlInformation($"{index + 1}: {deltaKRate:0.######}", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

                    var temp = Cache.SlopeConfigurations.ToArray();
                    Cache.SlopeConfigurations = temp;
                    temp[index] = new GenerateAODWaveformSlopeConfiguration
                    {
                        DeltaKRate = deltaKRate,
                        Coefficient = 1d
                    };

                    var currentItem = await CatchImagesAsync(temp, htmlLogUniqueId, cancellationToken);

                    pointXStrehlRatioList.Add(new Point(deltaKRate, currentItem.BestFocus.BestXStrehlRatioPoint.Y));
                    pointXECSList.Add(new Point(deltaKRate, currentItem.BestFocus.BestXStrehlRatioECS));
                    pointYStrehlRatioList.Add(new Point(deltaKRate, currentItem.BestFocus.BestYStrehlRatioPoint.Y));
                    pointYECSList.Add(new Point(deltaKRate, currentItem.BestFocus.BestYStrehlRatioECS));

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
                    YStrehlRatioScatterPlotControl = new HtmlContainer([.. Cache.Item.BestFocus.YStrehlRatioScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]),
                    ReultStrehlRatio = new HtmlPlot2DLinesChart([("X", pointXStrehlRatioList), ("Y", pointYStrehlRatioList)], "X: PMT Id - Y: Best Strehl Ratio"),
                    ReultECS = new HtmlPlot2DLinesChart([("X", pointXECSList), ("Y", pointYECSList)], "X: PMT Id - Y: ECS"),
                }), htmlLogUniqueId.LoggingHtml());

                isSuccess = true;

                if (isSilent == false) dialogWindowProvider.ShowDialog($"Training {index + 1} Success");
            }
            catch (Exception ex)
            {
                isSuccess = false;

                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog($"{Name}: Training {index + 1} Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());

                    return isSuccess;
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
        IReadOnlyList<GenerateAODWaveformSlopeConfiguration> slopeConfigurations,
        Guid htmlLogUniqueId,
        CancellationToken cancellationToken)
    {
        var item = new ChirpAODWaveformTrainingItem
        {
            ProductivityInformation = Cache.ProductivityInformation,
            LaserLightInformation = Cache.LaserLightInformation,
            CIBInformation = Cache.CIBInformation,
            SlopeConfigurations = [..slopeConfigurations.Select(t => t.Clone())]
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
            Cache.GenerateChirpAODWaveformParam.SlopeConfigurations = [..item.SlopeConfigurations.Select(t => t.Clone())];

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

            try
            {
                var bestFocus = calibrationAlgorithmService.GetBestFocus(darkFieldImage.Image, startECS, stopECS);
                item.BestFocus = bestFocus;
            }
            finally
            {
                item.BestFocus.RawImageFilePath = darkFieldImage.RawImageFilePath;
                Cache.Items = [.. Cache.Items, item];

                Cache.SelectedItem = item;
            }

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
                XStrehlRatioScatterPlotControl = new HtmlContainer([.. item.BestFocus.XStrehlRatioScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]),
                YStrehlRatioScatterPlotControl = new HtmlContainer([.. item.BestFocus.YStrehlRatioScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
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