using System.IO;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;
using Local.SQL.Cache.Providers.Extensions;
using Local.SQL.Cache.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Optics;

public abstract partial class AbstractOpticsGrabbingImageWindowViewModel<TCache> : ViewModelBase where TCache : OpticsGrabbingImageCache, new()
{
    protected readonly IDialogWindowProvider DialogWindowProvider;
    protected readonly ILogger<AbstractOpticsGrabbingImageWindowViewModel<TCache>> Logger;
    protected readonly MicroscopeViewModel MicroscopeViewModel;
    protected readonly AfViewModel AFViewModel;
    protected readonly LaserViewModel LaserViewModel;
    protected readonly StageViewModel StageViewModel;
    protected readonly CIBViewModel CIBViewModel;
    protected readonly ICacheProvider CacheProvider;
    protected readonly ApplicationSetting ApplicationSetting;

    public string AODWaveformDirectoryPath => Path.Combine(ApplicationSetting.AppHomeDirectory, "AODWaveform", GetType().Name, DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public virtual string Name => "Grabbing Dark Image";

    public Guid HtmlLogUniqueId { get; set; }

    [ObservableProperty]
    private ApplicationCookie _applicationCookie = new();

    public abstract TCache Cache { get; set; }

    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<DarkFieldRawScanImageDTO>> _results = [];

    protected AbstractOpticsGrabbingImageWindowViewModel()
    {
        DialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();
        Logger = (ILogger<AbstractOpticsGrabbingImageWindowViewModel<TCache>>)HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
        MicroscopeViewModel = HostApplication.GetRequiredService<MicroscopeViewModel>();
        AFViewModel = HostApplication.GetRequiredService<AfViewModel>();
        LaserViewModel = HostApplication.GetRequiredService<LaserViewModel>();
        StageViewModel = HostApplication.GetRequiredService<StageViewModel>();
        CIBViewModel = HostApplication.GetRequiredService<CIBViewModel>();
        CacheProvider = HostApplication.GetRequiredService<ICacheProvider>();
        ApplicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
        ApplicationSetting = HostApplication.GetRequiredService<IOptions<ApplicationSetting>>().Value;
    }

    [RelayCommand]
    protected virtual async Task LoadedAsync() => await Task.Run(() =>
    {
        Results = [];

        Cache = CacheProvider.GetOrDefault<TCache>();
    }).ConfigureAwait(false);

    protected virtual bool InvokeDarkFieldRawScanImageDTO(DarkFieldRawScanImageDTO darkFieldRawScanImage)
    {
        Logger.LogHtmlInformation($"{darkFieldRawScanImage.CIBInformation}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
        {
            Result = new HtmlBullet(darkFieldRawScanImage.ToHtmlAnonymous())
        }), HtmlLogUniqueId.LoggingHtml());

        return true;
    }

