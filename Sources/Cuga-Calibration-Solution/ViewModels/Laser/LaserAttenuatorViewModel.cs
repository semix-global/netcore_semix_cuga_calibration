using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.Attenuator;
using Core.Models.Models.Laser.OpticalPowerMeter;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using System.Runtime.CompilerServices;
using System.Text;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserAttenuatorViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserAttenuatorViewModel : CalibrationViewModelBase<LaserAttenuatorCache>
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Attenuator" }
    ];

    #region Calibration

    [ObservableProperty]
    public partial LaserAttenuatorDTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<ProductivityInformationStatus> CalibratingStatuses { get; set; } = [];

    #endregion Calibration

    #region Review

    [ObservableProperty]
    public partial IReadOnlyList<LaserAttenuatorDTO> Reviews { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<LaserAttenuatorDTO> SelectedReviewItems { get; set; } = [];

    #endregion Review

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial LaserAttenuatorCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial LaserAttenuatorDTO[] Calibrations { get; set; } = [];

    [ObservableProperty]
    public partial LaserOpticalPowerMeterDTO[] LaserOpticalPowerMeters { get; set; } = [];

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务重载

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);


        LaserOpticalPowerMeters = ApplicationCookieService.GetCalibrations<LaserOpticalPowerMeterDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<LaserAttenuatorCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<LaserAttenuatorDTO>(cancellationToken);

        UpdateEntryStatus(Unsafe.As<CalibrationDTOBase[]>(Calibrations), cancellationToken);

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

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                return true;

            case 1:
                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

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
            CalibratingItem = new LaserAttenuatorDTO();

            var currentSaturationCoefficient = LaserViewModel.GetLaserLightSaturationCoefficient();

            try
            {
                LaserViewModel.SetLaserLightSaturationCoefficient(1d);

                await GetAttenuatorPointsAsync(CalibratingItem, cancellationToken).ConfigureAwait(false);

                var maxPowerValue = CalibratingItem.AttenuatorPoints.Max(t => t.Y);

                CalibratingItem.SaturationCoefficient = CalibratingItem.AttenuatorPoints.Where(t => Math.Abs(t.Y - maxPowerValue) < Constants.Tolerance)
                    .OrderBy(t => t.X)
                    .First()
                    .X;
                CalibratingItem.IsCalibrated = true;

                var htmlBullet = new HtmlBullet(new
                {
                    ConfigSaturationCoefficient = currentSaturationCoefficient,
                    CalibratingItem.SaturationCoefficient,
                    CalibratingItem.MaxMeasurePower,
                    ScatterPlotControl = new HtmlContainer([.. CalibratingItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                });

                if (CalibratingItem.IsCalibrated)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                Guard.IsTrue(Save([CalibratingItem], cancellationToken));

                return CalibratingItem.IsCalibrated;
            }
            finally
            {
                LaserViewModel.SetLaserLightSaturationCoefficient(currentSaturationCoefficient);
            }
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
                var currentSaturationCoefficient = LaserViewModel.GetLaserLightSaturationCoefficient();
                try
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
                        Cache.Threshold,
                        Cache.RateThreshold
                    }), HtmlLogUniqueId.LoggingHtml());

                    var htmlBullet = new HtmlBullet(new
                    {
                        selectedReviewItem.MaxMeasurePower,
                        selectedReviewItem.SaturationCoefficient,
                        ScatterPlotControl = new HtmlContainer([.. selectedReviewItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                    });

                    LaserViewModel.SetLaserLightSaturationCoefficient(selectedReviewItem.SaturationCoefficient);

                    await GetAttenuatorPointsAsync(selectedReviewItem, cancellationToken).ConfigureAwait(false);

                    var (p0, p1, p2, p3, rSquared, yPredicted) = PolynomialCurve.Fit3(
                        Vector<double>.Build.DenseOfEnumerable(selectedReviewItem.AttenuatorPoints.Select(t => t.X)),
                        Vector<double>.Build.DenseOfEnumerable(selectedReviewItem.AttenuatorPoints.Select(t => t.Y)));

                    selectedReviewItem.P0 = p0;
                    selectedReviewItem.P1 = p1;
                    selectedReviewItem.P2 = p2;
                    selectedReviewItem.P3 = p3;
                    selectedReviewItem.RSquared = rSquared;
                    selectedReviewItem.FitAttenuatorPoints = [.. selectedReviewItem.AttenuatorPoints.Select((t, i) => new Point(t.X, yPredicted[i]))];

                    var maxPowerValue = selectedReviewItem.AttenuatorPoints.Max(t => t.Y);
                    var verifySaturationCoefficient = selectedReviewItem.AttenuatorPoints.Where(t => Math.Abs(t.Y - maxPowerValue) < Constants.Tolerance)
                        .OrderBy(t => t.X)
                        .First()
                        .X;

                    selectedReviewItem.IsVerified = selectedReviewItem.RSquared >= Cache.Threshold
                                                    && (1 - verifySaturationCoefficient) / 1d <= Cache.RateThreshold;

                    if (selectedReviewItem.IsOk)
                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                    else
                    {
                        errorMessageStringBuilder.AppendLine($"{title}: Error");
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                    }
                }
                finally
                {
                    LaserViewModel.SetLaserLightSaturationCoefficient(currentSaturationCoefficient);
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


    private async Task GetAttenuatorPointsAsync(LaserAttenuatorDTO laserAttenuatorDTO, CancellationToken cancellationToken)
    {
        var laserOpticalPower = LaserOpticalPowerMeters.Single(t => t.ProductivityInformation.OpticsIlluminationModeEnum == Cache.ProductivityInformation.OpticsIlluminationModeEnum
                                                                    && t.ProductivityInformation.OpticsMagType == Cache.ProductivityInformation.OpticsMagType
                                                                    && t.IsOk);

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

        laserAttenuatorDTO.ProductivityInformation = Cache.ProductivityInformation;
        laserAttenuatorDTO.WaitTime = Cache.Item.WaitTime;
        laserAttenuatorDTO.MeasurePowerPoints = [];
        laserAttenuatorDTO.MaxMeasurePower = 0d;
        laserAttenuatorDTO.AttenuatorPoints = [];
        laserAttenuatorDTO.P0 = 0d;
        laserAttenuatorDTO.P1 = 0d;
        laserAttenuatorDTO.P2 = 0d;
        laserAttenuatorDTO.P3 = 0d;
        laserAttenuatorDTO.RSquared = 0d;
        laserAttenuatorDTO.FitAttenuatorPoints = [];

        StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(laserOpticalPower.MaxMeasurePowerPosition);
        LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
        OpticsViewModel.ToggleODFilter(false);
        LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, Cache.Item.StartCoefficient);
        LaserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);

        var coefficients = Generate.LinearRangeContainsEdge(Cache.Item.StartCoefficient, Cache.Item.StepCoefficient, Cache.Item.StopCoefficient);
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
                laserAttenuatorDTO.MeasurePowerPoints = [.. laserAttenuatorDTO.MeasurePowerPoints, coefficientMeasurePowerPoint];
            }
            finally
            {
                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Scan);
            }
        }

        laserAttenuatorDTO.MaxMeasurePower = laserAttenuatorDTO.MeasurePowerPoints.Max(t => t.Y);
        laserAttenuatorDTO.AttenuatorPoints = [.. laserAttenuatorDTO.MeasurePowerPoints.Select(t => new Point(t.X, t.Y / laserAttenuatorDTO.MaxMeasurePower))];
    }

    private bool Save(IReadOnlyList<LaserAttenuatorDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                dto,
                .. Calibrations.Where(t => t.ProductivityInformation != dto.ProductivityInformation)
            ];
        }

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<LaserAttenuatorDTO[]>(calibrations);
        var status = Entry.Status;

        CalibratingStatuses =
        [
            .. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(t => new ProductivityInformationStatus { SelectedItem = t, IsCalibrated = false })
        ];

        Calibrations =
        [
            .. temps
                .Where(t => ApplicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation))
                .DistinctBy(t => t.ProductivityInformation)
                .Select(t =>
                {
                    CalibratingStatuses.Single(tt => tt.SelectedItem == t.ProductivityInformation).IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        status.TotalCalibrationCount = ApplicationCookie.OpticsMagTypeProductivityInformations.Count;
        status.CalibratedCount = Calibrations.Count(t => t.IsCalibrated);
        status.VerifiedCount = Calibrations.Count(t => t.IsVerified);
        status.Details =
        [
            .. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(productivityInformation =>
            {
                var item = Calibrations.SingleOrDefault(t => t.ProductivityInformation == productivityInformation);

                return new CalibrationViewModelStatus.Detail(
                    productivityInformation.ToString(),
                    item?.IsCalibrated,
                    item?.IsVerified);
            })
        ];
    }

    #endregion 校准
}