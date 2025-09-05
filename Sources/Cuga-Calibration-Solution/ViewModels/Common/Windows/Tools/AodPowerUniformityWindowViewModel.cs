using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Utilities;
using Humanizer;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniExcelLibs;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;
using Core.Models.Models.Common.AODWaveform.Generates;
using AodWaveGenerator = Net.Utilities.Algorithms.Modules.AodWaveGenerator;
using Constants = Net.Utilities.Models.Constants;

// ReSharper disable All

namespace CugaCalibration.ViewModels.Common.Windows.Tools;

[IOCAppService(ServiceType = typeof(AodPowerUniformityWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class AodPowerUniformityWindowViewModel(
    LaserViewModel laserViewModel,
    ConfigViewModel configViewModel,
    IOptions<ApplicationSetting> options,
    IDialogWindowProvider dialogWindowProvider,
    ILogger<AodPowerUniformityWindowViewModel> logger)
    : ViewModelBase
{
    public const double DefaultCoefficient = 1;

    public string AodWaveFileDirectory => Path.Combine(options.Value.AppHomeDirectory, EnumHelper.ToDescriptionString(OpticsAODTypeEnum), DirectoryHelper.RemoveInvalidDirectoryName(nameof(AodPowerUniformityWindowViewModel)),
        DateTime.Now.ToString(Constants.MiddleFileDateTimeFormat));

    [ObservableProperty]
    private OpticsAODTypeEnum _opticsAODTypeEnum;

    [ObservableProperty]
    private double _waitTime = 15;

    [ObservableProperty]
    private double _chirpSoundPacketLength;

    [ObservableProperty]
    private double _prescanFlatnessTime;

    [ObservableProperty]
    private double _sampleRate = 1064;

    [ObservableProperty]
    private int _zeroNum;

    [ObservableProperty]
    private int _aodWaveGenerateRetryCount = 1000;

    [ObservableProperty]
    private double _startCenterFrequency;

    [ObservableProperty]
    private double _endCenterFrequency;

    [ObservableProperty]
    private double _stepCenterFrequency;

    [ObservableProperty]
    private double _targetMeasurePower;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetThresholdRateMin))]
    [NotifyPropertyChangedFor(nameof(TargetThresholdRateMax))]
    private double _targetThreshold = 0.05;

    public double TargetThresholdRateMin => 1 - TargetThreshold;

    public double TargetThresholdRateMax => 1 + TargetThreshold;

    [ObservableProperty]
    private double _coefficientStep = 0.1;

    [ObservableProperty]
    private double _retryCount = 20;

    [ObservableProperty]
    private AodPowerUniformityItemDto? _selectItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MeasurePowerPoints))]
    [NotifyPropertyChangedFor(nameof(CoefficientPoints))]
    private AodPowerUniformityItemDto[] _items = [];

    public Point[] MeasurePowerPoints => [.. Items.Select(t => new Point(t.CenterFrequency, t.MeasurePower))];

    public Point[] CoefficientPoints => [.. Items.Select(t => new Point(t.CenterFrequency, t.Coefficient))];

    [ObservableProperty]
    private Point[] _measureCoefficientPowerPoints = [];

    partial void OnOpticsAODTypeEnumChanged(OpticsAODTypeEnum value)
    {
        Items = [];
        MeasureCoefficientPowerPoints = [];
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                var aodFileDirectory = AodWaveFileDirectory;
                Items = [];
                foreach (var centerFrequency in Generate.LinearRange(StartCenterFrequency, StepCenterFrequency, EndCenterFrequency))
                {
                    try
                    {
                        var item = new AodPowerUniformityItemDto
                        {
                            AodWaveFileDirectory = aodFileDirectory,
                            Coefficient = DefaultCoefficient,
                            ChirpSoundPacketLength = ChirpSoundPacketLength,
                            PrescanFlatnessTime = PrescanFlatnessTime,
                            CenterFrequency = centerFrequency,
                            SampleRate = SampleRate,
                            ZeroNum = ZeroNum,
                            IsOk = false
                        };

                        if (await GetMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false) == false) continue;
                        Items = [.. Items, item];
                    }
                    catch (Exception ex)
                    {
                        if (ex is OperationCanceledException) throw;

                        logger.LogError(ex, "Step1");
                    }
                }

                TargetMeasurePower = Items.Min(t => t.MeasurePower);

                Log("GetPower");
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
        }, cancellationToken);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                foreach (var item in Items)
                {
                    try
                    {
                        await AodPowerUniformityAsync(item, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (ex is OperationCanceledException) throw;

                        logger.LogError(ex, "Step1");
                    }
                }

                Log("Uniformity");
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
        }, cancellationToken);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1OneAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                if (SelectItem is null) return;

                await GetMeasurePowerAsync(SelectItem, cancellationToken).ConfigureAwait(false);

                Log("GetPower");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    logger.LogWarning("Canceled");
                    return;
                }

                logger.LogError(ex, "Step2");
            }
        }, cancellationToken);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2OneAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                if (SelectItem is null) return;

                await AodPowerUniformityAsync(SelectItem, cancellationToken).ConfigureAwait(false);

                Log("Uniformity");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    logger.LogWarning("Canceled");
                    return;
                }

                logger.LogError(ex, "Step2");
            }
        }, cancellationToken);
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            var dialog = dialogWindowProvider.TryShowSaveFilePathDialog(".xlsx", out var filePath);
            if (dialog == false) return;

            FileHelper.DeleteFileIfExists(filePath);
            MiniExcel.SaveAs(filePath, Items.Select(t => new GenerateAODWaveformUniformityConfiguration { CenterFrequency = t.CenterFrequency, Coefficient = t.Coefficient }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Save");
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseView(true);
    }

    private Task<bool> AodPowerUniformityAsync(AodPowerUniformityItemDto item, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            item.IsOk = false;
            MeasureCoefficientPowerPoints = [];

            var lastCoefficient = DefaultCoefficient;
            foreach (var coefficient in Generate.LinearRange(0, CoefficientStep, DefaultCoefficient).OrderByDescending(t => t))
            {
                if (coefficient == 0) return false;

                item.Coefficient = coefficient;

                if (await GetMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false) == false) return false;
                MeasureCoefficientPowerPoints = [.. MeasureCoefficientPowerPoints, new Point(coefficient, item.MeasurePower)];
                MeasureCoefficientPowerPoints = [.. MeasureCoefficientPowerPoints.OrderBy(t => t.X)];
                Notify();

                var (isOk, isLessThan) = IsOk();
                if (isOk)
                {
                    item.IsOk = true;
                    return true;
                }

                if (isLessThan) break;

                lastCoefficient = coefficient;
            }

            var retryCount = 0;
            var lowCoefficient = item.Coefficient;
            var highCoefficient = lastCoefficient;
            // 二分查找
            while (lowCoefficient <= highCoefficient)
            {
                var midCoefficient = (highCoefficient + lowCoefficient) / 2;
                item.Coefficient = midCoefficient;

                if (await GetMeasurePowerAsync(item, cancellationToken).ConfigureAwait(false) == false) return false;
                MeasureCoefficientPowerPoints = [.. MeasureCoefficientPowerPoints, new Point(midCoefficient, item.MeasurePower)];
                MeasureCoefficientPowerPoints = [.. MeasureCoefficientPowerPoints.OrderBy(t => t.X)];
                Notify();

                var (isOk, isLessThan) = IsOk();
                if (isOk)
                {
                    item.IsOk = true;
                    return true;
                }

                if (isLessThan)
                {
                    lowCoefficient = midCoefficient;
                }
                else
                {
                    highCoefficient = midCoefficient;
                }

                retryCount += 1;

                if (retryCount > RetryCount) break;
            }

            return false;
        }, cancellationToken);

        (bool IsOk, bool IsLessThan) IsOk()
        {
            var rate = item.MeasurePower / TargetMeasurePower;
            item.Rate = rate;

            return (TargetThresholdRateMin < rate && rate < TargetThresholdRateMax, item.MeasurePower < TargetMeasurePower);
        }
    }

    private Task<bool> GetMeasurePowerAsync(AodPowerUniformityItemDto item, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                var (isSuccess,
                    aodWaveFilePath,
                    _,
                    _,
                    _,
                    _,
                    _,
                    _,
                    _,
                    _,
                    _,
                    aodWaveSignals,
                    aodWaveSignalsFourier,
                    _,
                    exception) = OpticsAODTypeEnum == OpticsAODTypeEnum.Chirp
                    ? AodWaveGenerator.GenerateChirpAodWaveFile(
                        0,
                        item.CenterFrequency,
                        item.ChirpSoundPacketLength,
                        FunctionMonotonicTypeEnum.Flatness,
                        item.SampleRate,
                        item.Coefficient,
                        item.AodWaveFileDirectory,
                        zeroSampleCount: item.ZeroNum,
                        endpointSampleCount: 0,
                        generateRetryTimes: AodWaveGenerateRetryCount)
                    : AodWaveGenerator.GeneratePrescanAodWaveFile(
                        0,
                        item.CenterFrequency,
                        item.PrescanFlatnessTime,
                        FunctionMonotonicTypeEnum.Flatness,
                        item.SampleRate,
                        item.Coefficient,
                        item.AodWaveFileDirectory,
                        zeroSampleCount: item.ZeroNum,
                        endpointSampleCount: 0,
                        generateRetryTimes: AodWaveGenerateRetryCount);

                if (isSuccess == false)
                    throw exception ?? new Exception("Generate Chirp Aod Wave File Failed!");

                item.AodWaveFilePath = aodWaveFilePath;
                item.AodWaveSignals = aodWaveSignals;
                item.AodWaveSignalsFourier = aodWaveSignalsFourier;

                if (OpticsAODTypeEnum == OpticsAODTypeEnum.Chirp)
                {
                    laserViewModel.SetPrescanAODWaveProfileByCoefficient(OpticsMagTypeEnum.High, DefaultCoefficient);
                    laserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Through);
                    laserViewModel.SetChirpAODWaveProfiles([AODWaveformProfileFactory.CreateChirp(OpticsAODElectrodeEnum.Electrode1, item.AodWaveFilePath)]);
                }
                else
                {
                    laserViewModel.SetPrescanAODWaveProfileByCoefficient(OpticsMagTypeEnum.High, DefaultCoefficient);
                    laserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Through);
                }

                await Task.Delay(TimeSpan.FromSeconds(WaitTime), cancellationToken).ConfigureAwait(false);

                var result = laserViewModel.GetOpticalPowerMeter();

                item.MeasurePower = result;
                return true;
            }
            finally
            {
                Notify();
                laserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Scan);
            }
        }, cancellationToken);
    }

    private void Log(string title)
    {
        var guid = Guid.NewGuid();
        logger.LogHtmlInformation(OpticsAODTypeEnum.Humanize() + title.Humanize(LetterCasing.Title), HtmlHeaderLevelEnum.Header1, guid.LoggingHtml());
        logger.LogHtmlInformation("1. Param", HtmlHeaderLevelEnum.Header2, new HtmlQuote(new
        {
            OpticsAodTypeEnum = OpticsAODTypeEnum,
            AodWaveFileDirectory,
            DefaultCoefficient,
            Param = OpticsAODTypeEnum == OpticsAODTypeEnum.Chirp ? $"Sound Packet Length: {ChirpSoundPacketLength}" : $"Flatness Time: {PrescanFlatnessTime}",
            PrescanFlatnessTime,
            SampleRate,
            ZeroNum
        }), guid.LoggingHtml());
        logger.LogHtmlInformation("2. Table", HtmlHeaderLevelEnum.Header2, new HtmlTable([
            .. Items.Select(t => new
            {
                t.Coefficient,
                t.CenterFrequency,
                t.MeasurePower,
                Signals = new HtmlPlot2DLinesChart([(string.Empty, t.AodWaveSignals)], string.Empty),
                Fouriers = new HtmlPlot2DLinesChart([(string.Empty, t.AodWaveSignalsFourier)], string.Empty),
                t.AodWaveFilePath
            })
        ]), guid.LoggingHtml());
        logger.LogHtmlInformation("3. Plot", HtmlHeaderLevelEnum.Header2, new HtmlBullet(new
        {
            MeasurePowerPoints = new HtmlPlot2DLinesChart([(string.Empty, MeasurePowerPoints)], string.Empty),
            CoefficientPoints = new HtmlPlot2DLinesChart([(string.Empty, CoefficientPoints)], string.Empty)
        }), guid.LoggingHtml());
        logger.LogHtmlTrace(guid.LoggedEndHtml(EnumHelper.ToDescriptionString(OpticsAODTypeEnum) + title));
    }

    private void Notify()
    {
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(MeasurePowerPoints));
        OnPropertyChanged(nameof(CoefficientPoints));
        OnPropertyChanged(nameof(MeasureCoefficientPowerPoints));
    }
}

public sealed partial class AodPowerUniformityItemDto : ObservableObject
{
    [ObservableProperty]
    private string _aodWaveFileDirectory = string.Empty;

    [ObservableProperty]
    private double _coefficient;

    [ObservableProperty]
    private double _chirpSoundPacketLength;

    [ObservableProperty]
    private double _prescanFlatnessTime;

    [ObservableProperty]
    private double _centerFrequency = 0.495;

    [ObservableProperty]
    private double _sampleRate = 1064;

    [ObservableProperty]
    private int _zeroNum;

    [ObservableProperty]
    private Point[] _aodWaveSignals = [];

    [ObservableProperty]
    private Point[] _aodWaveSignalsFourier = [];

    [ObservableProperty]
    private string _aodWaveFilePath = string.Empty;

    [ObservableProperty]
    private double _measurePower;

    [ObservableProperty]
    private double _rate;

    [ObservableProperty]
    private bool _isOk;
}