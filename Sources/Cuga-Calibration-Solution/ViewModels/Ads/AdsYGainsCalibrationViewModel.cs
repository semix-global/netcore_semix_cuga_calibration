using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models;
using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.Ads.YGains;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Net.Utilities.Algorithm.MathNet.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Extensions;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Behaviors;
using Net.Utilities.WPF.Enums;
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
        new() { StepName = "Y Positive Gains" },
        new() { StepName = "Y Positive HRP" },
        new() { StepName = "Y Negative Gains" },
        new() { StepName = "Y Negative HRP" }
    ];

    #region 界面相关

    #region Calibrate

    private AdsYGainsItemDto? _lastAdsYGainsItemDto;

    [ObservableProperty]
    private ObservableCollection<AdsYGainsItemDto> _yGainsItemDtoList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsCacheItem> _adsYGainsCacheItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsDichotomySpeedCacheItem> _adsYGainsDichotomySpeedCacheItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsCacheItem> _adsYGainsHrpCacheItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsCacheItem> _resultAdsYGainsHrpCacheItemList = [];

    [ObservableProperty]
    private ObservableCollection<AdsYGainsCacheItem> _yGainsCacheItemList = [];

    [ObservableProperty]
    private AdsYGainsItemDto? _selectAdsYGainsItemDto;

    [ObservableProperty]
    private AdsYGainsCacheItem _selectAdsYGainsCacheItem = new();

    [ObservableProperty]
    private AdsYGainsItemDto _resultAdsYGainsItemDto = new();

    [ObservableProperty]
    private List<WpfPlotModel> _plotList = [];

    [ObservableProperty]
    private List<WpfPlotModel> _plotY1Y2Y3List = [];

    [ObservableProperty]
    private List<WpfPlotModel> _plotY4Y5Y6List = [];

    private bool IsY1Stop = false;

    private bool IsY2Stop = false;

    private bool IsY3Stop = false;

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

    [ObservableProperty]
    private AdsYGainsCache _cache = new();

    [ObservableProperty]
    private AdsYGainsItemDto _calibration = new();

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

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<AdsYGainsCache>();
        Calibration = CacheProvider.GetOrDefault<AdsYGainsItemDto>();

        return isHasCache || CacheProvider.Set(Cache, cancellationToken);
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
            case 2:
                Cache.IsPositive = true;
                return true;

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
                SynchronizationContextProvider.Send(() => AdsYGainsDichotomySpeedCacheItemList.Clear());
                Cache.IsPositive = true;
                return true;

            case 1:
                return true;

            case 2:
                Cache.IsPositive = false;
                ClearCalibrationTemp();
                return true;
            case 3:
                return true;
            case 4:
                var isCalibrated = CalibrationStepIndex == 4;

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

                if (IsCalibrated == false) CalibrationStepIndex = -1;
                ClearCalibrationTemp();

                return true;

            default:
                return false;
        }
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
                var plotList = new List<(string Title, Point[] Points)>();
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
                        ("H", adsYGainsItemDto.NegativePlotH.ToPoints()),
                        ("R", adsYGainsItemDto.NegativePlotR.ToPoints()),
                        ("P", adsYGainsItemDto.NegativePlotP.ToPoints())
                    ];
                }

                DialogWindowProvider.ShowPlot(plotList);
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
                Y1 = Cache.Y1,
                Y2 = Cache.Y2,
                Y3 = Cache.Y3,
                Y4 = Cache.Y4,
                Y5 = Cache.Y5,
                Y6 = Cache.Y6,
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
                    IsY1Stop = false;
                    IsY2Stop = false;
                    IsY3Stop = false;
                    var adsYGainsDichotomySpeedCacheItem = new AdsYGainsDichotomySpeedCacheItem()
                    {
                        IsPositive = Cache.IsPositive,
                        SpeedYValue = speedvalue,
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
                        IsPositive = Cache.IsPositive,
                        StartPosition = Cache.GetStartPosition(),
                        EndPosition = Cache.GetEndPosition(),
                        SpeedXValue = speedvalue
                    }), HtmlLogUniqueId.LoggingHtml());
                    var (isSuccess, transBuffer) = await GetZ1Z2Z3CurveAsync(adsYGainsCacheItem).ConfigureAwait(false);
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

                    var y1Min = Cache.FindMinY;
                    var y1Max = Cache.FindMaxY;
                    var y2Min = Cache.FindMinY;
                    var y2Max = Cache.FindMaxY;
                    var y3Min = Cache.FindMinY;
                    var y3Max = Cache.FindMaxY;
                    var Z1List = new List<(int y1, double z1)>();
                    var Z2List = new List<(int y2, double z2)>();
                    var Z3List = new List<(int y3, double z3)>();
                    var isStop = true;
                    var index = 1;
                    while (isStop)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        index++;
                        var resultValue = await DichotomyFindY1Y2Y3Async(Cache.IsPositive, speedvalue, index, y1Min, y1Max, y2Min, y2Max, y3Min, y3Max);
                        Z1List.Add((resultValue.y1, resultValue.Z1));
                        Z2List.Add((resultValue.y2, resultValue.Z2));
                        Z3List.Add((resultValue.y3, resultValue.Z3));
                        if (!IsY1Stop)
                        {
                            if (resultValue.z1IsPositive == z1IsPositive) y1Min = resultValue.y1;
                            else y1Max = resultValue.y1;
                            if (y1Min == y1Max || (y1Min + 1 == y1Max))
                            {
                                var minZ1Item = Z1List.OrderBy(t => Math.Abs(t.z1)).FirstOrDefault();
                                AdsYGainsDichotomySpeedCacheItemList.Single(t => t.SpeedYValue == speedvalue).SetY1(minZ1Item.y1);
                                IsY1Stop = true;
                            }
                        }
                        else
                        {
                            var minZ1Item = Z1List.OrderBy(t => Math.Abs(t.z1)).FirstOrDefault();
                            AdsYGainsDichotomySpeedCacheItemList.Single(t => t.SpeedYValue == speedvalue).SetY1(minZ1Item.y1);
                        }

                        if (!IsY2Stop)
                        {
                            if (resultValue.z2IsPositive == z2IsPositive) y2Min = resultValue.y2;
                            else y2Max = resultValue.y2;
                            if (y2Min == y2Max || (y2Min + 1 == y2Max))
                            {
                                var minZ2Item = Z2List.OrderBy(t => Math.Abs(t.z2)).FirstOrDefault();
                                AdsYGainsDichotomySpeedCacheItemList.Single(t => t.SpeedYValue == speedvalue).SetY2(minZ2Item.y2);
                                IsY2Stop = true;
                            }
                        }
                        else
                        {
                            var minZ2Item = Z2List.OrderBy(t => Math.Abs(t.z2)).FirstOrDefault();
                            AdsYGainsDichotomySpeedCacheItemList.Single(t => t.SpeedYValue == speedvalue).SetY2(minZ2Item.y2);
                        }

                        if (!IsY3Stop)
                        {
                            if (resultValue.z3IsPositive == z3IsPositive) y3Min = resultValue.y3;
                            else y3Max = resultValue.y3;
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

                        if (IsY1Stop && IsY2Stop && IsY3Stop) isStop = false;
                        SetDefaultYValue();
                    }
                }

                if (Cache.IsPositive)
                {
                    Logger.LogHtmlInformation("SpeedBestYValueTable", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        Threshold = Cache.Threshold,
                        SpeedXPositiveTable = new HtmlTable([.. AdsYGainsDichotomySpeedCacheItemList.Select(t => new { t.SpeedYValue, t.PositiveY1, t.PositiveY2, t.PositiveY3 }).Cast<object>()])
                    }), HtmlLogUniqueId.LoggingHtml());
                }
                else
                {
                    Logger.LogHtmlInformation("SpeedBestYValueTable", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        Threshold = Cache.Threshold,
                        SpeedXNegativeTable = new HtmlTable([.. AdsYGainsDichotomySpeedCacheItemList.Select(t => new { t.SpeedYValue, t.NegativeY4, t.NegativeY5, t.NegativeY6 }).Cast<object>()])
                    }), HtmlLogUniqueId.LoggingHtml());
                }
            }
            catch (Exception ex)
            {
                SetDefaultYValue();
                DialogWindowProvider.ShowDialog("Calibrate Z1Z2Z3 Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} {ex.Message}!"), HtmlLogUniqueId.LoggingHtml());
                result = false;
                return result;
            }

            return result;
        }).ConfigureAwait(false);
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        SynchronizationContextProvider.Send(() => AdsYGainsHrpCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => ResultAdsYGainsHrpCacheItemList.Clear());
        var result = true;
        await InvokeCalibrateAsync(async () =>
        {
            try
            {
                foreach (var speedCacheItem in AdsYGainsDichotomySpeedCacheItemList)
                {
                    Logger.LogHtmlInformation($"Param_V{speedCacheItem.SpeedYValue.ToString()}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        speedCacheItem.IsPositive,
                        SpeedXValue = speedCacheItem.SpeedYValue,
                        HrpThreshold = Cache.VerifyThreshold,
                        StartPosition = Cache.GetStartPosition(),
                        EndPosition = Cache.GetEndPosition()
                    }), HtmlLogUniqueId.LoggingHtml());
                    var y1ValueList = new List<double>();
                    var y2ValueList = new List<double>();
                    var y3ValueList = new List<double>();
                    for (var i = -(Cache.Y1Number / 2); i < (Cache.Y1Number / 2) + 1; i++)
                    {
                        y1ValueList.Add(speedCacheItem.GetY1() + i * Cache.FindInterval1);
                    }

                    for (var j = -(Cache.Y2Number / 2); j < (Cache.Y2Number / 2) + 1; j++)
                    {
                        y2ValueList.Add(speedCacheItem.GetY2() + j * Cache.FindInterval2);
                    }

                    for (var k = -(Cache.Y3Number / 2); k < (Cache.Y3Number / 2) + 1; k++)
                    {
                        y3ValueList.Add(speedCacheItem.GetY3() + k * Cache.FindInterval3);
                    }

                    var index = 0;
                    foreach (var y1Value in y1ValueList)
                    {
                        foreach (var y2Value in y2ValueList)
                        {
                            foreach (var y3Value in y3ValueList)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                index++;
                                var adsYGainsHrpCacheItem = new AdsYGainsCacheItem()
                                {
                                    Index = index,
                                    IsPositive = Cache.IsPositive,
                                    SpeedYValue = speedCacheItem.SpeedYValue,
                                };
                                adsYGainsHrpCacheItem.SetAdsY1(y1Value);
                                adsYGainsHrpCacheItem.SetAdsY2(y2Value);
                                adsYGainsHrpCacheItem.SetAdsY3(y3Value);
                                await GetHrpAsync(adsYGainsHrpCacheItem).ConfigureAwait(false);
                                SynchronizationContextProvider.Send(() => AdsYGainsHrpCacheItemList.Add(adsYGainsHrpCacheItem));
                                SetDefaultYValue();
                            }
                        }
                    }

                    var HrpItemList = AdsYGainsHrpCacheItemList.Where(t => t.SpeedYValue == speedCacheItem.SpeedYValue);
                    var bestHrpItem = HrpItemList.OrderBy(t => t.SumHRP).First();
                    SynchronizationContextProvider.Send(() => ResultAdsYGainsHrpCacheItemList.Add(bestHrpItem));
                }

                if (Cache.IsPositive)
                {
                    Logger.LogHtmlInformation("SpeedBestXValueTable", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        Threshold = Cache.Threshold,
                        SpeedXPositiveTable = new HtmlTable([.. ResultAdsYGainsHrpCacheItemList.Select(t => new { t.SpeedYValue, t.PositiveY1, t.PositiveY2, t.PositiveY3 }).Cast<object>()])
                    }), HtmlLogUniqueId.LoggingHtml());
                }
                else
                {
                    Logger.LogHtmlInformation("SpeedBestXValueTable", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        Threshold = Cache.Threshold,
                        SpeedXNegativeTable = new HtmlTable([.. ResultAdsYGainsHrpCacheItemList.Select(t => new { t.SpeedYValue, t.NegativeY4, t.NegativeY5, t.NegativeY6 }).Cast<object>()])
                    }), HtmlLogUniqueId.LoggingHtml());
                }

                var adsYGainsItemDto = new AdsYGainsItemDto()
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
                var (p0, p1, p2, _, yPredicted1) = PolyFit.Poly2Fit(X, Y1);
                var (p3, p4, p5, _, yPredicted2) = PolyFit.Poly2Fit(X, Y2);
                var (p6, p7, p8, _, yPredicted3) = PolyFit.Poly2Fit(X, Y3);
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
                if (Cache.IsPositive) ResultAdsYGainsItemDto.UpdatePositive(adsYGainsItemDto);
                else ResultAdsYGainsItemDto.UpdateNegative(adsYGainsItemDto);
                if (Cache.IsPositive)
                {
                    PlotY1Y2Y3List.Clear();
                    PlotY1Y2Y3List = [.. PlotY1Y2Y3List, new WpfPlotModel("Y1", adsYGainsItemDto.GetPositiveY1Plots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY1Plots().ToArray()])];
                    PlotY1Y2Y3List = [.. PlotY1Y2Y3List, new WpfPlotModel("Y2", adsYGainsItemDto.GetPositiveY2Plots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY2Plots().ToArray()])];
                    PlotY1Y2Y3List = [.. PlotY1Y2Y3List, new WpfPlotModel("Y3", adsYGainsItemDto.GetPositiveY3Plots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY3Plots().ToArray()])];
                    PlotY1Y2Y3List = [.. PlotY1Y2Y3List, new WpfPlotModel("Y1Smooth", adsYGainsItemDto.GetPositiveY1SmoothPlots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY1SmoothPlots().ToArray()])];
                    PlotY1Y2Y3List = [.. PlotY1Y2Y3List, new WpfPlotModel("Y2Smooth", adsYGainsItemDto.GetPositiveY2SmoothPlots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY2SmoothPlots().ToArray()])];
                    PlotY1Y2Y3List = [.. PlotY1Y2Y3List, new WpfPlotModel("Y3Smooth", adsYGainsItemDto.GetPositiveY3SmoothPlots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY3SmoothPlots().ToArray()])];
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
                            ("Y1Plots", ResultAdsYGainsItemDto.PositiveY1Plots.ToArray()), ($"Y1={ResultAdsYGainsItemDto.PositiveY1P1}*V^2{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.PositiveY1P2)}*V{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.PositiveY1P3)}", ResultAdsYGainsItemDto.PositiveY1SmoothPlots.ToArray()),
                            ("Y2Plots", ResultAdsYGainsItemDto.PositiveY2Plots.ToArray()), ($"Y2={ResultAdsYGainsItemDto.PositiveY2P1}*V^2{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.PositiveY2P2)}*V{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.PositiveY2P3)}", ResultAdsYGainsItemDto.PositiveY2SmoothPlots.ToArray()),
                            ("Y3Plots", ResultAdsYGainsItemDto.PositiveY3Plots.ToArray()), ($"Y3={ResultAdsYGainsItemDto.PositiveY3P1}*V^2{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.PositiveY3P2)}*V{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.PositiveY3P3)}", ResultAdsYGainsItemDto.PositiveY3SmoothPlots.ToArray())
                        ], "Y1X2Y3Plots")
                    }), HtmlLogUniqueId.LoggingHtml());
                }
                else
                {
                    PlotY4Y5Y6List.Clear();
                    PlotY4Y5Y6List = [.. PlotY4Y5Y6List, new WpfPlotModel("Y4", adsYGainsItemDto.GetPositiveY1Plots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY1Plots().ToArray()])];
                    PlotY4Y5Y6List = [.. PlotY4Y5Y6List, new WpfPlotModel("Y5", adsYGainsItemDto.GetPositiveY2Plots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY2Plots().ToArray()])];
                    PlotY4Y5Y6List = [.. PlotY4Y5Y6List, new WpfPlotModel("Y6", adsYGainsItemDto.GetPositiveY3Plots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY3Plots().ToArray()])];
                    PlotY4Y5Y6List = [.. PlotY4Y5Y6List, new WpfPlotModel("Y4Smooth", adsYGainsItemDto.GetPositiveY1SmoothPlots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY1SmoothPlots().ToArray()])];
                    PlotY4Y5Y6List = [.. PlotY4Y5Y6List, new WpfPlotModel("Y5Smooth", adsYGainsItemDto.GetPositiveY2SmoothPlots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY2SmoothPlots().ToArray()])];
                    PlotY4Y5Y6List = [.. PlotY4Y5Y6List, new WpfPlotModel("Y6Smooth", adsYGainsItemDto.GetPositiveY3SmoothPlots().ToArray(), null, [.. adsYGainsItemDto.GetPositiveY3SmoothPlots().ToArray()])];
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
                            ("Y4Plots", ResultAdsYGainsItemDto.NegativeY4Plots.ToArray()), ($"Y4={ResultAdsYGainsItemDto.NegativeY4P1}*V^2{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.NegativeY4P2)}*V{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.NegativeY4P3)}", ResultAdsYGainsItemDto.NegativeY4SmoothPlots.ToArray()),
                            ("Y5Plots", ResultAdsYGainsItemDto.NegativeY5Plots.ToArray()), ($"Y5={ResultAdsYGainsItemDto.NegativeY5P1}*V^2{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.NegativeY5P2)}*V{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.NegativeY5P3)}", ResultAdsYGainsItemDto.NegativeY5SmoothPlots.ToArray()),
                            ("Y6Plots", ResultAdsYGainsItemDto.NegativeY6Plots.ToArray()), ($"Y6={ResultAdsYGainsItemDto.NegativeY6P1}*V^2{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.NegativeY6P2)}*V{GetYPositiveAndNegativeString(ResultAdsYGainsItemDto.NegativeY6P3)}", ResultAdsYGainsItemDto.NegativeY6SmoothPlots.ToArray())
                        ], "Y4X5Y6Plots")
                    }), HtmlLogUniqueId.LoggingHtml());
                }
            }
            catch (Exception ex)
            {
                SetDefaultYValue();
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
            SetDefaultYValue();
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

            SetBestY1Y2Y3Values(selectItemDto, Cache.DefaultSpeedXValue);
            if (!IsAutoCalibrate)
            {
                DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);
            }
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
                        selectItemDto.PositiveY3P3,
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
                        selectItemDto.NegativeY6P3,
                    }), HtmlLogUniqueId.LoggingHtml());
                var verifySpeedValueList = Cache.SpeedYValueList.Zip(Cache.SpeedYValueList.Skip(1), (t1, t2) => (t1 + t2) / 2).ToList();
                foreach (var speedvalueItem in verifySpeedValueList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    index++;
                    var adsYGainsHrpCacheItem = new AdsYGainsCacheItem()
                    {
                        Index = index,
                        IsPositive = Cache.IsPositive,
                        SpeedYValue = speedvalueItem
                    };
                    adsYGainsHrpCacheItem.SetAdsY1(GetYValue(adsYGainsItemDto.GetY1P1(), adsYGainsItemDto.GetY1P2(), adsYGainsItemDto.GetY1P3(), speedvalueItem));
                    adsYGainsHrpCacheItem.SetAdsY2(GetYValue(adsYGainsItemDto.GetY2P1(), adsYGainsItemDto.GetY2P2(), adsYGainsItemDto.GetY2P3(), speedvalueItem));
                    adsYGainsHrpCacheItem.SetAdsY3(GetYValue(adsYGainsItemDto.GetY3P1(), adsYGainsItemDto.GetY3P2(), adsYGainsItemDto.GetY3P3(), speedvalueItem));
                    (resultTemp, var transBuffer) = await GetHrpAsync(adsYGainsHrpCacheItem).ConfigureAwait(false);
                    if (adsYGainsItemDto.IsPositive) SynchronizationContextProvider.Send(() => PositiveAdsYGainsHrpCacheItemList.Add(adsYGainsHrpCacheItem));
                    else SynchronizationContextProvider.Send(() => NegativeAdsYGainsHrpCacheItemList.Add(adsYGainsHrpCacheItem));
                    if (resultTemp == false) break;
                }
            }
            catch (Exception ex)
            {
                SetDefaultYValue();
                DialogWindowProvider.ShowDialog("Verify Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} {ex.Message}!"), HtmlLogUniqueId.LoggingHtml());
                resultTemp = false;
                return resultTemp;
            }

            return resultTemp;
        }
    }

    private (List<Point> pointZ, List<Point> pointSmoothZ, List<double> smoothZ) GetadsYGainsValue(List<double> PonitZ, int pointCount)
    {
        var sgolayfiltListZ = SavitzkyGolayFilter.Smooth(3, 51, MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(PonitZ));

        var x = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfEnumerable(Enumerable.Range(1, sgolayfiltListZ.Count).Select(x => (double)x));

        var (p0, p1, p2, p3, p4, p5, _, yPredictedZ) = PolyFit.Poly5Fit(x, sgolayfiltListZ);

        List<double> smoothZ = [.. yPredictedZ];

        // 定义五次多项式的系数 [a5, a4, a3, a2, a1, a0]
        double[] coefficientsZ = [p0, p1, p2, p3, p4, p5]; // 示例系数

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

    private async Task<(bool, List<List<double>>)> GetZ1Z2Z3CurveAsync(AdsYGainsCacheItem adsYGainsCacheItem, int repeatCount = 1)
    {
        var transBuffer = new List<List<double>>();
        try
        {
            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.GetStartPosition(), false);
            AdsViewModel.SetSensorYSpeedFeedForwardValue(Cache.IsPositive, (adsYGainsCacheItem.GetAdsY1(), adsYGainsCacheItem.GetAdsY2(), adsYGainsCacheItem.GetAdsY3()));
            StageViewModel.SetYSpeedValue(adsYGainsCacheItem.SpeedYValue);
            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.GetStartPosition(), false);
            Thread.Sleep(HostEnvironment.IsDevelopment() ? 1000 : 20000);
            var task = Task.Run(() => AdsViewModel.GetSensorSpeedZ1Z2Z3TraceBufferList(TimeSpan.FromSeconds(Cache.WaitTime)));
            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.GetEndPosition(), false);
            transBuffer = await task.ConfigureAwait(false);
            StageViewModel.SetXSpeedValue(Cache.DefaultSpeedYValue);
            if (transBuffer.Count > 0) return (true, transBuffer);
            if (repeatCount > 5) return (false, transBuffer);
            return await GetZ1Z2Z3CurveAsync(adsYGainsCacheItem, repeatCount++).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (repeatCount > 5) return (false, transBuffer);
            return await GetZ1Z2Z3CurveAsync(adsYGainsCacheItem, repeatCount++).ConfigureAwait(false);
        }
    }

    private async Task<(bool, List<(double Height, double Roll, double Pitch, double xSpeed, double ySpeed)>)> GetHrpAsync(AdsYGainsCacheItem adsYGainsItemDto, int repeatCount = 1)
    {
        try
        {
            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.GetStartPosition(), false);
            AdsViewModel.SetSensorYSpeedFeedForwardValue(adsYGainsItemDto.IsPositive, (adsYGainsItemDto.GetAdsY1(), adsYGainsItemDto.GetAdsY2(), adsYGainsItemDto.GetAdsY3()));
            StageViewModel.SetYSpeedValue(adsYGainsItemDto.SpeedYValue);
            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.GetStartPosition(), false);
            Thread.Sleep(HostEnvironment.IsDevelopment() ? 1000 : 12000);
            var task = Task.Run(() => AdsViewModel.GetSensorHeightRollPitchTraceBufferList(TimeSpan.FromSeconds(Cache.WaitTime)));
            StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.GetEndPosition(), false);
            var transBuffer = await task.ConfigureAwait(false);
            StageViewModel.SetXSpeedValue(Cache.DefaultSpeedYValue);
            if (transBuffer.Count > 0)
            {
                var YSpeedList = transBuffer.Select(t => t.ySpeed).ToList();
                if (YSpeedList.Count == 0 && YSpeedList is null) return (false, transBuffer);
                var ySpeedStartIndex = 0;
                var ySpeedEndIndex = 0;
                foreach (var (itemSpeed, index) in YSpeedList.Select((t, index) => (t, index)))
                {
                    if (Math.Round(itemSpeed / adsYGainsItemDto.SpeedYValue, 2) > 0.5)
                    {
                        ySpeedStartIndex = index;
                        break;
                    }
                }
                YSpeedList.Reverse();
                foreach (var (itemSpeed, index) in YSpeedList.Select((t, index) => (t, index)))
                {
                    if (Math.Round(itemSpeed / adsYGainsItemDto.SpeedYValue, 2) > 0.5)
                    {
                        ySpeedEndIndex = index;
                        break;
                    }
                }
                var heightList = transBuffer.Select(t => t.Height).Skip(ySpeedStartIndex).SkipLast(ySpeedEndIndex).ToList();
                var rollList = transBuffer.Select(t => t.Roll).Skip(ySpeedStartIndex).SkipLast(ySpeedEndIndex).ToList();
                var pitchList = transBuffer.Select(t => t.Pitch).Skip(ySpeedStartIndex).SkipLast(ySpeedEndIndex).ToList();
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
                        result ? $"V_{adsYGainsItemDto.SpeedYValue} Y1_{adsYGainsItemDto.GetAdsY1().ToString()} Y2_{adsYGainsItemDto.GetAdsY2().ToString()} Y3_{adsYGainsItemDto.GetAdsY3().ToString()} OK" : $"V_{adsYGainsItemDto.SpeedYValue} Y1_{adsYGainsItemDto.GetAdsY1().ToString()} Y2_{adsYGainsItemDto.GetAdsY2().ToString()} Y3_{adsYGainsItemDto.GetAdsY3().ToString()} Failed",
                        HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            adsYGainsItemDto.SpeedYValue,
                            adsYGainsItemDto.IsPositive,
                            Cache.VerifyThreshold,
                            Y1 = adsYGainsItemDto.GetAdsY1(),
                            Y2 = adsYGainsItemDto.GetAdsY2(),
                            Y3 = adsYGainsItemDto.GetAdsY3(),
                            PlotHrp = new HtmlPlot2DLinesChart([
                                ("H", adsYGainsItemDto.GetPlotH().ToPoints()),
                        ("P", adsYGainsItemDto.GetPlotP().ToPoints()),
                        ("R", adsYGainsItemDto.GetPlotR().ToPoints())
                            ], "PlotHrp"),
                            HeightMax = heightMax,
                            RollMax = rollMax,
                            PitchMax = pitchMax,
                            PositiveZ1 = z1,
                            PositiveZ2 = z2,
                            PositiveZ3 = z3,
                        }), HtmlLogUniqueId.LoggingHtml());
                }
                else
                {
                    Logger.LogHtmlInformation(
                        result ? $"V_{adsYGainsItemDto.SpeedYValue} Y4_{adsYGainsItemDto.GetAdsY1().ToString()} Y5_{adsYGainsItemDto.GetAdsY2().ToString()} Y6_{adsYGainsItemDto.GetAdsY3().ToString()} OK" : $"V_{adsYGainsItemDto.SpeedYValue} Y4_{adsYGainsItemDto.GetAdsY1().ToString()} Y5_{adsYGainsItemDto.GetAdsY2().ToString()} Y6_{adsYGainsItemDto.GetAdsY3().ToString()} Failed",
                        HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                        {
                            adsYGainsItemDto.SpeedYValue,
                            adsYGainsItemDto.IsPositive,
                            Cache.VerifyThreshold,
                            Y4 = adsYGainsItemDto.GetAdsY1(),
                            Y5 = adsYGainsItemDto.GetAdsY2(),
                            Y6 = adsYGainsItemDto.GetAdsY3(),
                            PlotHrp = new HtmlPlot2DLinesChart([
                                ("H", adsYGainsItemDto.GetPlotH().ToPoints()),
                        ("P", adsYGainsItemDto.GetPlotP().ToPoints()),
                        ("R", adsYGainsItemDto.GetPlotR().ToPoints())
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

            return await GetHrpAsync(adsYGainsItemDto, repeatCount++).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (repeatCount > 5) return (false, new List<(double Height, double Roll, double Pitch, double xSpeed, double ySpeed)>());
            return await GetHrpAsync(adsYGainsItemDto, repeatCount++).ConfigureAwait(false);
        }

    }

    private bool Save(AdsYGainsItemDto itemDto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibration = itemDto.Clone();

        return CacheProvider.Set(Calibration, cancellationToken) && CacheProvider.Set(Cache, cancellationToken);
    });

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(() => AdsYGainsCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsYGainsDichotomySpeedCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => AdsYGainsHrpCacheItemList.Clear());
        SynchronizationContextProvider.Send(() => ResultAdsYGainsHrpCacheItemList.Clear());
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

    private void SetDefaultYValue()
    {
        StageViewModel.SetYSpeedValue(Cache.DefaultSpeedYValue);
        AdsViewModel.SetSensorYSpeedFeedForwardValue(false, (Cache.Y4, Cache.Y5, Cache.Y6));
        AdsViewModel.SetSensorYSpeedFeedForwardValue(true, (Cache.Y1, Cache.Y2, Cache.Y3));
    }

    private void SetBestY1Y2Y3Values(AdsYGainsItemDto adsXGainsItemDto, double speedValue)
    {
        StageViewModel.SetXSpeedValue(speedValue);
        double y1 = 0d, y2 = 0d, y3 = 0d, y4 = 0d, y5 = 0d, y6 = 0d;
        if (adsXGainsItemDto.PositiveY1P1 != 0) y1 = GetYValue(adsXGainsItemDto.PositiveY1P1, adsXGainsItemDto.PositiveY1P2, adsXGainsItemDto.PositiveY1P3, speedValue);
        if (adsXGainsItemDto.PositiveY2P1 != 0) y2 = GetYValue(adsXGainsItemDto.PositiveY2P1, adsXGainsItemDto.PositiveY2P2, adsXGainsItemDto.PositiveY2P3, speedValue);
        if (adsXGainsItemDto.PositiveY3P1 != 0) y3 = GetYValue(adsXGainsItemDto.PositiveY3P1, adsXGainsItemDto.PositiveY3P2, adsXGainsItemDto.PositiveY3P3, speedValue);
        if (adsXGainsItemDto.NegativeY4P1 != 0) y4 = GetYValue(adsXGainsItemDto.NegativeY4P1, adsXGainsItemDto.NegativeY4P2, adsXGainsItemDto.NegativeY4P3, speedValue);
        if (adsXGainsItemDto.NegativeY5P1 != 0) y5 = GetYValue(adsXGainsItemDto.NegativeY5P1, adsXGainsItemDto.NegativeY5P2, adsXGainsItemDto.NegativeY5P3, speedValue);
        if (adsXGainsItemDto.NegativeY5P1 != 0) y6 = GetYValue(adsXGainsItemDto.NegativeY5P1, adsXGainsItemDto.NegativeY5P2, adsXGainsItemDto.NegativeY5P3, speedValue);
        if (y1 != 0 && y2 != 0 && y3 != 0) AdsViewModel.SetSensorYSpeedFeedForwardValue(true, (y1, y2, y3));
        if (y4 != 0 && y5 != 0 && y6 != 0) AdsViewModel.SetSensorYSpeedFeedForwardValue(false, (y4, y5, y6));
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
        var ySpeedStartIndex = 0;
        var ySpeedEndIndex = 0;
        foreach (var (itemSpeed, index) in YSpeedList.Select((t, index) => (t, index)))
        {
            if (Math.Round(itemSpeed / adsYGainsCacheItem.SpeedYValue, 2) > 0.5)
            {
                ySpeedStartIndex = index;
                break;
            }
        }
        YSpeedList.Reverse();
        foreach (var (itemSpeed, index) in YSpeedList.Select((t, index) => (t, index)))
        {
            if (Math.Round(itemSpeed / adsYGainsCacheItem.SpeedYValue, 2) > 0.5)
            {
                ySpeedEndIndex = index;
                break;
            }
        }
        var z1List = transBuffer[0].SkipLast(ySpeedEndIndex).ToList();
        var z2List = transBuffer[1].SkipLast(ySpeedEndIndex).ToList();
        var z3List = transBuffer[2].SkipLast(ySpeedEndIndex).ToList();
        adsYGainsCacheItem.SetPlotZ1(z1List);
        adsYGainsCacheItem.SetPlotZ2(z2List);
        adsYGainsCacheItem.SetPlotZ3(z3List);
        var (pointZ1, pointSmoothZ1, smoothZ1) = GetadsYGainsValue(adsYGainsCacheItem.GetPlotZ1(), ySpeedStartIndex);
        adsYGainsCacheItem.SetSmoothPlotZ1(smoothZ1);
        adsYGainsCacheItem.SetMaxZ1(pointZ1.Max(t => t.Y));
        adsYGainsCacheItem.SetMinZ1(pointZ1.Min(t => t.Y));
        adsYGainsCacheItem.SetPointZ1(pointZ1);
        adsYGainsCacheItem.SetSmoothPointZ1(pointSmoothZ1);
        adsYGainsCacheItem.SetZ1(pointZ1[1].Y - pointZ1[0].Y);
        var (pointZ2, pointSmoothZ2, smoothZ2) = GetadsYGainsValue(adsYGainsCacheItem.GetPlotZ2(), ySpeedStartIndex);
        adsYGainsCacheItem.SetSmoothPlotZ2(smoothZ2);
        adsYGainsCacheItem.SetMaxZ2(pointZ2.Max(t => t.Y));
        adsYGainsCacheItem.SetMinZ2(pointZ2.Min(t => t.Y));
        adsYGainsCacheItem.SetPointZ2(pointZ2);
        adsYGainsCacheItem.SetSmoothPointZ2(pointSmoothZ2);
        adsYGainsCacheItem.SetZ2(pointZ2[1].Y - pointZ2[0].Y);
        var (pointZ3, pointSmoothZ3, smoothZ3) = GetadsYGainsValue(adsYGainsCacheItem.GetPlotZ3(), ySpeedStartIndex);
        adsYGainsCacheItem.SetSmoothPlotZ3(smoothZ3);
        adsYGainsCacheItem.SetPointZ3(pointZ3);
        adsYGainsCacheItem.SetSmoothPointZ3(pointSmoothZ3);
        adsYGainsCacheItem.SetMaxZ3(pointZ3.Max(t => t.Y));
        adsYGainsCacheItem.SetMinZ3(pointZ3.Min(t => t.Y));
        adsYGainsCacheItem.SetZ3(pointZ3[1].Y - pointZ3[0].Y);
        var heightList = transBuffer[3].Skip(ySpeedStartIndex).SkipLast(ySpeedEndIndex).ToList();
        var rollList = transBuffer[4].Skip(ySpeedStartIndex).SkipLast(ySpeedEndIndex).ToList();
        var pitchList = transBuffer[5].Skip(ySpeedStartIndex).SkipLast(ySpeedEndIndex).ToList();
        var heightMax = heightList.Max(Math.Abs);
        var rollMax = rollList.Max(Math.Abs);
        var pitchMax = pitchList.Max(Math.Abs);
        adsYGainsCacheItem.SetH(heightMax);
        adsYGainsCacheItem.SetR(rollMax);
        adsYGainsCacheItem.SetP(pitchMax);
        adsYGainsCacheItem.SetPlotH(heightList);
        adsYGainsCacheItem.SetPlotR(rollList);
        adsYGainsCacheItem.SetPlotP(pitchList);
        if (Cache.IsPositive)
        {
            Logger.LogHtmlInformation($"Y1_{adsYGainsCacheItem.GetAdsY1()} Y2_{adsYGainsCacheItem.GetAdsY1()} Y3_{adsYGainsCacheItem.GetAdsY3()}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
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
                PlotZ1Z2Z3 = new HtmlPlot2DLinesChart([
                    ("Z1", adsYGainsCacheItem.GetPlotZ1().ToPoints()), ("smoothZ1", adsYGainsCacheItem.GetSmoothPlotZ1().ToPoints()),
                    ("Z2", adsYGainsCacheItem.GetPlotZ2().ToPoints()), ("smoothZ2", adsYGainsCacheItem.GetSmoothPlotZ2().ToPoints()),
                    ("Z3", adsYGainsCacheItem.GetPlotZ3().ToPoints()), ("smoothZ3", adsYGainsCacheItem.GetSmoothPlotZ3().ToPoints())
                ], "PlotZ1Z2Z3"),
                PlotHRP = new HtmlPlot2DLinesChart([("H", transBuffer[3].ToPoints()), ("R", transBuffer[4].ToPoints()), ("P", transBuffer[5].ToPoints())], "PlotHRP")
            }), HtmlLogUniqueId.LoggingHtml());
        }
        else
        {
            Logger.LogHtmlInformation($"Y4_{adsYGainsCacheItem.GetAdsY1()} Y5_{adsYGainsCacheItem.GetAdsY1()} Y6_{adsYGainsCacheItem.GetAdsY3()}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
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
                    ("Z4", adsYGainsCacheItem.GetPlotZ1().ToPoints()), ("smoothZ4", adsYGainsCacheItem.GetSmoothPlotZ1().ToPoints()),
                    ("Z5", adsYGainsCacheItem.GetPlotZ2().ToPoints()), ("smoothZ5", adsYGainsCacheItem.GetSmoothPlotZ2().ToPoints()),
                    ("Z6", adsYGainsCacheItem.GetPlotZ3().ToPoints()), ("smoothZ6", adsYGainsCacheItem.GetSmoothPlotZ3().ToPoints())
                ], "PlotZ4Z5Z6"),
                PlotHRP = new HtmlPlot2DLinesChart([("H", transBuffer[3].ToPoints()), ("R", transBuffer[4].ToPoints()), ("P", transBuffer[5].ToPoints())], "PlotHRP")
            }), HtmlLogUniqueId.LoggingHtml());
        }
    }

    private async Task<(int y1, int y2, int y3, double Z1, double Z2, double Z3, bool z1IsPositive, bool z2IsPositive, bool z3IsPositive)> DichotomyFindY1Y2Y3Async(bool isPositive, double speedValue, int index, int minY1, int maxY1, int minY2, int maxY2, int minY3, int maxY3)
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
            SpeedYValue = speedValue,
        };
        adsYGainsCacheItemTemp.SetAdsY1(x1Value);
        adsYGainsCacheItemTemp.SetAdsY2(x2Value);
        adsYGainsCacheItemTemp.SetAdsY3(x3Value);
        var (isSuccess, transBuffer) = await GetZ1Z2Z3CurveAsync(adsYGainsCacheItemTemp).ConfigureAwait(false);
        if (isSuccess == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Get Hrp Failed!"), HtmlLogUniqueId.LoggingHtml());
            return (0, 0, 0, 0, 0, 0, false, false, false);
        }

        FindAdsYGainZ1Z2Z3(adsYGainsCacheItemTemp, transBuffer);
        SynchronizationContextProvider.Send(() => AdsYGainsCacheItemList.Add(adsYGainsCacheItemTemp));
        z1IsPositive = adsYGainsCacheItemTemp.GetZ1() <= 0;
        z2IsPositive = adsYGainsCacheItemTemp.GetZ2() <= 0;
        z3IsPositive = adsYGainsCacheItemTemp.GetZ3() <= 0;
        return (x1Value, x2Value, x3Value, adsYGainsCacheItemTemp.GetZ1(), adsYGainsCacheItemTemp.GetZ2(), adsYGainsCacheItemTemp.GetZ3(), z1IsPositive, z2IsPositive, z3IsPositive);
    }

    #endregion 校准

    #region 自动化校准

    public override void GetAutoCalibrationStep()
    {
        AutoCalibrationStepList =
        [
            new() { StepName = "Loading" },
            new() { StepName = "Y Positive Gains" },
            new() { StepName = "Y Positive HPR" },
            new() { StepName = "Y Negative Gains" },
            new() { StepName = "Y Negative  HPR" },
            new() { StepName = "Review" }
        ];
    }

    public override async Task<bool> AutomationActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            GetAutoCalibrationStep();
            await base.AutomationActionAsync(cancellationToken);
            var result = false;
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
                        if (await Step1CalibrateActionAsync(cancellationToken) == false) return false;
                        if (await AutoNextingAsync(cancellationToken) == false) return false;
                        break;

                    case 4:
                        if (await Step2CalibrateActionAsync(cancellationToken) == false) return false;
                        if (await NextingAsync(cancellationToken) == false) return false;
                        if (await AutoNextingAsync(cancellationToken) == false) return false;
                        break;

                    case 5:
                        if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false) return false;
                        await InvokeCalibrateAsync(async () =>
                        {
                            if (SelectReviewItemDto is not null && await VerifyCaibrationAsync(SelectReviewItemDto, cancellationToken) == false)
                            {
                                DialogWindowProvider.ShowDialog($"Auto Calibration Review  Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                return false;
                            }

                            result = true;
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
                if (await VerifyCaibrationAsync(SelectReviewItemDto!, cancellationToken) == false)
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