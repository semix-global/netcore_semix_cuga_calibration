using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.BeamStabilizer;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;
using Core.Models.Models.Laser.OpticalPowerMeter;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserOpticalPowerMeterViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserOpticalPowerMeterViewModel(ApplicationCookie applicationCookie) : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Optics Mag" },
        new() { StepName = "Find Position" },
        new() { StepName = "Optical Power Meter" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private IReadOnlyList<LaserOpticalPowerMeterDto> _calibratings = [];

    [ObservableProperty]
    private LaserOpticalPowerMeterDto? _selectedCalibratingItem;

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationCalibrationStatus> _calibrationStatuses = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<LaserOpticalPowerMeterDto> _reviews = [];

    [ObservableProperty]
    private LaserOpticalPowerMeterDto? _selectedReviewItem;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private LaserOpticalPowerMeterCache _cache = new();

    [ObservableProperty]
    private LaserOpticalPowerMeterDto[] _calibrations = [];

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

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<LaserOpticalPowerMeterCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserOpticalPowerMeterDto>();
        
        if (CalibrationStatuses.Count == 0)
            CalibrationStatuses =
            [
                .. OpticsMagTypeProductivityInformations.Select(t => new ProductivityInformationCalibrationStatus { ProductivityInformation = t, IsCalibrated = false })
            ];

        foreach (var calibrationStatus in Calibrations)
        {
            CalibrationStatuses
                .Single(t => t.ProductivityInformation == calibrationStatus.ProductivityInformation)
                .IsCalibrated = calibrationStatus.IsCalibrated;
        }

        if (isHasCache == false) CacheProvider.Set(Cache, cancellationToken);

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
                return true;

            case 2:
                CalibrationStatuses.Single(t => t.ProductivityInformation == Cache.ProductivityInformation).IsCalibrated = true;
                DialogWindowProvider.ShowDialog($"Optical Power Meter {Cache.ProductivityInformation} Ok!");

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
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Cache.Item.FindPosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(async () =>
        {
            try
            {
                Calibratings = [];
                SelectedCalibratingItem = null;

                var coefficient = applicationCookie.LaserLightInformations.Max(t => t.Coefficient);

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    coefficient,
                    Cache.ProductivityInformation,
                    Cache.Item.FindPosition,
                    Cache.Item.RowNumber,
                    Cache.Item.ColumnNumber,
                    Cache.Item.ColumnCellWidth,
                    Cache.Item.RowCellHeight,
                    Cache.Item.WaitTime,
                    Cache.Item.RepeatCount,
                    Cache.Threshold
                }), HtmlLogUniqueId.LoggingHtml());

                // 中心点的索引
                var centerX = (Cache.Item.ColumnNumber - 1) / 2d;
                var centerY = (Cache.Item.RowNumber - 1) / 2d;

                var laserOpticalPowerObjDto = new LaserOpticalPowerMeterDto
                {
                    Coefficient = coefficient,
                    ProductivityInformation = Cache.ProductivityInformation,
                    FindCenterPosition = Cache.Item.FindPosition,
                    RowNumber = Cache.Item.RowNumber,
                    ColumnNumber = Cache.Item.ColumnNumber,
                    Map = []
                };

                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(laserOpticalPowerObjDto.FindCenterPosition);
                LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
                LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, coefficient);
                LaserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);
                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

                var repeatCout = 0;

                while (repeatCout < Cache.Item.RepeatCount)
                {
                    repeatCout++;

                    var temp = laserOpticalPowerObjDto.Map;
                    laserOpticalPowerObjDto.Map = [];

                    for (var row = 0; row < Cache.Item.RowNumber; row++)
                    {
                        for (var column = 0; column < Cache.Item.ColumnNumber; column++)
                        {
                            var position = laserOpticalPowerObjDto.FindCenterPosition + (Vector)new Point((column - centerX) * Cache.Item.ColumnCellWidth, (row - centerY) * Cache.Item.RowCellHeight);
                            laserOpticalPowerObjDto.Map.Add(new LaserOpticalPowerItemDto
                            {
                                MeasurePosition = position,
                                MeasurePower = temp.SingleOrDefault(t => t.MeasurePosition == position)?.MeasurePower ?? 0,
                                Row = row,
                                Column = column
                            });
                        }
                    }

                    SelectedCalibratingItem = laserOpticalPowerObjDto;

                    foreach (var laserOpticalPowerObjItem in laserOpticalPowerObjDto.Map)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (laserOpticalPowerObjItem.MeasurePower > 0) continue;

                        StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(laserOpticalPowerObjItem.MeasurePosition);

                        await Task.Delay(TimeSpan.FromSeconds(Cache.Item.WaitTime), cancellationToken).ConfigureAwait(false);

                        var measurePower = LaserViewModel.GetOpticalPowerMeter();

                        laserOpticalPowerObjItem.MeasurePower = measurePower;

                        OnPropertyChanged(nameof(SelectedCalibratingItem));
                    }

                    var maximumIndex = Vector<double>.Build.DenseOfEnumerable(laserOpticalPowerObjDto.Map.Select(t => t.MeasurePower)).MaximumIndex();
                    // maximumIndex转换为二维数组的索引
                    var maximumIndexRow = maximumIndex / Cache.Item.ColumnNumber;
                    var maximumIndexCol = maximumIndex % Cache.Item.ColumnNumber;
                    laserOpticalPowerObjDto.MeasureMaxPower = laserOpticalPowerObjDto.Map[maximumIndex].MeasurePower;
                    laserOpticalPowerObjDto.MeasureMaxPowerPosition = laserOpticalPowerObjDto.Map[maximumIndex].MeasurePosition;

                    var resultLaserOpticalPowerDto = laserOpticalPowerObjDto.Clone();
                    Calibratings = [.. Calibratings, resultLaserOpticalPowerDto];

                    var isInEdge = maximumIndexRow == 0 || maximumIndexRow == Cache.Item.RowNumber - 1 || maximumIndexCol == 0 || maximumIndexCol == Cache.Item.ColumnNumber - 1;

                    var htmlBulletList = new HtmlBullet(new
                    {
                        laserOpticalPowerObjDto.FindCenterPosition,
                        laserOpticalPowerObjDto.MeasureMaxPower,
                        laserOpticalPowerObjDto.MeasureMaxPowerPosition,
                        MaxRow = maximumIndexRow,
                        MaxColumn = maximumIndexCol,
                        IsInEdge = isInEdge,
                        Map = new HtmlPlot3DChart([.. laserOpticalPowerObjDto.Map.Select(t => new Point3D(t.MeasurePosition.X, t.MeasurePosition.Y, t.MeasurePower))], string.Empty, HtmlPlot3DType.Bar3D),
                        Table = new HtmlExpand(new HtmlTable([
                            .. laserOpticalPowerObjDto.Map.Select(t => new
                            {
                                t.Row,
                                t.Column,
                                t.MeasurePosition,
                                t.MeasurePower
                            })
                        ]), string.Empty)
                    });

                    // 判断maximumIndexRow,maximumIndexCol是不是再边缘点上
                    if (isInEdge)
                    {
                        Logger.LogHtmlInformation($"{repeatCout}", HtmlHeaderLevelEnum.Header3, htmlBulletList, HtmlLogUniqueId.LoggingHtml());
                        laserOpticalPowerObjDto.FindCenterPosition = laserOpticalPowerObjDto.MeasureMaxPowerPosition;

                        continue;
                    }

                    LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Close);

                    Logger.LogHtmlInformation($"{repeatCout} OK", HtmlHeaderLevelEnum.Header3, htmlBulletList, HtmlLogUniqueId.LoggingHtml());

                    SelectedCalibratingItem.IsCalibrated = true;
                    if (Save(SelectedCalibratingItem, cancellationToken) == false)
                    {
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                        SelectedCalibratingItem.IsCalibrated = false;

                        return false;
                    }

                    return true;
                }

                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Retry count exceeded!"), HtmlLogUniqueId.LoggingHtml());
                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Close);

                return false;
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

        await InvokeVerifyAsync(async () =>
        {
            try
            {
                Cache.ProductivityInformation = SelectedReviewItem.ProductivityInformation;
                
                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.ProductivityInformation,
                    Cache.Item.WaitTime,
                    Cache.Threshold,
                    SelectedReviewItem.Coefficient,
                    SelectedReviewItem.MeasureMaxPowerPosition,
                    SelectedReviewItem.MeasureMaxPower
                }), HtmlLogUniqueId.LoggingHtml());

                SelectedReviewItem.IsVerified = false;

                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(SelectedReviewItem.MeasureMaxPowerPosition);
                LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
                LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, SelectedReviewItem.Coefficient);
                LaserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);
                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

                var resultList = new List<double>();

                foreach (var _ in Enumerable.Range(0, 3))
                {
                    await Task.Delay(TimeSpan.FromSeconds(Cache.Item.WaitTime), cancellationToken).ConfigureAwait(false);

                    var measurePower = LaserViewModel.GetOpticalPowerMeter();

                    resultList.Add(measurePower);
                }

                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Close);

                var average = resultList.Average();
                var errorRate = Math.Abs((average - SelectedReviewItem.MeasureMaxPower) / SelectedReviewItem.MeasureMaxPower);
                var result = errorRate < Cache.Threshold;

                Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    ReviewMeasureMaxPower = average,
                    errorRate
                }), HtmlLogUniqueId.LoggingHtml());

                SelectedReviewItem.IsVerified = result;
                if (Save(SelectedReviewItem, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    SelectedReviewItem.IsVerified = false;

                    return false;
                }

                DialogWindowProvider.ShowDialog($"""
                                                 Verify {(result ? "OK" : "Failed")}
                                                 New Measure Max Power: ({average:0.###})
                                                 Old Measure Max Power:({SelectedReviewItem.MeasureMaxPower:0.###})
                                                 Error:({errorRate:0.###})
                                                 """,
                    DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);

                return result;
            }
            finally
            {
                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Close);
            }
        }).ConfigureAwait(false);
    }

    private bool Save(LaserOpticalPowerMeterDto item, CancellationToken cancellationToken) => InvokeSave(update =>
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