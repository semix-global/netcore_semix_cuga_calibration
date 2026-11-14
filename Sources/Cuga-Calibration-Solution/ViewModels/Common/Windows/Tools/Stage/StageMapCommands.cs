using System.Globalization;
using System.IO;
using System.Threading.Channels;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Algorithm.Extensions;
using Core.Algorithm.Helper;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.TemplateMatch;
using Core.Algorithm.Models.TemplateMatch;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.CIB.XPixelSize;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.PixelSize;
using Core.Models.Models.Setting;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
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
    ReviewViewModel reviewViewModel,
    CIBViewModel cibViewModel,
    OpticsViewModel opticsViewModel,
    LaserViewModel laserViewModel,
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    CalibrationSetting calibrationSetting,
    IApplicationCookieService applicationCookieService,
    IAlgorithmEngines algorithmEngines,
    CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel,
    AlignmentUserControlViewModel alignmentUserControlViewModel,
    ILogger<StageMapCommands> logger) : ObservableObject
{
    public AlignmentUserControlViewModel AlignmentUserControlViewModel { get; } = alignmentUserControlViewModel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(Name),
        nameof(CacheItem),
        nameof(StageMapDocument),
        nameof(StageMap),
        nameof(VerifyStageMap),
        nameof(AlignmentResult))]
    public partial bool IsDark { get; set; } = true;

    public ILogger Logger { get; set; } = logger;

    public string ImageFileDirectory { get; set; } = string.Empty;

    public string TemplateFileDirectory { get; set; } = string.Empty;

    public CalChipSiteModelEnum CalChipSiteModelEnum { get; set; }

    public MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CacheItem))]
    public partial ChuckStageMapCache Cache { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(StageMapDocument),
        nameof(StageMap),
        nameof(VerifyStageMap),
        nameof(AlignmentResult))]
    public partial ChuckStageMapDTO ChuckStageMap { get; set; } = new();

    #region 明暗场的区分属性

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

    #endregion

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task AddStageMapTemplateAsync(bool? isCreateTemplate, CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                Guard.IsNotNull(isCreateTemplate);

                Guard.IsEqualTo(Cache.MicroscopeLensInformation, microscopeViewModel.GetCurrentMicroscopeLensInformation());

                var stageMapTemplatePoint = new StageMapTemplate
                {
                    FindBFMachinePosition = stageViewModel.GetMachineStagePosition()
                };

                if (isCreateTemplate == true)
                {
                    if (CacheItem.StageMapTemplates.Length > 0 && stageMapTemplatePoint.FindBFMachinePosition.Y - CacheItem.StageMapTemplates[0].FindBFMachinePosition.Y != 0) // 选择的点Y必须相同
                    {
                        dialogWindowProvider.ShowDialog($"{Name}: Add Template Warning: Y Position Must Be Same!", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                        return;
                    }

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

                        stageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(dfPosition, CalChipSiteModelEnum);

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

                            var originImageFilePath = Path.Combine(TemplateFileDirectory, $"{Cache.MicroscopeLensInformation}_{cacheDFItem.ProductivityInformation}_{cacheDFItem.CIBInformation}", $"DF_{Guid.NewGuid():N}.jpg");
                            darkFieldImageDto.Image.SaveImage(originImageFilePath);
                            stageMapTemplatePoint.OriginImageFilePath = originImageFilePath;
                        }
                        finally
                        {
                            stageViewModel.SetBrightFieldAbsoluteStageXy(bfPosition, CalChipSiteModelEnum);
                        }
                    }
                    else
                    {
                        var originImageFilePath = Path.Combine(TemplateFileDirectory, Cache.MicroscopeLensInformation.ToString(), $"BF_{Guid.NewGuid():N}.jpg");

                        using var image = reviewViewModel.GetBrightFieldImage();
                        image.SaveImage(originImageFilePath);
                        stageMapTemplatePoint.OriginImageFilePath = originImageFilePath;
                    }

                    stageMapTemplatePoint.TemplateFilePath = $"{stageMapTemplatePoint.OriginImageFilePath}_Template";
                    createDarkImageTemplateWindowViewModel.ImageFilePath = stageMapTemplatePoint.OriginImageFilePath;
                    createDarkImageTemplateWindowViewModel.TemplateFilePath = stageMapTemplatePoint.TemplateFilePath;
                    createDarkImageTemplateWindowViewModel.TemplateMatchSetting = CacheItem.TemplateMatchSetting.Clone();

                    Guard.IsTrue(windowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel) == true, nameof(createDarkImageTemplateWindowViewModel));

                    stageMapTemplatePoint.TemplateROI = createDarkImageTemplateWindowViewModel.Rect;
                    stageMapTemplatePoint.TemplateImageFilePath = createDarkImageTemplateWindowViewModel.TemplateImageFilePath;
                }
                else
                {
                    if (CacheItem.StageMapTemplates.Length > 0) // 只能一个点
                    {
                        dialogWindowProvider.ShowDialog($"{Name}: Add Template Warning: Only One Point!", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                        return;
                    }
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
                Logger.LogError(ex, "{Name}: Add Template", Name);
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

                Point brightFieldPosition;
                if (IsDark)
                {
                    var cacheDFItem = (ChuckStageMapCacheDFItem)CacheItem;

                    var darkFieldPosition = stageViewModel.MachineToDarkFieldPosition(stageMapDie.Markers[index]);

                    brightFieldPosition = darkFieldPosition - cibViewModel.GetCIBInformationOffset(
                        StageCoordinateSystemEnum.Dark,
                        cacheDFItem.ProductivityInformation,
                        cacheDFItem.CIBInformation,
                        Cache.MicroscopeLensInformation,
                        isLineCentricityOffset: false);
                }
                else
                {
                    brightFieldPosition = stageViewModel.MachineToBrightFieldPosition(stageMapDie.Markers[index]);
                }

                stageViewModel.SetBrightFieldAbsoluteStageXy(microscopeViewModel.GetMicroscopeLensInformationPosition(
                    Cache.MicroscopeLensInformation,
                    microscopeViewModel.GetCurrentMicroscopeLensInformation(),
                    brightFieldPosition), CalChipSiteModelEnum);

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
                Logger.LogError(ex, "{Name}: Goto Selected Item Position", Name);
            }
        }).ConfigureAwait(false);
    }

    public async Task AlignmentThenApplyTemplatesAsync(Guid htmlLogUniqueId, CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            Guard.IsTrue(MicroscopeCalChip.IsOk, "Microscope Cal Chip is Empty or not verify.");

            AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum;

            if (IsDark)
            {
                var cacheDFItem = (ChuckStageMapCacheDFItem)CacheItem;
                AlignmentUserControlViewModel.ProductivityInformation = cacheDFItem.ProductivityInformation;

                if (dialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment?",
                        out var dialogResult,
                        DialogButtonsEnum.YesNo,
                        DialogIconEnum.Question) == true && dialogResult == DialogResultEnum.Yes) ChuckStageMap.IsDarkFieldAlignment = AlignmentUserControlViewModel.IsDarkFieldAlignment = true;
                else ChuckStageMap.IsDarkFieldAlignment = AlignmentUserControlViewModel.IsDarkFieldAlignment = false;
            }
            else
            {
                AlignmentUserControlViewModel.IsDarkFieldAlignment = false;
            }

            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            var newAlignmentResult = AlignmentUserControlViewModel.AlignmentResult;
            if (AlignmentResult.MarkPoint1 != Point.Origin
                && AlignmentResult.MarkPoint2 != Point.Origin
                && CacheItem.StageMapTemplates.Length > 0)
            {
                var oldMarkPoint1 = AlignmentResult.MarkPoint1;
                var oldMarkPoint2 = AlignmentResult.MarkPoint2;

                var newMarkPoint1 = newAlignmentResult.MarkPoint1;
                var newMarkPoint2 = newAlignmentResult.MarkPoint2;

                var offset = (newMarkPoint1 - oldMarkPoint1 + (newMarkPoint2 - oldMarkPoint2)) / 2d;

                foreach (var stageMapTemplate in CacheItem.StageMapTemplates) stageMapTemplate.FindBFMachinePosition += offset;

                using var scope = StageMapDocument.View.Sync.EnterScope();
                StageMapDocument.Reset();
            }

            AlignmentResult = newAlignmentResult;

            stageViewModel.SetBrightFieldAbsoluteStageXy(stageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum)), CalChipSiteModelEnum);

            logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                AlignmentUserControlViewModel.CalChipSiteModelEnum,
                AlignmentUserControlViewModel.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(AlignmentResult)
            }), htmlLogUniqueId.LoggingHtml());
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task GenerateStageMapDocumentAsync(Guid htmlLogUniqueId, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            Guard.IsTrue(MicroscopeCalChip.IsOk, "Microscope Cal Chip is Empty or not verify.");

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

                    Guard.IsTrue(cacheDFItem.CIBInformation.PMTId == calibrationSetting.SettingCommonParam.MainCIBInformation.PMTId, "PMT Id Not Match!"); // 不允许偏执(StageMap只看中心光斑, 副作用, 如果swath规划不是按照8号规划可能出现Y方向的一些问题)

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

            Logger.LogHtmlInformation("Templates", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                CalChipPosition = MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum),
                Table = new HtmlTable(
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
                        }),
                        CartesianCoordinatePosition = cartesianCoordinatePositions[t.Index]
                    })
                ])
            }), htmlLogUniqueId.LoggingHtml());

            StageMapDocument.RunDesign(() =>
            {
                StageMapDocument.Reset();

                Func<Point, Point> cartesianCoordinateToMachinePosition = IsDark ? stageViewModel.DarkFieldToMachinePosition : stageViewModel.BrightFieldToMachinePosition;

                var cartesianCircle = new Circle(stageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum)), CacheItem.WaferRadius);
                var machineCircle = new Circle(cartesianCoordinateToMachinePosition(cartesianCircle.Center), cartesianCircle.Radius);

                StageMapDocument.WaferModel.Add(new StageMapWafer { Circle = machineCircle });

                var stageMapReticleBuilder = new StageMapDieBuilder
                {
                    DiePitchSize = new Size(CacheItem.DiePitchWidth, CacheItem.DiePitchHeight),
                    OriginalDiePoint = cartesianCoordinatePositions[0]
                };

                var cartesianCoordinateStageMapDies = stageMapReticleBuilder.BuildDie(cartesianCircle);
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
                        IsInWafer = points.All(cartesianCircle.Contains),
                        Markers = [.. points.Select(cartesianCoordinateToMachinePosition)]
                    };
                }));
            }, true);

            Guard.IsTrue(StageMapDocument.DieModel.Count > 0);
            Guard.IsTrue(StageMapDocument.DieModel.All(t => t.Markers.Length == CacheItem.StageMapTemplates.Length));
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task CalibrateAsync(bool isCanSelected, Guid htmlLogUniqueId, CancellationToken cancellationToken)
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

            var stageMapDieByIndex = selectStageMapDies.ToDictionary(t => t.Index);
            Guard.IsTrue(stageMapDieByIndex.Count == yLength * xLength, "Selected dies must form a complete rectangle.");

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

                    var stageMapDie = stageMapDieByIndex[new WaferMapDieIndex(stageMapIndexX, stageMapIndexY)];

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

            Logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());

            StageMap.CalculateErrorMatrix();
            StageMap.Refresh();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
                .. StageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                .. StageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
            ]), htmlLogUniqueId.LoggingHtml());
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DFRepeatCalibrateAsync(Guid htmlLogUniqueId, CancellationToken cancellationToken)
    {
        Guard.IsTrue(IsDark, "Repeat scan only supports dark field.");
        Guard.IsTrue(MicroscopeCalChip.IsOk, "Microscope Cal Chip is Empty or not verify.");

        ChuckStageMap.DFRepeatStageMaps = [];

        var circle = new Circle(stageViewModel.DarkFieldToMachinePosition(stageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum))), CacheItem.WaferRadius);

        var historyStageMapList = new List<StageMap>();

        bool isSuccess;
        var times = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());

            if (times == 0)
            {
                await DownloadStageMapAsync(
                    StageMap,
                    htmlLogUniqueId,
                    cancellationToken,
                    isInterpolateErrorsAdaptTo: true,
                    interpolateCircle: circle).ConfigureAwait(false);
            }

            var scanStageMap = StageMap.Clone();
            scanStageMap.Reset();

            ChuckStageMap.DFRepeatStageMaps = [.. ChuckStageMap.DFRepeatStageMaps, scanStageMap];

            await ScanStageMapAsync(
                scanStageMap,
                htmlLogUniqueId,
                cancellationToken,
                isInterpolateErrors: true,
                interpolateStageMap: StageMap,
                interpolateCircle: circle).ConfigureAwait(false);

            historyStageMapList.Add(scanStageMap.Clone());

            Logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());

            isSuccess = scanStageMap.CanRepeatAndCalculateErrorMatrix(historyStageMapList, Cache.ChuckStageMapCacheDFItem.StageMapRepeatTimes);

            scanStageMap.Refresh();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
                .. scanStageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                .. scanStageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
            ]), htmlLogUniqueId.LoggingHtml());

            if (isSuccess || ++times > Cache.ChuckStageMapCacheDFItem.StageMapRepeatTimes - 1)
            {
                StageMap.MergeErrorMatrix(scanStageMap, CacheItem.StageMapTemplates.Length);

                StageMap.Refresh();

                if (isSuccess)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlContainer([
                        .. StageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                        .. StageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
                    ]), htmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlContainer([
                        new HtmlComment("More than the number of times."),
                        .. StageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                        .. StageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
                    ]), htmlLogUniqueId.LoggingHtml());

                break;
            }
        }

        return isSuccess;
    }

    public async Task VerifyAsync(
        StageMap downloadStageMap,
        Guid htmlLogUniqueId,
        CancellationToken cancellationToken,
        bool isDownloadStageMap = true,
        bool isInterpolateErrorsAdaptTo = true)
    {
        Guard.IsTrue(MicroscopeCalChip.IsOk, "Microscope Cal Chip is Empty or not verify.");

        var brightFieldMachinePosition = MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum);
        var circleCenter = IsDark
            ? stageViewModel.DarkFieldToMachinePosition(stageViewModel.MachineToBrightFieldPosition(brightFieldMachinePosition))
            : brightFieldMachinePosition;
        var circle = new Circle(circleCenter, CacheItem.WaferRadius);

        if (isDownloadStageMap)
        {
            await DownloadStageMapAsync(
                downloadStageMap,
                htmlLogUniqueId,
                cancellationToken,
                isInterpolateErrorsAdaptTo: isInterpolateErrorsAdaptTo,
                interpolateCircle: circle).ConfigureAwait(false);
        }

        VerifyStageMap = StageMap.Clone();
        VerifyStageMap.Reset();

        await ScanStageMapAsync(
            VerifyStageMap,
            htmlLogUniqueId,
            cancellationToken,
            isInterpolateErrors: IsDark,
            interpolateStageMap: downloadStageMap,
            interpolateCircle: circle).ConfigureAwait(false);

        Logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

        VerifyStageMap.CalculateErrorMatrix();
        VerifyStageMap.Refresh();

        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlContainer([
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
        Logger.LogHtmlInformation("Scan StageMap", HtmlHeaderLevelEnum.Header3, new HtmlTable(
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
        Guard.IsTrue(MicroscopeCalChip.IsOk, "Microscope Cal Chip is Empty or not verify.");

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

            Logger.LogHtmlInformation("Productivity Information", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                ProductivityInformation = new HtmlQuote(cacheDFItem.ProductivityInformation.ToHtmlAnonymous()),
                xDirection,
                yDirection,
                xPixelSize,
                yPixelSize,
                ChuckBFMachinePosition = MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum)
            }), htmlLogUniqueId.LoggingHtml());
        }
        else
        {
            var size = applicationCookieService.GetCalibrations<MicroscopePixelSizeDTO>(cancellationToken).SingleOrDefault(t => t.MicroscopeLensInformation == Cache.MicroscopeLensInformation);
            Guard.IsTrue(size?.IsOk == true, "Microscope Pixel Size is Empty or not verify.");

            xPixelSize = size.Result.PixelSize.Width;
            yPixelSize = size.Result.PixelSize.Height;

            Logger.LogHtmlInformation("Pixel Size", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                xDirection,
                yDirection,
                xPixelSize,
                yPixelSize
            }), htmlLogUniqueId.LoggingHtml());
        }

        if (IsDark == false)
        {
            stageViewModel.SetBrightFieldAbsoluteStageXy(stageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum)), CalChipSiteModelEnum);
            await microscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        var templateMatchEngine = algorithmEngines.TemplateMatch(calibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum);

        var scanHtmlLogUniqueId = Guid.NewGuid();
        var scanHtmlLogFileName = $"Details_{Name}_Rows({scanHtmlLogUniqueId:N})";

        var templateMatchModels = new ITemplateMatchModel[CacheItem.StageMapTemplates.Length];
        try
        {
            foreach (var (index, template) in CacheItem.StageMapTemplates.Index())
            {
                var result = templateMatchEngine.Read(
                    CacheItem.TemplateMatchSetting.ToReadParam(calibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum, template.TemplateFilePath),
                    scanHtmlLogUniqueId);
                Guard.IsTrue(result.Success, result.ErrorMsg);

                templateMatchModels[index] = Guard.IsNotNullAndReturn(result.Data);
            }

            var templateMatchScoreThreshold = CacheItem.TemplateMatchSetting.ToTemplateMatchScoreThreshold(calibrationSetting);

            Point[][] stageMapIdealMatrix =
            [
                .. stageMap.IdealMatrix.Select<Point[], Point[]>(t =>
                [
                    .. t.Select(tt => tt)
                ])
            ];
            var (yLength, xLength) = stageMapIdealMatrix.GetYXLength();

            Logger.LogHtmlInformation("rows", HtmlHeaderLevelEnum.Header4, new HtmlComment($"Detail file prefix: {scanHtmlLogFileName}"), htmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation("rows", HtmlHeaderLevelEnum.Header1, new HtmlComment(Name), scanHtmlLogUniqueId.LoggingHtml());

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

            var channel = Channel.CreateBounded<ScanYResult>(new BoundedChannelOptions(yLength)
            {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = false
            });

            if (IsDark)
            {
                var cacheDFItem = (ChuckStageMapCacheDFItem)CacheItem;

                opticsViewModel.SetOpticsConfiguration(cacheDFItem.OpticsConfiguration);
                cibViewModel.SetCIBConfiguration([cacheDFItem.CIBInformation], cacheDFItem.CIBConfiguration);
                laserViewModel.SetPrescanAODWaveProfileByCoefficient(cacheDFItem.ProductivityInformation, cacheDFItem.LaserLightInformation.Coefficient);
                laserViewModel.SetChirpAODWaveProfile(cacheDFItem.ProductivityInformation);
            }

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

                            stageViewModel.SetMachineAbsoluteStageXy(points[0], CalChipSiteModelEnum, false);
                            DarkFieldImageDTO[] darkFieldImages =
                            [
                                .. await cibViewModel.GetPMTImagesAsync(
                                    cacheDFItem.ProductivityInformation,
                                    StageCoordinateSystemEnum.Machine,
                                    points,
                                    cacheDFItem.ImageWidth,
                                    cacheDFItem.CIBInformation,
                                    (true, null),
                                    (true, null),
                                    (true, null),
                                    (true, null),
                                    true,
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
                                stageViewModel.SetMachineAbsoluteStageXy(points[index], CalChipSiteModelEnum, true);
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

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
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
                        var templateIdIndex = xDirection > 0 ? isInWaferColumnIndex % CacheItem.StageMapTemplates.Length : ^(isInWaferColumnIndex % CacheItem.StageMapTemplates.Length + 1);

                        var templateROI = CacheItem.StageMapTemplates[templateIdIndex].TemplateROI;

#pragma warning disable IDE0079
#pragma warning disable IDISP007

                        using var originImage = images[i].Image;

#pragma warning restore IDISP007
#pragma warning restore IDE0079

                        using var image = IsDark
                            ? xDirection > 0 ? originImage : originImage.HorizontalFlip()
                            : originImage;

                        var templateMatchRet = templateMatchEngine.Match(
                            image,
                            new MatchTemplateParam { Model = templateMatchModels[templateIdIndex], ScoreThreshold = templateMatchScoreThreshold },
                            scanHtmlLogUniqueId);

                        var isSuccess = templateMatchRet.Success;
                        var matchPoint = templateMatchRet.Data?.MarkPoint ?? Point.Origin;
                        var matchOffset = templateMatchRet.Data?.Offset ?? Point.Origin;
                        var matchScore = templateMatchRet.Data?.Score ?? 0d;
                        var matchAngle = templateMatchRet.Data?.Angle ?? 0d;

                        var oldMatchPoint = matchPoint;
                        var oldMatchOffset = matchOffset;
                        var templateCenter = templateROI.Center + (Vector)templateMatchModels[templateIdIndex].CenterOffset;
                        var offset = matchPoint - templateCenter;
                        matchOffset = new Point(offset.X, -offset.Y);

                        var resultImageFilePath = Path.Combine(isSuccess ? ImageFileDirectory : $"{FileHelper.GetFileFullName(CacheItem.StageMapTemplates[templateIdIndex].TemplateFilePath)}{Name}_Error", $"Origin_Score({matchScore:0.###},{templateMatchScoreThreshold:0.###})_Angle{matchAngle:0.###}_{Guid.NewGuid():N}.jpg");
                        image.SaveImage(resultImageFilePath);

                        var vector = new Vector(xDirection * matchOffset.X * xPixelSize, yDirection * matchOffset.Y * yPixelSize);

                        htmlLogs[i] =
                            new HtmlLog(
                                isSuccess ? LogLevelEnum.Info : LogLevelEnum.Error,
                                DateTime.Now.ToString(Constants.LongFileDateTimeFormat, CultureInfo.InvariantCulture),
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
                                            ResultImage = new HtmlImage(resultImageFilePath, htmlImageOverlays: TemplateMatchHtmlOverlay.CreateMatchResultOverlays(matchPoint, templateROI.Size, templateMatchModels[templateIdIndex].CenterOffset))
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

                    Logger.LogHtmlInformation($"{y + 1} row", HtmlHeaderLevelEnum.Header2, new HtmlContainer([.. htmlLogs]), scanHtmlLogUniqueId.LoggingHtml());
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
            Logger.LogHtmlInformation(scanHtmlLogUniqueId.LoggedEndHtml(fileName: scanHtmlLogFileName));

            foreach (var templateMatchModel in templateMatchModels) templateMatchModel.Dispose();
            stageViewModel.SetBrightFieldAbsoluteStageXy(stageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum)), CalChipSiteModelEnum);
        }
    }

    private async Task DownloadStageMapAsync(
        StageMap downloadStageMap,
        Guid htmlLogUniqueId,
        CancellationToken cancellationToken,
        bool isInterpolateErrorsAdaptTo = false,
        Circle? interpolateCircle = null)
    {
        await Task.Run(() =>
        {
            stageViewModel.SetEnableStageMap(false);

            var stageMapErrorDTO = isInterpolateErrorsAdaptTo
                ? downloadStageMap.InterpolateErrorsAdaptTo(Guard.IsNotNullAndReturn(interpolateCircle))
                : downloadStageMap.AdaptTo();

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

            Logger.LogHtmlInformation("Download", HtmlHeaderLevelEnum.Header4, new HtmlContainer(
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