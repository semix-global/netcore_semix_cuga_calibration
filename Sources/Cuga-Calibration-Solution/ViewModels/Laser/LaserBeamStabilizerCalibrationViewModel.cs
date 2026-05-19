using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Utilities.SourceGenerators.Attributes;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserBeamStabilizerCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserBeamStabilizerCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select time interval", DefaultIsNextEnable = true },
        new() { StepName = "Beam Stabilizer calibration" }
    ];

    #region Review

    [ObservableProperty]
    private LaserBeamStabilizerObjDto? _reviewDto;

    #endregion Review

    [ObservableProperty]
    private LaserBeamStabilizerObjDto _firstLaserBeamStabilizerObjDto = new();

    [ObservableProperty]
    private ObservableCollection<LaserBeamStabilizerObjDto> _laserBeamStabilizerObjDtoList = [];

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private LaserBeamStabilizerCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private LaserBeamStabilizerObjDto _calibration = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务重载

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        Cache = ApplicationCookieService.GetCache<LaserBeamStabilizerCache>(cancellationToken);
        Calibration = ApplicationCookieService.GetCalibration<LaserBeamStabilizerObjDto>(cancellationToken);

        Cache.Threshold = Cache.Threshold == 0 ? 25 : Cache.Threshold;
        FirstLaserBeamStabilizerObjDto = new LaserBeamStabilizerObjDto { Interval = 30 };
        SynchronizationContextProvider.Send(LaserBeamStabilizerObjDtoList.Clear);

        UpdateEntryStatus(Calibration, cancellationToken);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewDto = Calibration.Clone();

        Cache.OriginPosition1 = ReviewDto.OriginPosition1;
        Cache.OriginPosition2 = ReviewDto.OriginPosition2;
        Cache.CurrentPDPosition1 = ReviewDto.CurrentPDPosition1;
        Cache.CurrentPDPosition2 = ReviewDto.CurrentPDPosition2;
        Cache.Interval = ReviewDto.Interval;

        return ReviewDto.IsCalibrated;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                var (originPoint1, originPoint2) = LaserViewModel.GetLaserBeamOriginPoint();
                Cache.OriginPosition1 = originPoint1;
                Cache.OriginPosition2 = originPoint2;
                return true;

            case 1:
                var isCalibrated = CalibrationStepIndex == 1;

                FirstLaserBeamStabilizerObjDto.IsCalibrated = isCalibrated;
                Cache.CurrentPDPosition1 = FirstLaserBeamStabilizerObjDto.CurrentPDPosition1;
                Cache.CurrentPDPosition2 = FirstLaserBeamStabilizerObjDto.CurrentPDPosition2;
                Cache.OriginPosition1 = FirstLaserBeamStabilizerObjDto.OriginPosition1;
                Cache.OriginPosition2 = FirstLaserBeamStabilizerObjDto.OriginPosition2;
                Cache.Interval = FirstLaserBeamStabilizerObjDto.Interval;
                if (Save(FirstLaserBeamStabilizerObjDto, cancellationToken) == false)
                {
                    FirstLaserBeamStabilizerObjDto.IsCalibrated = false;
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name}Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    DialogWindowProvider.ShowDialog("Save Failed!", DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    return false;
                }

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
                Interval = FirstLaserBeamStabilizerObjDto.Interval.ToString()
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(async () =>
        {
            SynchronizationContextProvider.Send(LaserBeamStabilizerObjDtoList.Clear);

            var isFirstCalibrateSuccess = ExecuteBeamStabilizerCalibrate(cancellationToken);
            if (!isFirstCalibrateSuccess)
            {
                // 打开反射镜自动校准
                LaserViewModel.AdjustBeamStabilizer(true);
                var isSecondCalibrateSuccess = false;
                for (var i = 0; i < Cache.RepeatNumber; i++)
                {
                    await Task.Delay(60 * 1000, cancellationToken).ConfigureAwait(false);
                    isSecondCalibrateSuccess = ExecuteBeamStabilizerCalibrate(cancellationToken);
                    if (isSecondCalibrateSuccess) break;
                }

                //关闭反射镜自动校准
                LaserViewModel.AdjustBeamStabilizer(false);
                if (!isSecondCalibrateSuccess)
                {
                    CalibrationSteps[CalibrationStepIndex].StepIsNextEnable = false;
                    Logger.LogHtmlError("Beam Stabilizer calibration result failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        FirstLaserBeamStabilizerObjDto.CurrentPDPosition1,
                        FirstLaserBeamStabilizerObjDto.CurrentPDPosition2,
                        FirstLaserBeamStabilizerObjDto.OriginPosition1,
                        FirstLaserBeamStabilizerObjDto.OriginPosition2,
                        Interval = FirstLaserBeamStabilizerObjDto.Interval.ToString()
                    }), HtmlLogUniqueId.LoggingHtml());
                    DialogWindowProvider.ShowDialog("Beam Stabilizer calibration failed.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }
            }

            Logger.LogHtmlInformation("Beam Stabilizer calibration result OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                FirstLaserBeamStabilizerObjDto.CurrentPDPosition1,
                FirstLaserBeamStabilizerObjDto.CurrentPDPosition2,
                FirstLaserBeamStabilizerObjDto.OriginPosition1,
                FirstLaserBeamStabilizerObjDto.OriginPosition2,
                Interval = FirstLaserBeamStabilizerObjDto.Interval.ToString()
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(() =>
        {
            if (ReviewDto is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Please select a review item!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            ReviewDto.IsVerified = false;

            ApplicationCookieService.SetCache(Cache, cancellationToken);

            SynchronizationContextProvider.Send(LaserBeamStabilizerObjDtoList.Clear);

            var isFirstCalibrateSuccess = ExecuteBeamStabilizerCalibrate(cancellationToken);

            if (!isFirstCalibrateSuccess)
            {
                //打开反射镜自动校准
                LaserViewModel.AdjustBeamStabilizer(true);
                var isSecondCalibrateSuccess = ExecuteBeamStabilizerCalibrate(cancellationToken);
                //关闭反射镜自动校准
                LaserViewModel.AdjustBeamStabilizer(false);

                if (!isSecondCalibrateSuccess)
                {
                    CalibrationSteps[CalibrationStepIndex].StepIsNextEnable = false;

                    Logger.LogHtmlError("Verify Beam Stabilizer calibration result failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        FirstLaserBeamStabilizerObjDto.CurrentPDPosition1,
                        FirstLaserBeamStabilizerObjDto.CurrentPDPosition2,
                        FirstLaserBeamStabilizerObjDto.OriginPosition1,
                        FirstLaserBeamStabilizerObjDto.OriginPosition2,
                        Interval = FirstLaserBeamStabilizerObjDto.Interval.ToString()
                    }), HtmlLogUniqueId.LoggingHtml());
                    DialogWindowProvider.ShowDialog("Verify Beam Stabilizer calibration failed.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }
            }

            ReviewDto.IsVerified = true;
            if (Save(ReviewDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name}Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                ReviewDto.IsVerified = false;
                return false;
            }

            Logger.LogHtmlInformation(" Verify Beam Stabilizer calibration result OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                FirstLaserBeamStabilizerObjDto.CurrentPDPosition1,
                FirstLaserBeamStabilizerObjDto.CurrentPDPosition2,
                FirstLaserBeamStabilizerObjDto.OriginPosition1,
                FirstLaserBeamStabilizerObjDto.OriginPosition2,
                Interval = FirstLaserBeamStabilizerObjDto.Interval.ToString()
            }), HtmlLogUniqueId.LoggingHtml());

            DialogWindowProvider.ShowDialog("Verify Beam Stabilizer calibration OK");
            return true;
        }).ConfigureAwait(false);
    }

    private bool ExecuteBeamStabilizerCalibrate(CancellationToken cancellationToken)
    {
        var checkSuccess = true;
        var (originPoint1, originPoint2) = LaserViewModel.GetLaserBeamOriginPoint();

        Cache.OriginPosition1 = originPoint1;
        Cache.OriginPosition2 = originPoint2;

        FirstLaserBeamStabilizerObjDto.OriginPosition1 = originPoint1;
        FirstLaserBeamStabilizerObjDto.OriginPosition2 = originPoint2;
        LaserBeamStabilizerObjDtoList = [];
        for (var i = 0; i < FirstLaserBeamStabilizerObjDto.Interval; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isCheckOffsetSuccess = CheckLaserBeamStabilizerOffsetPosition(originPoint1, originPoint2);
            var laserBeamStabilizerObjDto = new LaserBeamStabilizerObjDto
            {
                Index = i + 1,
                CurrentPDPosition1 = FirstLaserBeamStabilizerObjDto.CurrentPDPosition1,
                CurrentPDPosition2 = FirstLaserBeamStabilizerObjDto.CurrentPDPosition2
            };
            SynchronizationContextProvider.Send(() => LaserBeamStabilizerObjDtoList.Add(laserBeamStabilizerObjDto));

            Logger.LogHtmlInformation($"Check the laser beam stabilizer position offset. The {laserBeamStabilizerObjDto.Index} time.", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                FirstLaserBeamStabilizerObjDto.CurrentPDPosition1,
                FirstLaserBeamStabilizerObjDto.CurrentPDPosition2,
                FirstLaserBeamStabilizerObjDto.OriginPosition1,
                FirstLaserBeamStabilizerObjDto.OriginPosition2
            }), HtmlLogUniqueId.LoggingHtml());

            if (!isCheckOffsetSuccess)
            {
                checkSuccess = false;
                break;
            }
        }

        return checkSuccess;
    }

    private bool CheckLaserBeamStabilizerOffsetPosition(Point originPoint1, Point originPoint2)
    {
        var (currentPdPoint1, currentPdPoint2) = LaserViewModel.GetLaserBeamPoint();

        FirstLaserBeamStabilizerObjDto.CurrentPDPosition1 = currentPdPoint1;
        FirstLaserBeamStabilizerObjDto.CurrentPDPosition2 = currentPdPoint2;
        Cache.CurrentPDPosition1 = currentPdPoint1;
        Cache.CurrentPDPosition2 = currentPdPoint2;
        var offsetPdPoint1 = currentPdPoint1 - originPoint1;
        var offsetPdPoint2 = currentPdPoint2 - originPoint2;

        Thread.Sleep(1000);
        if (Math.Abs(offsetPdPoint1.X) > Cache.Threshold || Math.Abs(offsetPdPoint1.Y) > Cache.Threshold || Math.Abs(offsetPdPoint2.X) > Cache.Threshold || Math.Abs(offsetPdPoint2.Y) > Cache.Threshold)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment($"{Name} Check laser beam stabilizer offset position failed"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        return true;
    }

    private bool Save(LaserBeamStabilizerObjDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        ApplicationCookieService.SetCalibration(dto, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase calibration, CancellationToken cancellationToken)
    {
        var temp = Guard.IsAssignableToTypeAndReturn<LaserBeamStabilizerObjDto>(calibration);
        var status = Entry.Status;

        Calibration = temp;

        status.TotalCalibrationCount = 1;
        status.CalibratedCount = Calibration.IsCalibrated ? 1 : 0;
        status.VerifiedCount = Calibration.IsVerified ? 1 : 0;
        status.Details = [];
    }

    #endregion 校准
}