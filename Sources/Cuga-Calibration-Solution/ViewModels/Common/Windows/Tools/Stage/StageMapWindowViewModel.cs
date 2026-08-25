using System.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Cookies;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.SQL.Cache.Providers.Services.Interfaces;
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
using Core.Models.Helper;
using Net.Utilities.Calibration;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WaferMap.WPF.Primitives.Builders;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

[IOCAppService(ServiceType = typeof(StageMapWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class StageMapWindowViewModel(
    CIBViewModel cibViewModel,
    MicroscopeViewModel microscopeViewModel,
    StageViewModel stageViewModel,
    CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel,
    AlignmentUserControlViewModel alignmentUserControlViewModel,
    ICacheProvider cacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    IOptions<ApplicationSetting> options,
    ApplicationCookie applicationCookie,
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

                    var waferMapDieBuilder = new WaferMapDieBuilder
                    {
                        DiePitchSize = new Size(Cache.DiePitchWidth, Cache.DiePitchHeight),
                        OriginalDiePoint = dfPosition
                    };

                    var dies = waferMapDieBuilder.BuildDie(circle);

                    Cache.CanvasDocument.OverlayerModel.AddRange(dies.Select(t => new StageMapDie
                    {
                        Index = t.Index,
                        Row = t.Row,
                        Col = t.Col,
                        Rect = t.Rect,
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

        for (var row = minRow; row <= maxRow; row++)
        {
            for (var col = minColumn; row <= maxColumn; row++)
            {
                var stageMapDie = stageMapDies.Single(t => t.Row == row && t.Col == col);

                foreach (var marker in stageMapDie.Markers)
                {
                    var dfMachinePoint = stageViewModel.DarkFieldToMachinePosition(stageMapDie.Rect.Point + marker);
                    
                    
                }
            }
        }

        return true;
    }, isSilent).ConfigureAwait(false);

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