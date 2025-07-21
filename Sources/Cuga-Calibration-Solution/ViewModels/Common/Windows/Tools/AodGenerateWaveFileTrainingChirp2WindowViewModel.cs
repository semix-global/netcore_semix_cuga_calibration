using CommunityToolkit.Common;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Core.Utilities;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Complex = System.Numerics.Complex;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(AodGenerateWaveFileTrainingChirp2WindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AodGenerateWaveFileTrainingChirp2WindowViewModel(
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    CreateRoiWindowViewModel createRoiWindowViewModel,
    StageViewModel stageViewModel,
    LaserViewModel laserViewModel,
    ConfigViewModel configViewModel,
    AfViewModel afViewModel,
    IOptions<ApplicationSetting> options,
    CalibrationSetting calibrationSetting,
    ILogger<AodGenerateWaveFileTrainingChirp2WindowViewModel> logger) : ViewModelBase
{
    public string ImageDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", DirectoryHelper.RemoveInvalidDirectoryName(nameof(AodGenerateWaveFileTrainingChirp2WindowViewModel)), DateTime.Now.ToString(Constants.MiddleFileDateTimeFormat));

    public string AodWaveDirectory => Path.Combine(options.Value.AppHomeDirectory, "Chirp", DirectoryHelper.RemoveInvalidDirectoryName(nameof(AodGenerateWaveFileTrainingChirp2WindowViewModel)), DateTime.Now.ToString(Constants.MiddleFileDateTimeFormat));

    #region 0. 确认生成波形参数

    [ObservableProperty]
    public partial GenerateChirpAodWaveParamDto GenerateChirpAodWaveParamDto { get; set; } = new()
    {
        HeaderFrequency = 275,
        FooterFrequency = 155
    };

    #endregion 0. 确认生成波形参数

    #region 1. 确认ROI范围用来寻找最大灰阶值

    [ObservableProperty]
    public partial int PmtId { get; set; } = 8;

    [ObservableProperty]
    public partial int ChannelId { get; set; } = 3;

    [ObservableProperty]
    public partial Point FindPosition { get; set; }

    [ObservableProperty]
    public partial int XWidthPixel { get; set; } = 800;

    [ObservableProperty]
    public partial OpticsMagTypeEnum OpticsMagTypeEnum { get; set; } = OpticsMagTypeEnum.High;

    [ObservableProperty]
    public partial StageSpeedEnum StageSpeedEnum { get; set; } = StageSpeedEnum.Low;

    [ObservableProperty]
    public partial Rect RoiRect { get; set; } = new(0, 0, 256, 256);

    #endregion 1. 确认ROI范围用来寻找最大灰阶值

    #region 2. 补偿训练

    [ObservableProperty]
    public partial TrainingAlgorithmEnum TrainingAlgorithmEnum { get; set; } = TrainingAlgorithmEnum.Mtf;

    [ObservableProperty]
    public partial double XPixelSize { get; set; }

    [ObservableProperty]
    public partial double YPixelSize { get; set; }

    [ObservableProperty]
    public partial double PotDiameter { get; set; }

    [ObservableProperty]
    public partial double XPointDiameter { get; set; }

    [ObservableProperty]
    public partial double YPointDiameter { get; set; }

    [ObservableProperty]
    public partial double PrescanCoefficient { get; set; } = calibrationSetting.SettingCommonParam.MainCoefficient;

    [ObservableProperty]
    public partial ObservableCollection<DeltaKItem> DeltaKItems { get; set; } = [];

    #endregion 2. 补偿训练

    [ObservableProperty]
    public partial AodGenerateWaveFileTrainingChirp2? SelectItem { get; set; }

    [ObservableProperty]
    public partial AodGenerateWaveFileTrainingChirp2[] Items { get; set; } = [];

    [ObservableProperty]
    public partial Point[] ItemsXPoints { get; set; } = [];

    [ObservableProperty]
    public partial Point[] ItemsYPoints { get; set; } = [];

    [RelayCommand]
    private async Task Step1Async()
    {
        var htmlGuid = Guid.NewGuid();
        var isSuccess = false;
        try
        {
            await Task.Run(() =>
            {
                FindPosition = stageViewModel.GetBrightFieldStagePosition();
                if (calibrationSetting.HighMagSettingDarkFieldAutoFocusParam.IsEnableChuck == false) ThrowHelper.ThrowInvalidOperationException("Dark Field Auto Focus is not enabled for Chuck model");
                if (calibrationSetting.HighMagSettingDarkFieldAutoFocusParam.IsEnableDsw == false) ThrowHelper.ThrowInvalidOperationException("Dark Field Auto Focus is not enabled for DSW model");

                afViewModel.SetDarkFieldAutoFocus(null, OpticsMagTypeEnum.High, CalChipSiteModelEnum.DswModel);

                var offset = calibrationSetting.HighMagSettingDarkFieldAutoFocusParam.DswMotorValue - calibrationSetting.HighMagSettingDarkFieldAutoFocusParam.ChuckMotorValue;
                var (ecs, afMotor) = laserViewModel.RuntimeAfCalibration(FindPosition, null, CalChipSiteModelEnum.DswModel);
                calibrationSetting.HighMagSettingDarkFieldAutoFocusParam.DswEcsValue = ecs;
                calibrationSetting.HighMagSettingDarkFieldAutoFocusParam.DswMotorValue = afMotor;

                var darkFieldImageDto = laserViewModel.GetDarkFieldLineScanImage(
                    CalChipSiteModelEnum.DswModel,
                    FindPosition,
                    (false, calibrationSetting.SettingCommonParam.MainCoefficient),
                    false,
                    null,
                    XWidthPixel,
                    OpticsMagTypeEnum,
                    StageSpeedEnum,
                    stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright,
                    pmtId: PmtId,
                    channelId: ChannelId);
                var detectImageDirectory = ImageDirectory;
                using var _ = darkFieldImageDto;

                var filePath = $"{detectImageDirectory}\\{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg";
                HalconHelper.Save(darkFieldImageDto.Image, filePath);
                createRoiWindowViewModel.ImageFilePath = filePath;

                var showDialog = windowManagerService.ShowDialog(createRoiWindowViewModel);

                if (showDialog == false)
                {
                    dialogWindowProvider.ShowDialog("Generate ROI Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                RoiRect = createRoiWindowViewModel.Rect;

                logger.LogHtmlInformation("Create ROI", HtmlHeaderLevelEnum.Header1, new HtmlBullet(new
                {
                    RTFCECS = ecs,
                    RTFAfMotorHeight = afMotor,
                    calibrationSetting.SettingCommonParam.MainCoefficient,
                    FindPosition,
                    XWidthPixel,
                    OpticsMagTypeEnum,
                    StageSpeedEnum,
                    RoiRect,
                    Image = new HtmlImage(filePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(RoiRect)])
                }), htmlGuid.LoggingHtml());

                isSuccess = true;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                logger.LogWarning("Canceled");
                return;
            }

            logger.LogError(ex, "Step1");
        }
        finally
        {
            logger.LogHtmlInformation(htmlGuid.LoggedEndHtml($"Step1CreateROI{(isSuccess ? "OK" : "Error")}"));
        }
    }

    [RelayCommand]
    private void AddDeltaKItem()
    {
        var newItem = new DeltaKItem();
        DeltaKItems.Add(newItem);
    }

    [RelayCommand]
    private void RemoveDeltaKItem(DeltaKItem item)
    {
        if (DeltaKItems.Contains(item) == false) return;

        DeltaKItems.Remove(item);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step3Async(CancellationToken cancellationToken)
    {
        var htmlGuid = Guid.NewGuid();
        var isSuccess = false;
        try
        {
            var imageDirectory = ImageDirectory;
            var aodWaveDirectory = AodWaveDirectory;

            Items = [];

            var deltaKs = DeltaKItems.Select(t => t.IsOk ? t.DeltaKValue : 0d).ToArray();
            if (deltaKs.Length == 0)
            {
                dialogWindowProvider.ShowDialog("Delta K Items is empty", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            logger.LogHtmlInformation("Training", HtmlHeaderLevelEnum.Header1, new HtmlBullet(new { DefaultDeltaKs = deltaKs }), htmlGuid.LoggingHtml());

            foreach (var (index, deltaKItem) in DeltaKItems.Select((deltaKItem, index) => (index, deltaKItem)))
            {
                if (deltaKItem.IsOk) continue;

                cancellationToken.ThrowIfCancellationRequested();

                Items =
                [
                    .. Generate.LinearRange(deltaKItem.DeltaKMin, deltaKItem.DeltaKStep, deltaKItem.DeltaKMax).Select(t =>
                    {
                        deltaKs[index] = t;
                        return new AodGenerateWaveFileTrainingChirp2
                        {
                            DeltaKs = [.. deltaKs]
                        };
                    })
                ];

                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                logger.LogHtmlInformation($"segmentation: {index + 1}", HtmlHeaderLevelEnum.Header2, htmlGuid.LoggingHtml());

                ItemsXPoints = [];
                ItemsYPoints = [];
                foreach (var item in Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SelectItem = item;
                    if (await CatchImagesAsync(item, imageDirectory, aodWaveDirectory, htmlGuid, cancellationToken).ConfigureAwait(false) == false) continue;

                    item.IsOk = true;
                    ItemsXPoints = [.. ItemsXPoints, new Point(item.DeltaKs[index], item.TargetValueY)];
                    ItemsYPoints = [.. ItemsYPoints, new Point(item.DeltaKs[index], item.TargetValueY)];
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }

                var maxItem = TrainingAlgorithmEnum switch
                {
                    TrainingAlgorithmEnum.Mtf => Items.OrderByDescending(t => t.TargetValueY).First(),
                    TrainingAlgorithmEnum.LightQuality => Items.OrderBy(t => t.TargetValueY).First(),
                    TrainingAlgorithmEnum.StrehlRatio => Items.OrderByDescending(t => t.TargetValueY).First(),
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<AodGenerateWaveFileTrainingChirp2>(nameof(TrainingAlgorithmEnum))
                };

                DeltaKItems[index].DeltaKValue = deltaKs[index] = maxItem.DeltaKs[index];

                logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    BestDeltaKs = deltaKs[index],
                    BestTargetValueY = maxItem.TargetValueY,
                    BestTargetValueX = maxItem.TargetValueX,
                    Table = new HtmlTable([
                        ..Items.Select(t => new
                        {
                            MaxDeltaKs = t.DeltaKs[index],
                            t.ChirpAodWaveFilePath,
                            Signals = new HtmlPlot2DLinesChart([(string.Empty, t.AodWaveSignals)], string.Empty),
                            Fouriers = new HtmlPlot2DLinesChart([(string.Empty, t.AodWaveSignalsFourier)], string.Empty)
                        })
                    ]),
                    ItemsXPoints = new HtmlPlot2DLinesChart([(string.Empty, ItemsXPoints)], string.Empty),
                    ItemsYPoints = new HtmlPlot2DLinesChart([(string.Empty, ItemsYPoints)], string.Empty)
                }), htmlGuid.LoggingHtml());
            }

            stageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(FindPosition);
            isSuccess = true;
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                logger.LogWarning("Canceled");
                return;
            }

            logger.LogError(ex, "Step3");
        }
        finally
        {
            logger.LogHtmlInformation(htmlGuid.LoggedEndHtml($"Step3Training{(isSuccess ? "OK" : "Error")}"));
        }
    }

    private async Task<bool> CatchImagesAsync(AodGenerateWaveFileTrainingChirp2 item, string imageDirectory, string aodWaveDirectory, Guid htmlGuid, CancellationToken cancellationToken)
    {
        try
        {
            return await Task.Run(() =>
            {
                if (GenerateChirpAodWaveFile() == false) return false;

                logger.LogHtmlInformation($"{item.DeltaKs.ToArrayString()}", HtmlHeaderLevelEnum.Header3, htmlGuid.LoggingHtml());
                logger.LogHtmlInformation("Aod Wave", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    GenerateChirpAodWaveParamDto.BandWidth,
                    GenerateChirpAodWaveParamDto.CenterFrequency,
                    GenerateChirpAodWaveParamDto.SoundPackageLength,
                    MonotonicTypeEnum = GenerateChirpAodWaveParamDto.FunctionMonotonicTypeEnum,
                    GenerateChirpAodWaveParamDto.SampleRate,
                    GenerateChirpAodWaveParamDto.Amplitude,
                    GenerateChirpAodWaveParamDto.AodWaveDirectory,
                    GenerateChirpAodWaveParamDto.ZeroSampleCount,
                    GenerateChirpAodWaveParamDto.EndpointSampleCount,
                    item.DeltaKs,
                    Signals = new HtmlTab(new
                    {
                        AodWaveSignals = new HtmlPlot2DLinesChart([(string.Empty, item.AodWaveSignals)], string.Empty),
                        AodWaveSignalsFourier = new HtmlPlot2DLinesChart([(string.Empty, item.AodWaveSignalsFourier)], string.Empty),
                        AodWaveFlatnessTotalFrequencySignals = new HtmlPlot2DLinesChart([(string.Empty, item.AodWaveFlatnessTotalFrequencySignals)], string.Empty)
                    })
                }), htmlGuid.LoggingHtml());

                cancellationToken.ThrowIfCancellationRequested();
                laserViewModel.SendPrescanByList(laserViewModel.ReadPrescanByFile(configViewModel.GetPrescanFilePath(OpticsMagTypeEnum), PrescanCoefficient));
                laserViewModel.SendChirpAodByList(laserViewModel.ReadChirpAodByConfigFile(item.ChirpAodWaveFilePath));

                using var darkFieldImageDto = laserViewModel.GetDarkFieldLineScanImage(
                    CalChipSiteModelEnum.DswModel,
                    FindPosition,
                    (true, null),
                    true,
                    null,
                    XWidthPixel,
                    OpticsMagTypeEnum,
                    StageSpeedEnum,
                    stageCoordinateSystemEnum: StageCoordinateSystemEnum.Dark,
                    pmtId: PmtId,
                    channelId: ChannelId);
                var filePath = $"{imageDirectory}\\{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}" +
                               $"_{string.Join(",", item.DeltaKs)}" +
                               $".jpg";
                filePath = FileHelper.GetEnsureLongPathSupport(filePath);
                HalconHelper.Save(darkFieldImageDto.Image, filePath);

                item.ImageFilePath = filePath;

                switch (TrainingAlgorithmEnum)
                {
                    case TrainingAlgorithmEnum.Mtf:
                        var (mtfX, mtfY) = calibrationAlgorithmService.ModulationTransferFunction(darkFieldImageDto.Image, RoiRect);
                        item.TargetValueX = mtfX;
                        item.TargetValueY = mtfY;

                        break;

                    case TrainingAlgorithmEnum.LightQuality:
                        var (width, height) = calibrationAlgorithmService.GetLightQuality(darkFieldImageDto.Image, RoiRect);
                        item.TargetValueX = width;
                        item.TargetValueY = height;

                        break;

                    case TrainingAlgorithmEnum.StrehlRatio:
                        var ((xStrehlRatio, xLine, xFitLine), (yStrehlRatio, yLine, yFitLine)) = StrehlRatioUtility.GetStrehlRatio(darkFieldImageDto.Matrix, RoiRect, XPixelSize, YPixelSize, PotDiameter, XPointDiameter, YPointDiameter);
                        item.TargetValueX = xStrehlRatio;
                        item.TargetValueY = yStrehlRatio;
                        logger.LogHtmlInformation("StrehlRatio", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            xFitLine = new HtmlPlot2DLinesChart([(nameof(xFitLine), xFitLine.ToPoints()), (nameof(xLine), xLine.ToPoints())], string.Empty),
                            yFitLine = new HtmlPlot2DLinesChart([(nameof(yFitLine), yFitLine.ToPoints()), (nameof(yLine), yLine.ToPoints())], string.Empty)
                        }), htmlGuid.LoggingHtml());

                        break;

                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(TrainingAlgorithmEnum));
                        break;
                }

                logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    OpticsMagTypeEnum,
                    PrescanCoefficient,
                    FindPosition,
                    XWidthPixel,
                    StageSpeedEnum,
                    TrainingAlgorithmEnum = EnumHelper.ToDescriptionString(TrainingAlgorithmEnum),
                    item.ChirpAodWaveFilePath,
                    item.TargetValueX,
                    item.TargetValueY,
                    Image = new HtmlImage(item.ImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(RoiRect)]),
                    RawImageFile = new HtmlDownload(darkFieldImageDto.Bytes, $"{Path.GetFileName(item.ImageFilePath)}.raw"),
                }), htmlGuid.LoggingHtml());

                return true;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException) throw;

            logger.LogHtmlError(ex, "Error", HtmlHeaderLevelEnum.Header3, htmlGuid.LoggingHtml());

            return false;
        }

        bool GenerateChirpAodWaveFile()
        {
            try
            {
                var (aodWaveFilePath,
                    aodWaveFlatnessTotalFrequencySignals,
                    aodWaveSignals,
                    aodWaveSignalsFourier) = GenerateChirpAodWave.GenerateChirpAodWaveFile(
                    GenerateChirpAodWaveParamDto.BandWidth,
                    GenerateChirpAodWaveParamDto.CenterFrequency,
                    GenerateChirpAodWaveParamDto.SoundPackageLength,
                    GenerateChirpAodWaveParamDto.FunctionMonotonicTypeEnum,
                    GenerateChirpAodWaveParamDto.SampleRate,
                    GenerateChirpAodWaveParamDto.Amplitude,
                    aodWaveDirectory,
                    zeroSampleCount: GenerateChirpAodWaveParamDto.ZeroSampleCount,
                    endpointSampleCount: GenerateChirpAodWaveParamDto.EndpointSampleCount,
                    deltaKs: item.DeltaKs,
                    generateRetryTimes: 1000);

                item.ChirpAodWaveFilePath = aodWaveFilePath;
                item.AodWaveFlatnessTotalFrequencySignals = aodWaveFlatnessTotalFrequencySignals;
                item.AodWaveSignals = aodWaveSignals;
                item.AodWaveSignalsFourier = aodWaveSignalsFourier;

                return true;
            }
            catch (Exception ex)
            {
                logger.LogHtmlError(ex, "Generate Aod Wave Error", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    item.DeltaKs
                }), htmlGuid.LoggingHtml());

                return false;
            }
        }
    }

    [RelayCommand]
    private void Close() => CloseView(true);
}

