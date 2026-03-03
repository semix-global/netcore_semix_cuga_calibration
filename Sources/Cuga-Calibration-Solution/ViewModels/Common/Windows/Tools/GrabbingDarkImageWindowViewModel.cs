using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Setting;
using CugaCalibration.ViewModels.Chuck;
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
    CalibrationSetting calibrationSetting,
    ApplicationCookie applicationCookie) : ViewModelBase
{
    [ObservableProperty]
    private ApplicationCookie _applicationCookie = applicationCookie;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = applicationCookie.OIHighProductivityInformation.Clone();

    [ObservableProperty]
    private StageCoordinateSystemEnum _stageCoordinateSystemEnum = StageCoordinateSystemEnum.Bright;

    [ObservableProperty]
    private int _imageWidth = 800;

    [ObservableProperty]
    private double _scanLength = 5100;

    [ObservableProperty]
    private int _columnCount = 5;

    [ObservableProperty]
    private double _columnWidth = 15300;

    [ObservableProperty]
    private double _startECS = 6000;

    [ObservableProperty]
    private double _stopECS = 7000;

    [ObservableProperty]
    private IReadOnlyList<CIBInformation> _cIBInformations = [calibrationSetting.SettingCommonParam.MainCIBInformation];

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = calibrationSetting.SettingCommonParam.MainLaserLightInformation.Clone();

    [ObservableProperty]
    private bool _isForward = true;

    [ObservableProperty]
    private bool _isAutoFocus = true;

    [ObservableProperty]
    private double _eCS = 6000;

    [ObservableProperty]
    private bool _isKeepOrigin;

    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];

    [ObservableProperty]
    private string _chirpAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];

    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<DarkFieldRawScanImageDTO>> _results = [];

    [RelayCommand]
    private void ChangedPrescanAODWaveformProfiles()
    {
        try
        {
            var dialog = dialogWindowProvider.TryShowSelectFilePathDialog(AODWaveformGenerator1.PrescanAODWaveformFileExtension, out var filePath);
            if (dialog == false) return;

            PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(filePath);
            PrescanAODWaveformResultFilePath = filePath;
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
        PrescanAODWaveformProfiles = [];
        PrescanAODWaveformResultFilePath = string.Empty;
    }

    [RelayCommand]
    private void ChangedChirpAODWaveformProfiles()
    {
        try
        {
            var dialog = dialogWindowProvider.TryShowSelectFilePathDialog(AODWaveformGenerator1.ChirpAODWaveformFileExtension, out var filePath);
            if (dialog == false) return;

            ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(filePath);
            ChirpAODWaveformResultFilePath = filePath;
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
        ChirpAODWaveformProfiles = [];
        ChirpAODWaveformResultFilePath = string.Empty;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task GetPMTImagesByWidthAsync(CancellationToken cancellationToken) =>
        InvokeGetPMTImagesAsync("Width", async () =>
        {
            if (IsAutoFocus == false)
            {
                afViewModel.ToggleBrightFieldEnable(false);
                afViewModel.SetSensorEcsValue(ECS);
            }

            var startPosition = StageCoordinateSystemEnum switch
            {
                StageCoordinateSystemEnum.Bright => stageViewModel.GetBrightFieldStagePosition(),
                StageCoordinateSystemEnum.Dark => stageViewModel.GetDarkFieldStagePosition(),
                StageCoordinateSystemEnum.Machine => stageViewModel.GetMachineStagePosition(),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
            };

            var resultList = new List<IReadOnlyList<DarkFieldRawScanImageDTO>>();
            foreach (var cibInformations in CIBInformations
                         .GroupBy(t => t.PMTId)
                         .OrderBy(t => t.Key)
                         .Select(gg => gg.OrderByDescending(t => t).ToArray()))
            {
                var currentStartPosition = cibViewModel.GetCIBInformationPosition(
                    StageCoordinateSystemEnum,
                    cibInformations[0],
                    startPosition,
                    microscopeViewModel.GetCurrentMicroscopeLensInformation());

                var darkFieldRawScanImages = await cibViewModel.GetPMTImagesAsync(
                    ProductivityInformation,
                    StageCoordinateSystemEnum,
                    currentStartPosition,
                    ImageWidth,
                    cibInformations,
                    (false, CalChipSiteModelEnum),
                    (false, CIBConfiguration),
                    (true, null),
                    true,
                    cancellationToken,
                    isForward: IsForward,
                    isAutoFocus: IsAutoFocus,
                    isKeepOrigin: IsKeepOrigin);

                foreach (var darkFieldImage in darkFieldRawScanImages)
                {
                    using var _ = darkFieldImage;
                }

                resultList.Add(darkFieldRawScanImages);
            }

            Results = resultList;
        }, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task GetPMTImagesByPTPAsync(CancellationToken cancellationToken) =>
        InvokeGetPMTImagesAsync("PTP", async () =>
        {
            if (IsAutoFocus == false)
            {
                afViewModel.ToggleBrightFieldEnable(false);
                afViewModel.SetSensorEcsValue(ECS);
            }

            var (xDirection, yDirection) = stageViewModel.GetMachineDirection();
            var startPosition = StageCoordinateSystemEnum switch
            {
                StageCoordinateSystemEnum.Bright => stageViewModel.GetBrightFieldStagePosition(),
                StageCoordinateSystemEnum.Dark => stageViewModel.GetDarkFieldStagePosition(),
                StageCoordinateSystemEnum.Machine => stageViewModel.GetMachineStagePosition(),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
            };

            var resultList = new List<IReadOnlyList<DarkFieldRawScanImageDTO>>();
            foreach (var cibInformations in CIBInformations
                         .GroupBy(t => t.PMTId)
                         .OrderBy(t => t.Key)
                         .Select(gg => gg.OrderByDescending(t => t).ToArray()))
            {
                var currentStartPosition = cibViewModel.GetCIBInformationPosition(
                    StageCoordinateSystemEnum,
                    cibInformations[0],
                    startPosition,
                    microscopeViewModel.GetCurrentMicroscopeLensInformation());

                var currentStopPosition = StageCoordinateSystemEnum switch
                {
                    StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => currentStartPosition + new Vector(ScanLength, 0),
                    StageCoordinateSystemEnum.Machine => currentStartPosition + new Vector(xDirection * ScanLength, yDirection * 0),
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
                };

                var darkFieldRawScanImages = await cibViewModel.GetPMTImagesAsync(
                    ProductivityInformation,
                    StageCoordinateSystemEnum,
                    currentStartPosition,
                    currentStopPosition,
                    cibInformations,
                    (false, CalChipSiteModelEnum),
                    (false, CIBConfiguration),
                    (true, null),
                    true,
                    cancellationToken,
                    isForward: IsForward,
                    isAutoFocus: IsAutoFocus,
                    isKeepOrigin: IsKeepOrigin);

                foreach (var darkFieldImage in darkFieldRawScanImages)
                {
                    using var _ = darkFieldImage as DarkFieldImageDTO;
                }

                resultList.Add(darkFieldRawScanImages);
            }

            Results = resultList;
        }, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task GetPMTImagesByPEGAsync(CancellationToken cancellationToken) =>
        InvokeGetPMTImagesAsync("PEG", async () =>
        {
            if (IsAutoFocus == false)
            {
                afViewModel.ToggleBrightFieldEnable(false);
                afViewModel.SetSensorEcsValue(ECS);
            }

            var (xDirection, yDirection) = stageViewModel.GetMachineDirection();
            var startPosition = StageCoordinateSystemEnum switch
            {
                StageCoordinateSystemEnum.Bright => stageViewModel.GetBrightFieldStagePosition(),
                StageCoordinateSystemEnum.Dark => stageViewModel.GetDarkFieldStagePosition(),
                StageCoordinateSystemEnum.Machine => stageViewModel.GetMachineStagePosition(),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
            };

            var resultList = new List<IReadOnlyList<DarkFieldRawScanImageDTO>>();
            foreach (var cibInformations in CIBInformations
                         .GroupBy(t => t.PMTId)
                         .OrderBy(t => t.Key)
                         .Select(gg => gg.OrderByDescending(t => t).ToArray()))
            {
                Guard.IsEqualTo(cibInformations.Length, 1);

                var currentStartPosition = cibViewModel.GetCIBInformationPosition(
                    StageCoordinateSystemEnum,
                    cibInformations[0],
                    startPosition,
                    microscopeViewModel.GetCurrentMicroscopeLensInformation());

                var positions = Enumerable.Range(0, ColumnCount)
                    .Select(t => StageCoordinateSystemEnum switch
                    {
                        StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => currentStartPosition + new Vector(t * ColumnWidth, 0),
                        StageCoordinateSystemEnum.Machine => currentStartPosition + new Vector(xDirection * t * ColumnWidth, yDirection * 0),
                        _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
                    }).ToArray();

                var darkFieldRawScanImages = await cibViewModel.GetPMTImagesAsync(
                    ProductivityInformation,
                    StageCoordinateSystemEnum,
                    positions,
                    ImageWidth,
                    cibInformations[0],
                    (false, CalChipSiteModelEnum),
                    (false, CIBConfiguration),
                    (true, null),
                    true,
                    cancellationToken,
                    isAutoFocus: IsAutoFocus,
                    isKeepOrigin: IsKeepOrigin);

                foreach (var darkFieldImage in darkFieldRawScanImages)
                {
                    using var _ = darkFieldImage;
                }

                resultList.Add(darkFieldRawScanImages);
            }

            Results = resultList;
        }, cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task GetPMTImagesByXZSyncAsync(CancellationToken cancellationToken) =>
        InvokeGetPMTImagesAsync("XZSync", async () =>
        {
            var (xDirection, yDirection) = stageViewModel.GetMachineDirection();
            var startPosition = StageCoordinateSystemEnum switch
            {
                StageCoordinateSystemEnum.Bright => stageViewModel.GetBrightFieldStagePosition(),
                StageCoordinateSystemEnum.Dark => stageViewModel.GetDarkFieldStagePosition(),
                StageCoordinateSystemEnum.Machine => stageViewModel.GetMachineStagePosition(),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
            };

            var resultList = new List<IReadOnlyList<DarkFieldRawScanImageDTO>>();
            foreach (var cibInformations in CIBInformations
                         .GroupBy(t => t.PMTId)
                         .OrderBy(t => t.Key)
                         .Select(gg => gg.OrderByDescending(t => t).ToArray()))
            {
                var currentStartPosition = cibViewModel.GetCIBInformationPosition(
                    StageCoordinateSystemEnum,
                    cibInformations[0],
                    startPosition,
                    microscopeViewModel.GetCurrentMicroscopeLensInformation());

                var currentStopPosition = StageCoordinateSystemEnum switch
                {
                    StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => currentStartPosition + new Vector(ScanLength, 0),
                    StageCoordinateSystemEnum.Machine => currentStartPosition + new Vector(xDirection * ScanLength, yDirection * 0),
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(StageCoordinateSystemEnum))
                };

                var darkFieldRawScanImages = await cibViewModel.GetPMTImagesAsync(
                    ProductivityInformation,
                    StageCoordinateSystemEnum,
                    currentStartPosition,
                    currentStopPosition,
                    StartECS,
                    StopECS,
                    cibInformations,
                    (false, CalChipSiteModelEnum),
                    (false, CIBConfiguration),
                    (true, null),
                    true,
                    cancellationToken,
                    isForward: IsForward,
                    isKeepOrigin: IsKeepOrigin);

                foreach (var darkFieldImage in darkFieldRawScanImages)
                {
                    using var _ = darkFieldImage;
                }

                resultList.Add(darkFieldRawScanImages);
            }

            Results = resultList;
        }, cancellationToken);

    private async Task InvokeGetPMTImagesAsync(
        string modeName,
        Func<Task> action,
        CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                if (dialogWindowProvider.TryShowDialog($"Grabbing Image By {modeName}, Please confirm Param.", out var dialogResultEnum, DialogButtonsEnum.OKCancel) == false || dialogResultEnum != DialogResultEnum.OK) return;

                if (PrescanAODWaveformProfiles.Count > 0)
                {
                    foreach (var prescanAODWaveformProfile in PrescanAODWaveformProfiles) prescanAODWaveformProfile.ApplyCoefficient(LaserLightInformation.Coefficient);

                    laserViewModel.SetPrescanAODWaveProfiles(ProductivityInformation.OpticsIlluminationModeEnum, PrescanAODWaveformProfiles);
                }
                else laserViewModel.SetPrescanAODWaveProfileByCoefficient(ProductivityInformation, LaserLightInformation.Coefficient);

                if (ChirpAODWaveformProfiles.Count > 0) laserViewModel.SetChirpAODWaveProfiles(ProductivityInformation.OpticsIlluminationModeEnum, ChirpAODWaveformProfiles);
                else laserViewModel.SetChirpAODWaveProfile(ProductivityInformation);

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
        }, cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand]
    private void Close()
    {
        CloseView(true);
    }
}