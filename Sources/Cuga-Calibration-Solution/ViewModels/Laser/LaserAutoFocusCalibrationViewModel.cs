using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Extensions;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserAutoFocusCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserAutoFocusCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Find a Position" },
        new() { StepName = "AB Brightness" },
        new() { StepName = "Gain" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private LaserAutoFocusDto[] _aBrightnessList = [];

    [ObservableProperty]
    private LaserAutoFocusDto? _aBrightnessSelected;

    [ObservableProperty]
    private Point[] _aBrightnessFList = [];

    [ObservableProperty]
    private Point[] _aBrightnessNList = [];

    [ObservableProperty]
    private LaserAutoFocusDto[] _bBrightnessList = [];

    [ObservableProperty]
    private LaserAutoFocusDto? _bBrightnessSelected;

    [ObservableProperty]
    private Point[] _bBrightnessFList = [];

    [ObservableProperty]
    private Point[] _bBrightnessNList = [];

    [ObservableProperty]
    private LaserAutoFocusDto? _resultLaserAutoFocusDto;

    [ObservableProperty]
    private LaserAutoFocusDto[] _nscStandardList = [];

    [ObservableProperty]
    private LaserAutoFocusDto? _nscStandardSelected;

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private LaserAutoFocusDto? _reviewDto;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private LaserAutoFocusCache _cache = new();

    [ObservableProperty]
    private LaserAutoFocusDto _calibration = new();

    [ObservableProperty]
    private MicroscopeCalChipDto _microscopeCalChip = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (CalibrationStatusService.GetAdsCalibrationIsOKStatus() == false)
        {
            DialogWindowProvider.ShowDialog("The ADS precondition is Failure", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<MicroscopeFocusItemDto>(out _, out var errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<MicroscopeCalChipDto>(out var microscopeCalChip, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        MicroscopeCalChip = microscopeCalChip;

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<LaserAutoFocusCache>();
        Calibration = CacheProvider.GetOrDefault<LaserAutoFocusDto>();

        return isHasCache || CacheProvider.Set(Cache, cancellationToken);
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
        Cache.FindPosition = MicroscopeCalChip.ShinyWaferBrightFieldMachinePosition;
        StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewDto = Calibration.Clone();

        if (ReviewDto.IsCalibrated == false) return false;

        MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
        StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                return true;

            case 1:
                return true;

            case 2:
                if (ResultLaserAutoFocusDto is null)
                {
                    DialogWindowProvider.TryShowDialog("Please find current!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    ResultLaserAutoFocusDto.IsCalibrated = true;
                    if (Save(ResultLaserAutoFocusDto, cancellationToken) == false)
                    {
                        ResultLaserAutoFocusDto.IsCalibrated = false;
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                        return false;
                    }
                }

                IsCalibrated = true;

                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            ResultLaserAutoFocusDto = null;

            Cache.FindPosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(async () =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.FindPosition,
                Cache.ThresholdRangeRatio,
                Cache.ThresholdIdealFMin,
                Cache.ThresholdIdealFMax,
                Cache.ThresholdIdealNMin,
                Cache.ThresholdIdealNMax,
                Cache.ThresholdFMin,
                Cache.ThresholdFMax,
                Cache.ThresholdNMin,
                Cache.ThresholdNMax,
                Cache.ThresholdCurrentMin,
                Cache.ThresholdCurrentMax,
                Cache.FindInterval
            }), HtmlLogUniqueId.LoggingHtml());

            if (Cache.ThresholdIdealFMin >= Cache.ThresholdIdealFMax || Cache.ThresholdIdealNMin >= Cache.ThresholdIdealNMax)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{nameof(Cache.ThresholdIdealFMin)} >= {nameof(Cache.ThresholdIdealFMax)} || {nameof(Cache.ThresholdIdealNMin)} >= {nameof(Cache.ThresholdIdealNMax)}"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            // 恢复默认值
            AfViewModel.SetSensorNscCompensationCoefficient(0, 1);
            await Task.Delay(100, cancellationToken);

            StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));
            AfViewModel.SetDarkFieldAutoFocus(null, OpticsMagTypeEnum.High, CalChipSiteModelEnum.ShinyWaferModel);
            // NSC模式On
            AfViewModel.ToggleDarkFieldEnable(false);
            AfViewModel.GetSensorNscCurveIsOk();
            AfViewModel.ToggleDarkFieldEnable(true);
            await Task.Delay(100, cancellationToken);

            var times = 1;

            ABrightnessList = [];
            ABrightnessSelected = null;
            ABrightnessFList = [];
            ABrightnessNList = [];

            Logger.LogHtmlInformation("A Brightness", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (f, n) = AfViewModel.GetSensorFnValue(true);
                var current = AfViewModel.GetSensorCurrentValue(true);

                var item = new LaserAutoFocusDto { Fa = f, Na = n, CurrentA = current };

                ABrightnessList = [.. ABrightnessList, item];
                ABrightnessFList = [.. ABrightnessFList, new Point(item.CurrentA, item.Fa)];
                ABrightnessNList = [.. ABrightnessNList, new Point(item.CurrentA, item.Na)];

                Logger.LogHtmlInformation($"time: {times}", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    FindPosition = Cache.FindPosition.ToShortString(),
                    ABrightnessFList = new HtmlPlot2DLinesChart([(string.Empty, ABrightnessFList)], string.Empty),
                    ABrightnessNList = new HtmlPlot2DLinesChart([(string.Empty, ABrightnessNList)], string.Empty)
                }), HtmlLogUniqueId.LoggingHtml());

                if (Cache.ThresholdNMin <= n && n <= Cache.ThresholdNMax && Cache.ThresholdFMin <= f && f <= Cache.ThresholdFMax)
                {
                    ABrightnessSelected = item;

                    ResultLaserAutoFocusDto = ABrightnessSelected.Clone();
                    ResultLaserAutoFocusDto.Fa = ABrightnessSelected.Fa;
                    ResultLaserAutoFocusDto.Na = ABrightnessSelected.Na;
                    ResultLaserAutoFocusDto.CurrentA = ABrightnessSelected.CurrentA;

                    Logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        ResultLaserAutoFocusDto.Fa,
                        ResultLaserAutoFocusDto.Na,
                        ResultLaserAutoFocusDto.CurrentA,
                        ABrightnessFList = new HtmlPlot2DLinesChart([(string.Empty, ABrightnessFList)], string.Empty),
                        ABrightnessNList = new HtmlPlot2DLinesChart([(string.Empty, ABrightnessNList)], string.Empty)
                    }), HtmlLogUniqueId.LoggingHtml());

                    break;
                }

                var interval = (n > Cache.ThresholdNMax && f >= Cache.ThresholdFMin) ||
                               (f > Cache.ThresholdFMax && n >= Cache.ThresholdNMin)
                    ? -Cache.FindInterval
                    : (n < Cache.ThresholdNMin && f <= Cache.ThresholdFMax) ||
                      (f < Cache.ThresholdFMin && n <= Cache.ThresholdNMax)
                        ? Cache.FindInterval
                        : ThrowHelper.ThrowArgumentException<double>("f and n orientation discrepancy");

                current += interval;

                if (current > Cache.ThresholdCurrentMax || current < Cache.ThresholdCurrentMin)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        Error = $"{Name}Error: Current Value({current}) Out Of Range({Cache.ThresholdCurrentMin},{Cache.ThresholdCurrentMax}).",
                        ABrightnessFList = new HtmlPlot2DLinesChart([(string.Empty, ABrightnessFList)], string.Empty),
                        ABrightnessNList = new HtmlPlot2DLinesChart([(string.Empty, ABrightnessNList)], string.Empty)
                    }), HtmlLogUniqueId.LoggingHtml());

                    return false;
                }

                AfViewModel.SetSensorCurrentValue(true, current);

                await Task.Delay(1000, cancellationToken);

                times++;
            }

            times = 1;
            BBrightnessList = [];
            BBrightnessSelected = null;
            BBrightnessFList = [];
            BBrightnessNList = [];

            Logger.LogHtmlInformation("B Brightness", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (f, n) = AfViewModel.GetSensorFnValue(false);
                var current = AfViewModel.GetSensorCurrentValue(false);

                var item = new LaserAutoFocusDto { Fb = f, Nb = n, CurrentB = current };

                SynchronizationContextProvider.Send(() =>
                {
                    BBrightnessList = [.. BBrightnessList, item];
                    BBrightnessFList = [.. BBrightnessFList, new Point(item.CurrentB, item.Fb)];
                    BBrightnessNList = [.. BBrightnessNList, new Point(item.CurrentB, item.Nb)];
                });

                Logger.LogHtmlInformation($"time: {times}", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    FindPosition = Cache.FindPosition.ToShortString(),
                    BBrightnessFList = new HtmlPlot2DLinesChart([(string.Empty, BBrightnessFList)], string.Empty),
                    BBrightnessNList = new HtmlPlot2DLinesChart([(string.Empty, BBrightnessNList)], string.Empty)
                }), HtmlLogUniqueId.LoggingHtml());

                if (Cache.ThresholdNMin <= n && n <= Cache.ThresholdNMax && Cache.ThresholdFMin <= f && f <= Cache.ThresholdFMax)
                {
                    BBrightnessSelected = item;

                    ResultLaserAutoFocusDto.Fb = BBrightnessSelected.Fb;
                    ResultLaserAutoFocusDto.Nb = BBrightnessSelected.Nb;
                    ResultLaserAutoFocusDto.CurrentB = BBrightnessSelected.CurrentB;

                    Logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        ResultLaserAutoFocusDto.Fb,
                        ResultLaserAutoFocusDto.Nb,
                        ResultLaserAutoFocusDto.CurrentB,
                        BBrightnessFList = new HtmlPlot2DLinesChart([(string.Empty, BBrightnessFList)], string.Empty),
                        BBrightnessNList = new HtmlPlot2DLinesChart([(string.Empty, BBrightnessNList)], string.Empty)
                    }), HtmlLogUniqueId.LoggingHtml());

                    return true;
                }

                var interval = (n > Cache.ThresholdNMax && f >= Cache.ThresholdFMin) ||
                               (f > Cache.ThresholdFMax && n >= Cache.ThresholdNMin)
                    ? -Cache.FindInterval
                    : (n < Cache.ThresholdNMin && f <= Cache.ThresholdFMax) ||
                      (f < Cache.ThresholdFMin && n <= Cache.ThresholdNMax)
                        ? Cache.FindInterval
                        : ThrowHelper.ThrowArgumentException<double>("f and n orientation discrepancy");

                current += interval;

                if (current > Cache.ThresholdCurrentMax || current < Cache.ThresholdCurrentMin)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        Error = $"{Name}Error: Current Value({current}) Out Of Range({Cache.ThresholdCurrentMin},{Cache.ThresholdCurrentMax}).",
                        BBrightnessFList = new HtmlPlot2DLinesChart([(string.Empty, BBrightnessFList)], string.Empty),
                        BBrightnessNList = new HtmlPlot2DLinesChart([(string.Empty, BBrightnessNList)], string.Empty)
                    }), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                AfViewModel.SetSensorCurrentValue(false, current);

                await Task.Delay(1000, cancellationToken);

                times++;
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(async () =>
        {
            Guard.IsNotNull(ResultLaserAutoFocusDto);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.FindPosition,
                Cache.HalfEcsLength,
                Cache.SpeedEcs,
                Cache.NscStandardValue,
                Cache.ThresholdNscOffset,
                Cache.ThresholdNscGain,
                Cache.RetryCount
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("Offset Gain", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            NscStandardList = [];
            NscStandardSelected = null;

            var (originOffset, originGain) = AfViewModel.GetSensorNscCompensationCoefficient();

            try
            {
                AfViewModel.SetSensorNscCompensationCoefficient(0, 1);
                await Task.Delay(100, cancellationToken);

                AfViewModel.ToggleDarkFieldEnable(true);
                await Task.Delay(100, cancellationToken);

                var averageEcs = AfViewModel.GetSensorAverageEcsValue();

                var startEcs = averageEcs - Cache.HalfEcsLength;
                var endEcs = averageEcs + Cache.HalfEcsLength;

                double offset = 0, gain = 1;
                var count = 1;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    AfViewModel.SetSensorNscCompensationCoefficient(offset, gain);
                    await Task.Delay(100, cancellationToken);

                    var traceBufferList = AfViewModel.GetNscCompensationCoefficientTraceBufferList(startEcs, endEcs, Cache.SpeedEcs, TimeSpan.FromSeconds(Cache.HalfEcsLength * 2 / Cache.SpeedEcs + 2));

                    var ecs = traceBufferList.Select(t => t.Ecs).ToList();
                    var nsc = traceBufferList.Select(t => t.Nsc).ToList();
                    var lvdt = traceBufferList.Select(t => t.Lvdt).ToList();

                    var max = nsc.Max();
                    var min = nsc.Min();
                    var currentOffset = (max + min) / 2;
                    var currentGain = Cache.NscStandardValue / (max - currentOffset);
                    var offsetIsOk = Math.Abs(currentOffset) <= Cache.ThresholdNscOffset;
                    var gainIsOk = Math.Abs(max - Cache.NscStandardValue) <= Cache.ThresholdNscGain &&
                                   Math.Abs(min + Cache.NscStandardValue) <= Cache.ThresholdNscGain;
                    if (offsetIsOk == false) offset += currentOffset / gain;
                    if (gainIsOk == false) gain *= currentGain;

                    var item = new LaserAutoFocusDto
                    {
                        NscOffset = offset,
                        NscGain = gain,
                        NscCurrentMax = max,
                        NscCurrentMin = min,
                        NscCurrentOffset = currentOffset,
                        NscCurrentGain = currentGain,
                        EcsData = ecs,
                        NscData = nsc,
                        LvdtData = lvdt
                    };
                    NscStandardList = [.. NscStandardList, item];

                    var htmlBullet = new HtmlBullet(new
                    {
                        Cache.FindPosition,
                        startEcs,
                        endEcs,
                        Cache.SpeedEcs,
                        item.NscOffset,
                        NscGain = 1 / item.NscGain,
                        item.NscCurrentMax,
                        item.NscCurrentMin,
                        item.NscCurrentOffset,
                        NscCurrentGain = 1 / item.NscCurrentGain,
                        traceBufferList = new HtmlPlot2DLinesChart([(nameof(ecs), ecs.ToPoints()), (nameof(nsc), nsc.ToPoints()), (nameof(lvdt), lvdt.ToPoints())], string.Empty)
                    });

                    if (offsetIsOk && gainIsOk)
                    {
                        NscStandardSelected = item;

                        ResultLaserAutoFocusDto.NscOffset = NscStandardSelected.NscOffset;
                        ResultLaserAutoFocusDto.NscGain = NscStandardSelected.NscGain;
                        ResultLaserAutoFocusDto.NscCurrentMax = NscStandardSelected.NscCurrentMax;
                        ResultLaserAutoFocusDto.NscCurrentMin = NscStandardSelected.NscCurrentMin;
                        ResultLaserAutoFocusDto.EcsData = [.. NscStandardSelected.EcsData];
                        ResultLaserAutoFocusDto.NscData = [.. NscStandardSelected.NscData];
                        ResultLaserAutoFocusDto.LvdtData = [.. NscStandardSelected.LvdtData];

                        Logger.LogHtmlInformation($"time: {count} OK", HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                        return true;
                    }

                    Logger.LogHtmlInformation($"time: {count}", HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                    if (++count > Cache.RetryCount) ThrowHelper.ThrowInvalidOperationException("Pmt Delay Retry Limit Exceeded");
                }
            }
            finally
            {
                AfViewModel.SetSensorNscCompensationCoefficient(originOffset, originGain);
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        if (ReviewDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(() =>
        {
            ReviewDto.IsVerified = false;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.FindPosition,
                Cache.ThresholdRangeRatio,
                Cache.ThresholdIdealFMin,
                Cache.ThresholdIdealFMax,
                Cache.ThresholdIdealNMin,
                Cache.ThresholdIdealNMax,
                Cache.ThresholdFMin,
                Cache.ThresholdFMax,
                Cache.ThresholdNMin,
                Cache.ThresholdNMax,
                Cache.ThresholdCurrentMin,
                Cache.ThresholdCurrentMax,
                Cache.FindInterval,
                Cache.HalfEcsLength,
                Cache.SpeedEcs,
                Cache.NscStandardValue,
                Cache.ThresholdNscOffset,
                Cache.ThresholdNscGain,
                Cache.RetryCount
            }), HtmlLogUniqueId.LoggingHtml());

            var (originOffset, originGain) = AfViewModel.GetSensorNscCompensationCoefficient();

            try
            {
                StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));
                AfViewModel.ToggleDarkFieldEnable(false);
                AfViewModel.GetSensorNscCurveIsOk();
                AfViewModel.ToggleDarkFieldEnable(true);

                // 1. 使用校准后的电流，读当前的fa na fb nb
                var (fa, na) = AfViewModel.GetSensorFnValue(true);
                var (fb, nb) = AfViewModel.GetSensorFnValue(false);
                var averageEcs = AfViewModel.GetSensorAverageEcsValue();

                AfViewModel.SetSensorNscCompensationCoefficient(ReviewDto.NscOffset, ReviewDto.NscGain);
                var startEcs = averageEcs - Cache.HalfEcsLength;
                var endEcs = averageEcs + Cache.HalfEcsLength;
                var traceBufferList = AfViewModel.GetNscCompensationCoefficientTraceBufferList(startEcs, endEcs, Cache.SpeedEcs, TimeSpan.FromSeconds(Cache.HalfEcsLength * 2 / Cache.SpeedEcs + 2));

                var ecs = traceBufferList.Select(t => t.Ecs).ToList();
                var nsc = traceBufferList.Select(t => t.Nsc).ToList();
                var lvdt = traceBufferList.Select(t => t.Lvdt).ToList();

                var currentMax = nsc.Max();
                var currentMin = nsc.Min();
                var currentNscOffset = (currentMax + currentMin) / 2;
                var currentNscGain = Cache.NscStandardValue / (currentMax - currentNscOffset);
                var abBrightnessResult = Cache.ThresholdNMin < na && na < Cache.ThresholdNMax &&
                                         Cache.ThresholdNMin < nb && nb < Cache.ThresholdNMax &&
                                         Cache.ThresholdFMin < fa && fa < Cache.ThresholdFMax &&
                                         Cache.ThresholdFMin < fb && fb < Cache.ThresholdFMax;
                var offsetIsOk = Math.Abs(currentNscOffset) <= Cache.ThresholdNscOffset;
                var gainIsOk = Math.Abs(currentMax - Cache.NscStandardValue) <= Cache.ThresholdNscGain &&
                               Math.Abs(currentMin + Cache.NscStandardValue) <= Cache.ThresholdNscGain;
                var compensationCoefficientResult = offsetIsOk && gainIsOk;

                var result = abBrightnessResult && compensationCoefficientResult;

                Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ReviewDto.CurrentA,
                    ReviewDto.CurrentB,
                    ReviewDto.NscOffset,
                    NscGain = 1 / ReviewDto.NscGain,
                    Fa = fa,
                    Na = na,
                    Fb = fb,
                    Nb = nb,
                    startEcs,
                    endEcs,
                    Cache.SpeedEcs,
                    currentMax,
                    currentMin,
                    currentNscOffset,
                    currentNscGain = 1 / currentNscGain,
                    traceBufferList = new HtmlPlot2DLinesChart([(nameof(ecs), ecs.ToPoints()), (nameof(nsc), nsc.ToPoints()), (nameof(lvdt), lvdt.ToPoints())], string.Empty)
                }), HtmlLogUniqueId.LoggingHtml());

                ReviewDto.IsVerified = result;

                if (Save(ReviewDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name}Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    ReviewDto.IsVerified = false;
                    return false;
                }

                DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);

                return result;
            }
            finally
            {
                AfViewModel.SetSensorNscCompensationCoefficient(originOffset, originGain);
            }
        }).ConfigureAwait(false);
    }

    private bool Save(LaserAutoFocusDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        return CacheProvider.Set(dto, cancellationToken) && CacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}