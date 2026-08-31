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
using Core.Models.Extensions;
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
using Python.Runtime;
using System.Reflection;
using MathNet.Numerics;
using Net.Utilities.Graphics.Primitives.Editors.Getters.Options;
using Net.Utilities.Graphics.Primitives.Enums.Editors;
using Net.Utilities.WaferMap.WPF.Primitives;

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

    public string Name { get; } = "StageMap";

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

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task AddStageMapTemplateAsync(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                Guard.IsGreaterThan(Cache.ImageWidth, 0);
                Guard.IsEqualTo(Cache.MicroscopeLensInformation, microscopeViewModel.GetCurrentMicroscopeLensInformation());

                var stageMapTemplatePoint = new StageMapTemplate
                {
                    FindBFMachinePosition = stageViewModel.GetMachineStagePosition()
                };

                var bfPosition = stageViewModel.MachineToBrightFieldPosition(stageMapTemplatePoint.FindBFMachinePosition);
                var dfPosition = cibViewModel.GetCIBInformationPosition(
                    StageCoordinateSystemEnum.Dark,
                    Cache.ProductivityInformation,
                    Cache.CIBInformation,
                    bfPosition,
                    Cache.MicroscopeLensInformation);

                stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(dfPosition, CalChipSiteModelEnum.ChuckModel);

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
                        cancellationToken,
                        isKeepRawImageCIBProfileModeEnum: true);

                    using var _ = darkFieldImageDto;

                    var originImageFilePath = Path.Combine(TemplateFileDirectory, Cache.MicroscopeLensInformation.ToString(), $"{Guid.NewGuid():N}.jpg");
                    stageMapTemplatePoint.TemplateFilePath = $"{originImageFilePath}_Template";
                    darkFieldImageDto.Image.SaveImage(originImageFilePath);

                    createDarkImageTemplateWindowViewModel.ImageFilePath = originImageFilePath;
                    createDarkImageTemplateWindowViewModel.TemplateFilePath = stageMapTemplatePoint.TemplateFilePath;
                    createDarkImageTemplateWindowViewModel.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
                    createDarkImageTemplateWindowViewModel.AlgorithmTemplateSizeEnum = Cache.AlgorithmTemplateSizeEnum;

                    Guard.IsTrue(windowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel) == true, nameof(createDarkImageTemplateWindowViewModel));

                    stageMapTemplatePoint.TemplateROI = createDarkImageTemplateWindowViewModel.Rect;
                    stageMapTemplatePoint.TemplateImageFilePath = createDarkImageTemplateWindowViewModel.TemplateImageFilePath;

                    Cache.StageMapTemplates = [.. Cache.StageMapTemplates, stageMapTemplatePoint];

                    foreach (var (index, stageMapTemplate) in Cache.StageMapTemplates.Index()) stageMapTemplate.Index = index + 1;
                }
                finally
                {
                    stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(bfPosition, CalChipSiteModelEnum.ChuckModel);
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog($"{Name}: Add Stage Map Template Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                dialogWindowProvider.ShowDialog($"""
                                                 {Name}: Add Stage Map Template Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogError(ex, "Add Stage Map Template");
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand]
    private void RemoveStageMapTemplates(IEnumerable? selectedItems)
    {
        if (selectedItems is null) return;

        var stageMapTemplates = Cache.StageMapTemplates.ToList();

        foreach (StageMapTemplate selectedItem in selectedItems) stageMapTemplates.Remove(selectedItem);

        Cache.StageMapTemplates = [.. stageMapTemplates];

        foreach (var (index, stageMapTemplate) in Cache.StageMapTemplates.Index()) stageMapTemplate.Index = index + 1;
    }

    [RelayCommand]
    private async Task GotoStageMapDocumentSelectedItemPositionAsync(StageMapTemplate stageMapTemplate)
    {
        await Task.Run(() =>
        {
            try
            {
                var index = Array.IndexOf(Cache.StageMapTemplates, stageMapTemplate);

                var (xDirection, _) = stageViewModel.GetMachineDirection();

                var drawable = Cache.StageMapDocument.Edit.SelectedItems.FirstOrDefault();
                if (drawable is null)
                {
                    dialogWindowProvider.ShowDialog("Goto Stage Map Document Selected Item Position Warning: Don't Select Die!", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                    return;
                }

                var stageMapDie = Guard.IsNotNullAndAssignableToTypeAndReturn<StageMapDie>(drawable);
                var darkFieldPosition = stageViewModel.MachineToDarkFieldPosition(stageMapDie.Markers[xDirection > 0 ? index : ^(index + 1)]);

                stageViewModel.SetBrightFieldAbsoluteStageXy(microscopeViewModel.GetMicroscopeLensInformationPosition(
                    Cache.MicroscopeLensInformation,
                    microscopeViewModel.GetCurrentMicroscopeLensInformation(),
                    darkFieldPosition));

                dialogWindowProvider.ShowDialog("Goto Stage Map Document Selected Item Position OK");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog($"{Name}: Goto Stage Map Document Selected Item Position Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                dialogWindowProvider.ShowDialog($"""
                                                 {Name}: Goto Stage Map Document Selected Item Position Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogError(ex, "Goto Stage Map Document Selected Item Position");
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(0, async () =>
    {
        AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
        AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;

        if (dialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment?",
                out var dialogResult,
                DialogButtonsEnum.YesNo,
                DialogIconEnum.Question) == true && dialogResult == DialogResultEnum.Yes) AlignmentUserControlViewModel.IsDarkFieldAlignment = Cache.IsDarkFieldAlignment = true;

        await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

        var newAlignmentResult = AlignmentUserControlViewModel.AlignmentResult;
        if (dialogWindowProvider.TryShowDialog("Yes: Apply the alignment offset to the template points? No: Not Apply?",
                out dialogResult,
                DialogButtonsEnum.YesNo,
                DialogIconEnum.Question) == true && dialogResult == DialogResultEnum.Yes)
        {
            var oldMarkPoint1 = Cache.AlignmentResult.MarkPoint1;
            var oldMarkPoint2 = Cache.AlignmentResult.MarkPoint1;

            var newMarkPoint1 = newAlignmentResult.MarkPoint1;
            var newMarkPoint2 = newAlignmentResult.MarkPoint1;

            var offset = (newMarkPoint1 - oldMarkPoint1 + (newMarkPoint2 - oldMarkPoint2)) / 2d;

            foreach (var stageMapTemplate in Cache.StageMapTemplates)
            {
                stageMapTemplate.FindBFMachinePosition += offset;
            }
        }

        Cache.AlignmentResult = newAlignmentResult;

        stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Point.Origin, CalChipSiteModelEnum.ChuckModel);

        logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            AlignmentUserControlViewModel.CalChipSiteModelEnum,
            AlignmentUserControlViewModel.IsDarkFieldAlignment,
            AlignmentResult = new HtmlQuote(Cache.AlignmentResult)
        }), HtmlLogUniqueId.LoggingHtml());

        return true;
    }, isSilent, cancellationToken).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(1, () =>
    {
        Guard.IsNotEmpty(Cache.StageMapTemplates);
        Guard.IsGreaterThan(Cache.WaferRadius, 0d);
        Guard.IsGreaterThan(Cache.DiePitchWidth, 0d);
        Guard.IsGreaterThan(Cache.DiePitchHeight, 0d);

        var dfPositions = new Point[Cache.StageMapTemplates.Length];

        foreach (var (index, stageMapTemplate) in Cache.StageMapTemplates.Index())
        {
            stageMapTemplate.FindBFMachineVector = stageMapTemplate.FindBFMachinePosition - Cache.StageMapTemplates[0].FindBFMachinePosition;

            var bfPosition = stageViewModel.MachineToBrightFieldPosition(stageMapTemplate.FindBFMachinePosition);
            var dfPosition = cibViewModel.GetCIBInformationPosition(
                StageCoordinateSystemEnum.Dark,
                Cache.ProductivityInformation,
                Cache.CIBInformation,
                bfPosition,
                Cache.MicroscopeLensInformation);

            dfPositions[index] = dfPosition;
        }

        logger.LogHtmlInformation("Templates", HtmlHeaderLevelEnum.Header3, new HtmlTable(
        [
            .. Cache.StageMapTemplates.Index().Select(t => new
            {
                t.Item.Index,
                t.Item.FindBFMachinePosition,
                t.Item.FindBFMachineVector,
                t.Item.TemplateROI,
                t.Item.TemplateFilePath,
                t.Item.TemplateImageFilePath,
                DFPosition = dfPositions[t.Index]
            })
        ]), HtmlLogUniqueId.LoggingHtml());

        Cache.StageMapDocument.RunDesign(() =>
        {
            Cache.StageMapDocument.WaferModel.Clear();
            Cache.StageMapDocument.DieModel.Clear();

            var dfCircle = new Circle(Point.Origin, Cache.WaferRadius);
            var dfMachineCircle = new Circle(stageViewModel.DarkFieldToMachinePosition(dfCircle.Center), dfCircle.Radius);

            Cache.StageMapDocument.WaferModel.Add(new StageMapWafer { Circle = dfMachineCircle });

            var stageMapReticleBuilder = new StageMapDieBuilder
            {
                DiePitchSize = new Size(Cache.DiePitchWidth, Cache.DiePitchHeight),
                OriginalDiePoint = dfPositions[0]
            };

            var stageMapDies = stageMapReticleBuilder.BuildDie(dfCircle);
            Vector[] markers =
            [
                .. dfPositions.Select(tt => tt - dfPositions[0])
            ];

            Cache.StageMapDocument.DieModel.AddRange(stageMapDies.Select(t =>
            {
                Point[] points = [.. markers.Select(tt => t.Rect.Point + tt)];

                var resultExtents = new Extents();
                resultExtents.Add(stageViewModel.DarkFieldToMachinePosition(t.Rect.XMinYMin));
                resultExtents.Add(stageViewModel.DarkFieldToMachinePosition(t.Rect.XMaxYMax));

                return new StageMapDie
                {
                    Index = t.Index,
                    Row = t.Row,
                    Col = t.Col,
                    Rect = (Rect)resultExtents,
                    IsInWafer = points.All(dfCircle.Contains),
                    Markers = [.. points.Select(stageViewModel.DarkFieldToMachinePosition)]
                };
            }));
        }, true);

        return Task.FromResult(true);
    }, isSilent, cancellationToken).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(2, async () =>
    {
        Cache.StageMap = new StageMap();

        StageMapDie[] stageMapDies;
        using (Cache.StageMapDocument.View.Sync.EnterScope()) stageMapDies = [.. Cache.StageMapDocument.DieModel];

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
                .RunAsync<StageMapDieSelectionGetter>(Cache.StageMapDocument.Edit, new SelectionInputOptions<StageMapDie>
                {
                    IsMultipleSelection = true,
                    CancellationToken = cancellationToken
                });

            Guard.IsTrue(inputResult.OutputResultModeEnum == OutputResultModeEnum.Ok);

            selectStageMapDies = [.. inputResult.Output];
        }

        foreach (var stageMapDie in selectStageMapDies) stageMapDie.IsInStageMapScan = true;

        Guard.IsNotEmpty(selectStageMapDies);

        var (xDirection, yDirection) = stageViewModel.GetMachineDirection();

        var minY = selectStageMapDies.Min(t => t.Index.Y);
        var maxY = selectStageMapDies.Max(t => t.Index.Y);
        var minX = selectStageMapDies.Min(t => t.Index.X);
        var maxX = selectStageMapDies.Max(t => t.Index.X);

        var yLength = maxY - minY + 1;
        var xLength = maxX - minX + 1;

        Guard.IsGreaterThan(yLength, 2);
        Guard.IsGreaterThan(xLength, 2);

        Cache.StageMap.IdealMatrix = [.. Generate.LinearRangeInt32(0, yLength - 1).Select(_ => new Point[xLength * Cache.StageMapTemplates.Length])];
        Cache.StageMap.ErrorMatrix = [.. Generate.LinearRangeInt32(0, yLength - 1).Select(_ => new Vector[xLength * Cache.StageMapTemplates.Length])];
        Cache.StageMap.IsInWaferMatrix = [.. Generate.LinearRangeInt32(0, yLength - 1).Select(_ => new bool[xLength * Cache.StageMapTemplates.Length])];
        Cache.StageMap.IsMatchMatrix = [.. Generate.LinearRangeInt32(0, yLength - 1).Select(_ => new bool[xLength * Cache.StageMapTemplates.Length])];

        for (var y = 0; y < yLength; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var x = 0; x < xLength; x++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var stageMapIndexX = xDirection > 0 ? minX + x : maxX - x;
                var stageMapIndexY = yDirection > 0 ? minY + y : maxY - y;

                var stageMapDie = selectStageMapDies.Single(t => t.Index == new WaferMapDieIndex(stageMapIndexX, stageMapIndexY));

                for (var markerIndex = 0; markerIndex < Cache.StageMapTemplates.Length; markerIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var dfMachinePoint = stageMapDie.Markers[xDirection > 0 ? markerIndex : ^(markerIndex + 1)];
                    dfMachinePoint = dfMachinePoint.WithY(stageMapDie.Markers[0].Y);

                    var matrixColumn = x * Cache.StageMapTemplates.Length + markerIndex;

                    Cache.StageMap.IdealMatrix[y][matrixColumn] = dfMachinePoint;
                    Cache.StageMap.IsInWaferMatrix[y][matrixColumn] = stageMapDie.IsInWafer;
                }
            }
        }

        Cache.StageMap.Refresh();

        await ScanStageMapAsync(
            Cache.StageMap,
            HtmlLogUniqueId,
            cancellationToken).ConfigureAwait(false);

        logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

        ProcessFirstMeasurement(Cache.StageMap);
        Cache.StageMap.Refresh();

        logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
            .. Cache.StageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
            .. Cache.StageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
        ]), HtmlLogUniqueId.LoggingHtml());

        return true;
    }, isSilent, cancellationToken).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step3Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(3, async () =>
    {
        DownloadStageMap();

        Cache.RepeatStageMaps = [];

        bool isSuccess;
        var times = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentHtmlLogUniqueId = Guid.NewGuid();
            var fileName = $"Details_{Steps[3].Replace(" ", string.Empty)}_{times}";

            logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {fileName}({currentHtmlLogUniqueId:N})"), HtmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation($"{currentHtmlLogUniqueId:N}", HtmlHeaderLevelEnum.Header1, new HtmlComment(Name), currentHtmlLogUniqueId.LoggingHtml());

            try
            {
                var scanStageMap = Cache.StageMap.Clone();
                scanStageMap.Reset();
                Cache.RepeatStageMaps = [.. Cache.RepeatStageMaps, scanStageMap];

                await ScanStageMapAsync(
                    scanStageMap,
                    currentHtmlLogUniqueId,
                    cancellationToken,
                    isInterpolateErrors: true).ConfigureAwait(false);

                logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                (isSuccess, var tempStateMap) = ProcessStage2Residuals();
                scanStageMap.Refresh();

                logger.LogHtmlInformation("Origin", HtmlHeaderLevelEnum.Header4, new HtmlContainer([
                    .. scanStageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                    .. scanStageMap.PlotDataSource.GetAllHtmlPlot3DCharts(),
                ]), HtmlLogUniqueId.LoggingHtml());

                logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
                    .. tempStateMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                    .. tempStateMap.PlotDataSource.GetAllHtmlPlot3DCharts(),
                ]), HtmlLogUniqueId.LoggingHtml());

                if (isSuccess)
                {
                    Cache.StageMap.SubtractInplace(tempStateMap);

                    logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlContainer([
                        .. Cache.StageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                        .. Cache.StageMap.PlotDataSource.GetAllHtmlPlot3DCharts(),
                    ]), HtmlLogUniqueId.LoggingHtml());

                    break;
                }

                if (++times > Cache.StageMapRepeatTimes - 1)
                {
                    isSuccess = false;
                    logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("More than the number of times."), HtmlLogUniqueId.LoggingHtml());

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

        DownloadStageMap();

        Cache.VerifyStageMap = Cache.StageMap.Clone();
        Cache.VerifyStageMap.Reset();

        await ScanStageMapAsync(
            Cache.VerifyStageMap,
            HtmlLogUniqueId,
            cancellationToken,
            isInterpolateErrors: true).ConfigureAwait(false);

        logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

        logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

        ProcessFirstMeasurement(Cache.VerifyStageMap);
        Cache.VerifyStageMap.Refresh();

        logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
            .. Cache.VerifyStageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
            .. Cache.VerifyStageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
        ]), HtmlLogUniqueId.LoggingHtml());

        return true;
    }, isSilent, cancellationToken).ConfigureAwait(false);

    private async Task ScanStageMapAsync(
        StageMap stageMap,
        Guid htmlLogUniqueId,
        CancellationToken cancellationToken,
        bool isInterpolateErrors = false)
    {
        logger.LogHtmlInformation("Scan StageMap", HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());

        Guard.IsGreaterThan(Cache.ROIMatchWidthScale, 0d);
        Guard.IsGreaterThan(Cache.ROIMatchHeightScale, 0d);

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
                .. Cache.StageMapTemplates
                    .Select(t =>
                    {
                        calibrationAlgorithmService.TryReadTemplate(Cache.AlgorithmTemplateTypeEnum, t.TemplateFilePath, out var templateId);

                        return templateId;
                    })
            ];

            var templateMatchScoreThreshold = Cache.AlgorithmTemplateTypeEnum.ToTemplateMatchScoreThreshold(calibrationSetting);

            Point[][] stageMapIdealMatrix =
            [
                .. stageMap.IdealMatrix.Select<Point[], Point[]>(t =>
                [
                    .. t.Select(tt => tt)
                ])
            ];
            var (yLength, xLength) = stageMapIdealMatrix.GetYXLength();

            logger.LogHtmlInformation("rows", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

            if (isInterpolateErrors)
            {
                var errors = Cache.StageMap.InterpolateErrors(stageMapIdealMatrix, new Circle(stageViewModel.DarkFieldToMachinePosition(Point.Origin), Cache.WaferRadius));
                for (var y = 0; y < yLength; y++)
                {
                    for (var x = 0; x < xLength; x++)
                    {
                        var vector = errors[y][x].WithY(0);
                        stageMapIdealMatrix[y][x] += vector;
                    }
                }
            }

            for (var y = 0; y < yLength; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var isInWaferColumnIndexes = stageMap.IsInWaferMatrix[y]
                    .Index()
                    .Where(t => t.Item)
                    .Select(t => t.Index)
                    .ToArray();

                var points = isInWaferColumnIndexes.Select(t => stageMapIdealMatrix[y][t]).ToArray();
                if (points.Length == 0) continue;

                logger.LogHtmlInformation($"{y + 1} row", HtmlHeaderLevelEnum.Header5, htmlLogUniqueId.LoggingHtml());

                DarkFieldImageDTO[] darkFieldImages = [];

                try
                {
                    stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(points[0], CalChipSiteModelEnum.ChuckModel);
                    darkFieldImages =
                    [
                        .. await cibViewModel.GetPMTImagesAsync(
                            Cache.ProductivityInformation,
                            StageCoordinateSystemEnum.Machine,
                            points,
                            Cache.ImageWidth,
                            Cache.CIBInformation,
                            (true, null),
                            (false, Cache.OpticsConfiguration),
                            (false, Cache.CIBConfiguration),
                            (false, Cache.LaserLightInformation),
                            false,
                            cancellationToken,
                            isKeepRawImageCIBProfileModeEnum: true)
                    ];

                    Guard.IsEqualTo(points.Length, darkFieldImages.Length);

                    foreach (var (i, isInWaferColumnIndex) in isInWaferColumnIndexes.Index())
                    {
                        var templateIdIndex = xDirection > 0 ? i % Cache.StageMapTemplates.Length : ^(i % Cache.StageMapTemplates.Length + 1);

                        var templateROI = Cache.StageMapTemplates[templateIdIndex].TemplateROI;
                        var imageBounds = new Rect(Point.Origin, darkFieldImages[i].Image.Size);
                        var searchROI = Cache.IsROIMatchEnabled
                            ? templateROI.Inflate(
                                templateROI.Width * Cache.ROIMatchWidthScale,
                                templateROI.Height * Cache.ROIMatchHeightScale).Intersect(imageBounds)
                            : imageBounds;

                        using var image = xDirection > 0 ? darkFieldImages[i].Image : darkFieldImages[i].Image.HorizontalFlip();
                        using var roiImage = image.ToROI(searchROI);

                        var isSuccess = calibrationAlgorithmService.TryTemplateMatchToOffset(
                            Cache.AlgorithmTemplateTypeEnum,
                            roiImage,
                            templateIds[templateIdIndex],
                            out var matchPoint,
                            out var matchOffset,
                            out var matchScore,
                            out var matchAngle);
                        var oldMatchPoint = matchPoint;
                        var oldMatchOffset = matchOffset;
                        if (Cache.IsROIMatchEnabled)
                        {
                            matchPoint += (Vector)searchROI.Point;
                            var offset = matchPoint - Cache.StageMapTemplates[templateIdIndex].TemplateROI.Center;
                            matchOffset = new Point(offset.X, -offset.Y);
                        }
                        else
                        {
                            var offset = matchPoint - Cache.StageMapTemplates[templateIdIndex].TemplateROI.Center;
                            matchOffset = new Point(offset.X, -offset.Y);
                        }

                        var resultImageFilePath = Path.Combine(isSuccess ? ImageFileDirectory : $"{FileHelper.GetFileFullName(Cache.StageMapTemplates[templateIdIndex].TemplateFilePath)}_Error", $"Origin_Score({matchScore:0.###},{templateMatchScoreThreshold:0.###})_Angle{matchAngle:0.###}_({htmlLogUniqueId:N}).jpg");
                        image.SaveImage(resultImageFilePath);

                        var vector = new Vector(xDirection * matchOffset.X * xSize.XPixelSize, yDirection * matchOffset.Y * ySize.YPixelSize);
                        vector = vector.WithY(vector.Y - Cache.StageMapTemplates[templateIdIndex].FindBFMachineVector.Y);

                        var htmlBullet = new HtmlBullet(new
                        {
                            templateROI,
                            imageBounds,
                            searchROI,
                            roiImage.Size,
                            oldMatchPoint,
                            oldMatchOffset,
                            matchPoint,
                            matchOffset,
                            matchScore,
                            matchAngle,
                            templateMatchScoreThreshold,
                            vector,
                            darkFieldImages[i].RawImageFilePath,
                            HtmlTab = new HtmlTab(new
                            {
                                ResultImage = new HtmlImage(resultImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(searchROI), new HtmlImageRectangleOverlay(templateROI), new HtmlImageCrossOverlay(matchPoint)]),
                                TemplateImage = new HtmlImage(Cache.StageMapTemplates[templateIdIndex].TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                            })
                        });

                        if (isSuccess) logger.LogHtmlInformation($"{isInWaferColumnIndex} Ok", HtmlHeaderLevelEnum.Header6, htmlBullet, htmlLogUniqueId.LoggingHtml());
                        else logger.LogHtmlError($"{isInWaferColumnIndex} Error", HtmlHeaderLevelEnum.Header6, htmlBullet, htmlLogUniqueId.LoggingHtml());

                        stageMap.IsMatchMatrix[y][isInWaferColumnIndex] = isSuccess;
                        if (isSuccess) stageMap.ErrorMatrix[y][isInWaferColumnIndex] = vector;

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
            ]), htmlLogUniqueId.LoggingHtml());
        }
        finally
        {
            foreach (var templateId in templateIds) calibrationAlgorithmService.TryCleanTemplate(Cache.AlgorithmTemplateTypeEnum, templateId);
            stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Point.Origin, CalChipSiteModelEnum.ChuckModel);
        }
    }

    private static void ProcessFirstMeasurement(StageMap stageMap)
    {
        using var _ = Py.GIL();
        using var module = PyModule.FromString("closed_loop_calibration", ClosedLoopCalibrationPythonScript);
        using var process = module.GetAttr("process_first_measurement");

        using var pyResidual = stageMap.ToPythonErrorMatrix();
        using var pyDesiredPositions = stageMap.ToPythonIdealMatrix();
        using var pyMask = stageMap.ToPythonIsMatchMatrix();
        using var result = process.Invoke(pyResidual, pyDesiredPositions, pyMask);

        stageMap.ApplyPythonErrorMatrix(result);
    }

    private (bool IsSuccess, StageMap StageMap) ProcessStage2Residuals()
    {
        Guard.IsNotEmpty(Cache.RepeatStageMaps);

        using var _ = Py.GIL();
        using var module = PyModule.FromString("closed_loop_calibration", ClosedLoopCalibrationPythonScript);
        using var process = module.GetAttr("process_stage2_residuals");

        using var pyResiduals = new PyList();
        using var pyMasks = new PyList();
        foreach (var stageMap in Cache.RepeatStageMaps)
        {
            using var pyResidual = stageMap.ToPythonErrorMatrix();
            using var pyMask = stageMap.ToPythonIsMatchMatrix();

            pyResiduals.Append(pyResidual);
            pyMasks.Append(pyMask);
        }

        using var pyDesiredPositions = Cache.RepeatStageMaps[0].ToPythonIdealMatrix();
        using var pyAlpha = StageMapResidualAlpha.ToPython();
        using var pyMinimumCount = StageMapMinimumRetryCount.ToPython();
        using var pyMaximumCount = (Cache.StageMapRepeatTimes + 1).ToPython();

        using var result = process.Invoke(pyResiduals, pyDesiredPositions, pyAlpha, pyMinimumCount, pyMaximumCount, pyMasks);

        using var pyNeedMoreMeasurement = Guard.IsNotNullAndReturn(result[0]);
        using var pyResidualTable = Guard.IsNotNullAndReturn(result[1]);

        var needMoreMeasurement = pyNeedMoreMeasurement.As<bool>();

        var temp = Cache.RepeatStageMaps[0].Clone();
        temp.Reset();
        temp.ApplyPythonErrorMatrix(pyResidualTable);

        return (needMoreMeasurement, temp);
    }

    private void DownloadStageMap()
    {
        stageViewModel.SetEnableStageMap(false);

        var stageMapErrorDTO = Cache.StageMap.AdaptTo(new Circle(stageViewModel.DarkFieldToMachinePosition(Point.Origin), Cache.WaferRadius));
        var xPoint3DList = new List<Point3D>();
        var yPoint3DList = new List<Point3D>();

        foreach (var (y, row) in stageMapErrorDTO.Rows.Index())
        {
            foreach (var (x, col) in row.Cols.Index())
            {
                xPoint3DList.Add(new Point3D(stageMapErrorDTO.BaseX + x * stageMapErrorDTO.XStep, stageMapErrorDTO.BaseY + y * stageMapErrorDTO.YStep, col.Error.X));
                yPoint3DList.Add(new Point3D(stageMapErrorDTO.BaseX + x * stageMapErrorDTO.XStep, stageMapErrorDTO.BaseY + y * stageMapErrorDTO.YStep, col.Error.Y));
            }
        }

        logger.LogHtmlInformation("Download", HtmlHeaderLevelEnum.Header3, new HtmlContainer(
        [
            new HtmlPlot3DChart(xPoint3DList, "X Error", HtmlPlot3DType.Surface),
            new HtmlPlot3DChart(yPoint3DList, "Y Error", HtmlPlot3DType.Surface)
        ]), HtmlLogUniqueId.LoggingHtml());

        stageViewModel.SetStageMap(stageMapErrorDTO);


        stageViewModel.SetEnableStageMap(true);
    }

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
}