using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Setting;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;
using Core.Models.Enums.Stage;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

[IOCAppService(ServiceType = typeof(StageMapWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class StageMapWindowViewModel(
    ICacheProvider cacheProvider,
    IApplicationCookieService applicationCookieCacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    IOptions<ApplicationSetting> options,
    ApplicationCookie applicationCookie,
    CalibrationSetting calibrationSetting,
    StageMapCommands stageMapCommands,
    ILogger<StageMapWindowViewModel> logger) : ViewModelBase
{
    public string Name { get; } = "StageMap";

    public CalibrationSetting CalibrationSetting { get; } = calibrationSetting;

    public ApplicationCookie ApplicationCookie { get; } = applicationCookie;

    public StageMapCommands StageMapCommands { get; } = stageMapCommands;

    public string ImageFileDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", nameof(StageMapWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat, CultureInfo.InvariantCulture));

    public string TemplateFileDirectory => Path.Combine(options.Value.AppHomeDirectory, "Template", nameof(StageMapWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat, CultureInfo.InvariantCulture));

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

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await Task.Run(() =>
        {
            Cache = cacheProvider.GetOrDefault<StageMapCache>();

            MicroscopeCalChip = applicationCookieCacheProvider.GetCalibration<MicroscopeCalChipDTO>();

            StageMapCommands.Logger = logger;
            StageMapCommands.ImageFileDirectory = ImageFileDirectory;
            StageMapCommands.TemplateFileDirectory = TemplateFileDirectory;
            StageMapCommands.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
            StageMapCommands.MicroscopeCalChip = MicroscopeCalChip;
            StageMapCommands.Cache = Cache.ChuckStageMapCache;
            StageMapCommands.ChuckStageMap = Cache.ChuckStageMap;

            Cache.ChuckStageMap.BFStageMap.Refresh();
            Cache.ChuckStageMap.DFStageMap.Refresh();
            foreach (var cacheRepeatStageMap in Cache.ChuckStageMap.DFRepeatStageMaps) cacheRepeatStageMap.Refresh();
            Cache.ChuckStageMap.BFVerifyStageMap.Refresh();
            Cache.ChuckStageMap.DFVerifyStageMap.Refresh();
        }).ConfigureAwait(true);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(0, async () =>
    {
        await StageMapCommands.AlignmentThenApplyTemplatesAsync(HtmlLogUniqueId, cancellationToken).ConfigureAwait(false);

        return true;
    }, isSilent, cancellationToken).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(1, async () =>
    {
        await StageMapCommands.GenerateStageMapDocumentAsync(HtmlLogUniqueId, cancellationToken).ConfigureAwait(false);

        return true;
    }, isSilent, cancellationToken).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(2, async () =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        await StageMapCommands.CalibrateAsync(true, HtmlLogUniqueId, cancellationToken).ConfigureAwait(false);

        return true;
    }, isSilent, cancellationToken).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step3Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(3, async () =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await StageMapCommands.DFRepeatCalibrateAsync(HtmlLogUniqueId, cancellationToken).ConfigureAwait(false);
    }, isSilent, cancellationToken).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step4Async(bool isSilent, CancellationToken cancellationToken) => await InvokeAsync(4, async () =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        await StageMapCommands.VerifyAsync(
            StageMapCommands.StageMap,
            HtmlLogUniqueId,
            cancellationToken,
            isDownloadStageMap: true,
            isInterpolateErrorsAdaptTo: true).ConfigureAwait(false);

        return true;
    }, isSilent, cancellationToken).ConfigureAwait(false);

    [RelayCommand]
    private void Close()
    {
        try
        {
            StageMapCommands.AddStageMapTemplateCancelCommand.Execute(null);

            Step4CancelCommand.Execute(null);
            Step3CancelCommand.Execute(null);
            Step2CancelCommand.Execute(null);
            Step1CancelCommand.Execute(null);
            Step0CancelCommand.Execute(null);

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            Cache.Id = 0;
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
            logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Cache.ToHtmlAnonymous(StageMapCommands.IsDark)), HtmlLogUniqueId.LoggingHtml());

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