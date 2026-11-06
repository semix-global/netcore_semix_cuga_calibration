using System.IO;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Algorithm.Models.TemplateMatch;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Setting;
using CugaCalibration.Core.Services.Interfaces;
using Humanizer;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WaferMap.WPF.Primitives;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;

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
    ILogger<StageMapCommands> logger) : ObservableObject
{
    public bool IsDark { get; set; } = true;

    public string TemplateFileDirectory { get; set; } = string.Empty;

    public ChuckStageMapCache Cache { get; set; } = new();

    public ChuckStageMapDTO ChuckStageMap { get; set; } = new();

    public string Name => $"{(IsDark ? "Dark" : "Bright")} {nameof(StageMapCommands).Humanize(LetterCasing.Title)}";

    public ChuckStageMapCacheBFItem CacheItem => IsDark ? Cache.ChuckStageMapCacheDFItem : Cache.ChuckStageMapCacheBFItem;

    public StageMapDocument StageMapDocument => IsDark ? ChuckStageMap.DFStageMapDocument : ChuckStageMap.BFStageMapDocument;

    public StageMap StageMap => IsDark ? ChuckStageMap.DFStageMap : ChuckStageMap.BFStageMap;

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
                            cancellationToken);

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

                    var generateTemplateParam = CacheItem.TemplateMatchSetting.ToGenerateParam(
                        calibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum,
                        reviewViewModel.GetBrightFieldImagePixelSize(),
                        stageMapTemplatePoint.TemplateFilePath);
                    Guard.IsTrue(reviewViewModel.TryGenerateTemplate(generateTemplateParam, Guid.Empty));

                    stageMapTemplatePoint.TemplateROI = generateTemplateParam.Roi;
                    stageMapTemplatePoint.TemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(stageMapTemplatePoint.TemplateFilePath);
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

    public async Task GenerateStageMapAsync(StageMapDie[] selectStageMapDies, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            if (IsDark) ChuckStageMap.DFStageMap = new StageMap();
            else ChuckStageMap.BFStageMap = new StageMap();

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
        }, cancellationToken).ConfigureAwait(false);
    }
}