public enum TrainingAlgorithmEnum
{
    [Description("MTF(X, Y)")]
    Mtf,

    [Description("LightQuality(Width, Height)")]
    LightQuality,

    [Description("StrehlRatio(X, Y)")]
    StrehlRatio
}

public sealed partial class DeltaKItem : ObservableObject
{
    [ObservableProperty]
    public partial double DeltaKMin { get; set; }

    [ObservableProperty]
    public partial double DeltaKMax { get; set; }

    [ObservableProperty]
    public partial double DeltaKStep { get; set; }

    [ObservableProperty]
    public partial bool IsOk { get; set; } = true;

    [ObservableProperty]
    public partial double DeltaKValue { get; set; }
}

public sealed partial class AodGenerateWaveFileTrainingChirp2 : ObservableObject
{
    [ObservableProperty]
    public partial string ChirpAodWaveFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double[] DeltaKs { get; set; } = [];

    [ObservableProperty]
    public partial Point[] AodWaveFlatnessTotalFrequencySignals { get; set; } = [];

    [ObservableProperty]
    public partial Point[] AodWaveSignals { get; set; } = [];

    [ObservableProperty]
    public partial Point[] AodWaveSignalsFourier { get; set; } = [];

    [ObservableProperty]
    public partial string ImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double TargetValueX { get; set; }

