using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.Attenuator;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.OpticalPower;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Hosting;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;
using static SkiaSharp.HarfBuzz.SKShaper;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserAttenuatorCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserAttenuatorCalibrationViewModel(ApplicationCookie applicationCookie) : CalibrationViewModelBase
{
    #region 属性

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select a Mag", DefaultIsNextEnable = true },
        new() { StepName = "Attenuator  Calibration" }
    ];

    #region Calibrate

    [ObservableProperty]
    private LaserAttenuatorObjDto _resultLaserAttenuatorObjDto = new();

    [ObservableProperty]
    private ObservableCollection<OpticsMagTypeEnumCalibrationStatus> _calibrationStatusList =
    [
        ..EnumHelper.Enums<OpticsMagTypeEnum>().Select(t => new OpticsMagTypeEnumCalibrationStatus { OpticsMagTypeEnum = t, IsCalibrated = false })
    ];

    [ObservableProperty]
    private ObservableCollection<LaserAttenuatorObjDto> _reviewList = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private LaserAttenuatorObjDto? _reviewDto;

    #endregion Review

    #region 缓存

    [ObservableProperty]
    private LaserAttenuatorCache _cache = new();

    [ObservableProperty]
    private LaserAttenuatorObjDto[] _calibrations = [];

    [ObservableProperty]
    private LaserOpticalPowerDto[] _laserOpticalPowers = [];

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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserOpticalPowerDto>(out var laserOpticalPowers, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        LaserOpticalPowers = laserOpticalPowers;

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<LaserAttenuatorCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserAttenuatorObjDto>();

        foreach (var calibrationStatus in Calibrations)
        {
            CalibrationStatusList
                .Single(t => t.OpticsMagTypeEnum == calibrationStatus.OpticsMagTypeEnum)
                .IsCalibrated = calibrationStatus.IsCalibrated;
        }

        if (isHasCache == false) CacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewList =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .OrderBy(t => t.OpticsMagTypeEnum)
        ];

        ReviewDto = ReviewList[0];

        return ReviewList.Any(t => t.IsCalibrated);
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                return true;

            case 1:
                var isCalibrated = CalibrationStepIndex == 1;

                ResultLaserAttenuatorObjDto.IsCalibrated = isCalibrated;
                if (Save(ResultLaserAttenuatorObjDto, cancellationToken) == false)
                {
                    ResultLaserAttenuatorObjDto.IsCalibrated = false;
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name}Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                CalibrationStatusList.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).IsCalibrated = true;
                DialogWindowProvider.ShowDialog($"Attenuator {Cache.OpticsMagTypeEnum} Ok!");

                IsCalibrated = CalibrationStatusList.All(s => s.IsCalibrated);
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
                Cache.OpticsMagTypeEnum
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
                ResultLaserAttenuatorObjDto.OpticsMagTypeEnum = Cache.OpticsMagTypeEnum;

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    InitialightIntensity = LaserOpticalPowers.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).MeasureMaxPower,
                    StagePosition = LaserOpticalPowers.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).MeasureMaxPowerPosition,
                    ResultLaserAttenuatorObjDto.OpticsMagTypeEnum
                }), HtmlLogUniqueId.LoggingHtml());

                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(LaserOpticalPowers.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).MeasureMaxPowerPosition);
                LaserViewModel.ToggleOpticsMagType(ResultLaserAttenuatorObjDto.OpticsMagTypeEnum);
                LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Through);
                LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.OpticsMagTypeEnum, applicationCookie.LaserLightInformationList.Max(t => t.Coefficient));

                await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);

                var firstLightIntensity = LaserViewModel.GetOpticalPowerMeter();

                await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);

                var secondLightIntensity = LaserViewModel.GetOpticalPowerMeter();

                ResultLaserAttenuatorObjDto.LaserPowerMeterAverageIntensity = (firstLightIntensity + secondLightIntensity) / 2; // 计算平均值。
                /* if (HostEnvironment.IsProduction())
                 {
                     if (ResultLaserAttenuatorObjDto.LaserPowerMeterAverageIntensity < ResultLaserAttenuatorObjDto.InitialightIntensity)
                     {
                         Logger.LogHtmlInformation($"{Name}: Attenuator calibration result failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                         {
                             ResultLaserAttenuatorObjDto.StagePosition,
                             ResultLaserAttenuatorObjDto.InitialightIntensity,
                             ResultLaserAttenuatorObjDto.LaserPowerMeterAverageIntensity
                         }), HtmlLogUniqueId.LoggingHtml());

                         return false;
                     }
                 }*/

                ResultLaserAttenuatorObjDto.CoefficientCurvePositions = [];
                ResultLaserAttenuatorObjDto.WaitTime = Cache.WaitTime;

                var coefficients = Generate.LinearRange(applicationCookie.LaserLightInformationList.Min(t => t.Coefficient), Cache.Interval, applicationCookie.LaserLightInformationList.Max(t => t.Coefficient));
                if (coefficients.Contains(applicationCookie.LaserLightInformationList.Min(t => t.Coefficient)) == false) coefficients = [applicationCookie.LaserLightInformationList.Min(t => t.Coefficient), .. coefficients];
                if (coefficients.Contains(applicationCookie.LaserLightInformationList.Max(t => t.Coefficient)) == false) coefficients = [.. coefficients, applicationCookie.LaserLightInformationList.Max(t => t.Coefficient)];

                foreach (var c in coefficients)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.OpticsMagTypeEnum, c);

                    await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);

                    var lightIntensityC = LaserViewModel.GetOpticalPowerMeter();

                    Logger.LogHtmlInformation($"coefficient : {c}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        coefficient = c,
                        lightIntensity = lightIntensityC
                    }), HtmlLogUniqueId.LoggingHtml());

                    var point = new Point
                    (
                        c,
                        lightIntensityC / ResultLaserAttenuatorObjDto.LaserPowerMeterAverageIntensity
                    );
                    ResultLaserAttenuatorObjDto.CoefficientCurvePositions = [.. ResultLaserAttenuatorObjDto.CoefficientCurvePositions, point];
                    ResultLaserAttenuatorObjDto.CoefficientFitCurvePositions = [];
                }

                var (p0, p1, p2, p3, rSquared, yPredicted) = PolynomialLeastSquares.Polynomial3Fit(Vector<double>.Build.DenseOfEnumerable(ResultLaserAttenuatorObjDto.CoefficientCurvePositions.Select(t => t.X)), Vector<double>.Build.DenseOfEnumerable(ResultLaserAttenuatorObjDto.CoefficientCurvePositions.Select(t => t.Y));

                ResultLaserAttenuatorObjDto.P0 = p0;
                ResultLaserAttenuatorObjDto.P1 = p1;
                ResultLaserAttenuatorObjDto.P2 = p2;
                ResultLaserAttenuatorObjDto.P3 = p3;
                ResultLaserAttenuatorObjDto.RSquared = rSquared;
                ResultLaserAttenuatorObjDto.CoefficientFitCurvePositions = [.. ResultLaserAttenuatorObjDto.CoefficientCurvePositions.Select((t, i) => new Point(t.X, yPredicted[i]))];

                LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Close);

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ResultLaserAttenuatorObjDto.OpticsMagTypeEnum,
                    InitialightIntensity = LaserOpticalPowers.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).MeasureMaxPower,
                    StagePosition = LaserOpticalPowers.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).MeasureMaxPowerPosition,
                    ResultLaserAttenuatorObjDto.LaserPowerMeterAverageIntensity,
                    CoefficientList = new HtmlPlot2DLinesChart(
                    [
                        (nameof(ResultLaserAttenuatorObjDto.CoefficientCurvePositions), ResultLaserAttenuatorObjDto.CoefficientCurvePositions.ToArray()),
                        (nameof(ResultLaserAttenuatorObjDto.CoefficientFitCurvePositions), ResultLaserAttenuatorObjDto.CoefficientFitCurvePositions.ToArray())
                    ], Cache.OpticsMagTypeEnum.ToString())
                }), HtmlLogUniqueId.LoggingHtml());

                return true;
            }
            finally
            {
                LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Close);
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        if (ReviewDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(() =>
        {
            ReviewDto.IsVerified = false;

            CacheProvider.Set(Cache, cancellationToken);

            ReviewDto.IsVerified = true;

            if (Save(ReviewDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                ReviewDto.IsVerified = false;
                return false;
            }

            Logger.LogHtmlInformation($"{ReviewDto.OpticsMagTypeEnum} OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                ReviewDto.OpticsMagTypeEnum,
                InitialightIntensity = LaserOpticalPowers.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).MeasureMaxPower,
                StagePosition = LaserOpticalPowers.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).MeasureMaxPowerPosition,
                CoefficientList = new HtmlPlot2DLinesChart([
                    (nameof(ReviewDto.CoefficientCurvePositions), ReviewDto.CoefficientCurvePositions.ToArray()),
                    (nameof(ReviewDto.CoefficientFitCurvePositions), ReviewDto.CoefficientFitCurvePositions.ToArray())
                ], ReviewDto.OpticsMagTypeEnum.ToString())
            }), HtmlLogUniqueId.LoggingHtml());

            DialogWindowProvider.ShowDialog("Verify Attenuator calibration OK");

            return true;
        }).ConfigureAwait(false);
    }

    private bool Save(LaserAttenuatorObjDto itemDto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibrations =
        [
            .. Calibrations
                .Where(t => t.OpticsMagTypeEnum != itemDto.OpticsMagTypeEnum),
            itemDto.Clone()
        ];

        CacheProvider.SetArray(Calibrations, cancellationToken);
        CacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}