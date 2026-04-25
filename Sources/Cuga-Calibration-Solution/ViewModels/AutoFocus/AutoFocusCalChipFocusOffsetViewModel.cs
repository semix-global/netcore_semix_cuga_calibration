using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.AutoFocus.CalChipFocusOffset;
using Core.Models.Models.AutoFocus.DarkAutoFocus;
using Core.Models.Models.Microscope.CalChip;
using Core.Utilities.SourceGenerators.Attributes;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Hosting;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Text;

namespace CugaCalibration.ViewModels.AutoFocus;

[IOCAppService(ServiceType = typeof(AutoFocusCalChipFocusOffsetViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AutoFocusCalChipFocusOffsetViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Chuck Param" },
        new() { StepName = "Chuck AF ECS" },
        new() { StepName = "DSW Param" },
        new() { StepName = "DSW AF ECS" },
        new() { StepName = "Undefine Param" },
        new() { StepName = "Undefine AF ECS" },
        new() { StepName = "Haze Param" },
        new() { StepName = "Haze AF ECS" },
        new() { StepName = "Shiny Wafer Param" },
        new() { StepName = "Shiny Wafer AF ECS" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private AutoFocusCalChipFocusOffsetDTO _calibratingItem = new();

    #endregion Calibrate

    [ObservableProperty]
    private AutoFocusCalChipFocusOffsetDTO _review = new();

    [ObservableProperty]
    private AutoFocusCalChipFocusOffsetDTO? _selectReview;

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private AutoFocusCalChipFocusOffsetCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private AutoFocusCalChipFocusOffsetDTO _calibration = new();

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    [ObservableProperty]
    private DarkAutoFocusDTO _darkAutoFocus = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDTO>();
        DarkAutoFocus = CalibrationStatusService.GetCalibration<DarkAutoFocusDTO>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<AutoFocusCalChipFocusOffsetCache>();
        Calibration = CacheProvider.GetOrDefault<AutoFocusCalChipFocusOffsetDTO>();

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;

        StageViewModel.SetMachineAbsoluteStageXy(Cache.Item.FindBrightMachinePosition);

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
                CalibratingItem = new AutoFocusCalChipFocusOffsetDTO
                {
                    ProductivityInformation = Cache.ProductivityInformation
                };
                return true;

            case 1:
                CalibratingItem.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBrightMachinePosition == Point.Origin
                    ? MicroscopeCalChip.DswItem.BrightFieldMachinePosition
                    : Cache.Item.FindBrightMachinePosition));

                return true;

            case 2:
                return true;

            case 3:
                CalibratingItem.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.UndefinedModel;
                StageViewModel.SetCalChipUndefinedBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBrightMachinePosition == Point.Origin
                    ? MicroscopeCalChip.UndefineWaferItem.BrightFieldMachinePosition
                    : Cache.Item.FindBrightMachinePosition));

                return true;

            case 4:
                return true;

            case 5:
                CalibratingItem.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.HazeModel;
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBrightMachinePosition == Point.Origin
                    ? MicroscopeCalChip.HazeItem.BrightFieldMachinePosition
                    : Cache.Item.FindBrightMachinePosition));

                return true;

            case 6:
                return true;

            case 7:
                CalibratingItem.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.ShinyWaferModel;
                StageViewModel.SetCalChipShinyWaferBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBrightMachinePosition == Point.Origin
                    ? MicroscopeCalChip.ShinyWaferItem.BrightFieldMachinePosition
                    : Cache.Item.FindBrightMachinePosition));

                return true;

            case 8:
                return true;

            case 9:
                IsCalibrated = true;

                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                return true;

            case 1:
                return true;

            case 2:
                CalibratingItem.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
                StageViewModel.SetMachineAbsoluteStageXy(Cache.Item.FindBrightMachinePosition);
                return true;

            case 3:
                return true;

            case 4:
                CalibratingItem.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBrightMachinePosition));
                return true;

            case 5:
                return true;

            case 6:
                CalibratingItem.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.UndefinedModel;
                StageViewModel.SetCalChipUndefinedBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBrightMachinePosition));
                return true;

            case 7:
                return true;

            case 8:
                CalibratingItem.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.HazeModel;
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBrightMachinePosition));
                return true;

            case 9:
                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Cache.Item.FindBrightMachinePosition = StageViewModel.GetMachineStagePosition();
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.CalChipSiteModelEnum,
                Cache.Item.FindBrightMachinePosition,
                Cache.MicroscopeLensInformation,
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.MicroscopeLensInformation)
                   && ApplicationCookie.ProductivityInformations.Contains(Cache.ProductivityInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(async () =>
        {
            Guard.IsNotNull(CalibratingItem);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation,
                Cache.Item.FindBrightMachinePosition,
                Cache.HalfEcsLength,
                Cache.SpeedEcsPerSecond
            }), HtmlLogUniqueId.LoggingHtml());

            CIBViewModel.ToggleRTFCParam(Cache.ProductivityInformation);
            await Task.Delay(100, cancellationToken);

            if (Cache.CalChipSiteModelEnum is CalChipSiteModelEnum.ChuckModel)
            {
                List<(CalChipSiteModelEnum calchip, double afMotor)> calChipAFMotors = [];
                foreach (var calChipSiteModelEnum in EnumHelper.Enums<CalChipSiteModelEnum>())
                {
                    MicroscopeCalChip.CalChipSiteModelEnum = calChipSiteModelEnum;
                    StageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(
                        StageViewModel.MachineToBrightFieldPosition(
                            calChipSiteModelEnum is CalChipSiteModelEnum.ChuckModel
                                ? Cache.Item.FindBrightMachinePosition
                                : MicroscopeCalChip.CurrentItem.BrightFieldMachinePosition),
                        calChipSiteModelEnum);

                    AfViewModel.ToggleDarkFieldEnable(true);
                    await Task.Delay(100, cancellationToken);

                    var motorValue = AfViewModel.GetDarkFieldAutoFocusMotorAbsoluteValue();
                    calChipAFMotors.Add((calChipSiteModelEnum, motorValue));
                }

                if (calChipAFMotors.All(t => Math.Abs(t.afMotor - calChipAFMotors[0].afMotor) <= Constants.Tolerance) == false)
                {
                    Logger.LogHtmlError(
                        "CalChip all AF motor value must be same,please check CUGA diagnosis RTFC param setting!",
                        HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                        {
                            AFMotor = new HtmlTable([.. calChipAFMotors.Select(t => new { t.calchip, t.afMotor })])
                        }), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }
            }

            #region S曲线

            StageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(
                StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBrightMachinePosition),
                Cache.CalChipSiteModelEnum);

            AfViewModel.ToggleDarkFieldEnable(true);
            await Task.Delay(100, cancellationToken);

            var averageEcs = HostEnvironment.IsDevelopment() == false
                ? AfViewModel.GetSensorAverageEcsValue()
                : 5600;

            var startEcs = averageEcs - Cache.HalfEcsLength;
            var endEcs = averageEcs + Cache.HalfEcsLength;

            AfViewModel.SetSensorEcsValue(startEcs);
            await Task.Delay(100, cancellationToken);

            var traceBufferList = AfViewModel.GetSensorNscTraceBufferList(startEcs, endEcs,
                Cache.SpeedEcsPerSecond,
                TimeSpan.FromSeconds(Math.Abs(endEcs - startEcs) / Cache.SpeedEcsPerSecond + 2));
            var ecs = traceBufferList.Select(t => t.Ecs).ToArray();
            var nsc = traceBufferList.Select(t => t.Nsc).ToArray();

            CalibratingItem.CurrentItem.Ecs = ecs;
            CalibratingItem.CurrentItem.Nsc = nsc;
            CalibratingItem.CurrentItem.MotorValue = AfViewModel.GetDarkFieldAutoFocusMotorAbsoluteValue();

            #endregion

            #region 找零点区间

            var ecsVector = Vector<double>.Build.DenseOfEnumerable(ecs);
            var nscVector = Vector<double>.Build.DenseOfEnumerable(nsc);

            Vector<double> nscIntervalVector, ecsIntervalVector;
            if (DarkAutoFocus.IsNscUseMaxValue)
            {
                var nscMaxIndex = nscVector.MaximumIndex();
                var nscMinPositiveLeftIndex = nscVector.SubVectorRange(0, nscMaxIndex).MinimumIndex();
                var nscMinNegativeRightIndex =
                    nscVector.SubVectorRange(nscMaxIndex, nscVector.Count - 1).MinimumIndex() + nscMaxIndex;
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
                var nscMaxPositiveRightIndex =
                    nscVector.SubVectorRange(nscMinIndex, nscVector.Count - 1).MaximumIndex() + nscMinIndex;

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

            #endregion

            #region 找AF焦点

            if (averageEcs > ecsIntervalVector[0] && averageEcs < ecsIntervalVector[^1])
            {
                CalibratingItem.CurrentItem.EcsNscPoints =
                    [.. CalibratingItem.CurrentItem.Ecs.Select((t, i) => new Point(t, CalibratingItem.CurrentItem.Nsc[i]))];

                CalibratingItem.CurrentItem.EcsNscMaxMins =
                [
                    new Point(ecsIntervalVector[0], nscIntervalVector[0]),
                    new Point(ecsIntervalVector[^1], nscIntervalVector[^1])
                ];

                var originEndPointIndex1 = ecsIntervalVector[nscIntervalVector.Index().Where(t => t.Item >= 0).Minima(t => t.Item).First().Index];
                var originEndPointIndex2 = ecsIntervalVector[nscIntervalVector.Index().Where(t => t.Item < 0).Maxima(t => t.Item).First().Index];
                CalibratingItem.CurrentItem.ECSValue = (originEndPointIndex1 + originEndPointIndex2) / 2;

                #endregion

                Logger.LogHtmlInformation("Result", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    averageEcs,
                    startEcs,
                    endEcs,
                    Result = new HtmlQuote(CalibratingItem.CurrentItem.ToFlatnessHtmlAnonymous())
                }), HtmlLogUniqueId.LoggingHtml());

                if (CalibrationStepIndex != CalibrationStepList.Count - 1)
                    return true;

                CalibratingItem.IsCalibrated = true;
                Guard.IsTrue(Save(CalibratingItem, cancellationToken));

                Logger.LogHtmlInformation($"Calibration {(CalibratingItem.IsCalibrated ? "Success" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Result = new HtmlTable([.. CalibratingItem.Results.Select(t => new { CalChipSiteMode = t.Key, AFMotor = t.Value.MotorValue, ECS = t.Value.ECSValue })])
                }), HtmlLogUniqueId.LoggingHtml());

                return CalibratingItem.IsCalibrated;
            }

            Logger.LogHtmlError(
                "NSC zero point not found. Please check whether the AF motor, ECS, slope, and other related configurations are correctly set.",
                HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                {
                    startEcs,
                    endEcs,
                    averageEcs,
                    Plot = CalibratingItem.CurrentItem.ScatterPlotControl.GetHtmlPlot2DLinesChart(0)
                }), HtmlLogUniqueId.LoggingHtml());
            return false;
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(async () =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                Cache.CalChipSiteModelEnum,
                Cache.MicroscopeLensInformation,
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());
            var errorMessageStringBuilder = new StringBuilder();
            try
            {
                Review.IsVerified = false;
                SelectReview = Review.Clone();

                var result = true;
                foreach (var calChipSiteModelEnum in EnumHelper.Enums<CalChipSiteModelEnum>())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var title = calChipSiteModelEnum.ToDescriptionOrString();

                    Cache.CalChipSiteModelEnum = calChipSiteModelEnum;

                    Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                    {
                        Cache.Item.FindBrightMachinePosition
                    }), HtmlLogUniqueId.LoggingHtml());

                    if (SelectReview.Results.TryGet(calChipSiteModelEnum, out var resultDTO) == false)
                    {
                        Logger.LogHtmlInformation("Current calchip calibration result cache is no exit!", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                        return false;
                    }

                    StageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBrightMachinePosition), calChipSiteModelEnum);
                    AfViewModel.SetDarkField(calChipSiteModelEnum, resultDTO.ECSValue, resultDTO.MotorValue);
                    AfViewModel.ToggleDarkFieldEnable(true);

                    await Task.Delay(5000, cancellationToken);

                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlBullet(new { CalibrationResult = new HtmlQuote(resultDTO.ToFlatnessHtmlAnonymous()) }), HtmlLogUniqueId.LoggingHtml());
                    result = true;
                }

                Review.IsVerified = result;
                Guard.IsTrue(Save(Review, cancellationToken));

                DialogWindowProvider.ShowDialog($"""
                                                 Verify : {(result ? "OK" : "Failed")}
                                                 {errorMessageStringBuilder}
                                                 """,
                    DialogButtonsEnum.OK,
                    result ? DialogIconEnum.Information : DialogIconEnum.Warning);

                return result;
            }
            finally
            {
                CIBViewModel.ToggleRTFCParam(Cache.ProductivityInformation);
            }
        }).ConfigureAwait(false);
    }

    private bool Save(AutoFocusCalChipFocusOffsetDTO dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);
        update(dto);

        Calibration = dto.Clone();

        CacheProvider.Set(Calibration, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}