    [ObservableProperty]
    public partial double TargetValueY { get; set; }

    [ObservableProperty]
    public partial bool IsOk { get; set; }
}

public static class GenerateChirpAodWave
{
    public static (
        string AodWaveFilePath,
        Point[] AodWaveFlatnessTotalFrequencySignals,
        Point[] AodWaveSignals,
        Point[] AodWaveSignalsFourier) GenerateChirpAodWaveFile(
            double bandWidth,
            double centerFrequency,
            double soundPacketLength,
            FunctionMonotonicTypeEnum monotonicTypeEnum,
            double sampleRate,
            double amplitude,
            string aodWaveDirectory,
            int zeroSampleCount = 0,
            int endpointSampleCount = 0,
            double[]? deltaKs = null,
            int generateRetryTimes = 1000)
    {
        // 5.742 = 音速(mm/us), 固体声速更快:
        const double chirpAodSoundSpeed = 5.742d;

        #region 参数判断

        Guard.IsGreaterThanOrEqualTo(bandWidth, 0d, nameof(bandWidth));
        Guard.IsGreaterThan(centerFrequency, 0d, nameof(centerFrequency));
        Guard.IsGreaterThan(sampleRate, 0d, nameof(sampleRate));
        Guard.IsGreaterThan(amplitude, 0d, nameof(amplitude));
        Guard.IsLessThanOrEqualTo(amplitude, 1d, nameof(amplitude));
        Guard.IsNotNullOrWhiteSpace(aodWaveDirectory, nameof(aodWaveDirectory));
        Guard.IsGreaterThanOrEqualTo(zeroSampleCount, 0, nameof(zeroSampleCount));
        Guard.IsGreaterThanOrEqualTo(endpointSampleCount, 0, nameof(endpointSampleCount));
        Guard.IsGreaterThan(generateRetryTimes, 0, nameof(generateRetryTimes));

        if (monotonicTypeEnum is FunctionMonotonicTypeEnum.Flatness && bandWidth != 0) ThrowHelper.ThrowArgumentException(nameof(bandWidth), "if monotonic is flatness then band Width is must 0");
        if (bandWidth == 0 && monotonicTypeEnum is not FunctionMonotonicTypeEnum.Flatness) ThrowHelper.ThrowArgumentException(nameof(monotonicTypeEnum), "if band Width is must 0 then monotonic is flatness and band");

        var flatnessTime = Math.Round(soundPacketLength / chirpAodSoundSpeed * 1000d, MidpointRounding.AwayFromZero); // (ns): mm/(mm/us) * 1000 = us * 1000 = ns

        Guard.IsGreaterThan(flatnessTime, 0, nameof(flatnessTime));

        #endregion 参数判断

        var lowFrequency = monotonicTypeEnum switch // 低频
        {
            FunctionMonotonicTypeEnum.Increasing or FunctionMonotonicTypeEnum.Deceasing => centerFrequency - bandWidth / 2d,
            FunctionMonotonicTypeEnum.Flatness => centerFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
        };
        var highFrequency = monotonicTypeEnum switch // 高频
        {
            FunctionMonotonicTypeEnum.Increasing or FunctionMonotonicTypeEnum.Deceasing => centerFrequency + bandWidth / 2d,
            FunctionMonotonicTypeEnum.Flatness => centerFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
        };

        var readonlyBandWidth = bandWidth;
        var readonlyCenterFrequency = centerFrequency; // 中心频率
        var readonlyLowFrequency = lowFrequency;
        var readonlyHighFrequency = highFrequency;

        var numberOfSamples = (int)Math.Round(flatnessTime * sampleRate / 1000 + 2 * endpointSampleCount, MidpointRounding.AwayFromZero); // 总采样点的个数: ns * (Msa/s) / 1000 = ns * (Gsa/s) = (10^-9s)*(10^9sa/s) = sa

        #region 返回结果

        #region 文件

        var frequencyFileName = monotonicTypeEnum switch
        {
            FunctionMonotonicTypeEnum.Increasing => $"{readonlyLowFrequency:0.###}Mhz_{readonlyHighFrequency:0.###}Mhz",
            FunctionMonotonicTypeEnum.Deceasing => $"{readonlyHighFrequency:0.###}Mhz_{readonlyLowFrequency:0.###}Mhz",
            FunctionMonotonicTypeEnum.Flatness => $"{readonlyCenterFrequency:0.###}Mhz_{readonlyCenterFrequency:0.###}Mhz",
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(monotonicTypeEnum))
        };
        // $总byte长度$补零个数$不知道含义$下发寄存器号（02prescan，03chirp）
        var aodWaveFilePath = Path.Combine(aodWaveDirectory, $"chirp" +
                                                             $"_{soundPacketLength:0.###}mm" +
                                                             $"_{readonlyBandWidth:0.###}BWMhz" +
                                                             $"_{frequencyFileName}" +
                                                             $"_{flatnessTime:0.###}ns" +
                                                             $"_{amplitude:0.###}AMP" +
                                                             $"_{(deltaKs is null ? string.Empty : string.Join(",", deltaKs))}DeltaKs" +
                                                             $"_{numberOfSamples}Count" +
                                                             $"${numberOfSamples}${zeroSampleCount}$600$03$.txt");

