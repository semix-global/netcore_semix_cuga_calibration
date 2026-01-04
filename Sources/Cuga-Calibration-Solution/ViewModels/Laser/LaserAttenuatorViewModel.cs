using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models;
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
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using System.Text;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserAttenuatorViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserAttenuatorViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Productivity" },
        new() { StepName = "Attenuator" }
    ];

    #region Calibration

    [ObservableProperty]
    private LaserAttenuatorDTO _calibratingItem = new();

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationCalibrationStatus> _calibrationStatuses = [];

    #endregion Calibration

    #region Review

    [ObservableProperty]
    private IReadOnlyList<LaserAttenuatorDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<LaserAttenuatorDTO> _selectedReviewItems = [];

    #endregion Review

    #region 缓存

    [ObservableProperty]
    private LaserAttenuatorCache _cache = new();

    [ObservableProperty]
    private LaserAttenuatorDTO[] _calibrations = [];

    [ObservableProperty]
    private LaserOpticalPowerMeterDTO[] _laserOpticalPowerMeters = [];

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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserOpticalPowerMeterDTO>(out var laserOpticalPowerMeters, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        LaserOpticalPowerMeters = laserOpticalPowerMeters;

        if (CalibrationStatuses.Count == 0)
            CalibrationStatuses = [.. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(t => new ProductivityInformationCalibrationStatus { SelectedItem = t })];

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<LaserAttenuatorCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserAttenuatorDTO>();

        Calibrations =
        [
            .. Calibrations
                .Where(t => ApplicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation))
                .Select(t =>
                {
                    CalibrationStatuses
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
                .Select(t => t.Clone())
                .OrderBy(t => t.ProductivityInformation)
        ];

        return Reviews.Count > 0;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                CalibratingItem = new LaserAttenuatorDTO();

                return true;

            case 1:
                CalibrationStatuses
                    .Single(t => t.SelectedItem == Cache.ProductivityInformation)
                    .IsCalibrated = true;

                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

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
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(async () =>
        {
            var laserOpticalPower = LaserOpticalPowerMeters.Single(t => t.ProductivityInformation == Cache.ProductivityInformation && t.IsOk);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.ProductivityInformation,
                Cache.Item.WaitTime,
                Cache.Item.StartCoefficient,
                Cache.Item.StepCoefficient,
                Cache.Item.StopCoefficient,
                laserOpticalPower.MaxCoefficient,
                laserOpticalPower.MaxMeasurePower,
                laserOpticalPower.MaxMeasurePowerPosition
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.ProductivityInformation = Cache.ProductivityInformation;
            CalibratingItem.WaitTime = Cache.Item.WaitTime;
            CalibratingItem.MeasurePowerPoints = [];
            CalibratingItem.MaxMeasurePower = 0d;
            CalibratingItem.AttenuatorPoints = [];
            CalibratingItem.P0 = 0d;
            CalibratingItem.P1 = 0d;
            CalibratingItem.P2 = 0d;
            CalibratingItem.P3 = 0d;
            CalibratingItem.RSquared = 0d;
            CalibratingItem.FitAttenuatorPoints = [];
            CalibratingItem.IsCalibrated = false;

            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(laserOpticalPower.MaxMeasurePowerPosition);
            LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
            LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, Cache.Item.StartCoefficient);
            LaserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);

            var coefficients = GenerateUtils.LinearContainsEdgeRange(Cache.Item.StartCoefficient, Cache.Item.StepCoefficient, Cache.Item.StopCoefficient);
            Guard.IsNotEmpty(coefficients);

            foreach (var coefficient in coefficients)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

                    cancellationToken.ThrowIfCancellationRequested();

                    LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, coefficient);

                    await Task.Delay(TimeSpan.FromSeconds(Cache.Item.WaitTime), cancellationToken).ConfigureAwait(false);

                    var measurePower = LaserViewModel.GetOpticalMeasurePower();
                    var coefficientMeasurePowerPoint = new Point(coefficient, measurePower);
                    CalibratingItem.MeasurePowerPoints = [.. CalibratingItem.MeasurePowerPoints, coefficientMeasurePowerPoint];
                }
                finally
                {
                    LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Close);
                }
            }

            CalibratingItem.MaxMeasurePower = CalibratingItem.MeasurePowerPoints.Max(t => t.Y);
            CalibratingItem.AttenuatorPoints = [.. CalibratingItem.MeasurePowerPoints.Select(t => new Point(t.X, t.Y / CalibratingItem.MaxMeasurePower))];

            var (p0, p1, p2, p3, rSquared, yPredicted) = PolynomialLeastSquares.Polynomial3Fit(
                Vector<double>.Build.DenseOfEnumerable(CalibratingItem.AttenuatorPoints.Select(t => t.X)),
                Vector<double>.Build.DenseOfEnumerable(CalibratingItem.AttenuatorPoints.Select(t => t.Y)));

            CalibratingItem.P0 = p0;
            CalibratingItem.P1 = p1;
            CalibratingItem.P2 = p2;
            CalibratingItem.P3 = p3;
            CalibratingItem.RSquared = rSquared;
            CalibratingItem.FitAttenuatorPoints = [.. CalibratingItem.AttenuatorPoints.Select((t, i) => new Point(t.X, yPredicted[i]))];
            CalibratingItem.IsCalibrated = CalibratingItem.RSquared >= Cache.Threshold;

            var htmlBullet = new HtmlBullet(new
            {
                CalibratingItem.MaxMeasurePower,
                CalibratingItem.P0,
                CalibratingItem.P1,
                CalibratingItem.P2,
                CalibratingItem.P3,
                CalibratingItem.RSquared,
                ScatterPlotControl = new HtmlContainer([.. CalibratingItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
            });

            if (CalibratingItem.IsCalibrated)
                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
            else
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

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

        await InvokeVerifyAsync(() =>
        {
            var errorMessageStringBuilder = new StringBuilder();

            foreach (var selectedReviewItem in SelectedReviewItems.OrderBy(t => t.ProductivityInformation))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var title = selectedReviewItem.ProductivityInformation.ToString();

                /*if (selectedReviewItem.IsCalibrated == false)
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error");
                    continue;
                }*/

                Cache.ProductivityInformation = selectedReviewItem.ProductivityInformation;

                Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    Cache.ProductivityInformation,
                    Cache.Threshold
                }), HtmlLogUniqueId.LoggingHtml());

                if (selectedReviewItem.IsCalibrated) selectedReviewItem.IsVerified = true;

                var htmlBullet = new HtmlBullet(new
                {
                    selectedReviewItem.MaxMeasurePower,
                    selectedReviewItem.P0,
                    selectedReviewItem.P1,
                    selectedReviewItem.P2,
                    selectedReviewItem.P3,
                    selectedReviewItem.RSquared,
                    ScatterPlotControl = new HtmlContainer([.. selectedReviewItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
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

    private bool Save(IReadOnlyList<LaserAttenuatorDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                .. Calibrations.Where(t => t.ProductivityInformation != dto.ProductivityInformation),
                dto.Clone()
            ];
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}