using System.IO;
using System.Threading.Channels;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Algorithm.Extensions;
using Core.Algorithm.Models.TemplateMatch;
using Core.Models.Enums.Stage;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.CIB.XPixelSize;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.PixelSize;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using CugaCalibration.Core.Services.Interfaces;
using HalconDotNet;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Editors.Getters.Options;
using Net.Utilities.Graphics.Primitives.Enums.Editors;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Enums.Loggings;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.WaferMap.WPF.Primitives;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using SourceGenerator.AssemblyMetadata;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

[IOCAppService(ServiceType = typeof(StageMapCommands), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class StageMapCommands(
    MicroscopeViewModel microscopeViewModel,
    StageViewModel stageViewModel,
    CIBViewModel cibViewModel,
    ReviewViewModel reviewViewModel,
    CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel,
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    CalibrationSetting calibrationSetting,
    IApplicationCookieService applicationCookieService,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ILogger<StageMapCommands> logger) : ObservableObject
{
    public string ImageFileDirectory { get; set; } = string.Empty;

    public string TemplateFileDirectory { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Name), nameof(CacheItem), nameof(StageMapDocument), nameof(StageMap), nameof(VerifyStageMap), nameof(AlignmentResult))]
    public partial bool IsDark { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CacheItem))]
    public partial ChuckStageMapCache Cache { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StageMapDocument), nameof(StageMap), nameof(VerifyStageMap), nameof(AlignmentResult))]
    public partial ChuckStageMapDTO ChuckStageMap { get; set; } = new();

    public string Name => IsDark ? "DFStageMap" : "BFStageMap";

    public ChuckStageMapCacheBFItem CacheItem => IsDark ? Cache.ChuckStageMapCacheDFItem : Cache.ChuckStageMapCacheBFItem;

    public StageMapDocument StageMapDocument => IsDark ? ChuckStageMap.DFStageMapDocument : ChuckStageMap.BFStageMapDocument;

    public StageMap StageMap
    {
        get => IsDark ? ChuckStageMap.DFStageMap : ChuckStageMap.BFStageMap;
        set
        {
            if (IsDark) ChuckStageMap.DFStageMap = value;
            else ChuckStageMap.BFStageMap = value;

            OnPropertyChanged();
        }
    }

    public StageMap VerifyStageMap
    {
        get => IsDark ? ChuckStageMap.DFVerifyStageMap : ChuckStageMap.BFVerifyStageMap;
        set
        {
            if (IsDark) ChuckStageMap.DFVerifyStageMap = value;
            else ChuckStageMap.BFVerifyStageMap = value;

            OnPropertyChanged();
        }
    }

    public AlignmentResultDto AlignmentResult
    {
        get => IsDark ? ChuckStageMap.DFAlignmentResult : ChuckStageMap.BFAlignmentResult;
        set
        {
            if (IsDark) ChuckStageMap.DFAlignmentResult = value;
            else ChuckStageMap.BFAlignmentResult = value;

            OnPropertyChanged();
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task AddStageMapTemplateAsync(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                Guard.IsEqualTo(Cache.MicroscopeLensInformation, microscopeViewModel.GetCurrentMicroscopeLensInformation());

                var stageMapTemplatePoint = new StageMapTemplate
                {
                    FindBFMachinePosition = stageViewModel.GetMachineStagePosition()
                };

                // 选择的点Y必须相同： (之前计算与第一个点的差不准, 因为是明场下相减不是暗场, 实际上应该是两个暗场模版中心位置的差)
                if (CacheItem.StageMapTemplates.Length > 0) Guard.IsEqualTo(stageMapTemplatePoint.FindBFMachinePosition.Y, CacheItem.StageMapTemplates[0].FindBFMachinePosition.Y);

                if (IsDark)
                {
                    var cacheDFItem = (ChuckStageMapCacheDFItem)CacheItem;

                    Guard.IsGreaterThan(cacheDFItem.ImageWidth, 0);

                    var bfPosition = stageViewModel.MachineToBrightFieldPosition(stageMapTemplatePoint.FindBFMachinePosition);
                    var dfPosition = cibViewModel.GetCIBInformationPosition(
                        StageCoordinateSystemEnum.Dark,
                        cacheDFItem.ProductivityInformation,
                        cacheDFItem.CIBInformation,
                        bfPosition,
                        Cache.MicroscopeLensInformation);

                    stageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(dfPosition, CalChipSiteModelEnum.ChuckModel);

                    try
                    {
                        var darkFieldImageDto = await cibViewModel.GetPMTImageAsync(
                            cacheDFItem.ProductivityInformation,
                            StageCoordinateSystemEnum.Dark,
                            dfPosition,
                            cacheDFItem.ImageWidth,
                            cacheDFItem.CIBInformation,
                            (true, null),
                            (false, cacheDFItem.OpticsConfiguration),
                            (false, cacheDFItem.CIBConfiguration),
                            (false, cacheDFItem.LaserLightInformation),
                            false,
                            cancellationToken).ConfigureAwait(false);

                        using var _ = darkFieldImageDto;

                        var originImageFilePath = Path.Combine(TemplateFileDirectory, Cache.MicroscopeLensInformation.ToString(), $"DF_{Guid.NewGuid():N}.jpg");
                        stageMapTemplatePoint.TemplateFilePath = $"{originImageFilePath}_Template";
                        darkFieldImageDto.Image.SaveImage(originImageFilePath);
                        stageMapTemplatePoint.OriginImageFilePath = originImageFilePath;

                        createDarkImageTemplateWindowViewModel.ImageFilePath = originImageFilePath;
                        createDarkImageTemplateWindowViewModel.TemplateFilePath = stageMapTemplatePoint.TemplateFilePath;
                        createDarkImageTemplateWindowViewModel.TemplateMatchSetting = cacheDFItem.TemplateMatchSetting.Clone();

                        Guard.IsTrue(windowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel) == true, nameof(createDarkImageTemplateWindowViewModel));

                        stageMapTemplatePoint.TemplateROI = createDarkImageTemplateWindowViewModel.Rect;
                        stageMapTemplatePoint.TemplateImageFilePath = createDarkImageTemplateWindowViewModel.TemplateImageFilePath;
                    }
                    finally
                    {
                        stageViewModel.SetBrightFieldAbsoluteStageXy(bfPosition, CalChipSiteModelEnum.ChuckModel);
                    }
                }
                else
                {
                    var originImageFilePath = Path.Combine(TemplateFileDirectory, Cache.MicroscopeLensInformation.ToString(), $"BF_{Guid.NewGuid():N}.jpg");
                    stageMapTemplatePoint.TemplateFilePath = $"{originImageFilePath}_Template";
                    using var image = reviewViewModel.GetBrightFieldImage();
                    image.SaveImage(originImageFilePath);
                    stageMapTemplatePoint.OriginImageFilePath = originImageFilePath;

                    createDarkImageTemplateWindowViewModel.ImageFilePath = originImageFilePath;
                    createDarkImageTemplateWindowViewModel.TemplateFilePath = stageMapTemplatePoint.TemplateFilePath;
                    createDarkImageTemplateWindowViewModel.TemplateMatchSetting = CacheItem.TemplateMatchSetting.Clone();

                    Guard.IsTrue(windowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel) == true, nameof(createDarkImageTemplateWindowViewModel));

                    stageMapTemplatePoint.TemplateROI = createDarkImageTemplateWindowViewModel.Rect;
                    stageMapTemplatePoint.TemplateImageFilePath = createDarkImageTemplateWindowViewModel.TemplateImageFilePath;
                }

                CacheItem.StageMapTemplates = [.. CacheItem.StageMapTemplates, stageMapTemplatePoint];

                dialogWindowProvider.ShowDialog($"{Name}: Add Template OK");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog($"{Name}: Add Template Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                    return;
                }

                dialogWindowProvider.ShowDialog($"""
                                                 {Name}: Add Template Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogError(ex, "{Name}: Add Template", Name);
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GotoStageMapDocumentSelectedItemPositionAsync(StageMapTemplate stageMapTemplate)
    {
        await Task.Run(() =>
        {
            try
            {
                var index = Array.IndexOf(CacheItem.StageMapTemplates, stageMapTemplate);

                var drawable = StageMapDocument.Edit.SelectedItems.FirstOrDefault();
                if (drawable is null)
                {
                    dialogWindowProvider.ShowDialog($"{Name}: Goto Selected Item Position Warning: Don't Select Die!", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                    return;
                }

                var stageMapDie = Guard.IsNotNullAndAssignableToTypeAndReturn<StageMapDie>(drawable);
                Guard.IsInRangeFor(index, stageMapDie.Markers);

                if (IsDark)
                {
                    var cacheDFItem = (ChuckStageMapCacheDFItem)CacheItem;

                    var darkFieldPositionTemp = stageViewModel.MachineToDarkFieldPosition(stageMapDie.Markers[index]);
                    var darkFieldPosition = darkFieldPositionTemp - cibViewModel.GetCIBInformationOffset(
                        StageCoordinateSystemEnum.Dark,
                        cacheDFItem.ProductivityInformation,
                        cacheDFItem.CIBInformation,
                        Cache.MicroscopeLensInformation,
                        isLineCentricityOffset: false);

                    stageViewModel.SetBrightFieldAbsoluteStageXy(microscopeViewModel.GetMicroscopeLensInformationPosition(
                        Cache.MicroscopeLensInformation,
                        microscopeViewModel.GetCurrentMicroscopeLensInformation(),
                        darkFieldPosition), CalChipSiteModelEnum.ChuckModel);
                }
                else
                {
                    var brightFieldPosition = stageViewModel.MachineToBrightFieldPosition(stageMapDie.Markers[index]);

                    stageViewModel.SetBrightFieldAbsoluteStageXy(microscopeViewModel.GetMicroscopeLensInformationPosition(
                        Cache.MicroscopeLensInformation,
                        microscopeViewModel.GetCurrentMicroscopeLensInformation(),
                        brightFieldPosition), CalChipSiteModelEnum.ChuckModel);
                }

                dialogWindowProvider.ShowDialog($"{Name}: Goto Selected Item Position OK");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog($"{Name}: Goto Selected Item Position Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                dialogWindowProvider.ShowDialog($"""
                                                 {Name}: Goto Selected Item Position Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogError(ex, "{Name}: Goto Selected Item Position", Name);
            }
        }).ConfigureAwait(false);
    }

    public async Task GenerateStageMapDocumnetAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            Guard.IsNotEmpty(CacheItem.StageMapTemplates);
            Guard.IsGreaterThan(CacheItem.WaferRadius, 0d);
            Guard.IsGreaterThan(CacheItem.DiePitchWidth, 0d);
            Guard.IsGreaterThan(CacheItem.DiePitchHeight, 0d);

            var cartesianCoordinatePositions = new Point[CacheItem.StageMapTemplates.Length]; // 笛卡尔坐标系可能BF/DF

            foreach (var (index, stageMapTemplate) in CacheItem.StageMapTemplates.Index())
            {
                var bfPosition = stageViewModel.MachineToBrightFieldPosition(stageMapTemplate.FindBFMachinePosition);

                if (IsDark)
                {
                    var cacheDFItem = (ChuckStageMapCacheDFItem)CacheItem;

                    var dfPosition = cibViewModel.GetCIBInformationPosition(
                        StageCoordinateSystemEnum.Dark,
                        cacheDFItem.ProductivityInformation,
                        cacheDFItem.CIBInformation,
                        bfPosition,
                        Cache.MicroscopeLensInformation);

                    cartesianCoordinatePositions[index] = dfPosition;
                }
                else
                {
                    cartesianCoordinatePositions[index] = bfPosition;
                }
            }

            StageMapDocument.RunDesign(() =>
            {
                StageMapDocument.Reset();

                Func<Point, Point> cartesianCoordinateToMachinePosition = IsDark ? stageViewModel.DarkFieldToMachinePosition : stageViewModel.BrightFieldToMachinePosition;

                var microscopeCalChip = applicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
                Guard.IsTrue(microscopeCalChip.IsOk, "Microscope Cal Chip is Empty or not verify.");

                var bfCircle = new Circle(stageViewModel.MachineToBrightFieldPosition(microscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), CacheItem.WaferRadius);
                var machineCircle = new Circle(cartesianCoordinateToMachinePosition(bfCircle.Center), bfCircle.Radius);

                StageMapDocument.WaferModel.Add(new StageMapWafer { Circle = machineCircle });

                var stageMapReticleBuilder = new StageMapDieBuilder
                {
                    DiePitchSize = new Size(CacheItem.DiePitchWidth, CacheItem.DiePitchHeight),
                    OriginalDiePoint = cartesianCoordinatePositions[0]
                };

                var cartesianCoordinateStageMapDies = stageMapReticleBuilder.BuildDie(bfCircle);
                Vector[] markers =
                [
                    .. cartesianCoordinatePositions.Select(tt => tt - cartesianCoordinatePositions[0])
                ];

                StageMapDocument.DieModel.AddRange(cartesianCoordinateStageMapDies.Select(t =>
                {
                    Point[] points = [.. markers.Select(tt => t.Rect.Point + tt)];

                    var resultExtents = new Extents();
                    resultExtents.Add(cartesianCoordinateToMachinePosition(t.Rect.XMinYMin));
                    resultExtents.Add(cartesianCoordinateToMachinePosition(t.Rect.XMaxYMax));

                    return new StageMapDie
                    {
                        Index = t.Index,
                        Row = t.Row,
                        Col = t.Col,
                        Rect = (Rect)resultExtents,
                        IsInWafer = points.All(bfCircle.Contains),
                        Markers = [.. points.Select(cartesianCoordinateToMachinePosition)]
                    };
                }));
            }, true);

            Guard.IsTrue(StageMapDocument.DieModel.Count > 0);
            Guard.IsTrue(StageMapDocument.DieModel.All(t => t.Markers.Length == CacheItem.StageMapTemplates.Length));
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task CalibrateAsync(Guid htmlLogUniqueId, CancellationToken cancellationToken, bool isCanSelected = false)
    {
        await Task.Run(async () =>
        {
            stageViewModel.SetEnableStageMap(false);

            StageMapDie[] stageMapDies;
            using (StageMapDocument.View.Sync.EnterScope()) stageMapDies = [.. StageMapDocument.DieModel];

            StageMapDie[] selectStageMapDies;
            if (isCanSelected)
            {
                foreach (var stageMapDie in stageMapDies) stageMapDie.IsInStageMapScan = false;

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
                        .RunAsync<StageMapDieSelectionGetter>(StageMapDocument.Edit, new SelectionInputOptions<StageMapDie>
                        {
                            IsMultipleSelection = true,
                            CancellationToken = cancellationToken
                        })
                        .ConfigureAwait(false);
                    Guard.IsTrue(inputResult.OutputResultModeEnum == OutputResultModeEnum.Ok);

                    selectStageMapDies = [.. inputResult.Output];
                }
            }
            else
            {
                selectStageMapDies = [.. stageMapDies];
            }

            StageMap = new StageMap();

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

            StageMap.IdealMatrix = [.. Generate.LinearRangeInt32(0, yLength - 1).Select(_ => new Point[xLength * CacheItem.StageMapTemplates.Length])];
            StageMap.ErrorMatrix = [.. Generate.LinearRangeInt32(0, yLength - 1).Select(_ => new Vector[xLength * CacheItem.StageMapTemplates.Length])];
            StageMap.IsInWaferMatrix = [.. Generate.LinearRangeInt32(0, yLength - 1).Select(_ => new bool[xLength * CacheItem.StageMapTemplates.Length])];
            StageMap.IsMatchMatrix = [.. Generate.LinearRangeInt32(0, yLength - 1).Select(_ => new bool[xLength * CacheItem.StageMapTemplates.Length])];

            for (var y = 0; y < yLength; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (var x = 0; x < xLength; x++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var stageMapIndexX = xDirection > 0 ? minX + x : maxX - x;
                    var stageMapIndexY = yDirection > 0 ? minY + y : maxY - y;

                    var stageMapDie = selectStageMapDies.Single(t => t.Index == new WaferMapDieIndex(stageMapIndexX, stageMapIndexY));

                    for (var markerIndex = 0; markerIndex < CacheItem.StageMapTemplates.Length; markerIndex++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var dfMachinePoint = stageMapDie.Markers[xDirection > 0 ? markerIndex : ^(markerIndex + 1)];
                        dfMachinePoint = dfMachinePoint.WithY(stageMapDie.Markers[0].Y);

                        var matrixColumn = x * CacheItem.StageMapTemplates.Length + markerIndex;

                        StageMap.IdealMatrix[y][matrixColumn] = dfMachinePoint;
                        StageMap.IsInWaferMatrix[y][matrixColumn] = stageMapDie.IsInWafer;
                    }
                }
            }

            StageMap.Refresh();

            await ScanStageMapAsync(
                StageMap,
                htmlLogUniqueId,
                cancellationToken).ConfigureAwait(false);

            logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());

            StageMap.CalculateErrorMatrix();
            StageMap.Refresh();

            logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
                .. StageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                .. StageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
            ]), htmlLogUniqueId.LoggingHtml());
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DFRepeatCalibrateAsync(string stepName, Guid htmlLogUniqueId, CancellationToken cancellationToken)
    {
        Guard.IsTrue(IsDark, "Step3 only support dark field");

        var microscopeCalChip = applicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
        Guard.IsTrue(microscopeCalChip.IsOk, "Microscope Cal Chip is Empty or not verify.");

        ChuckStageMap.DFRepeatStageMaps = [];

        var circle = new Circle(stageViewModel.DarkFieldToMachinePosition(stageViewModel.MachineToBrightFieldPosition(microscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel))), CacheItem.WaferRadius);

        var historyStageMapList = new List<StageMap>();

        bool isSuccess;
        var times = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentHtmlLogUniqueId = Guid.NewGuid();
            var fileName = $"Details_{stepName.Replace(" ", string.Empty)}_{times + 1}";

            if (times == 0)
            {
                await DownloadStageMapAsync(
                    StageMap,
                    circle,
                    currentHtmlLogUniqueId,
                    cancellationToken).ConfigureAwait(false);
            }

            logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {fileName}({currentHtmlLogUniqueId:N})"), htmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation($"{currentHtmlLogUniqueId:N}", HtmlHeaderLevelEnum.Header1, new HtmlComment(Name), currentHtmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header3, currentHtmlLogUniqueId.LoggingHtml());

            try
            {
                var scanStageMap = StageMap.Clone();
                scanStageMap.Reset();

                ChuckStageMap.DFRepeatStageMaps = [.. ChuckStageMap.DFRepeatStageMaps, scanStageMap];

                await ScanStageMapAsync(
                    scanStageMap,
                    currentHtmlLogUniqueId,
                    cancellationToken,
                    isInterpolateErrors: true,
                    interpolateStageMap: StageMap,
                    interpolateCircle: circle).ConfigureAwait(false);

                historyStageMapList.Add(scanStageMap.Clone());

                logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, currentHtmlLogUniqueId.LoggingHtml());

                isSuccess = scanStageMap.CanRepeatAndCalculateErrorMatrix(historyStageMapList, Cache.ChuckStageMapCacheDFItem.StageMapRepeatTimes);

                scanStageMap.Refresh();

                logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
                    .. scanStageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                    .. scanStageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
                ]), currentHtmlLogUniqueId.LoggingHtml());

                if (isSuccess || ++times > Cache.ChuckStageMapCacheDFItem.StageMapRepeatTimes - 1)
                {
                    StageMap.MergeErrorMatrix(scanStageMap, CacheItem.StageMapTemplates.Length);

                    StageMap.Refresh();

                    if (isSuccess)
                        logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlContainer([
                            .. StageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                            .. StageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
                        ]), htmlLogUniqueId.LoggingHtml());
                    else
                        logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlContainer([
                            new HtmlComment("More than the number of times."),
                            .. StageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                            .. StageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
                        ]), htmlLogUniqueId.LoggingHtml());

                    break;
                }
            }
            finally
            {
                logger.LogHtmlInformation(currentHtmlLogUniqueId.LoggedEndHtml(fileName));
            }
        }

        return isSuccess;
    }

    public async Task VerifyAsync(StageMap downloadStageMap, Guid htmlLogUniqueId, CancellationToken cancellationToken, bool isDownloadStageMap = false)
    {
        var microscopeCalChip = applicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
        Guard.IsTrue(microscopeCalChip.IsOk, "Microscope Cal Chip is Empty or not verify.");

        var brightFieldMachinePosition = microscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel);
        var circleCenter = IsDark
            ? stageViewModel.DarkFieldToMachinePosition(stageViewModel.MachineToBrightFieldPosition(brightFieldMachinePosition))
            : brightFieldMachinePosition;
        var circle = new Circle(circleCenter, CacheItem.WaferRadius);

        if (isDownloadStageMap == false)
        {
            await DownloadStageMapAsync(
                downloadStageMap,
                circle,
                htmlLogUniqueId,
                cancellationToken).ConfigureAwait(false);
        }

        VerifyStageMap = StageMap.Clone();
        VerifyStageMap.Reset();

        await ScanStageMapAsync(
            VerifyStageMap,
            htmlLogUniqueId,
            cancellationToken,
            isInterpolateErrors: IsDark,
            interpolateStageMap: StageMap,
            interpolateCircle: circle).ConfigureAwait(false);

        logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());

        VerifyStageMap.CalculateErrorMatrix();
        VerifyStageMap.Refresh();

        logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
            .. VerifyStageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
            .. VerifyStageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
        ]), htmlLogUniqueId.LoggingHtml());
    }

    private async Task ScanStageMapAsync(
        StageMap stageMap,
        Guid htmlLogUniqueId,
        CancellationToken cancellationToken,
        bool isInterpolateErrors = false,
        StageMap? interpolateStageMap = null,
        Circle? interpolateCircle = null)
    {
        logger.LogHtmlInformation("Scan StageMap", HtmlHeaderLevelEnum.Header3, new HtmlTable(
        [
            .. CacheItem.StageMapTemplates.Index().Select(t => new
            {
                t.Index,
                t.Item.FindBFMachinePosition,
                t.Item.TemplateROI,
                t.Item.TemplateFilePath,
                HtmlTab = new HtmlTab(new
                {
                    ResultImage = new HtmlImage(t.Item.OriginImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(t.Item.TemplateROI)]),
                    TemplateImage = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            })
        ]), htmlLogUniqueId.LoggingHtml());

        var (xDirection, yDirection) = stageViewModel.GetMachineDirection();

        var microscopeCalChip = applicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
        Guard.IsTrue(microscopeCalChip.IsOk, "Microscope Cal Chip is Empty or not verify.");

        double xPixelSize, yPixelSize;

        if (IsDark)
        {
            var cacheDFItem = (ChuckStageMapCacheDFItem)CacheItem;

            var xSize = applicationCookieService.GetCalibrations<CIBXPixelSizeDTO>(cancellationToken).SingleOrDefault(t => t.ProductivityInformation == cacheDFItem.ProductivityInformation);
            Guard.IsTrue(xSize?.IsOk == true, "Laser X Pixel Size is Empty or not verify.");

            var ySize = applicationCookieService.GetCalibrations<CIBYPixelSizeDTO>(cancellationToken).SingleOrDefault(t => t.ProductivityInformation.OpticsIlluminationModeEnum == cacheDFItem.ProductivityInformation.OpticsIlluminationModeEnum
                                                                                                                           && t.ProductivityInformation.OpticsMagType == cacheDFItem.ProductivityInformation.OpticsMagType
                                                                                                                           && t.PmtId == cacheDFItem.CIBInformation.PMTId);
            Guard.IsTrue(ySize?.IsOk == true, "Laser Y Pixel Size is Empty or not verify.");

            xPixelSize = xSize.XPixelSize;
            yPixelSize = ySize.YPixelSize;

            logger.LogHtmlInformation("Productivity Information", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                ProductivityInformation = new HtmlQuote(cacheDFItem.ProductivityInformation.ToHtmlAnonymous()),
                xDirection,
                yDirection,
                xPixelSize,
                yPixelSize,
                ChuckBFMachinePosition = microscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)
            }), htmlLogUniqueId.LoggingHtml());
        }
        else
        {
            var size = applicationCookieService.GetCalibrations<MicroscopePixelSizeDTO>(cancellationToken).SingleOrDefault(t => t.MicroscopeLensInformation == Cache.MicroscopeLensInformation);
            Guard.IsTrue(size?.IsOk == true, "Microscope Pixel Size is Empty or not verify.");

            xPixelSize = size.Result.PixelSize.Width;
            yPixelSize = size.Result.PixelSize.Height;

            logger.LogHtmlInformation("Pixel Size", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                xDirection,
                yDirection,
                xPixelSize,
                yPixelSize
            }), htmlLogUniqueId.LoggingHtml());
        }

        if (IsDark == false)
        {
            stageViewModel.SetBrightFieldAbsoluteStageXy(stageViewModel.MachineToBrightFieldPosition(microscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), CalChipSiteModelEnum.ChuckModel);
            await microscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        HTuple[] templateIds = [];

        try
        {
            templateIds =
            [
                .. CacheItem.StageMapTemplates
                    .Select(t =>
                    {
                        calibrationAlgorithmService.TryReadTemplate(CacheItem.TemplateMatchSetting.ToHalconTemplateType(), t.TemplateFilePath, out var templateId);

                        return templateId;
                    })
            ];

            var templateMatchScoreThreshold = CacheItem.TemplateMatchSetting.ToTemplateMatchScoreThreshold(calibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum, calibrationSetting);

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
                var errors = Guard.IsNotNullAndReturn(interpolateStageMap).InterpolateErrors(stageMapIdealMatrix, Guard.IsNotNullAndReturn(interpolateCircle));

                for (var y = 0; y < yLength; y++)
                {
                    for (var x = 0; x < xLength; x++)
                    {
                        var vector = errors[y][x].WithY(0);
                        stageMapIdealMatrix[y][x] += vector;
                    }
                }
            }

            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var linkedToken = linkedCancellationTokenSource.Token;

            var channel = Channel.CreateBounded<ScanYResult>(new BoundedChannelOptions(2)
            {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = true
            });

            var writerTask = Task.Run(async () =>
            {
                (BitmapImage Image, string RawImageFilePath)[]? pendingImages = null;
                try
                {
                    for (var y = 0; y < yLength; y++)
                    {
                        linkedToken.ThrowIfCancellationRequested();

                        var isInWaferColumnIndexes = stageMap.IsInWaferMatrix[y]
                            .Index()
                            .Where(t => t.Item)
                            .Select(t => t.Index)
                            .ToArray();

                        var points = isInWaferColumnIndexes.Select(t => stageMapIdealMatrix[y][t]).ToArray();
                        if (points.Length == 0) continue;

                        if (IsDark)
                        {
                            var cacheDFItem = (ChuckStageMapCacheDFItem)CacheItem;

                            stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(points[0], calChipSiteModelEnum: CalChipSiteModelEnum.ChuckModel);
                            DarkFieldImageDTO[] darkFieldImages =
                            [
                                .. await cibViewModel.GetPMTImagesAsync(
                                    cacheDFItem.ProductivityInformation,
                                    StageCoordinateSystemEnum.Machine,
                                    points,
                                    cacheDFItem.ImageWidth,
                                    cacheDFItem.CIBInformation,
                                    (true, null),
                                    (false, cacheDFItem.OpticsConfiguration),
                                    (false, cacheDFItem.CIBConfiguration),
                                    (false, cacheDFItem.LaserLightInformation),
                                    false,
                                    linkedToken).ConfigureAwait(false)
                            ];

                            Guard.IsEqualTo(points.Length, darkFieldImages.Length);

                            var rowImages = darkFieldImages.Select(t => (t.Image, t.RawImageFilePath)).ToArray();
                            pendingImages = rowImages;

                            await channel.Writer.WriteAsync(new ScanYResult(y, isInWaferColumnIndexes, rowImages), linkedToken).ConfigureAwait(false);

                            pendingImages = null;
                        }
                        else
                        {
                            var images = new BitmapImage[points.Length];

                            var scanDirection = (y & 1) == 0 ? 1 : -1;
                            for (var i = 0; i < points.Length; i++)
                            {
                                var index = scanDirection > 0 ? i : ^(i + 1);
                                stageViewModel.SetMachineAbsoluteStageXy(points[index], CalChipSiteModelEnum.ChuckModel, true);
                                await Task.Delay(100, linkedToken).ConfigureAwait(false);

                                images[index] = reviewViewModel.GetBrightFieldImage();
                            }

                            Guard.IsEqualTo(points.Length, images.Length);

                            var rowImages = images.Select(t => (t, string.Empty)).ToArray();
                            pendingImages = rowImages;

                            await channel.Writer.WriteAsync(new ScanYResult(y, isInWaferColumnIndexes, rowImages), linkedToken).ConfigureAwait(false);

                            pendingImages = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (pendingImages is not null)
                    {
                        foreach (var (image, _) in pendingImages)
                        {
                            using var _ = image;
                        }
                    }

                    channel.Writer.TryComplete(ex);
                }
                finally
                {
                    channel.Writer.TryComplete();
                }
            }, linkedToken);

            try
            {
                await foreach (var row in channel.Reader.ReadAllAsync(linkedToken).ConfigureAwait(false))
                {
                    await TemplateMatchAsync(row).ConfigureAwait(false);
                }
            }
            finally
            {
                try
                {
                    if (writerTask.IsCompleted == false) await linkedCancellationTokenSource.CancelAsync().ConfigureAwait(false);

                    await writerTask.ConfigureAwait(false);
                }
                finally
                {
                    while (channel.Reader.TryRead(out var row))
                    {
                        foreach (var (image, _) in row.Images)
                        {
                            using var _ = image;
                        }
                    }
                }
            }

            stageMap.Refresh();

            logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
                .. stageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                .. stageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
            ]), htmlLogUniqueId.LoggingHtml());

            return;

            async Task TemplateMatchAsync(ScanYResult yResult)
            {
                var (y, isInWaferColumnIndexes, images) = yResult;

                var htmlLogs = new HtmlLog[isInWaferColumnIndexes.Length];

                try
                {
                    await Parallel.ForEachAsync(isInWaferColumnIndexes.Index(), new ParallelOptions
                    {
                        CancellationToken = linkedToken,
                        MaxDegreeOfParallelism = Environment.ProcessorCount
                    }, (item, token) =>
                    {
                        token.ThrowIfCancellationRequested();

                        var (i, isInWaferColumnIndex) = item;
                        var templateIdIndex = xDirection > 0 ? i % CacheItem.StageMapTemplates.Length : ^(i % CacheItem.StageMapTemplates.Length + 1);

                        var templateROI = CacheItem.StageMapTemplates[templateIdIndex].TemplateROI;

                        using var image = IsDark
                            ? xDirection > 0 ? images[i].Image : images[i].Image.HorizontalFlip()
                            : images[i].Image;

                        var isSuccess = calibrationAlgorithmService.TryTemplateMatchToOffset(
                            CacheItem.TemplateMatchSetting.ToHalconTemplateType(),
                            image,
                            templateIds[templateIdIndex],
                            htmlLogUniqueId,
                            out var matchPoint,
                            out var matchOffset,
                            out var matchScore,
                            out var matchAngle);
                        var oldMatchPoint = matchPoint;
                        var oldMatchOffset = matchOffset;
                        var offset = matchPoint - CacheItem.StageMapTemplates[templateIdIndex].TemplateROI.Center;
                        matchOffset = new Point(offset.X, -offset.Y);

                        var resultImageFilePath = Path.Combine(isSuccess ? ImageFileDirectory : $"{FileHelper.GetFileFullName(CacheItem.StageMapTemplates[templateIdIndex].TemplateFilePath)}{Name}_Error", $"Origin_Score({matchScore:0.###},{templateMatchScoreThreshold:0.###})_Angle{matchAngle:0.###}_{Guid.NewGuid():N}.jpg");
                        image.SaveImage(resultImageFilePath);

                        var vector = new Vector(xDirection * matchOffset.X * xPixelSize, yDirection * matchOffset.Y * yPixelSize);

                        htmlLogs[i] =
                            new HtmlLog(
                                isSuccess ? LogLevelEnum.Info : LogLevelEnum.Error,
                                DateTime.Now.ToString(Constants.LongFileDateTimeFormat),
                                CugaCalibrationSolutionAssemblyMetadata.Version,
                                [
                                    new HtmlBullet(new
                                    {
                                        isInWaferColumnIndex = isSuccess ? $"{isInWaferColumnIndex} Ok" : $"{isInWaferColumnIndex} Error",
                                        IdealPoint = stageMap.IdealMatrix[y][isInWaferColumnIndex],
                                        templateROI,
                                        ImageSize = image.Size,
                                        oldMatchPoint,
                                        oldMatchOffset,
                                        matchPoint,
                                        matchOffset,
                                        matchScore,
                                        matchAngle,
                                        templateMatchScoreThreshold,
                                        vector,
                                        images[i].RawImageFilePath,
                                        HtmlTab = new HtmlTab(new
                                        {
                                            ResultImage = new HtmlImage(resultImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(templateROI), new HtmlImageCrossOverlay(matchPoint)])
                                        })
                                    })
                                ]
                            );

                        stageMap.IsMatchMatrix[y][isInWaferColumnIndex] = isSuccess;
                        if (isSuccess) stageMap.ErrorMatrix[y][isInWaferColumnIndex] = vector;

                        stageMap.Refresh();

                        return ValueTask.CompletedTask;
                    }).ConfigureAwait(false);

                    stageMap.Refresh();

                    logger.LogHtmlInformation($"{y + 1} row", HtmlHeaderLevelEnum.Header5, new HtmlContainer([.. htmlLogs]), htmlLogUniqueId.LoggingHtml());
                }
                finally
                {
                    foreach (var (image, _) in yResult.Images)
                    {
                        using var _ = image;
                    }
                }
            }
        }
        finally
        {
            foreach (var templateId in templateIds) calibrationAlgorithmService.TryCleanTemplate(CacheItem.TemplateMatchSetting.ToHalconTemplateType(), templateId);
            stageViewModel.SetBrightFieldAbsoluteStageXy(stageViewModel.MachineToBrightFieldPosition(microscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), CalChipSiteModelEnum.ChuckModel);
        }
    }

    private async Task DownloadStageMapAsync(
        StageMap downloadStageMap,
        Circle? targetCircle,
        Guid htmlLogUniqueId,
        CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            stageViewModel.SetEnableStageMap(false);

            var stageMapErrorDTO = targetCircle is not null ? downloadStageMap.AdaptTo(targetCircle.Value) : downloadStageMap.AdaptTo();
            var xPoint3DList = new List<Point3D>();
            var yPoint3DList = new List<Point3D>();

            foreach (var (y, row) in stageMapErrorDTO.Ys.Index())
            {
                foreach (var (x, col) in row.Xs.Index())
                {
                    xPoint3DList.Add(new Point3D(stageMapErrorDTO.BaseX + x * stageMapErrorDTO.XStep, stageMapErrorDTO.BaseY + y * stageMapErrorDTO.YStep, col.Error.X));
                    yPoint3DList.Add(new Point3D(stageMapErrorDTO.BaseX + x * stageMapErrorDTO.XStep, stageMapErrorDTO.BaseY + y * stageMapErrorDTO.YStep, col.Error.Y));
                }
            }

            logger.LogHtmlInformation("Download", HtmlHeaderLevelEnum.Header3, new HtmlContainer(
            [
                new HtmlPlot3DChart(xPoint3DList, "X Error", HtmlPlot3DType.Surface),
                new HtmlPlot3DChart(yPoint3DList, "Y Error", HtmlPlot3DType.Surface)
            ]), htmlLogUniqueId.LoggingHtml());

            stageViewModel.SetStageMap(stageMapErrorDTO);

            stageViewModel.SetEnableStageMap(true);
        }, cancellationToken).ConfigureAwait(false);
    }

    private sealed record ScanYResult(int Y, int[] IsInWaferColumnIndexes, (BitmapImage Image, string RawImageFilePath)[] Images);
}