using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.Attenuator;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.OpticalPowerMeter;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserAttenuatorViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserAttenuatorViewModel(ApplicationCookie applicationCookie) : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Optics Mag" },
        new() { StepName = "Attenuator" }
    ];

    #region Calibration

    [ObservableProperty]
    private LaserAttenuatorDto? _calibratingItem;

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationCalibrationStatus> _calibrationStatuses = [];

    #endregion Calibration

    #region Review

    [ObservableProperty]
    private IReadOnlyList<LaserAttenuatorDto> _reviews = [];

    [ObservableProperty]
    private LaserAttenuatorDto? _selectedReviewItem;

    #endregion Review

    #region 缓存

    [ObservableProperty]
    private LaserAttenuatorCache _cache = new();

    [ObservableProperty]
    private LaserAttenuatorDto[] _calibrations = [];

    [ObservableProperty]
    private LaserOpticalPowerMeterDto[] _laserOpticalPowers = [];

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

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<LaserBeamStabilizerObjDto>(out _, out var errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserOpticalPowerMeterDto>(out var laserOpticalPowers, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        LaserOpticalPowers = laserOpticalPowers;

        if (CalibrationStatuses.Count == 0)
            CalibrationStatuses =
            [
                .. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(t => new ProductivityInformationCalibrationStatus { ProductivityInformation = t, IsCalibrated = false })
            ];

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<LaserAttenuatorCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserAttenuatorDto>();

        foreach (var calibrationStatus in Calibrations)
        {
            var status = CalibrationStatuses.SingleOrDefault(t => t.ProductivityInformation == calibrationStatus.ProductivityInformation);

            if (status is not null) status.IsCalibrated = calibrationStatus.IsCalibrated;
        }

        if (isHasCache == false) CacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        CalibratingItem = null;

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Reviews =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .OrderBy(t => t.ProductivityInformation)
        ];

        return Reviews.Any(t => t.IsCalibrated);
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                return true;

            case 1:
                CalibrationStatuses.Single(t => t.ProductivityInformation == Cache.ProductivityInformation).IsCalibrated = true;
                DialogWindowProvider.ShowDialog($"Attenuator {Cache.ProductivityInformation} Ok!");

                IsCalibrated = CalibrationStatuses.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务重载

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(async () =>
        {
            try
            {
                var startCoefficient = applicationCookie.LaserLightInformations.Min(t => t.Coefficient);
                var stopCoefficient = applicationCookie.LaserLightInformations.Max(t => t.Coefficient);
                var laserOpticalPower = LaserOpticalPowers.Single(t => t.ProductivityInformation == Cache.ProductivityInformation && t.IsOk);

                CalibratingItem = new LaserAttenuatorDto
                {
                    ProductivityInformation = Cache.ProductivityInformation,
                    WaitTime = Cache.Item.WaitTime,
                    OpticalPowerMeterCoefficient = laserOpticalPower.Coefficient,
                    OpticalPowerMeterMaxMeasurePower = laserOpticalPower.MeasureMaxPower,
                    OpticalPowerMeterMaxMeasurePowerPosition = laserOpticalPower.MeasureMaxPowerPosition
                };

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    startCoefficient,
                    stopCoefficient,
                    CalibratingItem.OpticalPowerMeterCoefficient,
                    CalibratingItem.OpticalPowerMeterMaxMeasurePower,
                    CalibratingItem.OpticalPowerMeterMaxMeasurePowerPosition,
                    Cache.Item.CoefficientStep,
                    CalibratingItem.ProductivityInformation,
                    CalibratingItem.WaitTime
                }), HtmlLogUniqueId.LoggingHtml());

                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(CalibratingItem.OpticalPowerMeterMaxMeasurePowerPosition);
                LaserViewModel.ToggleOpticsMagType(CalibratingItem.ProductivityInformation);
                LaserViewModel.SetPrescanAODWaveProfileByCoefficient(CalibratingItem.ProductivityInformation, stopCoefficient);
                LaserViewModel.SetChirpAODWaveProfile(CalibratingItem.ProductivityInformation);
                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

                await Task.Delay(TimeSpan.FromSeconds(CalibratingItem.WaitTime), cancellationToken).ConfigureAwait(false);
                var firstMeasurePowerPower = LaserViewModel.GetOpticalPowerMeter();

                await Task.Delay(TimeSpan.FromSeconds(CalibratingItem.WaitTime), cancellationToken).ConfigureAwait(false);
                var secondMeasurePowerPower = LaserViewModel.GetOpticalPowerMeter();

                CalibratingItem.MaxCoefficientAverageMeasurePower = (firstMeasurePowerPower + secondMeasurePowerPower) / 2; // 计算平均值
                CalibratingItem.CoefficientMeasurePowerPoints = [];
                CalibratingItem.CoefficientMeasurePowerRatePoints = [];
                CalibratingItem.CoefficientFitMeasurePowerRatePoints = [];
                CalibratingItem.P0 = 0;
                CalibratingItem.P1 = 0;
                CalibratingItem.P2 = 0;
                CalibratingItem.P3 = 0;
                CalibratingItem.RSquared = 0;

                var coefficients = GenerateUtils.LinearContainsEdgeRange(startCoefficient, Cache.Item.CoefficientStep, stopCoefficient);
                Guard.IsNotEmpty(coefficients);

                foreach (var coefficient in coefficients)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    LaserViewModel.SetPrescanAODWaveProfileByCoefficient(CalibratingItem.ProductivityInformation, coefficient);

                    await Task.Delay(TimeSpan.FromSeconds(Cache.Item.WaitTime), cancellationToken).ConfigureAwait(false);

                    var measurePower = LaserViewModel.GetOpticalPowerMeter();
                    var coefficientMeasurePowerPoint = new Point
                    (
                        coefficient,
                        measurePower
                    );
                    var coefficientMeasurePowerRatePoint = new Point
                    (
                        coefficient,
                        measurePower / CalibratingItem.MaxCoefficientAverageMeasurePower
                    );
                    CalibratingItem.CoefficientMeasurePowerPoints = [.. CalibratingItem.CoefficientMeasurePowerPoints, coefficientMeasurePowerPoint];
                    CalibratingItem.CoefficientMeasurePowerRatePoints = [.. CalibratingItem.CoefficientMeasurePowerRatePoints, coefficientMeasurePowerRatePoint];
                }

                var (p0, p1, p2, p3, rSquared, yPredicted) = PolynomialLeastSquares.Polynomial3Fit(Vector<double>.Build.DenseOfEnumerable(CalibratingItem.CoefficientMeasurePowerRatePoints.Select(t => t.X)), Vector<double>.Build.DenseOfEnumerable(CalibratingItem.CoefficientMeasurePowerRatePoints.Select(t => t.Y)));

                CalibratingItem.P0 = p0;
                CalibratingItem.P1 = p1;
                CalibratingItem.P2 = p2;
                CalibratingItem.P3 = p3;
                CalibratingItem.RSquared = rSquared;
                CalibratingItem.CoefficientFitMeasurePowerRatePoints = [.. CalibratingItem.CoefficientMeasurePowerRatePoints.Select((t, i) => new Point(t.X, yPredicted[i]))];

                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Close);

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    CalibratingItem.MaxCoefficientAverageMeasurePower,
                    CalibratingItem.P0,
                    CalibratingItem.P1,
                    CalibratingItem.P2,
                    CalibratingItem.P3,
                    CalibratingItem.RSquared,
                    MeasurePower = new HtmlPlot2DLinesChart([(nameof(CalibratingItem.CoefficientMeasurePowerPoints), CalibratingItem.CoefficientMeasurePowerPoints)], string.Empty),
                    MeasurePowerRate = new HtmlPlot2DLinesChart(
                    [
                        (nameof(CalibratingItem.CoefficientMeasurePowerRatePoints), CalibratingItem.CoefficientMeasurePowerRatePoints),
                        (nameof(CalibratingItem.CoefficientFitMeasurePowerRatePoints), CalibratingItem.CoefficientFitMeasurePowerRatePoints)
                    ], string.Empty)
                }), HtmlLogUniqueId.LoggingHtml());

                CalibratingItem.IsCalibrated = true;
                if (Save(CalibratingItem, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    CalibratingItem.IsCalibrated = false;

                    return false;
                }

                return true;
            }
            finally
            {
                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Close);
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        if (SelectedReviewItem is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(() =>
        {
            Cache.ProductivityInformation = SelectedReviewItem.ProductivityInformation;

            SelectedReviewItem.IsVerified = false;

            CacheProvider.Set(Cache, cancellationToken);

            SelectedReviewItem.IsVerified = true;

            if (Save(SelectedReviewItem, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                SelectedReviewItem.IsVerified = false;

                return false;
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.ProductivityInformation,
                SelectedReviewItem.WaitTime,
                SelectedReviewItem.OpticalPowerMeterCoefficient,
                SelectedReviewItem.OpticalPowerMeterMaxMeasurePower,
                SelectedReviewItem.OpticalPowerMeterMaxMeasurePowerPosition,
                SelectedReviewItem.MaxCoefficientAverageMeasurePower,
                SelectedReviewItem.P0,
                SelectedReviewItem.P1,
                SelectedReviewItem.P2,
                SelectedReviewItem.P3,
                SelectedReviewItem.RSquared,
                MeasurePower = new HtmlPlot2DLinesChart([(nameof(SelectedReviewItem.CoefficientMeasurePowerPoints), SelectedReviewItem.CoefficientMeasurePowerPoints)], string.Empty),
                MeasurePowerRate = new HtmlPlot2DLinesChart(
                [
                    (nameof(SelectedReviewItem.CoefficientMeasurePowerRatePoints), SelectedReviewItem.CoefficientMeasurePowerRatePoints),
                    (nameof(SelectedReviewItem.CoefficientFitMeasurePowerRatePoints), SelectedReviewItem.CoefficientFitMeasurePowerRatePoints)
                ], string.Empty)
            }), HtmlLogUniqueId.LoggingHtml());

            DialogWindowProvider.ShowDialog("Verify OK");

            return true;
        }).ConfigureAwait(false);
    }

    private bool Save(LaserAttenuatorDto item, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(item);
        update(Cache);

        Calibrations =
        [
            .. Calibrations.Where(t => t.ProductivityInformation != item.ProductivityInformation),
            item.Clone()
        ];

        CacheProvider.SetArray(Calibrations, cancellationToken);
        CacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}