        aodWaveFilePath = FileHelper.GetEnsureLongPathSupport(aodWaveFilePath);

        #endregion 文件

        var dt = 1d / sampleRate; // 每个采样点的时间间隔 (us/sa): 1 / (Msa/s) = 10^-6s/sa = us/sa

        var allSampleIndices = GenerateUtils.LinearIndexRange(0, numberOfSamples - 1);
        var headerSampleIndices = GenerateUtils.LinearIndexRange(0, endpointSampleCount - 1);
        var flatnessSampleIndices = GenerateUtils.LinearIndexRange(endpointSampleCount, numberOfSamples - endpointSampleCount - 1);
        var footerSampleIndices = GenerateUtils.LinearIndexRange(numberOfSamples - endpointSampleCount, numberOfSamples - 1);

        var aodWaveSignals = Vector<double>.Build.Dense(numberOfSamples);

        Vector<double> dLinearFrequencies;
        Vector<double> dFlatnessFrequencies;

        double minFlatnessFrequency;
        double maxFlatnessFrequency;

        Vector<Complex> fftResult;
        Vector<double> fftFrequencies;
        Vector<double> fftMagnitudes;

        #endregion 返回结果

        /*var count = 1;
        while (true)
        {*/

        #region 频率

        switch (monotonicTypeEnum)
        {
            case FunctionMonotonicTypeEnum.Increasing:
            case FunctionMonotonicTypeEnum.Deceasing:
                var kSegments = Vector<double>.Build.Dense(flatnessSampleIndices.Length, bandWidth / flatnessSampleIndices.Length);
                if (deltaKs?.Length > 0)
                {
                    var segmentLength = flatnessSampleIndices.Length / deltaKs.Length;
                    Guard.IsGreaterThan(segmentLength, 0d, nameof(segmentLength));
                    for (var i = 0; i < deltaKs.Length; i++)
                    {
                        var startIndex = i * segmentLength;
                        var endIndex = (i + 1) * segmentLength - 1;
                        if (i == deltaKs.Length - 1 && (endIndex >= flatnessSampleIndices.Length || endIndex < flatnessSampleIndices.Length - 1)) endIndex = flatnessSampleIndices.Length - 1;

                        kSegments.SetSubVectorRange(startIndex, endIndex, kSegments.GetByIndices(GenerateUtils.LinearIndexRange(startIndex, endIndex)) + deltaKs[i]);
                    }
                }

                dLinearFrequencies = kSegments.IntegrateCumulative();

                break;

            case FunctionMonotonicTypeEnum.Flatness:
            default:
                dLinearFrequencies = Vector<double>.Build.Dense(flatnessSampleIndices.Length, 0);

                break;
        }

