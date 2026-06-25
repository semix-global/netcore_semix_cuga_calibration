using System.Collections;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.AutoFocus.DarkAutoFocus;
using Core.Models.Models.AutoFocus.FAFBCompensation;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.CalChip;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.AutoFocus;

[IOCAppService(ServiceType = typeof(AutoFocusFAFBCompensationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AutoFocusFAFBCompensationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Image Param" },
        new() { StepName = "Alignment" },
        new() { StepName = "Compensation" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial AutoFocusFAFBCompensationDTO CalibratingItem { get; set; } = new();

    #endregion Calibrate

    [ObservableProperty]
    public partial AutoFocusFAFBCompensationDTO Review { get; set; } = new();

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public partial AutoFocusFAFBCompensationCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial AutoFocusFAFBCompensationDTO Calibration { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    [ObservableProperty]
    public partial DarkAutoFocusDTO DarkAutoFocus { get; set; } = new();

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = HostApplication.GetRequiredService<AlignmentUserControlViewModel>();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
        DarkAutoFocus = ApplicationCookieService.GetCalibration<DarkAutoFocusDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<AutoFocusFAFBCompensationCache>(cancellationToken);
        Calibration = ApplicationCookieService.GetCalibration<AutoFocusFAFBCompensationDTO>(cancellationToken);

        if (Cache.MicroscopeLensInformation == MicroscopeLensInformation.Default)
            Cache.MicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();

        UpdateEntryStatus(Calibration, cancellationToken);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Review = Calibration.Clone();

        return Review.IsCalibrated;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                CalibratingItem = new AutoFocusFAFBCompensationDTO();

                return true;

            case 1:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Items.ElementAtOrDefault(0)?.DSWFindBrightMachinePosition ?? MicroscopeCalChip.DswItem.BrightFieldMachinePosition));

                return true;

            case 2:
                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

                return true;

            default:
                return true;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand]
    private void AddAutoFocusFAFBCompensationCacheItem()
    {
        var cacheItemList = Cache.Items.ToList();
        cacheItemList.Add(new AutoFocusFAFBCompensationCacheItem());

        Cache.Items = cacheItemList;
    }

    [RelayCommand]
    private void RemoveAutoFocusFAFBCompensationCacheItem(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var cacheItemList = Cache.Items.ToList();
        foreach (AutoFocusFAFBCompensationCacheItem selectItem in selectItems) cacheItemList.Remove(selectItem);

        Cache.Items = cacheItemList;
    }

    [RelayCommand]
    private void MarkDSWFindBrightMachinePosition(AutoFocusFAFBCompensationCacheItem? items)
    {
        if (items is null) return;

        Guard.IsEqualTo(Cache.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

        items.DSWFindBrightMachinePosition = StageViewModel.GetMachineStagePosition();
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step0Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(() =>
    {
        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Cache.MicroscopeLensInformation
        }), HtmlLogUniqueId.LoggingHtml());

        return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.MicroscopeLensInformation);
    });

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step1Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () =>
    {
        AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
        AlignmentUserControlViewModel.IsDarkFieldAlignment = false;

        await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

        Cache.AlignmentResult = AlignmentUserControlViewModel.AlignmentResult;

        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            Cache.MicroscopeLensInformation,
            AlignmentResult = new HtmlQuote(Cache.AlignmentResult.ToHtmlAnonymous())
        }), HtmlLogUniqueId.LoggingHtml());

        return true;
    });

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step2Async(CancellationToken cancellationToken) => await InvokeCalibrateAsync(async () =>
    {
        Guard.IsGreaterThan(Cache.Items.Count, 0);

        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Cache.MicroscopeLensInformation,
            AlignmentResult = new HtmlQuote(Cache.AlignmentResult.ToHtmlAnonymous()),
            Cache.RangeECS,
            Cache.SpeedECSPerSecond,
            Points = new HtmlTable([.. Cache.Items.Select(t => new { t.DSWFindBrightMachinePosition })]),
            Cache.ThresholdECS
        }), HtmlLogUniqueId.LoggingHtml());

        CalibratingItem.KA = 0d;
        CalibratingItem.OffsetA = 0d;
        CalibratingItem.FARSquared = 0d;
        CalibratingItem.KB = 0d;
        CalibratingItem.OffsetB = 0d;
        CalibratingItem.FBRSquared = 0d;
        CalibratingItem.CalibratingItems = [];
        CalibratingItem.VerifyItems = [];
        CalibratingItem.LeastSquaresMinECS = null;
        CalibratingItem.LeastSquaresMaxECS = null;
        CalibratingItem.LeastSquareFindPoints = [];
        foreach (var dtoItem in CalibratingItem.CalibratingItems)
        {
            dtoItem.FACompensations = [];
            dtoItem.FBCompensations = [];
            dtoItem.NSCCompensations = [];
            dtoItem.NSCZeroPoint = null;
        }

        AfViewModel.ResetFAFBCompensation();
        await Task.Delay(100, cancellationToken);

        Logger.LogHtmlInformation("Get S Curve", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
        await GetNSCCurvesAsync(true, cancellationToken);

        Logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
        Algorithm(CalibratingItem);

        Guard.IsTrue(Save(CalibratingItem, cancellationToken));

        return CalibratingItem.IsCalibrated;
    }).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task AlgorithmAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlInformation("Algorithm Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ThresholdECS
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
            Algorithm(CalibratingItem);

            Guard.IsTrue(Save(CalibratingItem, cancellationToken));

            DialogWindowProvider.ShowDialog($"Algorithm {(CalibratingItem.IsCalibrated ? "OK" : "Failed")}",
                DialogButtonsEnum.OK,
                CalibratingItem.IsCalibrated ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return CalibratingItem.IsCalibrated;
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> VerifyAsync(CancellationToken cancellationToken) => await InvokeVerifyAsync(async () =>
    {
        Review.IsVerified = false;
        Review.VerifyItems = [];

        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Cache.MicroscopeLensInformation,
            AlignmentResult = new HtmlQuote(Cache.AlignmentResult.ToHtmlAnonymous()),
            Cache.RangeECS,
            Cache.SpeedECSPerSecond,
            Points = new HtmlTable([.. Cache.Items.Select(t => new { t.DSWFindBrightMachinePosition })]),
            Cache.ThresholdECS
        }), HtmlLogUniqueId.LoggingHtml());

        AfViewModel.SetFAFBCompensation(Review.KA, Review.OffsetA, Review.KB, Review.OffsetB);
        await Task.Delay(100, cancellationToken);

        await GetNSCCurvesAsync(false, cancellationToken);

        var ecses = Review.VerifyItems.Select(t => Guard.IsNotNullAndReturn(t.NSCZeroPoint).X).ToArray();
        var error = Math.Abs(ecses.Max() - ecses.Min());
        var isOk = error < Cache.ThresholdECS;

        Review.IsVerified = isOk;

        var htmlBullet = new HtmlBullet(new
        {
            Review.KA,
            Review.OffsetA,
            Review.KB,
            Review.OffsetB,
            ecses,
            error,
            isOk,
            CalibratingPlotDataSource = new HtmlContainer([.. Review.CalibratingPlotDataSource.GetAllHtmlPlot2DLinesCharts()]),
            VerifyPlotDataSource = new HtmlContainer([.. Review.VerifyPlotDataSource.GetAllHtmlPlot2DLinesCharts()])
        });

        if (isOk) Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
        else Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

        Guard.IsTrue(Save(Review, cancellationToken));

        DialogWindowProvider.ShowDialog($"""
                                         Verify:
                                         {nameof(ecses)}: {string.Join(", ", ecses)}
                                         {nameof(error)}: {error}
                                         """, DialogButtonsEnum.OK,
            isOk ? DialogIconEnum.Information : DialogIconEnum.Warning);

        if (isOk) AfViewModel.SetFAFBCompensation(Review.KA, Review.OffsetA, Review.KB, Review.OffsetB);
        else AfViewModel.ResetFAFBCompensation();

        await Task.Delay(100, cancellationToken);

        return isOk;
    }).ConfigureAwait(false);

    #endregion 校准

    private async Task GetNSCCurvesAsync(bool isCalibrating, CancellationToken cancellationToken)
    {
        var item = isCalibrating ? CalibratingItem : Review;

        var brightFieldPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Items[0].DSWFindBrightMachinePosition);
        try
        {
            foreach (var cacheItem in Cache.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Logger.LogHtmlInformation($"{cacheItem.DSWFindBrightMachinePosition}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                StageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(cacheItem.DSWFindBrightMachinePosition), CalChipSiteModelEnum.DswModel);

                CIBViewModel.ToggleRTFCParam(ApplicationCookie.ProductivityInformations[0]);
                await Task.Delay(100, cancellationToken);

                var (ecsMin, ecsMax) = AfViewModel.GetEcsMoveRange();

                AfViewModel.ToggleDarkFieldEnable(true);
                await Task.Delay(100, cancellationToken);

                var averageEcs = AfViewModel.GetSensorAverageEcsValue();

                var startECS = Math.Clamp(averageEcs - Cache.RangeECS, ecsMin, ecsMax);
                var stopECS = Math.Clamp(averageEcs + Cache.RangeECS, ecsMin, ecsMax);

                AfViewModel.SetSensorEcsValue(startECS);
                await Task.Delay(100, cancellationToken);

                var traceBufferList = AfViewModel.GetSensorNscTraceBufferList(
                    startECS,
                    stopECS,
                    Cache.SpeedECSPerSecond,
                    TimeSpan.FromSeconds(Math.Abs(stopECS - startECS) / Cache.SpeedECSPerSecond + 2d));

                var dtoItem = new AutoFocusFAFBCompensationDTOItem
                {
                    DSWFindBrightMachinePosition = cacheItem.DSWFindBrightMachinePosition,
                    AverageECS = averageEcs,
                    ECSes = [..traceBufferList.Select(t => t.Ecs)],
                    FAs = [..traceBufferList.Select(t => t.Fa)],
                    NAs = [..traceBufferList.Select(t => t.Na)],
                    FBs = [..traceBufferList.Select(t => t.Fb)],
                    NBs = [..traceBufferList.Select(t => t.Nb)],
                    NSCs = [..traceBufferList.Select(t => t.Nsc)]
                };

                bool isSuccess;
                if (isCalibrating)
                {
                    isSuccess = true;
                    item.CalibratingItems = [..item.CalibratingItems, dtoItem];
                }
                else
                {
                    (isSuccess, dtoItem.NSCZeroPoint) = GetNSCCurveZeroPoint(averageEcs, dtoItem.ECSes, dtoItem.NSCs);
                    item.VerifyItems = [..item.VerifyItems, dtoItem];
                }

                var htmlBullet = new HtmlBullet(new
                {
                    dtoItem.DSWFindBrightMachinePosition,
                    dtoItem.AverageECS,
                    ecsMin,
                    ecsMax,
                    startECS,
                    stopECS,
                    PlotDataSource = new HtmlContainer([.. (isCalibrating ? item.CalibratingPlotDataSource : item.VerifyPlotDataSource).GetAllHtmlPlot2DLinesCharts()])
                });

                if (isSuccess)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                    ThrowHelper.ThrowArgumentException("NSC zero point not found. Please check whether the AF motor, ECS, slope, and other related configurations are correctly set.");
                }
            }
        }
        finally
        {
            StageViewModel.SetBrightFieldAbsoluteStageXy(brightFieldPosition);
        }
    }

    private void Algorithm(AutoFocusFAFBCompensationDTO item)
    {
        var confirmViewModel = HostApplication.GetRequiredService<AutoFocusFAFBCompensationConfirmECSRangeWindowViewModel>();
        confirmViewModel.LeastSquaresMinECS = item.LeastSquaresMinECS ?? 0d;
        confirmViewModel.LeastSquaresMaxECS = item.LeastSquaresMaxECS ?? 0d;

        var isShowDialog = WindowManagerService.ShowDialog(confirmViewModel);
        Guard.IsTrue(isShowDialog == true);

        Guard.IsTrue(item.CalibratingItems.Count >= 2);

        item.LeastSquaresMinECS = null;
        item.LeastSquaresMaxECS = null;
        item.LeastSquareFindPoints = [];
        foreach (var dtoItem in item.CalibratingItems)
        {
            dtoItem.FACompensations = [];
            dtoItem.FBCompensations = [];
            dtoItem.NSCCompensations = [];
            dtoItem.NSCZeroPoint = null;
        }

        item.LeastSquaresMinECS = confirmViewModel.LeastSquaresMinECS;
        item.LeastSquaresMaxECS = confirmViewModel.LeastSquaresMaxECS;

        var naList = new List<double>();
        var faDiffList = new List<double>();
        var nbList = new List<double>();
        var fbDiffList = new List<double>();
        foreach (var ecs in item.CalibratingItems[0]
                     .ECSes
                     .Where(t => item.LeastSquaresMinECS <= t && t <= item.LeastSquaresMaxECS)
                     .Distinct()
                     .OrderBy(t => t))
        {
            var indexOfList = item.CalibratingItems.Select(t => t.ECSes.ToArray().IndexOf(ecs)).ToList();
            if (indexOfList.Any(t => t == -1)) continue;

            var faAverage = item.CalibratingItems.Index().Select(t => t.Item.FAs[indexOfList[t.Index]]).Average();
            item.LeastSquareFindPoints = [.. item.LeastSquareFindPoints, new Point(ecs, faAverage)];

            foreach (var (index, dtoItem) in item.CalibratingItems.Index())
            {
                // faAverage = fa + ka * (na - offseta) => faAverage - fa = ka * na - ka * offseta
                naList.Add(dtoItem.NAs[indexOfList[index]]);
                faDiffList.Add(faAverage - dtoItem.FAs[indexOfList[index]]);

                // faAverage = fb + kb * (nb - offsetb) => faAverage - fb = kb * nb - kb * offsetb
                nbList.Add(dtoItem.NBs[indexOfList[index]]);
                fbDiffList.Add(faAverage - dtoItem.FBs[indexOfList[index]]);
            }
        }

        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
        {
            item.LeastSquaresMinECS,
            item.LeastSquaresMaxECS,
            PlotFA = new HtmlPlot2DLinesChart([(string.Empty, [.. naList.Index().Select(t => new Point(t.Item, faDiffList[t.Index]))])], "fa"),
            PlotFB = new HtmlPlot2DLinesChart([(string.Empty, [.. nbList.Index().Select(t => new Point(t.Item, fbDiffList[t.Index]))])], "fb")
        }), HtmlLogUniqueId.LoggingHtml());

        var (ka, interceptA, rSquaredA, _) = PolynomialCurve.Fit1(Vector<double>.Build.Dense([..naList]), Vector<double>.Build.Dense([..faDiffList]));
        var (kb, interceptB, rSquaredB, _) = PolynomialCurve.Fit1(Vector<double>.Build.Dense([..nbList]), Vector<double>.Build.Dense([..fbDiffList]));

        var offsetA = -interceptA / ka;
        var offsetB = -interceptB / kb;

        Guard.IsFalse(double.IsNaN(ka));
        Guard.IsFalse(double.IsNaN(offsetA));
        Guard.IsFalse(double.IsNaN(kb));
        Guard.IsFalse(double.IsNaN(offsetB));

        item.KA = ka;
        item.OffsetA = offsetA;
        item.FARSquared = rSquaredA;
        item.KB = kb;
        item.OffsetB = offsetB;
        item.FBRSquared = rSquaredB;

        // 计算补偿曲线及 NSC 零点
        foreach (var dtoItem in item.CalibratingItems)
        {
            Logger.LogHtmlInformation($"{dtoItem.DSWFindBrightMachinePosition}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

            var faCompensations = new double[dtoItem.ECSes.Count];
            var fbCompensations = new double[dtoItem.ECSes.Count];
            var nscCompensations = new double[dtoItem.ECSes.Count];
            for (var i = 0; i < dtoItem.ECSes.Count; i++)
            {
                var fa = dtoItem.FAs[i];
                var na = dtoItem.NAs[i];
                var fb = dtoItem.FBs[i];
                var nb = dtoItem.NBs[i];

                // faAverage = fa + ka * (na - offseta)
                // fbAverage = fb + kb * (nb - offsetb)
                var faCompensation = fa + ka * (na - offsetA);
                var fbCompensation = fb + kb * (nb - offsetB);

                faCompensations[i] = faCompensation;
                fbCompensations[i] = fbCompensation;

                Guard.IsNotEqualTo(na, 0);
                Guard.IsNotEqualTo(nb, 0);

                nscCompensations[i] = (faCompensation / na - fbCompensation / nb) * 10000d;
            }

            dtoItem.FACompensations = faCompensations;
            dtoItem.FBCompensations = fbCompensations;
            dtoItem.NSCCompensations = nscCompensations;
            var (isSuccess, zeroPoint) = GetNSCCurveZeroPoint(dtoItem.AverageECS, dtoItem.ECSes, nscCompensations);
            dtoItem.NSCZeroPoint = zeroPoint;

            var tempHtmlBullet = new HtmlBullet(new
            {
                dtoItem.DSWFindBrightMachinePosition,
                dtoItem.AverageECS,
                PlotDataSource = new HtmlContainer([.. item.CalibratingPlotDataSource.GetAllHtmlPlot2DLinesCharts()])
            });

            if (isSuccess)
                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, tempHtmlBullet, HtmlLogUniqueId.LoggingHtml());
            else
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, tempHtmlBullet, HtmlLogUniqueId.LoggingHtml());

                ThrowHelper.ThrowArgumentException("NSC zero point not found. Please check whether the AF motor, ECS, slope, and other related configurations are correctly set.");
            }
        }

        var ecses = item.CalibratingItems.Select(t => Guard.IsNotNullAndReturn(t.NSCZeroPoint).X).ToArray();
        var error = Math.Abs(ecses.Max() - ecses.Min());
        var isOk = error < Cache.ThresholdECS;
        item.IsCalibrated = isOk;

        var htmlBullet = new HtmlBullet(new
        {
            item.KA,
            item.OffsetA,
            RARSquared = item.FARSquared,
            item.KB,
            item.OffsetB,
            RBRSquared = item.FBRSquared,
            ecses,
            error,
            isOk,
            CalibratingPlotDataSource = new HtmlContainer([.. item.CalibratingPlotDataSource.GetAllHtmlPlot2DLinesCharts()])
        });

        if (isOk) Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
        else Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
    }

    private (bool IsSuccess, Point ZeroPoint) GetNSCCurveZeroPoint(double averageEcs, IReadOnlyList<double> ecses, IReadOnlyList<double> nscs)
    {
        var ecsVector = Vector<double>.Build.Dense([..ecses]);
        var nscVector = Vector<double>.Build.Dense([..nscs]);

        Vector<double> nscIntervalVector, ecsIntervalVector;
        if (DarkAutoFocus.IsNscUseMaxValue)
        {
            var nscMaxIndex = nscVector.MaximumIndex();
            var nscMinPositiveLeftIndex = nscVector.SubVectorRange(0, nscMaxIndex).MinimumIndex();
            var nscMinNegativeRightIndex = nscVector.SubVectorRange(nscMaxIndex, nscVector.Count - 1).MinimumIndex() + nscMaxIndex;
            if (DarkAutoFocus.IsNscUsePositiveSlope)
            {
                nscIntervalVector = nscVector.SubVectorRange(nscMinPositiveLeftIndex, nscMaxIndex);
                ecsIntervalVector = ecsVector.SubVectorRange(nscMinPositiveLeftIndex, nscMaxIndex);
            }
            else
            {
                nscIntervalVector = nscVector.SubVectorRange(nscMaxIndex, nscMinNegativeRightIndex);
                ecsIntervalVector = ecsVector.SubVectorRange(nscMaxIndex, nscMinNegativeRightIndex);
            }
        }
        else
        {
            var nscMinIndex = nscVector.MinimumIndex();
            var nscMaxNegativeLeftIndex = nscVector.SubVectorRange(0, nscMinIndex).MaximumIndex();
            var nscMaxPositiveRightIndex = nscVector.SubVectorRange(nscMinIndex, nscVector.Count - 1).MaximumIndex() + nscMinIndex;

            if (DarkAutoFocus.IsNscUsePositiveSlope)
            {
                nscIntervalVector = nscVector.SubVectorRange(nscMinIndex, nscMaxPositiveRightIndex);
                ecsIntervalVector = ecsVector.SubVectorRange(nscMinIndex, nscMaxPositiveRightIndex);
            }
            else
            {
                nscIntervalVector = nscVector.SubVectorRange(nscMaxNegativeLeftIndex, nscMinIndex);
                ecsIntervalVector = ecsVector.SubVectorRange(nscMaxNegativeLeftIndex, nscMinIndex);
            }
        }

        var leftIndex = nscIntervalVector.Index().Where(t => t.Item < 0).Maxima(t => t.Item).First().Index;
        var rightIndex = nscIntervalVector.Index().Where(t => t.Item >= 0).Minima(t => t.Item).First().Index;

        var result = new Point((ecsIntervalVector[leftIndex] + ecsIntervalVector[rightIndex]) / 2d, (nscIntervalVector[leftIndex] + nscIntervalVector[rightIndex]) / 2d);
        var isOk = averageEcs > ecsIntervalVector[0] && averageEcs < ecsIntervalVector[^1];

        return (isOk, result);
    }

    private bool Save(AutoFocusFAFBCompensationDTO dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        ApplicationCookieService.SetCalibration(dto, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase calibration, CancellationToken cancellationToken)
    {
        var temp = Guard.IsAssignableToTypeAndReturn<AutoFocusFAFBCompensationDTO>(calibration);
        var status = Entry.Status;

        Calibration = temp;

        status.TotalCalibrationCount = 1;
        status.CalibratedCount = Calibration.IsCalibrated ? 1 : 0;
        status.VerifiedCount = Calibration.IsVerified ? 1 : 0;
        status.Details = [];
    }
}

[IOCAppService(ServiceType = typeof(AutoFocusFAFBCompensationConfirmECSRangeWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class AutoFocusFAFBCompensationConfirmECSRangeWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial double LeastSquaresMinECS { get; set; }

    [ObservableProperty]
    public partial double LeastSquaresMaxECS { get; set; }

    [RelayCommand]
    private void Close()
    {
        CloseView(true);
    }
}