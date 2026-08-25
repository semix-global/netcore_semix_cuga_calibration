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
using Core.Models.Models.Common.StageMap;
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
using Python.Runtime;
using System.Reflection;

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
    private const int StageMapMinimumRetryCount = 5;
    private const double StageMapResidualAlpha = 0.3d;
    private static readonly string ClosedLoopCalibrationPythonScript = GetEmbeddedResource("closed_loop_calibration.py");

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
        "Step 5 Download",
        "Step 6 Verify"
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
                        IsInWafer = circle.Contains(t.Rect.Point),
                        Markers = [Vector.Zero]
                    }));
                });

                Cache.CanvasDocument.View.ZoomToFit();
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
        Cache.StageMap = new StageMap();
        Cache.RepeatStageMaps = [];
        Cache.VerifyStageMap = new StageMap();

        var stageMapDies = Cache.CanvasDocument.OverlayerModel.OfType<StageMapDie>().ToArray();
        Guard.IsNotEmpty(stageMapDies);
        Guard.IsNotEmpty(Cache.StageMapTemplatePoints);

        var minRow = stageMapDies.Min(t => t.Row);
        var maxRow = stageMapDies.Max(t => t.Row);
        var minColumn = stageMapDies.Min(t => t.Col);
        var maxColumn = stageMapDies.Max(t => t.Col);
        var templatePointCount = Cache.StageMapTemplatePoints.Length;

        var rowCount = maxRow - minRow + 1;
        var columnCount = maxColumn - minColumn + 1;
        var matrixColumnCount = columnCount * templatePointCount;
        Cache.StageMap.IdealMatrix = new Point[rowCount, matrixColumnCount];
        Cache.StageMap.ErrorMatrix = new Vector[rowCount, matrixColumnCount];
        Cache.StageMap.ValidMatrix = new bool[rowCount, matrixColumnCount];

        for (var row = 0; row < rowCount; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var column = 0; column < columnCount; column++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var stageMapRow = minRow + row;
                var stageMapColumn = minColumn + column;

                var stageMapDie = stageMapDies.Single(t => t.Row == stageMapRow && t.Col == stageMapColumn);
                Guard.IsEqualTo(stageMapDie.Markers.Length, templatePointCount);

                for (var markerIndex = 0; markerIndex < templatePointCount; markerIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var dfMachinePoint = stageViewModel.DarkFieldToMachinePosition(stageMapDie.Rect.Point + stageMapDie.Markers[markerIndex]);
                    var matrixColumn = column * templatePointCount + markerIndex;
                    Cache.StageMap.IdealMatrix[row, matrixColumn] = dfMachinePoint;
                    Cache.StageMap.ValidMatrix[row, matrixColumn] = stageMapDie.IsInWafer;
                }
            }
        }

        Cache.StageMap.Refresh();

        await ScanStageMapAsync(Cache.StageMap, cancellationToken).ConfigureAwait(false);

        ProcessFirstMeasurement(Cache.StageMap);
        Cache.StageMap.Refresh();

        logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
            .. Cache.StageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
            .. Cache.StageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
        ]), HtmlLogUniqueId.LoggingHtml());

        return true;
    }, isSilent).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step3Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(3, async () =>
    {
        Guard.IsGreaterThanOrEqualTo(Cache.StageMapRetryCount, StageMapMinimumRetryCount);
        Guard.IsTrue(Cache.StageMap.IdealMatrix.Length > 0, nameof(Cache.StageMap.IdealMatrix));

        var repeatStageMaps = new List<StageMap>(Cache.StageMapRetryCount);
        Cache.RepeatStageMaps = [];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scanStageMap = Cache.StageMap.Clone();
            await ScanStageMapAsync(scanStageMap, cancellationToken).ConfigureAwait(false);
            repeatStageMaps.Add(scanStageMap);
            Cache.RepeatStageMaps = [.. repeatStageMaps];

            logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
                .. scanStageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                .. scanStageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
            ]), HtmlLogUniqueId.LoggingHtml());

            var isCompleted = ProcessStage2Residuals(Cache.StageMap);

            if (isCompleted) break;
        }

        Cache.StageMap.Refresh();

        return true;
    }, isSilent).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step4Async(bool isSilent, CancellationToken cancellationToken) => InvokeAsync(4, () =>
    {
        Guard.IsTrue(Cache.StageMap.IdealMatrix.Length > 0, nameof(Cache.StageMap.IdealMatrix));
        cancellationToken.ThrowIfCancellationRequested();

        var stageMapDto = ToStageMapDto(Cache.StageMap);
        stageViewModel.SetStageMap(stageMapDto);
        stageViewModel.SetEnableStageMap(true);
        logger.LogHtmlInformation("StageMap downloaded and enabled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

        return Task.FromResult(true);
    }, isSilent);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step5Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(5, async () =>
    {
        Guard.IsTrue(Cache.StageMap.IdealMatrix.Length > 0, nameof(Cache.StageMap.IdealMatrix));

        Cache.VerifyStageMap = Cache.StageMap.Clone();
        await ScanStageMapAsync(Cache.VerifyStageMap, cancellationToken).ConfigureAwait(false);
        Cache.VerifyStageMap.Refresh();
        LogStageMap("Verify StageMap", Cache.VerifyStageMap);

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

                        var vector = new Vector(xDirection * matchOffset.X * xSize.XPixelSize, yDirection * matchOffset.Y * ySize.YPixelSize);

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

    private StageMapDto ToStageMapDto(StageMap stageMap)
    {
        var rowCount = stageMap.IdealMatrix.GetLength(0);
        var matrixColumnCount = stageMap.IdealMatrix.GetLength(1);
        var templatePointCount = Cache.StageMapTemplatePoints.Length;

        Guard.IsGreaterThan(rowCount, 0);
        Guard.IsGreaterThan(matrixColumnCount, 0);
        Guard.IsGreaterThan(templatePointCount, 0);
        Guard.IsEqualTo(stageMap.ErrorMatrix.GetLength(0), rowCount);
        Guard.IsEqualTo(stageMap.ErrorMatrix.GetLength(1), matrixColumnCount);
        Guard.IsEqualTo(stageMap.ValidMatrix.GetLength(0), rowCount);
        Guard.IsEqualTo(stageMap.ValidMatrix.GetLength(1), matrixColumnCount);
        Guard.IsEqualTo(matrixColumnCount % templatePointCount, 0);
        Guard.IsGreaterThan(Cache.DiePitchWidth, 0d);
        Guard.IsGreaterThan(Cache.DiePitchHeight, 0d);

        // The scan matrix has one column per marker; the hardware error map has one column per die.
        var columnCount = matrixColumnCount / templatePointCount;
        var stageMapDto = new StageMapDto(rowCount, columnCount, Cache.DiePitchHeight, Cache.DiePitchWidth);

        for (var row = 0; row < rowCount; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                var matrixColumn = column * templatePointCount;
                var idealPoint = stageMap.IdealMatrix[row, matrixColumn];
                var isInWafer = stageMap.ValidMatrix[row, matrixColumn];
                var errorX = 0d;
                var errorY = 0d;
                var validCount = 0;

                for (var markerIndex = 0; markerIndex < templatePointCount; markerIndex++)
                {
                    var markerColumn = matrixColumn + markerIndex;
                    if (stageMap.ValidMatrix[row, markerColumn] == false) continue;

                    var error = stageMap.ErrorMatrix[row, markerColumn];
                    errorX += error.X;
                    errorY += error.Y;
                    validCount++;
                }

                var errorPoint = validCount == 0
                    ? Point.Origin
                    : new Point(errorX / validCount, errorY / validCount);
                var stageMapItem = stageMapDto.IdealStageMapItemMatrix[row][column];
                stageMapItem.Row = row;
                stageMapItem.Column = column;
                stageMapItem.Point = idealPoint;
                stageMapItem.IsInWafer = isInWafer;
                stageMapItem.IsMatchOk = isInWafer;
                stageMapDto.ErrorMatrix[row][column] = errorPoint;
                stageMapDto.RealMatrix[row][column] = new Point(idealPoint.X + errorPoint.X, idealPoint.Y + errorPoint.Y);
            }
        }

        return stageMapDto;
    }

    private static void ProcessFirstMeasurement(StageMap stageMap)
    {
        using var _ = Py.GIL();
        using var module = PyModule.FromString("closed_loop_calibration", ClosedLoopCalibrationPythonScript);
        using var process = module.GetAttr("process_first_measurement");
        using var pyResidual = ToPythonErrorArray(stageMap);
        using var pyDesiredPositions = ToPythonPointArray(stageMap);
        using var result = process.Invoke(pyResidual, pyDesiredPositions);

        ApplyPythonErrorArray(stageMap, result);
    }

    private bool ProcessStage2Residuals(StageMap stageMap)
    {
        var scanStageMaps = Cache.RepeatStageMaps;

        Guard.IsNotEmpty(scanStageMaps);
        Guard.IsGreaterThanOrEqualTo(Cache.StageMapRetryCount, StageMapMinimumRetryCount);

        foreach (var scanStageMap in scanStageMaps)
        {
            Guard.IsEqualTo(scanStageMap.IdealMatrix.GetLength(0), stageMap.IdealMatrix.GetLength(0));
            Guard.IsEqualTo(scanStageMap.IdealMatrix.GetLength(1), stageMap.IdealMatrix.GetLength(1));
            Guard.IsEqualTo(scanStageMap.ErrorMatrix.GetLength(0), stageMap.ErrorMatrix.GetLength(0));
            Guard.IsEqualTo(scanStageMap.ErrorMatrix.GetLength(1), stageMap.ErrorMatrix.GetLength(1));
            Guard.IsEqualTo(scanStageMap.ValidMatrix.GetLength(0), stageMap.ValidMatrix.GetLength(0));
            Guard.IsEqualTo(scanStageMap.ValidMatrix.GetLength(1), stageMap.ValidMatrix.GetLength(1));
        }

        using var _ = Py.GIL();
        using var module = PyModule.FromString("closed_loop_calibration", ClosedLoopCalibrationPythonScript);
        using var process = module.GetAttr("process_stage2_residuals");
        using var pyResiduals = ToPythonErrorHistory(scanStageMaps);
        using var pyDesiredPositions = ToPythonPointArray(stageMap);
        using var pyAlpha = StageMapResidualAlpha.ToPython();
        using var pyMinimumCount = StageMapMinimumRetryCount.ToPython();
        using var pyMaximumCount = Cache.StageMapRetryCount.ToPython();
        using var result = process.Invoke(pyResiduals, pyDesiredPositions, pyAlpha, pyMinimumCount, pyMaximumCount);
        using var pyNeedMoreMeasurement = Guard.IsNotNullAndReturn(result[0]);
        using var pyResidualTable = Guard.IsNotNullAndReturn(result[1]);

        var needMoreMeasurement = pyNeedMoreMeasurement.As<bool>();
        if (pyResidualTable.IsNone()) return needMoreMeasurement == false;

        ApplyPythonErrorArray(stageMap, pyResidualTable);

        return needMoreMeasurement == false;
    }

    private static PyList ToPythonPointArray(StageMap stageMap)
    {
        var result = new PyList();
        var rowCount = stageMap.IdealMatrix.GetLength(0);
        var columnCount = stageMap.IdealMatrix.GetLength(1);

        for (var row = 0; row < rowCount; row++)
        {
            using var pyRow = new PyList();
            for (var column = 0; column < columnCount; column++)
            {
                var point = stageMap.IdealMatrix[row, column];
                using var pyPoint = new PyList();
                using var pyX = point.X.ToPython();
                using var pyY = point.Y.ToPython();

                pyPoint.Append(pyX);
                pyPoint.Append(pyY);
                pyRow.Append(pyPoint);
            }

            result.Append(pyRow);
        }

        return result;
    }

    private static PyList ToPythonErrorArray(StageMap stageMap)
    {
        var result = new PyList();
        var rowCount = stageMap.ErrorMatrix.GetLength(0);
        var columnCount = stageMap.ErrorMatrix.GetLength(1);

        for (var row = 0; row < rowCount; row++)
        {
            using var pyRow = new PyList();
            for (var column = 0; column < columnCount; column++)
            {
                var vector = stageMap.ErrorMatrix[row, column];
                using var pyVector = new PyList();
                using var pyX = vector.X.ToPython();
                using var pyY = vector.Y.ToPython();

                pyVector.Append(pyX);
                pyVector.Append(pyY);
                pyRow.Append(pyVector);
            }

            result.Append(pyRow);
        }

        return result;
    }

    private static PyObject ToPythonErrorHistory(IReadOnlyList<StageMap> stageMaps)
    {
        var result = new PyList();

        foreach (var stageMap in stageMaps)
        {
            using var pyScan = ToPythonErrorArray(stageMap);
            result.Append(pyScan);
        }

        return result;
    }

    private static void ApplyPythonErrorArray(StageMap stageMap, PyObject pyValues)
    {
        using var pyValueArray = pyValues.InvokeMethod("tolist");
        using var rows = new PyList(pyValueArray);
        var rowCount = stageMap.ErrorMatrix.GetLength(0);
        var columnCount = stageMap.ErrorMatrix.GetLength(1);
        Guard.IsEqualTo(rows.Length(), rowCount);

        for (var row = 0; row < rowCount; row++)
        {
            using var pyRowObject = Guard.IsNotNullAndReturn(rows[row]);
            using var pyRow = new PyList(pyRowObject);
            Guard.IsEqualTo(pyRow.Length(), columnCount);

            for (var column = 0; column < columnCount; column++)
            {
                using var pyVectorObject = Guard.IsNotNullAndReturn(pyRow[column]);
                using var pyVector = new PyList(pyVectorObject);
                Guard.IsEqualTo(pyVector.Length(), 2);

                using var pyX = Guard.IsNotNullAndReturn(pyVector[0]);
                using var pyY = Guard.IsNotNullAndReturn(pyVector[1]);

                var x = pyX.As<double>();
                var y = pyY.As<double>();
                Guard.IsTrue(double.IsFinite(x));
                Guard.IsTrue(double.IsFinite(y));

                stageMap.ErrorMatrix[row, column] = new Vector(x, y);
            }
        }
    }

    private static string GetEmbeddedResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames().SingleOrDefault(t => t.EndsWith($".Assets.Python.{fileName}", StringComparison.OrdinalIgnoreCase));
        Guard.IsNotNull(resourceName);

        using var stream = assembly.GetManifestResourceStream(resourceName);
        Guard.IsNotNull(stream);

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [RelayCommand]
    private void Close()
    {
        try
        {
            Step5CancelCommand.Execute(null);
            Step4CancelCommand.Execute(null);
            Step3CancelCommand.Execute(null);
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