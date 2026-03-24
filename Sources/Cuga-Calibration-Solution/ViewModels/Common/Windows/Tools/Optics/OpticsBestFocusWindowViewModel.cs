using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Microscope.CalChip;
using Core.Services.Interfaces;
using Core.Utilities.SourceGenerators.Attributes;
using Local.SQL.Cache.Providers.Extensions;
using Local.SQL.Cache.Providers.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Optics;

[IOCAppService(ServiceType = typeof(OpticsBestFocusWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsBestFocusWindowViewModel(
    [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
    ICacheProvider recipeCacheProvider,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ISynchronizationContextProvider contextProvider) : OpticsGrabbingImageWindowViewModel
{
    [DefaultCache]
    [ObservableProperty]
    private OpticsBestFocusCache _cache = new();

    [ObservableProperty]
    private IReadOnlyList<OpticsBestFocusResult> _results = [];

    [ObservableProperty]
    private MicroscopeCalChipCache _microscopeCalChipCache = new();

    public override string Name => "Best Focus";

    public Guid HtmlLogUniqueId { get; private set; }

    public IReadOnlyList<string> Steps { get; } =
    [
        "Step 1 Alignment",
        "Step 2 Mark",
        "Step 3 Best Focus"
    ];

    protected override async Task LoadedAsync() => await Task.Run(() =>
    {
        Results = [];
        MicroscopeCalChipCache = recipeCacheProvider.GetOrDefault<MicroscopeCalChipCache>();
        Cache = CacheProvider.GetOrDefault<OpticsBestFocusCache>();
    });

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0Async(bool isNotSilent, CancellationToken cancellationToken)
    {
        return await InvokeAsync(0, () =>
        {
            var alignmentResult = StageViewModel.Alignment(
                MicroscopeCalChipCache.LowSite1,
                MicroscopeCalChipCache.LowSite2,
                MicroscopeCalChipCache.HighSite1,
                MicroscopeCalChipCache.HighSite2,
                MicroscopeCalChipCache.LowMicroscopeLensInformation,
                MicroscopeCalChipCache.HighMicroscopeLensInformation,
                MicroscopeCalChipCache.AlgorithmWaferTypeEnum,
                CalChipSiteModelEnum.DswModel);

            StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition((alignmentResult.MarkPoint1 + (Vector)alignmentResult.MarkPoint2) / 2d));

            Cache.AlignmentResult = alignmentResult;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                AlignmentResult = new HtmlQuote(Cache.AlignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return Task.FromResult(true);
        }, isNotSilent).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1Async(bool isNotSilent, CancellationToken cancellationToken)
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
        }, isNotSilent).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2Async(bool isNotSilent, CancellationToken cancellationToken)
    {
        return await InvokeAsync(2, async () =>
        {
            Results = [];

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Cache.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            if (Cache.PrescanAODWaveformProfiles.Count > 0)
            {
                Logger.LogHtmlInformation("Prescan AOD Waveform", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    GeneratePrescanAODWaveformParam = Cache.IsGenerateAODWaveform ? new HtmlQuote(Cache.GeneratePrescanAODWaveformParam.ToHtmlAnonymous()) : new HtmlQuote(new { Cache.PrescanAODWaveformResultFilePath }),
                    Cache.PrescanAODWaveformResultFilePath,
                    PrescanAODWaveformProfiles = new HtmlTable([.. Cache.PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
                }), HtmlLogUniqueId.LoggingHtml());
            }

            if (Cache.ChirpAODWaveformProfiles.Count > 0)
            {
                Logger.LogHtmlInformation("Chirp AOD Waveform", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    GenerateChirpAODWaveformParam = Cache.IsGenerateAODWaveform ? new HtmlQuote(Cache.GenerateChirpAODWaveformParam.ToHtmlAnonymous()) : new HtmlQuote(new { Cache.PrescanAODWaveformResultFilePath }),
                    Cache.ChirpAODWaveformResultFilePath,
                    ChirpAODWaveformProfiles = new HtmlTable([.. Cache.ChirpAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
                }), HtmlLogUniqueId.LoggingHtml());
            }

            MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
            var dswBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.DSWFindBFMachinePosition);
            StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(dswBFPosition);

#if NET
            await
#endif
            using var _ = cancellationToken.Register(() =>
            {
                if (GetPMTImagesByXZSyncCommand.CanBeCanceled) GetPMTImagesByXZSyncCommand.Cancel();
            });

            await GetPMTImagesByXZSyncCommand.ExecuteAsync(null);

            Logger.LogHtmlInformation("Best Focus", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            var startECS = Cache.CenterECS - Cache.RangeECS;
            var stopECS = Cache.CenterECS + Cache.RangeECS;

            var isSuccess = true;
            foreach (var darkFieldRawScanImages in base.Results)
            {
                foreach (var darkFieldRawScanImage in darkFieldRawScanImages)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var item = new OpticsBestFocusResult { DarkFieldRawScanImage = darkFieldRawScanImage };

                    try
                    {
                        var temp = darkFieldRawScanImage.Clone();
                        temp.IsKeepRawImageCIBProfileModeEnum = false;
                        using var image = temp.GetImage();

                        item.BestFocus = calibrationAlgorithmService.GetBestFocus(image, startECS, stopECS);
                        item.BestFocus.RawImageFilePath = darkFieldRawScanImage.RawImageFilePath;

                        Logger.LogHtmlInformation($"{darkFieldRawScanImage.CIBInformation} OK", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                        {
                            Result = new HtmlBullet(item.ToHtmlAnonymous())
                        }), HtmlLogUniqueId.LoggingHtml());
                    }
                    catch (Exception ex)
                    {
                        isSuccess = false;
                        item.BestFocus.RawImageFilePath = darkFieldRawScanImage.RawImageFilePath;
                        Logger.LogHtmlError($"{darkFieldRawScanImage.CIBInformation} Error", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                        {
                            Exception = ex,
                            Result = new HtmlBullet(item.ToHtmlAnonymous())
                        }), HtmlLogUniqueId.LoggingHtml());
                    }
                }
            }

            return isSuccess;
        }, isNotSilent).ConfigureAwait(false);
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

        var step0Task = Guard.IsAssignableToTypeAndReturn<Task<bool>>(Step0Command.ExecuteAsync( /* isNotSilent */ false));
        if (await step0Task == false) return;

        var newMarkPoint1 = Cache.AlignmentResult.MarkPoint1;
        var newMarkPoint2 = Cache.AlignmentResult.MarkPoint1;

        var offset = ((newMarkPoint1 - oldMarkPoint1) + (newMarkPoint2 - oldMarkPoint2)) / 2d;
        var oldDSWFindBFMachinePosition = Cache.DSWFindBFMachinePosition;
        Cache.DSWFindBFMachinePosition += offset;

        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            offset,
            oldDSWFindBFMachinePosition,
            Cache.DSWFindBFMachinePosition
        }), HtmlLogUniqueId.LoggingHtml());

        await Step2Command.ExecuteAsync( /* isNotSilent */ false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> ManualStep2Async(bool isNotSilent, CancellationToken cancellationToken)
    {
        return await InvokeAsync(2, () =>
        {
            Results = [];

            var openFileDialog = new OpenFileDialog
            {
                Title = "Select files",
                Multiselect = true,
                Filter = $"files (*.raw)|*.raw",
                DefaultExt = ".raw",
                CheckFileExists = true
            };

            bool? result = null;
            contextProvider.Send(() => result = openFileDialog.ShowDialog());
            if (result != true) return Task.FromResult(false);

            Logger.LogHtmlInformation("Best Focus", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            var isSuccess = true;
            foreach (var fileName in openFileDialog.FileNames)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var darkFieldRawScanImage = new DarkFieldRawScanImageDTO
                {
                    RawImageCIBProfileModeEnum = CIBProfileModeEnum.PMTLog,
                    RawImageFilePath = fileName,
                    IsKeepRawImageCIBProfileModeEnum = Cache.IsKeepRawImageCIBProfileModeEnum
                };

                var item = new OpticsBestFocusResult { DarkFieldRawScanImage = darkFieldRawScanImage };

                try
                {
                    var temp = darkFieldRawScanImage.Clone();
                    temp.IsKeepRawImageCIBProfileModeEnum = false;
                    using var image = temp.GetImage();
                    darkFieldRawScanImage.Size = (SizeI)image.GetSize();

                    item.BestFocus = calibrationAlgorithmService.GetBestFocus(image, 0d, 0d);
                    item.BestFocus.RawImageFilePath = darkFieldRawScanImage.RawImageFilePath;

                    Logger.LogHtmlInformation($"{darkFieldRawScanImage.RawImageFilePath} OK", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                    {
                        Result = new HtmlBullet(item.ToHtmlAnonymous())
                    }), HtmlLogUniqueId.LoggingHtml());
                }
                catch (Exception ex)
                {
                    isSuccess = false;
                    item.BestFocus.RawImageFilePath = darkFieldRawScanImage.RawImageFilePath;
                    Logger.LogHtmlError($"{darkFieldRawScanImage.RawImageFilePath} Error", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                    {
                        Exception = ex,
                        Result = new HtmlBullet(item.ToHtmlAnonymous())
                    }), HtmlLogUniqueId.LoggingHtml());
                }
            }

            return Task.FromResult(isSuccess);
        }, isNotSilent).ConfigureAwait(false);
    }

    private async Task<bool> InvokeAsync(
        int stepIndex,
        Func<Task<bool>> func,
        bool isNotSilent)
    {
        return await Task.Run(async () =>
        {
            var isInitHtmlLog = isNotSilent || stepIndex == 0;
            var isEndHtml = isNotSilent || stepIndex == Steps.Count - 1;

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
                if (ex is OperationCanceledException)
                {
                    if (isNotSilent == false) isEndHtml = true;

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
                    Logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{(isNotSilent ? Steps[stepIndex].Replace(" ", string.Empty) : "All")}_{(isSuccess ? "OK" : "Failed")}"));
            }

            if (isSuccess)
            {
                if (isEndHtml) DialogWindowProvider.ShowDialog($"{Name}: {(isNotSilent ? Steps[stepIndex] : "All")} Success");
            }
            else
                DialogWindowProvider.ShowDialog($"{Name}: {Steps[stepIndex]} Error", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return isSuccess;
        }).ConfigureAwait(false);
    }
}