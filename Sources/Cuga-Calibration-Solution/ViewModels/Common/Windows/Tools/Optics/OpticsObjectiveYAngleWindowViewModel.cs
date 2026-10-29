using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Microscope.CalChip;
using Core.Services.Interfaces;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;
using System.Text;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Optics;

[IOCAppService(ServiceType = typeof(OpticsObjectiveYAngleWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsObjectiveYAngleWindowViewModel(
    ApplicationCookie applicationCookie,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    IApplicationCookieService applicationCookieService,
    IDialogWindowProvider dialogWindowProvider,
    IOptions<ApplicationSetting> options,
    StageViewModel stageViewModel,
    AfViewModel afViewModel,
    LaserViewModel laserViewModel,
    FourierViewModel fourierViewModel,
    ILogger<OpticsObjectiveYAngleWindowViewModel> logger) : ViewModelBase
{
    #region 仿真器

    private static readonly string[] SimulatorImageFilePaths =
    [
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\YAngle", "Haze.jpg"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\YAngle", "ShinyWafer.jpg")
    ];

    #endregion 仿真器

    public string Name => "Optics Y Angle";

    public string AODWaveformDirectoryPath => Path.Combine(options.Value.AppHomeDirectory, nameof(AODWaveform), nameof(OpticsObjectiveYAngleWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public string ImageDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", nameof(OpticsObjectiveYAngleWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public ApplicationCookie ApplicationCookie => applicationCookie;

    public IHostEnvironment HostEnvironment { get; } = HostApplication.GetRequiredService<IHostEnvironment>();

    [DefaultCache]
    [ObservableProperty]
    public partial OpticsObjectiveYAngleCache Cache { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    [RelayCommand]
    private void Loaded()
    {
        Cache = applicationCookieService.GetOrDefault<OpticsObjectiveYAngleCache>(false);
        MicroscopeCalChip = applicationCookieService.GetCalibration<MicroscopeCalChipDTO>();
    }

    [RelayCommand]
    private void RefreshHazeBFMachinePosition()
    {
        if (MicroscopeCalChip.IsOk)
        {
            Cache.HazeBFMachinePosition = Guard.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition;

            return;
        }

        dialogWindowProvider.ShowDialog("Please Calibrate CalChip First", dialogButtonsEnum: DialogButtonsEnum.OK, dialogIconEnum: DialogIconEnum.Warning);
    }

    [RelayCommand]
    private void RefreshShinyWaferBFMachinePosition()
    {
        if (MicroscopeCalChip.IsOk)
        {
            Cache.ShinyWaferBFMachinePosition = Guard.IsNotNullAndReturn(MicroscopeCalChip.ShinyWaferItem).BrightFieldMachinePosition;

            return;
        }

        dialogWindowProvider.ShowDialog("Please Calibrate CalChip First", dialogButtonsEnum: DialogButtonsEnum.OK, dialogIconEnum: DialogIconEnum.Warning);
    }

    [RelayCommand]
    private void ImportAODWaveformParams()
    {
        try
        {
            var isSuccess = true;

            var stringBuilder = new StringBuilder();

            var productivityInformation = Cache.ShinyWaferProductivityInformation;
            var prescanCache = applicationCookieService.GetOrDefault<PrescanAODWaveformElectrodeDelayCache>(false);
            var prescanResult = prescanCache.Results.SingleOrDefault(t => t.GeneratePrescanAODWaveformParam.ProductivityInformation.OpticsIlluminationModeEnum == productivityInformation.OpticsIlluminationModeEnum
                                                                          && t.GeneratePrescanAODWaveformParam.ProductivityInformation.OpticsMagType == productivityInformation.OpticsMagType);

            if (prescanResult is null)
            {
                stringBuilder.AppendLine("Warning: Prescan AOD Waveform Param No matched found for current Productivity Information!");
                isSuccess = false;
            }
            else
            {
                Cache.GeneratePrescanAODWaveformParam = prescanResult.GeneratePrescanAODWaveformParam.Clone();
                Cache.GeneratePrescanAODWaveformParam.ProductivityInformation = productivityInformation;
                stringBuilder.AppendLine("Ok: Prescan AOD Waveform Param Import Success!");
            }

            var chirpCache = applicationCookieService.GetOrDefault<ChirpAODWaveformElectrodeDelayCache>(false);
            var chirpResult = chirpCache.Results.SingleOrDefault(t => t.GenerateChirpAODWaveformParam.ProductivityInformation.OpticsIlluminationModeEnum == productivityInformation.OpticsIlluminationModeEnum
                                                                      && t.GenerateChirpAODWaveformParam.ProductivityInformation.OpticsMagType == productivityInformation.OpticsMagType);

            if (chirpResult is null)
            {
                stringBuilder.AppendLine("Warning: Chirp AOD Waveform Param No matched found for current Productivity Information!");
                isSuccess = false;
            }
            else
            {
                Cache.GenerateChirpAODWaveformParam = chirpResult.GenerateChirpAODWaveformParam.Clone();
                Cache.GenerateChirpAODWaveformParam.ProductivityInformation = productivityInformation;
                stringBuilder.AppendLine("Ok: Chirp AOD Waveform Param Import Success!");
            }

            dialogWindowProvider.ShowDialog(stringBuilder.ToString(), dialogButtonsEnum: DialogButtonsEnum.OK, dialogIconEnum: isSuccess ? DialogIconEnum.Information : DialogIconEnum.Warning);
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Import Parameters Failed
                                             {ex.Message}
                                             """, dialogButtonsEnum: DialogButtonsEnum.OK, dialogIconEnum: DialogIconEnum.Warning);
            logger.LogError(ex, "Import Parameters Failed");
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step0Async(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            var htmlLogUniqueId = Guid.NewGuid();

            const string stepName = "Y Angle";
            BitmapImage? hazeFourierImage = null;
            BitmapImage? shinyWaferFourierImage = null;
            BitmapImage? drawingImage = null;

            logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, htmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation(stepName, HtmlHeaderLevelEnum.Header2, htmlLogUniqueId.LoggingHtml());

            logger.LogHtmlInformation("Diagnosis Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.RotateAngle,
                Cache.Threshold,
                Cache.CenterChannelLensMaximumAngle
            }), htmlLogUniqueId.LoggingHtml());

            try
            {
                Cache.PrescanAODWaveformResultFilePath = string.Empty;
                Cache.PrescanAODWaveformProfiles = [];
                Cache.ChirpAODWaveformResultFilePath = string.Empty;
                Cache.ChirpAODWaveformProfiles = [];

                Cache.Result.IsOk = false;
                Cache.Result.HazeImageFilePath = string.Empty;
                Cache.Result.ShinyWaferImageFilePath = string.Empty;
                Cache.Result.ResultImageFilePath = string.Empty;
                Cache.Result.HorizontalDegree = 0d;
                Cache.Result.YAngleDegrees = 0d;

                logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Cache.ToHtmlAnonymous()), htmlLogUniqueId.LoggingHtml());

                fourierViewModel.Home(Cache.ChannelId);
                var productivityInformation = Cache.HazeProductivityInformation;
                laserViewModel.ToggleOpticsMagType(productivityInformation);

                if (HostEnvironment.IsDevelopment()) fourierViewModel.UseSimulatorImage(SimulatorImageFilePaths[0]);

                {
                    var brightFieldPosition = SetPosition(Cache.HazeBFMachinePosition, CalChipSiteModelEnum.HazeModel);
                    laserViewModel.SetPrescanAODWaveProfileByCoefficient(productivityInformation, Cache.HazeLaserLightInformation.Coefficient);
                    laserViewModel.SetChirpAODWaveProfile(productivityInformation);

                    hazeFourierImage = fourierViewModel.GetImage(
                        productivityInformation,
                        Cache.HazeLaserLightInformation,
                        brightFieldPosition,
                        Cache.ScanLength,
                        Cache.ChannelId,
                        true);

                    Cache.Result.HazeImageFilePath = SaveImage(hazeFourierImage);

                    logger.LogHtmlInformation("Haze", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        HazeImage = new HtmlImage(Cache.Result.HazeImageFilePath)
                    }), htmlLogUniqueId.LoggingHtml());
                }

                productivityInformation = Cache.ShinyWaferProductivityInformation;
                laserViewModel.ToggleOpticsMagType(productivityInformation);

                if (HostEnvironment.IsDevelopment()) fourierViewModel.UseSimulatorImage(SimulatorImageFilePaths[1]);

                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var brightFieldPosition = SetPosition(Cache.ShinyWaferBFMachinePosition, CalChipSiteModelEnum.ShinyWaferModel);

                    Cache.GeneratePrescanAODWaveformParam.WithFrequencyFlatness(Cache.PrescanFrequency);
                    Cache.GeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
                    Cache.GenerateChirpAODWaveformParam.WithFrequencyFlatness(Cache.ChirpFrequency);
                    Cache.GenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

                    var prescanAODWaveformResult = AODWaveformGenerator1.GeneratePrescanAODWaveform(Cache.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);
                    Cache.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(prescanAODWaveformResult);
                    Cache.PrescanAODWaveformResultFilePath = prescanAODWaveformResult.FilePath;

                    var chirpAODWaveformResult = AODWaveformGenerator1.GenerateChirpAODWaveform(Cache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);
                    Cache.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(chirpAODWaveformResult);
                    Cache.ChirpAODWaveformResultFilePath = chirpAODWaveformResult.FilePath;

                    laserViewModel.SetPrescanAODWaveProfiles(productivityInformation.OpticsIlluminationModeEnum, [.. Cache.PrescanAODWaveformProfiles.Select(t => t.ApplyCoefficient(Cache.ShinyWaferLaserLightInformation.Coefficient))]);
                    laserViewModel.SetChirpAODWaveProfiles(productivityInformation.OpticsIlluminationModeEnum, Cache.ChirpAODWaveformProfiles);

                    shinyWaferFourierImage = fourierViewModel.GetImage(
                        productivityInformation,
                        Cache.ShinyWaferLaserLightInformation,
                        brightFieldPosition,
                        Cache.ScanLength,
                        Cache.ChannelId,
                        true);

                    Cache.Result.ShinyWaferImageFilePath = SaveImage(shinyWaferFourierImage);

                    logger.LogHtmlInformation("ShinyWafer", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        Cache.PrescanAODWaveformResultFilePath,
                        PrescanAODWaveformProfiles = new HtmlTable([.. Cache.PrescanAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())]),
                        Cache.ChirpAODWaveformResultFilePath,
                        ChirpAODWaveformProfiles = new HtmlTable([.. Cache.ChirpAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())]),
                        ShinyWaferImage = new HtmlImage(Cache.Result.ShinyWaferImageFilePath)
                    }), htmlLogUniqueId.LoggingHtml());
                }

                (drawingImage,
                    Cache.Result.CenterChannelLightDiameterPixel,
                    Cache.Result.HorizontalDegree,
                    Cache.Result.CenterChannelLightCenterPosition,
                    Cache.Result.ReflectedLightCenterPosition) = calibrationAlgorithmService.GetOpticsObjectiveYAngleResult(hazeFourierImage, shinyWaferFourierImage, Cache.RotateAngle, htmlLogUniqueId);

                Guard.IsGreaterThan(Cache.Result.CenterChannelLightDiameterPixel, 0d);
                Cache.Result.YAngleDegrees = Cache.Result.LightCenterYPixelOffset * Cache.CenterChannelLensMaximumAngle / Cache.Result.CenterChannelLightDiameterPixel;
                Cache.Result.ResultImageFilePath = SaveImage(drawingImage);
                Cache.Result.IsOk = Math.Abs(Cache.Result.YAngleDegrees) <= Cache.Threshold;

                logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(Cache.Result.ToHtmlAnonymous()), htmlLogUniqueId.LoggingHtml());
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog($"{Name}: {stepName} Canceled", dialogButtonsEnum: DialogButtonsEnum.OK, dialogIconEnum: DialogIconEnum.Warning);
                    logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());

                    return;
                }

                dialogWindowProvider.ShowDialog($"""
                                                 {Name}: {stepName} Failed
                                                 {ex.Message}
                                                 """, dialogButtonsEnum: DialogButtonsEnum.OK, dialogIconEnum: DialogIconEnum.Warning);
                logger.LogHtmlError(ex, "Failed", HtmlHeaderLevelEnum.Header3, htmlLogUniqueId.LoggingHtml());

                return;
            }
            finally
            {
                if (HostEnvironment.IsDevelopment()) fourierViewModel.UseSimulatorImage(null);

                hazeFourierImage?.Dispose();
                shinyWaferFourierImage?.Dispose();
                drawingImage?.Dispose();
                logger.LogHtmlInformation(htmlLogUniqueId.LoggedEndHtml($"{Name}_{stepName}_{(Cache.Result.IsOk ? "OK" : "Failed")}"));
            }

            dialogWindowProvider.ShowDialog($"{Name}: {stepName} {(Cache.Result.IsOk ? "Success" : "Error")}, Result: {Cache.Result.YAngleDegrees:0.###}°", dialogButtonsEnum: DialogButtonsEnum.OK, dialogIconEnum: Cache.Result.IsOk ? DialogIconEnum.Information : DialogIconEnum.Warning);
        }, cancellationToken).ConfigureAwait(false);

        Point SetPosition(Point position, CalChipSiteModelEnum calChipSiteModelEnum)
        {
            var bfPosition = stageViewModel.MachineToBrightFieldPosition(position == Point.Origin ? MicroscopeCalChip.GetBFMachinePosition(calChipSiteModelEnum) : position);
            stageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(bfPosition, calChipSiteModelEnum);
            afViewModel.ToggleDarkFieldEnable(true);

            return bfPosition;
        }

        string SaveImage(BitmapImage image)
        {
            var imageFilePath = Path.Combine(ImageDirectory, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
            DirectoryHelper.CreateFileDirectoryIfNotExists(imageFilePath);
            image.SaveImage(imageFilePath);

            return imageFilePath;
        }
    }

    [RelayCommand]
    private void Close()
    {
        if (Step0Command.IsRunning) Step0CancelCommand.Execute(null);

        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5d));

            applicationCookieService.Set(Cache, false, cancellationToken: cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save cache");
        }

        CloseView(null);
    }
}