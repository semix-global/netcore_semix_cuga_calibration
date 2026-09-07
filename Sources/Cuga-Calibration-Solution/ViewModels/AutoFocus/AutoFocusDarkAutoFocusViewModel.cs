using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.AutoFocus.DarkAutoFocus;
using Core.Models.Models.Microscope.CalChip;
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
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using System.Collections.Concurrent;

namespace CugaCalibration.ViewModels.AutoFocus;

[IOCAppService(ServiceType = typeof(AutoFocusDarkAutoFocusViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AutoFocusDarkAutoFocusViewModel : CalibrationViewModelBase<DarkAutoFocusCache>
{
    #region 属性

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
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
    public partial DarkAutoFocusDTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial DarkAutoFocusNSCDTO? NscStandardSelected { get; set; }

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    public partial DarkAutoFocusDTO Review { get; set; } = new();

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial DarkAutoFocusCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial DarkAutoFocusDTO Calibration { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<DarkAutoFocusCache>(cancellationToken);
        Calibration = ApplicationCookieService.GetCalibration<DarkAutoFocusDTO>(cancellationToken);

        UpdateEntryStatus(Calibration, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);

        Cache.FindPosition = Guard.IsNotNullAndReturn(MicroscopeCalChip.ShinyWaferItem).BrightFieldMachinePosition;
        StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition), CalChipSiteModelEnum.ShinyWaferModel);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        Review = Calibration.Clone();

        if (Review.IsCalibrated == false) return false;

        await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
        StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition), CalChipSiteModelEnum.ShinyWaferModel);

        return true;
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
            new ConcurrentBag<(string Title, HtmlHeaderLevelEnum HeaderLevel, HtmlContainer Container, bool isOK)>();
        var lightBHtmlContainer =
            new ConcurrentBag<(string Title, HtmlHeaderLevelEnum HeaderLevel, HtmlContainer Container, bool isOK)>();

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

                StageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition), CalChipSiteModelEnum.ShinyWaferModel);
                CIBViewModel.SetGlobalRTFCParams(ApplicationCookie.OILowProductivityInformation);

                AfViewModel.ResetSensorNscCompensation();
                await Task.Delay(100, cancellationToken);

                AfViewModel.ToggleDarkFieldEnable(true);
                await Task.Delay(100, cancellationToken);

                var currentACalibrationTask = Task.Run(() => GetCurrentResultAsync(CalibratingItem.CurrentADTO, true),
                    cancellationToken);
                var currentBCalibrationTask = Task.Run(() => GetCurrentResultAsync(CalibratingItem.CurrentBDTO, false),
                    cancellationToken);

                await Task.WhenAll(currentACalibrationTask, currentBCalibrationTask);

                var result = await currentACalibrationTask && await currentBCalibrationTask;

                if (result)
                {
                    CalibratingItem.LowCoefficient = Cache.LowCoefficient;
                    CalibratingItem.HighCoefficient = Cache.HighCoefficient;
                }

                OnPropertyChanged(nameof(CalibratingItem));

                Logger.LogHtmlInformation("A Brightness", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                foreach (var (title, headerLevel, container, isOk) in lightAHtmlContainer)
                {
                    if (isOk == false) Logger.LogHtmlHeaderIsError(headerLevel, container, HtmlLogUniqueId.LoggingHtml());
                    else Logger.LogHtmlInformation(title, headerLevel, container, HtmlLogUniqueId.LoggingHtml());
                }

                Logger.LogHtmlInformation("B Brightness", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                foreach (var (title, headerLevel, container, isOk) in lightBHtmlContainer)
                {
                    if (isOk == false) Logger.LogHtmlHeaderIsError(headerLevel, container, HtmlLogUniqueId.LoggingHtml());
                    else Logger.LogHtmlInformation(title, headerLevel, container, HtmlLogUniqueId.LoggingHtml());
                }

                Logger.LogHtmlInformation($"Calibration {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3,
                    new HtmlBullet(new
                    {
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
                var errorLogComment = new HtmlComment($"{(isA ? "A" : "B")} Current: F/N linear fit is not monotonically increasing!");
                if (isA)
                    lightAHtmlContainer.Add(("Error", HtmlHeaderLevelEnum.Header4, new HtmlContainer([errorLogComment]), false));
                else
                    lightBHtmlContainer.Add(("Error", HtmlHeaderLevelEnum.Header4, new HtmlContainer([errorLogComment]), false));

                if (HostEnvironment.IsDevelopment() == false) return false;
            }

            #endregion

            #region Aligorithm

            double CurrentFromF(double f) => (f - interceptF) / slopeF;
            double CurrentFromN(double n) => (n - interceptN) / slopeN;

            double FFromCurrent(double current) => slopeF * current + interceptF;
            double NFromCurrent(double current) => slopeN * current + interceptN;

            // Current range satisfying F ∈ [FMin, FMax]
            var fCurrentMin = CurrentFromF(Cache.CalibratingThresholdFMin);
            var fCurrentMax = CurrentFromF(Cache.CalibratingThresholdFMax);

            var fMin = FFromCurrent(fCurrentMin);
            var fMax = FFromCurrent(fCurrentMax);

            // Current range satisfying N ∈ [NMin, NMax]
            var nCurrentMin = CurrentFromN(Cache.CalibratingThresholdNMin);
            var nCurrentMax = CurrentFromN(Cache.CalibratingThresholdNMax);

            var nMin = NFromCurrent(nCurrentMin);
            var nMax = NFromCurrent(nCurrentMax);

            currentDTO.FDomain = new Point(fCurrentMin, fCurrentMax);
            currentDTO.NDomain = new Point(nCurrentMin, nCurrentMax);

            // 求三个区间（F值域对应电流范围、N值域对应电流范围、定义域）的交集，交集不能为空
            var intersectMin = Math.Max(Cache.ThresholdCurrentMin, Math.Max(fCurrentMin, nCurrentMin));
            var intersectMax = Math.Min(Cache.ThresholdCurrentMax, Math.Min(fCurrentMax, nCurrentMax));

            var fIntersectionRangeMin = FFromCurrent(intersectMin);
            var fIntersectionRangeMax = FFromCurrent(intersectMax);

            var nIntersectionRangeMin = NFromCurrent(intersectMin);
            var nIntersectionRangeMax = NFromCurrent(intersectMax);

            var middleCurrent = (intersectMin + intersectMax) / 2d;
            currentDTO.ResultDTO = new DarkAutoFocusCurrentDTOItem
            {
                Current = middleCurrent,
                F = slopeF * middleCurrent + interceptF,
                N = slopeN * middleCurrent + interceptN
            };

            var rangeHtmlBullet = new HtmlBullet(new
            {
                CurrentDomain = $"[{Cache.ThresholdCurrentMin}, {Cache.ThresholdCurrentMax}]",
                FDomain = $"[{fCurrentMin:0.###}, {fCurrentMax:0.###}]",
                FRange = $"[{fMin:0.###}, {fMax:0.###}]",
                NDomain = $"[{nCurrentMin:0.###}, {nCurrentMax:0.###}]",
                NRange = $"[{nMin:0.###}, {nMax:0.###}]",
                IntersectDomain = $"[{intersectMin:0.###}, {intersectMax:0.###}]",
                IntersectFRange = $"[{fIntersectionRangeMin:0.###}, {fIntersectionRangeMax:0.###}]",
                IntersectNRange = $"[{nIntersectionRangeMin:0.###}, {nIntersectionRangeMax:0.###}]",
                Plots = new HtmlQuote(currentDTO.ToFlatnessHtmlAnonymous())
            });
            if (isA)
                lightAHtmlContainer.Add(("Intersection", HtmlHeaderLevelEnum.Header4, new HtmlContainer([rangeHtmlBullet]), true));
            else
                lightBHtmlContainer.Add(("Intersection", HtmlHeaderLevelEnum.Header4, new HtmlContainer([rangeHtmlBullet]), true));

            if (intersectMin > intersectMax)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlBullet(new { Message = $"{(isA ? "A" : "B")} Current: Intersection of valid current ranges is empty." }), HtmlLogUniqueId.LoggingHtml());

                if (HostEnvironment.IsDevelopment() == false) return false;
            }

            #endregion

            #region result

            AfViewModel.SetSensorCurrentValue(isA, currentDTO.ResultDTO.Current);
            await Task.Delay(1000, cancellationToken);

            var (verifyF, verifyN) = AfViewModel.GetSensorFnValue(isA);
            var verifyResult = verifyF <= Cache.CalibratingThresholdFMax && verifyF >= Cache.CalibratingThresholdFMin &&
                               verifyN <= Cache.CalibratingThresholdNMax && verifyN >= Cache.CalibratingThresholdNMin;

            var resultHtmlBullet = new HtmlBullet(new
            {
                Messege = $"Light {(isA ? "A" : "B")} {(verifyResult ? "Success" : "Failed:Real F/N out of the threshold")}",
                CalibrationCurrent = middleCurrent,
                IdealF = currentDTO.ResultDTO.F,
                IdealN = currentDTO.ResultDTO.N,
                RealF = verifyF,
                RealN = verifyN,
            });
            if (isA)
                lightAHtmlContainer.Add(("Result", HtmlHeaderLevelEnum.Header4, new HtmlContainer([resultHtmlBullet]), verifyResult));
            else
                lightBHtmlContainer.Add(("Result", HtmlHeaderLevelEnum.Header4, new HtmlContainer([resultHtmlBullet]), verifyResult));

            #endregion

            if (verifyResult == false && HostEnvironment.IsDevelopment() == false) return false;

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
                Cache.EcsRange,
                Cache.SpeedEcsPerSecond,
                Cache.NscStandardNscPerNm,
                Cache.ThresholdNscStandardSymmetryRatio,
                Cache.ThresholdNscStandardGain
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("Nsc Profile", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            try
            {
                StageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition), CalChipSiteModelEnum.ShinyWaferModel);
                CIBViewModel.SetGlobalRTFCParams(ApplicationCookie.OILowProductivityInformation);

                AfViewModel.ResetSensorNscCompensation();
                await Task.Delay(100, cancellationToken);

                AfViewModel.SetSensorCurrentValue(true, CalibratingItem.CurrentA);
                AfViewModel.SetSensorCurrentValue(false, CalibratingItem.CurrentB);
                await Task.Delay(100, cancellationToken);

                AfViewModel.ToggleDarkFieldEnable(true);
                await Task.Delay(100, cancellationToken);

                var averageEcs = AfViewModel.GetSensorAverageEcsValue();
                var startEcs = averageEcs - Cache.EcsRange;
                var endEcs = averageEcs + Cache.EcsRange;

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
                        Plot = new HtmlContainer(CalibratingItem.NSCProfileResultDTO.PlotDataSource
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
                            Plot = CalibratingItem.NSCProfileResultDTO.PlotDataSource.GetHtmlPlot2DLinesChart(0)
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
                Cache.EcsRange,
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
                StageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition), CalChipSiteModelEnum.ShinyWaferModel);
                CIBViewModel.SetGlobalRTFCParams(ApplicationCookie.OILowProductivityInformation);

                AfViewModel.ResetSensorNscCompensation();
                await Task.Delay(100, cancellationToken);

                AfViewModel.SetSensorCurrentValue(true, CalibratingItem.CurrentA);
                AfViewModel.SetSensorCurrentValue(false, CalibratingItem.CurrentB);
                await Task.Delay(100, cancellationToken);

                AfViewModel.ToggleDarkFieldEnable(true);
                await Task.Delay(100, cancellationToken);

                var nmPerEcs = AfViewModel.GetNmPerEcs() * 1000;

                var averageEcs = AfViewModel.GetSensorAverageEcsValue();
                var startEcs = averageEcs - Cache.EcsRange;
                var endEcs = averageEcs + Cache.EcsRange;

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
                        Plot = new HtmlContainer(CalibratingItem.NSCProfileResultDTO.PlotDataSource
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

            StageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition), CalChipSiteModelEnum.ShinyWaferModel);
            CIBViewModel.SetGlobalRTFCParams(ApplicationCookie.OILowProductivityInformation);

            var originPosition = AfViewModel.GetDarkFieldAutoFocusMotorAbsoluteValue();
            CalibratingItem.AfMotor = originPosition;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                originOffset,
                originGain,
                originCurrentAValue,
                originCurrentBValue,
                originPosition,
                Cache.MicroscopeLensInformation.LensName,
                Cache.FindPosition,
                Cache.AFMotorRange,
                Cache.StepAFMotorAbsoluteValue,
                Cache.RSquaredThreshold
            }), HtmlLogUniqueId.LoggingHtml());

            try
            {
                var afMotorAbsoluteValues = Generate.LinearRangeContainsEdge(originPosition - Cache.AFMotorRange, Cache.StepAFMotorAbsoluteValue, originPosition + Cache.AFMotorRange);
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

                var result = rSquared >= Cache.RSquaredThreshold;

                Logger.LogHtmlInformation("Slope Result", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    CalibratingItem.EcsMotorPositionRelationSlope,
                    CalibratingItem.EcsMotorPositionRelationIntercept,
                    CalibratingItem.EcsMotorPositionRelationRSquare,
                    CalibratingItem.MinAFMotorAbsoluteValue,
                    CalibratingItem.MaxAFMotorAbsoluteValue,
                    Plot = new HtmlContainer([.. CalibratingItem.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
                }), HtmlLogUniqueId.LoggingHtml());

                CalibratingItem.IsCalibrated = result;
                Guard.IsTrue(Save(CalibratingItem, cancellationToken));

                return result;
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
                Cache.EcsRange,
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
                StageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition), CalChipSiteModelEnum.ShinyWaferModel);
                CIBViewModel.SetGlobalRTFCParams(ApplicationCookie.OILowProductivityInformation);

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

                var startEcs = averageEcs - Cache.EcsRange;
                var endEcs = averageEcs + Cache.EcsRange;
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
                    ReviewPlot = Review.NSCGainResultDTO.PlotDataSource.GetHtmlPlot2DLinesChart(0),
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
                    Plot = new HtmlContainer([.. Review.NSCGainResultDTO.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
                });

                if (result)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                var reviewDTO = Calibration.Clone();
                Review.IsVerified = reviewDTO.IsVerified = result;

                Guard.IsTrue(Save(reviewDTO, cancellationToken));

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
                    AfViewModel.SetSensorNscCompensation(0d /* af下发不使用 */, Review.NSCGainResultDTO.NscGain);
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

        ApplicationCookieService.SetCalibration(dto, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase calibration, CancellationToken cancellationToken)
    {
        var temp = Guard.IsAssignableToTypeAndReturn<DarkAutoFocusDTO>(calibration);
        var status = Entry.Status;

        Calibration = temp;

        status.TotalCalibrationCount = 1;
        status.CalibratedCount = Calibration.IsCalibrated ? 1 : 0;
        status.VerifiedCount = Calibration.IsVerified ? 1 : 0;
        status.Details = [];
    }

    #endregion 校准
}