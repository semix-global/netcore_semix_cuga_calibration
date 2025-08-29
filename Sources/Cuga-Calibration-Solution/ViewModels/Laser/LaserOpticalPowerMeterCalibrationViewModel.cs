using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.OpticalPower;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserOpticalPowerMeterCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserOpticalPowerMeterCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select a Mag", DefaultIsNextEnable = true },
        new() { StepName = "Move Laser Power Meter", DefaultIsNextEnable = true },
        new() { StepName = "Attenuator Calibration" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<LaserOpticalPowerDto> _laserOpticalPowerDtoList = [];

    [ObservableProperty]
    private LaserOpticalPowerDto? _selectLaserOpticalPowerDto;

    [ObservableProperty]
    private LaserOpticalPowerDto? _resultLaserOpticalPowerDto;

    [ObservableProperty]
    private ObservableCollection<OpticsMagTypeEnumCalibrationStatus> _calibrationStatusList =
    [
        ..EnumHelper.Enums<OpticsMagTypeEnum>().Select(t => new OpticsMagTypeEnumCalibrationStatus { OpticsMagTypeEnum = t, IsCalibrated = false })
    ];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private LaserOpticalPowerDto? _reviewDto;

    [ObservableProperty]
    private ObservableCollection<LaserOpticalPowerDto> _reviewList = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private LaserOpticalPowerCache _cache = new();

    [ObservableProperty]
    private LaserOpticalPowerDto[] _calibrations = [];

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

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<LaserOpticalPowerCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserOpticalPowerDto>();

        foreach (var calibrationStatus in Calibrations)
        {
            CalibrationStatusList
                .Single(t => t.OpticsMagTypeEnum == calibrationStatus.OpticsMagTypeEnum)
                .IsCalibrated = calibrationStatus.IsCalibrated;
        }

        return isHasCache || CacheProvider.Set(Cache, cancellationToken);
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

        ClearCalibrationTemp();

        switch (CalibrationStepIndex)
        {
            case 0:
                return true;

            case 1:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.FindPosition);
                LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Through);
                return true;

            case 2:

                if (ResultLaserOpticalPowerDto is null)
                {
                    DialogWindowProvider.TryShowDialog("Please find optical power!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);

                    return dialogButtonsEnum != DialogResultEnum.Retry;
                }

                ResultLaserOpticalPowerDto.IsCalibrated = true;
                if (ToLaserOpticalPowerObj(ResultLaserOpticalPowerDto, cancellationToken) == false)
                {
                    ResultLaserOpticalPowerDto.IsCalibrated = false;
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name}Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                CalibrationStatusList.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).IsCalibrated = true;
                DialogWindowProvider.ShowDialog($"OpticalPower {Cache.OpticsMagTypeEnum} Ok!");

                IsCalibrated = CalibrationStatusList.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务重载

    #region 校准

    [RelayCommand]
    private async Task GetPointAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                var position = StageViewModel.GetMachineStagePosition();

                Cache.FindPosition = position;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Point Failed", Name);
        }
    }

    [RelayCommand]
    private async Task GotoPointAsync()
    {
        try
        {
            await Task.Run(() => StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.FindPosition)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
        }
    }

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
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsMagTypeEnum,
                Cache.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(async () =>
        {
            ClearCalibrationTemp();

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsMagTypeEnum,
                Cache.FindPosition,
                Cache.MeasureMinPower,
                Cache.MeasureMaxPower,
                Cache.RowNumber,
                Cache.ColumnNumber,
                Cache.ColumnCellWidth,
                Cache.RowCellHeight,
                Cache.WaitTime,
                Cache.RepeatCount,
                Cache.Threshold
            }), HtmlLogUniqueId.LoggingHtml());

            // 中心点的索引
            var centerX = (Cache.ColumnNumber - 1) / 2d;
            var centerY = (Cache.RowNumber - 1) / 2d;

            var laserOpticalPowerObjDto = new LaserOpticalPowerDto
            {
                OpticsMagTypeEnum = Cache.OpticsMagTypeEnum,
                FindCenterPosition = Cache.FindPosition,
                RowNumber = Cache.RowNumber,
                ColumnNumber = Cache.ColumnNumber,
                Map = []
            };

            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(laserOpticalPowerObjDto.FindCenterPosition);

            LaserViewModel.ToggleOpticsMagType(Cache.OpticsMagTypeEnum);

            LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Through);

            LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.OpticsMagTypeEnum, CalibrationSetting.SettingCommonParam.MainLaserLightInformation);

            var repeatCout = 0;

            while (repeatCout < Cache.RepeatCount)
            {
                laserOpticalPowerObjDto.Index = repeatCout++;

                var temp = laserOpticalPowerObjDto.Map;
                laserOpticalPowerObjDto.Map = [];

                for (var row = 0; row < Cache.RowNumber; row++)
                {
                    for (var column = 0; column < Cache.ColumnNumber; column++)
                    {
                        var position = laserOpticalPowerObjDto.FindCenterPosition + (Vector)new Point((column - centerX) * Cache.ColumnCellWidth, (row - centerY) * Cache.RowCellHeight);
                        laserOpticalPowerObjDto.Map.Add(new LaserOpticalPowerItemDto
                        {
                            MeasurePosition = position,
                            MeasurePower = temp.SingleOrDefault(t => t.MeasurePosition == position)?.MeasurePower ?? 0,
                            Row = row,
                            Column = column
                        });
                    }
                }

                SelectLaserOpticalPowerDto = laserOpticalPowerObjDto;

                foreach (var laserOpticalPowerObjItem in laserOpticalPowerObjDto.Map)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (laserOpticalPowerObjItem.MeasurePower > 0) continue;

                    StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(laserOpticalPowerObjItem.MeasurePosition);

                    await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);

                    var result = LaserViewModel.GetOpticalPowerMeter();

                    laserOpticalPowerObjItem.MeasurePower = result;

                    OnPropertyChanged(nameof(SelectLaserOpticalPowerDto));
                }

                var maximumIndex = Vector<double>.Build.DenseOfEnumerable(laserOpticalPowerObjDto.Map.Select(t => t.MeasurePower)).MaximumIndex();
                // maximumIndex转换为二维数组的索引
                var maximumIndexRow = maximumIndex / Cache.ColumnNumber;
                var maximumIndexCol = maximumIndex % Cache.ColumnNumber;
                laserOpticalPowerObjDto.MeasureMaxPower = laserOpticalPowerObjDto.Map[maximumIndex].MeasurePower;
                laserOpticalPowerObjDto.MeasureMaxPowerPosition = laserOpticalPowerObjDto.Map[maximumIndex].MeasurePosition;

                var resultLaserOpticalPowerDto = laserOpticalPowerObjDto.Clone();
                SynchronizationContextProvider.Send(() => { LaserOpticalPowerDtoList.Add(resultLaserOpticalPowerDto); });

                var isInEdge = maximumIndexRow == 0 || maximumIndexRow == Cache.RowNumber - 1 || maximumIndexCol == 0 || maximumIndexCol == Cache.ColumnNumber - 1;

                var htmlBulletList = new HtmlQuote(new
                {
                    laserOpticalPowerObjDto.FindCenterPosition,
                    laserOpticalPowerObjDto.MeasureMaxPower,
                    laserOpticalPowerObjDto.MeasureMaxPowerPosition,
                    MaxRow = maximumIndexRow,
                    MaxColumn = maximumIndexCol,
                    IsInEdge = isInEdge,
                    Map = new HtmlTable([
                        .. laserOpticalPowerObjDto.Map.Select(t => new
                        {
                            t.Row,
                            t.Column,
                            t.MeasurePower,
                            t.MeasurePosition
                        })
                    ])
                });

                // 判断maximumIndexRow,maximumIndexCol是不是再边缘点上
                if (isInEdge)
                {
                    Logger.LogHtmlInformation($"{Name} Warning Times{repeatCout}: The maximum value is on the edge point!", HtmlHeaderLevelEnum.Header4, htmlBulletList, HtmlLogUniqueId.LoggingHtml());

                    laserOpticalPowerObjDto.FindCenterPosition = laserOpticalPowerObjDto.MeasureMaxPowerPosition;
                    continue;
                }

                SelectLaserOpticalPowerDto = resultLaserOpticalPowerDto;
                ResultLaserOpticalPowerDto = resultLaserOpticalPowerDto;
                LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Close);

                Logger.LogHtmlInformation($"{Name} Ok Times{repeatCout}", HtmlHeaderLevelEnum.Header4, htmlBulletList, HtmlLogUniqueId.LoggingHtml());

                return true;
            }

            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name}Error: Calibration Failed: Retry count exceeded!"), HtmlLogUniqueId.LoggingHtml());
            LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Close);
            return false;
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

        await InvokeVerifyAsync(async () =>
        {
            ClearCalibrationTemp();

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                ReviewDto.OpticsMagTypeEnum,
                Cache.RowNumber,
                Cache.ColumnNumber,
                Cache.ColumnCellWidth,
                Cache.RowCellHeight,
                Cache.WaitTime,
                Cache.Threshold,
                ReviewDto.MeasureMaxPowerPosition,
                ReviewDto.MeasureMaxPower
            }), HtmlLogUniqueId.LoggingHtml());

            ReviewDto.IsVerified = false;

            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(ReviewDto.MeasureMaxPowerPosition);

            LaserViewModel.ToggleOpticsMagType(ReviewDto.OpticsMagTypeEnum);

            LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Through);

            LaserViewModel.SetPrescanAODWaveProfileByCoefficient(ReviewDto.OpticsMagTypeEnum, CalibrationSetting.SettingCommonParam.MainLaserLightInformation);

            var resultList = new List<double>();

            foreach (var _ in Enumerable.Range(0, 3))
            {
                await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);

                var lightIntensity = LaserViewModel.GetOpticalPowerMeter();

                resultList.Add(lightIntensity);
            }

            LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Close);

            var average = resultList.Average();
            var errorRate = Math.Abs((average - ReviewDto.MeasureMaxPower) / ReviewDto.MeasureMaxPower);
            var result = errorRate < Cache.Threshold;

            Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                ReviewDto.OpticsMagTypeEnum,
                Cache.RowNumber,
                Cache.ColumnNumber,
                Cache.ColumnCellWidth,
                Cache.RowCellHeight,
                Cache.WaitTime,
                Cache.Threshold,
                ReviewDto.MeasureMaxPowerPosition,
                ReviewDto.MeasureMaxPower
            }), HtmlLogUniqueId.LoggingHtml());

            ReviewDto.IsVerified = result;
            if (ToLaserOpticalPowerObj(ReviewDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name}Error: Save Failed! "), HtmlLogUniqueId.LoggingHtml());
                ReviewDto.IsVerified = false;
                return false;
            }

            DialogWindowProvider.ShowDialog(
                $"Verify {(result ? "OK" : "Failed")}, New Measure Max Power: ({average:f3}),Old Measure Max Power:({ReviewDto.MeasureMaxPower:f3}),Error:({errorRate:f3})",
                DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);
            return result;
        }).ConfigureAwait(false);
    }

    private bool ToLaserOpticalPowerObj(LaserOpticalPowerDto itemDto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibrations =
        [
            .. Calibrations
                .Where(t => t.OpticsMagTypeEnum != itemDto.OpticsMagTypeEnum),
            itemDto.Clone()
        ];

        return CacheProvider.SetArray(Calibrations, cancellationToken) && CacheProvider.Set(Cache, cancellationToken);
    });

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(LaserOpticalPowerDtoList.Clear);
    }

    #endregion 校准
}