        var dHeaderFrequencies = monotonicTypeEnum switch
        {
            FunctionMonotonicTypeEnum.Increasing => Vector<double>.Build.Dense(headerSampleIndices.Length, lowFrequency),
            FunctionMonotonicTypeEnum.Deceasing => Vector<double>.Build.Dense(headerSampleIndices.Length, highFrequency),
            FunctionMonotonicTypeEnum.Flatness => Vector<double>.Build.Dense(headerSampleIndices.Length, centerFrequency),
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Vector<double>>(nameof(monotonicTypeEnum))
        };

        var dFooterFrequencies = monotonicTypeEnum switch
        {
            FunctionMonotonicTypeEnum.Increasing => Vector<double>.Build.Dense(footerSampleIndices.Length, highFrequency),
            FunctionMonotonicTypeEnum.Deceasing => Vector<double>.Build.Dense(footerSampleIndices.Length, lowFrequency),
            FunctionMonotonicTypeEnum.Flatness => Vector<double>.Build.Dense(footerSampleIndices.Length, centerFrequency),
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Vector<double>>(nameof(monotonicTypeEnum))
        };

        dFlatnessFrequencies = monotonicTypeEnum switch
        {
            FunctionMonotonicTypeEnum.Increasing => lowFrequency + dLinearFrequencies,
            FunctionMonotonicTypeEnum.Deceasing => highFrequency - dLinearFrequencies,
            FunctionMonotonicTypeEnum.Flatness => Vector<double>.Build.Dense(flatnessSampleIndices.Length, centerFrequency),
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Vector<double>>(nameof(monotonicTypeEnum))
        };

