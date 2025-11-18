using System.IO;
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
using HalconDotNet;
using Local.NoSQL.DB.Providers.Bases;
using Local.NoSQL.DB.Providers.Extensions;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Optics;

public sealed partial class OpticsObjectiveYAngleCache : ObservableCacheBase
{
    #region Param

    [ObservableProperty]
    private double _prescanFrequency;

    [ObservableProperty]
    private GeneratePrescanAODWaveformParam _generatePrescanAODWaveformParam = new();

    [ObservableProperty]
    private double _chirpFrequency;

    [ObservableProperty]
    private GenerateChirpAODWaveformParam _generateChirpAODWaveformParam = new();

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private int _channelId = CalibrationConstantsHelper.MainChannelId;

    [ObservableProperty]
    private Point _hazeBFMachinePosition = Point.Origin;

    [ObservableProperty]
    private LaserLightInformation _hazeLaserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private Point _shinyWaferBFMachinePosition = Point.Origin;

    [ObservableProperty]
    private LaserLightInformation _shinyWaferLaserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private double _waitTime = 5;

    #endregion

    [ObservableProperty]
    private string _chirpAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];

    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];

    [ObservableProperty]
    private OpticsObjectiveYAngleResult _result = new();

    public object ToHtmlAnonymous() => new
    {
        GeneratePrescanAODWaveformParam = new HtmlQuote(GeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
        GenerateChirpAODWaveformParam = new HtmlQuote(GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
        OpticsMagTypeEnum,
        ChannelId,
        HazeBFMachinePosition,
        HazeLaserLightInformation,
        ShinyWaferBFMachinePosition,
        ShinyWaferLaserLightInformation,
        WaitTime
    };
}

public sealed partial class OpticsObjectiveYAngleResult : ObservableCacheBase
{
    [ObservableProperty]
    private string _hazeImageFilePath = string.Empty;

    [ObservableProperty]
    private string _shinyWaferImageFilePath = string.Empty;

    [ObservableProperty]
    private string _resultImageFilePath = string.Empty;

    [ObservableProperty]
    private double _yAngleDegrees;

    public object ToHtmlAnonymous() => new
    {
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
    IDialogWindowProvider dialogWindowProvider,
    IOptions<ApplicationSetting> options,
    StageViewModel stageViewModel,
    AfViewModel afViewModel,
    LaserViewModel laserViewModel,
    FourierViewModel fourierViewModel,
    ILogger<OpticsObjectiveYAngleWindowViewModel> logger) : ViewModelBase
{
    public string Name => "Optics Y Angle";

    public string AODWaveformDirectoryPath => Path.Combine(options.Value.AppHomeDirectory, nameof(AODWaveform), nameof(OpticsObjectiveYAngleWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public string ImageDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", nameof(OpticsObjectiveYAngleWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public IReadOnlyList<LaserLightInformation> LaserLightInformations => applicationCookie.LaserLightInformations;

    public Guid HtmlLogUniqueId { get; private set; }

    [ObservableProperty]
    private OpticsObjectiveYAngleCache _cache = new();

    [RelayCommand]
    private void Loaded()
    {
        Cache = cacheProvider.GetOrDefault<OpticsObjectiveYAngleCache>();
    }

    [RelayCommand]
    private void RefreshBFMachinePosition()
    {
        if (cacheProvider.TryGetOrDefault<MicroscopeCalChipDto>(out var microscopeCalChip))
        {
            if (microscopeCalChip.IsOk)
            {
                Cache.HazeBFMachinePosition = microscopeCalChip.HazeBrightFieldMachinePosition;
                Cache.ShinyWaferBFMachinePosition = microscopeCalChip.ShinyWaferBrightFieldMachinePosition;

                return;
            }
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

            var isSuccess = false;
            HImage? hazeFourierImage = null;
            HImage? shinyWaferFourierImage = null;
            try
            {
                Cache.PrescanAODWaveformResultFilePath = string.Empty;
                Cache.PrescanAODWaveformProfiles = [];
                Cache.ChirpAODWaveformResultFilePath = string.Empty;
                Cache.ChirpAODWaveformProfiles = [];
                Cache.Result.HazeImageFilePath = string.Empty;
                Cache.Result.ShinyWaferImageFilePath = string.Empty;
                Cache.Result.YAngleDegrees = 0d;
                Cache.GeneratePrescanAODWaveformParam.WithFrequencyFlatness(Cache.PrescanFrequency);
                Cache.GenerateChirpAODWaveformParam.WithFrequencyFlatness(Cache.ChirpFrequency);

                logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Cache.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

                laserViewModel.ToggleOpticsMagType(Cache.OpticsMagTypeEnum);
                laserViewModel.ToggleEnableAutoGainControl(true);

                try
                {
                    stageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(stageViewModel.MachineToBrightFieldPosition(Cache.HazeBFMachinePosition));
                    afViewModel.ToggleDarkFieldEnable(true);
                    laserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.OpticsMagTypeEnum, Cache.HazeLaserLightInformation.Coefficient);
                    laserViewModel.SetChirpAODWaveProfile(Cache.OpticsMagTypeEnum);
                    laserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

                    await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);
                    hazeFourierImage = fourierViewModel.GetFourierImage(Cache.ChannelId);

                    var resultHazeImageFilePath = Path.Combine(ImageDirectory, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                    hazeFourierImage.Save(resultHazeImageFilePath);
                    Cache.Result.HazeImageFilePath = resultHazeImageFilePath;
                }
                finally
                {
                    laserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
                }

                try
                {
                    stageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(stageViewModel.MachineToBrightFieldPosition(Cache.ShinyWaferBFMachinePosition));
                    afViewModel.ToggleDarkFieldEnable(true);

                    Cache.GeneratePrescanAODWaveformParam.OpticsMagTypeEnum = Cache.OpticsMagTypeEnum;
                    Cache.GeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
                    var (prescanAODWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);
                    if (prescanAODWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));
                    Cache.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(prescanAODWaveformResult);
                    Cache.PrescanAODWaveformResultFilePath = prescanAODWaveformResult.FilePath;

                    Cache.GenerateChirpAODWaveformParam.OpticsMagTypeEnum = Cache.OpticsMagTypeEnum;
                    Cache.GenerateChirpAODWaveformParam.WithFrequencyFlatness(Cache.ChirpFrequency);
                    Cache.GenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
                    (var chirpAODWaveformResult, exception) = AODWaveformGenerator.GenerateChirpAODWaveform(Cache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);
                    if (chirpAODWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));
                    Cache.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(chirpAODWaveformResult);
                    Cache.ChirpAODWaveformResultFilePath = chirpAODWaveformResult.FilePath;

                    laserViewModel.SetPrescanAODWaveProfiles([..Cache.PrescanAODWaveformProfiles.Select(t => t.ApplyCoefficient(Cache.ShinyWaferLaserLightInformation.Coefficient))]);
                    laserViewModel.SetChirpAODWaveProfiles(Cache.ChirpAODWaveformProfiles);
                    laserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

                    await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);
                    shinyWaferFourierImage = fourierViewModel.GetFourierImage(Cache.ChannelId);

                    var resultShinyWaferImageFilePath = Path.Combine(ImageDirectory, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                    shinyWaferFourierImage.Save(resultShinyWaferImageFilePath);
                    Cache.Result.ShinyWaferImageFilePath = resultShinyWaferImageFilePath;
                }
                finally
                {
                    laserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
                }

                var yAngleDregress = calibrationAlgorithmService.GetOpticsObjectiveYAngleDregress(hazeFourierImage, shinyWaferFourierImage, out var drawingImage);

                Cache.Result.YAngleDegrees = yAngleDregress;
                var resultImageFilePath = Path.Combine(ImageDirectory, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                drawingImage.Save(resultImageFilePath);
                Cache.Result.ResultImageFilePath = resultImageFilePath;

                isSuccess = true;
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog($"{Name}: {stepName} Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    return false;
                }

                dialogWindowProvider.ShowDialog($"""
                                                 {Name}: {stepName} Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogHtmlError(ex, "Failed", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            }
            finally
            {
                hazeFourierImage?.Dispose();
                shinyWaferFourierImage?.Dispose();

                logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{Name}_{stepName}_{(isSuccess ? "OK" : "Failed")}"));
            }

            if (isSuccess)
                dialogWindowProvider.ShowDialog($"{Name}: {stepName} Success");
            else
                dialogWindowProvider.ShowDialog($"{Name}: {stepName} Error", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return isSuccess;
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