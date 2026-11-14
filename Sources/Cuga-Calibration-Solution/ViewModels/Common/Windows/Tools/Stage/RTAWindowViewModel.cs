using System.Globalization;
using System.IO;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Core.Models.Enums.Stage;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.CIB.XPixelSize;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Microscope.CalChip;
using Core.Services.Interfaces;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Editors.Getters.Options;
using Net.Utilities.Graphics.Primitives.Enums.Editors;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using Core.Models.Models.Common.StageMap;
using Microsoft.Extensions.Options;
using Net.Utilities.Models;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

[IOCAppService(ServiceType = typeof(RTAWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class RTAWindowViewModel(
    StageViewModel stageViewModel,
    CIBViewModel cibViewModel,
    ICacheProvider cacheProvider,
    IApplicationCookieService applicationCookieCacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    IOptions<ApplicationSetting> options,
    ApplicationCookie applicationCookie,
    StageMapCommands stageMapCommands,
    ILogger<RTAWindowViewModel> logger,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    IHostEnvironment hostEnvironment) : ViewModelBase
{
    public string Name { get; } = "RTA";

    public bool IsDevelopment { get; } = hostEnvironment.IsDevelopment();

    public ApplicationCookie ApplicationCookie { get; } = applicationCookie;

    public StageMapCommands StageMapCommands { get; } = stageMapCommands;

    public string ImageFileDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", nameof(StageMapWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat, CultureInfo.InvariantCulture));

    public string TemplateFileDirectory => Path.Combine(options.Value.AppHomeDirectory, "Template", nameof(StageMapWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat, CultureInfo.InvariantCulture));

    public string[] Steps { get; } =
    [
        "Step 1 Alignment",
        "Step 2 Generate Wafer Map",
        "Step 3 Scan"
    ];

    public Guid HtmlLogUniqueId { get; private set; }

    [DefaultCache]
    [ObservableProperty]
    public partial RTACache Cache { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await Task.Run(() =>
        {
            Cache = cacheProvider.GetOrDefault<RTACache>();

            MicroscopeCalChip = applicationCookieCacheProvider.GetCalibration<MicroscopeCalChipDTO>();

            StageMapCommands.IsDark = false;
            StageMapCommands.Logger = logger;
            StageMapCommands.ImageFileDirectory = ImageFileDirectory;
            StageMapCommands.TemplateFileDirectory = TemplateFileDirectory;
            StageMapCommands.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum;
            StageMapCommands.MicroscopeCalChip = MicroscopeCalChip;
            StageMapCommands.Cache = Cache.ChuckStageMapCache;
            StageMapCommands.ChuckStageMap = Cache.ChuckStageMap;
        }).ConfigureAwait(true);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(0, async () =>
    {
        await StageMapCommands.AlignmentThenApplyTemplatesAsync(HtmlLogUniqueId, cancellationToken).ConfigureAwait(false);

        return true;
    }, isSilent, cancellationToken).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(1, async () =>
    {
        await StageMapCommands.GenerateStageMapDocumentAsync(HtmlLogUniqueId, cancellationToken).ConfigureAwait(false);

        return true;
    }, isSilent, cancellationToken).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(2, async () =>
    {
        var cacheDFItem = (ChuckStageMapCacheDFItem)StageMapCommands.CacheItem;

        var xSize = applicationCookieCacheProvider.GetCalibrations<CIBXPixelSizeDTO>(cancellationToken).SingleOrDefault(t => t.ProductivityInformation == cacheDFItem.ProductivityInformation);
        Guard.IsTrue(xSize?.IsOk == true, "Laser X Pixel Size is Empty or not verify.");

        var ySize = applicationCookieCacheProvider.GetCalibrations<CIBYPixelSizeDTO>(cancellationToken).SingleOrDefault(t => t.ProductivityInformation.OpticsIlluminationModeEnum == cacheDFItem.ProductivityInformation.OpticsIlluminationModeEnum
                                                                                                                             && t.ProductivityInformation.OpticsMagType == cacheDFItem.ProductivityInformation.OpticsMagType
                                                                                                                             && t.PmtId == cacheDFItem.CIBInformation.PMTId);
        Guard.IsTrue(ySize?.IsOk == true, "Laser Y Pixel Size is Empty or not verify.");

        logger.LogHtmlInformation("Productivity Information", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            ProductivityInformation = new HtmlQuote(cacheDFItem.ProductivityInformation.ToHtmlAnonymous()),
            xSize.XPixelSize,
            ySize.YPixelSize
        }), HtmlLogUniqueId.LoggingHtml());

        Cache.RTAScanResult = new RTAScanResult(cacheDFItem.ProductivityInformation, xSize.XPixelSize, ySize.YPixelSize);

        dialogWindowProvider.ShowDialog("Please Select Dies to Scan");

        StageMapDie[] stageMapDies;
        using (StageMapCommands.StageMapDocument.View.Sync.EnterScope()) stageMapDies = [.. StageMapCommands.StageMapDocument.DieModel];

        foreach (var stageMapDie in stageMapDies) stageMapDie.IsInStageMapScan = false;

        var inputResult = await StageMapDieSelectionGetter
            .RunAsync<StageMapDieSelectionGetter>(StageMapCommands.StageMapDocument.Edit, new SelectionInputOptions<StageMapDie>
            {
                IsMultipleSelection = true,
                CancellationToken = cancellationToken
            })
            .ConfigureAwait(false);
        Guard.IsTrue(inputResult.OutputResultModeEnum == OutputResultModeEnum.Ok);

        var (xDirection, _) = stageViewModel.GetMachineDirection();
        StageMapDie[] selectStageMapDies = xDirection > 0
            ?
            [
                .. inputResult.Output
                    .OrderBy(t => t.Index.Y)
                    .ThenBy(t => t.Index.X)
            ]
            :
            [
                .. inputResult.Output
                    .OrderBy(t => t.Index.Y)
                    .ThenByDescending(t => t.Index.X)
            ];
        Guard.IsNotEmpty(selectStageMapDies);

        if (selectStageMapDies.DistinctBy(t => t.Row).Count() > 1)
        {
            dialogWindowProvider.ShowDialog("Only One Row Can Be Selected!", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return false;
        }

        if (selectStageMapDies.Length < 2)
        {
            dialogWindowProvider.ShowDialog("At Least Two Dies Must Be Selected!", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return false;
        }

        foreach (var stageMapDie in selectStageMapDies) stageMapDie.IsInStageMapScan = true;

        var startPosition = selectStageMapDies[0].Rect.Point;
        var endPosition = selectStageMapDies[^1].Rect.Point;

        var isSuccess = false;
        var forwardDarkFieldRawScanImage = new DarkFieldRawScanImageDTO();
        var reverseDarkFieldRawScanImage = new DarkFieldRawScanImageDTO();
        try
        {
            logger.LogHtmlInformation("Forward", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            if (IsDevelopment) cibViewModel.UseSimulatorImages(string.IsNullOrWhiteSpace(Cache.ForwardSimulatorImageFilePath) ? null : [Cache.ForwardSimulatorImageFilePath]);

            cancellationToken.ThrowIfCancellationRequested();
            stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(startPosition, calChipSiteModelEnum: Cache.CalChipSiteModelEnum);
            forwardDarkFieldRawScanImage = await cibViewModel.GetPMTImageAsync(
                cacheDFItem.ProductivityInformation,
                StageCoordinateSystemEnum.Machine,
                startPosition,
                endPosition,
                cacheDFItem.CIBInformation,
                (true, null),
                (false, cacheDFItem.OpticsConfiguration),
                (false, cacheDFItem.CIBConfiguration),
                (false, cacheDFItem.LaserLightInformation),
                false,
                cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            var forwardRTAScanSamples = calibrationAlgorithmService.GetRTAScanSamples(
                forwardDarkFieldRawScanImage,
                Cache.StageMapConfig,
                true,
                Cache.IsRemoveLinear,
                HtmlLogUniqueId);
            Guard.IsNotEmpty(forwardRTAScanSamples);
            Cache.RTAScanResult.ForwardRTAScanSamples = forwardRTAScanSamples;

            logger.LogHtmlInformation("Reverse", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            if (IsDevelopment) cibViewModel.UseSimulatorImages(string.IsNullOrWhiteSpace(Cache.ReverseSimulatorImageFilePath) ? null : [Cache.ReverseSimulatorImageFilePath]);

            cancellationToken.ThrowIfCancellationRequested();
            stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(endPosition, calChipSiteModelEnum: Cache.CalChipSiteModelEnum);
            reverseDarkFieldRawScanImage = await cibViewModel.GetPMTImageAsync(
                cacheDFItem.ProductivityInformation,
                StageCoordinateSystemEnum.Machine,
                startPosition,
                endPosition,
                cacheDFItem.CIBInformation,
                (true, null),
                (false, cacheDFItem.OpticsConfiguration),
                (false, cacheDFItem.CIBConfiguration),
                (false, cacheDFItem.LaserLightInformation),
                false,
                cancellationToken,
                isForward: false).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            var reverseRTAScanSamples = calibrationAlgorithmService.GetRTAScanSamples(
                reverseDarkFieldRawScanImage,
                Cache.StageMapConfig,
                false,
                Cache.IsRemoveLinear,
                HtmlLogUniqueId);
            Guard.IsNotEmpty(reverseRTAScanSamples);
            Cache.RTAScanResult.ReverseRTAScanSamples = reverseRTAScanSamples;

            isSuccess = true;
        }
        finally
        {
            var htmlBullet = new HtmlBullet(new
            {
                ForwardRawImageFilePath = forwardDarkFieldRawScanImage.RawImageFilePath,
                ReverseRawImageFilePath = reverseDarkFieldRawScanImage.RawImageFilePath,
                PlotDataSource = new HtmlContainer([.. Cache.RTAScanResult.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
            });
            if (isSuccess) logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
            else logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

            if (IsDevelopment) cibViewModel.UseSimulatorImages(null);

            stageViewModel.SetBrightFieldAbsoluteStageXy(stageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(Cache.CalChipSiteModelEnum)), Cache.CalChipSiteModelEnum);
        }

        return isSuccess;
    }, isSilent, cancellationToken).ConfigureAwait(false);

    [RelayCommand]
    private void Close()
    {
        try
        {
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