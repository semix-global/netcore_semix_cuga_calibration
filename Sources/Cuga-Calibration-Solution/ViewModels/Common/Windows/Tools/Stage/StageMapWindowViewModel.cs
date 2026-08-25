using System.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Cookies;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;
using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Algorithm;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.CIB.XPixelSize;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using CugaCalibration.Core.Services.Interfaces;
using HalconDotNet;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Net.Utilities.Calibration;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

[IOCAppService(ServiceType = typeof(StageMapWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class StageMapWindowViewModel(
    CIBViewModel cibViewModel,
    MicroscopeViewModel microscopeViewModel,
    StageViewModel stageViewModel,
    CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel,
    AlignmentUserControlViewModel alignmentUserControlViewModel,
    ICacheProvider cacheProvider,
    IApplicationCookieService applicationCookieCacheProvider,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    IOptions<ApplicationSetting> options,
    ApplicationCookie applicationCookie,
    CalibrationSetting calibrationSetting,
    ILogger<StageMapWindowViewModel> logger) : ViewModelBase
{
    public string Name { get; } = "StageMap Diagnostic Tool";

    public ApplicationCookie ApplicationCookie { get; } = applicationCookie;

    public AlignmentUserControlViewModel AlignmentUserControlViewModel { get; } = alignmentUserControlViewModel;

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

    [RelayCommand]
    private async Task LoadedAsync()
    {
        Cache = await Task.Run(() => cacheProvider.GetOrDefault<StageMapCache>()).ConfigureAwait(true);
    }

    [RelayCommand]
    private void RemoveStageMapTemplatePoints(IEnumerable? selectedItems)
    {
        if (selectedItems is null) return;

        var stageMapTemplatePoints = Cache.StageMapTemplatePoints.ToList();

        foreach (StageMapTemplatePoint selectedItem in selectedItems) stageMapTemplatePoints.Remove(selectedItem);

        Cache.StageMapTemplatePoints = [.. stageMapTemplatePoints];

        if (Cache.StageMapTemplatePoints.Length == 0)
        {
            Cache.CanvasDocument.RunDesign(() =>
            {
                Cache.CanvasDocument.DefaultModel.Clear();
                Cache.CanvasDocument.OverlayerModel.Clear();
            });
        }
        else
        {
            foreach (var drawable in Cache.CanvasDocument.OverlayerModel.OfType<StageMapDie>())
            {
                drawable.Markers = [.. drawable.Markers.AsSpan()[..Cache.StageMapTemplatePoints.Length]];
            }
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(0, async () =>
    {
        AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
        AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;

        dialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?",
            out var dialogResult,
            DialogButtonsEnum.YesNo,
            DialogIconEnum.Question);

        AlignmentUserControlViewModel.IsDarkFieldAlignment = Cache.IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;

        await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

        Cache.AlignmentResult = AlignmentUserControlViewModel.AlignmentResult;

        logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            AlignmentUserControlViewModel.CalChipSiteModelEnum,
            AlignmentUserControlViewModel.IsDarkFieldAlignment,
            AlignmentResult = new HtmlQuote(Cache.AlignmentResult)
        }), HtmlLogUniqueId.LoggingHtml());

        return true;
    }, isSilent).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(1, async () =>
    {
        Guard.IsEqualTo(Cache.MicroscopeLensInformation, microscopeViewModel.GetCurrentMicroscopeLensInformation());

        var stageMapTemplatePoint = new StageMapTemplatePoint();

        var findBFMachinePosition = stageViewModel.GetMachineStagePosition();

        var bfPosition = stageViewModel.MachineToBrightFieldPosition(findBFMachinePosition);
        var dfPosition = cibViewModel.GetCIBInformationPosition(
            StageCoordinateSystemEnum.Dark,
            Cache.ProductivityInformation,
            Cache.CIBInformation,
            bfPosition,
            Cache.MicroscopeLensInformation);

        stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(dfPosition, CalChipSiteModelEnum.ChuckModel);

        stageMapTemplatePoint.DFPosition = dfPosition;

        try
        {
            var darkFieldImageDto = await cibViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Dark,
                dfPosition,
                Cache.ImageWidth,
                Cache.CIBInformation,
                (true, null),
                (false, Cache.OpticsConfiguration),
                (false, Cache.CIBConfiguration),
                (false, Cache.LaserLightInformation),
                false,
                cancellationToken);

            using var _ = darkFieldImageDto;

            var originImageFilePath = Path.Combine(TemplateFileDirectory, Cache.MicroscopeLensInformation.ToString(), $"{Guid.NewGuid():N}.jpg");
            stageMapTemplatePoint.TemplateFilePath = $"{originImageFilePath}_Template";
            darkFieldImageDto.Image.SaveImage(originImageFilePath);

            createDarkImageTemplateWindowViewModel.ImageFilePath = originImageFilePath;
            createDarkImageTemplateWindowViewModel.TemplateFilePath = stageMapTemplatePoint.TemplateFilePath;
            createDarkImageTemplateWindowViewModel.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
            createDarkImageTemplateWindowViewModel.AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum.Size32;

            Guard.IsTrue(windowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel) == true, nameof(createDarkImageTemplateWindowViewModel));

            stageMapTemplatePoint.TemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(stageMapTemplatePoint.TemplateFilePath);

            if (Cache.CanvasDocument.DefaultModel.Count == 0)
            {
                Cache.CanvasDocument.RunDesign(() =>
                {
                    Cache.CanvasDocument.DefaultModel.Clear();
                    Cache.CanvasDocument.OverlayerModel.Clear();

                    var circle = new Circle(Point.Origin, Cache.WaferRadius);
                    Cache.CanvasDocument.DefaultModel.Add(new StageMapCircle { Circle = circle });

                    var stageMapReticleBuilder = new StageMapReticleBuilder
                    {
                        DiePitchSize = new Size(Cache.DiePitchWidth, Cache.DiePitchHeight),
                        OriginalDiePoint = dfPosition
                    };

                    var stageMapDies = stageMapReticleBuilder.BuildDie(circle);

                    Cache.CanvasDocument.OverlayerModel.AddRange(stageMapDies.Select(t => new StageMapDie
                    {
                        Index = t.Index,
                        Row = t.Row,
                        Col = t.Col,
                        Rect = t.Rect,
                        IsInWafer = circle.Contains(t.Rect),
                        Markers = [Vector.Zero]
                    }));
                });
            }
            else
            {
                foreach (var drawable in Cache.CanvasDocument.OverlayerModel.OfType<StageMapDie>())
                {
                    drawable.Markers = [.. drawable.Markers, dfPosition - Cache.StageMapTemplatePoints[0].DFPosition];
                }
            }

            Cache.StageMapTemplatePoints = [.. Cache.StageMapTemplatePoints, stageMapTemplatePoint];

            return true;
        }
        finally
        {
            stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(bfPosition, CalChipSiteModelEnum.ChuckModel);
        }
    }, isSilent).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(2, async () =>
    {
        Cache.ScanStageMap = new StageMap();

        var stageMapDies = Cache.CanvasDocument.OverlayerModel.OfType<StageMapDie>().ToArray();
        var minRow = stageMapDies.Min(t => t.Row);
        var maxRow = stageMapDies.Max(t => t.Row);
        var minColumn = stageMapDies.Min(t => t.Col);
        var maxColumn = stageMapDies.Max(t => t.Col);

        var rowCount = maxRow - minRow + 1;
        var columnCount = maxColumn - minColumn + 1;
        Cache.ScanStageMap.IdealMatrix = new Point[rowCount, columnCount * Cache.StageMapTemplatePoints.Length];
        Cache.ScanStageMap.ErrorMatrix = new Vector[rowCount, columnCount * Cache.StageMapTemplatePoints.Length];
        Cache.ScanStageMap.ValidMatrix = new bool[rowCount, columnCount * Cache.StageMapTemplatePoints.Length];

        for (var row = 0; row < rowCount; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var column = 0; column < columnCount; column++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var stageMapRow = minRow + row;
                var stageMapColumn = minColumn + column;

                var stageMapDie = stageMapDies.Single(t => t.Row == stageMapRow && t.Col == stageMapColumn);

                for (var markerIndex = 0; markerIndex < columnCount * Cache.StageMapTemplatePoints.Length; markerIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var dfMachinePoint = stageViewModel.DarkFieldToMachinePosition(stageMapDie.Rect.Point + stageMapDie.Markers[markerIndex]);
                    Cache.ScanStageMap.IdealMatrix[row, column * Cache.StageMapTemplatePoints.Length + markerIndex] = dfMachinePoint;
                    Cache.ScanStageMap.ValidMatrix[row, column * Cache.StageMapTemplatePoints.Length + markerIndex] = stageMapDie.IsInWafer;
                }
            }
        }

        Cache.ScanStageMap.Refresh();

        await ScanStageMapAsync(Cache.ScanStageMap, cancellationToken).ConfigureAwait(false);

        return true;
    }, isSilent).ConfigureAwait(false);

    private async Task ScanStageMapAsync(StageMap stageMap, CancellationToken cancellationToken)
    {
        logger.LogHtmlInformation("Scan StageMap", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

        var (xDirection, yDirection) = stageViewModel.GetMachineDirection();

        var xSize = applicationCookieCacheProvider.GetCalibrations<CIBXPixelSizeDTO>(cancellationToken).SingleOrDefault(t => t.ProductivityInformation == Cache.ProductivityInformation);
        Guard.IsTrue(xSize?.IsOk == true, "Laser X Pixel Size is Empty or not verify.");

        var ySize = applicationCookieCacheProvider.GetCalibrations<CIBYPixelSizeDTO>(cancellationToken).SingleOrDefault(t => t.ProductivityInformation.OpticsIlluminationModeEnum == Cache.ProductivityInformation.OpticsIlluminationModeEnum
                                                                                                                             && t.ProductivityInformation.OpticsMagType == Cache.ProductivityInformation.OpticsMagType
                                                                                                                             && t.PmtId == Cache.CIBInformation.PMTId);
        Guard.IsTrue(ySize?.IsOk == true, "Laser Y Pixel Size is Empty or not verify.");

        HTuple[] templateIds = [];

        try
        {
            templateIds =
            [
                .. Cache.StageMapTemplatePoints
                    .Select(t =>
                    {
                        calibrationAlgorithmService.TryReadTemplate(Cache.AlgorithmTemplateTypeEnum, t.TemplateFilePath, out var templateId);

                        return templateId;
                    })
            ];

            var templateMatchScoreThreshold = Cache.AlgorithmTemplateTypeEnum.ToTemplateMatchScoreThreshold(calibrationSetting);
            var (idealRowCount, _) = stageMap.IdealMatrix.GetRowCountColCount();

            logger.LogHtmlInformation("rows", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

            for (var row = 0; row < idealRowCount; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var isInWaferColumnIndexes = stageMap.ValidMatrix.Row(row)
                    .Index()
                    .Where(t => t.Item)
                    .Select(t => t.Index)
                    .ToArray();

                var points = isInWaferColumnIndexes.Select(t => stageMap.IdealMatrix.Row(row)[t]).ToArray();
                if (points.Length == 0) continue;

                logger.LogHtmlInformation($"{row + 1} row", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

                DarkFieldImageDTO[] darkFieldImages = [];

                try
                {
                    darkFieldImages =
                    [
                        .. await cibViewModel.GetPMTImagesAsync(
                            Cache.ProductivityInformation,
                            StageCoordinateSystemEnum.Machine,
                            points,
                            Cache.ImageWidth,
                            Cache.CIBInformation,
                            (false, CalChipSiteModelEnum.ChuckModel),
                            (false, Cache.OpticsConfiguration),
                            (false, Cache.CIBConfiguration),
                            (false, Cache.LaserLightInformation),
                            false,
                            cancellationToken)
                    ];
                    Guard.IsEqualTo(points.Length, darkFieldImages.Length);

                    foreach (var (i, isInWaferColumnIndex) in isInWaferColumnIndexes.Index())
                    {
                        var templateIdIndex = i % Cache.StageMapTemplatePoints.Length;

                        var bitmapImage = darkFieldImages[i].Image;

                        var isSuccess = calibrationAlgorithmService.TryTemplateMatchToOffset(
                            Cache.AlgorithmTemplateTypeEnum,
                            bitmapImage,
                            templateIds[templateIdIndex],
                            out var matchPoint,
                            out var matchOffset,
                            out var matchScore,
                            out var matchAngle);

                        var resultImageFilePath = Path.Combine(isSuccess ? ImageFileDirectory : $"{FileHelper.GetFileFullName(Cache.StageMapTemplatePoints[templateIdIndex].TemplateFilePath)}_Error", $"Origin_Score({matchScore:0.###},{templateMatchScoreThreshold:0.###})_Angle{matchAngle:0.###}_({HtmlLogUniqueId:N}).jpg");
                        bitmapImage.Save(resultImageFilePath);

                        var vector = new Vector(xDirection * matchOffset.X * xSize.XPixelSize, yDirection * matchOffset.X * ySize.YPixelSize);

                        var htmlBullet = new HtmlBullet(new
                        {
                            matchPoint,
                            matchOffset,
                            matchScore,
                            matchAngle,
                            templateMatchScoreThreshold,
                            vector,
                            HtmlTab = new HtmlTab(new
                            {
                                ResultImage = new HtmlImage(resultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(matchPoint)]),
                                TemplateImage = new HtmlImage(Cache.StageMapTemplatePoints[templateIdIndex].TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                            })
                        });

                        if (isSuccess) logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                        else logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                        if (isSuccess) stageMap.ErrorMatrix[row, isInWaferColumnIndex] = vector;

                        stageMap.Refresh();
                    }
                }
                finally
                {
                    foreach (var darkFieldImage in darkFieldImages)
                    {
                        using var _ = darkFieldImage;
                    }
                }
            }

            logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
                .. stageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                .. stageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
            ]), HtmlLogUniqueId.LoggingHtml());
        }
        finally
        {
            foreach (var templateId in templateIds) calibrationAlgorithmService.TryCleanTemplate(Cache.AlgorithmTemplateTypeEnum, templateId);
        }
    }

    [RelayCommand]
    private void Close()
    {
        try
        {
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

    private async Task<bool> InvokeAsync(
        int stepIndex,
        Func<Task<bool>> func,
        bool isSilent)
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
        }).ConfigureAwait(false);
    }
}