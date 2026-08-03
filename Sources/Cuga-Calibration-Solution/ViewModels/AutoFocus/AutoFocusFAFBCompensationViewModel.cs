using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models;
using Core.Models.Models.AutoFocus.DarkAutoFocus;
using Core.Models.Models.AutoFocus.FAFBCompensation;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.CalChip;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections;

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
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.Items.ElementAtOrDefault(0)?.FindBFMachinePosition ?? MicroscopeCalChip.GetBFMachinePosition(Cache.CalChipSiteModelEnum)), Cache.CalChipSiteModelEnum);

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
    private void MarkFindBrightMachinePosition(AutoFocusFAFBCompensationCacheItem? items)
    {
        try
        {
            if (items is null) return;

            Guard.IsEqualTo(Cache.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            items.FindBFMachinePosition = StageViewModel.GetMachineStagePosition();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, nameof(MarkFindBrightMachinePosition));
            DialogWindowProvider.ShowDialog($"""
                                             {nameof(MarkFindBrightMachinePosition)} Failed!
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step0Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(() =>
    {
        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Cache.MicroscopeLensInformation,
            Cache.CalChipSiteModelEnum
        }), HtmlLogUniqueId.LoggingHtml());

        return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.MicroscopeLensInformation);
    });

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step1Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () =>
    {
        AlignmentUserControlViewModel.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum;
        AlignmentUserControlViewModel.IsDarkFieldAlignment = false;

        await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

        Cache.AlignmentResult = AlignmentUserControlViewModel.AlignmentResult;

        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            Cache.MicroscopeLensInformation,
            Cache.CalChipSiteModelEnum,
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
            Cache.CalChipSiteModelEnum,
            AlignmentResult = new HtmlQuote(Cache.AlignmentResult.ToHtmlAnonymous()),
            Cache.SpeedECSPerSecond,
            Points = new HtmlTable([.. Cache.Items.Select(t => new { t.FindBFMachinePosition })]),
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
            dtoItem.FAPerNACompensations = [];
            dtoItem.FBPerNBCompensations = [];
            dtoItem.NSCCompensations = [];
            dtoItem.NSCZeroPoint = null;
        }

        AfViewModel.ResetFAFBCompensation();
        await Task.Delay(100, cancellationToken);

        Logger.LogHtmlInformation("Get S Curve", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
        await GetNSCCurvesAsync(true, cancellationToken);

        Logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
        await AlgorithmAsync(CalibratingItem, cancellationToken);

        Guard.IsTrue(Save(CalibratingItem, cancellationToken));

        return CalibratingItem.IsCalibrated;
    }).ConfigureAwait(false);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task AlgorithmAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(async () =>
        {
            Logger.LogHtmlInformation("Algorithm Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ThresholdECS
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("Algorithm", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
            await AlgorithmAsync(CalibratingItem, cancellationToken);

            Guard.IsTrue(Save(CalibratingItem, cancellationToken));

            AfViewModel.SetFAFBCompensation(CalibratingItem.KA, CalibratingItem.OffsetA, CalibratingItem.KB, CalibratingItem.OffsetB);
            
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
            Cache.CalChipSiteModelEnum,
            AlignmentResult = new HtmlQuote(Cache.AlignmentResult.ToHtmlAnonymous()),
            Cache.SpeedECSPerSecond,
            Points = new HtmlTable([.. Cache.Items.Select(t => new { t.FindBFMachinePosition })]),
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

        var brightFieldPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Items[0].FindBFMachinePosition);
        try
        {
            foreach (var cacheItem in Cache.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Logger.LogHtmlInformation($"{cacheItem.FindBFMachinePosition}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                var tempBrightFieldPosition = StageViewModel.MachineToBrightFieldPosition(cacheItem.FindBFMachinePosition);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(tempBrightFieldPosition, Cache.CalChipSiteModelEnum);
                await Task.Delay(1000, cancellationToken);

                StageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(tempBrightFieldPosition, Cache.CalChipSiteModelEnum);

                CIBViewModel.ToggleRTFCParam(ApplicationCookie.ProductivityInformations[0]);
                await Task.Delay(100, cancellationToken);

                var (ecsMin, ecsMax) = AfViewModel.GetEcsMoveRange();

                double? averageEcs = null;
                if (isCalibrating == false)
                {
                    AfViewModel.ToggleDarkFieldEnable(true);
                    await Task.Delay(100, cancellationToken);

                    averageEcs = AfViewModel.GetSensorAverageEcsValue();
                }

                var startECS = ecsMin;
                var stopECS = ecsMax;

                AfViewModel.SetSensorEcsValue(startECS);
                await Task.Delay(100, cancellationToken);

                var traceBufferList = AfViewModel.GetSensorNscTraceBufferList(
                    startECS,
                    stopECS,
                    Cache.SpeedECSPerSecond,
                    TimeSpan.FromSeconds(Math.Abs(stopECS - startECS) / Cache.SpeedECSPerSecond + 2d));

                var dtoItem = new AutoFocusFAFBCompensationDTOItem
                {
                    FindBrightMachinePosition = cacheItem.FindBFMachinePosition,
                    AverageECS = averageEcs,
                    ECSes = [.. traceBufferList.Select(t => t.Ecs)],
                    FAs = [.. traceBufferList.Select(t => t.Fa)],
                    NAs = [.. traceBufferList.Select(t => t.Na)],
                    FBs = [.. traceBufferList.Select(t => t.Fb)],
                    NBs = [.. traceBufferList.Select(t => t.Nb)],
                    NSCs = [.. traceBufferList.Select(t => t.Fa / t.Na - t.Fb / t.Nb)]
                };

                bool isSuccess;
                if (isCalibrating)
                {
                    isSuccess = true;
                    item.CalibratingItems = [.. item.CalibratingItems, dtoItem];
                }
                else
                {
                    Guard.IsNotNull(averageEcs);

                    (isSuccess, dtoItem.NSCZeroPoint) = GetNSCCurveZeroPoint(averageEcs.Value, dtoItem.ECSes, dtoItem.NSCs);
                    item.VerifyItems = [.. item.VerifyItems, dtoItem];
                }

                var htmlBullet = new HtmlBullet(new
                {
                    dtoItem.FindBrightMachinePosition,
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
            StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(brightFieldPosition, Cache.CalChipSiteModelEnum);
        }
    }

    private Task AlgorithmAsync(AutoFocusFAFBCompensationDTO item, CancellationToken cancellationToken)
    {
        Guard.IsTrue(item.CalibratingItems.Count >= 2);

        var confirmViewModel = HostApplication.GetRequiredService<AutoFocusFAFBCompensationConfirmECSRangeWindowViewModel>();
        confirmViewModel.LeastSquaresMinECS = item.LeastSquaresMinECS ?? 0d;
        confirmViewModel.LeastSquaresMaxECS = item.LeastSquaresMaxECS ?? 0d;

        var isShowDialog = WindowManagerService.ShowDialog(confirmViewModel);
        Guard.IsTrue(isShowDialog == true);

        item.LeastSquaresMinECS = null;
        item.LeastSquaresMaxECS = null;
        item.LeastSquareFindPoints = [];
        foreach (var dtoItem in item.CalibratingItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            dtoItem.FAPerNACompensations = [];
            dtoItem.FBPerNBCompensations = [];
            dtoItem.NSCCompensations = [];
            dtoItem.NSCZeroPoint = null;
        }

        item.LeastSquaresMinECS = confirmViewModel.LeastSquaresMinECS;
        item.LeastSquaresMaxECS = confirmViewModel.LeastSquaresMaxECS;

        var xAList = new List<double>();
        var yAList = new List<double>();
        var xBList = new List<double>();
        var yBList = new List<double>();
        foreach (var ecs in item.CalibratingItems[0]
                     .ECSes
                     .Where(t => item.LeastSquaresMinECS <= t && t <= item.LeastSquaresMaxECS)
                     .Distinct()
                     .OrderBy(t => t))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var indexOfList = item.CalibratingItems.Select(t => t.ECSes.ToArray().IndexOf(ecs)).ToList();
            if (indexOfList.Any(t => t == -1)) continue;

            var target = item.CalibratingItems.Index().Select(t => t.Item.FAs[indexOfList[t.Index]] / t.Item.NAs[indexOfList[t.Index]]).Average();
            item.LeastSquareFindPoints = [.. item.LeastSquareFindPoints, new Point(ecs, target)];

            foreach (var (index, dtoItem) in item.CalibratingItems.Index())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // target = fa / na + ka * (1 - offsetA / na)
                // => target - fa / na = ka * 1 - ka * offsetA / na
                // => target - fa / na = - ka * offsetA / na + ka * 1

                xAList.Add(1d / dtoItem.NAs[indexOfList[index]]);
                yAList.Add(target - dtoItem.FAs[indexOfList[index]] / dtoItem.NAs[indexOfList[index]]);
                xBList.Add(1d / dtoItem.NBs[indexOfList[index]]);
                yBList.Add(target - dtoItem.FBs[indexOfList[index]] / dtoItem.NBs[indexOfList[index]]);
            }
        }

        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
        {
            item.LeastSquaresMinECS,
            item.LeastSquaresMaxECS,
            PlotFA = new HtmlPlot2DLinesChart([(string.Empty, [.. xAList.Index().Select(t => new Point(t.Item, yAList[t.Index]))])], "fa"),
            PlotFB = new HtmlPlot2DLinesChart([(string.Empty, [.. xBList.Index().Select(t => new Point(t.Item, yBList[t.Index]))])], "fb")
        }), HtmlLogUniqueId.LoggingHtml());

        Guard.IsTrue(xAList.Count >= 2);

        // A * X = B (最小二乘法)
        var (slopeA, interceptA, rSquaredA, _) = PolynomialCurve.Fit1(Vector<double>.Build.Dense([.. xAList]), Vector<double>.Build.Dense([.. yAList]));
        var (slopeB, interceptB, rSquaredB, _) = PolynomialCurve.Fit1(Vector<double>.Build.Dense([.. xBList]), Vector<double>.Build.Dense([.. yBList]));

        item.KA = interceptA;
        item.OffsetA = -slopeA / interceptA;
        item.FARSquared = rSquaredA;
        item.KB = interceptB;
        item.OffsetB = -slopeB / interceptB;
        item.FBRSquared = rSquaredB;

        Guard.IsFalse(double.IsNaN(item.KA));
        Guard.IsFalse(double.IsNaN(item.OffsetA));
        Guard.IsFalse(double.IsNaN(item.KB));
        Guard.IsFalse(double.IsNaN(item.OffsetB));

        // 计算补偿曲线及 NSC 零点
        foreach (var dtoItem in item.CalibratingItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Logger.LogHtmlInformation($"{dtoItem.FindBrightMachinePosition}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

            var faPerNACompensations = new double[dtoItem.ECSes.Count];
            var fbPerNBCompensations = new double[dtoItem.ECSes.Count];
            var nscCompensations = new double[dtoItem.ECSes.Count];
            for (var i = 0; i < dtoItem.ECSes.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fa = dtoItem.FAs[i];
                var na = dtoItem.NAs[i];
                var fb = dtoItem.FBs[i];
                var nb = dtoItem.NBs[i];

                // target = fa / na + ka * (1 - offsetA / na)
                var faCompensation = fa / na + item.KA * (1d - item.OffsetA / na);
                var fbCompensation = fb / nb + item.KB * (1d - item.OffsetB / nb);

                faPerNACompensations[i] = faCompensation;
                fbPerNBCompensations[i] = fbCompensation;

                Guard.IsNotEqualTo(na, 0);
                Guard.IsNotEqualTo(nb, 0);

                nscCompensations[i] = faCompensation - fbCompensation;
            }

            dtoItem.FAPerNACompensations = faPerNACompensations;
            dtoItem.FBPerNBCompensations = fbPerNBCompensations;
            dtoItem.NSCCompensations = nscCompensations;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                dtoItem.FindBrightMachinePosition,
                dtoItem.AverageECS,
                PlotDataSource = new HtmlContainer([.. item.CalibratingPlotDataSource.GetAllHtmlPlot2DLinesCharts()])
            }), HtmlLogUniqueId.LoggingHtml());
        }

        item.IsCalibrated = true;
        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            item.KA,
            item.OffsetA,
            item.FARSquared,
            item.KB,
            item.OffsetB,
            item.FBRSquared,
            CalibratingPlotDataSource = new HtmlContainer([.. item.CalibratingPlotDataSource.GetAllHtmlPlot2DLinesCharts()])
        }), HtmlLogUniqueId.LoggingHtml());

        return Task.CompletedTask;
    }

    private (bool IsSuccess, Point ZeroPoint) GetNSCCurveZeroPoint(double averageEcs, IReadOnlyList<double> ecses, IReadOnlyList<double> nscs)
    {
        var ecsVector = Vector<double>.Build.Dense([.. ecses]);
        var nscVector = Vector<double>.Build.Dense([.. nscs]);

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