    [RelayCommand]
    private void ImportAODWaveformParams()
    {
        try
        {
            var isSuccess = true;

            var stringBuilder = new StringBuilder();

            var prescanCache = CacheProvider.GetOrDefault<PrescanAODWaveformElectrodeOffsetCache>();

            var prescanResult = prescanCache.Results.FirstOrDefault(t => t.GeneratePrescanAODWaveformParam.ProductivityInformation.Equals(Cache.ProductivityInformation));

            if (prescanResult is null)
            {
                stringBuilder.AppendLine("Warning: Prescan AOD Waveform Param No matched found for current Productivity Information!");
                isSuccess = false;
            }
            else
            {
                Cache.GeneratePrescanAODWaveformParam = prescanResult.GeneratePrescanAODWaveformParam;
                stringBuilder.AppendLine("Ok: Prescan AOD Waveform Param Import Success!");
            }

            var chirpCache = CacheProvider.GetOrDefault<ChirpAODWaveformElectrodeOffsetCache>();

            var chirpResult = chirpCache.Results.FirstOrDefault(t => t.GenerateChirpAODWaveformParam.ProductivityInformation.Equals(Cache.ProductivityInformation));

            if (chirpResult is null)
            {
                stringBuilder.AppendLine("Warning: Chirp AOD Waveform Param No matched found for current Productivity Information!");
                isSuccess = false;
            }
            else
            {
                Cache.GenerateChirpAODWaveformParam = chirpResult.GenerateChirpAODWaveformParam;
                stringBuilder.AppendLine("Ok: Chirp AOD Waveform Param Import Success!");
            }

            DialogWindowProvider.ShowDialog(stringBuilder.ToString(), DialogButtonsEnum.OK, isSuccess ? DialogIconEnum.Information : DialogIconEnum.Warning);
        }
        catch (Exception ex)
        {
            DialogWindowProvider.ShowDialog($"""
                                             {Name}: Import Parameters Failed
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            Logger.LogError(ex, "{@Name}: Import Parameters Failed", Name);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ChangedPrescanAODWaveformProfilesAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            try
            {
                ClearPrescanAODWaveformProfiles();

                if (Cache.IsGenerateAODWaveform)
                {
                    Cache.GeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
                    Cache.GeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

                    var prescanAODWaveformResult = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);

                    Cache.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(prescanAODWaveformResult);
                    Cache.PrescanAODWaveformResultFilePath = prescanAODWaveformResult.FilePath;
                }
                else
                {
                    var dialog = DialogWindowProvider.TryShowSelectFilePathDialog(AODWaveformGenerator.PrescanAODWaveformFileExtension, out var filePath);
                    if (dialog == false) return;

                    Cache.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(filePath);
                    Cache.PrescanAODWaveformResultFilePath = filePath;
                }

                DialogWindowProvider.ShowDialog("Changed Prescan AOD Waveform Profiles Success");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    DialogWindowProvider.ShowDialog("Changed Chirp AOD Waveform Profiles Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                    return;
                }

                Logger.LogError(ex, "{@Name}: Changed Prescan AOD Waveform Profiles Failed", Name);
                DialogWindowProvider.ShowDialog($"""
                                                 {Name}: Changed Prescan AOD Waveform Profiles Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand]
    private void ClearPrescanAODWaveformProfiles()
    {
        Cache.PrescanAODWaveformProfiles = [];
        Cache.PrescanAODWaveformResultFilePath = string.Empty;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ChangedChirpAODWaveformProfilesAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            try
            {
                ClearChirpAODWaveformProfiles();

                if (Cache.IsGenerateAODWaveform)
                {
                    Cache.GenerateChirpAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
                    Cache.GenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

                    var chirpAODWaveformResult = AODWaveformGenerator.GenerateChirpAODWaveform(Cache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);

                    Cache.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(chirpAODWaveformResult);
                    Cache.ChirpAODWaveformResultFilePath = chirpAODWaveformResult.FilePath;
                }
                else
                {
                    var dialog = DialogWindowProvider.TryShowSelectFilePathDialog(AODWaveformGenerator.ChirpAODWaveformFileExtension, out var filePath);
                    if (dialog == false) return;

                    Cache.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(filePath);
                    Cache.ChirpAODWaveformResultFilePath = filePath;
                }

                DialogWindowProvider.ShowDialog("Changed Chirp AOD Waveform Profiles Success");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    DialogWindowProvider.ShowDialog("Changed Chirp AOD Waveform Profiles Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                    return;
                }

                Logger.LogError(ex, "{@Name}: Changed Chirp AOD Waveform Profiles Failed", Name);
                DialogWindowProvider.ShowDialog($"""
                                                 {Name}: Changed Chirp AOD Waveform Profiles Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand]
    private void ClearChirpAODWaveformProfiles()
    {
        Cache.ChirpAODWaveformProfiles = [];
        Cache.ChirpAODWaveformResultFilePath = string.Empty;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> GetPMTImagesByWidthAsync(CancellationToken cancellationToken) => await InvokeGetPMTImagesAsync(OpticsGrabbingImageCache.OpticsGrabbingImageTypeEnum.Width, async startPosition =>
    {
        if (Cache.IsAutoFocus == false)
        {
            AFViewModel.ToggleBrightFieldEnable(false);
            AFViewModel.SetSensorEcsValue(Cache.ECS);
        }

        var boolList = new List<bool>();
        foreach (var cibInformations in Cache.CIBInformations
                     .GroupBy(t => t.PMTId)
                     .OrderBy(t => t.Key)
                     .Select(gg => gg.OrderByDescending(t => t).ToArray()))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentStartPosition = CIBViewModel.GetCIBInformationPosition(
                Cache.StageCoordinateSystemEnum,
                Cache.ProductivityInformation,
                cibInformations[0],
                startPosition,
                MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            var darkFieldImages = await CIBViewModel.GetPMTImagesAsync(
                Cache.ProductivityInformation,
                Cache.StageCoordinateSystemEnum,
                currentStartPosition,
                Cache.ImageWidth,
                cibInformations,
                (true, null),
                (false, Cache.CIBConfiguration),
                (true, null),
                true,
                cancellationToken,
                isForward: Cache.IsForward,
                isAutoFocus: Cache.IsAutoFocus,
                isKeepRawImageCIBProfileModeEnum: Cache.IsKeepRawImageCIBProfileModeEnum).ConfigureAwait(false);

            foreach (var darkFieldImage in darkFieldImages)
            {
                using var _ = darkFieldImage;

                boolList.Add(InvokeDarkFieldRawScanImageDTO(darkFieldImage));
            }

            Results = [..Results, darkFieldImages];
        }

        return boolList.All(t => t);
    }, false).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> GetPMTImagesByPTPAsync(CancellationToken cancellationToken) => await InvokeGetPMTImagesAsync(OpticsGrabbingImageCache.OpticsGrabbingImageTypeEnum.PTP, async startPosition =>
    {
        if (Cache.IsAutoFocus == false)
        {
            AFViewModel.ToggleBrightFieldEnable(false);
            AFViewModel.SetSensorEcsValue(Cache.ECS);
        }

        var boolList = new List<bool>();
        foreach (var cibInformations in Cache.CIBInformations
                     .GroupBy(t => t.PMTId)
                     .OrderBy(t => t.Key)
                     .Select(gg => gg.OrderByDescending(t => t).ToArray()))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentStartPosition = CIBViewModel.GetCIBInformationPosition(
                Cache.StageCoordinateSystemEnum,
                Cache.ProductivityInformation,
                cibInformations[0],
                startPosition,
                MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            var currentStopPosition = currentStartPosition + new Vector(Cache.ScanLength, 0);

            var darkFieldRawScanImages = await CIBViewModel.GetPMTImagesAsync(
                Cache.ProductivityInformation,
                Cache.StageCoordinateSystemEnum,
                currentStartPosition,
                currentStopPosition,
                cibInformations,
                (true, null),
                (false, Cache.CIBConfiguration),
                (true, null),
                true,
                cancellationToken,
                isForward: Cache.IsForward,
                isAutoFocus: Cache.IsAutoFocus,
                isKeepRawImageCIBProfileModeEnum: Cache.IsKeepRawImageCIBProfileModeEnum).ConfigureAwait(false);

            boolList.AddRange(darkFieldRawScanImages.Select(InvokeDarkFieldRawScanImageDTO));

            Results = [..Results, darkFieldRawScanImages];
        }

        return boolList.All(t => t);
    }, false).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> GetPMTImagesByPEGAsync(CancellationToken cancellationToken) => await InvokeGetPMTImagesAsync(OpticsGrabbingImageCache.OpticsGrabbingImageTypeEnum.PEG, async startPosition =>
    {
        if (Cache.IsAutoFocus == false)
        {
            AFViewModel.ToggleBrightFieldEnable(false);
            AFViewModel.SetSensorEcsValue(Cache.ECS);
        }

        var boolList = new List<bool>();
        foreach (var cibInformations in Cache.CIBInformations
                     .GroupBy(t => t.PMTId)
                     .OrderBy(t => t.Key)
                     .Select(gg => gg.OrderByDescending(t => t).ToArray()))
        {
            cancellationToken.ThrowIfCancellationRequested();

            Guard.IsEqualTo(cibInformations.Length, 1);

            var currentStartPosition = CIBViewModel.GetCIBInformationPosition(
                Cache.StageCoordinateSystemEnum,
                Cache.ProductivityInformation,
                cibInformations[0],
                startPosition,
                MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            var positions = Enumerable.Range(0, Cache.ColumnCount).Select(t => currentStartPosition + new Vector(t * Cache.ColumnWidth, 0)).ToArray();

            if (Cache.IsForward == false) positions = [.. positions.AsEnumerable().Reverse()];

            var darkFieldImages = await CIBViewModel.GetPMTImagesAsync(
                Cache.ProductivityInformation,
                Cache.StageCoordinateSystemEnum,
                positions,
                Cache.ImageWidth,
                cibInformations[0],
                (true, null),
                (false, Cache.CIBConfiguration),
                (true, null),
                true,
                cancellationToken,
                isAutoFocus: Cache.IsAutoFocus,
                isKeepRawImageCIBProfileModeEnum: Cache.IsKeepRawImageCIBProfileModeEnum).ConfigureAwait(false);

            foreach (var darkFieldImage in darkFieldImages)
            {
                using var _ = darkFieldImage;

                boolList.Add(InvokeDarkFieldRawScanImageDTO(darkFieldImage));
            }

            Results = [..Results, darkFieldImages];
        }

        return boolList.All(t => t);
    }, false).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> GetPMTImagesByXZSyncAsync(bool isNotSilent, CancellationToken cancellationToken) => await InvokeGetPMTImagesAsync(OpticsGrabbingImageCache.OpticsGrabbingImageTypeEnum.XZSync, async startPosition =>
    {
        var boolList = new List<bool>();
        foreach (var cibInformations in Cache.CIBInformations
                     .GroupBy(t => t.PMTId)
                     .OrderBy(t => t.Key)
                     .Select(gg => gg.OrderByDescending(t => t).ToArray()))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentStartPosition = CIBViewModel.GetCIBInformationPosition(
                Cache.StageCoordinateSystemEnum,
                Cache.ProductivityInformation,
                cibInformations[0],
                startPosition,
                MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            var currentStopPosition = currentStartPosition + new Vector(Cache.ScanLength, 0);

            var startECS = Cache.CenterECS - Cache.RangeECS;
            var stopECS = Cache.CenterECS + Cache.RangeECS;
            var darkFieldImages = await CIBViewModel.GetPMTImagesAsync(
                Cache.ProductivityInformation,
                Cache.StageCoordinateSystemEnum,
                currentStartPosition,
                currentStopPosition,
                startECS,
                stopECS,
                cibInformations,
                (true, null),
                (false, Cache.CIBConfiguration),
                (true, null),
                true,
                cancellationToken,
                isForward: Cache.IsForward,
                isKeepRawImageCIBProfileModeEnum: Cache.IsKeepRawImageCIBProfileModeEnum).ConfigureAwait(false);

            foreach (var darkFieldImage in darkFieldImages)
            {
                using var _ = darkFieldImage;

                boolList.Add(InvokeDarkFieldRawScanImageDTO(darkFieldImage));
            }

            Results = [..Results, darkFieldImages];
        }

        return boolList.All(t => t);
    }, isNotSilent).ConfigureAwait(false);

    protected async Task<bool> InvokeGetPMTImagesAsync(
        OpticsGrabbingImageCache.OpticsGrabbingImageTypeEnum opticsGrabbingImageTypeEnum,
        Func<Point, Task<bool>> func,
        bool isSilent)
    {
        return await Task.Run(async () =>
        {
            var title = $"Grabbing Image By {opticsGrabbingImageTypeEnum}";
            var isSuccess = false;
            try
            {
                HtmlLogUniqueId = isSilent ? HtmlLogUniqueId : Guid.NewGuid();

                Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var startPosition = Cache.StageCoordinateSystemEnum switch
                {
                    StageCoordinateSystemEnum.Bright => StageViewModel.GetBrightFieldStagePosition(),
                    StageCoordinateSystemEnum.Dark => StageViewModel.GetDarkFieldStagePosition(),
                    StageCoordinateSystemEnum.Machine => StageViewModel.GetMachineStagePosition(),
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
                };

                var htmlQuote = new HtmlQuote(Cache.ToHtmlAnonymous(opticsGrabbingImageTypeEnum));
                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    Base = htmlQuote,
                    startPosition
                }), HtmlLogUniqueId.LoggingHtml());

                if (isSilent == false)
                {
                    if (DialogWindowProvider.TryShowDialog($"""
                                                            {title}, Please confirm Param.
                                                            {htmlQuote.ToViewString()}
                                                            """, out var dialogResultEnum, DialogButtonsEnum.OKCancel) == false || dialogResultEnum != DialogResultEnum.OK) return false;
                }

                if (Cache.PrescanAODWaveformProfiles.Count > 0)
                {
                    foreach (var prescanAODWaveformProfile in Cache.PrescanAODWaveformProfiles) prescanAODWaveformProfile.ApplyCoefficient(Cache.LaserLightInformation.Coefficient);

                    LaserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, Cache.PrescanAODWaveformProfiles);

                    Logger.LogHtmlInformation("Prescan AOD Waveform", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        GeneratePrescanAODWaveformParam = Cache.IsGenerateAODWaveform ? new HtmlQuote(Cache.GeneratePrescanAODWaveformParam.ToHtmlAnonymous()) : new HtmlQuote(new { Cache.PrescanAODWaveformResultFilePath }),
                        Cache.PrescanAODWaveformResultFilePath,
                        PrescanAODWaveformProfiles = new HtmlTable([.. Cache.PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
                    }), HtmlLogUniqueId.LoggingHtml());
                }
                else
                {
                    LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, Cache.LaserLightInformation.Coefficient);

                    Logger.LogHtmlInformation("Prescan AOD Waveform", HtmlHeaderLevelEnum.Header4, new HtmlComment("Default"), HtmlLogUniqueId.LoggingHtml());
                }

                if (Cache.ChirpAODWaveformProfiles.Count > 0)
                {
                    LaserViewModel.SetChirpAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, Cache.ChirpAODWaveformProfiles);

                    Logger.LogHtmlInformation("Chirp AOD Waveform", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        GenerateChirpAODWaveformParam = Cache.IsGenerateAODWaveform ? new HtmlQuote(Cache.GenerateChirpAODWaveformParam.ToHtmlAnonymous()) : new HtmlQuote(new { Cache.PrescanAODWaveformResultFilePath }),
                        Cache.ChirpAODWaveformResultFilePath,
                        ChirpAODWaveformProfiles = new HtmlTable([.. Cache.ChirpAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
                    }), HtmlLogUniqueId.LoggingHtml());
                }
                else
                {
                    LaserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);

                    Logger.LogHtmlInformation("Chirp AOD Waveform", HtmlHeaderLevelEnum.Header4, new HtmlComment("Default"), HtmlLogUniqueId.LoggingHtml());
                }

                try
                {
                    Move();

                    Logger.LogHtmlInformation("Grabbing Image", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    Results = [];

                    isSuccess = await func(startPosition).ConfigureAwait(false);
                }
                finally
                {
                    Move();
                }

                void Move()
                {
                    switch (Cache.StageCoordinateSystemEnum)
                    {
                        case StageCoordinateSystemEnum.Bright:
                            StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(startPosition, Cache.CalChipSiteModelEnum);

                            break;

                        case StageCoordinateSystemEnum.Dark:
                            StageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(startPosition, Cache.CalChipSiteModelEnum);

                            break;

                        case StageCoordinateSystemEnum.Machine:
                            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(startPosition, Cache.CalChipSiteModelEnum);

                            break;

                        default:
                            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(Cache.StageCoordinateSystemEnum));

                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (isSilent) throw;

                if (ex is OperationCanceledException)
                {
                    DialogWindowProvider.ShowDialog($"{Name}: {title} Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    Logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                }
                else
                {
                    DialogWindowProvider.ShowDialog($"""
                                                     {Name}: {title} Failed
                                                     {ex.Message}
                                                     """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    Logger.LogHtmlError(ex, Name, HtmlHeaderLevelEnum.Header4, new HtmlComment($"{title} Failed"), HtmlLogUniqueId.LoggingHtml());
                }
            }
            finally
            {
                if (isSilent == false) Logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{title.Replace(" ", string.Empty)}_{(isSuccess ? "OK" : "Failed")}"));
            }

            if (isSuccess)
            {
                if (isSilent == false) DialogWindowProvider.ShowDialog($"{Name}: {title} Success");
            }
            else
                DialogWindowProvider.ShowDialog($"{Name}: {title} Error", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return isSuccess;
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private void Close()
    {
        try
        {
            Results = [];

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            CacheProvider.Set(Cache, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Failed to save cache", Name);
        }

        CloseView(true);
    }
}

[IOCAppService(ServiceType = typeof(OpticsGrabbingImageWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class OpticsGrabbingImageWindowViewModel : AbstractOpticsGrabbingImageWindowViewModel<OpticsGrabbingImageCache>
{
    [DefaultCache]
    public override OpticsGrabbingImageCache Cache
    {
        get;
        set => SetProperty(ref field, value);
    } = new();
}