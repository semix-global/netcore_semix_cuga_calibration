using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.CalChip;
using Core.Services.Interfaces;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Bases;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Optics;

public sealed partial class OpticsObjectiveYAngleCache : ObservableCacheBase
{
    #region Param

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial int ChannelId { get; set; } = CalibrationConstantsHelper.MainChannelId;

    [ObservableProperty]
    public partial double PrescanFrequency { get; set; }

    [ObservableProperty]
    public partial GeneratePrescanAODWaveformParam GeneratePrescanAODWaveformParam { get; set; } = new();

    [ObservableProperty]
    public partial double ChirpFrequency { get; set; }

    [ObservableProperty]
    public partial GenerateChirpAODWaveformParam GenerateChirpAODWaveformParam { get; set; } = new();

    [ObservableProperty]
    public partial Point HazeBFMachinePosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial LaserLightInformation HazeLaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial Point ShinyWaferBFMachinePosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial LaserLightInformation ShinyWaferLaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial double WaitTime { get; set; } = 5;

    [ObservableProperty]
    public partial double RotateAngle { get; set; }

    /// <summary>
    /// D型光斑直径(um)
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DegreePerUm))]
    public partial double CenterChannelLensDiameterUm { get; set; } = 33_000;

    /// <summary>
    /// 最大半角度(°)
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DegreePerUm))]
    public partial double CenterChannelLensMaximumAngle { get; set; } = 41.3;

    public double DegreePerUm => CenterChannelLensMaximumAngle / CenterChannelLensDiameterUm;

    [ObservableProperty]
    public partial double Threshold { get; set; } = 2;

    #endregion

    [ObservableProperty]
    public partial string ChirpAODWaveformResultFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<PrescanAODWaveformProfile> PrescanAODWaveformProfiles { get; set; } = [];

    [ObservableProperty]
    public partial string PrescanAODWaveformResultFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<ChirpAODWaveformProfile> ChirpAODWaveformProfiles { get; set; } = [];

    [ObservableProperty]
    public partial OpticsObjectiveYAngleResult Result { get; set; } = new();

    public object ToHtmlAnonymous() => new
    {
        ProductivityInformation,
        ChannelId,
        GeneratePrescanAODWaveformParam = new HtmlQuote(GeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
        GenerateChirpAODWaveformParam = new HtmlQuote(GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
        HazeBFMachinePosition,
        HazeLaserLightInformation,
        ShinyWaferBFMachinePosition,
        ShinyWaferLaserLightInformation,
        WaitTime,
        Threshold
    };
}

public sealed partial class OpticsObjectiveYAngleResult : ObservableObject
{
    [ObservableProperty]
    public partial bool IsOk { get; set; }

    [ObservableProperty]
    public partial string HazeImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ShinyWaferImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ResultImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Point CenterChannelLightCenterPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point ReflectedLightCenterPosition { get; set; } = Point.Origin;

    public double LightCenterXPixelOffset => ReflectedLightCenterPosition.X - CenterChannelLightCenterPosition.X;

    [ObservableProperty]
    public partial double CenterChannelLightDiameterPixel { get; set; }

    [ObservableProperty]
    public partial double HorizontalDegree { get; set; }

    [ObservableProperty]
    public partial double PixelSize { get; set; }

    [ObservableProperty]
    public partial double YAngleDegrees { get; set; }

    public object ToHtmlAnonymous() => new
    {
        IsOk,
        CenterChannelLightCenterPosition,
        ReflectedLightCenterPosition,
        LightCenterXPixelOffset = Math.Round(LightCenterXPixelOffset, 4),
        HorizontalDegree = Math.Round(HorizontalDegree, 4),
        CenterChannelLightDiameterPixel,
        PixelSize = Math.Round(PixelSize, 4),
        HazeImage = new HtmlImage(HazeImageFilePath),
        ShinyWaferImage = new HtmlImage(ShinyWaferImageFilePath),
        ResultImageh = new HtmlImage(ResultImageFilePath),
        YAngleDegrees
    };
}

[IOCAppService(ServiceType = typeof(OpticsObjectiveYAngleWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsObjectiveYAngleWindowViewModel(
    ApplicationCookie applicationCookie,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ICacheProvider cacheProvider,
    IApplicationCookieService applicationCookieService,
    IDialogWindowProvider dialogWindowProvider,
    IOptions<ApplicationSetting> options,
    StageViewModel stageViewModel,
    AfViewModel afViewModel,
    LaserViewModel laserViewModel,
    CIBViewModel cibViewModel,
    FourierViewModel fourierViewModel,
    ILogger<OpticsObjectiveYAngleWindowViewModel> logger) : ViewModelBase
{
    public string Name => "Optics Y Angle";

    public string AODWaveformDirectoryPath => Path.Combine(options.Value.AppHomeDirectory, nameof(AODWaveform), nameof(OpticsObjectiveYAngleWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public string ImageDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", nameof(OpticsObjectiveYAngleWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public ApplicationCookie ApplicationCookie => applicationCookie;

    public Guid HtmlLogUniqueId { get; private set; }

    [DefaultCache]
    [ObservableProperty]
    public partial OpticsObjectiveYAngleCache Cache { get; set; } = new();

    [RelayCommand]
    private void Loaded()
    {
        Cache = cacheProvider.GetOrDefault<OpticsObjectiveYAngleCache>();
    }

    [RelayCommand]
    private void RefreshBFMachinePosition()
    {
        var microscopeCalChip = applicationCookieService.GetCalibration<MicroscopeCalChipDTO>();

        if (microscopeCalChip.IsOk)
        {
            Cache.HazeBFMachinePosition = Guard.IsNotNullAndReturn(microscopeCalChip.HazeItem).BrightFieldMachinePosition;
            Cache.ShinyWaferBFMachinePosition = Guard.IsNotNullAndReturn(microscopeCalChip.ShinyWaferItem).BrightFieldMachinePosition;

            return;
        }

        dialogWindowProvider.ShowDialog("Please Calibrate CalChip First", DialogButtonsEnum.OK, DialogIconEnum.Warning);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step0Async(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            HtmlLogUniqueId = Guid.NewGuid();

            const string stepName = "Y Angle";

            logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation(stepName, HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

            logger.LogHtmlInformation("Diagnosis Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.RotateAngle,
                Cache.Threshold,
                Cache.CenterChannelLensMaximumAngle,
                Cache.CenterChannelLensDiameterUm,
                Cache.DegreePerUm
            }), HtmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            BitmapImage? hazeFourierImage = null;
            BitmapImage? shinyWaferFourierImage = null;
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
                Cache.Result.YAngleDegrees = 0d;
                Cache.GeneratePrescanAODWaveformParam.WithFrequencyFlatness(Cache.PrescanFrequency);
                Cache.GenerateChirpAODWaveformParam.WithFrequencyFlatness(Cache.ChirpFrequency);

                logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Cache.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

                laserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
                cibViewModel.SetAGC(ApplicationCookie.CIBInformations, true);

                try
                {
                    stageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(stageViewModel.MachineToBrightFieldPosition(Cache.HazeBFMachinePosition));
                    afViewModel.ToggleDarkFieldEnable(true);
                    laserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, Cache.HazeLaserLightInformation.Coefficient);
                    laserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);
                    laserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

                    await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);
                    hazeFourierImage = fourierViewModel.GetFourierImage(Cache.ChannelId);

                    var resultHazeImageFilePath = Path.Combine(ImageDirectory, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                    hazeFourierImage.SaveImage(resultHazeImageFilePath);
                    Cache.Result.HazeImageFilePath = resultHazeImageFilePath;

                    logger.LogHtmlInformation("Haze", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        HazeImage = new HtmlImage(Cache.Result.HazeImageFilePath)
                    }), HtmlLogUniqueId.LoggingHtml());
                }
                finally
                {
                    laserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
                }

                try
                {
                    stageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(stageViewModel.MachineToBrightFieldPosition(Cache.ShinyWaferBFMachinePosition));
                    afViewModel.ToggleDarkFieldEnable(true);

                    Cache.GeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
                    Cache.GeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
                    var prescanAODWaveformResult = AODWaveformGenerator1.GeneratePrescanAODWaveform(Cache.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);
                    Cache.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(prescanAODWaveformResult);
                    Cache.PrescanAODWaveformResultFilePath = prescanAODWaveformResult.FilePath;

                    Cache.GenerateChirpAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
                    Cache.GenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
                    var chirpAODWaveformResult = AODWaveformGenerator1.GenerateChirpAODWaveform(Cache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);
                    Cache.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(chirpAODWaveformResult);
                    Cache.ChirpAODWaveformResultFilePath = chirpAODWaveformResult.FilePath;

                    laserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, [.. Cache.PrescanAODWaveformProfiles.Select(t => t.ApplyCoefficient(Cache.ShinyWaferLaserLightInformation.Coefficient))]);
                    laserViewModel.SetChirpAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, Cache.ChirpAODWaveformProfiles);
                    laserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

                    await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);
                    shinyWaferFourierImage = fourierViewModel.GetFourierImage(Cache.ChannelId);

                    var resultShinyWaferImageFilePath = Path.Combine(ImageDirectory, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                    shinyWaferFourierImage.SaveImage(resultShinyWaferImageFilePath);
                    Cache.Result.ShinyWaferImageFilePath = resultShinyWaferImageFilePath;

                    logger.LogHtmlInformation("ShinyWafer", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        Cache.PrescanAODWaveformResultFilePath,
                        PrescanAODWaveformProfiles = new HtmlTable([.. Cache.PrescanAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())]),
                        Cache.ChirpAODWaveformResultFilePath,
                        ChirpAODWaveformProfiles = new HtmlTable([.. Cache.ChirpAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())]),
                        ShinyWaferImage = new HtmlImage(Cache.Result.ShinyWaferImageFilePath)
                    }), HtmlLogUniqueId.LoggingHtml());
                }
                finally
                {
                    laserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
                }

                var (drawingImage,
                    diameter,
                    horizontalDegree,
                    centerChannelLightCenterPosition,
                    reflectedLightCenterPosition) = calibrationAlgorithmService.GetOpticsObjectiveYAngleResult(hazeFourierImage, shinyWaferFourierImage, Cache.RotateAngle);
                Cache.Result.CenterChannelLightCenterPosition = centerChannelLightCenterPosition;
                Cache.Result.ReflectedLightCenterPosition = reflectedLightCenterPosition;
                Cache.Result.HorizontalDegree = horizontalDegree;
                Cache.Result.CenterChannelLightDiameterPixel = diameter;
                Cache.Result.PixelSize = Cache.CenterChannelLensDiameterUm / diameter;
                Cache.Result.YAngleDegrees = (reflectedLightCenterPosition.X - centerChannelLightCenterPosition.X) * Cache.Result.PixelSize * Cache.DegreePerUm;

                var resultImageFilePath = Path.Combine(ImageDirectory, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                using var _ = drawingImage;
                drawingImage.SaveImage(resultImageFilePath);
                Cache.Result.ResultImageFilePath = resultImageFilePath;

                logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(Cache.Result.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

                Cache.Result.IsOk = Math.Abs(Cache.Result.YAngleDegrees) <= Cache.Threshold;

                isSuccess = Cache.Result.IsOk;
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog($"{Name}: {stepName} Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    return;
                }

                dialogWindowProvider.ShowDialog($"""
                                                 {Name}: {stepName} Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogHtmlError(ex, "Failed", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                return;
            }
            finally
            {
                hazeFourierImage?.Dispose();
                shinyWaferFourierImage?.Dispose();
                logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{Name}_{stepName}_{(isSuccess ? "OK" : "Failed")}"));
            }

            if (isSuccess)
                dialogWindowProvider.ShowDialog($"{Name}: {stepName} Success, Result: {Cache.Result.YAngleDegrees:0.###}°");
            else
                dialogWindowProvider.ShowDialog($"{Name}: {stepName} Error, Result: {Cache.Result.YAngleDegrees:0.###}°", DialogButtonsEnum.OK, DialogIconEnum.Warning);
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

        CloseView(null);
    }
}