using System.IO;
using System.Threading.Channels;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Algorithm.Extensions;
using Core.Algorithm.Models.TemplateMatch;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.CIB.XPixelSize;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.PixelSize;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using HalconDotNet;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WaferMap.WPF.Primitives;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;

namespace CugaCalibration.ViewModels.Chuck;

[IOCAppService(ServiceType = typeof(ChuckStageMapViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckStageMapViewModel : CalibrationViewModelBase<ChuckStageMapCache>
{
    #region 属性

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "BF Param" },
        new() { StepName = "BF Alignment" },
        new() { StepName = "Generate BF StageMap" },
        new() { StepName = "Scan BF StageMap" },
        new() { StepName = "DF Param" },
        new() { StepName = "DF Alignment" },
        new() { StepName = "Generate DF StageMap" },
        new() { StepName = "Scan DF StageMap" },
        new() { StepName = "Repeat Scan DF StageMap" },
        new() { StepName = "Merge" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial ChuckStageMapDTO CalibratingItem { get; set; } = new();

    #endregion Calibrate

    [ObservableProperty]
    public partial ChuckStageMapDTO Review { get; set; } = new();

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial ChuckStageMapCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial ChuckStageMapDTO Calibration { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopePixelSizeDTO[] MicroscopePixelSizes { get; set; } = [];

    [ObservableProperty]
    public partial CIBXPixelSizeDTO[] CIBXPixelSizes { get; set; } = [];

    [ObservableProperty]
    public partial CIBYPixelSizeDTO[] CIBYPixelSizes { get; set; } = [];

    [ObservableProperty]
    public partial CreateDarkImageTemplateWindowViewModel CreateDarkImageTemplateWindowViewModel { get; set; } = HostApplication.GetRequiredService<CreateDarkImageTemplateWindowViewModel>();

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = HostApplication.GetRequiredService<AlignmentUserControlViewModel>();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
        MicroscopePixelSizes = ApplicationCookieService.GetCalibrations<MicroscopePixelSizeDTO>(cancellationToken);
        CIBXPixelSizes = ApplicationCookieService.GetCalibrations<CIBXPixelSizeDTO>(cancellationToken);
        CIBYPixelSizes = ApplicationCookieService.GetCalibrations<CIBYPixelSizeDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<ChuckStageMapCache>(cancellationToken);
        Calibration = ApplicationCookieService.GetCalibration<ChuckStageMapDTO>(cancellationToken);

        UpdateEntryStatus(Calibration, cancellationToken);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Review = Calibration;

        Review.BFStageMap.Refresh();
        Review.DFStageMap.Refresh();
        foreach (var stageMap in Review.DFRepeatStageMaps) stageMap.Refresh();
        Review.ExpandStageMap.Refresh();
        Review.BFVerifyStageMap.Refresh();
        Review.DFVerifyStageMap.Refresh();

        return Review.IsCalibrated;
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Guard.IsFalse(AddBFStageMapTemplateCommand.IsRunning
                      || AddDFStageMapTemplateCommand.IsRunning
                      || GotoBFStageMapDocumentSelectedItemPositionCommand.IsRunning
                      || GotoDFStageMapDocumentSelectedItemPositionCommand.IsRunning);

        switch (CalibrationStepIndex)
        {
            case 0:
                return true;

            case 1:
                return true;

            case 2:
                return true;

            case 3:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.ChuckStageMapCacheBFItem.StageMapTemplates.FirstOrDefault()?.FindBFMachinePosition ?? MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), CalChipSiteModelEnum.ChuckModel);

                return true;

            case 4:
                return true;

            case 5:
                return true;

            case 6:
                return true;

            case 7:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.ChuckStageMapCacheDFItem.StageMapTemplates.FirstOrDefault()?.FindBFMachinePosition ?? MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), CalChipSiteModelEnum.ChuckModel);

                return true;

            case 8:
                return true;

            case 9:
                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Guard.IsFalse(AddBFStageMapTemplateCommand.IsRunning
                      || AddDFStageMapTemplateCommand.IsRunning
                      || GotoBFStageMapDocumentSelectedItemPositionCommand.IsRunning
                      || GotoDFStageMapDocumentSelectedItemPositionCommand.IsRunning);

        switch (CalibrationStepIndex)
        {
            case 0:
                CalibratingItem = new ChuckStageMapDTO();

                return true;

            case 1:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.ChuckStageMapCacheBFItem.StageMapTemplates.FirstOrDefault()?.FindBFMachinePosition ?? MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), CalChipSiteModelEnum.ChuckModel);

                return true;

            case 2:
                return true;

            case 3:
                return true;

            case 4:
                return true;

            case 5:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.ChuckStageMapCacheDFItem.StageMapTemplates.FirstOrDefault()?.FindBFMachinePosition ?? MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), CalChipSiteModelEnum.ChuckModel);

                return true;

            case 6:
                return true;

            case 7:
                return true;

            case 8:
                return true;

            case 9:
                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Guard.IsTrue(MicroscopeCalChip.IsOk, "Microscope Cal Chip is Empty or not verify.");

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.MicroscopeLensInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
            AlignmentUserControlViewModel.IsDarkFieldAlignment = false;

            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            CalibratingItem.BFAlignmentResult = AlignmentUserControlViewModel.AlignmentResult;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.MicroscopeLensInformation,
                BFAlignmentResult = new HtmlQuote(CalibratingItem.BFAlignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), CalChipSiteModelEnum.ChuckModel);

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Guard.IsEqualTo(Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Length, 1);
            Guard.IsGreaterThan(Cache.ChuckStageMapCacheBFItem.WaferRadius, 0d);
            Guard.IsGreaterThan(Cache.ChuckStageMapCacheBFItem.DiePitchWidth, 0d);
            Guard.IsGreaterThan(Cache.ChuckStageMapCacheBFItem.DiePitchHeight, 0d);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.MicroscopeLensInformation,
                BFParam = new HtmlQuote(new
                {
                    TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheBFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                    Cache.ChuckStageMapCacheBFItem.WaferRadius,
                    Cache.ChuckStageMapCacheBFItem.DiePitchWidth,
                    Cache.ChuckStageMapCacheBFItem.DiePitchHeight,
                    Table = new HtmlTable(
                    [
                        .. Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Index().Select(t => new
                        {
                            t.Index,
                            t.Item.FindBFMachinePosition,
                            t.Item.TemplateROI,
                            t.Item.TemplateFilePath,
                            t.Item.TemplateImageFilePath,
                            Image = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    ])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            var bfPositions = new Point[Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Length];
            foreach (var (index, stageMapTemplate) in Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Index())
            {
                var bfPosition = StageViewModel.MachineToBrightFieldPosition(stageMapTemplate.FindBFMachinePosition);

                bfPositions[index] = bfPosition;
            }

            CalibratingItem.BFStageMapDocument.RunDesign(() =>
            {
                CalibratingItem.BFStageMapDocument.Reset();

                var bfCircle = new Circle(StageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), Cache.ChuckStageMapCacheBFItem.WaferRadius);
                var bfMachineCircle = new Circle(StageViewModel.BrightFieldToMachinePosition(bfCircle.Center), bfCircle.Radius);

                CalibratingItem.BFStageMapDocument.WaferModel.Add(new StageMapWafer { Circle = bfMachineCircle });

                var stageMapReticleBuilder = new StageMapDieBuilder
                {
                    DiePitchSize = new Size(Cache.ChuckStageMapCacheBFItem.DiePitchWidth, Cache.ChuckStageMapCacheBFItem.DiePitchHeight),
                    OriginalDiePoint = bfPositions[0]
                };

                var stageMapDies = stageMapReticleBuilder.BuildDie(bfCircle);
                Vector[] markers =
                [
                    .. bfPositions.Select(tt => tt - bfPositions[0])
                ];

                CalibratingItem.BFStageMapDocument.DieModel.AddRange(stageMapDies.Select(t =>
                {
                    Point[] points = [.. markers.Select(tt => t.Rect.Point + tt)];

                    var resultExtents = new Extents();
                    resultExtents.Add(StageViewModel.BrightFieldToMachinePosition(t.Rect.XMinYMin));
                    resultExtents.Add(StageViewModel.BrightFieldToMachinePosition(t.Rect.XMaxYMax));

                    return new StageMapDie
                    {
                        Index = t.Index,
                        Row = t.Row,
                        Col = t.Col,
                        Rect = (Rect)resultExtents,
                        IsInWafer = points.All(bfCircle.Contains),
                        Markers = [.. points.Select(StageViewModel.BrightFieldToMachinePosition)]
                    };
                }));
            }, true);

            Guard.IsTrue(CalibratingItem.BFStageMapDocument.DieModel.Count > 0);
            Guard.IsTrue(CalibratingItem.BFStageMapDocument.DieModel.All(t => t.Markers.Length == Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Length));

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step3Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () =>
    {
        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            Cache.MicroscopeLensInformation,
            BFParam = new HtmlQuote(new
            {
                TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheBFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                Cache.ChuckStageMapCacheBFItem.WaferRadius,
                Cache.ChuckStageMapCacheBFItem.DiePitchWidth,
                Cache.ChuckStageMapCacheBFItem.DiePitchHeight,
                Table = new HtmlTable(
                [
                    .. Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Index().Select(t => new
                    {
                        t.Index,
                        t.Item.FindBFMachinePosition,
                        t.Item.TemplateROI,
                        t.Item.TemplateFilePath,
                        t.Item.TemplateImageFilePath,
                        Image = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                ])
            })
        }), HtmlLogUniqueId.LoggingHtml());

        StageViewModel.SetEnableStageMap(false);

        CalibratingItem.BFStageMap = new StageMap();

        StageMapDie[] stageMapDies;
        using (CalibratingItem.BFStageMapDocument.View.Sync.EnterScope()) stageMapDies = [.. CalibratingItem.BFStageMapDocument.DieModel];

        foreach (var stageMapDie in stageMapDies) stageMapDie.IsInStageMapScan = true;

        var (xDirection, yDirection) = StageViewModel.GetMachineDirection();

        var minY = stageMapDies.Min(t => t.Index.Y);
        var maxY = stageMapDies.Max(t => t.Index.Y);
        var minX = stageMapDies.Min(t => t.Index.X);
        var maxX = stageMapDies.Max(t => t.Index.X);

        var yLength = maxY - minY + 1;
        var xLength = maxX - minX + 1;

        Guard.IsGreaterThan(yLength, 2);
        Guard.IsGreaterThan(xLength, 2);

        CalibratingItem.BFStageMap.IdealMatrix = [.. Generate.LinearRangeInt32(0, yLength - 1).Select(_ => new Point[xLength * Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Length])];
        CalibratingItem.BFStageMap.ErrorMatrix = [.. Generate.LinearRangeInt32(0, yLength - 1).Select(_ => new Vector[xLength * Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Length])];
        CalibratingItem.BFStageMap.IsInWaferMatrix = [.. Generate.LinearRangeInt32(0, yLength - 1).Select(_ => new bool[xLength * Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Length])];
        CalibratingItem.BFStageMap.IsMatchMatrix = [.. Generate.LinearRangeInt32(0, yLength - 1).Select(_ => new bool[xLength * Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Length])];

        for (var y = 0; y < yLength; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var x = 0; x < xLength; x++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var stageMapIndexX = xDirection > 0 ? minX + x : maxX - x;
                var stageMapIndexY = yDirection > 0 ? minY + y : maxY - y;

                var stageMapDie = stageMapDies.Single(t => t.Index == new WaferMapDieIndex(stageMapIndexX, stageMapIndexY));

                for (var markerIndex = 0; markerIndex < Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Length; markerIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var dfMachinePoint = stageMapDie.Markers[xDirection > 0 ? markerIndex : ^(markerIndex + 1)];
                    dfMachinePoint = dfMachinePoint.WithY(stageMapDie.Markers[0].Y);

                    var matrixColumn = x * Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Length + markerIndex;

                    CalibratingItem.BFStageMap.IdealMatrix[y][matrixColumn] = dfMachinePoint;
                    CalibratingItem.BFStageMap.IsInWaferMatrix[y][matrixColumn] = stageMapDie.IsInWafer;
                }
            }
        }

        CalibratingItem.BFStageMap.Refresh();

        await ScanBFStageMapAsync(
            CalibratingItem.BFStageMap,
            HtmlLogUniqueId,
            cancellationToken).ConfigureAwait(false);

        Logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

        CalibratingItem.BFStageMap.CalculateErrorMatrix();
        CalibratingItem.BFStageMap.Refresh();

        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
            .. CalibratingItem.BFStageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
            .. CalibratingItem.BFStageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
        ]), HtmlLogUniqueId.LoggingHtml());

        return true;
    });

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step4Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation,
                DFParam = new HtmlQuote(new
                {
                    Cache.ChuckStageMapCacheDFItem.ProductivityInformation,
                    Cache.ChuckStageMapCacheDFItem.LaserLightInformation,
                    Cache.ChuckStageMapCacheDFItem.CIBInformation,
                    Cache.ChuckStageMapCacheDFItem.OpticsConfiguration,
                    Cache.ChuckStageMapCacheDFItem.CIBConfiguration
                })
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.ProductivityInformations.Contains(Cache.ChuckStageMapCacheDFItem.ProductivityInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.ChuckStageMapCacheDFItem.LaserLightInformation)
                   && ApplicationCookie.CIBInformations.Contains(Cache.ChuckStageMapCacheDFItem.CIBInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step5Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
            AlignmentUserControlViewModel.ProductivityInformation = Cache.ChuckStageMapCacheDFItem.ProductivityInformation;

            if (DialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment?",
                    out var dialogResult,
                    DialogButtonsEnum.YesNo,
                    DialogIconEnum.Question) == true && dialogResult == DialogResultEnum.Yes) CalibratingItem.IsDarkFieldAlignment = AlignmentUserControlViewModel.IsDarkFieldAlignment = true;
            else CalibratingItem.IsDarkFieldAlignment = AlignmentUserControlViewModel.IsDarkFieldAlignment = false;

            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            CalibratingItem.DFAlignmentResult = AlignmentUserControlViewModel.AlignmentResult;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.MicroscopeLensInformation,
                BFParam = new HtmlQuote(new
                {
                    TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheBFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                    Cache.ChuckStageMapCacheBFItem.WaferRadius,
                    Cache.ChuckStageMapCacheBFItem.DiePitchWidth,
                    Cache.ChuckStageMapCacheBFItem.DiePitchHeight,
                    Table = new HtmlTable(
                    [
                        .. Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Index().Select(t => new
                        {
                            t.Index,
                            t.Item.FindBFMachinePosition,
                            t.Item.TemplateROI,
                            t.Item.TemplateFilePath,
                            t.Item.TemplateImageFilePath,
                            Image = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    ])
                }),
                DFParam = new HtmlQuote(new
                {
                    Cache.ChuckStageMapCacheDFItem.ProductivityInformation,
                    Cache.ChuckStageMapCacheDFItem.LaserLightInformation,
                    Cache.ChuckStageMapCacheDFItem.CIBInformation,
                    Cache.ChuckStageMapCacheDFItem.OpticsConfiguration,
                    Cache.ChuckStageMapCacheDFItem.CIBConfiguration
                }),
                DFAlignmentResult = new HtmlQuote(CalibratingItem.DFAlignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), CalChipSiteModelEnum.ChuckModel);

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step6Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Guard.IsNotEmpty(Cache.ChuckStageMapCacheDFItem.StageMapTemplates);
            Guard.IsGreaterThan(Cache.ChuckStageMapCacheDFItem.WaferRadius, 0d);
            Guard.IsGreaterThan(Cache.ChuckStageMapCacheDFItem.DiePitchWidth, 0d);
            Guard.IsGreaterThan(Cache.ChuckStageMapCacheDFItem.DiePitchHeight, 0d);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.MicroscopeLensInformation,
                BFParam = new HtmlQuote(new
                {
                    TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheBFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                    Cache.ChuckStageMapCacheBFItem.WaferRadius,
                    Cache.ChuckStageMapCacheBFItem.DiePitchWidth,
                    Cache.ChuckStageMapCacheBFItem.DiePitchHeight,
                    Table = new HtmlTable(
                    [
                        .. Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Index().Select(t => new
                        {
                            t.Index,
                            t.Item.FindBFMachinePosition,
                            t.Item.TemplateROI,
                            t.Item.TemplateFilePath,
                            t.Item.TemplateImageFilePath,
                            Image = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    ])
                }),
                DFParam = new HtmlQuote(new
                {
                    Cache.ChuckStageMapCacheDFItem.ProductivityInformation,
                    Cache.ChuckStageMapCacheDFItem.LaserLightInformation,
                    Cache.ChuckStageMapCacheDFItem.CIBInformation,
                    Cache.ChuckStageMapCacheDFItem.OpticsConfiguration,
                    Cache.ChuckStageMapCacheDFItem.CIBConfiguration,
                    TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                    Cache.ChuckStageMapCacheDFItem.ImageWidth,
                    Cache.ChuckStageMapCacheDFItem.WaferRadius,
                    Cache.ChuckStageMapCacheDFItem.DiePitchWidth,
                    Cache.ChuckStageMapCacheDFItem.DiePitchHeight,
                    Table = new HtmlTable(
                    [
                        .. Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Index().Select(t => new
                        {
                            t.Index,
                            t.Item.FindBFMachinePosition,
                            t.Item.TemplateROI,
                            t.Item.TemplateFilePath,
                            t.Item.TemplateImageFilePath,
                            Image = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    ])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            var dfPositions = new Point[Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Length];

            foreach (var (index, stageMapTemplate) in Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Index())
            {
                var bfPosition = StageViewModel.MachineToBrightFieldPosition(stageMapTemplate.FindBFMachinePosition);
                var dfPosition = CIBViewModel.GetCIBInformationPosition(
                    StageCoordinateSystemEnum.Dark,
                    Cache.ChuckStageMapCacheDFItem.ProductivityInformation,
                    Cache.ChuckStageMapCacheDFItem.CIBInformation,
                    bfPosition,
                    Cache.MicroscopeLensInformation);

                dfPositions[index] = dfPosition;
            }

            CalibratingItem.DFStageMapDocument.RunDesign(() =>
            {
                CalibratingItem.DFStageMapDocument.Reset();

                var dfCircle = new Circle(StageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), Cache.ChuckStageMapCacheDFItem.WaferRadius);
                var dfMachineCircle = new Circle(StageViewModel.DarkFieldToMachinePosition(dfCircle.Center), dfCircle.Radius);

                CalibratingItem.DFStageMapDocument.WaferModel.Add(new StageMapWafer { Circle = dfMachineCircle });

                var stageMapReticleBuilder = new StageMapDieBuilder
                {
                    DiePitchSize = new Size(Cache.ChuckStageMapCacheDFItem.DiePitchWidth, Cache.ChuckStageMapCacheDFItem.DiePitchHeight),
                    OriginalDiePoint = dfPositions[0]
                };

                var stageMapDies = stageMapReticleBuilder.BuildDie(dfCircle);
                Vector[] markers =
                [
                    .. dfPositions.Select(tt => tt - dfPositions[0])
                ];

                CalibratingItem.DFStageMapDocument.DieModel.AddRange(stageMapDies.Select(t =>
                {
                    Point[] points = [.. markers.Select(tt => t.Rect.Point + tt)];

                    var resultExtents = new Extents();
                    resultExtents.Add(StageViewModel.DarkFieldToMachinePosition(t.Rect.XMinYMin));
                    resultExtents.Add(StageViewModel.DarkFieldToMachinePosition(t.Rect.XMaxYMax));

                    return new StageMapDie
                    {
                        Index = t.Index,
                        Row = t.Row,
                        Col = t.Col,
                        Rect = (Rect)resultExtents,
                        IsInWafer = points.All(dfCircle.Contains),
                        Markers = [.. points.Select(StageViewModel.DarkFieldToMachinePosition)]
                    };
                }));
            }, true);

            Guard.IsTrue(CalibratingItem.DFStageMapDocument.DieModel.Count > 0);
            Guard.IsTrue(CalibratingItem.DFStageMapDocument.DieModel.All(t => t.Markers.Length == Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Length));

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step7Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () =>
    {
        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            Cache.MicroscopeLensInformation,
            BFParam = new HtmlQuote(new
            {
                TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheBFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                Cache.ChuckStageMapCacheBFItem.WaferRadius,
                Cache.ChuckStageMapCacheBFItem.DiePitchWidth,
                Cache.ChuckStageMapCacheBFItem.DiePitchHeight,
                Table = new HtmlTable(
                [
                    .. Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Index().Select(t => new
                    {
                        t.Index,
                        t.Item.FindBFMachinePosition,
                        t.Item.TemplateROI,
                        t.Item.TemplateFilePath,
                        t.Item.TemplateImageFilePath,
                        Image = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                ])
            }),
            DFParam = new HtmlQuote(new
            {
                Cache.ChuckStageMapCacheDFItem.ProductivityInformation,
                Cache.ChuckStageMapCacheDFItem.LaserLightInformation,
                Cache.ChuckStageMapCacheDFItem.CIBInformation,
                Cache.ChuckStageMapCacheDFItem.OpticsConfiguration,
                Cache.ChuckStageMapCacheDFItem.CIBConfiguration,
                TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                Cache.ChuckStageMapCacheDFItem.ImageWidth,
                Cache.ChuckStageMapCacheDFItem.WaferRadius,
                Cache.ChuckStageMapCacheDFItem.DiePitchWidth,
                Cache.ChuckStageMapCacheDFItem.DiePitchHeight,
                Table = new HtmlTable(
                [
                    .. Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Index().Select(t => new
                    {
                        t.Index,
                        t.Item.FindBFMachinePosition,
                        t.Item.TemplateROI,
                        t.Item.TemplateFilePath,
                        t.Item.TemplateImageFilePath,
                        Image = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                ])
            })
        }), HtmlLogUniqueId.LoggingHtml());

        StageViewModel.SetEnableStageMap(false);

        CalibratingItem.DFStageMap = new StageMap();

        StageMapDie[] stageMapDies;
        using (CalibratingItem.DFStageMapDocument.View.Sync.EnterScope()) stageMapDies = [.. CalibratingItem.DFStageMapDocument.DieModel];

        foreach (var stageMapDie in stageMapDies) stageMapDie.IsInStageMapScan = true;

        var (xDirection, yDirection) = StageViewModel.GetMachineDirection();

        var minY = stageMapDies.Min(t => t.Index.Y);
        var maxY = stageMapDies.Max(t => t.Index.Y);
        var minX = stageMapDies.Min(t => t.Index.X);
        var maxX = stageMapDies.Max(t => t.Index.X);

        var yLength = maxY - minY + 1;
        var xLength = maxX - minX + 1;

        Guard.IsGreaterThan(yLength, 2);
        Guard.IsGreaterThan(xLength, 2);

        CalibratingItem.DFStageMap.IdealMatrix = [.. Generate.LinearRangeInt32(0, yLength - 1).Select(_ => new Point[xLength * Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Length])];
        CalibratingItem.DFStageMap.ErrorMatrix = [.. Generate.LinearRangeInt32(0, yLength - 1).Select(_ => new Vector[xLength * Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Length])];
        CalibratingItem.DFStageMap.IsInWaferMatrix = [.. Generate.LinearRangeInt32(0, yLength - 1).Select(_ => new bool[xLength * Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Length])];
        CalibratingItem.DFStageMap.IsMatchMatrix = [.. Generate.LinearRangeInt32(0, yLength - 1).Select(_ => new bool[xLength * Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Length])];

        for (var y = 0; y < yLength; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var x = 0; x < xLength; x++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var stageMapIndexX = xDirection > 0 ? minX + x : maxX - x;
                var stageMapIndexY = yDirection > 0 ? minY + y : maxY - y;

                var stageMapDie = stageMapDies.Single(t => t.Index == new WaferMapDieIndex(stageMapIndexX, stageMapIndexY));

                for (var markerIndex = 0; markerIndex < Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Length; markerIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var dfMachinePoint = stageMapDie.Markers[xDirection > 0 ? markerIndex : ^(markerIndex + 1)];
                    dfMachinePoint = dfMachinePoint.WithY(stageMapDie.Markers[0].Y);

                    var matrixColumn = x * Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Length + markerIndex;

                    CalibratingItem.DFStageMap.IdealMatrix[y][matrixColumn] = dfMachinePoint;
                    CalibratingItem.DFStageMap.IsInWaferMatrix[y][matrixColumn] = stageMapDie.IsInWafer;
                }
            }
        }

        CalibratingItem.DFStageMap.Refresh();

        await ScanDFStageMapAsync(
            CalibratingItem.DFStageMap,
            HtmlLogUniqueId,
            cancellationToken).ConfigureAwait(false);

        Logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

        CalibratingItem.DFStageMap.CalculateErrorMatrix();
        CalibratingItem.DFStageMap.Refresh();

        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
            .. CalibratingItem.DFStageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
            .. CalibratingItem.DFStageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
        ]), HtmlLogUniqueId.LoggingHtml());

        return true;
    });

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step8Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () =>
    {
        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            Cache.MicroscopeLensInformation,
            BFParam = new HtmlQuote(new
            {
                TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheBFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                Cache.ChuckStageMapCacheBFItem.WaferRadius,
                Cache.ChuckStageMapCacheBFItem.DiePitchWidth,
                Cache.ChuckStageMapCacheBFItem.DiePitchHeight,
                Table = new HtmlTable(
                [
                    .. Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Index().Select(t => new
                    {
                        t.Index,
                        t.Item.FindBFMachinePosition,
                        t.Item.TemplateROI,
                        t.Item.TemplateFilePath,
                        t.Item.TemplateImageFilePath,
                        Image = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                ])
            }),
            DFParam = new HtmlQuote(new
            {
                Cache.ChuckStageMapCacheDFItem.ProductivityInformation,
                Cache.ChuckStageMapCacheDFItem.LaserLightInformation,
                Cache.ChuckStageMapCacheDFItem.CIBInformation,
                Cache.ChuckStageMapCacheDFItem.OpticsConfiguration,
                Cache.ChuckStageMapCacheDFItem.CIBConfiguration,
                TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                Cache.ChuckStageMapCacheDFItem.ImageWidth,
                Cache.ChuckStageMapCacheDFItem.WaferRadius,
                Cache.ChuckStageMapCacheDFItem.DiePitchWidth,
                Cache.ChuckStageMapCacheDFItem.DiePitchHeight,
                Table = new HtmlTable(
                [
                    .. Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Index().Select(t => new
                    {
                        t.Index,
                        t.Item.FindBFMachinePosition,
                        t.Item.TemplateROI,
                        t.Item.TemplateFilePath,
                        t.Item.TemplateImageFilePath,
                        Image = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                ]),
                Cache.ChuckStageMapCacheDFItem.StageMapRepeatTimes
            })
        }), HtmlLogUniqueId.LoggingHtml());

        StageViewModel.SetEnableStageMap(false);

        CalibratingItem.DFRepeatStageMaps = [];
        var historyStageMapList = new List<StageMap>();

        bool isSuccess;
        var times = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentHtmlLogUniqueId = Guid.NewGuid();
            var fileName = $"Details_{CalibrationSteps[^2].StepName.Replace(" ", string.Empty)}_{times + 1}";

            if (times == 0)
                DownloadStageMap(
                    CalibratingItem.DFStageMap,
                    new Circle(StageViewModel.DarkFieldToMachinePosition(Point.Origin), Cache.ChuckStageMapCacheDFItem.WaferRadius),
                    currentHtmlLogUniqueId);

            Logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header3, new HtmlComment($"See Above! Same Directory File Name: {fileName}({currentHtmlLogUniqueId:N})"), HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation($"{currentHtmlLogUniqueId:N}", HtmlHeaderLevelEnum.Header1, new HtmlComment(Name), currentHtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header3, currentHtmlLogUniqueId.LoggingHtml());

            try
            {
                var scanStageMap = CalibratingItem.DFStageMap.Clone();
                scanStageMap.Reset();

                CalibratingItem.DFRepeatStageMaps = [.. CalibratingItem.DFRepeatStageMaps, scanStageMap];

                await ScanDFStageMapAsync(
                    scanStageMap,
                    currentHtmlLogUniqueId,
                    cancellationToken,
                    isInterpolateErrors: true,
                    interpolateStageMap: CalibratingItem.DFStageMap).ConfigureAwait(false);

                historyStageMapList.Add(scanStageMap.Clone());

                Logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, currentHtmlLogUniqueId.LoggingHtml());

                isSuccess = scanStageMap.CanRepeatAndCalculateErrorMatrix(historyStageMapList, Cache.ChuckStageMapCacheDFItem.StageMapRepeatTimes);

                scanStageMap.Refresh();

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
                    .. scanStageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                    .. scanStageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
                ]), currentHtmlLogUniqueId.LoggingHtml());

                if (isSuccess || ++times > Cache.ChuckStageMapCacheDFItem.StageMapRepeatTimes - 1)
                {
                    CalibratingItem.DFStageMap.MergeErrorMatrix(scanStageMap, Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Length);

                    CalibratingItem.DFStageMap.Refresh();

                    if (isSuccess)
                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlContainer([
                            .. CalibratingItem.DFStageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                            .. CalibratingItem.DFStageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
                        ]), HtmlLogUniqueId.LoggingHtml());
                    else
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlContainer([
                            new HtmlComment("More than the number of times."),
                            .. CalibratingItem.DFStageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                            .. CalibratingItem.DFStageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
                        ]), HtmlLogUniqueId.LoggingHtml());

                    break;
                }
            }
            finally
            {
                Logger.LogHtmlInformation(currentHtmlLogUniqueId.LoggedEndHtml(fileName));
            }
        }

        return isSuccess;
    });

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step9Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(() =>
    {
        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            Cache.MicroscopeLensInformation,
            BFParam = new HtmlQuote(new
            {
                TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheBFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                Cache.ChuckStageMapCacheBFItem.WaferRadius,
                Cache.ChuckStageMapCacheBFItem.DiePitchWidth,
                Cache.ChuckStageMapCacheBFItem.DiePitchHeight,
                Table = new HtmlTable(
                [
                    .. Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Index().Select(t => new
                    {
                        t.Index,
                        t.Item.FindBFMachinePosition,
                        t.Item.TemplateROI,
                        t.Item.TemplateFilePath,
                        t.Item.TemplateImageFilePath,
                        Image = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                ])
            }),
            DFParam = new HtmlQuote(new
            {
                Cache.ChuckStageMapCacheDFItem.ProductivityInformation,
                Cache.ChuckStageMapCacheDFItem.LaserLightInformation,
                Cache.ChuckStageMapCacheDFItem.CIBInformation,
                Cache.ChuckStageMapCacheDFItem.OpticsConfiguration,
                Cache.ChuckStageMapCacheDFItem.CIBConfiguration,
                TemplateMatchSetting = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.TemplateMatchSetting.ToHtmlAnonymous(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum)),
                Cache.ChuckStageMapCacheDFItem.ImageWidth,
                Cache.ChuckStageMapCacheDFItem.WaferRadius,
                Cache.ChuckStageMapCacheDFItem.DiePitchWidth,
                Cache.ChuckStageMapCacheDFItem.DiePitchHeight,
                Table = new HtmlTable(
                [
                    .. Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Index().Select(t => new
                    {
                        t.Index,
                        t.Item.FindBFMachinePosition,
                        t.Item.TemplateROI,
                        t.Item.TemplateFilePath,
                        t.Item.TemplateImageFilePath,
                        Image = new HtmlImage(t.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                ]),
                Cache.ChuckStageMapCacheDFItem.StageMapRepeatTimes
            })
        }), HtmlLogUniqueId.LoggingHtml());

        StageViewModel.SetEnableStageMap(false);

        CalibratingItem.ExpandStageMap = CalibratingItem.DFStageMap.Expand(
            CalibratingItem.BFStageMap,
            new Circle(StageViewModel.BrightFieldToMachinePosition(Point.Origin), Cache.ChuckStageMapCacheBFItem.WaferRadius),
            new Circle(StageViewModel.DarkFieldToMachinePosition(Point.Origin), Cache.ChuckStageMapCacheDFItem.WaferRadius));
        cancellationToken.ThrowIfCancellationRequested();

        CalibratingItem.BFVerifyStageMap = new StageMap();
        CalibratingItem.BFVerifyMaxError = Vector.Zero;
        CalibratingItem.DFVerifyStageMap = new StageMap();
        CalibratingItem.DFVerifyMaxError = Vector.Zero;
        CalibratingItem.IsCalibrated = true;
        CalibratingItem.IsVerified = false;
        Guard.IsTrue(Save(CalibratingItem, cancellationToken));

        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlContainer([
            .. CalibratingItem.ExpandStageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
            .. CalibratingItem.ExpandStageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
        ]), HtmlLogUniqueId.LoggingHtml());

        return true;
    });

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.IsTrue(Review.IsCalibrated);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation, VerifyThreshold = Cache.Threshold
            }), HtmlLogUniqueId.LoggingHtml());

            var isVerifyThresholdValid = Cache.Threshold > 0d && double.IsInfinity(Cache.Threshold) == false;
            if (isVerifyThresholdValid == false)
            {
                DialogWindowProvider.ShowDialog("Set positive finite Verify Threshold before verification.", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return false;
            }

            Guard.IsNotEmpty(Cache.ChuckStageMapCacheBFItem.StageMapTemplates);
            Guard.IsNotEmpty(Cache.ChuckStageMapCacheDFItem.StageMapTemplates);
            Guard.IsNotEmpty(Review.BFStageMap.IdealMatrix);
            Guard.IsNotEmpty(Review.DFStageMap.IdealMatrix);
            Guard.IsNotEmpty(Review.ExpandStageMap.IdealMatrix);

            Review.BFVerifyStageMap = Review.BFStageMap.Clone();
            Review.BFVerifyStageMap.Reset();
            Review.BFVerifyMaxError = Vector.Zero;
            Review.DFVerifyStageMap = Review.DFStageMap.Clone();
            Review.DFVerifyStageMap.Reset();
            Review.DFVerifyMaxError = Vector.Zero;
            Review.IsVerified = false;
            Guard.IsTrue(Save(Review, cancellationToken));

            // 外扩结果已是合并后的标准网格, 直接下载以保留明场圆外的暗场误差.
            DownloadStageMap(Review.ExpandStageMap, null, HtmlLogUniqueId);

            await ScanBFStageMapAsync(Review.BFVerifyStageMap, HtmlLogUniqueId, cancellationToken).ConfigureAwait(false);
            var brightFieldVerifyResult = VerifyStageMap(Review.BFVerifyStageMap, Cache.Threshold);
            Review.BFVerifyMaxError = brightFieldVerifyResult.MaximumError;

            await ScanDFStageMapAsync(
                Review.DFVerifyStageMap,
                HtmlLogUniqueId,
                cancellationToken,
                isInterpolateErrors: true,
                interpolateStageMap: Review.ExpandStageMap).ConfigureAwait(false);
            var darkFieldVerifyResult = VerifyStageMap(Review.DFVerifyStageMap, Cache.Threshold);
            Review.DFVerifyMaxError = darkFieldVerifyResult.MaximumError;

            cancellationToken.ThrowIfCancellationRequested();
            Review.IsVerified = brightFieldVerifyResult.IsVerified && darkFieldVerifyResult.IsVerified;
            Guard.IsTrue(Save(Review, cancellationToken));

            Logger.LogHtmlInformation(Review.IsVerified ? "Verify OK" : "Verify Failed", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                VerifyThreshold = Cache.Threshold,
                BFVerifyMaximumError = Review.BFVerifyMaxError,
                DFVerifyMaximumError = Review.DFVerifyMaxError,
                Review.IsVerified
            }), HtmlLogUniqueId.LoggingHtml());

            if (Review.IsVerified) DialogWindowProvider.ShowDialog("Verify : OK");
            else DialogWindowProvider.ShowDialog("Verify : Failed. Check BF/DF errors and unmatched points.", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return Review.IsVerified;
        }).ConfigureAwait(false);
    }

    private static (bool IsVerified, Vector MaximumError) VerifyStageMap(StageMap stageMap, double threshold)
    {
        var (yLength, xLength) = stageMap.IdealMatrix.GetYXLength();
        var pointCount = 0;
        var maximumErrorX = 0d;
        var maximumErrorY = 0d;
        var isVerified = true;
        for (var y = 0; y < yLength; y++)
        {
            for (var x = 0; x < xLength; x++)
            {
                if (stageMap.IsInWaferMatrix[y][x] == false) continue;

                pointCount++;
                if (stageMap.IsMatchMatrix[y][x] == false)
                {
                    isVerified = false;
                    continue;
                }

                var error = stageMap.ErrorMatrix[y][x];
                var errorX = Math.Abs(error.X);
                var errorY = Math.Abs(error.Y);
                maximumErrorX = Math.Max(maximumErrorX, errorX);
                maximumErrorY = Math.Max(maximumErrorY, errorY);
                if ((errorX < threshold && errorY < threshold) == false) isVerified = false;
            }
        }

        return (isVerified && pointCount > 0, new Vector(maximumErrorX, maximumErrorY));
    }

    private sealed record ChuckStageMapBFScanRow(int Y, int[] IsInWaferColumnIndexes, BitmapImage[] Images);

    private async Task ScanBFStageMapAsync(
        StageMap stageMap,
        Guid htmlLogUniqueId,
        CancellationToken cancellationToken)
    {
        await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);

        var (xDirection, yDirection) = StageViewModel.GetMachineDirection();
        var size = MicroscopePixelSizes.SingleOrDefault(t => t.MicroscopeLensInformation == Cache.MicroscopeLensInformation);
        Guard.IsTrue(size?.IsOk == true, "Microscope Pixel Size is Empty or not verify.");

        Logger.LogHtmlInformation("Pixel Size", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
        {
            xDirection,
            yDirection,
            size.Result.PixelSize
        }), htmlLogUniqueId.LoggingHtml());

        HTuple[] templateIds = [];

        try
        {
            templateIds =
            [
                .. Cache.ChuckStageMapCacheBFItem.StageMapTemplates
                    .Select(t =>
                    {
                        CalibrationAlgorithmService.TryReadTemplate(Cache.ChuckStageMapCacheBFItem.TemplateMatchSetting.ToHalconTemplateType(), t.TemplateFilePath, out var templateId);

                        return templateId;
                    })
            ];

            var templateMatchScoreThreshold = Cache.ChuckStageMapCacheBFItem.TemplateMatchSetting.ToTemplateMatchScoreThreshold(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum, CalibrationSetting);

            Point[][] stageMapIdealMatrix =
            [
                .. stageMap.IdealMatrix.Select<Point[], Point[]>(t =>
                [
                    .. t.Select(tt => tt)
                ])
            ];
            var (yLength, _) = stageMapIdealMatrix.GetYXLength();

            Logger.LogHtmlInformation("rows", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var linkedToken = linkedCancellationTokenSource.Token;

            // 容量为 2: Reader 匹配第 y 行时, Writer 可继续采集后续行, 取图与模板匹配流水线并行, 扫描无需等待匹配完成
            var channel = Channel.CreateBounded<ChuckStageMapBFScanRow>(new BoundedChannelOptions(2) { SingleReader = true, SingleWriter = true, AllowSynchronousContinuations = true });

            // Writer: 移动平台取图, 一行采完塞进 channel
            var writerTask = Task.Run(async () =>
            {
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

                     
                    }
                }
                catch (Exception ex)
                {
                    channel.Writer.TryComplete(ex);
                    throw;
                }
                finally
                {
                    channel.Writer.TryComplete();
                }
            }, linkedToken);

            // Reader: 从 channel 读一行, 并行模板匹配, 行结果写矩阵
            try
            {
                await foreach (var row in channel.Reader.ReadAllAsync(linkedToken).ConfigureAwait(false))
                {
                    await ScanBFStageMapRowAsync(
                        row,
                        stageMap,
                        templateIds,
                        templateMatchScoreThreshold,
                        xDirection,
                        yDirection,
                        size,
                        htmlLogUniqueId,
                        linkedToken).ConfigureAwait(false);
                }
            }
            finally
            {
                if (writerTask.IsCompleted == false) linkedCancellationTokenSource.Cancel();
                try
                {
                    await writerTask.ConfigureAwait(false);
                }
                catch
                {
                    // Writer 的异常已通过 channel 传递给 Reader, 此处避免二次抛出覆盖根因
                }

                while (channel.Reader.TryRead(out var row)) DisposeImages(row);
            }

            stageMap.Refresh();
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
                .. stageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                .. stageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
            ]), htmlLogUniqueId.LoggingHtml());
        }
        finally
        {
            foreach (var templateId in templateIds) CalibrationAlgorithmService.TryCleanTemplate(Cache.ChuckStageMapCacheBFItem.TemplateMatchSetting.ToHalconTemplateType(), templateId);
            StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), CalChipSiteModelEnum.ChuckModel);
        }
    }

    private async Task ScanBFStageMapRowAsync(
        ChuckStageMapBFScanRow row,
        StageMap stageMap,
        HTuple[] templateIds,
        double templateMatchScoreThreshold,
        double xDirection,
        double yDirection,
        MicroscopePixelSizeDTO size,
        Guid htmlLogUniqueId,
        CancellationToken cancellationToken)
    {
        var y = row.Y;
        var isInWaferColumnIndexes = row.IsInWaferColumnIndexes;
        var images = row.Images;

        try
        {
            var rowHtmlBullets = new HtmlBullet[isInWaferColumnIndexes.Length];

            await Parallel.ForEachAsync(isInWaferColumnIndexes.Index(), new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            }, (item, token) =>
            {
                token.ThrowIfCancellationRequested();

                var (i, isInWaferColumnIndex) = item;
                var templateIdIndex = xDirection > 0 ? i % Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Length : ^(i % Cache.ChuckStageMapCacheBFItem.StageMapTemplates.Length + 1);

                var templateROI = Cache.ChuckStageMapCacheBFItem.StageMapTemplates[templateIdIndex].TemplateROI;

                using var image = images[i];

                var isSuccess = CalibrationAlgorithmService.TryTemplateMatchToOffset(
                    Cache.ChuckStageMapCacheBFItem.TemplateMatchSetting.ToHalconTemplateType(),
                    image,
                    templateIds[templateIdIndex],
                    htmlLogUniqueId,
                    out var matchPoint,
                    out var matchOffset,
                    out var matchScore,
                    out var matchAngle);
                var oldMatchPoint = matchPoint;
                var oldMatchOffset = matchOffset;
                var offset = matchPoint - Cache.ChuckStageMapCacheBFItem.StageMapTemplates[templateIdIndex].TemplateROI.Center;
                matchOffset = new Point(offset.X, -offset.Y);

                var resultImageFilePath = Path.Combine(isSuccess ? ImageFileDirectory : $"{FileHelper.GetFileFullName(Cache.ChuckStageMapCacheBFItem.StageMapTemplates[templateIdIndex].TemplateFilePath)}_Error", $"Origin_Score({matchScore:0.###},{templateMatchScoreThreshold:0.###})_Angle{matchAngle:0.###}_{Guid.NewGuid():N}.jpg");
                image.SaveImage(resultImageFilePath);

                var vector = new Vector(xDirection * matchOffset.X * size.Result.PixelSize.Width, yDirection * matchOffset.Y * size.Result.PixelSize.Height);

                rowHtmlBullets[i] = new HtmlBullet(new
                {
                    isInWaferColumnIndex,
                    isSuccess,
                    IdealPoint = stageMap.IdealMatrix[y][isInWaferColumnIndex],
                    templateROI,
                    image.Size,
                    oldMatchPoint,
                    oldMatchOffset,
                    matchPoint,
                    matchOffset,
                    matchScore,
                    matchAngle,
                    templateMatchScoreThreshold,
                    vector,
                    HtmlTab = new HtmlTab(new
                    {
                        ResultImage = new HtmlImage(resultImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(templateROI), new HtmlImageCrossOverlay(matchPoint)]),
                        TemplateImage = new HtmlImage(Cache.ChuckStageMapCacheBFItem.StageMapTemplates[templateIdIndex].TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                });

                stageMap.IsMatchMatrix[y][isInWaferColumnIndex] = isSuccess;
                if (isSuccess) stageMap.ErrorMatrix[y][isInWaferColumnIndex] = vector;

                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);

            stageMap.Refresh();

            // 每行一个 HtmlContainer: 流水线并发场景下, 行内列结果作为整体写入, 行间日志不交错
            Logger.LogHtmlInformation($"{y + 1} row", HtmlHeaderLevelEnum.Header5, new HtmlContainer([.. rowHtmlBullets]), htmlLogUniqueId.LoggingHtml());
        }
        finally
        {
            DisposeImages(row);
        }
    }

    private static void DisposeImages(ChuckStageMapBFScanRow row)
    {
        foreach (var image in row.Images)
        {
            using var _ = image;
        }
    }

    private sealed record ChuckStageMapDFScanRow(int Y, int[] IsInWaferColumnIndexes, DarkFieldImageDTO[] Images);

    private async Task ScanDFStageMapAsync(
        StageMap stageMap,
        Guid htmlLogUniqueId,
        CancellationToken cancellationToken,
        bool isInterpolateErrors = false,
        StageMap? interpolateStageMap = null)
    {
        var (xDirection, yDirection) = StageViewModel.GetMachineDirection();

        var xSize = CIBXPixelSizes.SingleOrDefault(t => t.ProductivityInformation == Cache.ChuckStageMapCacheDFItem.ProductivityInformation);
        Guard.IsTrue(xSize?.IsOk == true, "Laser X Pixel Size is Empty or not verify.");

        var ySize = CIBYPixelSizes.SingleOrDefault(t => t.ProductivityInformation.OpticsIlluminationModeEnum == Cache.ChuckStageMapCacheDFItem.ProductivityInformation.OpticsIlluminationModeEnum
                                                        && t.ProductivityInformation.OpticsMagType == Cache.ChuckStageMapCacheDFItem.ProductivityInformation.OpticsMagType
                                                        && t.PmtId == Cache.ChuckStageMapCacheDFItem.CIBInformation.PMTId);
        Guard.IsTrue(ySize?.IsOk == true, "Laser Y Pixel Size is Empty or not verify.");

        Logger.LogHtmlInformation("Productivity Information", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
        {
            ProductivityInformation = new HtmlQuote(Cache.ChuckStageMapCacheDFItem.ProductivityInformation.ToHtmlAnonymous()),
            xDirection,
            yDirection,
            xSize.XPixelSize,
            ySize.YPixelSize
        }), htmlLogUniqueId.LoggingHtml());

        HTuple[] templateIds = [];

        try
        {
            templateIds =
            [
                .. Cache.ChuckStageMapCacheDFItem.StageMapTemplates
                    .Select(t =>
                    {
                        CalibrationAlgorithmService.TryReadTemplate(Cache.ChuckStageMapCacheDFItem.TemplateMatchSetting.ToHalconTemplateType(), t.TemplateFilePath, out var templateId);

                        return templateId;
                    })
            ];

            var templateMatchScoreThreshold = Cache.ChuckStageMapCacheDFItem.TemplateMatchSetting.ToTemplateMatchScoreThreshold(CalibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum, CalibrationSetting);

            Point[][] stageMapIdealMatrix =
            [
                .. stageMap.IdealMatrix.Select<Point[], Point[]>(t =>
                [
                    .. t.Select(tt => tt)
                ])
            ];
            var (yLength, xLength) = stageMapIdealMatrix.GetYXLength();

            Logger.LogHtmlInformation("rows", HtmlHeaderLevelEnum.Header4, htmlLogUniqueId.LoggingHtml());

            if (isInterpolateErrors)
            {
                var sourceStageMap = Guard.IsNotNullAndReturn(interpolateStageMap);
                var targetCircle = new Circle(StageViewModel.DarkFieldToMachinePosition(Point.Origin), Cache.ChuckStageMapCacheDFItem.WaferRadius);
                var errors = sourceStageMap.InterpolateErrors(stageMapIdealMatrix, targetCircle);
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

            // 容量为 2: Reader 匹配第 y 行时, Writer 可继续采集后续行, 扫描与模板匹配流水线并行, 扫描无需等待匹配完成
            var channel = Channel.CreateBounded<ChuckStageMapDFScanRow>(new BoundedChannelOptions(2) { SingleReader = true, SingleWriter = true, AllowSynchronousContinuations = true });

            // Writer: 移动平台取图, 一行采完塞进 channel
            var writerTask = Task.Run(async () =>
            {
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

                        StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(points[0], calChipSiteModelEnum: CalChipSiteModelEnum.ChuckModel);
                        DarkFieldImageDTO[] darkFieldImages =
                        [
                            .. await CIBViewModel.GetPMTImagesAsync(
                                Cache.ChuckStageMapCacheDFItem.ProductivityInformation,
                                StageCoordinateSystemEnum.Machine,
                                points,
                                Cache.ChuckStageMapCacheDFItem.ImageWidth,
                                Cache.ChuckStageMapCacheDFItem.CIBInformation,
                                (true, null),
                                (false, Cache.ChuckStageMapCacheDFItem.OpticsConfiguration),
                                (false, Cache.ChuckStageMapCacheDFItem.CIBConfiguration),
                                (false, Cache.ChuckStageMapCacheDFItem.LaserLightInformation),
                                false,
                                linkedToken).ConfigureAwait(false)
                        ];

                        Guard.IsEqualTo(points.Length, darkFieldImages.Length);

                        await channel.Writer.WriteAsync(new ChuckStageMapDFScanRow(y, isInWaferColumnIndexes, darkFieldImages), linkedToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    channel.Writer.TryComplete(ex);
                    throw;
                }
                finally
                {
                    channel.Writer.TryComplete();
                }
            }, linkedToken);

            // Reader: 从 channel 读一行, 并行模板匹配, 行结果写矩阵
            try
            {
                await foreach (var row in channel.Reader.ReadAllAsync(linkedToken).ConfigureAwait(false))
                {
                    await ScanDFStageMapRowAsync(
                        row,
                        stageMap,
                        templateIds,
                        templateMatchScoreThreshold,
                        xDirection,
                        yDirection,
                        xSize.XPixelSize,
                        ySize.YPixelSize,
                        htmlLogUniqueId,
                        linkedToken).ConfigureAwait(false);
                }
            }
            finally
            {
                if (writerTask.IsCompleted == false) linkedCancellationTokenSource.Cancel();
                try
                {
                    await writerTask.ConfigureAwait(false);
                }
                catch
                {
                    // Writer 的异常已通过 channel 传递给 Reader, 此处避免二次抛出覆盖根因
                }

                while (channel.Reader.TryRead(out var row)) DisposeImages(row);
            }

            stageMap.Refresh();
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlContainer([
                .. stageMap.PlotDataSource.GetAllHtmlVectorFieldCharts(),
                .. stageMap.PlotDataSource.GetAllHtmlPlot3DCharts()
            ]), htmlLogUniqueId.LoggingHtml());
        }
        finally
        {
            foreach (var templateId in templateIds) CalibrationAlgorithmService.TryCleanTemplate(Cache.ChuckStageMapCacheDFItem.TemplateMatchSetting.ToHalconTemplateType(), templateId);
            StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ChuckModel)), CalChipSiteModelEnum.ChuckModel);
        }
    }

    private async Task ScanDFStageMapRowAsync(
        ChuckStageMapDFScanRow row,
        StageMap stageMap,
        HTuple[] templateIds,
        double templateMatchScoreThreshold,
        double xDirection,
        double yDirection,
        double xPixelSize,
        double yPixelSize,
        Guid htmlLogUniqueId,
        CancellationToken cancellationToken)
    {
        var y = row.Y;
        var isInWaferColumnIndexes = row.IsInWaferColumnIndexes;
        var darkFieldImages = row.Images;

        try
        {
            var rowHtmlBullets = new HtmlBullet[isInWaferColumnIndexes.Length];

            await Parallel.ForEachAsync(isInWaferColumnIndexes.Index(), new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            }, (item, token) =>
            {
                token.ThrowIfCancellationRequested();

                var (i, isInWaferColumnIndex) = item;
                var templateIdIndex = xDirection > 0 ? i % Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Length : ^(i % Cache.ChuckStageMapCacheDFItem.StageMapTemplates.Length + 1);

                var templateROI = Cache.ChuckStageMapCacheDFItem.StageMapTemplates[templateIdIndex].TemplateROI;
                using var image = xDirection > 0 ? darkFieldImages[i].Image : darkFieldImages[i].Image.HorizontalFlip();

                var isSuccess = CalibrationAlgorithmService.TryTemplateMatchToOffset(
                    Cache.ChuckStageMapCacheDFItem.TemplateMatchSetting.ToHalconTemplateType(),
                    image,
                    templateIds[templateIdIndex],
                    htmlLogUniqueId,
                    out var matchPoint,
                    out var matchOffset,
                    out var matchScore,
                    out var matchAngle);
                var oldMatchPoint = matchPoint;
                var oldMatchOffset = matchOffset;
                var offset = matchPoint - Cache.ChuckStageMapCacheDFItem.StageMapTemplates[templateIdIndex].TemplateROI.Center;
                matchOffset = new Point(offset.X, -offset.Y);

                var resultImageFilePath = Path.Combine(isSuccess ? ImageFileDirectory : $"{FileHelper.GetFileFullName(Cache.ChuckStageMapCacheDFItem.StageMapTemplates[templateIdIndex].TemplateFilePath)}_Error", $"Origin_Score({matchScore:0.###},{templateMatchScoreThreshold:0.###})_Angle{matchAngle:0.###}_{Guid.NewGuid():N}.jpg");
                image.SaveImage(resultImageFilePath);

                var vector = new Vector(xDirection * matchOffset.X * xPixelSize, yDirection * matchOffset.Y * yPixelSize);

                rowHtmlBullets[i] = new HtmlBullet(new
                {
                    isInWaferColumnIndex,
                    isSuccess,
                    IdealPoint = stageMap.IdealMatrix[y][isInWaferColumnIndex],
                    templateROI,
                    image.Size,
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
                        ResultImage = new HtmlImage(resultImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(templateROI), new HtmlImageCrossOverlay(matchPoint)]),
                        TemplateImage = new HtmlImage(Cache.ChuckStageMapCacheDFItem.StageMapTemplates[templateIdIndex].TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                });

                stageMap.IsMatchMatrix[y][isInWaferColumnIndex] = isSuccess;
                if (isSuccess) stageMap.ErrorMatrix[y][isInWaferColumnIndex] = vector;

                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);

            stageMap.Refresh();

            // 每行一个 HtmlContainer: 流水线并发场景下, 行内列结果作为整体写入, 行间日志不交错
            Logger.LogHtmlInformation($"{y + 1} row", HtmlHeaderLevelEnum.Header5, new HtmlContainer([.. rowHtmlBullets]), htmlLogUniqueId.LoggingHtml());
        }
        finally
        {
            DisposeImages(row);
        }
    }

    private static void DisposeImages(ChuckStageMapDFScanRow row)
    {
        foreach (var darkFieldImage in row.Images)
        {
            using var _ = darkFieldImage;
        }
    }

    private void DownloadStageMap(StageMap downloadStageMap, Circle? targetCircle, Guid htmlLogUniqueId)
    {
        StageViewModel.SetEnableStageMap(false);

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

        Logger.LogHtmlInformation("Download", HtmlHeaderLevelEnum.Header3, new HtmlContainer(
        [
            new HtmlPlot3DChart(xPoint3DList, "X Error", HtmlPlot3DType.Surface),
            new HtmlPlot3DChart(yPoint3DList, "Y Error", HtmlPlot3DType.Surface)
        ]), htmlLogUniqueId.LoggingHtml());

        StageViewModel.SetStageMap(stageMapErrorDTO);

        StageViewModel.SetEnableStageMap(true);
    }

    private bool Save(ChuckStageMapDTO dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        ApplicationCookieService.SetCalibration(dto, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase calibration, CancellationToken cancellationToken)
    {
        var temp = Guard.IsAssignableToTypeAndReturn<ChuckStageMapDTO>(calibration);
        var status = Entry.Status;

        Calibration = temp;

        status.TotalCalibrationCount = 1;
        status.CalibratedCount = Calibration.IsCalibrated ? 1 : 0;
        status.VerifiedCount = Calibration.IsVerified ? 1 : 0;
        status.Details = [];
    }

    #endregion 校准
}