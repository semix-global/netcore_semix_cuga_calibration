using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.ViewModels.Chuck;
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

namespace CugaCalibration.ViewModels.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(GrabbingDarkImageWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class GrabbingDarkImageWindowViewModel(
    IDialogWindowProvider dialogWindowProvider,
    ILogger<ChuckPrealignerCalibrationViewModel> logger,
    MicroscopeViewModel microscopeViewModel,
    AfViewModel afViewModel,
    LaserViewModel laserViewModel,
    StageViewModel stageViewModel,
    CIBViewModel cibViewModel,
    ApplicationCookie applicationCookie,
    ICacheProvider cacheProvider) : ViewModelBase
{
    [ObservableProperty]
    private ApplicationCookie _applicationCookie = applicationCookie;

    [DefaultCache]
    [ObservableProperty]
    private GrabbingDarkImageWindowCache _cache = new();

    [RelayCommand]
    private async Task LoadedAsync() => await Task.Run(() => Cache = cacheProvider.GetOrDefault<GrabbingDarkImageWindowCache>());

    [RelayCommand]
    private void ChangedPrescanAODWaveformProfiles()
    {
        try
        {
            var dialog = dialogWindowProvider.TryShowSelectFilePathDialog(AODWaveformGenerator1.PrescanAODWaveformFileExtension, out var filePath);
            if (dialog == false) return;

            Cache.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(filePath);
            Cache.PrescanAODWaveformResultFilePath = filePath;
        }
        catch (Exception ex)
        {
            ClearPrescanAODWaveformProfiles();

            logger.LogError(ex, "Changed Prescan AOD Waveform Profiles Failed");
            dialogWindowProvider.ShowDialog($"""
                                             Changed Prescan AOD Waveform Profiles Failed
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private void ClearPrescanAODWaveformProfiles()
    {
        Cache.PrescanAODWaveformProfiles = [];
        Cache.PrescanAODWaveformResultFilePath = string.Empty;
    }

    [RelayCommand]
    private void ChangedChirpAODWaveformProfiles()
    {
        try
        {
            var dialog = dialogWindowProvider.TryShowSelectFilePathDialog(AODWaveformGenerator1.ChirpAODWaveformFileExtension, out var filePath);
            if (dialog == false) return;

            Cache.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(filePath);
            Cache.ChirpAODWaveformResultFilePath = filePath;
        }
        catch (Exception ex)
        {
            ClearChirpAODWaveformProfiles();

            logger.LogError(ex, "Changed Chirp AOD Waveform Profiles Failed");
            dialogWindowProvider.ShowDialog($"""
                                             Changed Chirp AOD Waveform Profiles Failed
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private void ClearChirpAODWaveformProfiles()
    {
        Cache.ChirpAODWaveformProfiles = [];
        Cache.ChirpAODWaveformResultFilePath = string.Empty;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task GetPMTImagesByWidthAsync(CancellationToken cancellationToken) =>
        InvokeGetPMTImagesAsync("Width", async () =>
        {
            if (Cache.IsAutoFocus == false)
            {
                afViewModel.ToggleBrightFieldEnable(false);
                afViewModel.SetSensorEcsValue(Cache.ECS);
            }

            var startPosition = Cache.StageCoordinateSystemEnum switch
            {
                StageCoordinateSystemEnum.Bright => stageViewModel.GetBrightFieldStagePosition(),
                StageCoordinateSystemEnum.Dark => stageViewModel.GetDarkFieldStagePosition(),
                StageCoordinateSystemEnum.Machine => stageViewModel.GetMachineStagePosition(),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
            };

            var resultList = new List<IReadOnlyList<DarkFieldRawScanImageDTO>>();
            foreach (var cibInformations in Cache.CIBInformations
                         .GroupBy(t => t.PMTId)
                         .OrderBy(t => t.Key)
                         .Select(gg => gg.OrderByDescending(t => t).ToArray()))
            {
                var currentStartPosition = cibViewModel.GetCIBInformationPosition(
                    Cache.StageCoordinateSystemEnum,
                    cibInformations[0],
                    startPosition,
                    microscopeViewModel.GetCurrentMicroscopeLensInformation());

                var darkFieldRawScanImages = await cibViewModel.GetPMTImagesAsync(
                    Cache.ProductivityInformation,
                    Cache.StageCoordinateSystemEnum,
                    currentStartPosition,
                    Cache.ImageWidth,
                    cibInformations,
                    (false, Cache.CalChipSiteModelEnum),
                    (false, Cache.CIBConfiguration),
                    (true, null),
                    true,
                    cancellationToken,
                    isForward: Cache.IsForward,
                    isAutoFocus: Cache.IsAutoFocus,
                    isKeepOrigin: Cache.IsKeepOrigin);

                foreach (var darkFieldImage in darkFieldRawScanImages)
                {
                    using var _ = darkFieldImage;
                }

                resultList.Add(darkFieldRawScanImages);
            }

            Cache.Results = resultList;
        }, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task GetPMTImagesByPTPAsync(CancellationToken cancellationToken) =>
        InvokeGetPMTImagesAsync("PTP", async () =>
        {
            if (Cache.IsAutoFocus == false)
            {
                afViewModel.ToggleBrightFieldEnable(false);
                afViewModel.SetSensorEcsValue(Cache.ECS);
            }

            var (xDirection, yDirection) = stageViewModel.GetMachineDirection();
            var startPosition = Cache.StageCoordinateSystemEnum switch
            {
                StageCoordinateSystemEnum.Bright => stageViewModel.GetBrightFieldStagePosition(),
                StageCoordinateSystemEnum.Dark => stageViewModel.GetDarkFieldStagePosition(),
                StageCoordinateSystemEnum.Machine => stageViewModel.GetMachineStagePosition(),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
            };

            var resultList = new List<IReadOnlyList<DarkFieldRawScanImageDTO>>();
            foreach (var cibInformations in Cache.CIBInformations
                         .GroupBy(t => t.PMTId)
                         .OrderBy(t => t.Key)
                         .Select(gg => gg.OrderByDescending(t => t).ToArray()))
            {
                var currentStartPosition = cibViewModel.GetCIBInformationPosition(
                    Cache.StageCoordinateSystemEnum,
                    cibInformations[0],
                    startPosition,
                    microscopeViewModel.GetCurrentMicroscopeLensInformation());

                var currentStopPosition = Cache.StageCoordinateSystemEnum switch
                {
                    StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => currentStartPosition + new Vector(Cache.ScanLength, 0),
                    StageCoordinateSystemEnum.Machine => currentStartPosition + new Vector(xDirection * Cache.ScanLength, yDirection * 0),
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
                };

                var darkFieldRawScanImages = await cibViewModel.GetPMTImagesAsync(
                    Cache.ProductivityInformation,
                    Cache.StageCoordinateSystemEnum,
                    currentStartPosition,
                    currentStopPosition,
                    cibInformations,
                    (false, Cache.CalChipSiteModelEnum),
                    (false, Cache.CIBConfiguration),
                    (true, null),
                    true,
                    cancellationToken,
                    isForward: Cache.IsForward,
                    isAutoFocus: Cache.IsAutoFocus,
                    isKeepOrigin: Cache.IsKeepOrigin);

                foreach (var darkFieldImage in darkFieldRawScanImages)
                {
                    using var _ = darkFieldImage as DarkFieldImageDTO;
                }

                resultList.Add(darkFieldRawScanImages);
            }

            Cache.Results = resultList;
        }, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task GetPMTImagesByPEGAsync(CancellationToken cancellationToken) =>
        InvokeGetPMTImagesAsync("PEG", async () =>
        {
            if (Cache.IsAutoFocus == false)
            {
                afViewModel.ToggleBrightFieldEnable(false);
                afViewModel.SetSensorEcsValue(Cache.ECS);
            }

            var (xDirection, yDirection) = stageViewModel.GetMachineDirection();
            var startPosition = Cache.StageCoordinateSystemEnum switch
            {
                StageCoordinateSystemEnum.Bright => stageViewModel.GetBrightFieldStagePosition(),
                StageCoordinateSystemEnum.Dark => stageViewModel.GetDarkFieldStagePosition(),
                StageCoordinateSystemEnum.Machine => stageViewModel.GetMachineStagePosition(),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
            };

            var resultList = new List<IReadOnlyList<DarkFieldRawScanImageDTO>>();
            foreach (var cibInformations in Cache.CIBInformations
                         .GroupBy(t => t.PMTId)
                         .OrderBy(t => t.Key)
                         .Select(gg => gg.OrderByDescending(t => t).ToArray()))
            {
                Guard.IsEqualTo(cibInformations.Length, 1);

                var currentStartPosition = cibViewModel.GetCIBInformationPosition(
                    Cache.StageCoordinateSystemEnum,
                    cibInformations[0],
                    startPosition,
                    microscopeViewModel.GetCurrentMicroscopeLensInformation());

                var positions = Enumerable.Range(0, Cache.ColumnCount)
                    .Select(t => Cache.StageCoordinateSystemEnum switch
                    {
                        StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => currentStartPosition + new Vector(t * Cache.ColumnWidth, 0),
                        StageCoordinateSystemEnum.Machine => currentStartPosition + new Vector(xDirection * t * Cache.ColumnWidth, yDirection * 0),
                        _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
                    }).ToArray();

                var darkFieldRawScanImages = await cibViewModel.GetPMTImagesAsync(
                    Cache.ProductivityInformation,
                    Cache.StageCoordinateSystemEnum,
                    positions,
                    Cache.ImageWidth,
                    cibInformations[0],
                    (false, Cache.CalChipSiteModelEnum),
                    (false, Cache.CIBConfiguration),
                    (true, null),
                    true,
                    cancellationToken,
                    isAutoFocus: Cache.IsAutoFocus,
                    isKeepOrigin: Cache.IsKeepOrigin);

                foreach (var darkFieldImage in darkFieldRawScanImages)
                {
                    using var _ = darkFieldImage;
                }

                resultList.Add(darkFieldRawScanImages);
            }

            Cache.Results = resultList;
        }, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task GetPMTImagesByXZSyncAsync(CancellationToken cancellationToken) =>
        InvokeGetPMTImagesAsync("XZSync", async () =>
        {
            var (xDirection, yDirection) = stageViewModel.GetMachineDirection();
            var startPosition = Cache.StageCoordinateSystemEnum switch
            {
                StageCoordinateSystemEnum.Bright => stageViewModel.GetBrightFieldStagePosition(),
                StageCoordinateSystemEnum.Dark => stageViewModel.GetDarkFieldStagePosition(),
                StageCoordinateSystemEnum.Machine => stageViewModel.GetMachineStagePosition(),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
            };

            var resultList = new List<IReadOnlyList<DarkFieldRawScanImageDTO>>();
            foreach (var cibInformations in Cache.CIBInformations
                         .GroupBy(t => t.PMTId)
                         .OrderBy(t => t.Key)
                         .Select(gg => gg.OrderByDescending(t => t).ToArray()))
            {
                var currentStartPosition = cibViewModel.GetCIBInformationPosition(
                    Cache.StageCoordinateSystemEnum,
                    cibInformations[0],
                    startPosition,
                    microscopeViewModel.GetCurrentMicroscopeLensInformation());

                var currentStopPosition = Cache.StageCoordinateSystemEnum switch
                {
                    StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => currentStartPosition + new Vector(Cache.ScanLength, 0),
                    StageCoordinateSystemEnum.Machine => currentStartPosition + new Vector(xDirection * Cache.ScanLength, yDirection * 0),
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
                };

                var darkFieldRawScanImages = await cibViewModel.GetPMTImagesAsync(
                    Cache.ProductivityInformation,
                    Cache.StageCoordinateSystemEnum,
                    currentStartPosition,
                    currentStopPosition,
                    Cache.StartECS,
                    Cache.StopECS,
                    cibInformations,
                    (false, Cache.CalChipSiteModelEnum),
                    (false, Cache.CIBConfiguration),
                    (true, null),
                    true,
                    cancellationToken,
                    isForward: Cache.IsForward,
                    isKeepOrigin: Cache.IsKeepOrigin);

                foreach (var darkFieldImage in darkFieldRawScanImages)
                {
                    using var _ = darkFieldImage;
                }

                resultList.Add(darkFieldRawScanImages);
            }

            Cache.Results = resultList;
        }, cancellationToken);

    private async Task InvokeGetPMTImagesAsync(
        string modeName,
        Func<Task> action,
        CancellationToken cancellationToken)
    {
        await Task.Run((Func<Task>)(async () =>
        {
            try
            {
                if (dialogWindowProvider.TryShowDialog($"Grabbing Image By {modeName}, Please confirm Param.", out var dialogResultEnum, DialogButtonsEnum.OKCancel) == false || dialogResultEnum != DialogResultEnum.OK) return;

                if (Cache.PrescanAODWaveformProfiles.Count > 0)
                {
                    foreach (var prescanAODWaveformProfile in Cache.PrescanAODWaveformProfiles) prescanAODWaveformProfile.ApplyCoefficient(Cache.LaserLightInformation.Coefficient);

                    laserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, Cache.PrescanAODWaveformProfiles);
                }
                else laserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, Cache.LaserLightInformation.Coefficient);

                if (Cache.ChirpAODWaveformProfiles.Count > 0) laserViewModel.SetChirpAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, Cache.ChirpAODWaveformProfiles);
                else laserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);

                await action();

                dialogWindowProvider.ShowDialog($"Grabbing Image By {modeName} Completed.");
            }
            catch (Exception ex)
            {
                dialogWindowProvider.ShowDialog($"""
                                                 Get PMT Images By {modeName} Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogError(ex, "Get PMT Images By {ModeName}", modeName);
            }
        }), cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand]
    private void Close()
    {
        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            cacheProvider.Set(Cache, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save cache");
        }

        CloseView(true);
    }
}