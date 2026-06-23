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
using Microsoft.Extensions.Hosting;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;

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
        }), HtmlLogUniqueId.LoggingHtml());

        CalibratingItem.KA = 0d;
        CalibratingItem.OffsetA = 0d;
        CalibratingItem.KB = 0d;
        CalibratingItem.OffsetB = 0d;
        CalibratingItem.CalibratingItems = [];
        CalibratingItem.VerifyItems = [];

        AfViewModel.ResetSensorNscCompensation();
        await Task.Delay(100, cancellationToken);

        await GetSCurvesAsync(true, cancellationToken);

        CalibratingItem.IsCalibrated = true;

        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            CalibratingItem.KA,
            CalibratingItem.OffsetA,
            CalibratingItem.KB,
            CalibratingItem.OffsetB,
            PlotDataSource = new HtmlContainer([.. CalibratingItem.CalibratingPlotDataSource.GetAllHtmlPlot2DLinesCharts()])
        }), HtmlLogUniqueId.LoggingHtml());

        Guard.IsTrue(Save(CalibratingItem, cancellationToken));

        return CalibratingItem.IsCalibrated;
    }).ConfigureAwait(false);

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

        await GetSCurvesAsync(false, cancellationToken);

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

    private async Task GetSCurvesAsync(bool isCalibrating, CancellationToken cancellationToken)
    {
        var item = isCalibrating ? CalibratingItem : Review;

        var brightFieldPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Items[0].DSWFindBrightMachinePosition);
        try
        {
            foreach (var cacheItem in Cache.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Logger.LogHtmlInformation($"{cacheItem.DSWFindBrightMachinePosition}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                StageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(cacheItem.DSWFindBrightMachinePosition), CalChipSiteModelEnum.DswModel);

                CIBViewModel.ToggleRTFCParam(ApplicationCookie.ProductivityInformations[0]);
                await Task.Delay(100, cancellationToken);

                var (ecsMin, ecsMax) = AfViewModel.GetEcsMoveRange();

                AfViewModel.ToggleDarkFieldEnable(true);
                await Task.Delay(100, cancellationToken);

                var averageEcs = HostEnvironment.IsDevelopment() == false
                    ? AfViewModel.GetSensorAverageEcsValue()
                    : 5600;

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
                    ECSes = [..traceBufferList.Select(t => t.Ecs)],
                    FAs = [..traceBufferList.Select(t => t.Fa)],
                    NAs = [..traceBufferList.Select(t => t.Na)],
                    FBs = [..traceBufferList.Select(t => t.Fb)],
                    NBs = [..traceBufferList.Select(t => t.Nb)],
                    NSCs = [..traceBufferList.Select(t => t.Nsc)]
                };

                var ecsVector = Vector<double>.Build.Dense([..dtoItem.ECSes]);
                var nscVector = Vector<double>.Build.Dense([..dtoItem.NSCs]);

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

                dtoItem.NSCZeroPoint = new Point((ecsIntervalVector[leftIndex] + ecsIntervalVector[rightIndex]) / 2d, (nscIntervalVector[leftIndex] + nscIntervalVector[rightIndex]) / 2d);

                if (isCalibrating)
                    item.CalibratingItems = [..item.CalibratingItems, dtoItem];
                else
                    item.VerifyItems = [..item.VerifyItems, dtoItem];

                var isOk = averageEcs > ecsIntervalVector[0] && averageEcs < ecsIntervalVector[^1];

                var htmlBullet = new HtmlBullet(new
                {
                    ecsMin,
                    ecsMax,
                    averageEcs,
                    startECS,
                    stopECS,
                    PlotDataSource = new HtmlContainer([.. (isCalibrating ? item.CalibratingPlotDataSource : item.VerifyPlotDataSource).GetAllHtmlPlot2DLinesCharts()])
                });

                if (isOk)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                    ThrowHelper.ThrowArgumentException("NSC zero point not found. Please check whether the AF motor, ECS, slope, and other related configurations are correctly set.");
                }
            }
        }
        finally
        {
            StageViewModel.SetBrightFieldAbsoluteStageXy(brightFieldPosition);
        }
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