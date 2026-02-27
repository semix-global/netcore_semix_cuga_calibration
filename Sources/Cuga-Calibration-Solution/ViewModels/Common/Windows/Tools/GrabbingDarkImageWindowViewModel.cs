using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Core.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Behaviors;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(GrabbingDarkImageWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class GrabbingDarkImageWindowViewModel(
    IDialogWindowProvider dialogWindowProvider,
    LaserViewModel laserViewModel,
    StageViewModel stageViewModel,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    IOptions<ApplicationSetting> options,
    ISynchronizationContextProvider contextProvider,
    CalibrationSetting calibrationSetting,
    ApplicationCookie applicationCookie,
    ILogger<GrabbingDarkImageWindowViewModel> logger)
    : ViewModelBase
{
    public IReadOnlyList<LaserLightInformation> LaserLightInformationList => applicationCookie.LaserLightInformations;

    [ObservableProperty]
    private StageCoordinateSystemEnum _stageCoordinateSystemEnum = StageCoordinateSystemEnum.Bright;

    [ObservableProperty]
    private bool _isPtp;

    [ObservableProperty]
    private Point _startPosition;

    [ObservableProperty]
    private Point _endPosition;

    [ObservableProperty]
    private int _xWidth = 800;

    [ObservableProperty]
    private int _pmtId = 8;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = calibrationSetting.SettingCommonParam.MainLaserLightInformation;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.High;

    [ObservableProperty]
    private StageSpeedEnum _stageSpeedEnum = StageSpeedEnum.Low;

    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum = CalibrationConstantsHelper.MainOpticsIlluminationModeEnum;

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;

    [ObservableProperty]
    private bool _isForward = true;

    [ObservableProperty]
    private bool _isAutoFocus = true;

    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];

    [ObservableProperty]
    private string _chirpAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];

    [ObservableProperty]
    private ObservableCollection<GrabbingDarkImageDto> _grabbingDarkImageList = [];

    [ObservableProperty]
    private GrabbingDarkImageDto _selectGrabbingDarkImageDto = new();

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

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

            logger.LogError(ex, "{@Name}: Changed Prescan AOD Waveform Profiles Failed", nameof(GrabbingDarkImageWindowViewModel));
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

            logger.LogError(ex, "{@Name}: Changed Chirp AOD Waveform Profiles Failed", nameof(GrabbingDarkImageWindowViewModel));
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

    [RelayCommand]
    private Task GrabbingImageAsync()
    {
        return Task.Run(() =>
        {
            var htmlLogUniqueId = Guid.NewGuid();
            logger.LogHtmlInformation("Grabbing Image Start", HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());
            var isSuccess = false;

            try
            {
                if (dialogWindowProvider.TryShowDialog($"""
                                                        Grabbing Image Please confirm Param.
                                                        {nameof(StageCoordinateSystemEnum)}: {StageCoordinateSystemEnum}
                                                        {(IsPtp
                                                            ? $"""
                                                               {nameof(StartPosition)}: {StartPosition}
                                                               {nameof(EndPosition)}: {EndPosition}
                                                               """
                                                            : $"{nameof(XWidth)}: {XWidth}")}
                                                        {nameof(PmtId)}: {PmtId}
                                                        {nameof(LaserLightInformation)}: {LaserLightInformation}
                                                        {nameof(CIBConfiguration.Gain)}: {CIBConfiguration.Gain}
                                                        {nameof(OpticsMagTypeEnum)}: {OpticsMagTypeEnum}
                                                        {nameof(StageSpeedEnum)}: {StageSpeedEnum}
                                                        {nameof(CalChipSiteModelEnum)}: {CalChipSiteModelEnum}
                                                        {nameof(CIBConfiguration.IsL0K)}: {CIBConfiguration.IsL0K}
                                                        {nameof(CIBConfiguration.IsAutoGainControl)}: {CIBConfiguration.IsAutoGainControl}
                                                        {nameof(IsForward)}: {IsForward}
                                                        {(string.IsNullOrWhiteSpace(PrescanAODWaveformResultFilePath) ? string.Empty : $"{nameof(PrescanAODWaveformResultFilePath)}: {PrescanAODWaveformResultFilePath}")}
                                                        {(string.IsNullOrWhiteSpace(ChirpAODWaveformResultFilePath) ? string.Empty : $"{nameof(ChirpAODWaveformResultFilePath)}: {ChirpAODWaveformResultFilePath}")}
                                                        """, out var dialogResultEnum, DialogButtonsEnum.OKCancel) == false || dialogResultEnum != DialogResultEnum.OK) return;

                var isCustomPrescanAod = PrescanAODWaveformProfiles.Count > 0 && string.IsNullOrWhiteSpace(PrescanAODWaveformResultFilePath) == false;
                if (isCustomPrescanAod)
                {
                    foreach (var prescanAODWaveformProfile in PrescanAODWaveformProfiles) prescanAODWaveformProfile.ApplyCoefficient(LaserLightInformation.Coefficient);
                    laserViewModel.SetPrescanAODWaveProfiles(OpticsIlluminationModeEnum, PrescanAODWaveformProfiles);
                }

                var isCustomChirpAod = ChirpAODWaveformProfiles.Count > 0 && string.IsNullOrWhiteSpace(ChirpAODWaveformResultFilePath) == false;
                if (isCustomChirpAod)
                {
                    laserViewModel.SetChirpAODWaveProfiles(OpticsIlluminationModeEnum, ChirpAODWaveformProfiles);
                }

                var resultPosition = StageCoordinateSystemEnum switch
                {
                    StageCoordinateSystemEnum.Bright => stageViewModel.GetBrightFieldStagePosition(),
                    StageCoordinateSystemEnum.Dark => stageViewModel.GetDarkFieldStagePosition(),
                    StageCoordinateSystemEnum.Machine => stageViewModel.GetMachineStagePosition(),
                    _ => throw new ArgumentOutOfRangeException(nameof(StageCoordinateSystemEnum), StageCoordinateSystemEnum, null)
                };

                var result = IsPtp
                    ?
                    [
                        .. laserViewModel.GetDarkFieldLineScanImageList(
                            StartPosition,
                            EndPosition,
                            OpticsMagTypeEnum,
                            StageSpeedEnum,
                            OpticsIlluminationModeEnum,
                            PmtId,
                            StageCoordinateSystemEnum,
                            CIBConfiguration,
                            (isCustomPrescanAod, isCustomPrescanAod ? null : LaserLightInformation),
                            isCustomChirpAod,
                            IsForward,
                            IsAutoFocus).Select(ToDarkFieldImageDto)
                    ]
                    : laserViewModel.GetDarkFieldLineScanImageList(
                        CalChipSiteModelEnum,
                        resultPosition,
                        XWidth,
                        OpticsMagTypeEnum,
                        StageSpeedEnum,
                        OpticsIlluminationModeEnum,
                        PmtId,
                        StageCoordinateSystemEnum,
                        CIBConfiguration,
                        (isCustomPrescanAod, isCustomPrescanAod ? null : LaserLightInformation),
                        isCustomChirpAod,
                        IsForward,
                        IsAutoFocus);

                var darkFieldImageList = new List<DarkFieldImage>();

                foreach (var (i, darkFieldImageDto) in result.Select((t, i) => (i, t)))
                {
                    using var _ = darkFieldImageDto;
                    var filePath = $@"{options.Value.AppHomeDirectory}\Images\{nameof(GrabbingDarkImageWindowViewModel)}\{OpticsMagTypeEnum}\{PmtId}-{i + 1}\{LaserLightInformation}\{CIBConfiguration.Gain}\{htmlLogUniqueId}.jpg";
                    darkFieldImageDto.Image.Save(filePath);
                    darkFieldImageList.Add(new DarkFieldImage
                    {
                        Width = darkFieldImageDto.Width,
                        Height = darkFieldImageDto.Height,
                        FilePath = filePath,
                        RawImageFilePath = darkFieldImageDto.RawImageFilePath,
                        DarkFieldImageList = [.. darkFieldImageDto.Image.GetHorizontalProjects()]
                    });
                }

                var plotList = new List<WpfPlotModel>
                {
                    new("ch1", [..darkFieldImageList[0].DarkFieldImageList.ToPoints()]),
                    new("ch2", [..darkFieldImageList[1].DarkFieldImageList.ToPoints()]),
                    new("ch3", [..darkFieldImageList[2].DarkFieldImageList.ToPoints()])
                };
                var grabbingDarkImageDto = new GrabbingDarkImageDto
                {
                    PmtId = PmtId,
                    Coefficient = LaserLightInformation.Coefficient,
                    OpticsMagTypeEnum = OpticsMagTypeEnum,
                    StageSpeedEnum = StageSpeedEnum,
                    CalChipSiteModelEnum = CalChipSiteModelEnum,
                    StageCoordinateSystemEnum = StageCoordinateSystemEnum,
                    CIBConfiguration = CIBConfiguration.Clone(),
                    IsForward = IsForward,
                    XWidth = IsPtp ? string.Empty : XWidth.ToString(),
                    StartPosition = IsPtp ? StartPosition.ToString() : string.Empty,
                    EndPosition = IsPtp ? EndPosition.ToString() : string.Empty,
                    PrescanFilePath = PrescanAODWaveformResultFilePath,
                    ChirpFilePath = ChirpAODWaveformResultFilePath,
                    DarkFieldImageList = darkFieldImageList,
                    PlotList = plotList
                };
                contextProvider.Send(() => GrabbingDarkImageList.Add(grabbingDarkImageDto));
                SelectGrabbingDarkImageDto = grabbingDarkImageDto;
                isSuccess = true;

                logger.LogHtmlInformation($"Ok Mag:{OpticsMagTypeEnum};Coefficient: {LaserLightInformation}; Gain: {CIBConfiguration.Gain}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    PmtId,
                    OpticsMagTypeEnum,
                    LaserLightInformation,
                    CIBConfiguration.Gain,
                    CalChipSiteModelEnum,
                    HtmlTab = new HtmlTab(new
                    {
                        CH1 = new HtmlImage(darkFieldImageList[0].FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        CH2 = new HtmlImage(darkFieldImageList[1].FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        CH3 = new HtmlImage(darkFieldImageList[2].FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    }),
                    DarkFieldImageList = new HtmlPlot2DLinesChart([
                        ("ch1", darkFieldImageList[0].DarkFieldImageList.ToPoints()),
                        ("ch2", darkFieldImageList[1].DarkFieldImageList.ToPoints()),
                        ("ch3", darkFieldImageList[2].DarkFieldImageList.ToPoints())
                    ], "Dark Field Image")
                }), htmlLogUniqueId.LoggingHtml());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{@Name}: Grabbing Image Failed", nameof(GrabbingDarkImageWindowViewModel));
            }
            finally
            {
                logger.LogHtmlInformation(htmlLogUniqueId.LoggedEndHtml(
                    $"Mag({EnumHelper.ToDescriptionString(OpticsMagTypeEnum)})_CalChip({EnumHelper.ToDescriptionString(CalChipSiteModelEnum)})__GainVoltage({CIBConfiguration.Gain}){(isSuccess ? "OK" : "Failed")}"));
            }

            return;

            DarkFieldImageDTO ToDarkFieldImageDto(DarkFieldRawScanImageDTO origin)
            {
                var rawBytes = System.IO.File.ReadAllBytes(origin.RawImageFilePath);
                var image = RawImageFactory.CreateImage(rawBytes);

                return new DarkFieldImageDTO
                {
                    Image = image
                }.AdaptIn(origin);
            }
        });
    }

    [RelayCommand]
    private async Task SavePictureAsync(DarkFieldImage darkFieldImage)
    {
        try
        {
            await Task.Run(() =>
            {
                var tryShowSaveFilePathDialog = dialogWindowProvider.TryShowSaveFilePathDialog(".jpg", out var saveFilePath);
                if (tryShowSaveFilePathDialog == false) return;

                System.IO.File.Copy(darkFieldImage.FilePath, saveFilePath, true);
                System.IO.File.WriteAllBytes($"{saveFilePath}.raw", System.IO.File.ReadAllBytes(darkFieldImage.RawImageFilePath));
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: Grabbing Image Failed", nameof(GrabbingDarkImageWindowViewModel));
        }
    }

    [RelayCommand]
    private void ShowPlot(DarkFieldImage darkFieldImage)
    {
        try
        {
            dialogWindowProvider.ShowPlot([.. darkFieldImage.DarkFieldImageList]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: Show Plot Failed", nameof(GrabbingDarkImageWindowViewModel));
        }
    }

    [RelayCommand]
    private void DeleteImage(GrabbingDarkImageDto grabbingDarkImageDto)
    {
        contextProvider.Send(() => GrabbingDarkImageList.Remove(grabbingDarkImageDto));
        if (GrabbingDarkImageList.Count > 0) SelectGrabbingDarkImageDto = GrabbingDarkImageList[0];
    }

    [RelayCommand]
    private void Close()
    {
        CloseView(true);
    }

    public sealed partial class GrabbingDarkImageDto : ObservableObject
    {
        [ObservableProperty]
        private CIBConfiguration _cIBConfiguration = new();

        [ObservableProperty]
        private int _pmtId;

        [ObservableProperty]
        private double _coefficient;

        [ObservableProperty]
        private OpticsMagTypeEnum _opticsMagTypeEnum;

        [ObservableProperty]
        private StageSpeedEnum _stageSpeedEnum;

        [ObservableProperty]
        private CalChipSiteModelEnum _calChipSiteModelEnum;

        [ObservableProperty]
        private StageCoordinateSystemEnum _stageCoordinateSystemEnum;

        [ObservableProperty]
        private bool _isForward;

        [ObservableProperty]
        private string _xWidth = string.Empty;

        [ObservableProperty]
        private string _startPosition = string.Empty;

        [ObservableProperty]
        private string _endPosition = string.Empty;

        [ObservableProperty]
        private string _prescanFilePath = string.Empty;

        [ObservableProperty]
        private string _chirpFilePath = string.Empty;

        [ObservableProperty]
        private List<DarkFieldImage> _darkFieldImageList = [];

        [ObservableProperty]
        private List<WpfPlotModel> _plotList = [];
    }

    public sealed partial class DarkFieldImage : ObservableObject
    {
        [ObservableProperty]
        private double _width;

        [ObservableProperty]
        private double _height;

        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private string _rawImageFilePath = string.Empty;

        [ObservableProperty]
        private List<double> _darkFieldImageList = [];
    }
}