        if (dFlatnessFrequencies.Exists(f => f <= 0 || f > readonlyHighFrequency * 1.5)) ThrowHelper.ThrowArgumentException(nameof(dFlatnessFrequencies), $"Synthesized frequencies must be in the range (0, {readonlyHighFrequency * 1.5:f3} MHz)");

        #endregion 频率

        #region 相位

        var dHeaderPhases = 2 * Math.PI * dHeaderFrequencies * dt;
        var headerPhases = dHeaderPhases.IntegrateCumulative();
        var dFooterPhases = 2 * Math.PI * dFooterFrequencies * dt;
        var footerPhases = dFooterPhases.IntegrateCumulative();
        var dFlatnessPhases = 2 * Math.PI * dFlatnessFrequencies * dt;
        var flatnessPhases = dFlatnessPhases.IntegrateCumulative();

        #endregion 相位

        #region 波形

        if (headerSampleIndices.Length > 0) aodWaveSignals.SetSubVectorRange(headerSampleIndices[0], headerSampleIndices[^1], amplitude * headerPhases.PointwiseCos().PointwiseMultiply(Vector<double>.Build.DenseOfArray(headerSampleIndices) / headerSampleIndices.Length));
        aodWaveSignals.SetSubVectorRange(flatnessSampleIndices[0], flatnessSampleIndices[^1], amplitude * flatnessPhases.PointwiseCos());
        if (footerSampleIndices.Length > 0) aodWaveSignals.SetSubVectorRange(footerSampleIndices[0], footerSampleIndices[^1], amplitude * footerPhases.PointwiseCos().PointwiseMultiply(1 - (Vector<double>.Build.DenseOfArray(footerSampleIndices) - footerSampleIndices[0] + 1) / footerSampleIndices.Length));

