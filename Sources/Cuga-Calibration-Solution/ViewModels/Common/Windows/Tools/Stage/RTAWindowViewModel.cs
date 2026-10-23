using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Cookies;
using Core.Services.Interfaces;
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
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections;
using System.ComponentModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

[IOCAppService(ServiceType = typeof(RTAWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class RTAWindowViewModel(
    CIBViewModel cibViewModel,
    MicroscopeViewModel microscopeViewModel,
    StageViewModel stageViewModel,
    AlignmentUserControlViewModel alignmentUserControlViewModel,
    ICacheProvider cacheProvider,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    IDialogWindowProvider dialogWindowProvider,
    ApplicationCookie applicationCookie,
    ILogger<RTAWindowViewModel> logger) : ViewModelBase
{
    public string Name { get; } = "RTA";

    public ApplicationCookie ApplicationCookie { get; } = applicationCookie;

    public AlignmentUserControlViewModel AlignmentUserControlViewModel { get; } = alignmentUserControlViewModel;

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
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial bool IsReady { get; private set; }

    [ObservableProperty]
    public partial bool IsClosing { get; private set; }

    public bool IsCloseReady { get; private set; }

    public bool IsEditable => CanRun();

    partial void OnCacheChanged(RTACache oldValue, RTACache newValue)
    {
        oldValue.PropertyChanged -= OnCachePropertyChanged;
        newValue.PropertyChanged += OnCachePropertyChanged;
        NotifyCommandStates();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is nameof(IsBusy) or nameof(IsReady) or nameof(IsClosing)) NotifyCommandStates();
    }

    private void OnCachePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RTACache.StageMapTemplates) or nameof(RTACache.IsWaferMapCurrent)) NotifyCommandStates();
    }

    private void NotifyCommandStates()
    {
        // 缓存回调可能发生在采集线程, 命令状态统一回到 UI 线程更新.
        HostApplication.ContextProvider.Send(() =>
        {
            OnPropertyChanged(nameof(IsEditable));
            AddStageMapTemplateCommand.NotifyCanExecuteChanged();
            RemoveStageMapTemplatesCommand.NotifyCanExecuteChanged();
            GotoStageMapDocumentSelectedItemPositionCommand.NotifyCanExecuteChanged();
            Step0Command.NotifyCanExecuteChanged();
            Step1Command.NotifyCanExecuteChanged();
            Step2Command.NotifyCanExecuteChanged();
        });
    }

    private bool CanRun() => IsReady && IsBusy == false && IsClosing == false;

    private bool CanAddMarker() => CanRun() && Cache.StageMapTemplates.Length == 0;

    private bool CanGenerateWaferMap() => CanRun() && Cache.StageMapTemplates.Length == 1;

    private bool CanUseWaferMap() => CanRun() && Cache.IsWaferMapCurrent;

    [RelayCommand]
    private async Task LoadedAsync()
    {
        IsCloseReady = false;
        IsClosing = false;
        IsReady = false;
        IsBusy = true;

        try
        {
            Cache = await Task.Run(() => cacheProvider.GetOrDefault<RTACache>()).ConfigureAwait(true);
            Cache.InvalidateWaferMap();
            Cache.ScanResult.Refresh();
            IsReady = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RTA 加载缓存失败");
            dialogWindowProvider.ShowDialog($"{Name}: Load Failed\n{ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddMarker))]
    private async Task AddStageMapTemplateAsync() => await RunOperationAsync("Mark", () =>
    {
        Guard.IsEqualTo(Cache.StageMapTemplates.Length, 0);
        Guard.IsEqualTo(Cache.MicroscopeLensInformation, microscopeViewModel.GetCurrentMicroscopeLensInformation());

        Cache.StageMapTemplates =
        [
            new StageMapTemplate
            {
                FindBFMachinePosition = stageViewModel.GetMachineStagePosition()
            }
        ];

        return Task.FromResult(true);
    }, false, CancellationToken.None).ConfigureAwait(true);

    [RelayCommand(CanExecute = nameof(CanRun))]
    private void RemoveStageMapTemplates(IEnumerable? selectedItems)
    {
        if (CanRun() == false || selectedItems is null) return;

        var selectedTemplates = selectedItems.Cast<StageMapTemplate>().ToArray();
        Cache.StageMapTemplates = [.. Cache.StageMapTemplates.Except(selectedTemplates)];
    }

    [RelayCommand(CanExecute = nameof(CanUseWaferMap))]
    private async Task GotoStageMapDocumentSelectedItemPositionAsync(StageMapTemplate? stageMapTemplate) => await RunOperationAsync("Goto", () =>
    {
        Guard.IsTrue(Cache.IsWaferMapCurrent, "Generate the wafer map again before Goto.");
        Guard.IsEqualTo(Array.IndexOf(Cache.StageMapTemplates, stageMapTemplate), 0);

        StageMapDie? die;
        using (Cache.StageMapDocument.View.Sync.EnterScope()) die = Cache.StageMapDocument.Edit.SelectedItems.OfType<StageMapDie>().FirstOrDefault();
        Guard.IsNotNull(die, "Select a die first.");
        Guard.IsEqualTo(die.Markers.Length, 1);

        var darkFieldPosition = stageViewModel.MachineToDarkFieldPosition(die.Markers[0]);
        var offset = cibViewModel.GetCIBInformationOffset(
            StageCoordinateSystemEnum.Dark,
            Cache.ProductivityInformation,
            Cache.CIBInformation,
            microscopeViewModel.GetCurrentMicroscopeLensInformation(),
            isLineCentricityOffset: false);

        stageViewModel.SetBrightFieldAbsoluteStageXy(darkFieldPosition - offset, Cache.CalChipSiteModelEnum);

        return Task.FromResult(true);
    }, false, CancellationToken.None).ConfigureAwait(true);

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanRun))]
    private async Task<bool> Step0Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(0, async () =>
    {
        var cache = Cache;

        if (dialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: use bright field alignment?",
                out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question) != true) return false;

        cache.InvalidateWaferMap();
        AlignmentUserControlViewModel.CalChipSiteModelEnum = cache.CalChipSiteModelEnum;
        AlignmentUserControlViewModel.ProductivityInformation = cache.ProductivityInformation.Clone();
        AlignmentUserControlViewModel.IsDarkFieldAlignment = cache.IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;

        await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var newAlignmentResult = AlignmentUserControlViewModel.AlignmentResult.Clone();
        if (cache.StageMapTemplates.Length == 1 && cache.AlignmentResult.MarkPoint1 != cache.AlignmentResult.MarkPoint2
            && dialogWindowProvider.TryShowDialog("Yes: apply the alignment offset to the marker point? No: keep the marker point?",
                out dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question) == true && dialogResult == DialogResultEnum.Yes)
        {
            var offset = (newAlignmentResult.MarkPoint1 - cache.AlignmentResult.MarkPoint1
                          + (newAlignmentResult.MarkPoint2 - cache.AlignmentResult.MarkPoint2)) / 2d;
            cache.StageMapTemplates[0].FindBFMachinePosition += offset;
        }

        cache.AlignmentResult = newAlignmentResult;
        stageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin, cache.CalChipSiteModelEnum);

        logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            cache.CalChipSiteModelEnum,
            cache.IsDarkFieldAlignment,
            AlignmentResult = new HtmlQuote(cache.AlignmentResult)
        }), HtmlLogUniqueId.LoggingHtml());

        return true;
    }, isSilent, cancellationToken).ConfigureAwait(true);

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanGenerateWaferMap))]
    private async Task<bool> Step1Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(1, () =>
    {
        var cache = Cache;
        cache.InvalidateWaferMap();
        Guard.IsEqualTo(cache.StageMapTemplates.Length, 1);
        Guard.IsTrue(cache.WaferRadius is > 0d and <= double.MaxValue);
        Guard.IsTrue(cache.DiePitchWidth is > 0d and <= double.MaxValue);
        Guard.IsTrue(cache.DiePitchHeight is > 0d and <= double.MaxValue);
        cancellationToken.ThrowIfCancellationRequested();

        var brightFieldPosition = stageViewModel.MachineToBrightFieldPosition(cache.StageMapTemplates[0].FindBFMachinePosition);
        var darkFieldPosition = cibViewModel.GetCIBInformationPosition(
            StageCoordinateSystemEnum.Dark,
            cache.ProductivityInformation,
            cache.CIBInformation,
            brightFieldPosition,
            cache.MicroscopeLensInformation);

        cache.StageMapDocument.RunDesign(() =>
        {
            cache.StageMapDocument.WaferModel.Clear();
            cache.StageMapDocument.DieModel.Clear();

            var circle = new Circle(Point.Origin, cache.WaferRadius);
            cache.StageMapDocument.WaferModel.Add(new StageMapWafer
            {
                Circle = new Circle(stageViewModel.DarkFieldToMachinePosition(circle.Center), circle.Radius)
            });

            var builder = new StageMapDieBuilder
            {
                DiePitchSize = new Size(cache.DiePitchWidth, cache.DiePitchHeight),
                OriginalDiePoint = darkFieldPosition
            };

            foreach (var die in builder.BuildDie(circle))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var extents = new Extents();
                extents.Add(stageViewModel.DarkFieldToMachinePosition(die.Rect.XMinYMin));
                extents.Add(stageViewModel.DarkFieldToMachinePosition(die.Rect.XMaxYMax));

                cache.StageMapDocument.DieModel.Add(new StageMapDie
                {
                    Index = die.Index,
                    Row = die.Row,
                    Col = die.Col,
                    Rect = (Rect)extents,
                    IsInWafer = circle.Contains(die.Rect.Point),
                    Markers = [stageViewModel.DarkFieldToMachinePosition(die.Rect.Point)]
                });
            }
        }, true);

        cancellationToken.ThrowIfCancellationRequested();
        cache.MarkWaferMapCurrent();

        return Task.FromResult(true);
    }, isSilent, cancellationToken).ConfigureAwait(true);

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanUseWaferMap))]
    private async Task<bool> Step2Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(2, async () =>
    {
        var cache = Cache;
        Guard.IsTrue(cache.IsWaferMapCurrent, "Generate the wafer map again before scanning.");

        StageMapDie[] dies;
        using (cache.StageMapDocument.View.Sync.EnterScope()) dies = [.. cache.StageMapDocument.DieModel];

        StageMapDie[] rowDies;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var inputResult = await StageMapDieSelectionGetter.RunAsync<StageMapDieSelectionGetter>(cache.StageMapDocument.Edit, new SelectionInputOptions<StageMapDie>
            {
                IsMultipleSelection = true,
                CancellationToken = cancellationToken
            });
            if (inputResult.OutputResultModeEnum != OutputResultModeEnum.Ok) throw new OperationCanceledException(cancellationToken);

            var rows = inputResult.Output.Select(t => t.Index.Y).Distinct().ToArray();
            if (rows.Length == 1)
            {
                rowDies = [.. dies.Where(t => t.Index.Y == rows[0] && t.IsInWafer)];
                if (rowDies.Length > 0) break;
            }

            dialogWindowProvider.ShowDialog("Select exactly one row containing dies inside the wafer.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }

        Guard.IsTrue(rowDies.All(t => t.Markers.Length == 1));
        var row = rowDies[0].Index.Y;
        var startPosition = new Point(rowDies.Min(t => t.Rect.XMin), rowDies[0].Markers[0].Y);
        var stopPosition = new Point(rowDies.Max(t => t.Rect.XMax), startPosition.Y);
        var productivityInformation = cache.ProductivityInformation.Clone();
        var cibInformation = cache.CIBInformation.Clone();
        var calChipSiteModelEnum = cache.CalChipSiteModelEnum;
        var opticsConfiguration = cache.OpticsConfiguration.Clone();
        var cibConfiguration = cache.CIBConfiguration.Clone();
        var laserLightInformation = cache.LaserLightInformation.Clone();
        Guard.IsTrue(productivityInformation.SampleRate is > 0d and <= double.MaxValue);

        var (xDirection, _) = stageViewModel.GetMachineDirection();
        var scanResult = cache.ScanResult;
        scanResult.RowScans = [];
        cache.StageMapDocument.RunDesign(() =>
        {
            foreach (var die in dies) die.IsInStageMapScan = rowDies.Contains(die);
        });

        logger.LogHtmlInformation("Selected Row", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            Row = row,
            DieCount = rowDies.Length,
            startPosition,
            stopPosition,
            productivityInformation.SampleRate
        }), HtmlLogUniqueId.LoggingHtml());

        try
        {
            foreach (var isForward in new[] { true, false })
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rowScan = new RTARowScan
                {
                    Row = row,
                    IsForward = isForward,
                    StartPosition = startPosition,
                    StopPosition = stopPosition,
                    SampleRate = productivityInformation.SampleRate
                };
                scanResult.RowScans = [.. scanResult.RowScans, rowScan];

                try
                {
                    var isMachineForward = xDirection > 0 ? isForward : isForward == false;
                    var rawScanImage = await cibViewModel.GetPMTImageAsync(
                        productivityInformation,
                        StageCoordinateSystemEnum.Machine,
                        startPosition,
                        stopPosition,
                        cibInformation,
                        (false, calChipSiteModelEnum),
                        (false, opticsConfiguration),
                        (false, cibConfiguration),
                        (false, laserLightInformation),
                        false,
                        cancellationToken,
                        isForward: isMachineForward).ConfigureAwait(false);

                    // 先记录原图, 后续算法失败或取消时仍保留本次采集信息.
                    rowScan.RawScanImage = rawScanImage;
                    cancellationToken.ThrowIfCancellationRequested();
                    Guard.IsEqualTo(rawScanImage.IsForward, isForward);

                    var samples = calibrationAlgorithmService.GetRTAScanSamples(rawScanImage);
                    cancellationToken.ThrowIfCancellationRequested();
                    Guard.IsNotEmpty(samples, "No valid RTA matching samples.");
                    rowScan.Samples = samples;

                    logger.LogHtmlInformation(isForward ? "Forward" : "Reverse", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        rowScan.Row,
                        rowScan.IsForward,
                        rowScan.StartPosition,
                        rowScan.StopPosition,
                        rowScan.SampleRate,
                        rawScanImage.Size,
                        rawScanImage.RawImageFilePath,
                        SampleCount = rowScan.Samples.Length,
                        rowScan.FFTStatus
                    }), HtmlLogUniqueId.LoggingHtml());

                    if (rowScan.GetFFTInput().Length < 4)
                        logger.LogHtmlWarning(rowScan.FFTStatus, HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                }
                catch (OperationCanceledException)
                {
                    rowScan.AnalysisError = "Canceled";
                    throw;
                }
                catch (Exception ex)
                {
                    rowScan.AnalysisError = ex.Message;
                    logger.LogHtmlError(ex, isForward ? "Forward" : "Reverse", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    if (rowScan.RawScanImage is null) throw;
                }
                finally
                {
                    scanResult.Refresh();
                }
            }

            if (scanResult.RowScans.Any(t => t.IsAnalysisSuccessful))
                logger.LogHtmlInformation("Mean Position And FFT", HtmlHeaderLevelEnum.Header3, new HtmlContainer([
                    .. scanResult.PlotDataSource.GetAllHtmlPlot2DLinesCharts()
                ]), HtmlLogUniqueId.LoggingHtml());

            return scanResult.RowScans.Length == 2 && scanResult.RowScans.All(t => t.IsAnalysisSuccessful);
        }
        finally
        {
            stageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin, calChipSiteModelEnum);
        }
    }, isSilent, cancellationToken).ConfigureAwait(true);

    [RelayCommand]
    private async Task CloseAsync()
    {
        if (IsClosing) return;

        IsClosing = true;
        Step2CancelCommand.Execute(null);
        Step1CancelCommand.Execute(null);
        Step0CancelCommand.Execute(null);

        try
        {
            IAsyncRelayCommand[] commands = [LoadedCommand, AddStageMapTemplateCommand, GotoStageMapDocumentSelectedItemPositionCommand, Step0Command, Step1Command, Step2Command];
            await Task.WhenAll(commands.Select(t => t.ExecutionTask).OfType<Task>()).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RTA 等待任务退出失败");
        }

        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await Task.Run(() => cacheProvider.Set(Cache, cancellationTokenSource.Token)).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RTA 保存缓存失败");
        }

        IsReady = false;
        IsCloseReady = true;
        CloseView(null);
    }

    private Task<bool> InvokeAsync(int stepIndex, Func<Task<bool>> func, bool isSilent, CancellationToken cancellationToken)
    {
        return RunOperationAsync(Steps[stepIndex], func, isSilent, cancellationToken);
    }

    private async Task<bool> RunOperationAsync(string title, Func<Task<bool>> func, bool isSilent, CancellationToken cancellationToken)
    {
        if (CanRun() == false) return false;

        IsBusy = true;
        HtmlLogUniqueId = Guid.NewGuid();
        var isSuccess = false;

        try
        {
            logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Cache.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            isSuccess = await Task.Run(func, cancellationToken).ConfigureAwait(true);
            if (isSilent == false && IsClosing == false)
                dialogWindowProvider.ShowDialog($"{Name}: {title} {(isSuccess ? "Success" : "Failed")}", DialogButtonsEnum.OK, isSuccess ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return isSuccess;
        }
        catch (OperationCanceledException)
        {
            logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            return false;
        }
        catch (Exception ex)
        {
            logger.LogHtmlError(ex, title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            if (IsClosing == false) dialogWindowProvider.ShowDialog($"{Name}: {title} Failed\n{ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return false;
        }
        finally
        {
            try
            {
                logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{title.Replace(" ", string.Empty)}_{(isSuccess ? "OK" : "Failed")}"));
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}