using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Cookies;
using Core.Services.Interfaces;
using Core.Models;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.SQL.Cache.Providers.Services.Interfaces;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Extensions;
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
using System.Collections;
using System.IO;
using System.Text;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(ChirpAODWaveformTrainingWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChirpAODWaveformTrainingWindowViewModel(
    CIBViewModel cibViewModel,
    LaserViewModel laserViewModel,
    MicroscopeViewModel microscopeViewModel,
    StageViewModel stageViewModel,
    AlignmentUserControlViewModel alignmentUserControlViewModel,
    ICacheProvider cacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    IOptions<ApplicationSetting> options,
    ApplicationCookie applicationCookie,
    ILogger<ChirpAODWaveformTrainingWindowViewModel> logger) : ViewModelBase
{
    private readonly AsyncAutoResetEvent _asyncAutoResetEvent = new(false);

    public ApplicationCookie ApplicationCookie => applicationCookie;

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = alignmentUserControlViewModel;

    public string Name => "Chirp AOD Waveform Training";

    public IReadOnlyList<string> Steps { get; } =
    [
        "Step 1 Alignment",
        "Step 2 Mark",
        "Step 3 Training"
    ];

    public Guid HtmlLogUniqueId { get; set; }

    public string AODWaveformDirectoryPath => Path.Combine(options.Value.AppHomeDirectory, "AODWaveform", nameof(ChirpAODWaveformTrainingWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    [DefaultCache]
    [ObservableProperty]
    public partial ChirpAODWaveformTrainingCache Cache { get; set; } = new();

    [RelayCommand]
    private async Task LoadedAsync() => await Task.Run(() => Cache = cacheProvider.GetOrDefault<ChirpAODWaveformTrainingCache>());

    [RelayCommand]
    private void Continue()
    {
        _asyncAutoResetEvent.Set();
    }

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
                                             {Name}: Import Parameters Failed
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "{@Name}: Import Parameters Failed", Name);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0Async(bool isSilent, CancellationToken cancellationToken)
    {
        return await InvokeAsync(0, async () =>
        {
            AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
            AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;

            dialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?",
                out var dialogResult,
                DialogButtonsEnum.YesNo,
                DialogIconEnum.Question);

            AlignmentUserControlViewModel.IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;

            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            var alignmentResult = AlignmentUserControlViewModel.AlignmentResult;

            logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                CalChipSiteModelEnum.DswModel,
                AlignmentUserControlViewModel.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(alignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            stageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(stageViewModel.MachineToBrightFieldPosition((alignmentResult.MarkPoint1 + (Vector)alignmentResult.MarkPoint2) / 2d));

            Cache.AlignmentResult = alignmentResult;

            logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                AlignmentResult = new HtmlQuote(Cache.AlignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        }, isSilent).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1Async(bool isSilent, CancellationToken cancellationToken)
    {
        return await InvokeAsync(1, () =>
        {
            Cache.MicroscopeLensInformation = microscopeViewModel.GetCurrentMicroscopeLensInformation();
            Cache.DSWFindBFMachinePosition = stageViewModel.GetMachineStagePosition();

            logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation,
                Cache.DSWFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return Task.FromResult(true);
        }, isSilent).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2Async(bool isSilent, CancellationToken cancellationToken)
    {
        return await InvokeAsync(2, async () =>
        {
            if (Cache.ChirpAODWaveformTrainingSlopes.Count <= 0)
            {
                dialogWindowProvider.ShowDialog("Slopes is Empty", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return false;
            }

            if ((Cache.ChirpAODWaveformTrainingSlopes.Count & 1) == 0)
            {
                dialogWindowProvider.ShowDialog("Slopes count must be odd.", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return false;
            }

            Cache.Items = [];

            microscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
            var dswBFPosition = stageViewModel.MachineToBrightFieldPosition(Cache.DSWFindBFMachinePosition);
            stageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(dswBFPosition);

            logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache = new HtmlQuote(Cache.ToHtmlAnonymous()),
                dswBFPosition
            }), HtmlLogUniqueId.LoggingHtml());

            Cache.Item = new ChirpAODWaveformTrainingItem();
            Cache.SlopeConfigurations =
            [
                .. Cache.ChirpAODWaveformTrainingSlopes.Select(_ => new GenerateAODWaveformSlopeConfiguration
                {
                    DeltaKRate = 0d,
                    Coefficient = 0d
                })
            ];

            try
            {
                foreach (var index in EnumerateFromCenter(Cache.ChirpAODWaveformTrainingSlopes.Count)) await TrainingAsync(dswBFPosition, index, cancellationToken);
            }
            finally
            {
                stageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(dswBFPosition);
            }

            return true;

            static IEnumerable<int> EnumerateFromCenter(int length)
            {
                if (length <= 0) yield break;

                var center = length / 2;
                yield return center;

                for (var offset = 1; ; offset++)
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
        }, isSilent).ConfigureAwait(false);
    }


    [RelayCommand(IncludeCancelCommand = true)]
    private async Task AllAsync(CancellationToken cancellationToken)
    {
#if NET
        await
#endif
        using var _ = cancellationToken.Register(() =>
        {
            if (Step0Command.CanBeCanceled) Step0Command.Cancel();
            if (Step2Command.CanBeCanceled) Step2Command.Cancel();
        });

        var oldMarkPoint1 = Cache.AlignmentResult.MarkPoint1;
        var oldMarkPoint2 = Cache.AlignmentResult.MarkPoint1;

        var step0Task = Guard.IsAssignableToTypeAndReturn<Task<bool>>(Step0Command.ExecuteAsync(true));
        if (await step0Task.ConfigureAwait(true) == false) return;

        var newMarkPoint1 = Cache.AlignmentResult.MarkPoint1;
        var newMarkPoint2 = Cache.AlignmentResult.MarkPoint1;

        var offset = (newMarkPoint1 - oldMarkPoint1 + (newMarkPoint2 - oldMarkPoint2)) / 2d;
        var oldDSWFindBFMachinePosition = Cache.DSWFindBFMachinePosition;
        Cache.DSWFindBFMachinePosition += offset;

        logger.LogHtmlInformation(Steps[1], HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
        {
            offset,
            oldDSWFindBFMachinePosition,
            Cache.DSWFindBFMachinePosition
        }), HtmlLogUniqueId.LoggingHtml());

        await Step2Command.ExecuteAsync(true).ConfigureAwait(false);
    }

    [RelayCommand]
    private void Close()
    {
        try
        {
            AllCancelCommand.Execute(null);

            Step2CancelCommand.Execute(null);
            Step1CancelCommand.Execute(null);
            Step0CancelCommand.Execute(null);

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            cacheProvider.Set(Cache, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save cache");
        }

        CloseView(null);
    }

    private async Task TrainingAsync(Point dswBFPosition, int index, CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            var chirpAODWaveformTrainingSlope = Cache.ChirpAODWaveformTrainingSlopes[index];

            logger.LogHtmlInformation($"Training: {index + 1}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            var pointXStrehlRatioList = new List<Point>();
            var pointXECSList = new List<Point>();
            var pointYStrehlRatioList = new List<Point>();
            var pointYECSList = new List<Point>();
            try
            {
                foreach (var deltaKRate in Generate.LinearRangeContainsEdge(chirpAODWaveformTrainingSlope.StartDeltaKRate, chirpAODWaveformTrainingSlope.StepDeltaKRate, chirpAODWaveformTrainingSlope.StopDeltaKRate))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    logger.LogHtmlInformation($"{index + 1}: {deltaKRate:0.######}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    var temp = Cache.SlopeConfigurations.ToArray();
                    Cache.SlopeConfigurations = temp;
                    temp[index] = new GenerateAODWaveformSlopeConfiguration
                    {
                        DeltaKRate = deltaKRate,
                        Coefficient = 1d
                    };

                    var currentItem = await CatchImagesAsync(dswBFPosition, temp, cancellationToken);

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

                isSuccess = true;
            }
            finally
            {
                var htmlQuote = new HtmlQuote(new
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
                    ReultECS = new HtmlPlot2DLinesChart([("X", pointXECSList), ("Y", pointYECSList)], "X: PMT Id - Y: ECS")
                });

                if (isSuccess) logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlQuote, HtmlLogUniqueId.LoggingHtml());
                else logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlQuote, HtmlLogUniqueId.LoggingHtml());
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ChirpAODWaveformTrainingItem> CatchImagesAsync(
        Point dswBFPosition,
        IReadOnlyList<GenerateAODWaveformSlopeConfiguration> slopeConfigurations,
        CancellationToken cancellationToken)
    {
        var item = new ChirpAODWaveformTrainingItem
        {
            ProductivityInformation = Cache.ProductivityInformation,
            LaserLightInformation = Cache.LaserLightInformation,
            CIBInformation = Cache.CIBInformation,
            SlopeConfigurations = [.. slopeConfigurations.Select(t => t.Clone())]
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
            Cache.GenerateChirpAODWaveformParam.SlopeConfigurations = [.. item.SlopeConfigurations.Select(t => t.Clone())];

            var chirpAODWaveformResult = AODWaveformGenerator1.GenerateChirpAODWaveform(Cache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);

            item.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(chirpAODWaveformResult);
            item.ChirpAODWaveformResultFilePath = chirpAODWaveformResult.FilePath;

            laserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, item.PrescanAODWaveformProfiles);
            laserViewModel.SetChirpAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, item.ChirpAODWaveformProfiles);

            cancellationToken.ThrowIfCancellationRequested();

            var currentStartPosition = cibViewModel.GetCIBInformationPosition(
                StageCoordinateSystemEnum.Dark,
                Cache.ProductivityInformation,
                Cache.CIBInformation,
                dswBFPosition,
                Cache.MicroscopeLensInformation);
            var currentStopPosition = currentStartPosition + new Vector(Cache.ScanLength, 0);

            var startECS = Cache.CenterECS - Cache.RangeECS;
            var stopECS = Cache.CenterECS + Cache.RangeECS;
            using var darkFieldImage = await cibViewModel.GetPMTImageAsync(
                item.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                currentStartPosition,
                currentStopPosition,
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
            }), HtmlLogUniqueId.LoggingHtml());
        }
    }

    private async Task<bool> InvokeAsync(
        int stepIndex,
        Func<Task<bool>> func,
        bool isSilent)
    {
        return await Task.Run(async () =>
        {
            var isInitHtmlLog = isSilent == false || stepIndex == 0;
            var isEndHtml = isSilent == false || stepIndex == Steps.Count - 1;

            HtmlLogUniqueId = isInitHtmlLog ? Guid.NewGuid() : HtmlLogUniqueId;

            if (isInitHtmlLog) logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation(Steps[stepIndex], HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                isSuccess = await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (isSilent) isEndHtml = true;

                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog($"{Name}: {Steps[stepIndex]} Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    return false;
                }

                dialogWindowProvider.ShowDialog($"""
                                                 {Name}: {Steps[stepIndex]} Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogHtmlError(ex, Name, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            }
            finally
            {
                if (isEndHtml)
                    logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{(isSilent ? "All" : Steps[stepIndex].Replace(" ", string.Empty))}_{(isSuccess ? "OK" : "Failed")}"));
            }

            if (isSuccess)
            {
                if (isEndHtml) dialogWindowProvider.ShowDialog($"{Name}: {(isSilent ? "All" : Steps[stepIndex])} Success");
            }
            else
                dialogWindowProvider.ShowDialog($"{Name}: {Steps[stepIndex]} Error", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return isSuccess;
        }).ConfigureAwait(false);
    }
}