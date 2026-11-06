using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Setting;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Editors.Getters.Options;
using Net.Utilities.Graphics.Primitives.Enums.Editors;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;
using Core.Algorithm.Extensions;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.Common.StageMap;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

[IOCAppService(ServiceType = typeof(StageMapWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class StageMapWindowViewModel(
    StageViewModel stageViewModel,
    AlignmentUserControlViewModel alignmentUserControlViewModel,
    ICacheProvider cacheProvider,
    IApplicationCookieService applicationCookieCacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    IOptions<ApplicationSetting> options,
    ApplicationCookie applicationCookie,
    CalibrationSetting calibrationSetting,
    StageMapCommands stageMapCommands,
    ILogger<StageMapWindowViewModel> logger) : ViewModelBase
{
    public string Name { get; } = "StageMap";

    public CalibrationSetting CalibrationSetting { get; } = calibrationSetting;

    public ApplicationCookie ApplicationCookie { get; } = applicationCookie;

    public AlignmentUserControlViewModel AlignmentUserControlViewModel { get; } = alignmentUserControlViewModel;

    public StageMapCommands StageMapCommands { get; } = stageMapCommands;

    public string ImageFileDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", nameof(StageMapWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public string TemplateFileDirectory => Path.Combine(options.Value.AppHomeDirectory, "Template", nameof(StageMapWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public string[] Steps { get; } =
    [
        "Step 1 Alignment",
        "Step 2 Generate Wafer Map",
        "Step 3 Scan",
        "Step 4 Repeat",
        "Step 5 Verify"
    ];

    public Guid HtmlLogUniqueId { get; private set; }

    [DefaultCache]
    [ObservableProperty]
    public partial StageMapCache Cache { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await Task.Run(() =>
        {
            Cache = cacheProvider.GetOrDefault<StageMapCache>();

            MicroscopeCalChip = applicationCookieCacheProvider.GetCalibration<MicroscopeCalChipDTO>();

            StageMapCommands.IsDark = false;
            StageMapCommands.ImageFileDirectory = ImageFileDirectory;
            StageMapCommands.TemplateFileDirectory = TemplateFileDirectory;
            StageMapCommands.Cache = Cache.ChuckStageMapCache;
            StageMapCommands.ChuckStageMap = Cache.ChuckStageMap;

            StageMapCommands.StageMap.Refresh();
            foreach (var cacheRepeatStageMap in Cache.ChuckStageMap.DFRepeatStageMaps) cacheRepeatStageMap.Refresh();
            StageMapCommands.VerifyStageMap.Refresh();
        }).ConfigureAwait(true);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(0, async () =>
    {
        var cacheDFItem = (ChuckStageMapCacheDFItem)StageMapCommands.CacheItem;

        Guard.IsTrue(MicroscopeCalChip.IsOk, "Microscope Cal Chip is Empty or not verify.");

        AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
        AlignmentUserControlViewModel.ProductivityInformation = cacheDFItem.ProductivityInformation;

        if (dialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment?",
                out var dialogResult,
                DialogButtonsEnum.YesNo,
                DialogIconEnum.Question) == true && dialogResult == DialogResultEnum.Yes) StageMapCommands.ChuckStageMap.IsDarkFieldAlignment = AlignmentUserControlViewModel.IsDarkFieldAlignment = true;
        else StageMapCommands.ChuckStageMap.IsDarkFieldAlignment = AlignmentUserControlViewModel.IsDarkFieldAlignment = false;

        await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

        var newAlignmentResult = AlignmentUserControlViewModel.AlignmentResult;
        if (StageMapCommands.AlignmentResult.MarkPoint1 != Point.Origin
            && StageMapCommands.AlignmentResult.MarkPoint2 != Point.Origin
            && StageMapCommands.CacheItem.StageMapTemplates.Length > 0)
        {
            var oldMarkPoint1 = StageMapCommands.AlignmentResult.MarkPoint1;
            var oldMarkPoint2 = StageMapCommands.AlignmentResult.MarkPoint2;

            var newMarkPoint1 = newAlignmentResult.MarkPoint1;
            var newMarkPoint2 = newAlignmentResult.MarkPoint2;

            var offset = (newMarkPoint1 - oldMarkPoint1 + (newMarkPoint2 - oldMarkPoint2)) / 2d;

            foreach (var stageMapTemplate in StageMapCommands.CacheItem.StageMapTemplates) stageMapTemplate.FindBFMachinePosition += offset;

            using var scope = StageMapCommands.StageMapDocument.View.Sync.EnterScope();
            StageMapCommands.StageMapDocument.Reset();
        }

        StageMapCommands.AlignmentResult = newAlignmentResult;

        stageViewModel.SetBrightFieldAbsoluteStageXy(stageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), CalChipSiteModelEnum.ChuckModel);

        logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            AlignmentUserControlViewModel.CalChipSiteModelEnum,
            AlignmentUserControlViewModel.IsDarkFieldAlignment,
            AlignmentResult = new HtmlQuote(StageMapCommands.AlignmentResult)
        }), HtmlLogUniqueId.LoggingHtml());

        return true;
    }, isSilent, cancellationToken).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(1, async () =>
    {
        await StageMapCommands.GenerateStageMapDocumnetAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }, isSilent, cancellationToken).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(2, async () =>
    {
        stageViewModel.SetEnableStageMap(false);

        StageMapDie[] stageMapDies;
        using (StageMapCommands.StageMapDocument.View.Sync.EnterScope()) stageMapDies = [.. StageMapCommands.StageMapDocument.DieModel];

        foreach (var stageMapDie in stageMapDies) stageMapDie.IsInStageMapScan = false;

        StageMapDie[] selectStageMapDies;
        if (dialogWindowProvider.TryShowDialog("Yes: Select all dies? No: Manually select dies?",
                out var dialogResult,
                DialogButtonsEnum.YesNo,
                DialogIconEnum.Question) == true && dialogResult == DialogResultEnum.Yes)
        {
            selectStageMapDies = [.. stageMapDies];
        }
        else
        {
            var inputResult = await StageMapDieSelectionGetter
                .RunAsync<StageMapDieSelectionGetter>(StageMapCommands.StageMapDocument.Edit, new SelectionInputOptions<StageMapDie>
                {
                    IsMultipleSelection = true,
                    CancellationToken = cancellationToken
                })
                .ConfigureAwait(false);
            Guard.IsTrue(inputResult.OutputResultModeEnum == OutputResultModeEnum.Ok);

            selectStageMapDies = [.. inputResult.Output];
        }

        await StageMapCommands.GenerateStageMapAsync(selectStageMapDies, cancellationToken).ConfigureAwait(false);

        await StageMapCommands.ScanStageMapAsync(
            StageMapCommands.StageMap,
            HtmlLogUniqueId,
            cancellationToken).ConfigureAwait(false);

        logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

        StageMapCommands.StageMap.CalculateErrorMatrix();
        StageMapCommands.StageMap.Refresh();

        logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
            .. StageMapCommands.StageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
            .. StageMapCommands.StageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
        ]), HtmlLogUniqueId.LoggingHtml());

        return true;
    }, isSilent, cancellationToken).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step3Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(3, async () =>
    {
        Cache.ChuckStageMap.DFRepeatStageMaps = [];
        var historyStageMapList = new List<StageMap>();

        var circle = new Circle(stageViewModel.DarkFieldToMachinePosition(stageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel))), StageMapCommands.CacheItem.WaferRadius);

        bool isSuccess;
        var times = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentHtmlLogUniqueId = Guid.NewGuid();
            var fileName = $"Details_{Steps[3].Replace(" ", string.Empty)}_{times + 1}";

            if (times == 0)
            {
                await StageMapCommands.DownloadStageMapAsync(
                    StageMapCommands.StageMap,
                    circle,
                    currentHtmlLogUniqueId,
                    cancellationToken).ConfigureAwait(false);
            }

            logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {fileName}({currentHtmlLogUniqueId:N})"), HtmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation($"{currentHtmlLogUniqueId:N}", HtmlHeaderLevelEnum.Header1, new HtmlComment(Name), currentHtmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header3, currentHtmlLogUniqueId.LoggingHtml());

            try
            {
                var scanStageMap = StageMapCommands.StageMap.Clone();
                scanStageMap.Reset();

                Cache.ChuckStageMap.DFRepeatStageMaps = [.. Cache.ChuckStageMap.DFRepeatStageMaps, scanStageMap];

                await StageMapCommands.ScanStageMapAsync(
                    scanStageMap,
                    currentHtmlLogUniqueId,
                    cancellationToken,
                    isInterpolateErrors: true,
                    interpolateStageMap: StageMapCommands.StageMap,
                    interpolateCircle: circle).ConfigureAwait(false);

                historyStageMapList.Add(scanStageMap.Clone());

                logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, currentHtmlLogUniqueId.LoggingHtml());

                isSuccess = scanStageMap.CanRepeatAndCalculateErrorMatrix(historyStageMapList, Cache.ChuckStageMapCache.ChuckStageMapCacheDFItem.StageMapRepeatTimes);

                scanStageMap.Refresh();

                logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
                    .. scanStageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                    .. scanStageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
                ]), currentHtmlLogUniqueId.LoggingHtml());

                if (isSuccess || ++times > Cache.ChuckStageMapCache.ChuckStageMapCacheDFItem.StageMapRepeatTimes - 1)
                {
                    StageMapCommands.StageMap.MergeErrorMatrix(scanStageMap, Cache.ChuckStageMapCache.ChuckStageMapCacheDFItem.StageMapTemplates.Length);

                    StageMapCommands.StageMap.Refresh();

                    if (isSuccess)
                        logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlContainer([
                            .. StageMapCommands.StageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                            .. StageMapCommands.StageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
                        ]), HtmlLogUniqueId.LoggingHtml());
                    else
                        logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlContainer([
                            new HtmlComment("More than the number of times."),
                            .. StageMapCommands.StageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                            .. StageMapCommands.StageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
                        ]), HtmlLogUniqueId.LoggingHtml());

                    break;
                }
            }
            finally
            {
                logger.LogHtmlInformation(currentHtmlLogUniqueId.LoggedEndHtml(fileName));
            }
        }

        return isSuccess;
    }, isSilent, cancellationToken).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step4Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(4, async () =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        await StageMapCommands.DownloadStageMapAsync(
            StageMapCommands.StageMap,
            new Circle(stageViewModel.DarkFieldToMachinePosition(stageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel))), StageMapCommands.CacheItem.WaferRadius),
            HtmlLogUniqueId,
            cancellationToken).ConfigureAwait(false);

        StageMapCommands.VerifyStageMap = StageMapCommands.StageMap.Clone();
        StageMapCommands.VerifyStageMap.Reset();

        await StageMapCommands.ScanStageMapAsync(
            StageMapCommands.VerifyStageMap,
            HtmlLogUniqueId,
            cancellationToken,
            isInterpolateErrors: true).ConfigureAwait(false);

        logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

        logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

        StageMapCommands.VerifyStageMap.CalculateErrorMatrix();
        StageMapCommands.VerifyStageMap.Refresh();

        logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
            .. StageMapCommands.VerifyStageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
            .. StageMapCommands.VerifyStageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
        ]), HtmlLogUniqueId.LoggingHtml());

        return true;
    }, isSilent, cancellationToken).ConfigureAwait(false);

    [RelayCommand]
    private void Close()
    {
        try
        {
            Step4CancelCommand.Execute(null);
            Step3CancelCommand.Execute(null);
            Step2CancelCommand.Execute(null);
            Step1CancelCommand.Execute(null);
            Step0CancelCommand.Execute(null);

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            Cache.Id = 0;
            cacheProvider.Set(Cache, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save cache");
        }

        CloseView(null);
    }

    private async Task<bool> InvokeAsync(
        int stepIndex,
        Func<Task<bool>> func,
        bool isSilent,
        CancellationToken cancellationToken)
    {
        return await Task.Run(async () =>
        {
            var isInitHtmlLog = isSilent == false || stepIndex == 0;
            var isEndHtml = isSilent == false || stepIndex == Steps.Length - 1;

            HtmlLogUniqueId = isInitHtmlLog ? Guid.NewGuid() : HtmlLogUniqueId;

            if (isInitHtmlLog) logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation(Steps[stepIndex], HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Cache.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                isSuccess = await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    if (isSilent) isEndHtml = true;

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
                if (isEndHtml || isSuccess == false)
                    logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{(isSilent ? "All" : Steps[stepIndex].Replace(" ", string.Empty))}_{(isSuccess ? "OK" : "Failed")}"));
            }

            if (isSuccess)
            {
                if (isEndHtml) dialogWindowProvider.ShowDialog($"{Name}: {(isSilent ? "All" : Steps[stepIndex])} Success");
            }
            else
                dialogWindowProvider.ShowDialog($"{Name}: {Steps[stepIndex]} Error", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return isSuccess;
        }, cancellationToken).ConfigureAwait(false);
    }
}