        #endregion 波形

        #region 傅里叶

        var flatnessAodWaveSignals = aodWaveSignals.SubVectorRange(flatnessSampleIndices[0], flatnessSampleIndices[^1]);
        fftResult = flatnessAodWaveSignals.ToComplex().FastFourierTransform();
        (fftFrequencies, fftMagnitudes) = fftResult.GetPositiveFrequencies(sampleRate);

        #endregion 傅里叶

        /*#region 平坦部分的起始和终止频率

        var fftDerivative = fftMagnitudes.Differentiate() / fftFrequencies.Differentiate();
        var fftDerivativeFrequencies = fftFrequencies.SubVectorRange(0, fftFrequencies.Count - 2);

        // 寻找极值点
        const double threshold = 0.0001;
        var indices = fftDerivative.FindAbsAbove(threshold);
        var headerIndices = GenerateHelper.LinearIndexRange(0, indices[0] - 1);
        var flatnessIndices = GenerateHelper.LinearIndexRange(indices[0], indices[^1]);
        var footerIndices = GenerateHelper.LinearIndexRange(indices[^1] + 1, fftDerivative.Count - 1);

        var fftDerivativeSign = Vector<double>.Build.SameAs(fftDerivative);
        fftDerivativeSign.SetByIndices(flatnessIndices, fftDerivative.GetByIndices(flatnessIndices).PointwiseSign());
        fftDerivativeSign.SetByIndices(headerIndices, Vector<double>.Build.Dense(headerIndices.Length, fftDerivativeSign[flatnessIndices[0]]));
        fftDerivativeSign.SetByIndices(footerIndices, Vector<double>.Build.Dense(footerIndices.Length, fftDerivativeSign[flatnessIndices[^1]]));

        var fftExtremumPointIndices = fftDerivativeSign.Differentiate().FindAll(d => d != 0); // 通过sign寻找极值点, 极值点的索引都提前了1个点
        var fftExtremumPointFrequencies = fftFrequencies.GetByIndices([.. fftExtremumPointIndices.Select(d => d + 1)]);
        var readonlyMinFrequency = minFlatnessFrequency = fftExtremumPointFrequencies[0];
        var readonlyMaxFrequency = maxFlatnessFrequency = fftExtremumPointFrequencies[^1];

        // 寻找拐点
        var fftSecondDerivative = fftDerivative.Differentiate() / fftDerivativeFrequencies.Differentiate();

        indices = fftSecondDerivative.FindAbsAbove(0.0001);
        headerIndices = GenerateHelper.LinearIndexRange(0, indices[0] - 1);
        flatnessIndices = GenerateHelper.LinearIndexRange(indices[0], indices[^1]);
        footerIndices = GenerateHelper.LinearIndexRange(indices[^1] + 1, fftSecondDerivative.Count - 1);

        var fftSecondDerivativeSign = Vector<double>.Build.SameAs(fftSecondDerivative);
        fftSecondDerivativeSign.SetByIndices(flatnessIndices, fftSecondDerivative.GetByIndices(flatnessIndices).PointwiseSign());
        fftSecondDerivativeSign.SetByIndices(headerIndices, Vector<double>.Build.Dense(headerIndices.Length, fftSecondDerivativeSign[flatnessIndices[0]]));
        fftSecondDerivativeSign.SetByIndices(footerIndices, Vector<double>.Build.Dense(footerIndices.Length, fftSecondDerivativeSign[flatnessIndices[^1]]));

        var fftInflectionPointIndices = fftSecondDerivativeSign.Differentiate().FindAll(d => d != 0); // 通过sign寻找拐点点, 极值点的索引都提前了1个点
        var fftInflectionPointFrequencies = fftFrequencies.GetByIndices([.. fftInflectionPointIndices.Select(d => d + 1)]);

        switch (monotonicTypeEnum)
        {
            case FunctionMonotonicTypeEnum.Flatness:
                if (Math.Abs(minFlatnessFrequency - maxFlatnessFrequency) > Constants.Tolerance) ThrowHelper.ThrowArgumentException("leftFreq != rightFreq");
                break;

            case FunctionMonotonicTypeEnum.Deceasing:
            case FunctionMonotonicTypeEnum.Increasing:
                var leftIndex = fftInflectionPointFrequencies.Find(d => d < readonlyMinFrequency);
                var rightIndex = fftInflectionPointFrequencies.FindLast(d => d > readonlyMaxFrequency);

                minFlatnessFrequency = leftIndex?.Item2 ?? double.NaN;
                maxFlatnessFrequency = rightIndex?.Item2 ?? double.NaN;
                if (double.IsNaN(minFlatnessFrequency) || double.IsNaN(maxFlatnessFrequency)) ThrowHelper.ThrowArgumentException("leftFreq or rightFreq is NaN");

                break;

            default:
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(monotonicTypeEnum));
                break;
        }

        #endregion 平坦部分的起始和终止频率

        #region 修正

        if (monotonicTypeEnum == FunctionMonotonicTypeEnum.Flatness)
        {
            if (Math.Abs(centerFrequency - readonlyCenterFrequency) < sampleRate / flatnessSampleIndices.Length) break;
            if (centerFrequency < readonlyCenterFrequency)
            {
                centerFrequency += sampleRate / (2d * flatnessSampleIndices.Length);
            }
            else
            {
                centerFrequency -= sampleRate / (2d * flatnessSampleIndices.Length);
            }
        }
        else if (monotonicTypeEnum == FunctionMonotonicTypeEnum.Increasing)
        {
            if (Math.Abs(minFlatnessFrequency - readonlyLowFrequency) < sampleRate / flatnessSampleIndices.Length)
            {
                if (Math.Abs(maxFlatnessFrequency - readonlyHighFrequency) < sampleRate / flatnessSampleIndices.Length) break;
                if (maxFlatnessFrequency < readonlyHighFrequency)
                    bandWidth += sampleRate / (2d * flatnessSampleIndices.Length);
                else
                    bandWidth -= sampleRate / (2d * flatnessSampleIndices.Length);
            }
            else if (minFlatnessFrequency < readonlyLowFrequency)
                lowFrequency += sampleRate / (2d * flatnessSampleIndices.Length);
            else
                lowFrequency -= sampleRate / (2d * flatnessSampleIndices.Length);
        }
        else if (monotonicTypeEnum == FunctionMonotonicTypeEnum.Deceasing)
        {
            if (Math.Abs(maxFlatnessFrequency - readonlyHighFrequency) < sampleRate / flatnessSampleIndices.Length)
            {
                if (Math.Abs(minFlatnessFrequency - readonlyLowFrequency) < sampleRate / flatnessSampleIndices.Length) break;
                if (minFlatnessFrequency < readonlyLowFrequency)
                    bandWidth -= sampleRate / (2d * flatnessSampleIndices.Length);
                else
                    bandWidth += sampleRate / (2d * flatnessSampleIndices.Length);
            }
            else if (maxFlatnessFrequency < readonlyHighFrequency)
                highFrequency += sampleRate / (2d * flatnessSampleIndices.Length);
            else
                highFrequency -= sampleRate / (2d * flatnessSampleIndices.Length);
        }
        else
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(monotonicTypeEnum));
        }

        #endregion 修正

        if (++count > generateRetryTimes) ThrowHelper.ThrowInvalidOperationException("AOD waveforms could not be generated");
    }*/

