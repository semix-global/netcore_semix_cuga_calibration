using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Models;
using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.Ads.XGains;
using Core.Models.Models.Common.Status;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Behaviors;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using System.Collections.ObjectModel;
using static Core.Models.Models.Ads.XGains.AdsXGainsCache;

namespace CugaCalibration.ViewModels.Ads;

[IOCAppService(ServiceType = typeof(AdsXGainsCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AdsXGainsCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select a location" },
        new() { StepName = "X Positive And Negative Gains" },
        new() { StepName = "X Positive And Negative HPR" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<AdsXGainsCacheItem> _adsXGainsCacheItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsXGainsCacheItem> _adsXGainsCacheBestItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsXGainsCacheItem> _adsXGainsCacheConverseItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsXGainsCacheItem> _adsXGainsCacheConverseBestItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsXGainsDichotomySpeedCacheItem> _adsXGainsDichotomySpeedCacheItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsXGainsDichotomySpeedCacheItem> _adsXGainsDichotomySpeedCacheConverseItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsXGainsCacheItem> _adsXGainsHrpCacheItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsXGainsCacheItem> _adsXGainsHrpCacheConverseItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsXGainsCacheItem> _resultAdsXGainsHrpCacheItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsXGainsCacheItem> _resultAdsXGainsHrpCacheConverseItemList = [];

    [ObservableProperty]
    private AdsXGainsCacheItem? _selectAdsXGainsItemDto = new();

    [ObservableProperty]
    private AdsXGainsCacheItem _selectAdsXGainsCacheItem = new();

    [ObservableProperty]
    private AdsXGainsCacheItem _selectAdsXGainsCacheConverseItem = new();

    [ObservableProperty]
    private AdsXGainsItemDto _resultAdsXGainsItemDto = new();

    [ObservableProperty]
    private ObservableCollection<OpticsMagTypeEnumAndStageSpeedEnumCalibrationStatus> _calibrationStatusList =
    [
        ..EnumHelper.Enums<OpticsMagTypeEnum>().Select(t => new OpticsMagTypeEnumAndStageSpeedEnumCalibrationStatus
        {
            OpticsMagTypeEnum = t,
            StageSpeedEnumCalibrationStatusList = [..EnumHelper.Enums<StageSpeedEnum>().Select(tt => new StageSpeedEnumCalibrationStatus { StageSpeedEnum = tt, IsCalibrated = false })]
        })
    ];

    [ObservableProperty]
    private ObservableCollection<StageSpeedEnumCalibrationStatus> _calibrationStatusListItem =
    [
        .. EnumHelper.Enums<StageSpeedEnum>().Select(t => new StageSpeedEnumCalibrationStatus { StageSpeedEnum = t, IsCalibrated = false })
    ];

    private List<(double x1, double x2)> defaultXList = [];

    private bool IsX1Stop = false;
    private bool IsX2Stop = false;
    private bool IsX3Stop = false;
    private bool IsX4Stop = false;

    [ObservableProperty]
    private List<WpfPlotModel> _plotList = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private AdsXGainsItemDto _reviewReviewItemDto = new();

    [ObservableProperty]
    private ObservableCollection<AdsXGainsCacheItem> _positiveAdsXGainsHrpCacheItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsXGainsCacheItem> _negativeAdsXGainsHrpCacheItemList = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private AdsXGainsCache _cache = new();

    [ObservableProperty]
    private AdsXGainsItemDto _calibration = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<AdsPressureGainsDto>(out _, out var errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<AdsXGainsCache>();
        Calibration = CacheProvider.GetOrDefault<AdsXGainsItemDto>();
        if (isHasCache == false) CacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        ReviewReviewItemDto = Calibration.Clone();
        return ReviewReviewItemDto.IsCalibrated;
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            default:
                return true;
        }
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                SynchronizationContextProvider.Send(() => AdsXGainsCacheItemList.Clear());
                SynchronizationContextProvider.Send(() => AdsXGainsCacheConverseItemList.Clear());
                SynchronizationContextProvider.Send(() => AdsXGainsCacheBestItemList.Clear());
                SynchronizationContextProvider.Send(() => AdsXGainsCacheConverseBestItemList.Clear());
                SynchronizationContextProvider.Send(() => AdsXGainsDichotomySpeedCacheItemList.Clear());
                SynchronizationContextProvider.Send(() => AdsXGainsDichotomySpeedCacheConverseItemList.Clear());
                Cache.IsPositive = true;
                return true;

            case 1:
                return true;

            case 2:
                var isCalibrated = CalibrationStepIndex == 2;
                if (ResultAdsXGainsItemDto is null)
                {
                    DialogWindowProvider.TryShowDialog("Please find x gains!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    ResultAdsXGainsItemDto.IsCalibrated = isCalibrated;
                    if (Save(ResultAdsXGainsItemDto, cancellationToken) == false)
                    {
                        ResultAdsXGainsItemDto.IsCalibrated = false;
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                        return false;
                    }
                }

                IsCalibrated = isCalibrated;
                ClearCalibrationTemp();
                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> CancelingAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);
        var point = StageViewModel.BrightFieldToMachinePosition(Point.Origin);
        StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(point);
        return true;
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand]
    private async Task GetPointAsync(string name)
    {
        try
        {
            await Task.Run(() =>
            {
                var result = StageViewModel.GetMachineStagePosition();

                Cache.GetType().GetProperty(name)!.SetValue(Cache, result);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Point Failed", Name);
        }
    }

    [RelayCommand]
    private async Task GotoPointAsync(string name)
    {
        try
        {
            await Task.Run(() => { StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus((Point)Cache.GetType().GetProperty(name)!.GetValue(Cache)); }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
        }
    }

    [RelayCommand]
    private async Task ShowPlotHRPAsync(AdsXGainsCacheItem adsXGainsItemDto)
    {
        try
        {
            await Task.Run(() =>
            {
                var plotList = new List<(string Title, IReadOnlyList<Point> Points)>();
                if (adsXGainsItemDto.IsPositive)
                {
                    plotList =
                    [
                        ("H", adsXGainsItemDto.PositivePlotH.ToPoints()),
                        ("R", adsXGainsItemDto.PositivePlotR.ToPoints()),
                        ("P", adsXGainsItemDto.PositivePlotP.ToPoints())
                    ];
                }
                else
                {
                    plotList =
                    [
                        ("H", adsXGainsItemDto.NegativePlotH.ToPoints()),
                        ("R", adsXGainsItemDto.NegativePlotR.ToPoints()),
                        ("P", adsXGainsItemDto.NegativePlotP.ToPoints())
                    ];
                }

                DialogWindowProvider.ShowPlotAsReadonly(plotList);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Point Failed", Name);
        }
    }

    [RelayCommand]
    private async Task GetSensorXSpeedFeedForwardValueAsync()
    {
        try
        {
            await Task.Run(() => { var (_, _) = AdsViewModel.GetSensorXSpeedFeedForwardValue(Cache.IsPositive); }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Sensor X Speed Feed Forward Value Failed", Name);
        }
    }

    [RelayCommand]
    private async Task SetSensorXSpeedFeedForwardValueAsync()
    {
        try
        {
            await Task.Run(() => { AdsViewModel.SetSensorXSpeedFeedForwardValue(Cache.IsPositive, (Cache.IsPositive ? Cache.X1 : Cache.X3, Cache.IsPositive ? Cache.X2 : Cache.X4)); }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Set Sensor X Speed Feed Forward Value Failed", Name);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(() =>
        {
            defaultXList = [];
            var (x1, x2) = AdsViewModel.GetSensorXSpeedFeedForwardValue(true);
            var (x3, x4) = AdsViewModel.GetSensorXSpeedFeedForwardValue(false);
            defaultXList = [(x1, x2), (x3, x4)];
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                X1 = x1,
                X2 = x2,
                X3 = x3,
                X4 = x4,
                XPositiveStartPosition = Cache.PositiveStartPosition,
                XPositiveEndPosition = Cache.PositiveEndPosition,
                XNegativeStartPosition = Cache.NegativeStartPosition,
                XNegativeEndPosition = Cache.NegativeEndPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
        return true;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
        await InvokeCalibrateAsync(async () =>
        {
            try
            {
                if (Cache.IsPositive)
                {
                    Cache.SetStartPosition(Cache.PositiveStartPosition);
                    Cache.SetEndPosition(Cache.PositiveEndPosition);
                }
                else
                {
                    Cache.SetStartPosition(Cache.NegativeStartPosition);
                    Cache.SetEndPosition(Cache.NegativeEndPosition);
                }

                ClearCalibrationTemp();
                foreach (var speedvalue in Cache.SpeedXValueList)
                {
                    var z1IsPositive = false;
                    var z2IsPositive = false;
                    var z3IsPositive = false;
                    var z4IsPositive = false;
                    IsX1Stop = false;
                    IsX2Stop = false;
                    IsX3Stop = false;
                    IsX4Stop = false;
                    {
                        var adsXGainsDichotomySpeedCacheItem = new AdsXGainsDichotomySpeedCacheItem()
                        {
                            IsPositive = Cache.IsPositive,
                            SpeedXValue = speedvalue,
                        };
                        SynchronizationContextProvider.Send(() => AdsXGainsDichotomySpeedCacheItemList.Add(adsXGainsDichotomySpeedCacheItem));

                        var adsXGainsCacheItem = new AdsXGainsCacheItem
                        {
                            Index = 1,
                            SpeedXValue = speedvalue,
                            IsPositive = Cache.IsPositive
                        };
                        adsXGainsCacheItem.SetX1(Cache.FindMinX);
                        adsXGainsCacheItem.SetX2(Cache.FindMinX);
                        Logger.LogHtmlInformation($"Param_V{speedvalue}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                        {
                            Cache.IsPositive,
                            StartPosition = Cache.GetStartPosition(),
                            EndPosition = Cache.GetEndPosition(),
                            SpeedXValue = speedvalue
                        }), HtmlLogUniqueId.LoggingHtml());
                        var (isSuccess, transBuffer) = await GetZ1Z2CurveAsync(adsXGainsCacheItem, cancellationToken).ConfigureAwait(false);
                        if (isSuccess == false)
                        {
                            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Get Hrp Failed!"), HtmlLogUniqueId.LoggingHtml());
                            break;
                        }

                        FindAdsXGainZ1Z2(adsXGainsCacheItem, transBuffer);
                        SynchronizationContextProvider.Send(() => AdsXGainsCacheItemList.Add(adsXGainsCacheItem));
                        if (adsXGainsCacheItem.GetZ1() <= 0)
                        {
                            z1IsPositive = true;
                        }

                        if (adsXGainsCacheItem.GetZ2() <= 0)
                        {
                            z2IsPositive = true;
                        }
                    }

                    {
                        var adsXGainsDichotomySpeedCacheConverseItem = new AdsXGainsDichotomySpeedCacheItem()
                        {
                            IsPositive = !Cache.IsPositive,
                            SpeedXValue = speedvalue,
                        };
                        SynchronizationContextProvider.Send(() => AdsXGainsDichotomySpeedCacheConverseItemList.Add(adsXGainsDichotomySpeedCacheConverseItem));

                        var adsXGainsCacheConverseItem = new AdsXGainsCacheItem
                        {
                            Index = 1,
                            SpeedXValue = speedvalue,
                            IsPositive = !Cache.IsPositive
                        };
                        adsXGainsCacheConverseItem.SetX1(Cache.FindMinX);
                        adsXGainsCacheConverseItem.SetX2(Cache.FindMinX);

                        var (isSuccess, transBuffer) = await GetZ1Z2CurveAsync(adsXGainsCacheConverseItem, cancellationToken).ConfigureAwait(false);
                        if (isSuccess == false)
                        {
                            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Get Hrp Failed!"), HtmlLogUniqueId.LoggingHtml());
                            break;
                        }

                        FindAdsXGainZ1Z2(adsXGainsCacheConverseItem, transBuffer);
                        SynchronizationContextProvider.Send(() => AdsXGainsCacheConverseItemList.Add(adsXGainsCacheConverseItem));
                        if (adsXGainsCacheConverseItem.GetZ1() <= 0)
                        {
                            z3IsPositive = true;
                        }

                        if (adsXGainsCacheConverseItem.GetZ2() <= 0)
                        {
                            z4IsPositive = true;
                        }
                    }

                    var x1Min = Cache.FindMinX;
                    var x1Max = Cache.FindMaxX;
                    var x2Min = Cache.FindMinX;
                    var x2Max = Cache.FindMaxX;
                    var x3Min = Cache.FindMinX;
                    var x3Max = Cache.FindMaxX;
                    var x4Min = Cache.FindMinX;
                    var x4Max = Cache.FindMaxX;
                    var Z1List = new List<(int x1, double z1)>();
                    var Z2List = new List<(int x2, double z2)>();
                    var Z3List = new List<(int x1, double z1)>();
                    var Z4List = new List<(int x2, double z2)>();
                    double Z10 = 0, Z20 = 0, Z30 = 0, Z40 = 0;
                    double N1 = 0, N2 = 0, N3 = 0, N4 = 0;
                    var isStop12 = true;
                    var isStop34 = true;
                    var index12 = 1;
                    var index34 = 1;
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (isStop12)
                        {
                            index12++;
                            var resultValue = await DichotomyFindX1X2Async(Cache.IsPositive, speedvalue, index12, x1Min, x1Max, x2Min, x2Max, cancellationToken);
                            Z1List.Add((resultValue.x1, resultValue.Z1));
                            Z2List.Add((resultValue.x2, resultValue.Z2));
                            if (!IsX1Stop)
                            {
                                if (index12 > 3)
                                {
                                    if (((Math.Abs(resultValue.Z1) - Math.Abs(Z10)) > 200) || (Math.Abs(resultValue.Z1) > 300))
                                    {
                                        if (resultValue.z1IsPositive == z1IsPositive)
                                        {
                                            x1Max = resultValue.x1;
                                            x1Min = Cache.FindMinX;
                                        }
                                        else
                                        {
                                            x1Min = resultValue.x1;
                                            x1Max = Cache.FindMaxX;
                                        }

                                        if (x1Max > Cache.FindMaxX)
                                            x1Max = Cache.FindMaxX;
                                        if (x1Min < Cache.FindMinX)
                                            x1Min = Cache.FindMinX;
                                    }
                                    else
                                    {
                                        if (resultValue.z1IsPositive == z1IsPositive) x1Min = resultValue.x1;
                                        else x1Max = resultValue.x1;
                                    }
                                }
                                else
                                {
                                    if (resultValue.z1IsPositive == z1IsPositive) x1Min = resultValue.x1;
                                    else x1Max = resultValue.x1;
                                }

                                Z10 = resultValue.Z1;
                                if (x1Min == x1Max || (x1Min + 1 == x1Max))
                                {
                                    var (x1, _) = Z1List.OrderBy(t => Math.Abs(t.z1)).FirstOrDefault();
                                    AdsXGainsDichotomySpeedCacheItemList.Single(t => t.SpeedXValue == speedvalue).SetX1(x1);
                                    IsX1Stop = true;
                                }
                            }
                            else
                            {
                                var minZ1Item = Z1List.OrderBy(t => Math.Abs(t.z1)).FirstOrDefault();
                                AdsXGainsDichotomySpeedCacheItemList.Single(t => t.SpeedXValue == speedvalue).SetX1(minZ1Item.x1);
                            }

                            if (!IsX2Stop)
                            {
                                if (index12 > 3)
                                {
                                    if (((Math.Abs(resultValue.Z2) - Math.Abs(Z20)) > 200) || (Math.Abs(resultValue.Z2) > 300))
                                    {
                                        if (resultValue.z2IsPositive == z2IsPositive)
                                        {
                                            x2Max = resultValue.x2;
                                            x2Min = Cache.FindMinX;
                                        }
                                        else
                                        {
                                            x2Min = resultValue.x2;
                                            x2Max = Cache.FindMaxX;
                                        }

                                        if (x2Max > Cache.FindMaxX)
                                            x2Max = Cache.FindMaxX;
                                        if (x2Min < Cache.FindMinX)
                                            x2Min = Cache.FindMinX;
                                    }
                                    else
                                    {
                                        if (resultValue.z2IsPositive == z2IsPositive) x2Min = resultValue.x2;
                                        else x2Max = resultValue.x2;
                                    }
                                }
                                else
                                {
                                    if (resultValue.z2IsPositive == z2IsPositive) x2Min = resultValue.x2;
                                    else x2Max = resultValue.x2;
                                }

                                Z20 = resultValue.Z2;
                                if ((x2Min == x2Max) || (x2Min + 1 == x2Max))
                                {
                                    var (x2, _) = Z2List.OrderBy(t => Math.Abs(t.z2)).FirstOrDefault();
                                    AdsXGainsDichotomySpeedCacheItemList.Single(t => t.SpeedXValue == speedvalue).SetX2(x2);
                                    IsX2Stop = true;
                                }
                            }
                            else
                            {
                                var minZ2Item = Z2List.OrderBy(t => Math.Abs(t.z2)).FirstOrDefault();
                                AdsXGainsDichotomySpeedCacheItemList.Single(t => t.SpeedXValue == speedvalue).SetX2(minZ2Item.x2);
                            }

                            if (IsX1Stop && IsX2Stop) isStop12 = false;
                        }

                        if (isStop34)
                        {
                            index34++;
                            var resultValue = await DichotomyFindX1X2Async(!Cache.IsPositive, speedvalue, index34, x3Min, x3Max, x4Min, x4Max, cancellationToken);
                            Z3List.Add((resultValue.x1, resultValue.Z1));
                            Z4List.Add((resultValue.x2, resultValue.Z2));
                            if (!IsX3Stop)
                            {
                                if (index34 > 3)
                                {
                                    if (((Math.Abs(resultValue.Z1) - Math.Abs(Z30)) > 200) || (Math.Abs(resultValue.Z1) > 300))
                                    {
                                        N3++;
                                        if (N3 <= 1)
                                        {
                                            if (resultValue.z1IsPositive == z3IsPositive)
                                            {
                                                x3Max = resultValue.x1;
                                                x3Min = Cache.FindMinX;
                                            }
                                            else
                                            {
                                                x3Min = resultValue.x1;
                                                x3Max = Cache.FindMaxX;
                                            }
                                        }
                                        else
                                        {
                                            if (resultValue.z1IsPositive == z3IsPositive)
                                            {
                                                x3Min = resultValue.x1;
                                                x3Max = Cache.FindMaxX;
                                            }
                                            else
                                            {
                                                x3Max = resultValue.x1;
                                                x3Min = Cache.FindMinX;
                                            }
                                        }

                                        if (x3Max > Cache.FindMaxX)
                                            x3Max = Cache.FindMaxX;
                                        if (x3Min < Cache.FindMinX)
                                            x3Min = Cache.FindMinX;
                                    }
                                    else
                                    {
                                        N3 = 0;
                                        if (resultValue.z1IsPositive == z3IsPositive) x3Min = resultValue.x1;
                                        else x3Max = resultValue.x1;
                                    }
                                }
                                else
                                {
                                    if (resultValue.z1IsPositive == z3IsPositive) x3Min = resultValue.x1;
                                    else x3Max = resultValue.x1;
                                }

                                Z30 = resultValue.Z1;
                                if (x3Min == x3Max || (x3Min + 1 == x3Max))
                                {
                                    var (x1, _) = Z3List.OrderBy(t => Math.Abs(t.z1)).FirstOrDefault();
                                    AdsXGainsDichotomySpeedCacheConverseItemList.Single(t => t.SpeedXValue == speedvalue).SetX1(x1);
                                    IsX3Stop = true;
                                }
                            }
                            else
                            {
                                var minZ1Item = Z3List.OrderBy(t => Math.Abs(t.z1)).FirstOrDefault();
                                AdsXGainsDichotomySpeedCacheConverseItemList.Single(t => t.SpeedXValue == speedvalue).SetX1(minZ1Item.x1);
                            }

                            if (!IsX4Stop)
                            {
                                if (index34 > 3)
                                {
                                    if (((Math.Abs(resultValue.Z2) - Math.Abs(Z40)) > 200) || (Math.Abs(resultValue.Z2) > 300))
                                    {
                                        N4++;
                                        if (N4 <= 1)
                                        {
                                            if (resultValue.z2IsPositive == z4IsPositive)
                                            {
                                                x4Max = resultValue.x2;
                                                x4Min = Cache.FindMinX;
                                            }
                                            else
                                            {
                                                x4Min = resultValue.x2;
                                                x4Max = Cache.FindMaxX;
                                            }
                                        }
                                        else
                                        {
                                            if (resultValue.z2IsPositive == z4IsPositive)
                                            {
                                                x4Min = resultValue.x2;
                                                x4Max = Cache.FindMaxX;
                                            }
                                            else
                                            {
                                                x4Max = resultValue.x2;
                                                x4Min = Cache.FindMinX;
                                            }
                                        }

                                        if (x4Max > Cache.FindMaxX)
                                            x4Max = Cache.FindMaxX;
                                        if (x4Min < Cache.FindMinX)
                                            x4Min = Cache.FindMinX;
                                    }
                                    else
                                    {
                                        N4 = 0;
                                        if (resultValue.z2IsPositive == z4IsPositive) x4Min = resultValue.x2;
                                        else x4Max = resultValue.x2;
                                    }
                                }
                                else
                                {
                                    if (resultValue.z2IsPositive == z4IsPositive) x4Min = resultValue.x2;
                                    else x4Max = resultValue.x2;
                                }

                                Z40 = resultValue.Z2;
                                if ((x4Min == x4Max) || (x4Min + 1 == x4Max))
                                {
                                    var (x2, _) = Z4List.OrderBy(t => Math.Abs(t.z2)).FirstOrDefault();
                                    AdsXGainsDichotomySpeedCacheConverseItemList.Single(t => t.SpeedXValue == speedvalue).SetX2(x2);
                                    IsX4Stop = true;
                                }
                            }
                            else
                            {
                                var minZ2Item = Z4List.OrderBy(t => Math.Abs(t.z2)).FirstOrDefault();
                                AdsXGainsDichotomySpeedCacheConverseItemList.Single(t => t.SpeedXValue == speedvalue).SetX2(minZ2Item.x2);
                            }

                            if (IsX3Stop && IsX4Stop) isStop34 = false;
                        }

                        if ((isStop12 == false) && (isStop34 == false))
                            break;
                    }

                    var adsXGainsZ1Z2HrpCacheList = AdsXGainsCacheItemList.Where(t => t.SpeedXValue == speedvalue).ToList();
                    var hPoint3DList = Enumerable.Range(0, adsXGainsZ1Z2HrpCacheList.Count).Select((t, index) => new Point3D(adsXGainsZ1Z2HrpCacheList[index].GetX1(), adsXGainsZ1Z2HrpCacheList[index].GetX2(), adsXGainsZ1Z2HrpCacheList[index].GetH())).ToList();
                    var rPoint3DList = Enumerable.Range(0, adsXGainsZ1Z2HrpCacheList.Count).Select((t, index) => new Point3D(adsXGainsZ1Z2HrpCacheList[index].GetX1(), adsXGainsZ1Z2HrpCacheList[index].GetX2(), adsXGainsZ1Z2HrpCacheList[index].GetR())).ToList();
                    var pPoint3DList = Enumerable.Range(0, adsXGainsZ1Z2HrpCacheList.Count).Select((t, index) => new Point3D(adsXGainsZ1Z2HrpCacheList[index].GetX1(), adsXGainsZ1Z2HrpCacheList[index].GetX2(), adsXGainsZ1Z2HrpCacheList[index].GetP())).ToList();
                    Logger.LogHtmlInformation($"HRPPoint3D_V{speedvalue.ToString()}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        HPoint3DList = new HtmlPlot3DChart([.. hPoint3DList], "HPlot3D", HtmlPlot3DType.Bar3D),
                        RPoint3DList = new HtmlPlot3DChart([.. rPoint3DList], "RPlot3D", HtmlPlot3DType.Bar3D),
                        PPoint3DList = new HtmlPlot3DChart([.. pPoint3DList], "PPlot3D", HtmlPlot3DType.Bar3D)
                    }), HtmlLogUniqueId.LoggingHtml());

                    adsXGainsZ1Z2HrpCacheList = AdsXGainsCacheConverseItemList.Where(t => t.SpeedXValue == speedvalue).ToList();
                    hPoint3DList = Enumerable.Range(0, adsXGainsZ1Z2HrpCacheList.Count).Select((t, index) => new Point3D(adsXGainsZ1Z2HrpCacheList[index].GetX1(), adsXGainsZ1Z2HrpCacheList[index].GetX2(), adsXGainsZ1Z2HrpCacheList[index].GetH())).ToList();
                    rPoint3DList = Enumerable.Range(0, adsXGainsZ1Z2HrpCacheList.Count).Select((t, index) => new Point3D(adsXGainsZ1Z2HrpCacheList[index].GetX1(), adsXGainsZ1Z2HrpCacheList[index].GetX2(), adsXGainsZ1Z2HrpCacheList[index].GetR())).ToList();
                    pPoint3DList = Enumerable.Range(0, adsXGainsZ1Z2HrpCacheList.Count).Select((t, index) => new Point3D(adsXGainsZ1Z2HrpCacheList[index].GetX1(), adsXGainsZ1Z2HrpCacheList[index].GetX2(), adsXGainsZ1Z2HrpCacheList[index].GetP())).ToList();
                    Logger.LogHtmlInformation($"HRPPoint3D_V_Converse{speedvalue.ToString()}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        HPoint3DList = new HtmlPlot3DChart([.. hPoint3DList], "HPlot3D_Converse", HtmlPlot3DType.Bar3D),
                        RPoint3DList = new HtmlPlot3DChart([.. rPoint3DList], "RPlot3D_Converse", HtmlPlot3DType.Bar3D),
                        PPoint3DList = new HtmlPlot3DChart([.. pPoint3DList], "PPlot3D_Converse", HtmlPlot3DType.Bar3D)
                    }), HtmlLogUniqueId.LoggingHtml());
                }

                for (int r = 0; r < Cache.SpeedXValueList.Count; r++)
                {
                    var speedvalue = Cache.SpeedXValueList[r];
                    var HrpItemList = AdsXGainsCacheItemList.Where(t => t.SpeedXValue == speedvalue).ToList();
                    var bestHrpItem = HrpItemList.OrderBy(t => t.SumHRP).First();
                    AdsXGainsCacheBestItemList.Add(bestHrpItem);
                    HrpItemList = AdsXGainsCacheConverseItemList.Where(t => t.SpeedXValue == speedvalue).ToList();
                    bestHrpItem = HrpItemList.OrderBy(t => t.SumHRP).First();
                    AdsXGainsCacheConverseBestItemList.Add(bestHrpItem);
                }

                Logger.LogHtmlInformation("SpeedBestXValueTable", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Cache.Threshold,
                    //SpeedXPositiveTable = new HtmlTable([.. AdsXGainsDichotomySpeedCacheItemList.Select(t => new { t.SpeedXValue, t.PositiveX1, t.PositiveX2 }).Cast<object>()])
                    SpeedXPositiveTable = new HtmlTable([.. AdsXGainsCacheBestItemList.Select(t => new { t.SpeedXValue, t.PositiveX1, t.PositiveX2 }).Cast<object>()])
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("SpeedBestXValueTable_Converse", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Cache.Threshold,
                    SpeedXNegativeTable = new HtmlTable([.. AdsXGainsCacheConverseBestItemList.Select(t => new { t.SpeedXValue, t.NegativeX3, t.NegativeX4 }).Cast<object>()])
                }), HtmlLogUniqueId.LoggingHtml());
            }
            catch (Exception ex)
            {
                SetDefaultXValue(cancellationToken);

                DialogWindowProvider.ShowDialog("Calibrate Z1Z2 Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: {ex.Message}!"), HtmlLogUniqueId.LoggingHtml());
                result = false;
                return result;
            }

            return result;
        }).ConfigureAwait(false);
        Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingPeekHtml());
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        SynchronizationContextProvider.Send(() => AdsXGainsHrpCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsXGainsHrpCacheConverseItemList.Clear());
        SynchronizationContextProvider.Send(() => ResultAdsXGainsHrpCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => ResultAdsXGainsHrpCacheConverseItemList.Clear());

        var result = true;
        await InvokeCalibrateAsync(async () =>
        {
            try
            {
                for (int r = 0; r < AdsXGainsDichotomySpeedCacheItemList.Count; r++)
                {
                    var speedCacheItem = AdsXGainsDichotomySpeedCacheItemList[r];
                    var speedCacheConverseItem = AdsXGainsDichotomySpeedCacheConverseItemList[r];

                    Logger.LogHtmlInformation($"Param_V{speedCacheItem.SpeedXValue}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        speedCacheItem.IsPositive,
                        speedCacheItem.SpeedXValue,
                        HrpThreshold = Cache.VerifyThreshold,
                        StartPosition = Cache.GetStartPosition(),
                        EndPosition = Cache.GetEndPosition()
                    }), HtmlLogUniqueId.LoggingHtml());
                    Logger.LogHtmlInformation($"Param_V_Converse{speedCacheConverseItem.SpeedXValue}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        speedCacheConverseItem.IsPositive,
                        speedCacheConverseItem.SpeedXValue,
                        HrpThreshold = Cache.VerifyThreshold,
                        StartPosition = Cache.GetEndPosition(),
                        EndPosition = Cache.GetStartPosition()
                    }), HtmlLogUniqueId.LoggingHtml());

                    var HrpItemList = AdsXGainsCacheItemList.Where(t => t.SpeedXValue == speedCacheItem.SpeedXValue).ToList();
                    var hPoint3DList = Enumerable.Range(0, HrpItemList.Count()).Select((t, index) => new Point3D(HrpItemList.ElementAt(index).GetX1(), HrpItemList.ElementAt(index).GetX2(), HrpItemList.ElementAt(index).GetH())).ToList();
                    var rPoint3DList = Enumerable.Range(0, HrpItemList.Count()).Select((t, index) => new Point3D(HrpItemList.ElementAt(index).GetX1(), HrpItemList.ElementAt(index).GetX2(), HrpItemList.ElementAt(index).GetR())).ToList();
                    var pPoint3DList = Enumerable.Range(0, HrpItemList.Count()).Select((t, index) => new Point3D(HrpItemList.ElementAt(index).GetX1(), HrpItemList.ElementAt(index).GetX2(), HrpItemList.ElementAt(index).GetP())).ToList();
                    Logger.LogHtmlInformation($"HRPPoint3D_V{speedCacheItem.SpeedXValue.ToString()}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        HPoint3DList = new HtmlPlot3DChart([.. hPoint3DList], "HPlot3D", HtmlPlot3DType.Bar3D),
                        RPoint3DList = new HtmlPlot3DChart([.. rPoint3DList], "RPlot3D", HtmlPlot3DType.Bar3D),
                        PPoint3DList = new HtmlPlot3DChart([.. pPoint3DList], "PPlot3D", HtmlPlot3DType.Bar3D)
                    }), HtmlLogUniqueId.LoggingHtml());

                    var bestHrpItem = HrpItemList.OrderBy(t => t.SumHRP).First();
                    SynchronizationContextProvider.Send(() => ResultAdsXGainsHrpCacheItemList.Add(bestHrpItem));

                    HrpItemList = AdsXGainsCacheConverseItemList.Where(t => t.SpeedXValue == speedCacheConverseItem.SpeedXValue).ToList();
                    hPoint3DList = Enumerable.Range(0, HrpItemList.Count()).Select((t, index) => new Point3D(HrpItemList.ElementAt(index).GetX1(), HrpItemList.ElementAt(index).GetX2(), HrpItemList.ElementAt(index).GetH())).ToList();
                    rPoint3DList = Enumerable.Range(0, HrpItemList.Count()).Select((t, index) => new Point3D(HrpItemList.ElementAt(index).GetX1(), HrpItemList.ElementAt(index).GetX2(), HrpItemList.ElementAt(index).GetR())).ToList();
                    pPoint3DList = Enumerable.Range(0, HrpItemList.Count()).Select((t, index) => new Point3D(HrpItemList.ElementAt(index).GetX1(), HrpItemList.ElementAt(index).GetX2(), HrpItemList.ElementAt(index).GetP())).ToList();
                    Logger.LogHtmlInformation($"HRPPoint3D_V_Converse{speedCacheConverseItem.SpeedXValue.ToString()}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        HPoint3DList = new HtmlPlot3DChart([.. hPoint3DList], "HPlot3D", HtmlPlot3DType.Bar3D),
                        RPoint3DList = new HtmlPlot3DChart([.. rPoint3DList], "RPlot3D", HtmlPlot3DType.Bar3D),
                        PPoint3DList = new HtmlPlot3DChart([.. pPoint3DList], "PPlot3D", HtmlPlot3DType.Bar3D)
                    }), HtmlLogUniqueId.LoggingHtml());

                    bestHrpItem = HrpItemList.OrderBy(t => t.SumHRP).First();
                    SynchronizationContextProvider.Send(() => ResultAdsXGainsHrpCacheConverseItemList.Add(bestHrpItem));
                }

                Logger.LogHtmlInformation("SpeedBestXValueTable", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Cache.Threshold,
                    SpeedXPositiveTable = new HtmlTable([.. ResultAdsXGainsHrpCacheItemList.Select(t => new { t.SpeedXValue, t.PositiveX1, t.PositiveX2 }).Cast<object>()])
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("SpeedBestXValueTable_Converse", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Cache.Threshold,
                    SpeedXNegativeTable = new HtmlTable([.. ResultAdsXGainsHrpCacheConverseItemList.Select(t => new { t.SpeedXValue, t.NegativeX3, t.NegativeX4 }).Cast<object>()])
                }), HtmlLogUniqueId.LoggingHtml());

                {
                    var adsXGainsItemDto = new AdsXGainsItemDto
                    {
                        IsPositive = Cache.IsPositive
                    };
                    var SpeedValueList = new List<double>();
                    var x1PlotList = new List<Point>();
                    var x1SmoothPlotList = new List<Point>();
                    var x2PlotList = new List<Point>();
                    var x2SmoothPlotList = new List<Point>();
                    var x1List = new List<double>();
                    var x2List = new List<double>();
                    foreach (var hrpCacheItem in ResultAdsXGainsHrpCacheItemList)
                    {
                        SpeedValueList.Add(hrpCacheItem.SpeedXValue);
                        x1List.Add(hrpCacheItem.GetX1());
                        x2List.Add(hrpCacheItem.GetX2());
                        x1PlotList.Add(new Point(hrpCacheItem.SpeedXValue, hrpCacheItem.GetX1()));
                        x2PlotList.Add(new Point(hrpCacheItem.SpeedXValue, hrpCacheItem.GetX2()));
                    }

                    var X = Vector<double>.Build.DenseOfEnumerable(SpeedValueList);
                    var Y1 = Vector<double>.Build.DenseOfEnumerable(x1List);
                    var Y2 = Vector<double>.Build.DenseOfEnumerable(x2List);
                    var (p0, p1, p2, _, yPredicted1) = PolynomialLeastSquares.Polynomial2Fit(X, Y1);
                    for (var i = 0; i < SpeedValueList.Count; i++)
                    {
                        x1SmoothPlotList.Add(new Point(SpeedValueList[i], yPredicted1[i]));
                    }

                    adsXGainsItemDto.SetX1P1(p2);
                    adsXGainsItemDto.SetX1P2(p1);
                    adsXGainsItemDto.SetX1P3(p0);
                    adsXGainsItemDto.SetX1Plots(x1PlotList);
                    adsXGainsItemDto.SetX1SmoothPlots(x1SmoothPlotList);
                    var (p3, p4, p5, _, yPredicted2) = PolynomialLeastSquares.Polynomial2Fit(X, Y2);
                    for (var i = 0; i < SpeedValueList.Count; i++)
                    {
                        x2SmoothPlotList.Add(new Point(SpeedValueList[i], yPredicted2[i]));
                    }

                    adsXGainsItemDto.SetX2P1(p5);
                    adsXGainsItemDto.SetX2P2(p4);
                    adsXGainsItemDto.SetX2P3(p3);
                    adsXGainsItemDto.SetX2Plots(x2PlotList);
                    adsXGainsItemDto.SetX2SmoothPlots(x2SmoothPlotList);
                    ResultAdsXGainsItemDto.UpdatePositive(adsXGainsItemDto);
                }

                {
                    var adsXGainsItemDto = new AdsXGainsItemDto
                    {
                        IsPositive = !Cache.IsPositive
                    };
                    var SpeedValueList = new List<double>();
                    var x1PlotList = new List<Point>();
                    var x1SmoothPlotList = new List<Point>();
                    var x2PlotList = new List<Point>();
                    var x2SmoothPlotList = new List<Point>();
                    var x1List = new List<double>();
                    var x2List = new List<double>();
                    foreach (var hrpCacheItem in ResultAdsXGainsHrpCacheConverseItemList)
                    {
                        SpeedValueList.Add(hrpCacheItem.SpeedXValue);
                        x1List.Add(hrpCacheItem.GetX1());
                        x2List.Add(hrpCacheItem.GetX2());
                        x1PlotList.Add(new Point(hrpCacheItem.SpeedXValue, hrpCacheItem.GetX1()));
                        x2PlotList.Add(new Point(hrpCacheItem.SpeedXValue, hrpCacheItem.GetX2()));
                    }

                    var X = Vector<double>.Build.DenseOfEnumerable(SpeedValueList);
                    var Y1 = Vector<double>.Build.DenseOfEnumerable(x1List);
                    var Y2 = Vector<double>.Build.DenseOfEnumerable(x2List);
                    var (p0, p1, p2, _, yPredicted1) = PolynomialLeastSquares.Polynomial2Fit(X, Y1);
                    for (var i = 0; i < SpeedValueList.Count; i++)
                    {
                        x1SmoothPlotList.Add(new Point(SpeedValueList[i], yPredicted1[i]));
                    }

                    adsXGainsItemDto.SetX1P1(p2);
                    adsXGainsItemDto.SetX1P2(p1);
                    adsXGainsItemDto.SetX1P3(p0);
                    adsXGainsItemDto.SetX1Plots(x1PlotList);
                    adsXGainsItemDto.SetX1SmoothPlots(x1SmoothPlotList);
                    var (p3, p4, p5, _, yPredicted2) = PolynomialLeastSquares.Polynomial2Fit(X, Y2);
                    for (var i = 0; i < SpeedValueList.Count; i++)
                    {
                        x2SmoothPlotList.Add(new Point(SpeedValueList[i], yPredicted2[i]));
                    }

                    adsXGainsItemDto.SetX2P1(p5);
                    adsXGainsItemDto.SetX2P2(p4);
                    adsXGainsItemDto.SetX2P3(p3);
                    adsXGainsItemDto.SetX2Plots(x2PlotList);
                    adsXGainsItemDto.SetX2SmoothPlots(x2SmoothPlotList);
                    ResultAdsXGainsItemDto.UpdateNegative(adsXGainsItemDto);
                }

                Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    X1P1 = ResultAdsXGainsItemDto.PositiveX1P1,
                    X1P2 = ResultAdsXGainsItemDto.PositiveX1P2,
                    X1P3 = ResultAdsXGainsItemDto.PositiveX1P3,
                    X2P1 = ResultAdsXGainsItemDto.PositiveX2P1,
                    X2P2 = ResultAdsXGainsItemDto.PositiveX2P2,
                    X2P3 = ResultAdsXGainsItemDto.PositiveX2P3,
                    X1X2Plots = new HtmlPlot2DLinesChart([
                        ("X1Plots", ResultAdsXGainsItemDto.PositiveX1Plots.ToArray()), ($"X1={ResultAdsXGainsItemDto.PositiveX1P1})*V^2{GetXPositiveAndNegativeString(ResultAdsXGainsItemDto.PositiveX1P2)}*V{GetXPositiveAndNegativeString(ResultAdsXGainsItemDto.PositiveX1P3)}", ResultAdsXGainsItemDto.PositiveX1SmoothPlots.ToArray()),
                        ("X2Plots", ResultAdsXGainsItemDto.PositiveX2Plots.ToArray()), ($"X2={ResultAdsXGainsItemDto.PositiveX2P1}*V^2{GetXPositiveAndNegativeString(ResultAdsXGainsItemDto.PositiveX2P2)}*V{GetXPositiveAndNegativeString(ResultAdsXGainsItemDto.PositiveX2P3)}", ResultAdsXGainsItemDto.PositiveX2SmoothPlots.ToArray())
                    ], "X1X2Plots")
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    X3P1 = ResultAdsXGainsItemDto.NegativeX3P1,
                    X3P2 = ResultAdsXGainsItemDto.NegativeX3P2,
                    X3P3 = ResultAdsXGainsItemDto.NegativeX3P3,
                    X4P1 = ResultAdsXGainsItemDto.NegativeX4P1,
                    X4P2 = ResultAdsXGainsItemDto.NegativeX4P2,
                    X4P3 = ResultAdsXGainsItemDto.NegativeX4P3,
                    X3X4Plots = new HtmlPlot2DLinesChart([
                        ("X3Plots", ResultAdsXGainsItemDto.NegativeX3Plots.ToArray()), ($"X3={ResultAdsXGainsItemDto.NegativeX3P1}*V^2{GetXPositiveAndNegativeString(ResultAdsXGainsItemDto.NegativeX3P2)}*V{GetXPositiveAndNegativeString(ResultAdsXGainsItemDto.NegativeX3P3)}", ResultAdsXGainsItemDto.NegativeX3SmoothPlots.ToArray()),
                        ("X4Plots", ResultAdsXGainsItemDto.NegativeX4Plots.ToArray()), ($"X4={ResultAdsXGainsItemDto.NegativeX4P1}*V^2{GetXPositiveAndNegativeString(ResultAdsXGainsItemDto.NegativeX4P2)}*V{GetXPositiveAndNegativeString(ResultAdsXGainsItemDto.NegativeX4P3)}", ResultAdsXGainsItemDto.NegativeX4SmoothPlots.ToArray())
                    ], "X3X4Plots")
                }), HtmlLogUniqueId.LoggingHtml());
            }
            catch (Exception ex)
            {
                SetDefaultXValue(cancellationToken);
                DialogWindowProvider.ShowDialog("Calibrate HRP Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: {ex.Message}!"), HtmlLogUniqueId.LoggingHtml());
                result = false;
                return result;
            }

            return result;
        }).ConfigureAwait(false);
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(async () => { return await VerifyCaibrationAsync(ReviewReviewItemDto, cancellationToken); }).ConfigureAwait(false);
    }

    private async Task<bool> VerifyCaibrationAsync(AdsXGainsItemDto selectItemDto, CancellationToken cancellationToken)
    {
        SynchronizationContextProvider.Send(() => PositiveAdsXGainsHrpCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => NegativeAdsXGainsHrpCacheItemList.Clear());
        if (selectItemDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Please select a review item!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        var result = await VerifyAsync(true).ConfigureAwait(false) && await VerifyAsync(false).ConfigureAwait(false);
        if (!result)
        {
            SetDefaultXValue(cancellationToken);
            DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);
        }
        else
        {
            selectItemDto.IsVerified = result;
            if (Save(selectItemDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                selectItemDto.IsVerified = false;
                return false;
            }

            SetBestX1X2Values(selectItemDto, Cache.DefaultSpeedXValue, cancellationToken);
            if (!IsAutoCalibrate) DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);
        }

        return result;

        async Task<bool> VerifyAsync(bool isPositive)
        {
            var resultTemp = true;
            try
            {
                Cache.IsPositive = isPositive;
                var adsXGainsItemDto = selectItemDto.Clone();
                adsXGainsItemDto.IsPositive = Cache.IsPositive;

                // 下发与校准方向相反的，默认移动速度下的合适前馈，防止快速移动时不合适的前馈导致stage无法停稳就继续监测buffer带来的误差
                var negativeDto = selectItemDto.Clone();
                negativeDto.IsPositive = !isPositive;
                var x1 = GetXValue(negativeDto.GetX1P1(), negativeDto.GetX1P2(), negativeDto.GetX1P3(), Cache.DefaultSpeedXValue);
                var x2 = GetXValue(negativeDto.GetX2P1(), negativeDto.GetX2P2(), negativeDto.GetX2P3(), Cache.DefaultSpeedXValue);
                AdsViewModel.SetSensorXSpeedFeedForwardValue(negativeDto.IsPositive, (x1, x2));

                var index = 0;
                if (isPositive)
                    Logger.LogHtmlInformation("Positive", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        Threshold = Cache.VerifyThreshold,
                        StartPosition = Cache.GetStartPosition(),
                        EndPosition = Cache.GetEndPosition(),
                        selectItemDto.PositiveX1P1,
                        selectItemDto.PositiveX1P2,
                        selectItemDto.PositiveX1P3,
                        selectItemDto.PositiveX2P1,
                        selectItemDto.PositiveX2P2,
                        selectItemDto.PositiveX2P3
                    }), HtmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlInformation("Negative", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        Threshold = Cache.VerifyThreshold,
                        StartPosition = Cache.GetStartPosition(),
                        EndPosition = Cache.GetEndPosition(),
                        selectItemDto.NegativeX3P1,
                        selectItemDto.NegativeX3P2,
                        selectItemDto.NegativeX3P3,
                        selectItemDto.NegativeX4P1,
                        selectItemDto.NegativeX4P2,
                        selectItemDto.NegativeX4P3
                    }), HtmlLogUniqueId.LoggingHtml());
                var verifySpeedValueList = Cache.SpeedXValueList.Zip(Cache.SpeedXValueList.Skip(1), (t1, t2) => (t1 + t2) / 2).ToList();
                foreach (var speedvalueItem in verifySpeedValueList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    index++;
                    var adsXGainsHrpCacheItem = new AdsXGainsCacheItem
                    {
                        Index = index,
                        IsPositive = Cache.IsPositive,
                        SpeedXValue = speedvalueItem
                    };
                    adsXGainsHrpCacheItem.SetX1(GetXValue(adsXGainsItemDto.GetX1P1(), adsXGainsItemDto.GetX1P2(), adsXGainsItemDto.GetX1P3(), speedvalueItem));
                    adsXGainsHrpCacheItem.SetX2(GetXValue(adsXGainsItemDto.GetX2P1(), adsXGainsItemDto.GetX2P2(), adsXGainsItemDto.GetX2P3(), speedvalueItem));
                    (resultTemp, var transBuffer) = await GetHrpAsync(adsXGainsHrpCacheItem, cancellationToken).ConfigureAwait(false);
                    if (adsXGainsItemDto.IsPositive) SynchronizationContextProvider.Send(() => PositiveAdsXGainsHrpCacheItemList.Add(adsXGainsHrpCacheItem));
                    else SynchronizationContextProvider.Send(() => NegativeAdsXGainsHrpCacheItemList.Add(adsXGainsHrpCacheItem));
                    if (resultTemp == false) break;
                }
            }
            catch (Exception ex)
            {
                SetDefaultXValue(cancellationToken);
                DialogWindowProvider.ShowDialog("Verify Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: {ex.Message}!"), HtmlLogUniqueId.LoggingHtml());
                resultTemp = false;
                return resultTemp;
            }

            return resultTemp;
        }
    }

    private void SetDefaultXValue(CancellationToken cancellationToken)
    {
        StageViewModel.SetXSpeedValue(Cache.DefaultSpeedXValue);
        InvokeAdsService(() => AdsViewModel.SetSensorXSpeedFeedForwardValue(false, defaultXList[1]), cancellationToken);
        InvokeAdsService(() => AdsViewModel.SetSensorXSpeedFeedForwardValue(true, defaultXList[0]), cancellationToken);
    }

    private void SetBestX1X2Values(AdsXGainsItemDto adsXGainsItemDto, double speedValue, CancellationToken cancellationToken)
    {
        StageViewModel.SetXSpeedValue(speedValue);
        double x1 = 0d, x2 = 0d, x3 = 0d, x4 = 0d;
        if (adsXGainsItemDto.PositiveX1P1 != 0) x1 = GetXValue(adsXGainsItemDto.PositiveX1P1, adsXGainsItemDto.PositiveX1P2, adsXGainsItemDto.PositiveX1P3, speedValue);
        if (adsXGainsItemDto.PositiveX2P1 != 0) x2 = GetXValue(adsXGainsItemDto.PositiveX2P1, adsXGainsItemDto.PositiveX2P2, adsXGainsItemDto.PositiveX2P3, speedValue);
        if (adsXGainsItemDto.NegativeX3P1 != 0) x3 = GetXValue(adsXGainsItemDto.NegativeX3P1, adsXGainsItemDto.NegativeX3P2, adsXGainsItemDto.NegativeX3P3, speedValue);
        if (adsXGainsItemDto.NegativeX4P1 != 0) x4 = GetXValue(adsXGainsItemDto.NegativeX4P1, adsXGainsItemDto.NegativeX4P2, adsXGainsItemDto.NegativeX4P3, speedValue);
        if (x1 != 0 && x2 != 0)
        {
            InvokeAdsService(() => AdsViewModel.SetSensorXSpeedFeedForwardValue(true, (x1, x2)), cancellationToken);
        }

        if (x3 != 0 && x4 != 0)
        {
            InvokeAdsService(() => AdsViewModel.SetSensorXSpeedFeedForwardValue(false, (x3, x4)), cancellationToken);
        }
    }

    private static double GetXValue(double p1, double p2, double p3, double speed)
    {
        return Math.Round(p1 * speed * speed + p2 * speed + p3);
    }

    private string GetXPositiveAndNegativeString(double p)
    {
        if (p > 0) return " + " + p.ToString();
        else return " - " + Math.Abs(p).ToString();
    }

    private async Task<(bool, List<List<double>>)> GetZ1Z2CurveAsync(AdsXGainsCacheItem adsXGainsCacheItem, CancellationToken cancellationToken, int repeatCount = 1)
    {
        var transBuffer = new List<List<double>>();
        try
        {
            StageViewModel.SetXSpeedValue(Cache.DefaultSpeedXValue);
            if (adsXGainsCacheItem.IsPositive)
            {
                StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetStartPosition());
                InvokeAdsService(() => AdsViewModel.SetSensorXSpeedFeedForwardValue(adsXGainsCacheItem.IsPositive, (adsXGainsCacheItem.GetX1(), adsXGainsCacheItem.GetX2())), cancellationToken);
                StageViewModel.SetXSpeedValue(adsXGainsCacheItem.SpeedXValue);
                var (a1, a2) = AdsViewModel.GetSensorXSpeedFeedForwardValue(adsXGainsCacheItem.IsPositive);

                await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 30000, cancellationToken);
                var task = Task.Run(() => AdsViewModel.GetSensorSpeedZ1Z2Z3TraceBufferList(TimeSpan.FromSeconds(Cache.WaitTime)));
                await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 3000, cancellationToken);
                StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetEndPosition());
                transBuffer = await task.ConfigureAwait(false);

                (a1, a2) = AdsViewModel.GetSensorXSpeedFeedForwardValue(adsXGainsCacheItem.IsPositive);
                if ((a1 != adsXGainsCacheItem.GetX1()) || (a2 != adsXGainsCacheItem.GetX2()))
                    Logger.LogError("{@Name} Error: X1 X2 Can Not The Same End!", Name);
            }
            else
            {
                StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetEndPosition());
                InvokeAdsService(() => AdsViewModel.SetSensorXSpeedFeedForwardValue(adsXGainsCacheItem.IsPositive, (adsXGainsCacheItem.GetX1(), adsXGainsCacheItem.GetX2())), cancellationToken);
                StageViewModel.SetXSpeedValue(adsXGainsCacheItem.SpeedXValue);
                var (a1, a2) = AdsViewModel.GetSensorXSpeedFeedForwardValue(adsXGainsCacheItem.IsPositive);

                await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 30000, cancellationToken);
                var task = Task.Run(() => AdsViewModel.GetSensorSpeedZ1Z2Z3TraceBufferList(TimeSpan.FromSeconds(Cache.WaitTime)));
                await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 3000, cancellationToken);
                StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetStartPosition());
                transBuffer = await task.ConfigureAwait(false);

                (a1, a2) = AdsViewModel.GetSensorXSpeedFeedForwardValue(adsXGainsCacheItem.IsPositive);
                if ((a1 != adsXGainsCacheItem.GetX1()) || (a2 != adsXGainsCacheItem.GetX2()))
                    Logger.LogError("{@Name} Error: X1 X2 Can Not The Same End!", Name);
            }

            if (repeatCount > 5) return (false, transBuffer);
            if (transBuffer.Count <= 0) return await GetZ1Z2CurveAsync(adsXGainsCacheItem, cancellationToken, repeatCount++).ConfigureAwait(false);

            var dataIsError = HasConsecutiveZeros(transBuffer[0], 50) &&
                              HasConsecutiveZeros(transBuffer[1], 50) &&
                              HasConsecutiveZeros(transBuffer[3], 50) &&
                              HasConsecutiveZeros(transBuffer[4], 50) &&
                              HasConsecutiveZeros(transBuffer[5], 50);
            if (dataIsError) return await GetZ1Z2CurveAsync(adsXGainsCacheItem, cancellationToken, repeatCount++).ConfigureAwait(false);

            var XSpeedList = transBuffer[6];
            var speedChangedList = XSpeedList.ToPoints().Where(t => Math.Round(Math.Abs(t.Y) / adsXGainsCacheItem.SpeedXValue, 2) > 0.5);
            var xSpeedStartIndex = Convert.ToInt32(speedChangedList.First().X);
            var xSpeedEndIndex = Convert.ToInt32(speedChangedList.Last().X);

            return (true, transBuffer);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException) throw;
            if (repeatCount > 5) return (false, transBuffer);

            Logger.LogError(ex, "GetZ1Z2CurveAsync Error!");
            return await GetZ1Z2CurveAsync(adsXGainsCacheItem, cancellationToken, repeatCount++).ConfigureAwait(false);
        }

        bool HasConsecutiveZeros(IEnumerable<double> array, int requiredZeros = 10)
        {
            return array
                .SkipWhile(x => x != 0) // 跳过非零部分
                .TakeWhile(x => x == 0) // 取连续的零
                .Count() >= requiredZeros; // 判断数量
        }
    }

    private async Task<(bool, List<(double Height, double Roll, double Pitch, double xSpeed, double ySpeed)>)> GetHrpAsync(AdsXGainsCacheItem adsXGainsHrpCacheItem, CancellationToken cancellationToken, int repeatCount = 1)
    {
        try
        {
            StageViewModel.SetXSpeedValue(Cache.DefaultSpeedXValue);
            StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetStartPosition());
            InvokeAdsService(() => AdsViewModel.SetSensorXSpeedFeedForwardValue(Cache.IsPositive, (adsXGainsHrpCacheItem.GetX1(), adsXGainsHrpCacheItem.GetX2())), cancellationToken);
            StageViewModel.SetXSpeedValue(adsXGainsHrpCacheItem.SpeedXValue);
            StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetStartPosition());
            await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 30000, cancellationToken);
            var task = Task.Run(() => AdsViewModel.GetSensorHeightRollPitchTraceBufferList(TimeSpan.FromSeconds(Cache.WaitTime)));
            await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 3000, cancellationToken);
            StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetEndPosition());
            var transBuffer = await task.ConfigureAwait(false);
            StageViewModel.SetXSpeedValue(Cache.SpeedXValueList.First());
            if (transBuffer.Count > 0)
            {
                var XSpeedList = transBuffer.Select(t => t.xSpeed).ToList();
                if (XSpeedList.Count == 0 && XSpeedList is null) return (false, transBuffer);
                var speedChangedList = XSpeedList.ToPoints().Where(t => Math.Round(Math.Abs(t.Y) / adsXGainsHrpCacheItem.SpeedXValue, 2) > 0.5);
                var xSpeedStartIndex = Convert.ToInt32(speedChangedList.First().X);
                var xSpeedEndIndex = Convert.ToInt32(speedChangedList.Last().X);

                var heightList = transBuffer.Select(t => t.Height).Take(xSpeedEndIndex).ToList();
                var rollList = transBuffer.Select(t => t.Roll).Take(xSpeedEndIndex).ToList();
                var pitchList = transBuffer.Select(t => t.Pitch).Take(xSpeedEndIndex).ToList();
                var heightMax = heightList.Max(Math.Abs);
                var rollMax = rollList.Max(Math.Abs);
                var pitchMax = pitchList.Max(Math.Abs);
                var result = heightMax < Cache.VerifyThreshold && rollMax < Cache.VerifyThreshold && pitchMax < Cache.VerifyThreshold;
                var (_, _, _) = AdsViewModel.GetSensorSpeedZ1Z2Z3Value();

                adsXGainsHrpCacheItem.SetH(heightMax);
                adsXGainsHrpCacheItem.SetR(rollMax);
                adsXGainsHrpCacheItem.SetP(pitchMax);
                adsXGainsHrpCacheItem.SetPlotH(heightList);
                adsXGainsHrpCacheItem.SetPlotR(rollList);
                adsXGainsHrpCacheItem.SetPlotP(pitchList);
                adsXGainsHrpCacheItem.SumHRP = heightMax + rollMax + pitchMax;

                if (adsXGainsHrpCacheItem.IsPositive)
                {
                    Logger.LogHtmlInformation(result ? $"V_{adsXGainsHrpCacheItem.SpeedXValue} X1_{adsXGainsHrpCacheItem.GetX1()} X2_{adsXGainsHrpCacheItem.GetX2()} OK" : $"V_{adsXGainsHrpCacheItem.SpeedXValue} X1_{adsXGainsHrpCacheItem.GetX1()} X2_{adsXGainsHrpCacheItem.GetX2()} Failed",
                        HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            adsXGainsHrpCacheItem.SpeedXValue,
                            X1 = adsXGainsHrpCacheItem.GetX1(),
                            X2 = adsXGainsHrpCacheItem.GetX2(),
                            HeightMax = heightMax,
                            RollMax = rollMax,
                            PitchMax = pitchMax,
                            PlotHrp = new HtmlPlot2DLinesChart([
                                ("H", transBuffer.Select(t => t.Height).ToList().ToPoints()),
                                ("P", transBuffer.Select(t => t.Pitch).ToList().ToPoints()),
                                ("R", transBuffer.Select(t => t.Roll).ToList().ToPoints()),
                                ("S", transBuffer.Select(t => t.xSpeed).ToList().ToPoints())
                            ], "PlotHrp")
                        }), HtmlLogUniqueId.LoggingHtml());
                }
                else
                {
                    Logger.LogHtmlInformation(result ? $"V_{adsXGainsHrpCacheItem.SpeedXValue} X3_{adsXGainsHrpCacheItem.GetX1()} X4_{adsXGainsHrpCacheItem.GetX2()} OK" : $"V_{adsXGainsHrpCacheItem.SpeedXValue} X3_{adsXGainsHrpCacheItem.GetX1()} X4_{adsXGainsHrpCacheItem.GetX2()} Failed",
                        HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            adsXGainsHrpCacheItem.SpeedXValue,
                            X3 = adsXGainsHrpCacheItem.GetX1(),
                            X4 = adsXGainsHrpCacheItem.GetX2(),
                            HeightMax = heightMax,
                            RollMax = rollMax,
                            PitchMax = pitchMax,
                            PlotHrp = new HtmlPlot2DLinesChart([
                                ("H", transBuffer.Select(t => t.Height).ToList().ToPoints()),
                                ("P", transBuffer.Select(t => t.Pitch).ToList().ToPoints()),
                                ("R", transBuffer.Select(t => t.Roll).ToList().ToPoints()),
                                ("S", transBuffer.Select(t => t.xSpeed).ToList().ToPoints())
                            ], "PlotHrp")
                        }), HtmlLogUniqueId.LoggingHtml());
                }

                return (result, transBuffer);
            }

            if (repeatCount > 5) return (false, transBuffer);

            return await GetHrpAsync(adsXGainsHrpCacheItem, cancellationToken, repeatCount++).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException) throw;
            if (repeatCount > 5) return (false, new List<(double Height, double Roll, double Pitch, double xSpeed, double ySpeed)>());
            return await GetHrpAsync(adsXGainsHrpCacheItem, cancellationToken, repeatCount++).ConfigureAwait(false);
        }
    }

    private async Task<(int x1, int x2, double Z1, double Z2, bool z1IsPositive, bool z2IsPositive)> DichotomyFindX1X2Async(bool isPositive, double speedValue, int index, int minX1, int maxX1, int minX2, int maxX2, CancellationToken cancellationToken)
    {
        var z1IsPositive = false;
        var z2IsPositive = false;
        var x1Value = (minX1 + maxX1) / 2;
        var x2Value = (minX2 + maxX2) / 2;
        var adsXGainsCacheItemTemp = new AdsXGainsCacheItem
        {
            Index = index,
            IsPositive = isPositive,
            SpeedXValue = speedValue
        };
        adsXGainsCacheItemTemp.SetX1(x1Value);
        adsXGainsCacheItemTemp.SetX2(x2Value);
        var (isSuccess, transBuffer) = await GetZ1Z2CurveAsync(adsXGainsCacheItemTemp, cancellationToken).ConfigureAwait(false);
        if (isSuccess == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Get Hrp Failed!"), HtmlLogUniqueId.LoggingHtml());
            return (0, 0, 0, 0, false, false);
        }

        FindAdsXGainZ1Z2(adsXGainsCacheItemTemp, transBuffer);
        if (adsXGainsCacheItemTemp.IsPositive == Cache.IsPositive)
            SynchronizationContextProvider.Send(() => AdsXGainsCacheItemList.Add(adsXGainsCacheItemTemp));
        else
            SynchronizationContextProvider.Send(() => AdsXGainsCacheConverseItemList.Add(adsXGainsCacheItemTemp));
        z1IsPositive = adsXGainsCacheItemTemp.GetZ1() <= 0;
        z2IsPositive = adsXGainsCacheItemTemp.GetZ2() <= 0;
        return (x1Value, x2Value, adsXGainsCacheItemTemp.GetZ1(), adsXGainsCacheItemTemp.GetZ2(), z1IsPositive, z2IsPositive);
    }

    private void FindAdsXGainZ1Z2(AdsXGainsCacheItem adsXGainsCacheItem, List<List<double>> transBuffer)
    {
        var XSpeedList = transBuffer[6];
        if (XSpeedList.Count == 0 && transBuffer is null) return;
        var speedChangedList = XSpeedList.ToPoints().Where(t => Math.Round(Math.Abs(t.Y) / adsXGainsCacheItem.SpeedXValue, 2) > 0.5);
        var xSpeedStartIndex = Convert.ToInt32(speedChangedList.First().X);
        var xSpeedEndIndex = Convert.ToInt32(speedChangedList.Last().X);

        adsXGainsCacheItem.SetPlotZ1(transBuffer[0]);
        adsXGainsCacheItem.SetPlotZ2(transBuffer[1]);

        var (pointZ1, pointSmoothZ1, smoothZ1) = GetadsXGainsValue(adsXGainsCacheItem.GetPlotZ1(), xSpeedStartIndex, xSpeedEndIndex);
        adsXGainsCacheItem.SetSmoothPlotZ1(smoothZ1);
        adsXGainsCacheItem.SetMaxZ1(pointZ1.Max(t => t.Y));
        adsXGainsCacheItem.SetMinZ1(pointZ1.Min(t => t.Y));
        adsXGainsCacheItem.SetPointZ1(pointZ1);
        adsXGainsCacheItem.SetSmoothPointZ1(pointSmoothZ1);
        adsXGainsCacheItem.SetZ1(pointZ1[1].Y - pointZ1[0].Y);
        var (pointZ2, pointSmoothZ2, smoothZ2) = GetadsXGainsValue(adsXGainsCacheItem.GetPlotZ2(), xSpeedStartIndex, xSpeedEndIndex);
        adsXGainsCacheItem.SetSmoothPlotZ2(smoothZ2);
        adsXGainsCacheItem.SetMaxZ2(pointZ2.Max(t => t.Y));
        adsXGainsCacheItem.SetMinZ2(pointZ2.Min(t => t.Y));
        adsXGainsCacheItem.SetPointZ2(pointZ2);
        adsXGainsCacheItem.SetSmoothPointZ2(pointSmoothZ2);
        adsXGainsCacheItem.SetZ2(pointZ2[1].Y - pointZ2[0].Y);
        var heightList = transBuffer[3].Skip(xSpeedStartIndex).Take(xSpeedEndIndex - xSpeedStartIndex).ToList();
        var rollList = transBuffer[4].Skip(xSpeedStartIndex).Take(xSpeedEndIndex - xSpeedStartIndex).ToList();
        var pitchList = transBuffer[5].Skip(xSpeedStartIndex).Take(xSpeedEndIndex - xSpeedStartIndex).ToList();
        var heightMax = heightList.Max(Math.Abs);
        var rollMax = rollList.Max(Math.Abs);
        var pitchMax = pitchList.Max(Math.Abs);
        adsXGainsCacheItem.SetH(heightMax);
        adsXGainsCacheItem.SetR(rollMax);
        adsXGainsCacheItem.SetP(pitchMax);
        adsXGainsCacheItem.SetPlotH(heightList);
        adsXGainsCacheItem.SetPlotR(rollList);
        adsXGainsCacheItem.SetPlotP(pitchList);
        adsXGainsCacheItem.SumHRP = heightMax + rollMax + pitchMax;
        if (adsXGainsCacheItem.IsPositive)
        {
            Logger.LogHtmlInformation($"X1_{adsXGainsCacheItem.GetX1()} X2_{adsXGainsCacheItem.GetX2()}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                FeedForwardValueX1 = adsXGainsCacheItem.GetX1(),
                FeedForwardValueX2 = adsXGainsCacheItem.GetX2(),
                MaxValueZ1 = adsXGainsCacheItem.GetMaxZ1(),
                MinValueZ1 = adsXGainsCacheItem.GetMinZ1(),
                Z1 = adsXGainsCacheItem.GetZ1(),
                MaxValueZ2 = adsXGainsCacheItem.GetMaxZ2(),
                MinValueZ2 = adsXGainsCacheItem.GetMinZ2(),
                Z2 = adsXGainsCacheItem.GetZ2(),
                HeightMax = heightMax,
                RollMax = rollMax,
                PitchMax = pitchMax,
                xSpeedStartIndex,
                xSpeedEndIndex,
                PlotZ1Z2 = new HtmlPlot2DLinesChart([
                    ("Z1", transBuffer[0].ToPoints()), ("smoothZ1", adsXGainsCacheItem.GetSmoothPlotZ1().ToPoints()),
                    ("Z2", transBuffer[1].ToPoints()), ("smoothZ2", adsXGainsCacheItem.GetSmoothPlotZ2().ToPoints()),
                    ("X Speed", XSpeedList.ToPoints())
                ], "PlotZ1Z2"),
                PlotHRP = new HtmlPlot2DLinesChart([("H", heightList.ToPoints()), ("R", rollList.ToPoints()), ("P", pitchList.ToPoints())], "PlotHRP")
            }), HtmlLogUniqueId.LoggingHtml());
        }
        else
        {
            Logger.LogHtmlInformation($"X3_{adsXGainsCacheItem.GetX1()} X4_{adsXGainsCacheItem.GetX2()}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                FeedForwardValueX1 = adsXGainsCacheItem.GetX1(),
                FeedForwardValueX2 = adsXGainsCacheItem.GetX2(),
                MaxValueZ3 = adsXGainsCacheItem.GetMaxZ1(),
                MinValueZ3 = adsXGainsCacheItem.GetMinZ1(),
                Z3 = adsXGainsCacheItem.GetZ1(),
                MaxValueZ4 = adsXGainsCacheItem.GetMaxZ2(),
                MinValueZ4 = adsXGainsCacheItem.GetMinZ2(),
                Z4 = adsXGainsCacheItem.GetZ2(),
                HeightMax = heightMax,
                RollMax = rollMax,
                PitchMax = pitchMax,
                xSpeedStartIndex,
                xSpeedEndIndex,
                PlotZ3Z4 = new HtmlPlot2DLinesChart([
                    ("Z3", transBuffer[0].ToPoints()), ("smoothZ3", adsXGainsCacheItem.GetSmoothPlotZ1().ToPoints()),
                    ("Z4", transBuffer[1].ToPoints()), ("smoothZ4", adsXGainsCacheItem.GetSmoothPlotZ2().ToPoints()),
                    ("X Speed", XSpeedList.ToPoints())
                ], "PlotZ3Z4"),
                PlotHRP = new HtmlPlot2DLinesChart([("H", heightList.ToPoints()), ("R", rollList.ToPoints()), ("P", pitchList.ToPoints())], "PlotHRP")
            }), HtmlLogUniqueId.LoggingHtml());
        }
    }

    private static (List<Point> pointZ, List<Point> pointSmoothZ, List<double> smoothZ) GetadsXGainsValue(List<double> PonitZ, int startIndex, int endIndex)
    {
        var sgolayfiltListZ = MovMeanFilter.Smooth(501, MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(PonitZ));

        var x = Vector<double>.Build.DenseOfEnumerable(Enumerable.Range(1, sgolayfiltListZ.Count).Select(x => (double)x));

        List<double> smoothZ = [.. sgolayfiltListZ];
        smoothZ = smoothZ.Take(endIndex).ToList();

        var pointCount = startIndex;

        var areaThreshold = PonitZ.Take(pointCount).Average();

        var kkValue = smoothZ.Skip(pointCount).Average();

        var areaZ = kkValue - areaThreshold;

        double maxValue, minValue = 0;
        int maxIndex, minIndex = 0;
        var pointZ = new List<Point>();
        var pointSmoothZ = new List<Point>();
        if (areaZ > 0)
        {
            //开口向下
            maxValue = smoothZ.Skip(pointCount).Max();
            maxIndex = smoothZ.Skip(pointCount).ToList().IndexOf(maxValue);
            pointSmoothZ.Add(new Point(pointCount + maxIndex, maxValue));
            pointZ.Add(new Point(pointCount + maxIndex, PonitZ[pointCount + maxIndex]));
            minValue = smoothZ.Take(pointCount).Min();
            minIndex = smoothZ.Take(pointCount).ToList().IndexOf(minValue);
            pointSmoothZ.Add(new Point(minIndex, minValue));
            pointZ.Add(new Point(minIndex, PonitZ[minIndex]));
        }
        else
        {
            //开口向上
            minValue = smoothZ.Skip(pointCount).Min();
            minIndex = smoothZ.Skip(pointCount).ToList().IndexOf(minValue);
            pointSmoothZ.Add(new Point(pointCount + minIndex, minValue));
            pointZ.Add(new Point(pointCount + minIndex, PonitZ[pointCount + minIndex]));
            maxValue = smoothZ.Take(pointCount).Max();
            maxIndex = smoothZ.Take(pointCount).ToList().IndexOf(maxValue);
            pointSmoothZ.Add(new Point(maxIndex, maxValue));
            pointZ.Add(new Point(maxIndex, PonitZ[maxIndex]));
        }

        return (pointZ, pointSmoothZ, smoothZ);
    }

    public void InvokeAdsService(Action action, CancellationToken cancellationToken)
    {
        for (var i = 0; i < 5; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                action.Invoke();
                return;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "InvokeSetValue failed, retrying {RetryCount} times", i + 1);
            }
        }

        throw new CugaException($"Ads Service Invoke Error! {nameof(action.Method.Name)}");
    }

    private bool Save(AdsXGainsItemDto itemDto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibration = itemDto.Clone();

        CacheProvider.Set(Calibration, cancellationToken);
        CacheProvider.Set(Cache, cancellationToken);
    });

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(() => AdsXGainsCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsXGainsCacheConverseItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsXGainsCacheBestItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsXGainsCacheConverseBestItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsXGainsDichotomySpeedCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsXGainsDichotomySpeedCacheConverseItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsXGainsHrpCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsXGainsHrpCacheConverseItemList.Clear());
        SynchronizationContextProvider.Send(() => ResultAdsXGainsHrpCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => ResultAdsXGainsHrpCacheConverseItemList.Clear());
    }

    private async Task<(bool, List<(double Height, double Roll, double Pitch, double xSpeed, double ySpeed)>)> GetHrpNewAsync(AdsXGainsCacheItem adsXGainsHrpCacheItem, CancellationToken cancellationToken, int repeatCount = 1)
    {
        var transBuffer = new List<(double Height, double Roll, double Pitch, double xSpeed, double ySpeed)>();
        try
        {
            StageViewModel.SetXSpeedValue(Cache.DefaultSpeedXValue);
            if (adsXGainsHrpCacheItem.IsPositive)
            {
                StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetStartPosition());
                InvokeAdsService(() => AdsViewModel.SetSensorXSpeedFeedForwardValue(adsXGainsHrpCacheItem.IsPositive, (adsXGainsHrpCacheItem.GetX1(), adsXGainsHrpCacheItem.GetX2())), cancellationToken);
                StageViewModel.SetXSpeedValue(adsXGainsHrpCacheItem.SpeedXValue);

                await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 30000, cancellationToken);
                var task = Task.Run(() => AdsViewModel.GetSensorHeightRollPitchTraceBufferList(TimeSpan.FromSeconds(Cache.WaitTime)));
                await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 3000, cancellationToken);
                StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetEndPosition());
                transBuffer = await task.ConfigureAwait(false);
                StageViewModel.SetXSpeedValue(Cache.SpeedXValueList.First());
            }
            else
            {
                StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetEndPosition());
                InvokeAdsService(() => AdsViewModel.SetSensorXSpeedFeedForwardValue(adsXGainsHrpCacheItem.IsPositive, (adsXGainsHrpCacheItem.GetX1(), adsXGainsHrpCacheItem.GetX2())), cancellationToken);
                StageViewModel.SetXSpeedValue(adsXGainsHrpCacheItem.SpeedXValue);

                await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 30000, cancellationToken);
                var task = Task.Run(() => AdsViewModel.GetSensorHeightRollPitchTraceBufferList(TimeSpan.FromSeconds(Cache.WaitTime)));
                await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 3000, cancellationToken);
                StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetStartPosition());
                transBuffer = await task.ConfigureAwait(false);
                StageViewModel.SetXSpeedValue(Cache.SpeedXValueList.First());
            }

            if (transBuffer.Count > 0)
            {
                var XSpeedList = transBuffer.Select(t => t.xSpeed).ToList();
                if (XSpeedList.Count == 0 && XSpeedList is null)
                    return (false, transBuffer);
                var speedChangedList = XSpeedList.ToPoints().Where(t => Math.Round(Math.Abs(t.Y) / adsXGainsHrpCacheItem.SpeedXValue, 2) > 0.5);
                var xSpeedStartIndex = Convert.ToInt32(speedChangedList.First().X);
                var xSpeedEndIndex = Convert.ToInt32(speedChangedList.Last().X);

                var heightList = transBuffer.Select(t => t.Height).Take(xSpeedEndIndex).ToList();
                var rollList = transBuffer.Select(t => t.Roll).Take(xSpeedEndIndex).ToList();
                var pitchList = transBuffer.Select(t => t.Pitch).Take(xSpeedEndIndex).ToList();
                var heightMax = heightList.Max(Math.Abs);
                var rollMax = rollList.Max(Math.Abs);
                var pitchMax = pitchList.Max(Math.Abs);
                var result = heightMax < Cache.VerifyThreshold && rollMax < Cache.VerifyThreshold && pitchMax < Cache.VerifyThreshold;
                var (_, _, _) = AdsViewModel.GetSensorSpeedZ1Z2Z3Value();

                adsXGainsHrpCacheItem.SetH(heightMax);
                adsXGainsHrpCacheItem.SetR(rollMax);
                adsXGainsHrpCacheItem.SetP(pitchMax);
                adsXGainsHrpCacheItem.SetPlotH(heightList);
                adsXGainsHrpCacheItem.SetPlotR(rollList);
                adsXGainsHrpCacheItem.SetPlotP(pitchList);
                adsXGainsHrpCacheItem.SumHRP = heightMax + rollMax + pitchMax;

                if (adsXGainsHrpCacheItem.IsPositive)
                {
                    Logger.LogHtmlInformation(result ? $"V_{adsXGainsHrpCacheItem.SpeedXValue} X1_{adsXGainsHrpCacheItem.GetX1()} X2_{adsXGainsHrpCacheItem.GetX2()} OK" : $"V_{adsXGainsHrpCacheItem.SpeedXValue} X1_{adsXGainsHrpCacheItem.GetX1()} X2_{adsXGainsHrpCacheItem.GetX2()} Failed",
                        HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            adsXGainsHrpCacheItem.SpeedXValue,
                            X1 = adsXGainsHrpCacheItem.GetX1(),
                            X2 = adsXGainsHrpCacheItem.GetX2(),
                            HeightMax = heightMax,
                            RollMax = rollMax,
                            PitchMax = pitchMax,
                            PlotHrp = new HtmlPlot2DLinesChart([
                                ("H", transBuffer.Select(t => t.Height).ToList().ToPoints()),
                                ("P", transBuffer.Select(t => t.Pitch).ToList().ToPoints()),
                                ("R", transBuffer.Select(t => t.Roll).ToList().ToPoints()),
                                ("S", transBuffer.Select(t => t.xSpeed).ToList().ToPoints())
                            ], "PlotHrp")
                        }), HtmlLogUniqueId.LoggingHtml());
                }
                else
                {
                    Logger.LogHtmlInformation(result ? $"V_{adsXGainsHrpCacheItem.SpeedXValue} X3_{adsXGainsHrpCacheItem.GetX1()} X4_{adsXGainsHrpCacheItem.GetX2()} OK" : $"V_{adsXGainsHrpCacheItem.SpeedXValue} X3_{adsXGainsHrpCacheItem.GetX1()} X4_{adsXGainsHrpCacheItem.GetX2()} Failed",
                        HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            adsXGainsHrpCacheItem.SpeedXValue,
                            X3 = adsXGainsHrpCacheItem.GetX1(),
                            X4 = adsXGainsHrpCacheItem.GetX2(),
                            HeightMax = heightMax,
                            RollMax = rollMax,
                            PitchMax = pitchMax,
                            PlotHrp = new HtmlPlot2DLinesChart([
                                ("H", transBuffer.Select(t => t.Height).ToList().ToPoints()),
                                ("P", transBuffer.Select(t => t.Pitch).ToList().ToPoints()),
                                ("R", transBuffer.Select(t => t.Roll).ToList().ToPoints()),
                                ("S", transBuffer.Select(t => t.xSpeed).ToList().ToPoints())
                            ], "PlotHrp_Converse")
                        }), HtmlLogUniqueId.LoggingHtml());
                }

                return (true, transBuffer);
            }

            if (repeatCount > 5) return (false, transBuffer);

            return await GetHrpNewAsync(adsXGainsHrpCacheItem, cancellationToken, repeatCount++).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException) throw;
            if (repeatCount > 5) return (false, new List<(double Height, double Roll, double Pitch, double xSpeed, double ySpeed)>());
            return await GetHrpNewAsync(adsXGainsHrpCacheItem, cancellationToken, repeatCount++).ConfigureAwait(false);
        }
    }

    #endregion 校准

    #region 自动化校准

    public override void GetAutoCalibrationStep()
    {
        AutoCalibrationStepList =
        [
            new() { StepName = "Loading" },
            new() { StepName = "X Positive And Negative Gains" },
            new() { StepName = "X Positive And Negative HPR" },
            new() { StepName = "Review" }
        ];
    }

    public override async Task<bool> AutomationActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            GetAutoCalibrationStep();
            await base.AutomationActionAsync(cancellationToken);
            var result = true;
            foreach (var stepItem in AutoCalibrationStepList.Select((t, index) => (t, index)))
            {
                switch (stepItem.index)
                {
                    case 0:
                        if (await LoadedingAsync(cancellationToken) == false) return false;
                        if (await Step0CalibrateActionAsync(cancellationToken) == false) return false;
                        if (await NextingAsync(cancellationToken) == false) return false;
                        if (await AutoNextingAsync(cancellationToken) == false) return false;
                        break;

                    case 1:
                        if (await Step1CalibrateActionAsync(cancellationToken) == false) return false;
                        if (await AutoNextingAsync(cancellationToken) == false) return false;
                        break;

                    case 2:
                        if (await Step2CalibrateActionAsync(cancellationToken) == false) return false;
                        if (await NextingAsync(cancellationToken) == false) return false;
                        if (await AutoNextingAsync(cancellationToken) == false) return false;
                        break;

                    case 3:
                        AutoReviewCalibrationStepIndex = AutoCalibrationStepList.Count - 1;
                        if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false) return false;
                        await InvokeCalibrateAsync(async () =>
                        {
                            if (await VerifyCaibrationAsync(ReviewReviewItemDto, cancellationToken) == false)
                            {
                                DialogWindowProvider.ShowDialog($"Auto Calibration Review  Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                result = false;
                                return result;
                            }

                            return result;
                        });
                        AutoCalibrationStepIndex++;
                        break;

                    default:
                        break;
                }

                AutoCalibrationProgress = AutoCalibrationStepIndex / (double)AutoCalibrationStepList.Count * 100;
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Auto Calibration Error!");
            return false;
        }
    }

    private async Task<bool> AutoNextingAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            CalibrationStepName = AutoCalibrationStepList[AutoCalibrationStepIndex + 1].StepName.ToString();
            AutoCalibrationStepIndex++;
            CalibrationStepIndex++;
        }, cancellationToken);
        return true;
    }

    public override async Task<bool> AutomationReviewActionAsync(CancellationToken cancellationToken)
    {
        GetAutoCalibrationStep();
        await base.AutomationReviewActionAsync(cancellationToken);
        if (await LoadedingAsync(cancellationToken) == false) return false;
        if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false)
        {
            DialogWindowProvider.ShowDialog($"Please Calibration!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var result = false;
        await InvokeVerifyAsync(async () =>
        {
            try
            {
                if (await VerifyCaibrationAsync(ReviewReviewItemDto!, cancellationToken) == false)
                {
                    DialogWindowProvider.ShowDialog($"Auto Calibration Review Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                result = true;
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Review Failed! Error massage:{ex.Message}"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
        });

        AutoCalibrationProgress = (AutoCalibrationStepIndex + 1) / (double)AutoCalibrationStepList.Count * 100;
        return result;
    }

    #endregion 自动化校准
}