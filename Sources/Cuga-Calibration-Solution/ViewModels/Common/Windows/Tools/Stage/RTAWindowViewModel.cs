using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models.CIB.LineCentricity;
using Core.Models.Models.Common.Cookies;
using Core.Services.Interfaces;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Editors.Getters.Options;
using Net.Utilities.Graphics.Primitives.Enums.Editors;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections;
using Core.Models.Models.CIB.XPixelSize;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Microscope.CalChip;
using Microsoft.Extensions.Hosting;
using Net.Utilities.ScottPlot.Extensions;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

[IOCAppService(ServiceType = typeof(RTAWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class RTAWindowViewModel(
    CIBViewModel cibViewModel,
    MicroscopeViewModel microscopeViewModel,
    StageViewModel stageViewModel,
    AlignmentUserControlViewModel alignmentUserControlViewModel,
    ICacheProvider cacheProvider,
    IApplicationCookieService applicationCookieCacheProvider,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    IDialogWindowProvider dialogWindowProvider,
    ApplicationCookie applicationCookie,
    IHostEnvironment hostEnvironment,
    ILogger<RTAWindowViewModel> logger) : ViewModelBase
{
    public string Name { get; } = "RTA";

    public bool IsDevelopment { get; } = hostEnvironment.IsDevelopment();

    public ApplicationCookie ApplicationCookie { get; } = applicationCookie;

    public AlignmentUserControlViewModel AlignmentUserControlViewModel { get; } = alignmentUserControlViewModel;

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

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
        }).ConfigureAwait(true);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task AddStageMapTemplateAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            try
            {
                Guard.IsEqualTo(Cache.MicroscopeLensInformation, microscopeViewModel.GetCurrentMicroscopeLensInformation());

                var stageMapTemplatePoint = new StageMapTemplate
                {
                    FindBFMachinePosition = stageViewModel.GetMachineStagePosition()
                };

                // 只能一个点
                if (Cache.StageMapTemplates.Length > 0)
                {
                    dialogWindowProvider.ShowDialog("Add Stage Map Template Warning: Only One Point!", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                    return;
                }

                Cache.StageMapTemplates = [.. Cache.StageMapTemplates, stageMapTemplatePoint];
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
    }

    [RelayCommand]
    private async Task GotoStageMapDocumentSelectedItemPositionAsync(StageMapTemplate stageMapTemplate)
    {
        await Task.Run(() =>
        {
            try
            {
                var index = Array.IndexOf(Cache.StageMapTemplates, stageMapTemplate);

                var drawable = Cache.StageMapDocument.Edit.SelectedItems.FirstOrDefault();
                if (drawable is null)
                {
                    dialogWindowProvider.ShowDialog("Goto Stage Map Document Selected Item Position Warning: Don't Select Die!", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                    return;
                }

                var stageMapDie = Guard.IsNotNullAndAssignableToTypeAndReturn<StageMapDie>(drawable);
                var darkFieldPosition = stageViewModel.MachineToDarkFieldPosition(stageMapDie.Markers[index]);

                var currentCIBLineCentricity = applicationCookieCacheProvider.GetCalibrations<CIBLineCentricityDTO>().Single(t => t.ProductivityInformation == Cache.ProductivityInformation && t.PmtId == Cache.CIBInformation.PMTId);
                stageViewModel.SetBrightFieldAbsoluteStageXy(microscopeViewModel.GetMicroscopeLensInformationPosition(
                    currentCIBLineCentricity.MicroscopeLensInformation,
                    microscopeViewModel.GetCurrentMicroscopeLensInformation(),
                    darkFieldPosition), Cache.CalChipSiteModelEnum);

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
        Guard.IsTrue(MicroscopeCalChip.IsOk, "Microscope Cal Chip is Empty or not verify.");

        AlignmentUserControlViewModel.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum;
        AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;

        if (dialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment?",
                out var dialogResult,
                DialogButtonsEnum.YesNo,
                DialogIconEnum.Question) == true && dialogResult == DialogResultEnum.Yes) AlignmentUserControlViewModel.IsDarkFieldAlignment = true;
        else AlignmentUserControlViewModel.IsDarkFieldAlignment = false;

        await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

        var newAlignmentResult = AlignmentUserControlViewModel.AlignmentResult;
        if (Cache.AlignmentResult.MarkPoint1 != Point.Origin
            && Cache.AlignmentResult.MarkPoint2 != Point.Origin
            && dialogWindowProvider.TryShowDialog("Yes: Apply the alignment offset to the template points? No: Not Apply?",
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

        AlignmentResult = newAlignmentResult;

        stageViewModel.SetBrightFieldAbsoluteStageXy(stageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(Cache.CalChipSiteModelEnum)), Cache.CalChipSiteModelEnum);

        logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            AlignmentUserControlViewModel.CalChipSiteModelEnum,
            AlignmentUserControlViewModel.IsDarkFieldAlignment,
            AlignmentResult = new HtmlQuote(AlignmentResult)
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
            var bfPosition = stageViewModel.MachineToBrightFieldPosition(stageMapTemplate.FindBFMachinePosition);
            var dfPosition = cibViewModel.GetCIBInformationPosition(
                StageCoordinateSystemEnum.Dark,
                Cache.ProductivityInformation,
                Cache.CIBInformation,
                bfPosition,
                Cache.MicroscopeLensInformation);

            dfPositions[index] = dfPosition;
        }

        logger.LogHtmlInformation("Templates", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            CalChipPosition = MicroscopeCalChip.GetBFMachinePosition(Cache.CalChipSiteModelEnum),
            Table = new HtmlTable(
            [
                .. Cache.StageMapTemplates.Index().Select(t => new
                {
                    t.Index,
                    t.Item.FindBFMachinePosition,
                    t.Item.TemplateROI,
                    t.Item.TemplateFilePath,
                    t.Item.TemplateImageFilePath,
                    DFPosition = dfPositions[t.Index]
                })
            ])
        }), HtmlLogUniqueId.LoggingHtml());

        Cache.StageMapDocument.RunDesign(() =>
        {
            Cache.StageMapDocument.WaferModel.Clear();
            Cache.StageMapDocument.DieModel.Clear();

            var dfCircle = new Circle(stageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.GetBFMachinePosition(Cache.CalChipSiteModelEnum)), Cache.WaferRadius);
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

        Cache.AlignmentResult = AlignmentResult;

        return Task.FromResult(true);
    }, isSilent, cancellationToken).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(2, async () =>
    {
        var xSize = applicationCookieCacheProvider.GetCalibrations<CIBXPixelSizeDTO>(cancellationToken).SingleOrDefault(t => t.ProductivityInformation == Cache.ProductivityInformation);
        Guard.IsTrue(xSize?.IsOk == true, "Laser X Pixel Size is Empty or not verify.");

        var ySize = applicationCookieCacheProvider.GetCalibrations<CIBYPixelSizeDTO>(cancellationToken).SingleOrDefault(t => t.ProductivityInformation.OpticsIlluminationModeEnum == Cache.ProductivityInformation.OpticsIlluminationModeEnum
                                                                                                                             && t.ProductivityInformation.OpticsMagType == Cache.ProductivityInformation.OpticsMagType
                                                                                                                             && t.PmtId == Cache.CIBInformation.PMTId);
        Guard.IsTrue(ySize?.IsOk == true, "Laser Y Pixel Size is Empty or not verify.");

        logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            ProductivityInformation = new HtmlQuote(Cache.ProductivityInformation.ToHtmlAnonymous()),
            xSize.XPixelSize,
            ySize.YPixelSize
        }), HtmlLogUniqueId.LoggingHtml());

        Cache.RTAScanResult = new RTAScanResult(Cache.ProductivityInformation, xSize.XPixelSize, ySize.YPixelSize);

        dialogWindowProvider.ShowDialog("Please Select Dies to Scan");

        StageMapDie[] stageMapDies;
        using (Cache.StageMapDocument.View.Sync.EnterScope()) stageMapDies = [.. Cache.StageMapDocument.DieModel];

        foreach (var stageMapDie in stageMapDies) stageMapDie.IsInStageMapScan = false;

        var inputResult = await StageMapDieSelectionGetter
            .RunAsync<StageMapDieSelectionGetter>(Cache.StageMapDocument.Edit, new SelectionInputOptions<StageMapDie>
            {
                IsMultipleSelection = true,
                CancellationToken = cancellationToken
            })
            .ConfigureAwait(false);
        Guard.IsTrue(inputResult.OutputResultModeEnum == OutputResultModeEnum.Ok);

        StageMapDie[] selectStageMapDies = [.. inputResult.Output.OrderBy(t => t.Index.Y).ThenBy(t => t.Index.X)];
        Guard.IsNotEmpty(selectStageMapDies);

        if (selectStageMapDies.DistinctBy(t => t.Row).Count() > 1)
        {
            dialogWindowProvider.ShowDialog("Only One Row Can Be Selected!", DialogButtonsEnum.OK, DialogIconEnum.Warning);

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
            logger.LogHtmlInformation("Scan", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            if (IsDevelopment) cibViewModel.UseSimulatorImages(string.IsNullOrWhiteSpace(Cache.ForwardSimulatorImageFilePath) ? null : [Cache.ForwardSimulatorImageFilePath]);

            stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(startPosition);
            forwardDarkFieldRawScanImage = await cibViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Machine,
                startPosition,
                endPosition,
                Cache.CIBInformation,
                (true, null),
                (false, Cache.OpticsConfiguration),
                (false, Cache.CIBConfiguration),
                (false, Cache.LaserLightInformation),
                false,
                cancellationToken);

            var forwardRTAScanSamples = calibrationAlgorithmService.GetRTAScanSamples(forwardDarkFieldRawScanImage, Cache.StageMapConfig, true, HtmlLogUniqueId);
            Guard.IsNotEmpty(forwardRTAScanSamples);
            Cache.RTAScanResult.ForwardRTAScanSamples = forwardRTAScanSamples;

            if (IsDevelopment) cibViewModel.UseSimulatorImages(string.IsNullOrWhiteSpace(Cache.ReverseSimulatorImageFilePath) ? null : [Cache.ReverseSimulatorImageFilePath]);

            stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(endPosition);
            reverseDarkFieldRawScanImage = await cibViewModel.GetPMTImageAsync(
                Cache.ProductivityInformation,
                StageCoordinateSystemEnum.Machine,
                startPosition,
                endPosition,
                Cache.CIBInformation,
                (true, null),
                (false, Cache.OpticsConfiguration),
                (false, Cache.CIBConfiguration),
                (false, Cache.LaserLightInformation),
                false,
                cancellationToken,
                isForward: false);

            var reverseRTAScanSamples = calibrationAlgorithmService.GetRTAScanSamples(reverseDarkFieldRawScanImage, Cache.StageMapConfig, false, HtmlLogUniqueId);
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