using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
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
        new() { StepName = "Config" },
        new() { StepName = "Find a Position" },
        new() { StepName = "AB Brightness" },
        new() { StepName = "NSC Profile" },
        new() { StepName = "Nsc Offset Gain" }
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

        if (Cache.MicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.MicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();

        if (isHasCache == false) CacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
        Cache.FindPosition = MicroscopeCalChip.ShinyWaferBrightFieldMachinePosition;
        StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewDto = Calibration.Clone();

        if (ReviewDto.IsCalibrated == false) return false;

        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
        StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 1:
            case 2:
            case 3:
                return true;

            case 4:
                Guard.IsNotNull(ResultLaserAutoFocusDto);

                ResultLaserAutoFocusDto.IsCalibrated = true;
                if (Save(ResultLaserAutoFocusDto, cancellationToken) == false)
                {
                    ResultLaserAutoFocusDto.IsCalibrated = false;
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                IsCalibrated = true;

                return true;

            default:
                return true;
        }
    }

    #endregion 控制校准业务

    #region 校准



    [RelayCommand]
    private Task ConfigStepActionAsync()
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                IsAutoGain = Cache.CIBConfiguration.IsAutoGainControl,
                DcGainVoltage = Cache.CIBConfiguration.Gain,
                IsL0k = Cache.CIBConfiguration.IsL0K,
                CIBProfileTypeEnum = Cache.CIBConfiguration.CIBProfileMode
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

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
            var (originOffset, originGain) = AfViewModel.GetSensorNscCompensation();
            var originCurrentAValue = AfViewModel.GetSensorCurrentValue(true);
            var originCurrentBValue = AfViewModel.GetSensorCurrentValue(false);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                originOffset,
                originGain,
                originCurrentAValue,
                originCurrentBValue,
                Cache.FindPosition,
                Cache.ThresholdIdealFMin,
                Cache.ThresholdIdealFMax,
                Cache.ThresholdIdealNMin,
                Cache.ThresholdIdealNMax,
                Cache.ThresholdCurrentMin,
                Cache.ThresholdCurrentMax,
                Cache.CalibrationThresholdRangeRation,
                Cache.CalibrationThresholdFMin,
                Cache.CalibrationThresholdFMax,
                Cache.CalibrationThresholdNMin,
                Cache.CalibrationThresholdNMax,
                Cache.FindCurrentStep
            }), HtmlLogUniqueId.LoggingHtml());

            if (Cache.ThresholdIdealFMin >= Cache.ThresholdIdealFMax || Cache.ThresholdIdealNMin >= Cache.ThresholdIdealNMax)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{nameof(Cache.ThresholdIdealFMin)} >= {nameof(Cache.ThresholdIdealFMax)} || {nameof(Cache.ThresholdIdealNMin)} >= {nameof(Cache.ThresholdIdealNMax)}"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            try
            {
                StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));
                AfViewModel.SetDarkFieldAutoFocus(null, OpticsMagTypeEnum.High, CalChipSiteModelEnum.ShinyWaferModel);

                AfViewModel.ResetSensorNscCompensation();
                await Task.Delay(100, cancellationToken);

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
                        Cache.FindPosition,
                        ABrightnessFList = new HtmlPlot2DLinesChart([(string.Empty, ABrightnessFList)], string.Empty),
                        ABrightnessNList = new HtmlPlot2DLinesChart([(string.Empty, ABrightnessNList)], string.Empty)
                    }), HtmlLogUniqueId.LoggingHtml());

                    if (Cache.CalibrationThresholdNMin <= n && n <= Cache.CalibrationThresholdNMax && Cache.CalibrationThresholdFMin <= f && f <= Cache.CalibrationThresholdFMax)
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

                    var interval = (n > Cache.CalibrationThresholdNMax && f >= Cache.CalibrationThresholdFMin) ||
                                   (f > Cache.CalibrationThresholdFMax && n >= Cache.CalibrationThresholdNMin)
                        ? -Cache.FindCurrentStep
                        : (n < Cache.CalibrationThresholdNMin && f <= Cache.CalibrationThresholdFMax) ||
                          (f < Cache.CalibrationThresholdFMin && n <= Cache.CalibrationThresholdNMax)
                            ? Cache.FindCurrentStep
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
                        Cache.FindPosition,
                        BBrightnessFList = new HtmlPlot2DLinesChart([(string.Empty, BBrightnessFList)], string.Empty),
                        BBrightnessNList = new HtmlPlot2DLinesChart([(string.Empty, BBrightnessNList)], string.Empty)
                    }), HtmlLogUniqueId.LoggingHtml());

                    if (Cache.CalibrationThresholdNMin <= n && n <= Cache.CalibrationThresholdNMax && Cache.CalibrationThresholdFMin <= f && f <= Cache.CalibrationThresholdFMax)
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

                    var interval = (n > Cache.CalibrationThresholdNMax && f >= Cache.CalibrationThresholdFMin) ||
                                   (f > Cache.CalibrationThresholdFMax && n >= Cache.CalibrationThresholdNMin)
                        ? -Cache.FindCurrentStep
                        : (n < Cache.CalibrationThresholdNMin && f <= Cache.CalibrationThresholdFMax) ||
                          (f < Cache.CalibrationThresholdFMin && n <= Cache.CalibrationThresholdNMax)
                            ? Cache.FindCurrentStep
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
            }
            finally
            {
                AfViewModel.SetSensorNscCompensation(originOffset, originGain);
                AfViewModel.SetSensorCurrentValue(true, originCurrentAValue);
                AfViewModel.SetSensorCurrentValue(false, originCurrentBValue);
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(async () =>
        {
            Guard.IsNotNull(ResultLaserAutoFocusDto);

            var (originOffset, originGain) = AfViewModel.GetSensorNscCompensation();
            var originCurrentAValue = AfViewModel.GetSensorCurrentValue(true);
            var originCurrentBValue = AfViewModel.GetSensorCurrentValue(false);
            var ecsToNmRatio = AfViewModel.GetNmPerEcs();

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                originOffset,
                originGain,
                originCurrentAValue,
                originCurrentBValue,
                ecsToNmRatio,
                Cache.MicroscopeLensInformation.LensName,
                Cache.FindPosition,
                Cache.HalfEcsLength,
                Cache.SpeedEcsPerSecond,
                Cache.NscStandardNscPerNm,
                Cache.ThresholdNscStandardSymmetryRatio,
                Cache.ThresholdNscStandardGain
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("Nsc Profile", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            try
            {
                StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));
                AfViewModel.SetDarkFieldAutoFocus(null, OpticsMagTypeEnum.High, CalChipSiteModelEnum.ShinyWaferModel);

                AfViewModel.ResetSensorNscCompensation();
                await Task.Delay(100, cancellationToken);

                AfViewModel.SetSensorCurrentValue(true, ResultLaserAutoFocusDto.CurrentA);
                AfViewModel.SetSensorCurrentValue(false, ResultLaserAutoFocusDto.CurrentB);
                await Task.Delay(100, cancellationToken);

                AfViewModel.ToggleDarkFieldEnable(true);
                await Task.Delay(100, cancellationToken);

                var averageEcs = AfViewModel.GetSensorAverageEcsValue();
                var startEcs = averageEcs - Cache.HalfEcsLength;
                var endEcs = averageEcs + Cache.HalfEcsLength;

                AfViewModel.SetSensorEcsValue(startEcs);
                await Task.Delay(100, cancellationToken);

                var traceBufferList = AfViewModel.GetSensorNscTraceBufferList(startEcs, endEcs, Cache.SpeedEcsPerSecond, TimeSpan.FromSeconds(Math.Abs(endEcs - startEcs) / Cache.SpeedEcsPerSecond + 2));
                var ecs = traceBufferList.Select(t => t.Ecs).ToArray();
                var nsc = traceBufferList.Select(t => t.Nsc).ToArray();
                var lvdt = traceBufferList.Select(t => t.Lvdt).ToArray();
                var ecsVector = Vector<double>.Build.DenseOfEnumerable(ecs);
                var nscVector = Vector<double>.Build.DenseOfEnumerable(nsc);
                var nscMinIndex = nscVector.MinimumIndex();
                var nscMaxIndex = nscVector.MaximumIndex();
                Guard.IsNotEqualTo(nscMinIndex, nscMaxIndex, "nsc curve is a error");

                double nscLeftIntervalLeftEndpointValue, nscRightIntervalRightEndpointValue;
                Vector<double> nscLeftIntervalVector, nscMiddleIntervalVector, nscRightIntervalVector;
                Vector<double> ecsLeftIntervalVector, ecsMiddleIntervalVector, ecsRightIntervalVector;

                var isMinMax = nscMinIndex < nscMaxIndex;
                if (isMinMax)
                {
                    var nscNegativeLeftIndex = nscVector.SubVectorRange(0, nscMinIndex).MaximumIndex();
                    var nscNegativeRightIndex = nscVector.SubVectorRange(nscMaxIndex, nscVector.Count - 1).MinimumIndex() + nscMaxIndex;

                    var nscNegativeLeftVector = nscVector.SubVectorRange(nscNegativeLeftIndex, nscMinIndex);
                    var ecsNegativeLeftVector = ecsVector.SubVectorRange(nscNegativeLeftIndex, nscMinIndex);

                    var nscPositiveMiddleVector = nscVector.SubVectorRange(nscMinIndex, nscMaxIndex);
                    var ecsPositiveMiddleVector = ecsVector.SubVectorRange(nscMinIndex, nscMaxIndex);

                    var nscNegativeRightVector = nscVector.SubVectorRange(nscMaxIndex, nscNegativeRightIndex);
                    var ecsNegativeRightVector = ecsVector.SubVectorRange(nscMaxIndex, nscNegativeRightIndex);

                    nscLeftIntervalLeftEndpointValue = nscNegativeLeftVector[0];
                    nscRightIntervalRightEndpointValue = nscNegativeRightVector[^1];

                    nscLeftIntervalVector = nscNegativeLeftVector;
                    ecsLeftIntervalVector = ecsNegativeLeftVector;

                    nscMiddleIntervalVector = nscPositiveMiddleVector;
                    ecsMiddleIntervalVector = ecsPositiveMiddleVector;

                    nscRightIntervalVector = nscNegativeRightVector;
                    ecsRightIntervalVector = ecsNegativeRightVector;
                }
                else
                {
                    var nscPositiveLeftIndex = nscVector.SubVectorRange(0, nscMaxIndex).MinimumIndex();
                    var nscPositiveRightIndex = nscVector.SubVectorRange(nscMinIndex, nscVector.Count - 1).MaximumIndex() + nscMinIndex;

                    var nscPositiveLeftVector = nscVector.SubVectorRange(nscPositiveLeftIndex, nscMaxIndex);
                    var ecsPositiveLeftVector = ecsVector.SubVectorRange(nscPositiveLeftIndex, nscMaxIndex);

                    var nscNegativeMiddleVector = nscVector.SubVectorRange(nscMaxIndex, nscMinIndex);
                    var ecsNegativeMiddleVector = ecsVector.SubVectorRange(nscMaxIndex, nscMinIndex);

                    var nscPositiveRightVector = nscVector.SubVectorRange(nscMinIndex, nscPositiveRightIndex);
                    var ecsPositiveRightVector = ecsVector.SubVectorRange(nscMinIndex, nscPositiveRightIndex);

                    nscLeftIntervalLeftEndpointValue = nscPositiveLeftVector[0];
                    nscRightIntervalRightEndpointValue = nscPositiveRightVector[^1];

                    nscLeftIntervalVector = nscPositiveLeftVector;
                    ecsLeftIntervalVector = ecsPositiveLeftVector;

                    nscMiddleIntervalVector = nscNegativeMiddleVector;
                    ecsMiddleIntervalVector = ecsNegativeMiddleVector;

                    nscRightIntervalVector = nscPositiveRightVector;
                    ecsRightIntervalVector = ecsPositiveRightVector;
                }

                var isLeftVector = nscLeftIntervalVector[0] * nscLeftIntervalVector[^1] < 0 && ecsLeftIntervalVector[0] < averageEcs && averageEcs < ecsLeftIntervalVector[^1];
                var isMiddleVector = nscMiddleIntervalVector[0] * nscMiddleIntervalVector[^1] < 0 && ecsMiddleIntervalVector[0] < averageEcs && averageEcs < ecsMiddleIntervalVector[^1];
                var isRightVector = nscRightIntervalVector[0] * nscRightIntervalVector[^1] < 0 && ecsRightIntervalVector[0] < averageEcs && averageEcs < ecsRightIntervalVector[^1];

                var result = (isMinMax, isLeftVector, isMiddleVector, isRightVector) switch
                {
                    (true, true, false, false) => ((false, false), nscLeftIntervalVector, ecsLeftIntervalVector),
                    (true, false, true, false) => ((Math.Abs(nscRightIntervalRightEndpointValue) >= Math.Abs(nscLeftIntervalLeftEndpointValue), true), nscMiddleIntervalVector, ecsMiddleIntervalVector),
                    (true, false, false, true) => ((true, false), nscRightIntervalVector, ecsRightIntervalVector),
                    (false, true, false, false) => ((false, true), nscLeftIntervalVector, ecsLeftIntervalVector),
                    (false, false, true, false) => ((Math.Abs(nscLeftIntervalLeftEndpointValue) >= Math.Abs(nscRightIntervalRightEndpointValue), false), nscMiddleIntervalVector, ecsMiddleIntervalVector),
                    (false, false, false, true) => ((true, true), nscRightIntervalVector, ecsRightIntervalVector),
                    _ => (((bool IsNscUseMaxValue, bool IsNscUsePositiveSlope) Result, Vector<double> NscIntervalVector, Vector<double> EcsIntervalVector)?)null
                };

                if (result is not null)
                {
                    var nscIntervalVector = result.Value.NscIntervalVector;
                    var ecsIntervalVector = result.Value.EcsIntervalVector;

                    ResultLaserAutoFocusDto.OriginalEcs = ecs;
                    ResultLaserAutoFocusDto.OriginalNsc = nsc;
                    ResultLaserAutoFocusDto.OriginalLvdt = lvdt;

                    ResultLaserAutoFocusDto.IsNscUseMaxValue = result.Value.Result.IsNscUseMaxValue;
                    ResultLaserAutoFocusDto.IsNscUsePositiveSlope = result.Value.Result.IsNscUsePositiveSlope;
                    var nscAbsMax = Math.Abs(nscIntervalVector.Maximum());
                    var nscAbsMin = Math.Abs(nscIntervalVector.Minimum());
                    ResultLaserAutoFocusDto.OriginalSymmetryRatio = nscAbsMax >= nscAbsMin ? nscAbsMax / nscAbsMin : nscAbsMin / nscAbsMax;
                    var isSymmetryOk = ResultLaserAutoFocusDto.OriginalSymmetryRatio <= Cache.ThresholdNscStandardSymmetryRatio;

                    ResultLaserAutoFocusDto.EcsToNmRange = (ecsIntervalVector[^1] - ecsIntervalVector[0]) * ecsToNmRatio;
                    ResultLaserAutoFocusDto.NscStandard = ResultLaserAutoFocusDto.EcsToNmRange * Cache.NscStandardNscPerNm;

                    var isNotOverflow = (ResultLaserAutoFocusDto.NscStandard / 2) <= short.MaxValue * 0.9;

                    var htmlBullet = new HtmlBullet(new
                    {
                        startEcs,
                        endEcs,
                        averageEcs,
                        isSymmetryOk,
                        isNotOverflow,
                        ResultLaserAutoFocusDto.IsNscUseMaxValue,
                        ResultLaserAutoFocusDto.IsNscUsePositiveSlope,
                        ResultLaserAutoFocusDto.OriginalSymmetryRatio,
                        ResultLaserAutoFocusDto.EcsToNmRange,
                        ResultLaserAutoFocusDto.NscStandard,
                        TraceBufferList = new HtmlPlot2DLinesChart([(nameof(ecs), ecs.ToPoints()), (nameof(nsc), nsc.ToPoints()), (nameof(lvdt), lvdt.ToPoints())], string.Empty)
                    });

                    if (isSymmetryOk && isNotOverflow)
                    {
                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                        return true;
                    }

                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                }
                else
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                    {
                        startEcs,
                        endEcs,
                        averageEcs,
                        TraceBufferList = new HtmlPlot2DLinesChart([(nameof(ecs), ecs.ToPoints()), (nameof(nsc), nsc.ToPoints()), (nameof(lvdt), lvdt.ToPoints())], string.Empty)
                    }), HtmlLogUniqueId.LoggingHtml());

                return false;
            }
            finally
            {
                AfViewModel.SetSensorNscCompensation(originOffset, originGain);
                AfViewModel.SetSensorCurrentValue(true, originCurrentAValue);
                AfViewModel.SetSensorCurrentValue(false, originCurrentBValue);
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(async () =>
        {
            Guard.IsNotNull(ResultLaserAutoFocusDto);

            var (originOffset, originGain) = AfViewModel.GetSensorNscCompensation();
            var originCurrentAValue = AfViewModel.GetSensorCurrentValue(true);
            var originCurrentBValue = AfViewModel.GetSensorCurrentValue(false);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                originOffset,
                originGain,
                originCurrentAValue,
                originCurrentBValue,
                Cache.MicroscopeLensInformation.LensName,
                Cache.FindPosition,
                Cache.HalfEcsLength,
                Cache.SpeedEcsPerSecond,
                Cache.NscStandardNscPerNm,
                Cache.ThresholdNscStandardSymmetryRatio,
                Cache.ThresholdNscStandardGain,
                Cache.CalibrationThresholdNscSymmetryRatio,
                Cache.CalibrationThresholdNscNscPerNmRange,
                Cache.RetryCount
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("Offset Gain", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            NscStandardList = [];
            NscStandardSelected = null;

            try
            {
                StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));
                AfViewModel.SetDarkFieldAutoFocus(null, OpticsMagTypeEnum.High, CalChipSiteModelEnum.ShinyWaferModel);

                AfViewModel.ResetSensorNscCompensation();
                await Task.Delay(100, cancellationToken);

                AfViewModel.SetSensorCurrentValue(true, ResultLaserAutoFocusDto.CurrentA);
                AfViewModel.SetSensorCurrentValue(false, ResultLaserAutoFocusDto.CurrentB);
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

                    AfViewModel.SetSensorNscCompensation(offset, gain);
                    AfViewModel.SetSensorEcsValue(startEcs);
                    await Task.Delay(100, cancellationToken);

                    var traceBufferList = AfViewModel.GetSensorNscTraceBufferList(startEcs, endEcs, Cache.SpeedEcsPerSecond, TimeSpan.FromSeconds(Math.Abs(endEcs - startEcs) / Cache.SpeedEcsPerSecond + 2));
                    var ecs = traceBufferList.Select(t => t.Ecs).ToArray();
                    var nsc = traceBufferList.Select(t => t.Nsc).ToArray();
                    var lvdt = traceBufferList.Select(t => t.Lvdt).ToArray();
                    var nscVector = Vector<double>.Build.DenseOfEnumerable(nsc);

                    Vector<double> nscIntervalVector;
                    if (ResultLaserAutoFocusDto.IsNscUseMaxValue)
                    {
                        var nscMaxIndex = nscVector.MaximumIndex();
                        var nscMinPositiveLeftIndex = nscVector.SubVectorRange(0, nscMaxIndex).MinimumIndex();
                        var nscMinNegativeRightIndex = nscVector.SubVectorRange(nscMaxIndex, nscVector.Count - 1).MinimumIndex() + nscMaxIndex;
                        nscIntervalVector = ResultLaserAutoFocusDto.IsNscUsePositiveSlope
                            ? nscVector.SubVectorRange(nscMinPositiveLeftIndex, nscMaxIndex)
                            : nscVector.SubVectorRange(nscMaxIndex, nscMinNegativeRightIndex);
                    }
                    else
                    {
                        var nscMinIndex = nscVector.MinimumIndex();
                        var nscMaxNegativeLeftIndex = nscVector.SubVectorRange(0, nscMinIndex).MaximumIndex();
                        var nscMaxPositiveRightIndex = nscVector.SubVectorRange(nscMinIndex, nscVector.Count - 1).MaximumIndex() + nscMinIndex;
                        nscIntervalVector = ResultLaserAutoFocusDto.IsNscUsePositiveSlope
                            ? nscVector.SubVectorRange(nscMinIndex, nscMaxPositiveRightIndex)
                            : nscVector.SubVectorRange(nscMaxNegativeLeftIndex, nscMinIndex);
                    }

                    var nscMax = nscIntervalVector.Maximum();
                    var nscMin = nscIntervalVector.Minimum();
                    var nscAbsMax = Math.Abs(nscIntervalVector.Maximum());
                    var nscAbsMin = Math.Abs(nscIntervalVector.Minimum());

                    var currentNscPerNm = Math.Abs((nscMax - nscMin) / ResultLaserAutoFocusDto.EcsToNmRange);
                    var currentSymmetryRatio = nscAbsMax >= nscAbsMin ? nscAbsMax / nscAbsMin : nscAbsMin / nscAbsMax;
                    var currentOffset = (nscMax + nscMin) / 2;
                    var currentGain = ResultLaserAutoFocusDto.NscStandard / (nscMax - nscMin);

                    var perNmIsOk = Math.Abs(Cache.NscStandardNscPerNm - currentNscPerNm) <= Cache.CalibrationThresholdNscNscPerNmRange;
                    var symmetryRatioIsOk = currentSymmetryRatio <= Cache.CalibrationThresholdNscSymmetryRatio;

                    if (symmetryRatioIsOk == false) offset += currentOffset / gain;
                    if (perNmIsOk == false) gain *= currentGain;

                    var item = new LaserAutoFocusDto
                    {
                        NscStandard = ResultLaserAutoFocusDto.NscStandard,
                        NscOffset = offset,
                        NscGain = gain,
                        NscCurrentNscPerNm = currentNscPerNm,
                        NscCurrentSymmetryRatio = currentSymmetryRatio,
                        CalibrationEcs = ecs,
                        CalibrationNsc = nsc,
                        CalibrationLvdt = lvdt
                    };

                    NscStandardList = [.. NscStandardList, item];
                    var isOverflow = item.NscGain > Cache.ThresholdNscStandardGain;

                    var htmlBullet = new HtmlBullet(new
                    {
                        startEcs,
                        endEcs,
                        averageEcs,
                        nscMax,
                        nscMin,
                        isOverflow,
                        ResultLaserAutoFocusDto.IsNscUseMaxValue,
                        ResultLaserAutoFocusDto.IsNscUsePositiveSlope,
                        ResultLaserAutoFocusDto.OriginalSymmetryRatio,
                        ResultLaserAutoFocusDto.EcsToNmRange,
                        ResultLaserAutoFocusDto.NscStandard,
                        item.NscOffset,
                        item.NscGain,
                        item.NscCurrentNscPerNm,
                        item.NscCurrentSymmetryRatio,
                        TraceBufferList = new HtmlPlot2DLinesChart([(nameof(ecs), ecs.ToPoints()), (nameof(nsc), nsc.ToPoints()), (nameof(lvdt), lvdt.ToPoints())], string.Empty)
                    });

                    if (symmetryRatioIsOk && perNmIsOk)
                    {
                        NscStandardSelected = item;

                        ResultLaserAutoFocusDto.NscOffset = NscStandardSelected.NscOffset;
                        ResultLaserAutoFocusDto.NscGain = NscStandardSelected.NscGain;
                        ResultLaserAutoFocusDto.NscCurrentNscPerNm = NscStandardSelected.NscCurrentNscPerNm;
                        ResultLaserAutoFocusDto.NscCurrentSymmetryRatio = NscStandardSelected.NscCurrentSymmetryRatio;
                        ResultLaserAutoFocusDto.CalibrationEcs = NscStandardSelected.CalibrationEcs;
                        ResultLaserAutoFocusDto.CalibrationNsc = NscStandardSelected.CalibrationNsc;
                        ResultLaserAutoFocusDto.CalibrationLvdt = NscStandardSelected.CalibrationLvdt;
                        if (isOverflow)
                        {
                            Logger.LogHtmlError($"time: {count} Error", HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                            return false;
                        }

                        Logger.LogHtmlInformation($"time: {count} OK", HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                        return true;
                    }

                    Logger.LogHtmlInformation($"time: {count}", HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                    if (++count > Cache.RetryCount) ThrowHelper.ThrowInvalidOperationException("Laser Auto Focus Retry Limit Exceeded");
                }
            }
            finally
            {
                AfViewModel.SetSensorNscCompensation(originOffset, originGain);
                AfViewModel.SetSensorCurrentValue(true, originCurrentAValue);
                AfViewModel.SetSensorCurrentValue(false, originCurrentBValue);
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

        await InvokeVerifyAsync(async () =>
        {
            ReviewDto.IsVerified = false;

            var (originOffset, originGain) = AfViewModel.GetSensorNscCompensation();
            var originCurrentAValue = AfViewModel.GetSensorCurrentValue(true);
            var originCurrentBValue = AfViewModel.GetSensorCurrentValue(false);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                originOffset,
                originGain,
                originCurrentAValue,
                originCurrentBValue,
                Cache.FindPosition,
                Cache.ThresholdIdealFMin,
                Cache.ThresholdIdealFMax,
                Cache.ThresholdIdealNMin,
                Cache.ThresholdIdealNMax,
                Cache.ThresholdCurrentMin,
                Cache.ThresholdCurrentMax,
                Cache.ReviewThresholdRangeRation,
                Cache.ReviewThresholdFMin,
                Cache.ReviewThresholdFMax,
                Cache.ReviewThresholdNMin,
                Cache.ReviewThresholdNMax,
                Cache.FindCurrentStep,
                Cache.HalfEcsLength,
                Cache.SpeedEcsPerSecond,
                Cache.NscStandardNscPerNm,
                Cache.ThresholdNscStandardSymmetryRatio,
                Cache.ThresholdNscStandardGain,
                Cache.CalibrationThresholdNscSymmetryRatio,
                Cache.CalibrationThresholdNscNscPerNmRange,
                Cache.RetryCount
            }), HtmlLogUniqueId.LoggingHtml());

            try
            {
                StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));
                AfViewModel.SetDarkFieldAutoFocus(null, OpticsMagTypeEnum.High, CalChipSiteModelEnum.ShinyWaferModel);

                AfViewModel.SetSensorNscCompensation(ReviewDto.NscOffset, ReviewDto.NscGain);
                await Task.Delay(100, cancellationToken);

                AfViewModel.SetSensorCurrentValue(true, ReviewDto.CurrentA);
                AfViewModel.SetSensorCurrentValue(false, ReviewDto.CurrentB);
                await Task.Delay(100, cancellationToken);

                AfViewModel.ToggleDarkFieldEnable(false);
                AfViewModel.GetSensorNscCurveIsOk();
                AfViewModel.ToggleDarkFieldEnable(true);
                await Task.Delay(100, cancellationToken);

                // 1. 使用校准后的电流，读当前的fa na fb nb
                var (fa, na) = AfViewModel.GetSensorFnValue(true);
                var (fb, nb) = AfViewModel.GetSensorFnValue(false);
                var averageEcs = AfViewModel.GetSensorAverageEcsValue();

                var startEcs = averageEcs - Cache.HalfEcsLength;
                var endEcs = averageEcs + Cache.HalfEcsLength;
                var traceBufferList = AfViewModel.GetSensorNscTraceBufferList(startEcs, endEcs, Cache.SpeedEcsPerSecond, TimeSpan.FromSeconds(Math.Abs(endEcs - startEcs) / Cache.SpeedEcsPerSecond + 2));
                var ecs = traceBufferList.Select(t => t.Ecs).ToArray();
                var nsc = traceBufferList.Select(t => t.Nsc).ToArray();
                var lvdt = traceBufferList.Select(t => t.Lvdt).ToArray();
                var nscVector = Vector<double>.Build.DenseOfEnumerable(nsc);

                Vector<double> nscIntervalVector;
                if (ReviewDto.IsNscUseMaxValue)
                {
                    var nscMaxIndex = nscVector.MaximumIndex();
                    var nscMinPositiveLeftIndex = nscVector.SubVectorRange(0, nscMaxIndex).MinimumIndex();
                    var nscMinNegativeRightIndex = nscVector.SubVectorRange(nscMaxIndex, nscVector.Count - 1).MinimumIndex() + nscMaxIndex;
                    nscIntervalVector = ReviewDto.IsNscUsePositiveSlope
                        ? nscVector.SubVectorRange(nscMinPositiveLeftIndex, nscMaxIndex)
                        : nscVector.SubVectorRange(nscMaxIndex, nscMinNegativeRightIndex);
                }
                else
                {
                    var nscMinIndex = nscVector.MinimumIndex();
                    var nscMaxNegativeLeftIndex = nscVector.SubVectorRange(0, nscMinIndex).MaximumIndex();
                    var nscMaxPositiveRightIndex = nscVector.SubVectorRange(nscMinIndex, nscVector.Count - 1).MaximumIndex() + nscMinIndex;
                    nscIntervalVector = ReviewDto.IsNscUsePositiveSlope
                        ? nscVector.SubVectorRange(nscMinIndex, nscMaxPositiveRightIndex)
                        : nscVector.SubVectorRange(nscMaxNegativeLeftIndex, nscMinIndex);
                }

                var nscMax = nscIntervalVector.Maximum();
                var nscMin = nscIntervalVector.Minimum();
                var currentNscPerNm = Math.Abs((nscMax - nscMin) / ReviewDto.EcsToNmRange);
                var currentSymmetryRatio = Math.Abs(nscMax / nscMin);

                var faIsOk = Cache.ReviewThresholdFMin < fa && fa < Cache.ReviewThresholdFMax;
                var naIsOk = Cache.ReviewThresholdNMin < na && na < Cache.ReviewThresholdNMax;
                var fbIsOk = Cache.ReviewThresholdFMin < fb && fb < Cache.ReviewThresholdFMax;
                var nbIsOk = Cache.ReviewThresholdNMin < nb && nb < Cache.ReviewThresholdNMax;

                var perNmIsOk = Math.Abs(Cache.NscStandardNscPerNm - currentNscPerNm) <= Cache.CalibrationThresholdNscNscPerNmRange;
                var symmetryRatioIsOk = currentSymmetryRatio <= Cache.CalibrationThresholdNscSymmetryRatio;
                var result = faIsOk && naIsOk && fbIsOk && nbIsOk && perNmIsOk && symmetryRatioIsOk;

                var htmlBullet = new HtmlBullet(new
                {
                    ReviewDtoCurrentA = ReviewDto.CurrentA,
                    ReviewDtoFa = ReviewDto.Fa,
                    ReviewDtoNa = ReviewDto.Na,
                    ReviewDtoCurrentB = ReviewDto.CurrentB,
                    ReviewDtoFb = ReviewDto.Fb,
                    ReviewDtoNb = ReviewDto.Nb,
                    ReviewDtoIsNscUseMaxValue = ReviewDto.IsNscUseMaxValue,
                    ReviewDtoIsNscUsePositiveSlope = ReviewDto.IsNscUsePositiveSlope,
                    ReviewDtoOriginalSymmetryRatio = ReviewDto.OriginalSymmetryRatio,
                    ReviewDtoEcsToNmRange = ReviewDto.EcsToNmRange,
                    ReviewDtoNscStandard = ReviewDto.NscStandard,
                    ReviewDtoNscOffset = ReviewDto.NscOffset,
                    ReviewDtoNscGain = ReviewDto.NscGain,
                    ReviewDtoNscCurrentNscPerNm = ReviewDto.NscCurrentNscPerNm,
                    ReviewDtoNscCurrentSymmetryRatio = ReviewDto.NscCurrentSymmetryRatio,
                    ReviewDtoTraceBufferList = new HtmlPlot2DLinesChart([
                        (nameof(ReviewDto.OriginalEcs), ReviewDto.OriginalEcs.ToPoints()),
                        (nameof(ReviewDto.OriginalNsc), ReviewDto.OriginalNsc.ToPoints()),
                        (nameof(ReviewDto.OriginalLvdt), ReviewDto.OriginalLvdt.ToPoints()),
                        (nameof(ReviewDto.CalibrationEcs), ReviewDto.CalibrationEcs.ToPoints()),
                        (nameof(ReviewDto.CalibrationNsc), ReviewDto.CalibrationNsc.ToPoints()),
                        (nameof(ReviewDto.CalibrationLvdt), ReviewDto.CalibrationLvdt.ToPoints())
                    ], string.Empty),
                    Fa = fa,
                    Na = na,
                    Fb = fb,
                    Nb = nb,
                    startEcs,
                    endEcs,
                    nscMax,
                    nscMin,
                    currentNscPerNm,
                    currentSymmetryRatio,
                    faIsOk,
                    naIsOk,
                    fbIsOk,
                    nbIsOk,
                    perNmIsOk,
                    symmetryRatioIsOk,
                    TraceBufferList = new HtmlPlot2DLinesChart([(nameof(ecs), ecs.ToPoints()), (nameof(nsc), nsc.ToPoints()), (nameof(lvdt), lvdt.ToPoints())], string.Empty)
                });

                if (result)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                ReviewDto.IsVerified = result;

                if (Save(ReviewDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name}Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    ReviewDto.IsVerified = false;
                    return false;
                }

                DialogWindowProvider.ShowDialog($"""
                                                 Verify:
                                                 {nameof(fa)}: {faIsOk}
                                                 {nameof(na)}: {naIsOk}
                                                 {nameof(fb)}: {fbIsOk}
                                                 {nameof(nb)}: {nbIsOk}
                                                 {nameof(currentNscPerNm)}: {perNmIsOk}
                                                 {nameof(currentSymmetryRatio)}: {symmetryRatioIsOk}
                                                 """, DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);

                return result;
            }
            finally
            {
                AfViewModel.SetSensorNscCompensation(originOffset, originGain);
                AfViewModel.SetSensorCurrentValue(true, originCurrentAValue);
                AfViewModel.SetSensorCurrentValue(false, originCurrentBValue);
            }
        }).ConfigureAwait(false);
    }

    private bool Save(LaserAutoFocusDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        CacheProvider.Set(dto, cancellationToken);
        CacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}