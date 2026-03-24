using System.IO;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.ViewModels.Chuck;
using CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;
using Local.SQL.Cache.Providers.Extensions;
using Local.SQL.Cache.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Text;
using Core.Utilities;
using Microsoft.Extensions.Options;
using Net.Utilities.Models;
using Net.Utilities.WPF.MVVM;

namespace CugaCalibration.ViewModels.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(GrabbingDarkImageWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class GrabbingDarkImageWindowViewModel : ViewModelBase
{
    protected readonly IDialogWindowProvider DialogWindowProvider;
    protected readonly ILogger<ChuckPrealignerCalibrationViewModel> Logger;
    protected readonly MicroscopeViewModel MicroscopeViewModel;
    protected readonly AfViewModel AFViewModel;
    protected readonly LaserViewModel LaserViewModel;
    protected readonly StageViewModel StageViewModel;
    protected readonly CIBViewModel CIBViewModel;
    protected readonly ICacheProvider CacheProvider;
    protected readonly ApplicationSetting ApplicationSetting;

    public string AODWaveformDirectoryPath => Path.Combine(ApplicationSetting.AppHomeDirectory, "AODWaveform", GetType().Name, DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public virtual string Name => "Grabbing Dark Image";

    [ObservableProperty]
    private ApplicationCookie _applicationCookie = new();

    [DefaultCache]
    [ObservableProperty]
    private GrabbingDarkImageWindowCache _cache = new();

    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<DarkFieldRawScanImageDTO>> _results = [];

    public GrabbingDarkImageWindowViewModel()
    {
        DialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();
        Logger = (ILogger<ChuckPrealignerCalibrationViewModel>)HostApplication.GetRequiredService(typeof(ILogger<>).MakeGenericType(GetType()));
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

        Cache = CacheProvider.GetOrDefault<GrabbingDarkImageWindowCache>();
    });

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
                                             Import Parameters Failed
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            Logger.LogError(ex, "Import Parameters Failed");
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

                Logger.LogError(ex, "Changed Prescan AOD Waveform Profiles Failed");
                DialogWindowProvider.ShowDialog($"""
                                                 Changed Prescan AOD Waveform Profiles Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        }, cancellationToken);
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

                Logger.LogError(ex, "Changed Chirp AOD Waveform Profiles Failed");
                DialogWindowProvider.ShowDialog($"""
                                                 Changed Chirp AOD Waveform Profiles Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        }, cancellationToken);
    }

    [RelayCommand]
    private void ClearChirpAODWaveformProfiles()
    {
        Cache.ChirpAODWaveformProfiles = [];
        Cache.ChirpAODWaveformResultFilePath = string.Empty;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task GetPMTImagesByWidthAsync(CancellationToken cancellationToken) => InvokeGetPMTImagesAsync("Width", async startPosition =>
    {
        if (Cache.IsAutoFocus == false)
        {
            AFViewModel.ToggleBrightFieldEnable(false);
            AFViewModel.SetSensorEcsValue(Cache.ECS);
        }

        var resultList = new List<IReadOnlyList<DarkFieldRawScanImageDTO>>();
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
                isKeepRawImageCIBProfileModeEnum: Cache.IsKeepRawImageCIBProfileModeEnum);

            foreach (var darkFieldImage in darkFieldImages)
            {
                using var _1 = darkFieldImage;
            }

            resultList.Add(darkFieldImages);
        }

        Results = resultList;
    }, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task GetPMTImagesByPTPAsync(CancellationToken cancellationToken) => InvokeGetPMTImagesAsync("PTP", async startPosition =>
    {
        if (Cache.IsAutoFocus == false)
        {
            AFViewModel.ToggleBrightFieldEnable(false);
            AFViewModel.SetSensorEcsValue(Cache.ECS);
        }

        var resultList = new List<IReadOnlyList<DarkFieldRawScanImageDTO>>();
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
                isKeepRawImageCIBProfileModeEnum: Cache.IsKeepRawImageCIBProfileModeEnum);

            resultList.Add(darkFieldRawScanImages);
        }

        Results = resultList;
    }, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task GetPMTImagesByPEGAsync(CancellationToken cancellationToken) => InvokeGetPMTImagesAsync("PEG", async startPosition =>
    {
        if (Cache.IsAutoFocus == false)
        {
            AFViewModel.ToggleBrightFieldEnable(false);
            AFViewModel.SetSensorEcsValue(Cache.ECS);
        }

        var resultList = new List<IReadOnlyList<DarkFieldRawScanImageDTO>>();
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
                isKeepRawImageCIBProfileModeEnum: Cache.IsKeepRawImageCIBProfileModeEnum);

            foreach (var darkFieldImage in darkFieldImages)
            {
                using var _ = darkFieldImage;
            }

            resultList.Add(darkFieldImages);
        }

        Results = resultList;
    }, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task GetPMTImagesByXZSyncAsync(CancellationToken cancellationToken) => InvokeGetPMTImagesAsync("XZSync", async startPosition =>
    {
        var resultList = new List<IReadOnlyList<DarkFieldRawScanImageDTO>>();
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
                isKeepRawImageCIBProfileModeEnum: Cache.IsKeepRawImageCIBProfileModeEnum);

            foreach (var darkFieldImage in darkFieldImages)
            {
                using var _ = darkFieldImage;
            }

            resultList.Add(darkFieldImages);
        }

        Results = resultList;
    }, cancellationToken);

    protected async Task InvokeGetPMTImagesAsync(
        string modeName,
        Func<Point, Task> func,
        CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                if (DialogWindowProvider.TryShowDialog($"Grabbing Image By {modeName}, Please confirm Param.", out var dialogResultEnum, DialogButtonsEnum.OKCancel) == false || dialogResultEnum != DialogResultEnum.OK) return;

                if (Cache.PrescanAODWaveformProfiles.Count > 0)
                {
                    foreach (var prescanAODWaveformProfile in Cache.PrescanAODWaveformProfiles) prescanAODWaveformProfile.ApplyCoefficient(Cache.LaserLightInformation.Coefficient);

                    LaserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, Cache.PrescanAODWaveformProfiles);
                }
                else LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, Cache.LaserLightInformation.Coefficient);

                if (Cache.ChirpAODWaveformProfiles.Count > 0) LaserViewModel.SetChirpAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, Cache.ChirpAODWaveformProfiles);
                else LaserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);

                var startPosition = Cache.StageCoordinateSystemEnum switch
                {
                    StageCoordinateSystemEnum.Bright => StageViewModel.GetBrightFieldStagePosition(),
                    StageCoordinateSystemEnum.Dark => StageViewModel.GetDarkFieldStagePosition(),
                    StageCoordinateSystemEnum.Machine => StageViewModel.GetMachineStagePosition(),
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
                };

                try
                {
                    Results = [];

                    await func(startPosition);
                }
                finally
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

                DialogWindowProvider.ShowDialog($"Grabbing Image By {modeName} Completed.");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    DialogWindowProvider.ShowDialog($"Get PMT Images By {modeName} Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                    return;
                }

                DialogWindowProvider.ShowDialog($"""
                                                 Get PMT Images By {modeName} Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogError(ex, "Get PMT Images By {ModeName}", modeName);
            }
        }, cancellationToken).ConfigureAwait(false);
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
            Logger.LogError(ex, "Failed to save cache");
        }

        CloseView(true);
    }
}