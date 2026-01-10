using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.OpticalPowerMeter;
using HandyControl.Tools.Extension;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Text;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserOpticalPowerMeterViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserOpticalPowerMeterViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Find Machine Position" },
        new() { StepName = "Optical Power Meter" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private LaserOpticalPowerMeterDTO _calibratingItem = new();

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationStatus> _calibratingStatuses = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private IReadOnlyList<LaserOpticalPowerMeterDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<LaserOpticalPowerMeterDTO> _selectedReviewItems = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private LaserOpticalPowerMeterCache _cache = new();

    [ObservableProperty]
    private LaserOpticalPowerMeterDTO[] _calibrations = [];

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务重载

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (CalibrationStatusService.GetAdsCalibrationIsOKStatus() == false)
        {
            DialogWindowProvider.ShowDialog("The ADS precondition is Failure", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckGantryDto>(out _, out var errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckGlobalScaleErrorDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<LaserBeamStabilizerObjDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibratingStatuses.Count == 0)
            CalibratingStatuses = [.. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(t => new ProductivityInformationStatus { SelectedItem = t })];

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<LaserOpticalPowerMeterCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserOpticalPowerMeterDTO>();

        Calibrations =
        [
            .. Calibrations
                .Where(t => ApplicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation))
                .Select(t =>
                {
                    CalibratingStatuses
                        .Single(tt => tt.SelectedItem == t.ProductivityInformation)
                        .IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Reviews =
        [
            .. Calibrations
                .OrderBy(t => t.ProductivityInformation)
        ];

        return Reviews.Count > 0;
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
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.FindMachinePosition);

                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                CalibratingItem = new LaserOpticalPowerMeterDTO();
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.FindMachinePosition);

                return true;

            case 1:

                return true;

            case 2:
                CalibratingStatuses
                    .Single(t => t.SelectedItem == Cache.ProductivityInformation)
                    .IsCalibrated = true;

                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

                IsCalibrated = CalibratingStatuses.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务重载

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.OpticsMagTypeProductivityInformations.Contains(Cache.ProductivityInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Cache.Item.FindMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.FindMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step2Async(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(async () =>
        {
            Guard.IsTrue((Cache.Item.RowCount & 1) == 1, "It must be odd number!");
            Guard.IsTrue((Cache.Item.ColumnCount & 1) == 1, "It must be odd number!");

            var maxCoefficient = ApplicationCookie.LaserLightInformations.Max(t => t.Coefficient);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                maxCoefficient,
                Cache.ProductivityInformation,
                Cache.Item.FindMachinePosition,
                Cache.CalibratingRetryTimes,
                Cache.Item.WaitTime,
                Cache.Item.RowCount,
                Cache.Item.ColumnCount,
                Cache.Item.ColumnWidth,
                Cache.Item.RowHeight
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.ProductivityInformation = Cache.ProductivityInformation;
            CalibratingItem.MaxCoefficient = maxCoefficient;
            CalibratingItem.Items = [];
            CalibratingItem.MaxMeasurePower = 0d;
            CalibratingItem.MaxMeasurePowerPosition = Point.Origin;
            CalibratingItem.IsCalibrated = false;

            // 中心点的索引
            var centerX = (Cache.Item.ColumnCount - 1) / 2d;
            var centerY = (Cache.Item.RowCount - 1) / 2d;

            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.Item.FindMachinePosition);
            LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
            LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, maxCoefficient);
            LaserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);

            var times = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var extents = new Extents();

                for (var row = 0; row < Cache.Item.RowCount; row++)
                {
                    for (var column = 0; column < Cache.Item.ColumnCount; column++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var machinePosition = Cache.Item.FindMachinePosition + (Vector)new Point((column - centerX) * Cache.Item.ColumnWidth, (row - centerY) * Cache.Item.RowHeight);

                        extents.Add(machinePosition);

                        if (CalibratingItem.Items.Any(t => t.MeasurePosition == machinePosition)) continue;

                        CalibratingItem.Items = [.. CalibratingItem.Items, new LaserOpticalPowerMeterDTOItem { MeasurePosition = machinePosition, MeasurePower = double.NaN }];
                    }
                }

                foreach (var itemItem in CalibratingItem.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (itemItem.MeasurePower.IsNaN() == false) continue;

                    try
                    {
                        StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(itemItem.MeasurePosition);

                        LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

                        await Task.Delay(TimeSpan.FromSeconds(Cache.Item.WaitTime), cancellationToken).ConfigureAwait(false);

                        var measurePower = LaserViewModel.GetOpticalMeasurePower();

                        itemItem.MeasurePower = measurePower;
                    }
                    finally
                    {
                        LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
                    }
                }

                var maximumIndex = Vector<double>.Build.DenseOfEnumerable(CalibratingItem.Items.Select(t => t.MeasurePower)).MaximumIndex();
                CalibratingItem.MaxMeasurePower = CalibratingItem.Items[maximumIndex].MeasurePower;
                CalibratingItem.MaxMeasurePowerPosition = CalibratingItem.Items[maximumIndex].MeasurePosition;
                CalibratingItem.IsCalibrated = extents.OnEdge(CalibratingItem.MaxMeasurePowerPosition) == false;
                Cache.Item.FindMachinePosition = CalibratingItem.MaxMeasurePowerPosition;

                var htmlBullet = new HtmlBullet(new
                {
                    times,
                    CalibratingItem.MaxMeasurePower,
                    CalibratingItem.MaxMeasurePowerPosition,
                    CalibratingItem.IsCalibrated,
                    Plot = CalibratingItem.GetHtmlPlot3DChart(HtmlPlot3DType.Bar3D)
                });

                if (CalibratingItem.IsCalibrated)
                {
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                    break;
                }

                if (++times > Cache.CalibratingRetryTimes - 1)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                    break;
                }

                Logger.LogHtmlInformation("Plots", HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
            }

            Guard.IsTrue(Save([CalibratingItem], cancellationToken));

            return CalibratingItem.IsCalibrated;
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        if (SelectedReviewItems.Count == 0)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(async () =>
        {
            var errorMessageStringBuilder = new StringBuilder();

            foreach (var selectedReviewItem in SelectedReviewItems.OrderBy(t => t.ProductivityInformation))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var title = selectedReviewItem.ProductivityInformation.ToString();

                if (selectedReviewItem.IsCalibrated == false)
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error");
                    continue;
                }

                Cache.ProductivityInformation = selectedReviewItem.ProductivityInformation;

                Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    Cache.ProductivityInformation,
                    Cache.CalibratingRetryTimes,
                    Cache.Threshold,
                    Cache.Item.WaitTime
                }), HtmlLogUniqueId.LoggingHtml());

                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(selectedReviewItem.MaxMeasurePowerPosition);
                LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
                LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, selectedReviewItem.MaxCoefficient);
                LaserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);

                var verifyResultList = new List<double>();

                try
                {
                    LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);
                    foreach (var _ in Enumerable.Range(0, 3))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        await Task.Delay(TimeSpan.FromSeconds(Cache.Item.WaitTime), cancellationToken).ConfigureAwait(false);

                        var measurePower = LaserViewModel.GetOpticalMeasurePower();

                        verifyResultList.Add(measurePower);
                    }
                }
                finally
                {
                    LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
                }

                var verifyAverage = verifyResultList.Average();
                var verifyErrorRate = Math.Abs((verifyAverage - selectedReviewItem.MaxMeasurePower) / selectedReviewItem.MaxMeasurePower);
                selectedReviewItem.IsVerified = verifyErrorRate < Cache.Threshold;

                var htmlBullet = new HtmlBullet(new
                {
                    verifyResultList,
                    verifyAverage,
                    verifyErrorRate,
                    selectedReviewItem.MaxMeasurePower,
                    selectedReviewItem.MaxMeasurePowerPosition,
                    selectedReviewItem.IsVerified,
                    Plot = selectedReviewItem.GetHtmlPlot3DChart(HtmlPlot3DType.Bar3D)
                });

                if (selectedReviewItem.IsOk)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error");
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                }
            }

            Guard.IsTrue(Save(SelectedReviewItems, cancellationToken));

            var result = SelectedReviewItems.All(t => t.IsOk);

            DialogWindowProvider.ShowDialog($"""
                                             Verify : {(result ? "OK" : "Failed")}
                                             {errorMessageStringBuilder}
                                             """,
                DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }).ConfigureAwait(false);
    }

    private bool Save(IReadOnlyList<LaserOpticalPowerMeterDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                .. Calibrations.Where(t => t.ProductivityInformation != dto.ProductivityInformation),
                dto
            ];
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}