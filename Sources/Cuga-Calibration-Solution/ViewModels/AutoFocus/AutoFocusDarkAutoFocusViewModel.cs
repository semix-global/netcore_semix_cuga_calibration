using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models;
using Core.Models.Models.AutoFocus.DarkAutoFocus;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.CalChip;
using Core.Utilities.SourceGenerators.Attributes;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Hosting;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.Concurrent;

namespace CugaCalibration.ViewModels.AutoFocus;

[IOCAppService(ServiceType = typeof(AutoFocusDarkAutoFocusViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AutoFocusDarkAutoFocusViewModel : CalibrationViewModelBase
{
    #region 属性

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Param" },
        new() { StepName = "AB Brightness" },
        new() { StepName = "NSC Profile" },
        new() { StepName = "Nsc Offset Gain" },
        new() { StepName = "AF Motor Calibration " }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private DarkAutoFocusDTO _calibratingItem = new();

    [ObservableProperty]
    private DarkAutoFocusNSCDTO? _nscStandardSelected;

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private DarkAutoFocusDTO _review = new();

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private DarkAutoFocusCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private DarkAutoFocusDTO _calibration = new();

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<DarkAutoFocusCache>();
        Calibration = CacheProvider.GetOrDefault<DarkAutoFocusDTO>();

        if (Cache.MicroscopeLensInformation == MicroscopeLensInformation.Default)
            Cache.MicroscopeLensInformation =
                CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
        Cache.FindPosition = Guard.IsNotNullAndReturn(MicroscopeCalChip.ShinyWaferItem).BrightFieldMachinePosition;
        StageViewModel.SetCalChipShinyWaferBrightFieldAbsoluteStageXy(
            StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Review = Calibration.Clone();

        if (Review.IsCalibrated == false) return false;

        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
        StageViewModel.SetCalChipShinyWaferBrightFieldAbsoluteStageXy(
            StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));

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
                Guard.IsNotNull(CalibratingItem);

                CalibratingItem.IsCalibrated = true;
                if (Save(CalibratingItem, cancellationToken) == false)
                {
                    CalibratingItem.IsCalibrated = false;
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3,
                        new HtmlComment($"{Name} Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
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

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Cache.FindPosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.FindPosition,
                Cache.MicroscopeLensInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.MicroscopeLensInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var lightAHtmlContainer =
            new ConcurrentBag<(string Title, HtmlHeaderLevelEnum HeaderLevel, HtmlContainer Container)>();
        var lightBHtmlContainer =
            new ConcurrentBag<(string Title, HtmlHeaderLevelEnum HeaderLevel, HtmlContainer Container)>();

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
                Cache.CalibratingThresholdRangeRatio,
                Cache.CalibratingThresholdFMin,
                Cache.CalibratingThresholdFMax,
                Cache.CalibratingThresholdNMin,
                Cache.CalibratingThresholdNMax,
                Cache.FindCurrentStart,
                Cache.FindCurrentStep,
                Cache.FindCurrentStop
            }), HtmlLogUniqueId.LoggingHtml());

            if (Cache.ThresholdIdealFMin >= Cache.ThresholdIdealFMax ||
                Cache.ThresholdIdealNMin >= Cache.ThresholdIdealNMax)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3,
                    new HtmlComment(
                        $"{nameof(DarkAutoFocusCache.ThresholdIdealFMin)} >= {nameof(DarkAutoFocusCache.ThresholdIdealFMax)} || {nameof(DarkAutoFocusCache.ThresholdIdealNMin)} >= {nameof(DarkAutoFocusCache.ThresholdIdealNMax)}"),
                    HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            try
            {
                CalibratingItem = new DarkAutoFocusDTO();

                StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(
                    StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));
                CIBViewModel.ToggleRTFCParam(ApplicationCookie.OILowProductivityInformation);

                AfViewModel.ResetSensorNscCompensation();
                await Task.Delay(100, cancellationToken);

                AfViewModel.ToggleDarkFieldEnable(true);
                await Task.Delay(100, cancellationToken);

                var currentACalibrationTask = Task.Run(() => GetCurrentResultAsync(CalibratingItem.CurrentADTO, true),
                    cancellationToken);
                var currentBCalibrationTask = Task.Run(() => GetCurrentResultAsync(CalibratingItem.CurrentBDTO, false),
                    cancellationToken);

                await Task.WhenAll(currentBCalibrationTask, currentBCalibrationTask);

                var result = await currentACalibrationTask && await currentBCalibrationTask;

                if (result)
                {
                    CalibratingItem.LowCoefficient = Cache.LowCoefficient;
                    CalibratingItem.HighCoefficient = Cache.HighCoefficient;
                }

                Logger.LogHtmlInformation("A Brightness", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                foreach (var (title, headerLevel, container) in lightAHtmlContainer)
                {
                    Logger.LogHtmlInformation(title, headerLevel,
                        container, HtmlLogUniqueId.LoggingHtml());
                }

                Logger.LogHtmlInformation("B Brightness", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                foreach (var (title, headerLevel, container) in lightBHtmlContainer)
                {
                    Logger.LogHtmlInformation(title, headerLevel,
                        container, HtmlLogUniqueId.LoggingHtml());
                }

                Logger.LogHtmlInformation($"Calibration {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3,
                    new HtmlBullet(new
                    {
                        CurrentAResult = new HtmlQuote(CalibratingItem.CurrentADTO.ToFlatnessHtmlAnonymous()),
                        CurrentBResult = new HtmlQuote(CalibratingItem.CurrentBDTO.ToFlatnessHtmlAnonymous()),
                        CalibratingItem.LowCoefficient,
                        CalibratingItem.HighCoefficient,
                        LowCurrentA = CalibratingItem.LowCoefficient * CalibratingItem.CurrentA,
                        LowCurrentB = CalibratingItem.LowCoefficient * CalibratingItem.CurrentB,
                        HighCurrentA = CalibratingItem.HighCoefficient * CalibratingItem.CurrentA,
                        HighCurrentB = CalibratingItem.HighCoefficient * CalibratingItem.CurrentB
                    }), HtmlLogUniqueId.LoggingHtml());

                return result;
            }
            finally
            {
                AfViewModel.SetSensorNscCompensation(originOffset, originGain);
                AfViewModel.SetSensorCurrentValue(true, originCurrentAValue);
                AfViewModel.SetSensorCurrentValue(false, originCurrentBValue);
            }
        }).ConfigureAwait(false);
        return;

        async Task<bool> GetCurrentResultAsync(DarkAutoFocusCurrentDTO currentDTO, bool isA)
        {
            #region F/N Fit1

            foreach (var currentValue in Generate.LinearRange(Cache.FindCurrentStart, Cache.FindCurrentStep,
                         Cache.FindCurrentStop))
            {
                cancellationToken.ThrowIfCancellationRequested();

                AfViewModel.SetSensorCurrentValue(isA, currentValue);
                await Task.Delay(1000, cancellationToken);

                var (f, n) = AfViewModel.GetSensorFnValue(isA);
                var currentDTOItem = new DarkAutoFocusCurrentDTOItem()
                {
                    Current = currentValue,
                    F = f,
                    N = n
                };
                currentDTO.CurrentItems = [.. currentDTO.CurrentItems, currentDTOItem];
            }

            if (currentDTO.CurrentItems.Count == 0) return false;

            // F
            var (slopeF, interceptF, rSquaredF, yPredictedF) = PolynomialCurve.Fit1(
                Vector<double>.Build.Dense([.. currentDTO.CurrentItems.Select(t => t.Current)]),
                Vector<double>.Build.Dense([.. currentDTO.CurrentItems.Select(t => t.F)]));

            currentDTO.SlopeF = slopeF;
            currentDTO.InterceptF = interceptF;
            currentDTO.RSquaredF = rSquaredF;
            currentDTO.FitCurrentPointsF =
                [.. currentDTO.CurrentItems.Index().Select(t => new Point(t.Item.Current, yPredictedF[t.Index]))];

            // N
            var (slopeN, interceptN, rSquaredN, yPredictedN) = PolynomialCurve.Fit1(
                Vector<double>.Build.Dense([.. currentDTO.CurrentItems.Select(t => t.Current)]),
                Vector<double>.Build.Dense([.. currentDTO.CurrentItems.Select(t => t.N)]));

            currentDTO.SlopeN = slopeN;
            currentDTO.InterceptN = interceptN;
            currentDTO.RSquaredN = rSquaredN;
            currentDTO.FitCurrentPointsN =
                [.. currentDTO.CurrentItems.Index().Select(t => new Point(t.Item.Current, yPredictedN[t.Index]))];

            if (slopeF <= 0 || slopeN <= 0)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3,
                    new HtmlComment($"{(isA ? "A" : "B")} Current: F/N linear fit is not monotonically increasing!"),
                    HtmlLogUniqueId.LoggingHtml());
                if (HostEnvironment.IsDevelopment() == false) return false;
            }

            #endregion

            #region Aligorithm

            double CurrentFromF(double f) => (f - interceptF) / slopeF;
            double CurrentFromN(double n) => (n - interceptN) / slopeN;

            // Current range satisfying F ∈ [FMin, FMax]
            var fCurrentMin = CurrentFromF(Cache.CalibratingThresholdFMin);
            var fCurrentMax = CurrentFromF(Cache.CalibratingThresholdFMax);

            // Current range satisfying N ∈ [NMin, NMax]
            var nCurrentMin = CurrentFromN(Cache.CalibratingThresholdNMin);
            var nCurrentMax = CurrentFromN(Cache.CalibratingThresholdNMax);

            currentDTO.FDomain = new Point(fCurrentMin, fCurrentMax);
            currentDTO.NDomain = new Point(nCurrentMin, nCurrentMax);

            // 求三个区间（F值域对应电流范围、N值域对应电流范围、定义域）的交集，交集不能为空
            var intersectMin = Math.Max(Cache.ThresholdCurrentMin, Math.Max(fCurrentMin, nCurrentMin));
            var intersectMax = Math.Min(Cache.ThresholdCurrentMax, Math.Min(fCurrentMax, nCurrentMax));

            var rangeHtmlBullet = new HtmlBullet(new
            {
                Domain = $"[{Cache.ThresholdCurrentMin}, {Cache.ThresholdCurrentMax}]",
                FCurrentRange = $"[{fCurrentMin:0.###}, {fCurrentMax:0.###}]",
                NCurrentRange = $"[{nCurrentMin:0.###}, {nCurrentMax:0.###}]",
                IntersectRange = $"[{intersectMin:0.###}, {intersectMax:0.###}]"
            });
            if (isA)
                lightAHtmlContainer.Add(("Intersection", HtmlHeaderLevelEnum.Header4,
                    new HtmlContainer([rangeHtmlBullet])));
            else
                lightBHtmlContainer.Add(("Intersection", HtmlHeaderLevelEnum.Header4,
                    new HtmlContainer([rangeHtmlBullet])));

            if (intersectMin > intersectMax)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlBullet(new { Message = $"{(isA ? "A" : "B")} Current: Intersection of valid current ranges is empty." }), HtmlLogUniqueId.LoggingHtml());

                if (HostEnvironment.IsDevelopment() == false) return false;
            }

            #endregion

            #region Verify

            var middleCurrent = (intersectMin + intersectMax) / 2d;
            AfViewModel.SetSensorCurrentValue(isA, middleCurrent);
            await Task.Delay(1000, cancellationToken);

            var (verifyF, verifyN) = AfViewModel.GetSensorFnValue(isA);

            var verifyHtmlBullet = new HtmlBullet(new
            {
                verifyCurrent = middleCurrent,
                verifyF,
                verifyN
            });

            if (isA)
                lightAHtmlContainer.Add(("Verify", HtmlHeaderLevelEnum.Header4, new HtmlContainer([verifyHtmlBullet])));
            else
                lightBHtmlContainer.Add(("Verify", HtmlHeaderLevelEnum.Header4, new HtmlContainer([verifyHtmlBullet])));

            if (verifyF > Cache.CalibratingThresholdFMax || verifyF < Cache.CalibratingThresholdFMin ||
                verifyN > Cache.CalibratingThresholdNMax || verifyN < Cache.CalibratingThresholdNMin)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4,
                    new HtmlBullet(new { Message = $"{(isA ? "A" : "B")} Current: Verify Failed." }),
                    HtmlLogUniqueId.LoggingHtml());
                if (HostEnvironment.IsDevelopment() == false) return false;
            }

            currentDTO.ResultDTO = new DarkAutoFocusCurrentDTOItem
            {
                Current = middleCurrent,
                F = slopeF * middleCurrent + interceptF,
                N = slopeN * middleCurrent + interceptN
            };

            var resultHtmlBullet = new HtmlBullet(new
            {
                CurrentResult = middleCurrent,
                currentDTO.ResultDTO.F,
                currentDTO.ResultDTO.N
            });

            if (isA)
                lightAHtmlContainer.Add(("Result", HtmlHeaderLevelEnum.Header4, new HtmlContainer([resultHtmlBullet])));
            else
                lightBHtmlContainer.Add(("Result", HtmlHeaderLevelEnum.Header4, new HtmlContainer([resultHtmlBullet])));

            #endregion

            return true;
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(async () =>
        {
            var (originOffset, originGain) = AfViewModel.GetSensorNscCompensation();
            var originCurrentAValue = AfViewModel.GetSensorCurrentValue(true);
            var originCurrentBValue = AfViewModel.GetSensorCurrentValue(false);
            var nmPerEcs = AfViewModel.GetNmPerEcs();

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                originOffset,
                originGain,
                originCurrentAValue,
                originCurrentBValue,
                nmPerEcs,
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
                StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(
                    StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));
                CIBViewModel.ToggleRTFCParam(ApplicationCookie.OILowProductivityInformation);

                AfViewModel.ResetSensorNscCompensation();
                await Task.Delay(100, cancellationToken);

                AfViewModel.SetSensorCurrentValue(true, CalibratingItem.CurrentA);
                AfViewModel.SetSensorCurrentValue(false, CalibratingItem.CurrentB);
                await Task.Delay(100, cancellationToken);

                AfViewModel.ToggleDarkFieldEnable(true);
                await Task.Delay(100, cancellationToken);

                var averageEcs = AfViewModel.GetSensorAverageEcsValue();
                var startEcs = averageEcs - Cache.HalfEcsLength;
                var endEcs = averageEcs + Cache.HalfEcsLength;

                AfViewModel.SetSensorEcsValue(startEcs);
                await Task.Delay(100, cancellationToken);
                var traceBufferList = AfViewModel.GetSensorNscTraceBufferList(startEcs, endEcs, Cache.SpeedEcsPerSecond,
                    TimeSpan.FromSeconds(Math.Abs(endEcs - startEcs) / Cache.SpeedEcsPerSecond + 2));
                var ecs = traceBufferList.Select(t => t.Ecs).ToArray();
                var nsc = traceBufferList.Select(t => t.Nsc).ToArray();
                var lvdt = traceBufferList.Select(t => t.Lvdt).ToArray();
                var fa = traceBufferList.Select(t => t.Fa).ToArray();
                var na = traceBufferList.Select(t => t.Na).ToArray();
                var fb = traceBufferList.Select(t => t.Fb).ToArray();
                var nb = traceBufferList.Select(t => t.Nb).ToArray();

                CalibratingItem.NSCProfileResultDTO.CalibrationEcs = ecs;
                CalibratingItem.NSCProfileResultDTO.CalibrationNsc = nsc;
                CalibratingItem.NSCProfileResultDTO.CalibrationLvdt = lvdt;
                CalibratingItem.NSCProfileResultDTO.CalibrationFa = fa;
                CalibratingItem.NSCProfileResultDTO.CalibrationNa = na;
                CalibratingItem.NSCProfileResultDTO.CalibrationFb = fb;
                CalibratingItem.NSCProfileResultDTO.CalibrationNb = nb;

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
                    var nscNegativeRightIndex =
                        nscVector.SubVectorRange(nscMaxIndex, nscVector.Count - 1).MinimumIndex() + nscMaxIndex;

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
                    var nscPositiveRightIndex =
                        nscVector.SubVectorRange(nscMinIndex, nscVector.Count - 1).MaximumIndex() + nscMinIndex;

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

                var isLeftVector = nscLeftIntervalVector[0] * nscLeftIntervalVector[^1] < 0 &&
                                   ecsLeftIntervalVector[0] < averageEcs && averageEcs < ecsLeftIntervalVector[^1];
                var isMiddleVector = nscMiddleIntervalVector[0] * nscMiddleIntervalVector[^1] < 0 &&
                                     ecsMiddleIntervalVector[0] < averageEcs &&
                                     averageEcs < ecsMiddleIntervalVector[^1];
                var isRightVector = nscRightIntervalVector[0] * nscRightIntervalVector[^1] < 0 &&
                                    ecsRightIntervalVector[0] < averageEcs && averageEcs < ecsRightIntervalVector[^1];

                var result = (isMinMax, isLeftVector, isMiddleVector, isRightVector) switch
                {
                    (true, true, false, false) => ((false, false), nscLeftIntervalVector, ecsLeftIntervalVector),
                    (true, false, true, false) => (
                        (Math.Abs(nscRightIntervalRightEndpointValue) >= Math.Abs(nscLeftIntervalLeftEndpointValue),
                            true), nscMiddleIntervalVector, ecsMiddleIntervalVector),
                    (true, false, false, true) => ((true, false), nscRightIntervalVector, ecsRightIntervalVector),
                    (false, true, false, false) => ((false, true), nscLeftIntervalVector, ecsLeftIntervalVector),
                    (false, false, true, false) => (
                        (Math.Abs(nscLeftIntervalLeftEndpointValue) >= Math.Abs(nscRightIntervalRightEndpointValue),
                            false), nscMiddleIntervalVector, ecsMiddleIntervalVector),
                    (false, false, false, true) => ((true, true), nscRightIntervalVector, ecsRightIntervalVector),
                    _ => (((bool IsNscUseMaxValue, bool IsNscUsePositiveSlope) Result, Vector<double> NscIntervalVector,
                        Vector<double> EcsIntervalVector)?)null
                };

                if (result is not null)
                {
                    var nscIntervalVector = result.Value.NscIntervalVector;
                    var ecsIntervalVector = result.Value.EcsIntervalVector;

                    CalibratingItem.NSCProfileResultDTO.CalibrationEcsNscPoints =
                    [
                        .. CalibratingItem.NSCProfileResultDTO.CalibrationEcs
                            .Select((t, index) => new Point(t * nmPerEcs,
                                CalibratingItem.NSCProfileResultDTO.CalibrationNsc[index]))
                    ];
                    CalibratingItem.NSCProfileResultDTO.CalibrationEcsNscMaxMins =
                    [
                        new Point(ecsIntervalVector[0] * nmPerEcs, nscIntervalVector[0]),
                        new Point(ecsIntervalVector[^1] * nmPerEcs, nscIntervalVector[^1])
                    ];

                    CalibratingItem.IsNscUseMaxValue = result.Value.Result.IsNscUseMaxValue;
                    CalibratingItem.IsNscUsePositiveSlope = result.Value.Result.IsNscUsePositiveSlope;
                    var nscAbsMax = Math.Abs(nscIntervalVector.Maximum());
                    var nscAbsMin = Math.Abs(nscIntervalVector.Minimum());
                    CalibratingItem.OriginalSymmetryRatio =
                        nscAbsMax >= nscAbsMin ? nscAbsMax / nscAbsMin : nscAbsMin / nscAbsMax;
                    var isSymmetryOk = CalibratingItem.OriginalSymmetryRatio <= Cache.ThresholdNscStandardSymmetryRatio;

                    CalibratingItem.EcsToNmRange = (ecsIntervalVector[^1] - ecsIntervalVector[0]) * nmPerEcs;
                    CalibratingItem.NscStandard = CalibratingItem.EcsToNmRange * Cache.NscStandardNscPerNm;

                    var isNotOverflow = (CalibratingItem.NscStandard / 2) <= short.MaxValue * 0.9;

                    var htmlBullet = new HtmlBullet(new
                    {
                        startEcs,
                        endEcs,
                        averageEcs,
                        isSymmetryOk,
                        isNotOverflow,
                        CalibratingItem.IsNscUseMaxValue,
                        CalibratingItem.IsNscUsePositiveSlope,
                        CalibratingItem.OriginalSymmetryRatio,
                        CalibratingItem.EcsToNmRange,
                        CalibratingItem.NscStandard,
                        Plot = new HtmlContainer(CalibratingItem.NSCProfileResultDTO.ScatterPlotControl
                            .GetAllHtmlPlot2DLinesCharts())
                    });

                    if (isSymmetryOk && isNotOverflow)
                    {
                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, htmlBullet,
                            HtmlLogUniqueId.LoggingHtml());
                        return true;
                    }

                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                }
                else
                    Logger.LogHtmlError(
                        "NSC zero point not found. Please check whether the AF motor, ECS, slope, and other related configurations are correctly set.",
                        HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                        {
                            startEcs,
                            endEcs,
                            averageEcs,
                            Plot = CalibratingItem.NSCProfileResultDTO.ScatterPlotControl.GetHtmlPlot2DLinesChart(0)
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
            Guard.IsNotNull(CalibratingItem);

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

            CalibratingItem.NSCGainDTOItems = [];
            try
            {
                StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(
                    StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));
                CIBViewModel.ToggleRTFCParam(ApplicationCookie.OILowProductivityInformation);

                AfViewModel.ResetSensorNscCompensation();
                await Task.Delay(100, cancellationToken);

                AfViewModel.SetSensorCurrentValue(true, CalibratingItem.CurrentA);
                AfViewModel.SetSensorCurrentValue(false, CalibratingItem.CurrentB);
                await Task.Delay(100, cancellationToken);

                AfViewModel.ToggleDarkFieldEnable(true);
                await Task.Delay(100, cancellationToken);

                var nmPerEcs = AfViewModel.GetNmPerEcs() * 1000;

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

                    var traceBufferList = AfViewModel.GetSensorNscTraceBufferList(startEcs, endEcs,
                        Cache.SpeedEcsPerSecond,
                        TimeSpan.FromSeconds(Math.Abs(endEcs - startEcs) / Cache.SpeedEcsPerSecond + 2));
                    var ecs = traceBufferList.Select(t => t.Ecs).ToArray();
                    var nsc = traceBufferList.Select(t => t.Nsc).ToArray();
                    var lvdt = traceBufferList.Select(t => t.Lvdt).ToArray();
                    var fa = traceBufferList.Select(t => t.Fa).ToArray();
                    var na = traceBufferList.Select(t => t.Na).ToArray();
                    var fb = traceBufferList.Select(t => t.Fb).ToArray();
                    var nb = traceBufferList.Select(t => t.Nb).ToArray();
                    var nscVector = Vector<double>.Build.DenseOfEnumerable(nsc);
                    var ecsVector = Vector<double>.Build.DenseOfEnumerable(ecs);

                    Vector<double> nscIntervalVector, ecsIntervalVector;
                    if (CalibratingItem.IsNscUseMaxValue)
                    {
                        var nscMaxIndex = nscVector.MaximumIndex();
                        var nscMinPositiveLeftIndex = nscVector.SubVectorRange(0, nscMaxIndex).MinimumIndex();
                        var nscMinNegativeRightIndex =
                            nscVector.SubVectorRange(nscMaxIndex, nscVector.Count - 1).MinimumIndex() + nscMaxIndex;
                        nscIntervalVector = CalibratingItem.IsNscUsePositiveSlope
                            ? nscVector.SubVectorRange(nscMinPositiveLeftIndex, nscMaxIndex)
                            : nscVector.SubVectorRange(nscMaxIndex, nscMinNegativeRightIndex);
                        ecsIntervalVector = CalibratingItem.IsNscUsePositiveSlope
                            ? ecsVector.SubVectorRange(nscMinPositiveLeftIndex, nscMaxIndex)
                            : ecsVector.SubVectorRange(nscMaxIndex, nscMinNegativeRightIndex);
                    }
                    else
                    {
                        var nscMinIndex = nscVector.MinimumIndex();
                        var nscMaxNegativeLeftIndex = nscVector.SubVectorRange(0, nscMinIndex).MaximumIndex();
                        var nscMaxPositiveRightIndex =
                            nscVector.SubVectorRange(nscMinIndex, nscVector.Count - 1).MaximumIndex() + nscMinIndex;
                        nscIntervalVector = CalibratingItem.IsNscUsePositiveSlope
                            ? nscVector.SubVectorRange(nscMinIndex, nscMaxPositiveRightIndex)
                            : nscVector.SubVectorRange(nscMaxNegativeLeftIndex, nscMinIndex);
                        ecsIntervalVector = CalibratingItem.IsNscUsePositiveSlope
                            ? ecsVector.SubVectorRange(nscMinIndex, nscMaxPositiveRightIndex)
                            : ecsVector.SubVectorRange(nscMaxNegativeLeftIndex, nscMinIndex);
                    }

                    var calibrationEcsNscPoints = (Point[])
                        [.. ecs.Index().Select(t => new Point(t.Item * nmPerEcs, nsc[t.Index]))];
                    var calibrationEcsNscMaxMins = (Point[])
                    [
                        new Point(ecsIntervalVector[0] * nmPerEcs, nscIntervalVector[0]),
                        new Point(ecsIntervalVector[^1] * nmPerEcs, nscIntervalVector[^1])
                    ];

                    var nscMax = nscIntervalVector.Maximum();
                    var nscMin = nscIntervalVector.Minimum();
                    var nscAbsMax = Math.Abs(nscIntervalVector.Maximum());
                    var nscAbsMin = Math.Abs(nscIntervalVector.Minimum());

                    var currentNscPerNm = Math.Abs((nscMax - nscMin) / CalibratingItem.EcsToNmRange);
                    var currentSymmetryRatio = nscAbsMax >= nscAbsMin ? nscAbsMax / nscAbsMin : nscAbsMin / nscAbsMax;
                    var currentOffset = (nscMax + nscMin) / 2;
                    var currentGain = CalibratingItem.NscStandard / (nscMax - nscMin);

                    var perNmIsOk = Math.Abs(Cache.NscStandardNscPerNm - currentNscPerNm) <=
                                    Cache.CalibrationThresholdNscNscPerNmRange;
                    var symmetryRatioIsOk = currentSymmetryRatio <= Cache.CalibrationThresholdNscSymmetryRatio;

                    if (symmetryRatioIsOk == false) offset += currentOffset / gain;
                    if (perNmIsOk == false) gain *= currentGain;

                    var nscGainResultDTO = new DarkAutoFocusNSCDTO
                    {
                        NscOffset = offset,
                        NscGain = gain,
                        NscCurrentNscPerNm = currentNscPerNm,
                        NscCurrentSymmetryRatio = currentSymmetryRatio,
                        CalibrationEcs = ecs,
                        CalibrationNsc = nsc,
                        CalibrationLvdt = lvdt,
                        CalibrationFa = fa,
                        CalibrationNa = na,
                        CalibrationFb = fb,
                        CalibrationNb = nb,
                        CalibrationEcsNscPoints = calibrationEcsNscPoints,
                        CalibrationEcsNscMaxMins = calibrationEcsNscMaxMins
                    };

                    CalibratingItem.NSCGainDTOItems = [.. CalibratingItem.NSCGainDTOItems, nscGainResultDTO];
                    var isOverflow = nscGainResultDTO.NscGain > Cache.ThresholdNscStandardGain;

                    var htmlBullet = new HtmlBullet(new
                    {
                        startEcs,
                        endEcs,
                        averageEcs,
                        nscMax,
                        nscMin,
                        isOverflow,
                        CalibratingItem.IsNscUseMaxValue,
                        CalibratingItem.IsNscUsePositiveSlope,
                        CalibratingItem.OriginalSymmetryRatio,
                        CalibratingItem.EcsToNmRange,
                        CalibratingItem.NscStandard,
                        nscGainResultDTO.NscOffset,
                        nscGainResultDTO.NscGain,
                        nscGainResultDTO.NscCurrentNscPerNm,
                        nscGainResultDTO.NscCurrentSymmetryRatio,
                        Plot = new HtmlContainer(CalibratingItem.NSCProfileResultDTO.ScatterPlotControl
                            .GetAllHtmlPlot2DLinesCharts())
                    });

                    if (symmetryRatioIsOk && perNmIsOk)
                    {
                        NscStandardSelected = nscGainResultDTO;

                        CalibratingItem.NSCGainResultDTO.NscOffset = NscStandardSelected.NscOffset;
                        CalibratingItem.NSCGainResultDTO.NscGain = NscStandardSelected.NscGain;
                        CalibratingItem.NSCGainResultDTO.NscCurrentNscPerNm = NscStandardSelected.NscCurrentNscPerNm;
                        CalibratingItem.NSCGainResultDTO.NscCurrentSymmetryRatio =
                            NscStandardSelected.NscCurrentSymmetryRatio;
                        CalibratingItem.NSCGainResultDTO.CalibrationEcs = NscStandardSelected.CalibrationEcs;
                        CalibratingItem.NSCGainResultDTO.CalibrationNsc = NscStandardSelected.CalibrationNsc;
                        CalibratingItem.NSCGainResultDTO.CalibrationLvdt = NscStandardSelected.CalibrationLvdt;
                        CalibratingItem.NSCGainResultDTO.CalibrationFa = NscStandardSelected.CalibrationFa;
                        CalibratingItem.NSCGainResultDTO.CalibrationNa = NscStandardSelected.CalibrationNa;
                        CalibratingItem.NSCGainResultDTO.CalibrationFb = NscStandardSelected.CalibrationFb;
                        CalibratingItem.NSCGainResultDTO.CalibrationNb = NscStandardSelected.CalibrationNb;
                        CalibratingItem.NSCGainResultDTO.CalibrationEcsNscPoints =
                            NscStandardSelected.CalibrationEcsNscPoints;
                        CalibratingItem.NSCGainResultDTO.CalibrationEcsNscMaxMins =
                            NscStandardSelected.CalibrationEcsNscMaxMins;

                        if (isOverflow)
                        {
                            Logger.LogHtmlError($"time: {count} Error", HtmlHeaderLevelEnum.Header4, htmlBullet,
                                HtmlLogUniqueId.LoggingHtml());

                            return false;
                        }

                        Logger.LogHtmlInformation($"time: {count} OK", HtmlHeaderLevelEnum.Header4, htmlBullet,
                            HtmlLogUniqueId.LoggingHtml());
                        return true;
                    }

                    Logger.LogHtmlInformation($"time: {count}", HtmlHeaderLevelEnum.Header4, htmlBullet,
                        HtmlLogUniqueId.LoggingHtml());

                    if (++count > Cache.RetryCount)
                        ThrowHelper.ThrowInvalidOperationException("Laser Auto Focus Retry Limit Exceeded");
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
    private async Task Step4CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(async () =>
        {
            Guard.IsNotNull(CalibratingItem);
            Guard.IsNotNull(Cache);

            var (originOffset, originGain) = AfViewModel.GetSensorNscCompensation();
            var originCurrentAValue = AfViewModel.GetSensorCurrentValue(true);
            var originCurrentBValue = AfViewModel.GetSensorCurrentValue(false);

            CalibratingItem.EcsMotorPositionRelationSlope = 0;
            CalibratingItem.EcsMotorPositionRelationIntercept = 0;
            CalibratingItem.EcsMotorPositionRelationRSquare = 0;
            CalibratingItem.ECSMotorOrigins = [];
            CalibratingItem.FitECSMotorOrigins = [];

            StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(
                StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));
            CIBViewModel.ToggleRTFCParam(ApplicationCookie.OILowProductivityInformation);

            var originPosition = AfViewModel.GetDarkFieldAutoFocusMotorAbsoluteValue();

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                originOffset,
                originGain,
                originCurrentAValue,
                originCurrentBValue,
                originPosition,
                Cache.MicroscopeLensInformation.LensName,
                Cache.FindPosition,
                Cache.StartAFMotorAbsoluteValue,
                Cache.StepAFMotorAbsoluteValue,
                Cache.StopAFMotorAbsoluteValue
            }), HtmlLogUniqueId.LoggingHtml());

            try
            {
                var afMotorAbsoluteValues = Generate.LinearRangeContainsEdge(Cache.StartAFMotorAbsoluteValue, Cache.StepAFMotorAbsoluteValue, Cache.StopAFMotorAbsoluteValue);
                var closestIndex = afMotorAbsoluteValues
                    .Index()
                    .OrderBy(x => Math.Abs(x.Item - originPosition))
                    .First()
                    .Index;

                AfViewModel.SetDarkFieldAutoFocusMotorAbsoluteValue(originPosition);
                AfViewModel.ToggleDarkFieldEnable(true);

                foreach (var position in afMotorAbsoluteValues.AsSpan()[closestIndex..].ToArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    AfViewModel.SetDarkFieldAutoFocusMotorAbsoluteValue(position);

                    await Task.Delay(3600, cancellationToken);

                    var point0 = new Point(position, AfViewModel.GetSensorAverageEcsValue());

                    CalibratingItem.ECSMotorOrigins =
                        [.. ((IReadOnlyList<Point>)[.. CalibratingItem.ECSMotorOrigins, point0]).OrderBy(t => t.X)];
                }

                AfViewModel.ToggleBrightFieldEnable(false);
                AfViewModel.SetDarkFieldAutoFocusMotorAbsoluteValue(originPosition);
                AfViewModel.ToggleDarkFieldEnable(true);

                foreach (var position in afMotorAbsoluteValues.AsSpan()[..closestIndex].ToArray().AsEnumerable()
                             .Reverse().ToArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    AfViewModel.SetDarkFieldAutoFocusMotorAbsoluteValue(position);

                    await Task.Delay(3600, cancellationToken);

                    var point0 = new Point(position, AfViewModel.GetSensorAverageEcsValue());

                    CalibratingItem.ECSMotorOrigins =
                        [.. ((IReadOnlyList<Point>)[.. CalibratingItem.ECSMotorOrigins, point0]).OrderBy(t => t.X)];
                }

                var (slope, intercept, rSquared, yPredicted) = PolynomialCurve.Fit1(
                    Vector<double>.Build.Dense([.. CalibratingItem.ECSMotorOrigins.Select(t => t.X)]),
                    Vector<double>.Build.Dense([.. CalibratingItem.ECSMotorOrigins.Select(t => t.Y)]));

                CalibratingItem.FitECSMotorOrigins =
                    [.. CalibratingItem.ECSMotorOrigins.Index().Select(t => new Point(t.Item.X, yPredicted[t.Index]))];

                CalibratingItem.MinAFMotorAbsoluteValue = CalibratingItem.FitECSMotorOrigins[0].X;
                CalibratingItem.MaxAFMotorAbsoluteValue = CalibratingItem.FitECSMotorOrigins[^1].X;
                CalibratingItem.EcsMotorPositionRelationSlope = slope;
                CalibratingItem.EcsMotorPositionRelationIntercept = intercept;
                CalibratingItem.EcsMotorPositionRelationRSquare = rSquared;

                Logger.LogHtmlInformation("Slope Result", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    CalibratingItem.EcsMotorPositionRelationSlope,
                    CalibratingItem.EcsMotorPositionRelationIntercept,
                    CalibratingItem.EcsMotorPositionRelationRSquare,
                    CalibratingItem.MinAFMotorAbsoluteValue,
                    CalibratingItem.MaxAFMotorAbsoluteValue,
                    Plot = new HtmlContainer([.. CalibratingItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                }), HtmlLogUniqueId.LoggingHtml());

                return true;
            }
            finally
            {
                AfViewModel.SetSensorNscCompensation(originOffset, originGain);
                AfViewModel.SetSensorCurrentValue(true, originCurrentAValue);
                AfViewModel.SetSensorCurrentValue(false, originCurrentBValue);
                AfViewModel.SetDarkFieldAutoFocusMotorAbsoluteValue(originPosition);
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(async () =>
        {
            Review.IsVerified = false;

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
                Cache.ReviewThresholdRangeRatio,
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
                StageViewModel.SetCalChipShinyWaferDarkFieldAbsoluteStageXyByNotAutoFocus(
                    StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));
                CIBViewModel.ToggleRTFCParam(ApplicationCookie.OILowProductivityInformation);

                AfViewModel.SetSensorNscCompensation(Review.NSCGainResultDTO.NscOffset,
                    Review.NSCGainResultDTO.NscGain);
                await Task.Delay(100, cancellationToken);

                AfViewModel.SetSensorCurrentValue(true, Review.CurrentA);
                AfViewModel.SetSensorCurrentValue(false, Review.CurrentB);
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
                var traceBufferList = AfViewModel.GetSensorNscTraceBufferList(startEcs, endEcs, Cache.SpeedEcsPerSecond,
                    TimeSpan.FromSeconds(Math.Abs(endEcs - startEcs) / Cache.SpeedEcsPerSecond + 2));
                var ecsBuffer = traceBufferList.Select(t => t.Ecs).ToArray();
                var nscBuffer = traceBufferList.Select(t => t.Nsc).ToArray();
                var lvdtBuffer = traceBufferList.Select(t => t.Lvdt).ToArray();
                var faBuffer = traceBufferList.Select(t => t.Fa).ToArray();
                var naBuffer = traceBufferList.Select(t => t.Na).ToArray();
                var fbBuffer = traceBufferList.Select(t => t.Fb).ToArray();
                var nbBuffer = traceBufferList.Select(t => t.Nb).ToArray();
                var nscVector = Vector<double>.Build.DenseOfEnumerable(nscBuffer);

                Vector<double> nscIntervalVector;
                if (Review.IsNscUseMaxValue)
                {
                    var nscMaxIndex = nscVector.MaximumIndex();
                    var nscMinPositiveLeftIndex = nscVector.SubVectorRange(0, nscMaxIndex).MinimumIndex();
                    var nscMinNegativeRightIndex =
                        nscVector.SubVectorRange(nscMaxIndex, nscVector.Count - 1).MinimumIndex() + nscMaxIndex;
                    nscIntervalVector = Review.IsNscUsePositiveSlope
                        ? nscVector.SubVectorRange(nscMinPositiveLeftIndex, nscMaxIndex)
                        : nscVector.SubVectorRange(nscMaxIndex, nscMinNegativeRightIndex);
                }
                else
                {
                    var nscMinIndex = nscVector.MinimumIndex();
                    var nscMaxNegativeLeftIndex = nscVector.SubVectorRange(0, nscMinIndex).MaximumIndex();
                    var nscMaxPositiveRightIndex =
                        nscVector.SubVectorRange(nscMinIndex, nscVector.Count - 1).MaximumIndex() + nscMinIndex;
                    nscIntervalVector = Review.IsNscUsePositiveSlope
                        ? nscVector.SubVectorRange(nscMinIndex, nscMaxPositiveRightIndex)
                        : nscVector.SubVectorRange(nscMaxNegativeLeftIndex, nscMinIndex);
                }

                var nscMax = nscIntervalVector.Maximum();
                var nscMin = nscIntervalVector.Minimum();
                var currentNscPerNm = Math.Abs((nscMax - nscMin) / Review.EcsToNmRange);
                var currentSymmetryRatio = Math.Abs(nscMax / nscMin);

                var faIsOk = Cache.ReviewThresholdFMin < fa && fa < Cache.ReviewThresholdFMax;
                var naIsOk = Cache.ReviewThresholdNMin < na && na < Cache.ReviewThresholdNMax;
                var fbIsOk = Cache.ReviewThresholdFMin < fb && fb < Cache.ReviewThresholdFMax;
                var nbIsOk = Cache.ReviewThresholdNMin < nb && nb < Cache.ReviewThresholdNMax;

                var perNmIsOk = Math.Abs(Cache.NscStandardNscPerNm - currentNscPerNm) <=
                                Cache.CalibrationThresholdNscNscPerNmRange;
                var symmetryRatioIsOk = currentSymmetryRatio <= Cache.CalibrationThresholdNscSymmetryRatio;
                var result = faIsOk && naIsOk && fbIsOk && nbIsOk && perNmIsOk && symmetryRatioIsOk;

                Review.CurrentADTO.ResultDTO.F = fa;
                Review.CurrentADTO.ResultDTO.N = na;
                Review.CurrentBDTO.ResultDTO.F = fb;
                Review.CurrentBDTO.ResultDTO.N = nb;
                Review.NSCGainResultDTO.CalibrationFa = faBuffer;
                Review.NSCGainResultDTO.CalibrationNa = naBuffer;
                Review.NSCGainResultDTO.CalibrationFb = fbBuffer;
                Review.NSCGainResultDTO.CalibrationNb = nbBuffer;
                Review.NSCGainResultDTO.CalibrationEcs = ecsBuffer;
                Review.NSCGainResultDTO.CalibrationNsc = nscBuffer;
                Review.NSCGainResultDTO.CalibrationLvdt = lvdtBuffer;
                var htmlBullet = new HtmlBullet(new
                {
                    ReviewDtoCurrentA = Review.CurrentA,
                    ReviewDtoFa = Review.Fa,
                    ReviewDtoNa = Review.Na,
                    ReviewDtoCurrentB = Review.CurrentB,
                    ReviewDtoFb = Review.Fb,
                    ReviewDtoNb = Review.Nb,
                    ReviewDtoIsNscUseMaxValue = Review.IsNscUseMaxValue,
                    ReviewDtoIsNscUsePositiveSlope = Review.IsNscUsePositiveSlope,
                    ReviewDtoOriginalSymmetryRatio = Review.OriginalSymmetryRatio,
                    ReviewDtoEcsToNmRange = Review.EcsToNmRange,
                    ReviewDtoNscStandard = Review.NscStandard,
                    ReviewDtoNscOffset = Review.NSCGainResultDTO.NscOffset,
                    ReviewDtoNscGain = Review.NSCGainResultDTO.NscGain,
                    ReviewDtoNscCurrentNscPerNm = Review.NSCGainResultDTO.NscCurrentNscPerNm,
                    ReviewDtoNscCurrentSymmetryRatio = Review.NSCGainResultDTO.NscCurrentSymmetryRatio,
                    ReviewPlot = Review.NSCGainResultDTO.ScatterPlotControl.GetHtmlPlot2DLinesChart(0),
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
                    Plot = new HtmlContainer([.. Review.NSCGainResultDTO.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                });

                if (result)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                var reviewDTO = Calibration.Clone();
                Review.IsVerified = reviewDTO.IsVerified = result;

                if (Save(reviewDTO, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3,
                        new HtmlComment($"{Name}Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    Review.IsVerified = false;
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
                                                 """, DialogButtonsEnum.OK,
                    result ? DialogIconEnum.Information : DialogIconEnum.Warning);

                if (result)
                {
                    AfViewModel.SetSensorNscCompensation(0d/* af下发不使用 */, Review.NSCGainResultDTO.NscGain);
                    await Task.Delay(100, cancellationToken);

                    AfViewModel.SetSensorCurrentValue(true, Review.CurrentA);
                    AfViewModel.SetSensorCurrentValue(false, Review.CurrentB);
                    await Task.Delay(100, cancellationToken);
                }

                return result;
            }
            finally
            {
                if (Review.IsVerified == false)
                {
                    AfViewModel.SetSensorNscCompensation(originOffset, originGain);
                    AfViewModel.SetSensorCurrentValue(true, originCurrentAValue);
                    AfViewModel.SetSensorCurrentValue(false, originCurrentBValue);
                }
            }
        }).ConfigureAwait(false);
    }

    private bool Save(DarkAutoFocusDTO dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        CacheProvider.Set(dto, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}