        /*
         * double[-1,1]归一化数据需要转换为16-bit或32-bit整数格式进行传输[DSP、FPGA、DAC数字信号转换为模拟信号, 目前这个是16-bit PCM(脉冲编码调制)格式
         * 1. aodWaveSignal ∈ [-1, 1] 归一化
         * 2. 放大到 [-2^15, 2^15 - 1] 之间的16位整数(Int16的取值范围)
         *          - 防止溢出的四舍五入[-1，1] * 2^15 ∈ [-2^15, 2^15 - 1]
         *          1.0  :  +32767（避免溢出到 +32768，因为 Int16 最高是 32767）
         *          0.0  :  0
         *          -1.0 :  -32768
         * 3. Int16取值范围内, < 0: 负数在二进制补码表示下，等价于加 2^32
         *          - Int16的负数补码
         *          ((short)-900).ToString("X4")                        : FC7C
         *          ((long)-900).ToString("X4")                         : FFFFFC7C
         *          ((long)-900 + (long)Math.Pow(2, 32)).ToString("X4") : FFFFFC7C
         * 4. 获取Int16所有的补码, 只会有4位
         * 5. Excel拷贝txt显示曲线:
         *          一、公式: =(HEX2DEC(A1)-IF(HEX2DEC(A1)>=32768,65536,0))/POWER(2,16)
         *          二、不要[下拉填充点]直接推拽下拉太慢, 快速公式下拉填充: 直接双击[下拉填充点]一行直接生成
         */

        // 将结果转换为16位整数并保存到文件
        var aodWaveSignalResult = aodWaveSignals
            .Select(t => ConvertUtils.ToInt16NotOverflowException(Math.Round(Math.Pow(2, 15) * t, MidpointRounding.AwayFromZero)))
            .Select(Convert.ToInt64)
            .Select(t => t < 0 ? t + (long)Math.Pow(2, 32) : t)
            .ToArray();
        var hexStrings = aodWaveSignalResult
            .Select(t => t.ToString("X4")) // 转换为16进制补码字符串, 至少4位不足右边补0
            .Select(t => t[^4..]) // 字符串的最后 4 位
            .ToArray();

        DirectoryHelper.CreateFileDirectoryIfNotExists(aodWaveFilePath);
        FileHelper.DeleteFileIfExists(aodWaveFilePath);
        System.IO.File.WriteAllText(aodWaveFilePath, string.Join(Environment.NewLine, hexStrings));

        return (
            aodWaveFilePath,
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, dFlatnessFrequencies[index])).ToArray(),
            allSampleIndices.Select(t => new Point(t + 1, aodWaveSignals[t])).ToArray(),
            fftFrequencies.Zip(fftMagnitudes, (f, m) => new Point(f, m)).ToArray());
    }
}