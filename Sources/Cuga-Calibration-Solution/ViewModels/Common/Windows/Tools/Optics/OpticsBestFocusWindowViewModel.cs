using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.CalChip;
using Core.Services.Interfaces;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using Microsoft.Extensions.Options;
using MiniExcelLibs;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using System.IO;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Optics;

public sealed record Temp(double PixelX, double X, double ECS, double NSC, double Strehl)
{
    public Temp() : this(0d, 0d, 0d, 0d, 0d)
    {
    }
}

public sealed record TempTraceBuffer(double Trigger, double X, double Ecs, double NSC)
{
    public TempTraceBuffer() : this(0d, 0d, 0d, 0d)
    {
    }
}

[IOCAppService(ServiceType = typeof(OpticsBestFocusWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsBestFocusWindowViewModel(
    IApplicationCookieService applicationCookieService,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    IOptions<ApplicationSetting> options) : AbstractOpticsGrabbingImageWindowViewModel<OpticsBestFocusCache>
{
    [DefaultCache]
    [ObservableProperty]
    public override partial OpticsBestFocusCache Cache { get; set; } = new();

    [ObservableProperty]
    public new partial IReadOnlyList<OpticsBestFocusResult> Results { get; set; } = [];

    public List<string> FilePaths { get; set; } = [];

    public List<List<Temp>> TempXs { get; set; } = [];

    public List<List<Temp>> TempYs { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeCalChipCache MicroscopeCalChipCache { get; set; } = new();

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = HostApplication.GetRequiredService<AlignmentUserControlViewModel>();

    public override string Name => "Best Focus";

    public IReadOnlyList<string> Steps { get; } =
    [
        "Step 1 Alignment",
        "Step 2 Mark",
        "Step 3 Best Focus"
    ];

    protected override async Task LoadedAsync() => await Task.Run(async () =>
    {
        await base.LoadedAsync().ConfigureAwait(false);

        Results = [];
        MicroscopeCalChipCache = applicationCookieService.GetCache<MicroscopeCalChipCache>();
    }).ConfigureAwait(false);

    protected override bool InvokeDarkFieldImageDTO(DarkFieldImageDTO darkFieldImage, List<(double Trigger, double X, double Ecs, double NSC)> triggerBuffers)
    {
        base.InvokeDarkFieldImageDTO(darkFieldImage, triggerBuffers);

        var isSuccess = false;

        var item = new OpticsBestFocusResult { DarkFieldImage = darkFieldImage };

        var startECS = Cache.CenterECS - Cache.RangeECS;
        var stopECS = Cache.CenterECS + Cache.RangeECS;
        try
        {
            var filePath = Path.Combine(options.Value.AppHomeDirectory, "BestFocus", "TraceBuffer", $"{DateTime.Now.ToString(Constants.LongFileDateTimeFormat)}.xlsx");
            DirectoryHelper.CreateFileDirectoryIfNotExists(filePath);
            MiniExcel.SaveAs(filePath, triggerBuffers.Select(t => new TempTraceBuffer(t.Trigger, t.X, t.Ecs, t.NSC)).ToArray());
            FilePaths.Add(filePath);

            var tempXs = new List<Temp>();
            var tempYs = new List<Temp>();
            var temp = darkFieldImage.Clone();
            temp.IsKeepRawImageCIBProfileModeEnum = false;
            using var image = temp.GetImage();

            item.BestFocus = calibrationAlgorithmService.GetBestFocus(image, startECS, stopECS, Cache.AlgorithmEngineTypeEnum, Cache.AlgorithmBestFocusTypeEnum, HtmlLogUniqueId);
            item.BestFocus.RawImageFilePath = darkFieldImage.RawImageFilePath;

            var triggerStartIndex = triggerBuffers.Index().First(t => t.Item.Trigger > 0).Index;
            var triggerEndIndex = triggerBuffers.Index().Last(t => t.Item.Trigger > 0).Index;

            var filter = triggerBuffers.ToArray().AsSpan()[triggerStartIndex..(triggerEndIndex + 1)].ToArray();
            var linearSplineECS = LinearSpline.InterpolateSorted(Generate.LinearRange(0, filter.Length - 1), [.. filter.Select(t => t.Ecs)]);
            var linearSplineNSC = LinearSpline.InterpolateSorted(Generate.LinearRange(0, filter.Length - 1), [.. filter.Select(t => t.NSC)]);
            var linearSplineX = LinearSpline.InterpolateSorted(Generate.LinearRange(0, filter.Length - 1), [.. filter.Select(t => t.X)]);
            foreach (var bestFocusXFieldTiltFitPoint in item.BestFocus.XStrehlRatioFitPoints)
            {
                var t = bestFocusXFieldTiltFitPoint.X / Cache.ProductivityInformation.SampleRate;

                var ecs = linearSplineECS.Interpolate(t);
                var nsc = linearSplineNSC.Interpolate(t);
                var x = linearSplineX.Interpolate(t);

                tempXs.Add(new Temp(bestFocusXFieldTiltFitPoint.X, x, ecs, nsc, bestFocusXFieldTiltFitPoint.Y));
            }

            foreach (var bestFocusYFieldTiltFitPoint in item.BestFocus.YStrehlRatioFitPoints)
            {
                var t = bestFocusYFieldTiltFitPoint.X / Cache.ProductivityInformation.SampleRate;

                var ecs = linearSplineECS.Interpolate(t);
                var nsc = linearSplineNSC.Interpolate(t);
                var x = linearSplineX.Interpolate(t);

                tempYs.Add(new Temp(bestFocusYFieldTiltFitPoint.X, x, ecs, nsc, bestFocusYFieldTiltFitPoint.Y));
            }

            TempXs.Add(tempXs);
            TempYs.Add(tempYs);

            Logger.LogHtmlInformation("Best Focus OK", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
            {
                Result = new HtmlBullet(item.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            isSuccess = true;
        }
        catch (Exception ex)
        {
            item.BestFocus.RawImageFilePath = darkFieldImage.RawImageFilePath;
            Logger.LogHtmlError("Best Focus Error", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
            {
                Exception = ex,
                Result = new HtmlBullet(item.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());
        }

        Results = [.. Results, item];

        return isSuccess;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0Async(bool isSilent, CancellationToken cancellationToken)
    {
        return await InvokeAsync(0, async () =>
        {
            AlignmentUserControlViewModel.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum;
            AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;

            DialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?",
                out var dialogResult,
                DialogButtonsEnum.YesNo,
                DialogIconEnum.Question);

            AlignmentUserControlViewModel.IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;

            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            var alignmentResult = AlignmentUserControlViewModel.AlignmentResult;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.CalChipSiteModelEnum,
                AlignmentUserControlViewModel.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(alignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition((alignmentResult.MarkPoint1 + (Vector)alignmentResult.MarkPoint2) / 2d), CalChipSiteModelEnum.DswModel);

            Cache.AlignmentResult = alignmentResult;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                AlignmentResult = new HtmlQuote(Cache.AlignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        }, isSilent).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1Async(bool isSilent, CancellationToken cancellationToken)
    {
        return await InvokeAsync(1, () =>
        {
            Cache.MicroscopeLensInformation = MicroscopeViewModel.GetCurrentMicroscopeLensInformation();
            Cache.DSWFindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation,
                Cache.DSWFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return Task.FromResult(true);
        }, isSilent).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2Async(bool isSilent, CancellationToken cancellationToken)
    {
        return await InvokeAsync(2, async () =>
        {
            Results = [];
            FilePaths = [];
            TempXs = [];
            TempYs = [];

            await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
            var dswBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.DSWFindBFMachinePosition);
            StageViewModel.SetBrightFieldAbsoluteStageXy(dswBFPosition, CalChipSiteModelEnum.DswModel);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                AlignmentResult = new HtmlQuote(Cache.AlignmentResult.ToHtmlAnonymous()),
                Cache.MicroscopeLensInformation,
                Cache.DSWFindBFMachinePosition,
                dswBFPosition
            }), HtmlLogUniqueId.LoggingHtml());

#if NET
            await
#endif
            using var _ = cancellationToken.Register(() =>
            {
                if (GetPMTImagesByXZSyncCommand.CanBeCanceled) GetPMTImagesByXZSyncCommand.Cancel();
            });

            var task = Guard.IsAssignableToTypeAndReturn<Task<bool>>(GetPMTImagesByXZSyncCommand.ExecuteAsync(true));

            try
            {
                if (await task.ConfigureAwait(false) == false) return false;
            }
            finally
            {
                LogDetails();
            }

            var filePathX = Path.Combine(options.Value.AppHomeDirectory, "BestFocus", "X", $"{DateTime.Now.ToString(Constants.LongFileDateTimeFormat)}.xlsx");
            DirectoryHelper.CreateFileDirectoryIfNotExists(filePathX);
            var sheetXs = new Dictionary<string, object>();

            foreach (var (index, item) in TempXs.Index())
            {
                sheetXs.Add(HtmlPlot2DLinesChart.ToSafeSheetName($"{index + 1}-{Path.GetFileName(FilePaths[index])}", string.Empty), item);
            }

            await MiniExcel.SaveAsAsync(filePathX, sheetXs, cancellationToken: cancellationToken);

            var filePathY = Path.Combine(options.Value.AppHomeDirectory, "BestFocus", "Y", $"{DateTime.Now.ToString(Constants.LongFileDateTimeFormat)}.xlsx");
            DirectoryHelper.CreateFileDirectoryIfNotExists(filePathY);
            var sheetYs = new Dictionary<string, object>();

            foreach (var (index, item) in TempYs.Index())
            {
                sheetYs.Add(HtmlPlot2DLinesChart.ToSafeSheetName($"{index + 1}-{Path.GetFileName(FilePaths[index])}", string.Empty), item);
            }

            await MiniExcel.SaveAsAsync(filePathY, sheetYs, cancellationToken: cancellationToken);

            return true;
        }, isSilent).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task AllAsync(CancellationToken cancellationToken)
    {
#if NET
        await
#endif
        using var _ = cancellationToken.Register(() =>
        {
            if (Step0Command.CanBeCanceled) Step0Command.Cancel();
            if (Step2Command.CanBeCanceled) Step2Command.Cancel();
        });

        var oldMarkPoint1 = Cache.AlignmentResult.MarkPoint1;
        var oldMarkPoint2 = Cache.AlignmentResult.MarkPoint1;

        var step0Task = Guard.IsAssignableToTypeAndReturn<Task<bool>>(Step0Command.ExecuteAsync(true));
        if (await step0Task.ConfigureAwait(true) == false) return;

        var newMarkPoint1 = Cache.AlignmentResult.MarkPoint1;
        var newMarkPoint2 = Cache.AlignmentResult.MarkPoint1;

        var offset = (newMarkPoint1 - oldMarkPoint1 + (newMarkPoint2 - oldMarkPoint2)) / 2d;
        var oldDSWFindBFMachinePosition = Cache.DSWFindBFMachinePosition;
        Cache.DSWFindBFMachinePosition += offset;

        Logger.LogHtmlInformation(Steps[1], HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
        {
            offset,
            oldDSWFindBFMachinePosition,
            Cache.DSWFindBFMachinePosition
        }), HtmlLogUniqueId.LoggingHtml());

        await Step2Command.ExecuteAsync(true).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> ManualStep2Async(CancellationToken cancellationToken)
    {
        return await InvokeAsync(2, () =>
        {
            Results = [];

            if (DialogWindowProvider.TryShowSelectFilePathsDialog(".raw", out var fileNames) != true) return Task.FromResult(false);

            Guard.IsNotEmpty(fileNames);

            var boolList = new List<bool>();
            foreach (var fileName in fileNames)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var match = CIBInformation.PMTChannelRegex.Match(Path.GetFileName(fileName));

                var cibInformation = match.Success
                    ? ApplicationCookie.CIBInformations.SingleOrDefault(t => t.PMTId == int.Parse(match.Groups[1].Value)
                                                                             && t.ChannelId == int.Parse(match.Groups[2].Value), CIBInformation.Default)
                    : CIBInformation.Default;

                var darkFieldRawScanImage = new DarkFieldRawScanImageDTO
                {
                    CIBInformation = cibInformation,
                    Size = RAWImageFactory.GetSize(fileName).Size,
                    RawImageCIBProfileModeEnum = CIBProfileModeEnum.PMTLog,
                    RawImageFilePath = fileName,
                    IsKeepRawImageCIBProfileModeEnum = Cache.CIBConfiguration.IsKeepRawImageCIBProfileModeEnum
                };

#pragma warning disable IDE0079
#pragma warning disable IDISP004

                boolList.Add(InvokeDarkFieldImageDTO(new DarkFieldImageDTO().AdaptIn(darkFieldRawScanImage), []));

#pragma warning restore IDISP004
#pragma warning restore IDE0079
            }

            try
            {
                if (boolList.All(t => t) == false) return Task.FromResult(false);
            }
            finally
            {
                LogDetails();
            }

            return Task.FromResult(true);
        }, false).ConfigureAwait(false);
    }

    private void LogDetails()
    {
        var dictionary = new Dictionary<(string ContainerTitle, string PlotTitle), IReadOnlyList<Point>>();
        foreach (var (channelId, bestFocuses) in Results
                     .GroupBy(t => t.DarkFieldImage.CIBInformation.ChannelId)
                     .OrderBy(t => t.Key)
                     .Select(t => (t.Key, t.OrderBy(tt => tt.DarkFieldImage.CIBInformation).ToArray())))
        {
            dictionary[($"{nameof(BestFocus.BestXStrehlRatioPoint)} {nameof(CIBInformation.ChannelId)}: {channelId} ", "X: PMT Id - Y: Best Strehl Ratio X")] =
            [
                .. bestFocuses
                    .Where(t => t.BestFocus.IsAlgorithmOk)
                    .Select(t => new Point(t.DarkFieldImage.CIBInformation.PMTId, t.BestFocus.BestXStrehlRatioPoint.Y))
            ];
            dictionary[($"{nameof(BestFocus.BestXStrehlRatioECS)} {nameof(CIBInformation.ChannelId)}: {channelId} ", "X: PMT Id - Y: Best Strehl Ratio X ECS")] =
            [
                .. bestFocuses
                    .Where(t => t.BestFocus.IsAlgorithmOk)
                    .Select(t => new Point(t.DarkFieldImage.CIBInformation.PMTId, t.BestFocus.BestXStrehlRatioECS))
            ];
            dictionary[($"{nameof(BestFocus.BestYStrehlRatioPoint)} {nameof(CIBInformation.ChannelId)}: {channelId} ", "X: PMT Id - Y: Best Strehl Ratio Y")] =
            [
                .. bestFocuses
                    .Where(t => t.BestFocus.IsAlgorithmOk)
                    .Select(t => new Point(t.DarkFieldImage.CIBInformation.PMTId, t.BestFocus.BestYStrehlRatioPoint.Y))
            ];
            dictionary[($"{nameof(BestFocus.BestYStrehlRatioECS)} {nameof(CIBInformation.ChannelId)}: {channelId} ", "X: PMT Id - Y: Best Strehl Ratio Y ECS")] =
            [
                .. bestFocuses
                    .Where(t => t.BestFocus.IsAlgorithmOk)
                    .Select(t => new Point(t.DarkFieldImage.CIBInformation.PMTId, t.BestFocus.BestYStrehlRatioECS))
            ];
        }

        Logger.LogHtmlInformation("Details", HtmlHeaderLevelEnum.Header3, new HtmlContainer([.. dictionary.Select(t => new HtmlExpand(t.Key.ContainerTitle, new HtmlPlot2DLinesChart([(string.Empty, t.Value)], t.Key.PlotTitle)))]), HtmlLogUniqueId.LoggingHtml());
    }

    private async Task<bool> InvokeAsync(
        int stepIndex,
        Func<Task<bool>> func,
        bool isSilent)
    {
        return await Task.Run(async () =>
        {
            var isInitHtmlLog = isSilent == false || stepIndex == 0;
            var isEndHtml = isSilent == false || stepIndex == Steps.Count - 1;

            HtmlLogUniqueId = isInitHtmlLog ? Guid.NewGuid() : HtmlLogUniqueId;

            if (isInitHtmlLog) Logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation(Steps[stepIndex], HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                isSuccess = await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (isSilent) isEndHtml = true;

                if (ex is OperationCanceledException)
                {
                    DialogWindowProvider.ShowDialog($"{Name}: {Steps[stepIndex]} Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    Logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    return false;
                }

                DialogWindowProvider.ShowDialog($"""
                                                 {Name}: {Steps[stepIndex]} Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlError(ex, Name, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            }
            finally
            {
                if (isEndHtml)
                    Logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{(isSilent ? "All" : Steps[stepIndex].Replace(" ", string.Empty))}_{(isSuccess ? "OK" : "Failed")}"));
            }

            if (isSuccess)
            {
                if (isEndHtml) DialogWindowProvider.ShowDialog($"{Name}: {(isSilent ? "All" : Steps[stepIndex])} Success");
            }
            else
                DialogWindowProvider.ShowDialog($"{Name}: {Steps[stepIndex]} Error", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return isSuccess;
        }).ConfigureAwait(false);
    }

    public override void Dispose()
    {
        base.Dispose();

        foreach (var temp in Results)
        {
            using var _ = temp.DarkFieldImage;
        }
    }
}