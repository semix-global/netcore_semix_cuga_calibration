using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Exceptions;
using Core.Models.Models;
using Core.Models.Models.Ads.YGains;
using Core.Utilities.SourceGenerators.Attributes;
using Local.SQL.Cache.Providers.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Behaviors;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using System.Collections.ObjectModel;
using static Core.Models.Models.Ads.YGains.AdsYGainsCache;

namespace CugaCalibration.ViewModels.Ads;

[IOCAppService(ServiceType = typeof(AdsYGainsCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AdsYGainsCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select a location" },
        new() { StepName = "Y Positive And Negative Gains" },
        new() { StepName = "Y Positive And Negative HRP" }
    ];

    #region 界面相关

    #region Calibrate

    private AdsYGainsItemDto? _lastAdsYGainsItemDto;

    [ObservableProperty]
    private ObservableCollection<AdsYGainsItemDto> _yGainsItemDtoList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsCacheItem> _adsYGainsCacheItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsCacheItem> _adsYGainsCacheBestItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsCacheItem> _adsYGainsCacheConverseItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsCacheItem> _adsYGainsCacheConverseBestItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsDichotomySpeedCacheItem> _adsYGainsDichotomySpeedCacheItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsDichotomySpeedCacheItem> _adsYGainsDichotomySpeedCacheConverseItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsCacheItem> _adsYGainsHrpCacheItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsCacheItem> _adsYGainsHrpCacheConverseItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsCacheItem> _resultAdsYGainsHrpCacheItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsCacheItem> _resultAdsYGainsHrpCacheConverseItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsCacheItem> _yGainsCacheItemList = [];

    [ObservableProperty]
    private AdsYGainsItemDto? _selectAdsYGainsItemDto;

    [ObservableProperty]
    private AdsYGainsCacheItem _selectAdsYGainsCacheItem = new();

    [ObservableProperty]
    private AdsYGainsCacheItem _selectAdsYGainsCacheConverseItem = new();

    [ObservableProperty]
    private AdsYGainsItemDto _resultAdsYGainsItemDto = new();

    [ObservableProperty]
    private List<WpfPlotModel> _plotList = [];

    [ObservableProperty]
    private List<WpfPlotModel> _plotY1Y2Y3List = [];

    [ObservableProperty]
    private List<WpfPlotModel> _plotY4Y5Y6List = [];

    private bool IsY1Stop;
    private bool IsY4Stop;
    private bool IsY2Stop;
    private bool IsY5Stop;
    private bool IsY3Stop;
    private bool IsY6Stop;

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private AdsYGainsItemDto _reviewAdsYGainsItemDto = new();

    [ObservableProperty]
    private AdsYGainsItemDto? _selectReviewItemDto;

    [ObservableProperty]
    private ObservableCollection<AdsYGainsCacheItem> _positiveAdsYGainsHrpCacheItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsCacheItem> _negativeAdsYGainsHrpCacheItemList = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private AdsYGainsCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private AdsYGainsItemDto _calibration = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<AdsYGainsCache>();
        Calibration = CacheProvider.GetOrDefault<AdsYGainsItemDto>();
        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        GetDefaultYValue();
        return Task.FromResult(true);
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        SelectReviewItemDto = Calibration.Clone();

        return SelectReviewItemDto.IsCalibrated;
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
                SynchronizationContextProvider.Send(() => AdsYGainsCacheItemList.Clear());
                SynchronizationContextProvider.Send(() => AdsYGainsCacheBestItemList.Clear());
                SynchronizationContextProvider.Send(() => AdsYGainsCacheConverseItemList.Clear());
                SynchronizationContextProvider.Send(() => AdsYGainsCacheConverseBestItemList.Clear());
                SynchronizationContextProvider.Send(() => AdsYGainsDichotomySpeedCacheItemList.Clear());
                SynchronizationContextProvider.Send(() => AdsYGainsDichotomySpeedCacheConverseItemList.Clear());
                Cache.IsPositive = true;
                return true;

            case 1:
                return true;

            case 2:
                var isCalibrated = CalibrationStepIndex == 2;

                if (ResultAdsYGainsItemDto is null)
                {
                    DialogWindowProvider.TryShowDialog("Please find y gains!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    ResultAdsYGainsItemDto.IsCalibrated = isCalibrated;
                    if (Save(ResultAdsYGainsItemDto, cancellationToken) == false)
                    {
                        ResultAdsYGainsItemDto.IsCalibrated = false;
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
    private async Task ShowPlotHRPAsync(AdsYGainsCacheItem adsYGainsItemDto)
    {
        try
        {
            await Task.Run(() =>
            {
                var plotList = new List<(string Title, IReadOnlyList<Point> Points)>();
                if (adsYGainsItemDto.IsPositive)
                {
                    plotList =
                    [
                        ("H", adsYGainsItemDto.PositivePlotH.ToPoints()),
                        ("R", adsYGainsItemDto.PositivePlotR.ToPoints()),
                        ("P", adsYGainsItemDto.PositivePlotP.ToPoints())
                    ];
                }
                else
                {
                    plotList =
                    [
                        ("H_Converse", adsYGainsItemDto.NegativePlotH.ToPoints()),
                        ("R_Converse", adsYGainsItemDto.NegativePlotR.ToPoints()),
                        ("P_Converse", adsYGainsItemDto.NegativePlotP.ToPoints())
                    ];
                }

                DialogWindowProvider.ShowPlotAsReadonly(plotList);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get PlotHrpFailed", Name);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Y1,
                Cache.Y2,
                Cache.Y3,
                Cache.Y4,
                Cache.Y5,
                Cache.Y6,
                YPositiveStartPosition = Cache.PositiveStartPosition,
                YPositiveEndPosition = Cache.PositiveEndPosition,
                YNegativeStartPosition = Cache.NegativeStartPosition,
                YNegativeEndPosition = Cache.NegativeEndPosition
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
                foreach (var speedvalue in Cache.SpeedYValueList)
                {
                    var z1IsPositive = false;
                    var z2IsPositive = false;
                    var z3IsPositive = false;
                    var z4IsPositive = false;
                    var z5IsPositive = false;
                    var z6IsPositive = false;
                    IsY1Stop = false;
                    IsY2Stop = false;
                    IsY3Stop = false;
                    IsY4Stop = false;
                    IsY5Stop = false;
                    IsY6Stop = false;
                    {
                        var adsYGainsDichotomySpeedCacheItem = new AdsYGainsDichotomySpeedCacheItem
                        {
                            IsPositive = Cache.IsPositive,
                            SpeedYValue = speedvalue
                        };
                        SynchronizationContextProvider.Send(() => AdsYGainsDichotomySpeedCacheItemList.Add(adsYGainsDichotomySpeedCacheItem));
                        var adsYGainsCacheItem = new AdsYGainsCacheItem
                        {
                            Index = 1,
                            SpeedYValue = speedvalue,
                            IsPositive = Cache.IsPositive
                        };
                        adsYGainsCacheItem.SetAdsY1(Cache.FindMinY);
                        adsYGainsCacheItem.SetAdsY2(Cache.FindMinY);
                        adsYGainsCacheItem.SetAdsY3(Cache.FindMinY);
                        Logger.LogHtmlInformation($"Param_V{speedvalue}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                        {
                            Cache.IsPositive,
                            StartPosition = Cache.GetStartPosition(),
                            EndPosition = Cache.GetEndPosition(),
                            SpeedXValue = speedvalue
                        }), HtmlLogUniqueId.LoggingHtml());
                        var (isSuccess, transBuffer) = await GetZ1Z2Z3CurveAsync(adsYGainsCacheItem, cancellationToken).ConfigureAwait(false);
                        if (isSuccess == false)
                        {
                            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Get Z1Z2Z3 Failed!"), HtmlLogUniqueId.LoggingHtml());
                            break;
                        }

                        FindAdsYGainZ1Z2Z3(adsYGainsCacheItem, transBuffer);
                        SelectAdsYGainsCacheItem = adsYGainsCacheItem;
                        OnPropertyChanged(nameof(SelectAdsYGainsCacheItem));
                        SynchronizationContextProvider.Send(() => AdsYGainsCacheItemList.Add(adsYGainsCacheItem));
                        if (adsYGainsCacheItem.GetZ1() <= 0)
                        {
                            z1IsPositive = true;
                        }

                        if (adsYGainsCacheItem.GetZ2() <= 0)
                        {
                            z2IsPositive = true;
                        }

                        if (adsYGainsCacheItem.GetZ3() <= 0)
                        {
                            z3IsPositive = true;
                        }
                    }
                    {
                        var adsYGainsDichotomySpeedCacheItem = new AdsYGainsDichotomySpeedCacheItem
                        {
                            IsPositive = !Cache.IsPositive,
                            SpeedYValue = speedvalue
                        };
                        SynchronizationContextProvider.Send(() => AdsYGainsDichotomySpeedCacheConverseItemList.Add(adsYGainsDichotomySpeedCacheItem));
                        var adsYGainsCacheItem = new AdsYGainsCacheItem
                        {
                            Index = 1,
                            SpeedYValue = speedvalue,
                            IsPositive = !Cache.IsPositive
                        };
                        adsYGainsCacheItem.SetAdsY1(Cache.FindMinY);
                        adsYGainsCacheItem.SetAdsY2(Cache.FindMinY);
                        adsYGainsCacheItem.SetAdsY3(Cache.FindMinY);

                        var (isSuccess, transBuffer) = await GetZ1Z2Z3CurveAsync(adsYGainsCacheItem, cancellationToken).ConfigureAwait(false);
                        if (isSuccess == false)
                        {
                            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Get Z1Z2Z3 Failed!"), HtmlLogUniqueId.LoggingHtml());
                            break;
                        }

                        FindAdsYGainZ1Z2Z3(adsYGainsCacheItem, transBuffer);
                        SelectAdsYGainsCacheConverseItem = adsYGainsCacheItem;
                        OnPropertyChanged(nameof(SelectAdsYGainsCacheConverseItem));
                        SynchronizationContextProvider.Send(() => AdsYGainsCacheConverseItemList.Add(adsYGainsCacheItem));
                        if (adsYGainsCacheItem.GetZ1() <= 0)
                        {
                            z4IsPositive = true;
                        }

                        if (adsYGainsCacheItem.GetZ2() <= 0)
                        {
                            z5IsPositive = true;
                        }

                        if (adsYGainsCacheItem.GetZ3() <= 0)
                        {
                            z6IsPositive = true;
                        }
                    }

                    var y1Min = Cache.FindMinY;
                    var y4Min = Cache.FindMinY;
                    var y1Max = Cache.FindMaxY;
                    var y4Max = Cache.FindMaxY;
                    var y2Min = Cache.FindMinY;
                    var y5Min = Cache.FindMinY;
                    var y2Max = Cache.FindMaxY;
                    var y5Max = Cache.FindMaxY;
                    var y3Min = Cache.FindMinY;
                    var y6Min = Cache.FindMinY;
                    var y3Max = Cache.FindMaxY;
                    var y6Max = Cache.FindMaxY;
                    var Z1List = new List<(int y1, double z1)>();
                    var Z4List = new List<(int y1, double z1)>();
                    var Z2List = new List<(int y2, double z2)>();
                    var Z5List = new List<(int y2, double z2)>();
                    var Z3List = new List<(int y3, double z3)>();
                    var Z6List = new List<(int y3, double z3)>();
                    double Z10 = 0, Z20 = 0, Z30 = 0, Z40 = 0, Z50 = 0, Z60 = 0;
                    int N1 = 0, N2 = 0, N3 = 0, N4 = 0, N5 = 0, N6 = 0;
                    var isStop123 = true;
                    var isStop456 = true;
                    var index123 = 1;
                    var index456 = 1;
                    while (true)
                    {
                        if (isStop123)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            index123++;
                            if (index123 > 15)
                                isStop123 = false;
                            var resultValue = await DichotomyFindY1Y2Y3Async(Cache.IsPositive, speedvalue, index123, y1Min, y1Max, y2Min, y2Max, y3Min, y3Max, cancellationToken);
                            Z1List.Add((resultValue.y1, resultValue.Z1));
                            Z2List.Add((resultValue.y2, resultValue.Z2));
                            Z3List.Add((resultValue.y3, resultValue.Z3));
                            if (!IsY1Stop)
                            {
                                if (index123 > 3)
                                {
                                    if (((Math.Abs(resultValue.Z1) - Math.Abs(Z10)) > 200) || (Math.Abs(resultValue.Z1) > 300))
                                    {
                                        N1++;
                                        if (N1 <= 1)
                                        {
                                            if (resultValue.z1IsPositive == z1IsPositive)
                                            {
                                                y1Max = resultValue.y1;
                                                y2Max = resultValue.y1 - 2;
                                                y1Min = Cache.FindMinY;
                                                y2Min = Cache.FindMinY;
                                            } //y1Min = Cache.FindMinY; }
                                            else
                                            {
                                                y1Min = resultValue.y1;
                                                y2Min = resultValue.y1 + 2;
                                                y1Max = Cache.FindMaxY;
                                                y2Max = Cache.FindMaxY;
                                            } //y1Max = Cache.FindMaxY; }
                                        }
                                        else
                                        {
                                            if (resultValue.z1IsPositive == z1IsPositive)
                                            {
                                                y1Min = resultValue.y1;
                                                y2Min = resultValue.y1 + 2;
                                            }
                                            else
                                            {
                                                y1Max = resultValue.y1;
                                                y2Max = resultValue.y1 - 2;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        N1 = 0;
                                        if (resultValue.z1IsPositive == z1IsPositive)
                                        {
                                            y1Min = resultValue.y1;
                                            y2Min = resultValue.y1 + 2;
                                        }
                                        else
                                        {
                                            y1Max = resultValue.y1;
                                            y2Max = resultValue.y1 - 2;
                                        }
                                    }

                                    if (y1Max > Cache.FindMaxY)
                                        y1Max = Cache.FindMaxY;
                                    if (y1Min > Cache.FindMaxY)
                                        y1Min = Cache.FindMaxY;
                                    if (y1Min < Cache.FindMinY)
                                        y1Min = Cache.FindMinY;
                                    if (y1Max < Cache.FindMinY)
                                        y1Max = Cache.FindMinY;

                                    if (y2Max > Cache.FindMaxY)
                                        y2Max = Cache.FindMaxY;
                                    if (y2Min > Cache.FindMaxY)
                                        y2Min = Cache.FindMaxY;
                                    if (y2Min < Cache.FindMinY)
                                        y2Min = Cache.FindMinY;
                                    if (y2Max < Cache.FindMinY)
                                        y2Max = Cache.FindMinY;
                                }
                                else
                                {
                                    if (resultValue.z1IsPositive == z1IsPositive)
                                    {
                                        y1Min = resultValue.y1;
                                        y2Min = resultValue.y1 + 2;
                                    }
                                    else
                                    {
                                        y1Max = resultValue.y1;
                                        y2Max = resultValue.y1 - 2;
                                    }
                                }

                                Z10 = resultValue.Z1;

                                if (y1Min == y1Max || (y1Min + 1 == y1Max))
                                {
                                    var minZ1Item = Z1List.OrderBy(t => Math.Abs(t.z1)).FirstOrDefault();
                                    AdsYGainsDichotomySpeedCacheItemList.Single(t => t.SpeedYValue == speedvalue).SetY1(minZ1Item.y1);
                                    AdsYGainsDichotomySpeedCacheItemList.Single(t => t.SpeedYValue == speedvalue).SetY2(minZ1Item.y1);
                                    IsY1Stop = true;
                                }
                            }
                            else
                            {
                                var minZ1Item = Z1List.OrderBy(t => Math.Abs(t.z1)).FirstOrDefault();
                                AdsYGainsDichotomySpeedCacheItemList.Single(t => t.SpeedYValue == speedvalue).SetY1(minZ1Item.y1);
                                AdsYGainsDichotomySpeedCacheItemList.Single(t => t.SpeedYValue == speedvalue).SetY2(minZ1Item.y1);
                            }

                            if (!IsY3Stop)
                            {
                                if (index123 > 3)
                                {
                                    if (((Math.Abs(resultValue.Z3) - Math.Abs(Z30)) > 150) || (Math.Abs(resultValue.Z3) > 180))
                                    {
                                        N3++;
                                        if (N3 <= 1)
                                        {
                                            if (resultValue.z3IsPositive == z3IsPositive)
                                            {
                                                y3Max = resultValue.y3;
                                                y3Min = Cache.FindMinY;
                                            }
                                            else
                                            {
                                                y3Min = resultValue.y3;
                                                y3Max = Cache.FindMaxY;
                                            }
                                        }
                                        else
                                        {
                                            if (resultValue.z3IsPositive == z3IsPositive)
                                            {
                                                y3Min = resultValue.y3;
                                                y3Max = Cache.FindMaxY;
                                            }
                                            else
                                            {
                                                y3Max = resultValue.y3;
                                                y3Min = Cache.FindMinY;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        N3 = 0;
                                        if (resultValue.z3IsPositive == z3IsPositive) y3Min = resultValue.y3;
                                        else y3Max = resultValue.y3;
                                    }

                                    if (y3Max > Cache.FindMaxY)
                                        y3Max = Cache.FindMaxY;
                                    if (y3Min < Cache.FindMinY)
                                        y3Min = Cache.FindMinY;
                                }
                                else
                                {
                                    if (resultValue.z3IsPositive == z3IsPositive) y3Min = resultValue.y3;
                                    else y3Max = resultValue.y3;
                                }

                                Z30 = resultValue.Z3;

                                if (y3Min == y3Max || (y3Min + 1 == y3Max))
                                {
                                    var minZ3Item = Z3List.OrderBy(t => Math.Abs(t.z3)).FirstOrDefault();
                                    AdsYGainsDichotomySpeedCacheItemList.Single(t => t.SpeedYValue == speedvalue).SetY3(minZ3Item.y3);
                                    IsY3Stop = true;
                                }
                            }
                            else
                            {
                                var minZ3Item = Z3List.OrderBy(t => Math.Abs(t.z3)).FirstOrDefault();
                                AdsYGainsDichotomySpeedCacheItemList.Single(t => t.SpeedYValue == speedvalue).SetY3(minZ3Item.y3);
                            }

                            if (IsY1Stop && IsY3Stop) isStop123 = false;
                        }

                        if (isStop456)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            index456++;
                            if (index456 > 15)
                                isStop456 = false;
                            var resultValue = await DichotomyFindY1Y2Y3Async(!Cache.IsPositive, speedvalue, index456, y4Min, y4Max, y5Min, y5Max, y6Min, y6Max, cancellationToken);
                            Z4List.Add((resultValue.y1, resultValue.Z1));
                            Z5List.Add((resultValue.y2, resultValue.Z2));
                            Z6List.Add((resultValue.y3, resultValue.Z3));
                            if (!IsY4Stop)
                            {
                                if (index456 > 3)
                                {
                                    if (((Math.Abs(resultValue.Z1) - Math.Abs(Z40)) > 200) || (Math.Abs(resultValue.Z1) > 300))
                                    {
                                        N4++;
                                        if (N4 <= 1)
                                        {
                                            if (resultValue.z1IsPositive == z4IsPositive)
                                            {
                                                y4Max = resultValue.y1;
                                                y4Min = Cache.FindMinY;
                                            } //y5Max = resultValue.y1 - 2; y5Min = Cache.FindMinY;
                                            else
                                            {
                                                y4Min = resultValue.y1;
                                                y4Max = Cache.FindMaxY;
                                            } //y5Min = resultValue.y1 + 2; y5Max = Cache.FindMaxY;
                                        }
                                        else
                                        {
                                            //if (resultValue.z1IsPositive == z4IsPositive) y4Min = resultValue.y1;
                                            //else y4Max = resultValue.y1;
                                            if (resultValue.z1IsPositive == z4IsPositive)
                                            {
                                                y4Min = resultValue.y1;
                                                y4Max = Cache.FindMaxY;
                                            } //y5Min = resultValue.y1 + 2;
                                            else
                                            {
                                                y4Max = resultValue.y1;
                                                y4Min = Cache.FindMinY;
                                            } //y5Max = resultValue.y1 - 2;
                                        }
                                    }
                                    else
                                    {
                                        N4 = 0;
                                        if (resultValue.z1IsPositive == z4IsPositive)
                                        {
                                            y4Min = resultValue.y1;
                                        } //y5Min = resultValue.y1 + 2;
                                        else
                                        {
                                            y4Max = resultValue.y1;
                                        } //y5Max = resultValue.y1 - 2;
                                    }

                                    if (y4Max > Cache.FindMaxY)
                                        y4Max = Cache.FindMaxY;
                                    if (y4Min > Cache.FindMaxY)
                                        y4Min = Cache.FindMaxY;
                                    if (y4Min < Cache.FindMinY)
                                        y4Min = Cache.FindMinY;
                                    if (y4Max < Cache.FindMinY)
                                        y4Max = Cache.FindMinY;

                                    if (y5Max > Cache.FindMaxY)
                                        y5Max = Cache.FindMaxY;
                                    if (y5Min > Cache.FindMaxY)
                                        y5Min = Cache.FindMaxY;
                                    if (y5Min < Cache.FindMinY)
                                        y5Min = Cache.FindMinY;
                                    if (y5Max < Cache.FindMinY)
                                        y5Max = Cache.FindMinY;
                                }
                                else
                                {
                                    if (resultValue.z1IsPositive == z4IsPositive)
                                    {
                                        y4Min = resultValue.y1;
                                    }
                                    else
                                    {
                                        y4Max = resultValue.y1;
                                    }
                                }

                                Z40 = resultValue.Z1;

                                if (y4Min == y4Max || (y4Min + 1 == y4Max))
                                {
                                    var minZ1Item = Z4List.OrderBy(t => Math.Abs(t.z1)).FirstOrDefault();
                                    AdsYGainsDichotomySpeedCacheConverseItemList.Single(t => t.SpeedYValue == speedvalue).SetY1(minZ1Item.y1);
                                    IsY4Stop = true;
                                }
                            }
                            else
                            {
                                var minZ1Item = Z4List.OrderBy(t => Math.Abs(t.z1)).FirstOrDefault();
                                AdsYGainsDichotomySpeedCacheConverseItemList.Single(t => t.SpeedYValue == speedvalue).SetY1(minZ1Item.y1);
                            }

                            if (!IsY5Stop)
                            {
                                if (index456 > 3)
                                {
                                    if (((Math.Abs(resultValue.Z2) - Math.Abs(Z50)) > 200) || (Math.Abs(resultValue.Z2) > 300))
                                    {
                                        N5++;
                                        if (N5 <= 1)
                                        {
                                            if (resultValue.z2IsPositive == z5IsPositive)
                                            {
                                                y5Max = resultValue.y2;
                                                y5Min = Cache.FindMinY;
                                            }
                                            else
                                            {
                                                y5Min = resultValue.y2;
                                                y5Max = Cache.FindMaxY;
                                            }
                                        }
                                        else
                                        {
                                            if (resultValue.z2IsPositive == z5IsPositive)
                                            {
                                                y5Min = resultValue.y2;
                                                y5Max = Cache.FindMaxY;
                                            }
                                            else
                                            {
                                                y5Max = resultValue.y2;
                                                y5Min = Cache.FindMinY;
                                            }
                                        }

                                        if (y5Max > Cache.FindMaxY)
                                            y5Max = Cache.FindMaxY;
                                        if (y5Min < Cache.FindMinY)
                                            y5Min = Cache.FindMinY;
                                    }
                                    else
                                    {
                                        N5 = 0;
                                        if (resultValue.z2IsPositive == z5IsPositive) y5Min = resultValue.y2;
                                        else y5Max = resultValue.y2;
                                    }
                                }
                                else
                                {
                                    if (resultValue.z2IsPositive == z5IsPositive) y5Min = resultValue.y2;
                                    else y5Max = resultValue.y2;
                                }

                                Z50 = resultValue.Z2;

                                if (y5Min == y5Max || (y5Min + 1 == y5Max))
                                {
                                    var minZ2Item = Z5List.OrderBy(t => Math.Abs(t.z2)).FirstOrDefault();
                                    AdsYGainsDichotomySpeedCacheConverseItemList.Single(t => t.SpeedYValue == speedvalue).SetY2(minZ2Item.y2);
                                    IsY5Stop = true;
                                }
                            }
                            else
                            {
                                var minZ2Item = Z5List.OrderBy(t => Math.Abs(t.z2)).FirstOrDefault();
                                AdsYGainsDichotomySpeedCacheConverseItemList.Single(t => t.SpeedYValue == speedvalue).SetY2(minZ2Item.y2);
                            }

                            if (!IsY6Stop)
                            {
                                if (index456 > 3)
                                {
                                    if (((Math.Abs(resultValue.Z3) - Math.Abs(Z60)) > 150) || (Math.Abs(resultValue.Z3) > 180))
                                    {
                                        N6++;
                                        if (N6 <= 1)
                                        {
                                            if (resultValue.z3IsPositive == z6IsPositive)
                                            {
                                                y6Max = resultValue.y3;
                                                y6Min = Cache.FindMinY;
                                            }
                                            else
                                            {
                                                y6Min = resultValue.y3;
                                                y6Max = Cache.FindMaxY;
                                            }
                                        }
                                        else
                                        {
                                            if (resultValue.z3IsPositive == z6IsPositive)
                                            {
                                                y6Min = resultValue.y3;
                                                y6Max = Cache.FindMaxY;
                                            }
                                            else
                                            {
                                                y6Max = resultValue.y3;
                                                y6Min = Cache.FindMinY;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        N6 = 0;
                                        if (resultValue.z3IsPositive == z6IsPositive) y6Min = resultValue.y3;
                                        else y6Max = resultValue.y3;
                                    }

                                    if (y6Max > Cache.FindMaxY)
                                        y6Max = Cache.FindMaxY;
                                    if (y6Min < Cache.FindMinY)
                                        y6Min = Cache.FindMinY;
                                }
                                else
                                {
                                    if (resultValue.z3IsPositive == z6IsPositive) y6Min = resultValue.y3;
                                    else y6Max = resultValue.y3;
                                }

                                Z60 = resultValue.Z3;

                                if (y6Min == y6Max || (y6Min + 1 == y6Max))
                                {
                                    var minZ3Item = Z6List.OrderBy(t => Math.Abs(t.z3)).FirstOrDefault();
                                    AdsYGainsDichotomySpeedCacheConverseItemList.Single(t => t.SpeedYValue == speedvalue).SetY3(minZ3Item.y3);
                                    IsY6Stop = true;
                                }
                            }
                            else
                            {
                                var minZ3Item = Z6List.OrderBy(t => Math.Abs(t.z3)).FirstOrDefault();
                                AdsYGainsDichotomySpeedCacheConverseItemList.Single(t => t.SpeedYValue == speedvalue).SetY3(minZ3Item.y3);
                            }

                            if (IsY4Stop && IsY5Stop && IsY6Stop) isStop456 = false;
                        }

                        if ((isStop123 == false) && (isStop456 == false))
                            break;
                    }
                }

                for (int r = 0; r < Cache.SpeedYValueList.Count; r++)
                {
                    var speedvalue = Cache.SpeedYValueList[r];
                    var HrpItemList = AdsYGainsCacheItemList.Where(t => t.SpeedYValue == speedvalue).ToList();
                    var bestHrpItem = HrpItemList.OrderBy(t => t.SumHRP).First();
                    AdsYGainsCacheBestItemList.Add(bestHrpItem);
                    HrpItemList = AdsYGainsCacheConverseItemList.Where(t => t.SpeedYValue == speedvalue).ToList();
                    bestHrpItem = HrpItemList.OrderBy(t => t.SumHRP).First();
                    AdsYGainsCacheConverseBestItemList.Add(bestHrpItem);
                }

                Logger.LogHtmlInformation("SpeedBestYValueTable", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Cache.Threshold,
                    //SpeedXPositiveTable = new HtmlTable([.. AdsYGainsDichotomySpeedCacheItemList.Select(t => new { t.SpeedYValue, t.PositiveY1, t.PositiveY2, t.PositiveY3 }).Cast<object>()])
                    SpeedYPositiveTable = new HtmlTable([.. AdsYGainsCacheBestItemList.Select(t => new { t.SpeedYValue, t.PositiveY1, t.PositiveY2, t.PositiveY3 }).Cast<object>()])
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("SpeedBestYValueTable_Converse", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Cache.Threshold,
                    //SpeedXNegativeTable = new HtmlTable([.. AdsYGainsDichotomySpeedCacheConverseItemList.Select(t => new { t.SpeedYValue, t.NegativeY4, t.NegativeY5, t.NegativeY6 }).Cast<object>()])
                    SpeedYNegativeTable = new HtmlTable([.. AdsYGainsCacheConverseBestItemList.Select(t => new { t.SpeedYValue, t.NegativeY4, t.NegativeY5, t.NegativeY6 }).Cast<object>()])
                }), HtmlLogUniqueId.LoggingHtml());
            }
            catch (Exception ex)
            {
                SetDefaultYValue(cancellationToken);
                DialogWindowProvider.ShowDialog("Calibrate Z1Z2Z3 Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} {ex.Message}!"), HtmlLogUniqueId.LoggingHtml());
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
        SynchronizationContextProvider.Send(() => AdsYGainsHrpCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsYGainsHrpCacheConverseItemList.Clear());
        SynchronizationContextProvider.Send(() => ResultAdsYGainsHrpCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => ResultAdsYGainsHrpCacheConverseItemList.Clear());
        var result = true;
        await InvokeCalibrateAsync(async () =>
        {
            try
            {
                for (int r = 0; r < AdsYGainsDichotomySpeedCacheItemList.Count; r++)
                {
                    var speedCacheItem = AdsYGainsDichotomySpeedCacheItemList[r];
                    var speedCacheConverseItem = AdsYGainsDichotomySpeedCacheConverseItemList[r];
                    Logger.LogHtmlInformation($"Param_V{speedCacheItem.SpeedYValue.ToString()}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        speedCacheItem.IsPositive,
                        SpeedYValue = speedCacheItem.SpeedYValue,
                        HrpThreshold = Cache.VerifyThreshold,
                        StartPosition = Cache.GetStartPosition(),
                        EndPosition = Cache.GetEndPosition()
                    }), HtmlLogUniqueId.LoggingHtml());

                    Logger.LogHtmlInformation($"Param_V_Converse{speedCacheConverseItem.SpeedYValue.ToString()}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        speedCacheConverseItem.IsPositive,
                        SpeedYValue = speedCacheConverseItem.SpeedYValue,
                        HrpThreshold = Cache.VerifyThreshold,
                        StartPosition = Cache.GetEndPosition(),
                        EndPosition = Cache.GetStartPosition()
                    }), HtmlLogUniqueId.LoggingHtml());

                    var HrpItemList = AdsYGainsCacheItemList.Where(t => t.SpeedYValue == speedCacheItem.SpeedYValue).ToList();
                    var bestHrpItem = HrpItemList.OrderBy(t => t.SumHRP).First();
                    SynchronizationContextProvider.Send(() => ResultAdsYGainsHrpCacheItemList.Add(bestHrpItem));

                    HrpItemList = AdsYGainsCacheConverseItemList.Where(t => t.SpeedYValue == speedCacheConverseItem.SpeedYValue).ToList();
                    bestHrpItem = HrpItemList.OrderBy(t => t.SumHRP).First();
                    SynchronizationContextProvider.Send(() => ResultAdsYGainsHrpCacheConverseItemList.Add(bestHrpItem));
                }

                Logger.LogHtmlInformation("SpeedBestYValueTable", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Cache.Threshold,
                    SpeedYPositiveTable = new HtmlTable([.. ResultAdsYGainsHrpCacheItemList.Select(t => new { t.SpeedYValue, t.PositiveY1, t.PositiveY2, t.PositiveY3 }).Cast<object>()])
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("SpeedBestYValueTable_Converse", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Cache.Threshold,
                    SpeedYNegativeTable = new HtmlTable([.. ResultAdsYGainsHrpCacheConverseItemList.Select(t => new { t.SpeedYValue, t.NegativeY4, t.NegativeY5, t.NegativeY6 }).Cast<object>()])
                }), HtmlLogUniqueId.LoggingHtml());

                {
                    var adsYGainsItemDto = new AdsYGainsItemDto
                    {
                        IsPositive = Cache.IsPositive
                    };
                    var SpeedValueList = new List<double>();
                    var y1PlotList = new List<Point>();
                    var y1SmoothPlotList = new List<Point>();
                    var y2PlotList = new List<Point>();
                    var y2SmoothPlotList = new List<Point>();
                    var y3PlotList = new List<Point>();
                    var y3SmoothPlotList = new List<Point>();
                    var y1List = new List<double>();
                    var y2List = new List<double>();
                    var y3List = new List<double>();
                    foreach (var hrpCacheItem in ResultAdsYGainsHrpCacheItemList)
                    {
                        SpeedValueList.Add(hrpCacheItem.SpeedYValue);
                        y1List.Add(hrpCacheItem.GetAdsY1());
                        y2List.Add(hrpCacheItem.GetAdsY2());
                        y3List.Add(hrpCacheItem.GetAdsY3());
                        y1PlotList.Add(new Point(hrpCacheItem.SpeedYValue, hrpCacheItem.GetAdsY1()));
                        y2PlotList.Add(new Point(hrpCacheItem.SpeedYValue, hrpCacheItem.GetAdsY2()));
                        y3PlotList.Add(new Point(hrpCacheItem.SpeedYValue, hrpCacheItem.GetAdsY3()));
                    }

                    var X = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(SpeedValueList);
                    var Y1 = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(y1List);
                    var Y2 = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(y2List);
                    var Y3 = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(y3List);
                    var (p0, p1, p2, _, yPredicted1) = PolynomialCurve.Fit2(X, Y1);
                    var (p3, p4, p5, _, yPredicted2) = PolynomialCurve.Fit2(X, Y2);
                    var (p6, p7, p8, _, yPredicted3) = PolynomialCurve.Fit2(X, Y3);

                    for (var i = 0; i < SpeedValueList.Count; i++)
                    {
                        y1SmoothPlotList.Add(new Point(SpeedValueList[i], yPredicted1[i]));
                        y2SmoothPlotList.Add(new Point(SpeedValueList[i], yPredicted2[i]));
                        y3SmoothPlotList.Add(new Point(SpeedValueList[i], yPredicted3[i]));
                    }

                    adsYGainsItemDto.SetPositiveY1Plots(y1PlotList);
                    adsYGainsItemDto.SetPositiveY1SmoothPlots(y1SmoothPlotList);
                    adsYGainsItemDto.SetPositiveY2Plots(y2PlotList);
                    adsYGainsItemDto.SetPositiveY2SmoothPlots(y2SmoothPlotList);
                    adsYGainsItemDto.SetPositiveY3Plots(y3PlotList);
                    adsYGainsItemDto.SetPositiveY3SmoothPlots(y3SmoothPlotList);
                    adsYGainsItemDto.SetY1P1(p2);
                    adsYGainsItemDto.SetY1P2(p1);
                    adsYGainsItemDto.SetY1P3(p0);
                    adsYGainsItemDto.SetY2P1(p5);
                    adsYGainsItemDto.SetY2P2(p4);
                    adsYGainsItemDto.SetY2P3(p3);
                    adsYGainsItemDto.SetY3P1(p8);
                    adsYGainsItemDto.SetY3P2(p7);
                    adsYGainsItemDto.SetY3P3(p6);

                    PlotY1Y2Y3List.Clear();
                    PlotY1Y2Y3List = [.. PlotY1Y2Y3List, new WpfPlotModel("Y1", adsYGainsItemDto.GetPositiveY1Plots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY1Plots().ToArray()])];
                    PlotY1Y2Y3List = [.. PlotY1Y2Y3List, new WpfPlotModel("Y2", adsYGainsItemDto.GetPositiveY2Plots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY2Plots().ToArray()])];
                    PlotY1Y2Y3List = [.. PlotY1Y2Y3List, new WpfPlotModel("Y3", adsYGainsItemDto.GetPositiveY3Plots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY3Plots().ToArray()])];
                    PlotY1Y2Y3List = [.. PlotY1Y2Y3List, new WpfPlotModel("Y1Smooth", adsYGainsItemDto.GetPositiveY1SmoothPlots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY1SmoothPlots().ToArray()])];
                    PlotY1Y2Y3List = [.. PlotY1Y2Y3List, new WpfPlotModel("Y2Smooth", adsYGainsItemDto.GetPositiveY2SmoothPlots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY2SmoothPlots().ToArray()])];
                    PlotY1Y2Y3List = [.. PlotY1Y2Y3List, new WpfPlotModel("Y3Smooth", adsYGainsItemDto.GetPositiveY3SmoothPlots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY3SmoothPlots().ToArray()])];

                    ResultAdsYGainsItemDto.UpdatePositive(adsYGainsItemDto);
                }

                {
                    var adsYGainsItemDto = new AdsYGainsItemDto
                    {
                        IsPositive = !Cache.IsPositive
                    };
                    var SpeedValueList = new List<double>();
                    var y1PlotList = new List<Point>();
                    var y1SmoothPlotList = new List<Point>();
                    var y2PlotList = new List<Point>();
                    var y2SmoothPlotList = new List<Point>();
                    var y3PlotList = new List<Point>();
                    var y3SmoothPlotList = new List<Point>();
                    var y1List = new List<double>();
                    var y2List = new List<double>();
                    var y3List = new List<double>();
                    foreach (var hrpCacheItem in ResultAdsYGainsHrpCacheConverseItemList)
                    {
                        SpeedValueList.Add(hrpCacheItem.SpeedYValue);
                        y1List.Add(hrpCacheItem.GetAdsY1());
                        y2List.Add(hrpCacheItem.GetAdsY2());
                        y3List.Add(hrpCacheItem.GetAdsY3());
                        y1PlotList.Add(new Point(hrpCacheItem.SpeedYValue, hrpCacheItem.GetAdsY1()));
                        y2PlotList.Add(new Point(hrpCacheItem.SpeedYValue, hrpCacheItem.GetAdsY2()));
                        y3PlotList.Add(new Point(hrpCacheItem.SpeedYValue, hrpCacheItem.GetAdsY3()));
                    }

                    var X = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(SpeedValueList);
                    var Y1 = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(y1List);
                    var Y2 = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(y2List);
                    var Y3 = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(y3List);

                    var (p0, p1, p2, _, yPredicted1) = PolynomialCurve.Fit2(X, Y1);
                    var (p3, p4, p5, _, yPredicted2) = PolynomialCurve.Fit2(X, Y2);
                    var (p6, p7, p8, _, yPredicted3) = PolynomialCurve.Fit2(X, Y3);

                    for (var i = 0; i < SpeedValueList.Count; i++)
                    {
                        y1SmoothPlotList.Add(new Point(SpeedValueList[i], yPredicted1[i]));
                        y2SmoothPlotList.Add(new Point(SpeedValueList[i], yPredicted2[i]));
                        y3SmoothPlotList.Add(new Point(SpeedValueList[i], yPredicted3[i]));
                    }

                    adsYGainsItemDto.SetPositiveY1Plots(y1PlotList);
                    adsYGainsItemDto.SetPositiveY1SmoothPlots(y1SmoothPlotList);
                    adsYGainsItemDto.SetPositiveY2Plots(y2PlotList);
                    adsYGainsItemDto.SetPositiveY2SmoothPlots(y2SmoothPlotList);
                    adsYGainsItemDto.SetPositiveY3Plots(y3PlotList);
                    adsYGainsItemDto.SetPositiveY3SmoothPlots(y3SmoothPlotList);
                    adsYGainsItemDto.SetY1P1(p2);
                    adsYGainsItemDto.SetY1P2(p1);
                    adsYGainsItemDto.SetY1P3(p0);
                    adsYGainsItemDto.SetY2P1(p5);
                    adsYGainsItemDto.SetY2P2(p4);
                    adsYGainsItemDto.SetY2P3(p3);
                    adsYGainsItemDto.SetY3P1(p8);
                    adsYGainsItemDto.SetY3P2(p7);
                    adsYGainsItemDto.SetY3P3(p6);

                    PlotY4Y5Y6List.Clear();
                    PlotY4Y5Y6List = [.. PlotY4Y5Y6List, new WpfPlotModel("Y4", adsYGainsItemDto.GetPositiveY1Plots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY1Plots().ToArray()])];
                    PlotY4Y5Y6List = [.. PlotY4Y5Y6List, new WpfPlotModel("Y5", adsYGainsItemDto.GetPositiveY2Plots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY2Plots().ToArray()])];
                    PlotY4Y5Y6List = [.. PlotY4Y5Y6List, new WpfPlotModel("Y6", adsYGainsItemDto.GetPositiveY3Plots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY3Plots().ToArray()])];
                    PlotY4Y5Y6List = [.. PlotY4Y5Y6List, new WpfPlotModel("Y4Smooth", adsYGainsItemDto.GetPositiveY1SmoothPlots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY1SmoothPlots().ToArray()])];
                    PlotY4Y5Y6List = [.. PlotY4Y5Y6List, new WpfPlotModel("Y5Smooth", adsYGainsItemDto.GetPositiveY2SmoothPlots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY2SmoothPlots().ToArray()])];
                    PlotY4Y5Y6List = [.. PlotY4Y5Y6List, new WpfPlotModel("Y6Smooth", adsYGainsItemDto.GetPositiveY3SmoothPlots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY3SmoothPlots().ToArray()])];

                    ResultAdsYGainsItemDto.UpdateNegative(adsYGainsItemDto);
                }

                Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Y1P1 = ResultAdsYGainsItemDto.PositiveY1P1,
                    Y1P2 = ResultAdsYGainsItemDto.PositiveY1P2,
                    Y1P3 = ResultAdsYGainsItemDto.PositiveY1P3,
                    Y2P1 = ResultAdsYGainsItemDto.PositiveY2P1,
                    Y2P2 = ResultAdsYGainsItemDto.PositiveY2P2,
                    Y2P3 = ResultAdsYGainsItemDto.PositiveY2P3,
                    Y3P1 = ResultAdsYGainsItemDto.PositiveY3P1,
                    Y3P2 = ResultAdsYGainsItemDto.PositiveY3P2,
                    Y3P3 = ResultAdsYGainsItemDto.PositiveY3P3,
                    Y1X2Plots = new HtmlPlot2DLinesChart([
                        ("Y1Plots", ResultAdsYGainsItemDto.PositiveY1Plots.ToArray()),
                        ($"Y1={ResultAdsYGainsItemDto.PositiveY1P1}*V^2{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.PositiveY1P2)}*V{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.PositiveY1P3)}",
                            ResultAdsYGainsItemDto.PositiveY1SmoothPlots.ToArray()),
                        ("Y2Plots", ResultAdsYGainsItemDto.PositiveY2Plots.ToArray()),
                        ($"Y2={ResultAdsYGainsItemDto.PositiveY2P1}*V^2{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.PositiveY2P2)}*V{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.PositiveY2P3)}",
                            ResultAdsYGainsItemDto.PositiveY2SmoothPlots.ToArray()),
                        ("Y3Plots", ResultAdsYGainsItemDto.PositiveY3Plots.ToArray()),
                        ($"Y3={ResultAdsYGainsItemDto.PositiveY3P1}*V^2{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.PositiveY3P2)}*V{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.PositiveY3P3)}",
                            ResultAdsYGainsItemDto.PositiveY3SmoothPlots.ToArray())
                    ], "Y1X2Y3Plots")
                }), HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Y4P1 = ResultAdsYGainsItemDto.NegativeY4P1,
                    Y4P2 = ResultAdsYGainsItemDto.NegativeY4P2,
                    Y4P3 = ResultAdsYGainsItemDto.NegativeY4P3,
                    Y5P1 = ResultAdsYGainsItemDto.NegativeY5P1,
                    Y5P2 = ResultAdsYGainsItemDto.NegativeY5P2,
                    Y5P3 = ResultAdsYGainsItemDto.NegativeY5P3,
                    Y6P1 = ResultAdsYGainsItemDto.NegativeY6P1,
                    Y6P2 = ResultAdsYGainsItemDto.NegativeY6P2,
                    Y6P3 = ResultAdsYGainsItemDto.NegativeY6P3,
                    Y4X5Plots = new HtmlPlot2DLinesChart([
                        ("Y4Plots", ResultAdsYGainsItemDto.NegativeY4Plots.ToArray()),
                        ($"Y4={ResultAdsYGainsItemDto.NegativeY4P1}*V^2{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.NegativeY4P2)}*V{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.NegativeY4P3)}",
                            ResultAdsYGainsItemDto.NegativeY4SmoothPlots.ToArray()),
                        ("Y5Plots", ResultAdsYGainsItemDto.NegativeY5Plots.ToArray()),
                        ($"Y5={ResultAdsYGainsItemDto.NegativeY5P1}*V^2{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.NegativeY5P2)}*V{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.NegativeY5P3)}",
                            ResultAdsYGainsItemDto.NegativeY5SmoothPlots.ToArray()),
                        ("Y6Plots", ResultAdsYGainsItemDto.NegativeY6Plots.ToArray()),
                        ($"Y6={ResultAdsYGainsItemDto.NegativeY6P1}*V^2{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.NegativeY6P2)}*V{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.NegativeY6P3)}",
                            ResultAdsYGainsItemDto.NegativeY6SmoothPlots.ToArray())
                    ], "Y4X5Y6Plots")
                }), HtmlLogUniqueId.LoggingHtml());
            }
            catch (Exception ex)
            {
                SetDefaultYValue(cancellationToken);
                DialogWindowProvider.ShowDialog("Calibrate HRP Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} {ex.Message}!"), HtmlLogUniqueId.LoggingHtml());
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
        await InvokeVerifyAsync(async () =>
        {
            if (SelectReviewItemDto is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Please select a review item!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            var result = await VerifyCaibrationAsync(SelectReviewItemDto, cancellationToken);
            return result;
        }).ConfigureAwait(false);
    }

    private async Task<bool> VerifyCaibrationAsync(AdsYGainsItemDto selectItemDto, CancellationToken cancellationToken)
    {
        SynchronizationContextProvider.Send(() => PositiveAdsYGainsHrpCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => NegativeAdsYGainsHrpCacheItemList.Clear());
        var result = await VerifyAsync(true).ConfigureAwait(false) && await VerifyAsync(false).ConfigureAwait(false);
        if (!result)
        {
            DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);
            SetDefaultYValue(cancellationToken);
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

            SetBestY1Y2Y3Values(selectItemDto, Cache.DefaultSpeedXValue, cancellationToken);

            DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);
        }

        return result;

        async Task<bool> VerifyAsync(bool isPositive)
        {
            var resultTemp = true;
            try
            {
                Cache.IsPositive = isPositive;
                var adsYGainsItemDto = selectItemDto.Clone();
                adsYGainsItemDto.IsPositive = Cache.IsPositive;

                // 下发与校准方向相反的，默认移动速度下的合适前馈，防止快速移动时不合适的前馈导致stage无法停稳就继续监测buffer带来的误差
                var negativeDto = selectItemDto.Clone();
                negativeDto.IsPositive = !isPositive;
                var y1 = GetYValue(negativeDto.GetY1P1(), negativeDto.GetY1P2(), negativeDto.GetY1P3(), Cache.DefaultSpeedYValue);
                var y2 = GetYValue(negativeDto.GetY2P1(), negativeDto.GetY2P2(), negativeDto.GetY2P3(), Cache.DefaultSpeedYValue);
                var y3 = GetYValue(negativeDto.GetY3P1(), negativeDto.GetY3P2(), negativeDto.GetY3P3(), Cache.DefaultSpeedYValue);
                AdsViewModel.SetSensorYSpeedFeedForwardValue(negativeDto.IsPositive, (y1, y2, y3));

                var index = 0;
                if (isPositive)
                    Logger.LogHtmlInformation("Positive", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        Threshold = Cache.VerifyThreshold,
                        StartPosition = Cache.GetStartPosition(),
                        EndPosition = Cache.GetEndPosition(),
                        selectItemDto.PositiveY1P1,
                        selectItemDto.PositiveY1P2,
                        selectItemDto.PositiveY1P3,
                        selectItemDto.PositiveY2P1,
                        selectItemDto.PositiveY2P2,
                        selectItemDto.PositiveY2P3,
                        selectItemDto.PositiveY3P1,
                        selectItemDto.PositiveY3P2,
                        selectItemDto.PositiveY3P3
                    }), HtmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlInformation("Negative", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        Threshold = Cache.VerifyThreshold,
                        StartPosition = Cache.GetStartPosition(),
                        EndPosition = Cache.GetEndPosition(),
                        selectItemDto.NegativeY4P1,
                        selectItemDto.NegativeY4P2,
                        selectItemDto.NegativeY4P3,
                        selectItemDto.NegativeY5P1,
                        selectItemDto.NegativeY5P2,
                        selectItemDto.NegativeY5P3,
                        selectItemDto.NegativeY6P1,
                        selectItemDto.NegativeY6P2,
                        selectItemDto.NegativeY6P3
                    }), HtmlLogUniqueId.LoggingHtml());
                var verifySpeedValueList = Cache.SpeedYValueList.Zip(Cache.SpeedYValueList.Skip(1), (t1, t2) => (t1 + t2) / 2).ToList();
                foreach (var speedvalueItem in verifySpeedValueList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    index++;
                    var adsYGainsHrpCacheItem = new AdsYGainsCacheItem
                    {
                        Index = index,
                        IsPositive = Cache.IsPositive,
                        SpeedYValue = speedvalueItem
                    };
                    adsYGainsHrpCacheItem.SetAdsY1(GetYValue(adsYGainsItemDto.GetY1P1(), adsYGainsItemDto.GetY1P2(), adsYGainsItemDto.GetY1P3(), speedvalueItem));
                    adsYGainsHrpCacheItem.SetAdsY2(GetYValue(adsYGainsItemDto.GetY2P1(), adsYGainsItemDto.GetY2P2(), adsYGainsItemDto.GetY2P3(), speedvalueItem));
                    adsYGainsHrpCacheItem.SetAdsY3(GetYValue(adsYGainsItemDto.GetY3P1(), adsYGainsItemDto.GetY3P2(), adsYGainsItemDto.GetY3P3(), speedvalueItem));
                    (resultTemp, _) = await GetHrpAsync(adsYGainsHrpCacheItem, cancellationToken).ConfigureAwait(false);
                    if (adsYGainsItemDto.IsPositive) SynchronizationContextProvider.Send(() => PositiveAdsYGainsHrpCacheItemList.Add(adsYGainsHrpCacheItem));
                    else SynchronizationContextProvider.Send(() => NegativeAdsYGainsHrpCacheItemList.Add(adsYGainsHrpCacheItem));
                    if (resultTemp == false) break;
                }
            }
            catch (Exception ex)
            {
                SetDefaultYValue(cancellationToken);
                DialogWindowProvider.ShowDialog("Verify Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} {ex.Message}!"), HtmlLogUniqueId.LoggingHtml());
                resultTemp = false;
                return resultTemp;
            }

            return resultTemp;
        }
    }

    private (List<Point> pointZ1, List<Point> pointSmoothZ1, List<double> smoothZ1, List<Point> pointZ2, List<Point> pointSmoothZ2, List<double> smoothZ2) GetadsYGainsValueTwo(List<double> PonitZ1, List<double> PonitZ2, int pointCount, int pointEnd)
    {
        var sgolayfiltListZ = MovMeanFilter.Smooth(501, MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(PonitZ1));
        var x = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(Enumerable.Range(1, sgolayfiltListZ.Count).Select(x => (double)x));
        List<double> smoothZ1 = [.. sgolayfiltListZ];
        smoothZ1 = smoothZ1.Take(pointEnd).ToList();

        sgolayfiltListZ = MovMeanFilter.Smooth(501, MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(PonitZ2));
        x = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(Enumerable.Range(1, sgolayfiltListZ.Count).Select(x => (double)x));
        List<double> smoothZ2 = [.. sgolayfiltListZ];
        smoothZ2 = smoothZ2.Take(pointEnd).ToList();

        var areaThreshold = PonitZ1.Take(pointCount).Average() + PonitZ2.Take(pointCount).Average();

        var kkValue = smoothZ1.Skip(pointCount).Average() + smoothZ2.Skip(pointCount).Average();

        var areaZ = kkValue - areaThreshold;

        double maxValue, minValue = 0;
        int maxIndex, minIndex = 0;
        var pointZ1 = new List<Point>();
        var pointSmoothZ1 = new List<Point>();

        var pointZ2 = new List<Point>();
        var pointSmoothZ2 = new List<Point>();

        if (areaZ > 0)
        {
            //开口向下
            maxValue = smoothZ1.Skip(pointCount).Max();
            maxIndex = smoothZ1.Skip(pointCount).ToList().IndexOf(maxValue);
            pointSmoothZ1.Add(new Point(pointCount + maxIndex, maxValue));
            pointZ1.Add(new Point(pointCount + maxIndex, PonitZ1[pointCount + maxIndex]));
            minValue = smoothZ1.Take(pointCount).Min();
            minIndex = smoothZ1.Take(pointCount).ToList().IndexOf(minValue);
            pointSmoothZ1.Add(new Point(minIndex, minValue));
            pointZ1.Add(new Point(minIndex, PonitZ1[minIndex]));
        }
        else
        {
            //开口向上
            minValue = smoothZ1.Skip(pointCount).Min();
            minIndex = smoothZ1.Skip(pointCount).ToList().IndexOf(minValue);
            pointSmoothZ1.Add(new Point(pointCount + minIndex, minValue));
            pointZ1.Add(new Point(pointCount + minIndex, PonitZ1[pointCount + minIndex]));
            maxValue = smoothZ1.Take(pointCount).Max();
            maxIndex = smoothZ1.Take(pointCount).ToList().IndexOf(maxValue);
            pointSmoothZ1.Add(new Point(maxIndex, maxValue));
            pointZ1.Add(new Point(maxIndex, PonitZ1[maxIndex]));
        }

        if (areaZ > 0)
        {
            //开口向下
            maxValue = smoothZ2.Skip(pointCount).Max();
            maxIndex = smoothZ2.Skip(pointCount).ToList().IndexOf(maxValue);
            pointSmoothZ2.Add(new Point(pointCount + maxIndex, maxValue));
            pointZ2.Add(new Point(pointCount + maxIndex, PonitZ2[pointCount + maxIndex]));
            minValue = smoothZ2.Take(pointCount).Min();
            minIndex = smoothZ2.Take(pointCount).ToList().IndexOf(minValue);
            pointSmoothZ2.Add(new Point(minIndex, minValue));
            pointZ2.Add(new Point(minIndex, PonitZ2[minIndex]));
        }
        else
        {
            //开口向上
            minValue = smoothZ2.Skip(pointCount).Min();
            minIndex = smoothZ2.Skip(pointCount).ToList().IndexOf(minValue);
            pointSmoothZ2.Add(new Point(pointCount + minIndex, minValue));
            pointZ2.Add(new Point(pointCount + minIndex, PonitZ2[pointCount + minIndex]));
            maxValue = smoothZ2.Take(pointCount).Max();
            maxIndex = smoothZ2.Take(pointCount).ToList().IndexOf(maxValue);
            pointSmoothZ2.Add(new Point(maxIndex, maxValue));
            pointZ2.Add(new Point(maxIndex, PonitZ2[maxIndex]));
        }

        return (pointZ1, pointSmoothZ1, smoothZ1, pointZ2, pointSmoothZ2, smoothZ2);
    }

    private (List<Point> pointZ, List<Point> pointSmoothZ, List<double> smoothZ) GetadsYGainsValue(List<double> PonitZ, int pointCount, int pointEnd)
    {
        var sgolayfiltListZ = MovMeanFilter.Smooth(501, MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(PonitZ));

        MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(Enumerable.Range(1, sgolayfiltListZ.Count).Select(x => (double)x));

        List<double> smoothZ = [.. sgolayfiltListZ];
        smoothZ = smoothZ.Take(pointEnd).ToList();
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

    private async Task<(bool, List<List<double>>)> GetZ1Z2Z3CurveAsync(AdsYGainsCacheItem adsYGainsCacheItem, CancellationToken cancellationToken, int repeatCount = 1)
    {
        var transBuffer = new List<List<double>>();
        try
        {
            if (adsYGainsCacheItem.IsPositive)
            {
                StageViewModel.SetYSpeedValue(Cache.DefaultSpeedYValue);
                StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetStartPosition());
                InvokeAdsService(() => AdsViewModel.SetSensorYSpeedFeedForwardValue(adsYGainsCacheItem.IsPositive, (adsYGainsCacheItem.GetAdsY1(), adsYGainsCacheItem.GetAdsY2(), adsYGainsCacheItem.GetAdsY3())), cancellationToken);
                StageViewModel.SetYSpeedValue(adsYGainsCacheItem.SpeedYValue);
                await Task.Delay(HostEnvironment.IsDevelopment() ? 1000 : 30000, cancellationToken);
                var task = Task.Run(() => AdsViewModel.GetSensorSpeedZ1Z2Z3TraceBufferList(TimeSpan.FromSeconds(Cache.WaitTime)));
                await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 3000, cancellationToken);
                StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetEndPosition());
                transBuffer = await task.ConfigureAwait(false);
            }
            else
            {
                StageViewModel.SetYSpeedValue(Cache.DefaultSpeedYValue);
                StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetEndPosition());
                InvokeAdsService(() => AdsViewModel.SetSensorYSpeedFeedForwardValue(adsYGainsCacheItem.IsPositive, (adsYGainsCacheItem.GetAdsY1(), adsYGainsCacheItem.GetAdsY2(), adsYGainsCacheItem.GetAdsY3())), cancellationToken);
                StageViewModel.SetYSpeedValue(adsYGainsCacheItem.SpeedYValue);
                await Task.Delay(HostEnvironment.IsDevelopment() ? 1000 : 30000, cancellationToken);
                var task = Task.Run(() => AdsViewModel.GetSensorSpeedZ1Z2Z3TraceBufferList(TimeSpan.FromSeconds(Cache.WaitTime)));
                await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 3000, cancellationToken);
                StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetStartPosition());
                transBuffer = await task.ConfigureAwait(false);
            }

            if (repeatCount > 5) return (false, transBuffer);
            if (transBuffer.Count <= 0) return await GetZ1Z2Z3CurveAsync(adsYGainsCacheItem, cancellationToken, repeatCount++).ConfigureAwait(false);

            var dataIsError = HasConsecutiveZeros(transBuffer[0], 50) &&
                              HasConsecutiveZeros(transBuffer[1], 50) &&
                              HasConsecutiveZeros(transBuffer[2], 50) &&
                              HasConsecutiveZeros(transBuffer[3], 50) &&
                              HasConsecutiveZeros(transBuffer[4], 50) &&
                              HasConsecutiveZeros(transBuffer[5], 50);
            if (dataIsError) return await GetZ1Z2Z3CurveAsync(adsYGainsCacheItem, cancellationToken, repeatCount++).ConfigureAwait(false);

            var YSpeedList = transBuffer[7];
            var speedChangedList = YSpeedList.ToPoints().Where(t => Math.Round(Math.Abs(t.Y) / adsYGainsCacheItem.SpeedYValue, 2) > 0.5);

            return (true, transBuffer);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException) throw;
            if (repeatCount > 5) return (false, transBuffer);

            Logger.LogError(ex, "GetZ1Z2Z3CurveAsync Error!");
            return await GetZ1Z2Z3CurveAsync(adsYGainsCacheItem, cancellationToken, repeatCount++).ConfigureAwait(false);
        }

        bool HasConsecutiveZeros(IEnumerable<double> array, int requiredZeros = 10)
        {
            return array
                .SkipWhile(x => x != 0) // 跳过非零部分
                .TakeWhile(x => x == 0) // 取连续的零
                .Count() >= requiredZeros; // 判断数量
        }
    }

    private async Task<(bool, List<(double Height, double Roll, double Pitch, double xSpeed, double ySpeed)>)> GetHrpAsync(AdsYGainsCacheItem adsYGainsItemDto, CancellationToken cancellationToken, int repeatCount = 1)
    {
        try
        {
            StageViewModel.SetYSpeedValue(Cache.DefaultSpeedYValue);
            StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetStartPosition());
            InvokeAdsService(() => AdsViewModel.SetSensorYSpeedFeedForwardValue(adsYGainsItemDto.IsPositive, (adsYGainsItemDto.GetAdsY1(), adsYGainsItemDto.GetAdsY2(), adsYGainsItemDto.GetAdsY3())), cancellationToken);
            StageViewModel.SetYSpeedValue(adsYGainsItemDto.SpeedYValue);
            StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetStartPosition());
            await Task.Delay(HostEnvironment.IsDevelopment() ? 1000 : 30000, cancellationToken);
            var task = Task.Run(() => AdsViewModel.GetSensorHeightRollPitchTraceBufferList(TimeSpan.FromSeconds(Cache.WaitTime)));
            await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 3000, cancellationToken);
            StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetEndPosition());
            var transBuffer = await task.ConfigureAwait(false);
            StageViewModel.SetXSpeedValue(Cache.DefaultSpeedYValue);
            if (transBuffer.Count > 0)
            {
                var YSpeedList = transBuffer.Select(t => t.ySpeed).ToList();
                if (YSpeedList.Count == 0 && YSpeedList is null) return (false, transBuffer);

                var speedChangedList = YSpeedList.ToPoints().Where(t => Math.Round(Math.Abs(t.Y) / adsYGainsItemDto.SpeedYValue, 2) > 0.5);
                var ySpeedStartIndex = Convert.ToInt32(speedChangedList.First().X);
                var ySpeedEndIndex = Convert.ToInt32(speedChangedList.Last().X);

                var heightList = transBuffer.Select(t => t.Height).Skip(ySpeedStartIndex).Take(ySpeedEndIndex - ySpeedStartIndex).ToList();
                var rollList = transBuffer.Select(t => t.Roll).Skip(ySpeedStartIndex).Take(ySpeedEndIndex - ySpeedStartIndex).ToList();
                var pitchList = transBuffer.Select(t => t.Pitch).Skip(ySpeedStartIndex).Take(ySpeedEndIndex - ySpeedStartIndex).ToList();
                var heightMax = heightList.Max(Math.Abs);
                var rollMax = rollList.Max(Math.Abs);
                var pitchMax = pitchList.Max(Math.Abs);
                adsYGainsItemDto.SetPlotH(heightList);
                adsYGainsItemDto.SetPlotP(pitchList);
                adsYGainsItemDto.SetPlotR(rollList);
                adsYGainsItemDto.SetH(heightMax);
                adsYGainsItemDto.SetR(rollMax);
                adsYGainsItemDto.SetP(pitchMax);
                adsYGainsItemDto.SumHRP = heightMax + rollMax + pitchMax;
                var result = heightMax < Cache.VerifyThreshold && rollMax < Cache.VerifyThreshold && pitchMax < Cache.VerifyThreshold;
                var (z1, z2, z3) = AdsViewModel.GetSensorSpeedZ1Z2Z3Value();

                adsYGainsItemDto.SetZ1(z1);
                adsYGainsItemDto.SetZ2(z2);
                adsYGainsItemDto.SetZ3(z3);

                if (adsYGainsItemDto.IsPositive)
                {
                    Logger.LogHtmlInformation(
                        result
                            ? $"V_{adsYGainsItemDto.SpeedYValue} Y1_{adsYGainsItemDto.GetAdsY1().ToString()} Y2_{adsYGainsItemDto.GetAdsY2().ToString()} Y3_{adsYGainsItemDto.GetAdsY3().ToString()} OK"
                            : $"V_{adsYGainsItemDto.SpeedYValue} Y1_{adsYGainsItemDto.GetAdsY1().ToString()} Y2_{adsYGainsItemDto.GetAdsY2().ToString()} Y3_{adsYGainsItemDto.GetAdsY3().ToString()} Failed",
                        HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            adsYGainsItemDto.SpeedYValue,
                            adsYGainsItemDto.IsPositive,
                            Cache.VerifyThreshold,
                            Y1 = adsYGainsItemDto.GetAdsY1(),
                            Y2 = adsYGainsItemDto.GetAdsY2(),
                            Y3 = adsYGainsItemDto.GetAdsY3(),
                            ySpeedStartIndex,
                            ySpeedEndIndex,
                            PlotHrp = new HtmlPlot2DLinesChart([
                                ("H", transBuffer.Select(t => t.Height).ToList().ToPoints()),
                                ("P", transBuffer.Select(t => t.Pitch).ToList().ToPoints()),
                                ("R", transBuffer.Select(t => t.Roll).ToList().ToPoints()),
                                ("S", transBuffer.Select(t => t.ySpeed).ToList().ToPoints())
                            ], "PlotHrp"),
                            HeightMax = heightMax,
                            RollMax = rollMax,
                            PitchMax = pitchMax,
                            PositiveZ1 = z1,
                            PositiveZ2 = z2,
                            PositiveZ3 = z3
                        }), HtmlLogUniqueId.LoggingHtml());
                }
                else
                {
                    Logger.LogHtmlInformation(
                        result
                            ? $"V_{adsYGainsItemDto.SpeedYValue} Y4_{adsYGainsItemDto.GetAdsY1().ToString()} Y5_{adsYGainsItemDto.GetAdsY2().ToString()} Y6_{adsYGainsItemDto.GetAdsY3().ToString()} OK"
                            : $"V_{adsYGainsItemDto.SpeedYValue} Y4_{adsYGainsItemDto.GetAdsY1().ToString()} Y5_{adsYGainsItemDto.GetAdsY2().ToString()} Y6_{adsYGainsItemDto.GetAdsY3().ToString()} Failed",
                        HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            adsYGainsItemDto.SpeedYValue,
                            adsYGainsItemDto.IsPositive,
                            Cache.VerifyThreshold,
                            Y4 = adsYGainsItemDto.GetAdsY1(),
                            Y5 = adsYGainsItemDto.GetAdsY2(),
                            Y6 = adsYGainsItemDto.GetAdsY3(),
                            PlotHrp = new HtmlPlot2DLinesChart([
                                ("H", transBuffer.Select(t => t.Height).ToList().ToPoints()),
                                ("P", transBuffer.Select(t => t.Pitch).ToList().ToPoints()),
                                ("R", transBuffer.Select(t => t.Roll).ToList().ToPoints()),
                                ("S", transBuffer.Select(t => t.ySpeed).ToList().ToPoints())
                            ], "PlotHrp"),
                            HeightMax = heightMax,
                            RollMax = rollMax,
                            PitchMax = pitchMax,
                            PositiveZ4 = z1,
                            PositiveZ5 = z2,
                            PositiveZ6 = z3
                        }), HtmlLogUniqueId.LoggingHtml());
                }

                return (result, transBuffer);
            }

            if (repeatCount > 5) return (false, transBuffer);

            return await GetHrpAsync(adsYGainsItemDto, cancellationToken, repeatCount++).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException) throw;
            if (repeatCount > 5) return (false, []);
            return await GetHrpAsync(adsYGainsItemDto, cancellationToken, repeatCount++).ConfigureAwait(false);
        }
    }

    private bool Save(AdsYGainsItemDto itemDto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibration = itemDto.Clone();

        CacheProvider.Set(Calibration, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(() => AdsYGainsCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsYGainsCacheBestItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsYGainsCacheConverseItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsYGainsCacheConverseBestItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsYGainsDichotomySpeedCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsYGainsDichotomySpeedCacheConverseItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsYGainsHrpCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsYGainsHrpCacheConverseItemList.Clear());
        SynchronizationContextProvider.Send(() => ResultAdsYGainsHrpCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => ResultAdsYGainsHrpCacheConverseItemList.Clear());
    }

    private void GetDefaultYValue()
    {
        var positionValue = AdsViewModel.GetSensorYSpeedFeedForwardValue(true);
        Cache.Y1 = positionValue.Y1;
        Cache.Y2 = positionValue.Y2;
        Cache.Y3 = positionValue.Y3;
        var negativeValue = AdsViewModel.GetSensorYSpeedFeedForwardValue(false);
        Cache.Y4 = negativeValue.Y1;
        Cache.Y5 = negativeValue.Y2;
        Cache.Y6 = negativeValue.Y3;
    }

    private void SetDefaultYValue(CancellationToken cancellationToken)
    {
        StageViewModel.SetYSpeedValue(Cache.DefaultSpeedYValue);
        InvokeAdsService(() => AdsViewModel.SetSensorYSpeedFeedForwardValue(false, (Cache.Y4, Cache.Y5, Cache.Y6)), cancellationToken);
        InvokeAdsService(() => AdsViewModel.SetSensorYSpeedFeedForwardValue(true, (Cache.Y1, Cache.Y2, Cache.Y3)), cancellationToken);
    }

    private void SetBestY1Y2Y3Values(AdsYGainsItemDto adsXGainsItemDto, double speedValue, CancellationToken cancellationToken)
    {
        StageViewModel.SetXSpeedValue(speedValue);
        double y1 = 0d, y2 = 0d, y3 = 0d, y4 = 0d, y5 = 0d, y6 = 0d;
        if (adsXGainsItemDto.PositiveY1P1 != 0) y1 = GetYValue(adsXGainsItemDto.PositiveY1P1, adsXGainsItemDto.PositiveY1P2, adsXGainsItemDto.PositiveY1P3, speedValue);
        if (adsXGainsItemDto.PositiveY2P1 != 0) y2 = GetYValue(adsXGainsItemDto.PositiveY2P1, adsXGainsItemDto.PositiveY2P2, adsXGainsItemDto.PositiveY2P3, speedValue);
        if (adsXGainsItemDto.PositiveY3P1 != 0) y3 = GetYValue(adsXGainsItemDto.PositiveY3P1, adsXGainsItemDto.PositiveY3P2, adsXGainsItemDto.PositiveY3P3, speedValue);
        if (adsXGainsItemDto.NegativeY4P1 != 0) y4 = GetYValue(adsXGainsItemDto.NegativeY4P1, adsXGainsItemDto.NegativeY4P2, adsXGainsItemDto.NegativeY4P3, speedValue);
        if (adsXGainsItemDto.NegativeY5P1 != 0) y5 = GetYValue(adsXGainsItemDto.NegativeY5P1, adsXGainsItemDto.NegativeY5P2, adsXGainsItemDto.NegativeY5P3, speedValue);
        if (adsXGainsItemDto.NegativeY5P1 != 0) y6 = GetYValue(adsXGainsItemDto.NegativeY5P1, adsXGainsItemDto.NegativeY5P2, adsXGainsItemDto.NegativeY5P3, speedValue);
        if (y1 != 0 && y2 != 0 && y3 != 0) InvokeAdsService(() => AdsViewModel.SetSensorYSpeedFeedForwardValue(true, (y1, y2, y3)), cancellationToken);
        if (y4 != 0 && y5 != 0 && y6 != 0) InvokeAdsService(() => AdsViewModel.SetSensorYSpeedFeedForwardValue(false, (y4, y5, y6)), cancellationToken);
    }

    private double GetYValue(double p1, double p2, double p3, double speed)
    {
        return Math.Round(p1 * speed * speed + p2 * speed + p3);
    }

    private string GetYPositiveAndNegativeString(double p)
    {
        if (p > 0) return " + " + p.ToString();
        else return " - " + Math.Abs(p).ToString();
    }

    private void FindAdsYGainZ1Z2Z3(AdsYGainsCacheItem adsYGainsCacheItem, List<List<double>> transBuffer)
    {
        var YSpeedList = transBuffer[7];
        if (YSpeedList.Count == 0 && YSpeedList is null) return;

        var speedChangedList = YSpeedList.ToPoints().Where(t => Math.Round(Math.Abs(t.Y) / adsYGainsCacheItem.SpeedYValue, 2) > 0.5);
        var ySpeedStartIndex = Convert.ToInt32(speedChangedList.First().X);
        var ySpeedEndIndex = Convert.ToInt32(speedChangedList.Last().X);

        var z1List = transBuffer[0];
        var z2List = transBuffer[1];
        var z3List = transBuffer[2];

        adsYGainsCacheItem.SetPlotZ1(z1List);
        adsYGainsCacheItem.SetPlotZ2(z2List);
        adsYGainsCacheItem.SetPlotZ3(z3List);

        var (pointZ1, pointSmoothZ1, smoothZ1, pointZ2, pointSmoothZ2, smoothZ2) = GetadsYGainsValueTwo(adsYGainsCacheItem.GetPlotZ1(), adsYGainsCacheItem.GetPlotZ2(), ySpeedStartIndex, ySpeedEndIndex);
        adsYGainsCacheItem.SetSmoothPlotZ1(smoothZ1);
        adsYGainsCacheItem.SetMaxZ1(pointZ1.Max(t => t.Y));
        adsYGainsCacheItem.SetMinZ1(pointZ1.Min(t => t.Y));
        adsYGainsCacheItem.SetPointZ1(pointZ1);
        adsYGainsCacheItem.SetSmoothPointZ1(pointSmoothZ1);
        adsYGainsCacheItem.SetZ1(pointZ1[1].Y - pointZ1[0].Y);

        adsYGainsCacheItem.SetSmoothPlotZ2(smoothZ2);
        adsYGainsCacheItem.SetMaxZ2(pointZ2.Max(t => t.Y));
        adsYGainsCacheItem.SetMinZ2(pointZ2.Min(t => t.Y));
        adsYGainsCacheItem.SetPointZ2(pointZ2);
        adsYGainsCacheItem.SetSmoothPointZ2(pointSmoothZ2);
        adsYGainsCacheItem.SetZ2(pointZ2[1].Y - pointZ2[0].Y);

        var (pointZ3, pointSmoothZ3, smoothZ3) = GetadsYGainsValue(adsYGainsCacheItem.GetPlotZ3(), ySpeedStartIndex, ySpeedEndIndex);
        adsYGainsCacheItem.SetSmoothPlotZ3(smoothZ3);
        adsYGainsCacheItem.SetPointZ3(pointZ3);
        adsYGainsCacheItem.SetSmoothPointZ3(pointSmoothZ3);
        adsYGainsCacheItem.SetMaxZ3(pointZ3.Max(t => t.Y));
        adsYGainsCacheItem.SetMinZ3(pointZ3.Min(t => t.Y));
        adsYGainsCacheItem.SetZ3(pointZ3[1].Y - pointZ3[0].Y);
        var heightList = transBuffer[3].Skip(ySpeedStartIndex).Take(ySpeedEndIndex - ySpeedStartIndex).ToList();
        var rollList = transBuffer[4].Skip(ySpeedStartIndex).Take(ySpeedEndIndex - ySpeedStartIndex).ToList();
        var pitchList = transBuffer[5].Skip(ySpeedStartIndex).Take(ySpeedEndIndex - ySpeedStartIndex).ToList();
        var heightMax = heightList.Max(Math.Abs);
        var rollMax = rollList.Max(Math.Abs);
        var pitchMax = pitchList.Max(Math.Abs);
        adsYGainsCacheItem.SetH(heightMax);
        adsYGainsCacheItem.SetR(rollMax);
        adsYGainsCacheItem.SetP(pitchMax);
        adsYGainsCacheItem.SetPlotH(heightList);
        adsYGainsCacheItem.SetPlotR(rollList);
        adsYGainsCacheItem.SetPlotP(pitchList);
        adsYGainsCacheItem.SumHRP = heightMax + rollMax + pitchMax;
        if (adsYGainsCacheItem.IsPositive)
        {
            Logger.LogHtmlInformation($"Y1_{adsYGainsCacheItem.GetAdsY1()} Y2_{adsYGainsCacheItem.GetAdsY2()} Y3_{adsYGainsCacheItem.GetAdsY3()}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                Y1 = adsYGainsCacheItem.GetAdsY1(),
                Y2 = adsYGainsCacheItem.GetAdsY2(),
                Y3 = adsYGainsCacheItem.GetAdsY3(),
                MaxValueZ1 = adsYGainsCacheItem.GetMaxZ1(),
                MinValueZ1 = adsYGainsCacheItem.GetMinZ1(),
                Z1 = adsYGainsCacheItem.GetZ1(),
                MaxValueZ2 = adsYGainsCacheItem.GetMaxZ2(),
                MinValueZ2 = adsYGainsCacheItem.GetMinZ2(),
                Z2 = adsYGainsCacheItem.GetZ2(),
                MaxValueZ3 = adsYGainsCacheItem.GetMaxZ3(),
                MinValueZ3 = adsYGainsCacheItem.GetMinZ3(),
                Z3 = adsYGainsCacheItem.GetZ3(),
                HeightMax = heightMax,
                RollMax = rollMax,
                PitchMax = pitchMax,
                ySpeedStartIndex,
                ySpeedEndIndex,
                PlotZ1Z2Z3 = new HtmlPlot2DLinesChart([
                    ("Z1", transBuffer[0].ToPoints()), ("smoothZ1", adsYGainsCacheItem.GetSmoothPlotZ1().ToPoints()),
                    ("Z2", transBuffer[1].ToPoints()), ("smoothZ2", adsYGainsCacheItem.GetSmoothPlotZ2().ToPoints()),
                    ("Z3", transBuffer[2].ToPoints()), ("smoothZ3", adsYGainsCacheItem.GetSmoothPlotZ3().ToPoints()),
                    ("Y Speed", YSpeedList.ToPoints())
                ], "PlotZ1Z2Z3"),
                PlotHRP = new HtmlPlot2DLinesChart([("H", transBuffer[3].ToPoints()), ("R", transBuffer[4].ToPoints()), ("P", transBuffer[5].ToPoints())], "PlotHRP")
            }), HtmlLogUniqueId.LoggingHtml());
        }
        else
        {
            Logger.LogHtmlInformation($"Y4_{adsYGainsCacheItem.GetAdsY1()} Y5_{adsYGainsCacheItem.GetAdsY2()} Y6_{adsYGainsCacheItem.GetAdsY3()}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                Y4 = adsYGainsCacheItem.GetAdsY1(),
                Y5 = adsYGainsCacheItem.GetAdsY2(),
                Y6 = adsYGainsCacheItem.GetAdsY3(),
                MaxValueZ4 = adsYGainsCacheItem.GetMaxZ1(),
                MinValueZ4 = adsYGainsCacheItem.GetMinZ1(),
                Z4 = adsYGainsCacheItem.GetZ1(),
                MaxValueZ5 = adsYGainsCacheItem.GetMaxZ2(),
                MinValueZ5 = adsYGainsCacheItem.GetMinZ2(),
                Z5 = adsYGainsCacheItem.GetZ2(),
                MaxValueZ6 = adsYGainsCacheItem.GetMaxZ3(),
                MinValueZ6 = adsYGainsCacheItem.GetMinZ3(),
                Z6 = adsYGainsCacheItem.GetZ3(),
                HeightMax = heightMax,
                RollMax = rollMax,
                PitchMax = pitchMax,
                PlotZ4Z5Z6 = new HtmlPlot2DLinesChart([
                    ("Z4", transBuffer[0].ToPoints()), ("smoothZ4", adsYGainsCacheItem.GetSmoothPlotZ1().ToPoints()),
                    ("Z5", transBuffer[1].ToPoints()), ("smoothZ5", adsYGainsCacheItem.GetSmoothPlotZ2().ToPoints()),
                    ("Z6", transBuffer[2].ToPoints()), ("smoothZ6", adsYGainsCacheItem.GetSmoothPlotZ3().ToPoints()),
                    ("Y Speed", YSpeedList.ToPoints())
                ], "PlotZ4Z5Z6"),
                PlotHRP = new HtmlPlot2DLinesChart([("H", transBuffer[3].ToPoints()), ("R", transBuffer[4].ToPoints()), ("P", transBuffer[5].ToPoints())], "PlotHRP")
            }), HtmlLogUniqueId.LoggingHtml());
        }
    }

    private async Task<(int y1, int y2, int y3, double Z1, double Z2, double Z3, bool z1IsPositive, bool z2IsPositive, bool z3IsPositive)> DichotomyFindY1Y2Y3Async(bool isPositive, double speedValue, int index, int minY1, int maxY1, int minY2, int maxY2, int minY3,
        int maxY3, CancellationToken cancellationToken)
    {
        var z1IsPositive = false;
        var z2IsPositive = false;
        var z3IsPositive = false;
        var x1Value = (minY1 + maxY1) / 2;
        var x2Value = (minY2 + maxY2) / 2;
        var x3Value = (minY3 + maxY3) / 2;
        var adsYGainsCacheItemTemp = new AdsYGainsCacheItem
        {
            Index = index,
            IsPositive = isPositive,
            SpeedYValue = speedValue
        };
        adsYGainsCacheItemTemp.SetAdsY1(x1Value);
        adsYGainsCacheItemTemp.SetAdsY2(x2Value);
        adsYGainsCacheItemTemp.SetAdsY3(x3Value);
        var (isSuccess, transBuffer) = await GetZ1Z2Z3CurveAsync(adsYGainsCacheItemTemp, cancellationToken).ConfigureAwait(false);
        if (isSuccess == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Get Hrp Failed!"), HtmlLogUniqueId.LoggingHtml());
            return (0, 0, 0, 0, 0, 0, false, false, false);
        }

        FindAdsYGainZ1Z2Z3(adsYGainsCacheItemTemp, transBuffer);
        if (adsYGainsCacheItemTemp.IsPositive)
            SynchronizationContextProvider.Send(() => AdsYGainsCacheItemList.Add(adsYGainsCacheItemTemp));
        else
            SynchronizationContextProvider.Send(() => AdsYGainsCacheConverseItemList.Add(adsYGainsCacheItemTemp));
        z1IsPositive = adsYGainsCacheItemTemp.GetZ1() <= 0;
        z2IsPositive = adsYGainsCacheItemTemp.GetZ2() <= 0;
        z3IsPositive = adsYGainsCacheItemTemp.GetZ3() <= 0;
        return (x1Value, x2Value, x3Value, adsYGainsCacheItemTemp.GetZ1(), adsYGainsCacheItemTemp.GetZ2(), adsYGainsCacheItemTemp.GetZ3(), z1IsPositive, z2IsPositive, z3IsPositive);
    }

    private async Task<(bool, List<(double Height, double Roll, double Pitch, double xSpeed, double ySpeed)>)> GetHrpNewAsync(AdsYGainsCacheItem adsYGainsItemDto, CancellationToken cancellationToken, int repeatCount = 1)
    {
        try
        {
            var transBuffer = new List<(double Height, double Roll, double Pitch, double xSpeed, double ySpeed)>();
            StageViewModel.SetYSpeedValue(Cache.DefaultSpeedYValue);
            if (adsYGainsItemDto.IsPositive)
            {
                StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetStartPosition());
                InvokeAdsService(() => AdsViewModel.SetSensorYSpeedFeedForwardValue(adsYGainsItemDto.IsPositive, (adsYGainsItemDto.GetAdsY1(), adsYGainsItemDto.GetAdsY2(), adsYGainsItemDto.GetAdsY3())), cancellationToken);
                StageViewModel.SetYSpeedValue(adsYGainsItemDto.SpeedYValue);

                await Task.Delay(HostEnvironment.IsDevelopment() ? 1000 : 30000, cancellationToken);
                var task = Task.Run(() => AdsViewModel.GetSensorHeightRollPitchTraceBufferList(TimeSpan.FromSeconds(Cache.WaitTime)));
                await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 3000, cancellationToken);
                StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetEndPosition());
                transBuffer = await task.ConfigureAwait(false);
                StageViewModel.SetXSpeedValue(Cache.DefaultSpeedYValue);
            }
            else
            {
                StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetEndPosition());
                InvokeAdsService(() => AdsViewModel.SetSensorYSpeedFeedForwardValue(adsYGainsItemDto.IsPositive, (adsYGainsItemDto.GetAdsY1(), adsYGainsItemDto.GetAdsY2(), adsYGainsItemDto.GetAdsY3())), cancellationToken);
                StageViewModel.SetYSpeedValue(adsYGainsItemDto.SpeedYValue);

                await Task.Delay(HostEnvironment.IsDevelopment() ? 1000 : 30000, cancellationToken);
                var task = Task.Run(() => AdsViewModel.GetSensorHeightRollPitchTraceBufferList(TimeSpan.FromSeconds(Cache.WaitTime)));
                await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 3000, cancellationToken);
                StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(Cache.GetStartPosition());
                transBuffer = await task.ConfigureAwait(false);
                StageViewModel.SetXSpeedValue(Cache.DefaultSpeedYValue);
            }

            if (transBuffer.Count > 0)
            {
                var YSpeedList = transBuffer.Select(t => t.ySpeed).ToList();
                if (YSpeedList.Count == 0 && YSpeedList is null) return (false, transBuffer);

                var speedChangedList = YSpeedList.ToPoints().Where(t => Math.Round(Math.Abs(t.Y) / adsYGainsItemDto.SpeedYValue, 2) > 0.5);
                var ySpeedStartIndex = Convert.ToInt32(speedChangedList.First().X);
                var ySpeedEndIndex = Convert.ToInt32(speedChangedList.Last().X);

                var heightList = transBuffer.Select(t => t.Height).Skip(ySpeedStartIndex).Take(ySpeedEndIndex - ySpeedStartIndex).ToList();
                var rollList = transBuffer.Select(t => t.Roll).Skip(ySpeedStartIndex).Take(ySpeedEndIndex - ySpeedStartIndex).ToList();
                var pitchList = transBuffer.Select(t => t.Pitch).Skip(ySpeedStartIndex).Take(ySpeedEndIndex - ySpeedStartIndex).ToList();
                var heightMax = heightList.Max(Math.Abs);
                var rollMax = rollList.Max(Math.Abs);
                var pitchMax = pitchList.Max(Math.Abs);
                adsYGainsItemDto.SetPlotH(heightList);
                adsYGainsItemDto.SetPlotP(pitchList);
                adsYGainsItemDto.SetPlotR(rollList);
                adsYGainsItemDto.SetH(heightMax);
                adsYGainsItemDto.SetR(rollMax);
                adsYGainsItemDto.SetP(pitchMax);
                adsYGainsItemDto.SumHRP = heightMax + rollMax + pitchMax;
                var result = heightMax < Cache.VerifyThreshold && rollMax < Cache.VerifyThreshold && pitchMax < Cache.VerifyThreshold;
                var (z1, z2, z3) = AdsViewModel.GetSensorSpeedZ1Z2Z3Value();

                adsYGainsItemDto.SetZ1(z1);
                adsYGainsItemDto.SetZ2(z2);
                adsYGainsItemDto.SetZ3(z3);

                if (adsYGainsItemDto.IsPositive)
                {
                    Logger.LogHtmlInformation(
                        result
                            ? $"V_{adsYGainsItemDto.SpeedYValue} Y1_{adsYGainsItemDto.GetAdsY1().ToString()} Y2_{adsYGainsItemDto.GetAdsY2().ToString()} Y3_{adsYGainsItemDto.GetAdsY3().ToString()} OK"
                            : $"V_{adsYGainsItemDto.SpeedYValue} Y1_{adsYGainsItemDto.GetAdsY1().ToString()} Y2_{adsYGainsItemDto.GetAdsY2().ToString()} Y3_{adsYGainsItemDto.GetAdsY3().ToString()} Failed",
                        HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            adsYGainsItemDto.SpeedYValue,
                            adsYGainsItemDto.IsPositive,
                            Cache.VerifyThreshold,
                            Y1 = adsYGainsItemDto.GetAdsY1(),
                            Y2 = adsYGainsItemDto.GetAdsY2(),
                            Y3 = adsYGainsItemDto.GetAdsY3(),
                            ySpeedStartIndex,
                            ySpeedEndIndex,
                            PlotHrp = new HtmlPlot2DLinesChart([
                                ("H", transBuffer.Select(t => t.Height).ToList().ToPoints()),
                                ("P", transBuffer.Select(t => t.Pitch).ToList().ToPoints()),
                                ("R", transBuffer.Select(t => t.Roll).ToList().ToPoints()),
                                ("S", transBuffer.Select(t => t.ySpeed).ToList().ToPoints())
                            ], "PlotHrp"),
                            HeightMax = heightMax,
                            RollMax = rollMax,
                            PitchMax = pitchMax,
                            PositiveZ1 = z1,
                            PositiveZ2 = z2,
                            PositiveZ3 = z3
                        }), HtmlLogUniqueId.LoggingHtml());
                }
                else
                {
                    Logger.LogHtmlInformation(
                        result
                            ? $"V_{adsYGainsItemDto.SpeedYValue} Y4_{adsYGainsItemDto.GetAdsY1().ToString()} Y5_{adsYGainsItemDto.GetAdsY2().ToString()} Y6_{adsYGainsItemDto.GetAdsY3().ToString()} OK"
                            : $"V_{adsYGainsItemDto.SpeedYValue} Y4_{adsYGainsItemDto.GetAdsY1().ToString()} Y5_{adsYGainsItemDto.GetAdsY2().ToString()} Y6_{adsYGainsItemDto.GetAdsY3().ToString()} Failed",
                        HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            adsYGainsItemDto.SpeedYValue,
                            adsYGainsItemDto.IsPositive,
                            Cache.VerifyThreshold,
                            Y4 = adsYGainsItemDto.GetAdsY1(),
                            Y5 = adsYGainsItemDto.GetAdsY2(),
                            Y6 = adsYGainsItemDto.GetAdsY3(),
                            PlotHrp = new HtmlPlot2DLinesChart([
                                ("H", transBuffer.Select(t => t.Height).ToList().ToPoints()),
                                ("P", transBuffer.Select(t => t.Pitch).ToList().ToPoints()),
                                ("R", transBuffer.Select(t => t.Roll).ToList().ToPoints()),
                                ("S", transBuffer.Select(t => t.ySpeed).ToList().ToPoints())
                            ], "PlotHrp"),
                            HeightMax = heightMax,
                            RollMax = rollMax,
                            PitchMax = pitchMax,
                            PositiveZ4 = z1,
                            PositiveZ5 = z2,
                            PositiveZ6 = z3
                        }), HtmlLogUniqueId.LoggingHtml());
                }

                return (result, transBuffer);
            }

            if (repeatCount > 5) return (false, transBuffer);

            return await GetHrpNewAsync(adsYGainsItemDto, cancellationToken, repeatCount++).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException) throw;
            if (repeatCount > 5) return (false, []);
            return await GetHrpNewAsync(adsYGainsItemDto, cancellationToken, repeatCount++).ConfigureAwait(false);
        }
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

    #endregion 校准

}