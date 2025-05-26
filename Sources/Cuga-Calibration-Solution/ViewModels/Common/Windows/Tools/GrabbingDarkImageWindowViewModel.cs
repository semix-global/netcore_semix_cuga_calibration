using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithm.Halcon.Helper;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Extensions;
using Net.Utilities.Helper.Enum;
using Net.Utilities.Helper.IOC.Providers;
using Net.Utilities.Models;
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
    IOptions<ApplicationSetting> options,
    ISynchronizationContextProvider contextProvider,
    ILogger<GrabbingDarkImageWindowViewModel> logger)
    : ViewModelBase
{
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
    private double _coefficient = 1;

    [ObservableProperty]
    private double _gainVoltage;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.High;

    [ObservableProperty]
    private StageSpeedEnum _stageSpeedEnum = StageSpeedEnum.Low;

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;

    [ObservableProperty]
    private bool _isEnableL0K;

    [ObservableProperty]
    private bool _isEnableAutoGain;

    [ObservableProperty]
    private bool _isForward = true;

    [ObservableProperty]
    private string _prescanFilePath = string.Empty;

    [ObservableProperty]
    private string _chirpFilePath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<GrabbingDarkImageDto> _grabbingDarkImageList = [];

    [ObservableProperty]
    private GrabbingDarkImageDto _selectGrabbingDarkImageDto = new();

    [RelayCommand]
    private void ChangePrescanFile()
    {
        var dialog = dialogWindowProvider.TryShowSelectFilePathDialog(".txt", out var filePath);
        if (dialog == false) return;

        PrescanFilePath = filePath;
    }

    [RelayCommand]
    private void ChangeChirpFile()
    {
        var dialog = dialogWindowProvider.TryShowSelectFilePathDialog(".txt", out var filePath);
        if (dialog == false) return;

        ChirpFilePath = filePath;
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
                                                               {nameof(StartPosition)}: {StartPosition.ToShortString()}
                                                               {nameof(EndPosition)}: {EndPosition.ToShortString()}
                                                               """
                                                            : $"{nameof(XWidth)}: {XWidth}")}
                                                        {nameof(PmtId)}: {PmtId}
                                                        {nameof(Coefficient)}: {Coefficient}
                                                        {nameof(GainVoltage)}: {GainVoltage}
                                                        {nameof(OpticsMagTypeEnum)}: {OpticsMagTypeEnum}
                                                        {nameof(StageSpeedEnum)}: {StageSpeedEnum}
                                                        {nameof(CalChipSiteModelEnum)}: {CalChipSiteModelEnum}
                                                        {nameof(IsEnableL0K)}: {IsEnableL0K}
                                                        {nameof(IsEnableAutoGain)}: {IsEnableAutoGain}
                                                        {nameof(IsForward)}: {IsForward}
                                                        {(string.IsNullOrWhiteSpace(PrescanFilePath) ? string.Empty : $"{nameof(PrescanFilePath)}: {PrescanFilePath}")}
                                                        {(string.IsNullOrWhiteSpace(ChirpFilePath) ? string.Empty : $"{nameof(ChirpFilePath)}: {ChirpFilePath}")}
                                                        """, out var dialogResultEnum, DialogButtonsEnum.OKCancel) == false || dialogResultEnum != DialogResultEnum.OK) return;

                var isCustomPrescanAod = string.IsNullOrWhiteSpace(PrescanFilePath) == false;
                if (isCustomPrescanAod)
                {
                    var prescanDto = laserViewModel.ReadPrescanByFile(PrescanFilePath, Coefficient);
                    laserViewModel.SendPrescanByList(prescanDto);
                }

                var isCustomChirpAod = string.IsNullOrWhiteSpace(ChirpFilePath) == false;
                if (isCustomChirpAod)
                {
                    var prescanDto = laserViewModel.ReadChirpAodByConfigFile(PrescanFilePath);
                    laserViewModel.SendChirpAodByList(prescanDto);
                }

                if (IsEnableAutoGain)
                    laserViewModel.ToggleEnableAutoGain(true);
                else
                {
                    laserViewModel.ToggleEnableAutoGain(false);
                    //下发电压
                    laserViewModel.SetGain(GainVoltage);
                }

                laserViewModel.ToggleEnableL0K(IsEnableL0K);

                var resultPosition = StageCoordinateSystemEnum switch
                {
                    StageCoordinateSystemEnum.Bright => stageViewModel.GetBrightFieldStagePosition(),
                    StageCoordinateSystemEnum.Dark => stageViewModel.GetDarkFieldStagePosition(),
                    StageCoordinateSystemEnum.Machine => stageViewModel.GetMachineStagePosition(),
                    _ => throw new ArgumentOutOfRangeException(nameof(StageCoordinateSystemEnum), StageCoordinateSystemEnum, null)
                };

                var result = IsPtp
                    ? laserViewModel.GetDarkFieldLineScanImageList(
                        CalChipSiteModelEnum,
                        StartPosition,
                        EndPosition,
                        OpticsMagTypeEnum,
                        StageSpeedEnum,
                        PmtId,
                        StageCoordinateSystemEnum,
                        (isCustomPrescanAod, isCustomPrescanAod ? null : Coefficient),
                        isCustomChirpAod,
                        IsForward)
                    : laserViewModel.GetDarkFieldLineScanImageList(
                        CalChipSiteModelEnum,
                        resultPosition,
                        XWidth,
                        OpticsMagTypeEnum,
                        StageSpeedEnum,
                        PmtId,
                        StageCoordinateSystemEnum,
                        (isCustomPrescanAod, isCustomPrescanAod ? null : Coefficient),
                        isCustomChirpAod,
                        null,
                        IsForward);

                var darkFieldImageList = new List<DarkFieldImage>();

                foreach (var (i, darkFieldImageDto) in result.Select((t, i) => (i, t)))
                {
                    using var _ = darkFieldImageDto;
                    var filePath = $"{options.Value.AppHomeDirectory}\\Images\\{nameof(GrabbingDarkImageWindowViewModel)}\\{OpticsMagTypeEnum}\\{PmtId}-{i + 1}\\{Coefficient}\\{GainVoltage}\\{htmlLogUniqueId}.jpg";
                    HalconHelper.Save(darkFieldImageDto.Image, filePath);
                    var size = HalconHelper.GetSize(darkFieldImageDto.Image);
                    darkFieldImageList.Add(new DarkFieldImage
                    {
                        ByteArray = darkFieldImageDto.Bytes,
                        Width = size.Width,
                        Height = size.Height,
                        FilePath = filePath,
                        DarkFieldImageList = [.. darkFieldImageDto.ProjectionYs]
                    });
                }

                var plotList = new List<WpfPlotModel>
                {
                    new("ch1", darkFieldImageList[0].DarkFieldImageList.ToPoints()),
                    new("ch2", darkFieldImageList[1].DarkFieldImageList.ToPoints()),
                    new("ch3", darkFieldImageList[2].DarkFieldImageList.ToPoints())
                };
                var grabbingDarkImageDto = new GrabbingDarkImageDto
                {
                    PmtId = PmtId,
                    Coefficient = Coefficient,
                    GainVoltage = GainVoltage,
                    OpticsMagTypeEnum = OpticsMagTypeEnum,
                    StageSpeedEnum = StageSpeedEnum,
                    CalChipSiteModelEnum = CalChipSiteModelEnum,
                    StageCoordinateSystemEnum = StageCoordinateSystemEnum,
                    IsEnableL0K = IsEnableL0K,
                    IsEnableAutoGain = IsEnableAutoGain,
                    IsForward = IsForward,
                    XWidth = IsPtp ? string.Empty : XWidth.ToString(),
                    StartPosition = IsPtp ? StartPosition.ToShortString() : string.Empty,
                    EndPosition = IsPtp ? EndPosition.ToShortString() : string.Empty,
                    PrescanFilePath = PrescanFilePath,
                    ChirpFilePath = ChirpFilePath,
                    DarkFieldImageList = darkFieldImageList,
                    PlotList = plotList
                };
                contextProvider.Send(() => GrabbingDarkImageList.Add(grabbingDarkImageDto));
                SelectGrabbingDarkImageDto = grabbingDarkImageDto;
                isSuccess = true;

                logger.LogHtmlInformation($"Ok Mag:{OpticsMagTypeEnum};Coefficient: {Coefficient}; Gain: {GainVoltage}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    PmtId,
                    OpticsMagTypeEnum,
                    Coefficient,
                    GainVoltage,
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
                    $"Mag({EnumHelper.ToDescriptionString(OpticsMagTypeEnum)})_CalChip({EnumHelper.ToDescriptionString(CalChipSiteModelEnum)})__GainVoltage({GainVoltage}){(isSuccess ? "OK" : "Failed")}"));
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
                System.IO.File.WriteAllBytes(CalibrationConstantsHelper.ImagePathToRawImagePath(saveFilePath), darkFieldImage.ByteArray);
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
        private int _pmtId;

        [ObservableProperty]
        private double _coefficient;

        [ObservableProperty]
        private double _gainVoltage;

        [ObservableProperty]
        private OpticsMagTypeEnum _opticsMagTypeEnum;

        [ObservableProperty]
        private StageSpeedEnum _stageSpeedEnum;

        [ObservableProperty]
        private CalChipSiteModelEnum _calChipSiteModelEnum;

        [ObservableProperty]
        private StageCoordinateSystemEnum _stageCoordinateSystemEnum;

        [ObservableProperty]
        private bool _isEnableL0K;

        [ObservableProperty]
        private bool _isEnableAutoGain;

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
        private byte[] _byteArray = [];

        [ObservableProperty]
        private double _width;

        [ObservableProperty]
        private double _height;

        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private List<double> _darkFieldImageList = [];
    }
}