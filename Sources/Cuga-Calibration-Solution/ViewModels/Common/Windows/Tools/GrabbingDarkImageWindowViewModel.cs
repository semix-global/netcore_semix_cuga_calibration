using System.Text;
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
    private void ImportAODWaveformParams()
    {
        try
        {
            var isSuccess = true;

            var stringBuilder = new StringBuilder();

            var prescanCache = cacheProvider.GetOrDefault<PrescanAODWaveformElectrodeOffsetCache>();

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

            var chirpCache = cacheProvider.GetOrDefault<ChirpAODWaveformElectrodeOffsetCache>();

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

            dialogWindowProvider.ShowDialog(stringBuilder.ToString(), DialogButtonsEnum.OK, isSuccess ? DialogIconEnum.Information : DialogIconEnum.Warning);
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Import Parameters Failed
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Import Parameters Failed");
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
                    foreach (var configuration in Cache.GeneratePrescanAODWaveformParam.ElectrodeConfigurations) configuration.WithAmplitude(Cache.LaserLightInformation.Coefficient);

                    var prescanAODWaveformResult = AODWaveformGenerator1.GeneratePrescanAODWaveform(Cache.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);

                    Cache.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(prescanAODWaveformResult);
                    Cache.PrescanAODWaveformResultFilePath = prescanAODWaveformResult.FilePath;
                }
                else
                {
                    var dialog = dialogWindowProvider.TryShowSelectFilePathDialog(AODWaveformGenerator1.PrescanAODWaveformFileExtension, out var filePath);
                    if (dialog == false) return;

                    Cache.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(filePath);
                    Cache.PrescanAODWaveformResultFilePath = filePath;
                }

                dialogWindowProvider.ShowDialog("Changed Prescan AOD Waveform Profiles Success");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog("Changed Chirp AOD Waveform Profiles Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                    return;
                }

                logger.LogError(ex, "Changed Prescan AOD Waveform Profiles Failed");
                dialogWindowProvider.ShowDialog($"""
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

                    var chirpAODWaveformResult = AODWaveformGenerator1.GenerateChirpAODWaveform(Cache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);

                    Cache.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(chirpAODWaveformResult);
                    Cache.ChirpAODWaveformResultFilePath = chirpAODWaveformResult.FilePath;
                }
                else
                {
                    var dialog = dialogWindowProvider.TryShowSelectFilePathDialog(AODWaveformGenerator1.ChirpAODWaveformFileExtension, out var filePath);
                    if (dialog == false) return;

                    Cache.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(filePath);
                    Cache.ChirpAODWaveformResultFilePath = filePath;
                }

                dialogWindowProvider.ShowDialog("Changed Chirp AOD Waveform Profiles Success");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog("Changed Chirp AOD Waveform Profiles Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                    return;
                }

                logger.LogError(ex, "Changed Chirp AOD Waveform Profiles Failed");
                dialogWindowProvider.ShowDialog($"""
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
            afViewModel.ToggleBrightFieldEnable(false);
            afViewModel.SetSensorEcsValue(Cache.ECS);
        }

        var resultList = new List<IReadOnlyList<DarkFieldRawScanImageDTO>>();
        foreach (var cibInformations in Cache.CIBInformations
                     .GroupBy(t => t.PMTId)
                     .OrderBy(t => t.Key)
                     .Select(gg => gg.OrderByDescending(t => t).ToArray()))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentStartPosition = cibViewModel.GetCIBInformationPosition(
                Cache.StageCoordinateSystemEnum,
                cibInformations[0],
                startPosition,
                microscopeViewModel.GetCurrentMicroscopeLensInformation());

            var darkFieldImages = await cibViewModel.GetPMTImagesAsync(
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

        Cache.Results = resultList;
    }, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task GetPMTImagesByPTPAsync(CancellationToken cancellationToken) => InvokeGetPMTImagesAsync("PTP", async startPosition =>
    {
        if (Cache.IsAutoFocus == false)
        {
            afViewModel.ToggleBrightFieldEnable(false);
            afViewModel.SetSensorEcsValue(Cache.ECS);
        }

        var resultList = new List<IReadOnlyList<DarkFieldRawScanImageDTO>>();
        foreach (var cibInformations in Cache.CIBInformations
                     .GroupBy(t => t.PMTId)
                     .OrderBy(t => t.Key)
                     .Select(gg => gg.OrderByDescending(t => t).ToArray()))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentStartPosition = cibViewModel.GetCIBInformationPosition(
                Cache.StageCoordinateSystemEnum,
                cibInformations[0],
                startPosition,
                microscopeViewModel.GetCurrentMicroscopeLensInformation());

            var currentStopPosition = currentStartPosition + new Vector(Cache.ScanLength, 0);

            var darkFieldRawScanImages = await cibViewModel.GetPMTImagesAsync(
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

        Cache.Results = resultList;
    }, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task GetPMTImagesByPEGAsync(CancellationToken cancellationToken) => InvokeGetPMTImagesAsync("PEG", async startPosition =>
    {
        if (Cache.IsAutoFocus == false)
        {
            afViewModel.ToggleBrightFieldEnable(false);
            afViewModel.SetSensorEcsValue(Cache.ECS);
        }

        var resultList = new List<IReadOnlyList<DarkFieldRawScanImageDTO>>();
        foreach (var cibInformations in Cache.CIBInformations
                     .GroupBy(t => t.PMTId)
                     .OrderBy(t => t.Key)
                     .Select(gg => gg.OrderByDescending(t => t).ToArray()))
        {
            cancellationToken.ThrowIfCancellationRequested();

            Guard.IsEqualTo(cibInformations.Length, 1);

            var currentStartPosition = cibViewModel.GetCIBInformationPosition(
                Cache.StageCoordinateSystemEnum,
                cibInformations[0],
                startPosition,
                microscopeViewModel.GetCurrentMicroscopeLensInformation());

            var positions = Enumerable.Range(0, Cache.ColumnCount).Select(t => currentStartPosition + new Vector(t * Cache.ColumnWidth, 0)).ToArray();

            if (Cache.IsForward) positions = [..positions.AsEnumerable().Reverse()];

            var darkFieldImages = await cibViewModel.GetPMTImagesAsync(
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

        Cache.Results = resultList;
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

            var currentStartPosition = cibViewModel.GetCIBInformationPosition(
                Cache.StageCoordinateSystemEnum,
                cibInformations[0],
                startPosition,
                microscopeViewModel.GetCurrentMicroscopeLensInformation());

            var currentStopPosition = currentStartPosition + new Vector(Cache.ScanLength, 0);

            var darkFieldImages = await cibViewModel.GetPMTImagesAsync(
                Cache.ProductivityInformation,
                Cache.StageCoordinateSystemEnum,
                currentStartPosition,
                currentStopPosition,
                Cache.StartECS,
                Cache.StopECS,
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

        Cache.Results = resultList;
    }, cancellationToken);

    private async Task InvokeGetPMTImagesAsync(
        string modeName,
        Func<Point, Task> func,
        CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
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

                var startPosition = Cache.StageCoordinateSystemEnum switch
                {
                    StageCoordinateSystemEnum.Bright => stageViewModel.GetBrightFieldStagePosition(),
                    StageCoordinateSystemEnum.Dark => stageViewModel.GetDarkFieldStagePosition(),
                    StageCoordinateSystemEnum.Machine => stageViewModel.GetMachineStagePosition(),
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
                };

                try
                {
                    Cache.Results = [];

                    await func(startPosition);
                }
                finally
                {
                    switch (Cache.StageCoordinateSystemEnum)
                    {
                        case StageCoordinateSystemEnum.Bright:
                            stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(startPosition, Cache.CalChipSiteModelEnum);

                            break;

                        case StageCoordinateSystemEnum.Dark:
                            stageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(startPosition, Cache.CalChipSiteModelEnum);

                            break;

                        case StageCoordinateSystemEnum.Machine:
                            stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(startPosition, Cache.CalChipSiteModelEnum);

                            break;

                        default:
                            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(Cache.StageCoordinateSystemEnum));

                            break;
                    }
                }

                dialogWindowProvider.ShowDialog($"Grabbing Image By {modeName} Completed.");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog($"Get PMT Images By {modeName} Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                    return;
                }

                dialogWindowProvider.ShowDialog($"""
                                                 Get PMT Images By {modeName} Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogError(ex, "Get PMT Images By {ModeName}", modeName);
            }
        }, cancellationToken).ConfigureAwait(false);
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