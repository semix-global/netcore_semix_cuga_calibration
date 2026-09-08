using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Exceptions;
using Core.Models.Models;
using Core.Models.Models.Ads.YGains;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using static Core.Models.Models.Ads.YGains.AdsYGainsDTO;

namespace CugaCalibration.ViewModels.Ads;

[IOCAppService(ServiceType = typeof(AdsYGainsCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AdsYGainsCalibrationViewModel : CalibrationViewModelBase<AdsYGainsCache>
{
    #region 属性

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select a location" },
        new() { StepName = "Y Positive And Negative Gains" },
        new() { StepName = "Y Positive And Negative HRP" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial AdsYGainsDTO Calibrating { get; set; } = new();

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    public partial AdsYGainsDTO ReviewDTO { get; set; } = new();

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial AdsYGainsCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial AdsYGainsDTO Calibration { get; set; } = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Cache = ApplicationCookieService.GetCache<AdsYGainsCache>(cancellationToken);
        Calibration = ApplicationCookieService.GetCalibration<AdsYGainsDTO>(cancellationToken);

        UpdateEntryStatus(Calibration, cancellationToken);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        ReviewDTO = Calibration.Clone();

        return ReviewDTO.IsCalibrated;
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

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.StartPosition,
                Cache.EndPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var (iniY1, iniY2, iniY3) = AdsViewModel.GetSensorYSpeedFeedForwardValue(true);
            var (iniY4, iniY5, iniY6) = AdsViewModel.GetSensorYSpeedFeedForwardValue(false);
            try
            {
                Calibrating.CalibrateItems = [];
                foreach (var speedValue in Cache.YSpeeds)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Logger.LogHtmlInformation($"Speed{speedValue}mm/s", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        Cache.FindMinY,
                        Cache.FindMaxY,
                        SpeedYValue = speedValue
                    }), HtmlLogUniqueId.LoggingHtml());

                    var forwardOriginItem = new AdsYGainsDTOItem { IsPositive = true, SpeedYValue = speedValue, Y1OrY4 = Cache.FindMinY, Y2OrY5 = Cache.FindMinY, Y3OrY6 = Cache.FindMinY };
                    await GetAndSetDataAsync(forwardOriginItem, cancellationToken);
                    LogDichotomy(forwardOriginItem);
                    var reverseOriginItem = new AdsYGainsDTOItem { IsPositive = false, SpeedYValue = speedValue, Y1OrY4 = Cache.FindMinY, Y2OrY5 = Cache.FindMinY, Y3OrY6 = Cache.FindMinY };
                    await GetAndSetDataAsync(reverseOriginItem, cancellationToken);
                    LogDichotomy(reverseOriginItem);

                    Calibrating.CalibrateItems =
                    [
                        .. Calibrating.CalibrateItems,
                        forwardOriginItem,
                        reverseOriginItem
                    ];

                    await DichotomyFindY1Y2Y3Async(forwardOriginItem, reverseOriginItem, cancellationToken);

                    LogYTable($"Speed{speedValue}Y1Y2Y3Table", HtmlHeaderLevelEnum.Header4, Calibrating.CalibrateItems.Where(t => t.IsPositive && t.SpeedYValue.Equals(speedValue)).ToArray());
                    LogYTable($"Speed{speedValue}Y4Y5Y6Table", HtmlHeaderLevelEnum.Header4, Calibrating.CalibrateItems.Where(t => t.IsPositive == false && t.SpeedYValue.Equals(speedValue)).ToArray());
                }

                return true;
            }
            catch (Exception ex)
            {
                DialogWindowProvider.ShowDialog("Calibrate Z1Z2Z3 Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlError(ex, "Error", HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} {ex.Message}!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
            finally
            {
                AdsViewModel.SetSensorYSpeedFeedForwardValue(true, (iniY1, iniY2, iniY3));
                AdsViewModel.SetSensorYSpeedFeedForwardValue(false, (iniY4, iniY5, iniY6));
                Logger.LogHtmlInformation(HtmlLogUniqueId.LoggingPeekHtml());
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            try
            {
                Calibrating.IsCalibrated = true;

                var bestList = Calibrating.CalibrateItems
                    .Where(t => t.H < Cache.Threshold && t.R < Cache.Threshold && t.P < Cache.Threshold)
                    .GroupBy(t => (t.IsPositive, t.SpeedYValue))
                    .Select(g => g.OrderBy(t =>
                    {
                        double[] arr = [t.H, t.R, t.P];
                        var mean = arr.Average();
                        var varVal = arr.PopulationVariance();
                        return mean + varVal;
                    }).First()).ToList();

                var lookup = bestList.ToLookup(t => t.IsPositive);
                Calibrating.BestY1Y2Y3Items = lookup[true].OrderBy(t => t.SpeedYValue).ToArray();
                Calibrating.BestY4Y5Y6Items = lookup[false].OrderBy(t => t.SpeedYValue).ToArray();

                LogYTable("BestY1Y2Y3ValueTable", HtmlHeaderLevelEnum.Header3, Calibrating.BestY1Y2Y3Items);
                LogYTable("BestY4Y5Y6ValueTable", HtmlHeaderLevelEnum.Header3, Calibrating.BestY4Y5Y6Items);

                LogYHtml(true);
                LogYHtml(false);

                Guard.IsTrue(Save(Calibrating, cancellationToken));
            }
            catch (Exception ex)
            {
                DialogWindowProvider.ShowDialog("Calibrate HRP Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlError(ex, "Error", HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} {ex.Message}!"), HtmlLogUniqueId.LoggingHtml());
                Calibrating.IsCalibrated = false;
            }

            return Calibrating.IsCalibrated;
        });

        void LogYHtml(bool isPositive)
        {
            var (index1, index2, index3, plotSource) = isPositive ? ("1", "2", "3", Calibrating.Y1Y2Y3PlotDataSource) : ("4", "5", "6", Calibrating.Y4Y5Y6PlotDataSource);
            var title = Calibrating.IsCalibrated ? "OK" : "Failed";
            Logger.LogHtmlInformation($"(Y{index1},Y{index2},Y{index3}): " + title, HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                YPlots = plotSource.GetHtmlPlot2DLinesChart(0)
            }), HtmlLogUniqueId.LoggingHtml());
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(async () =>
        {
            var (iniY1, iniY2, iniY3) = AdsViewModel.GetSensorYSpeedFeedForwardValue(true);
            var (iniY4, iniY5, iniY6) = AdsViewModel.GetSensorYSpeedFeedForwardValue(false);
            try
            {
                ReviewDTO.IsVerified = true;
                var verifySpeedValueList = Cache.YSpeeds.Zip(Cache.YSpeeds.Skip(1), (t1, t2) => (t1 + t2) / 2).ToList();
                foreach (var speedValueItem in verifySpeedValueList)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var forwardItem = new AdsYGainsDTOItem
                    {
                        IsPositive = true,
                        SpeedYValue = speedValueItem,
                        Y1OrY4 = (int)GetYValue(ReviewDTO.Y1P0, ReviewDTO.Y1P1, ReviewDTO.Y1P2, speedValueItem),
                        Y2OrY5 = (int)GetYValue(ReviewDTO.Y2P0, ReviewDTO.Y2P1, ReviewDTO.Y2P2, speedValueItem),
                        Y3OrY6 = (int)GetYValue(ReviewDTO.Y3P0, ReviewDTO.Y3P1, ReviewDTO.Y3P2, speedValueItem)
                    };
                    var reverseItem = new AdsYGainsDTOItem
                    {
                        IsPositive = false,
                        SpeedYValue = speedValueItem,
                        Y1OrY4 = (int)GetYValue(ReviewDTO.Y4P0, ReviewDTO.Y4P1, ReviewDTO.Y4P2, speedValueItem),
                        Y2OrY5 = (int)GetYValue(ReviewDTO.Y5P0, ReviewDTO.Y5P1, ReviewDTO.Y5P2, speedValueItem),
                        Y3OrY6 = (int)GetYValue(ReviewDTO.Y6P0, ReviewDTO.Y6P1, ReviewDTO.Y6P2, speedValueItem)
                    };

                    Logger.LogHtmlInformation($"Speed{speedValueItem}mm/s", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        SpeedYValue = speedValueItem,
                        Cache.VerifyThreshold
                    }), HtmlLogUniqueId.LoggingHtml());

                    var result1 = await GetHrpAsync(forwardItem, cancellationToken).ConfigureAwait(false);
                    var result2 = result1 && await GetHrpAsync(reverseItem, cancellationToken).ConfigureAwait(false);

                    ReviewDTO.VerifyItems =
                    [
                        .. ReviewDTO.VerifyItems,
                        forwardItem,
                        reverseItem
                    ];

                    if (result2 == false)
                    {
                        ReviewDTO.IsVerified = false;
                        break;
                    }
                }

                LogYTable("Y1Y2Y3ValueTable", HtmlHeaderLevelEnum.Header3, ReviewDTO.VerifyItems.Where(t => t.IsPositive).ToArray());
                LogYTable("Y4Y5Y6ValueTable", HtmlHeaderLevelEnum.Header3, ReviewDTO.VerifyItems.Where(t => t.IsPositive == false).ToArray());
            }
            catch (Exception ex)
            {
                Logger.LogHtmlError(ex, "Error", HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} {ex.Message}!"), HtmlLogUniqueId.LoggingHtml());
                ReviewDTO.IsVerified = false;
            }
            finally
            {
                AdsViewModel.SetSensorYSpeedFeedForwardValue(true, (iniY1, iniY2, iniY3));
                AdsViewModel.SetSensorYSpeedFeedForwardValue(false, (iniY4, iniY5, iniY6));
            }

            Guard.IsTrue(Save(ReviewDTO, cancellationToken));

            DialogWindowProvider.ShowDialog($"Verify {(ReviewDTO.IsVerified ? "OK" : "Failed")}", DialogButtonsEnum.OK, ReviewDTO.IsVerified ? DialogIconEnum.Information : DialogIconEnum.Warning);
            return ReviewDTO.IsVerified;
        }).ConfigureAwait(false);

        double GetYValue(double p0, double p1, double p2, double speed)
        {
            return Math.Round(p2 * speed * speed + p1 * speed + p0);
        }
    }

    private async Task<(bool, (List<double> Z_ECS0, List<double> Z_ECS1, List<double> Z_ECS2, List<double> Height, List<double> Roll, List<double> Pitch, List<double> X_Speed, List<double> Y_Speed))> GetTraceBuffeAsync(AdsYGainsDTOItem item, CancellationToken cancellationToken, int repeatCount = 1)
    {
        (List<double> Z_ECS0, List<double> Z_ECS1, List<double> Z_ECS2, List<double> Height, List<double> Roll, List<double> Pitch, List<double> X_Speed, List<double> Y_Speed) transBuffer = default;
        try
        {
            var (startPos, endPos) = item.IsPositive ? (Cache.StartPosition, Cache.EndPosition) : (Cache.EndPosition, Cache.StartPosition);
            StageViewModel.SetMachineAbsoluteStageXyByFixedSpeedAndNotAutoFocus(startPos);
            InvokeAdsService(() => AdsViewModel.SetSensorYSpeedFeedForwardValue(item.IsPositive, (item.Y1OrY4, item.Y2OrY5, item.Y3OrY6)), cancellationToken);
            StageViewModel.SetYSpeedValue(item.SpeedYValue);

            using var cancellationTokenSource = new CancellationTokenSource();
            await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 30000, cancellationToken);
            var task = AdsViewModel.GetSensorSpeedZ1Z2Z3TraceBufferListAsync(cancellationTokenSource.Token);
            await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 3000, cancellationToken);
            StageViewModel.SetMachineAbsoluteStageXyByFixedSpeedAndNotAutoFocus(endPos);
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(Cache.WaitTime));
            transBuffer = await task.ConfigureAwait(false);

            if (repeatCount > 5) return (false, transBuffer);

            var buffers = new[] { transBuffer.Z_ECS0, transBuffer.Z_ECS1, transBuffer.Z_ECS2, transBuffer.Height, transBuffer.Roll, transBuffer.Pitch };
            if (buffers.Any(b => b.Count == 0)) return await GetTraceBuffeAsync(item, cancellationToken, repeatCount + 1);
            var dataIsError = buffers.All(b => HasConsecutiveZeros(b, 50));

            if (dataIsError) return await GetTraceBuffeAsync(item, cancellationToken, repeatCount + 1).ConfigureAwait(false);

            return (true, transBuffer);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException) throw;
            if (repeatCount > 5) return (false, transBuffer);

            Logger.LogError(ex, "GetZ1Z2Z3CurveAsync Error!");
            return await GetTraceBuffeAsync(item, cancellationToken, repeatCount + 1).ConfigureAwait(false);
        }

        bool HasConsecutiveZeros(IEnumerable<double> array, int requiredZeros = 10)
        {
            return array
                .SkipWhile(x => x != 0) // 跳过非零部分
                .TakeWhile(x => x == 0) // 取连续的零
                .Count() >= requiredZeros; // 判断数量
        }
    }

    private async Task<bool> GetHrpAsync(AdsYGainsDTOItem item, CancellationToken cancellationToken)
    {
        try
        {
            await GetAndSetDataAsync(item, cancellationToken);
            var result = item.H < Cache.VerifyThreshold && item.R < Cache.VerifyThreshold && item.P < Cache.VerifyThreshold;
            var (z1, z2, z3) = AdsViewModel.GetSensorSpeedZ1Z2Z3Value();

            var (index1, index2, index3) = item.IsPositive ? ("1", "2", "3") : ("4", "5", "6");
            Logger.LogHtmlInformation($"(Y{index1},Y{index2},Y{index3}): ({item.Y1OrY4},{item.Y2OrY5},{item.Y3OrY6})" + (result ? "OK" : "Failed"),
                HtmlHeaderLevelEnum.Header4,
                new HtmlBullet(new
                {
                    Z = $"Z{index1}: {z1:F2} Z{index2}: {z2:F2} Z{index3}: {z3:F2}",
                    HeightMax = item.H,
                    RollMax = item.R,
                    PitchMax = item.P,
                    PlotZ = item.ZPlotDataSource.GetHtmlPlot2DLinesChart(0),
                    PlotHrp = item.HrpPlotDataSource.GetHtmlPlot2DLinesChart(0)
                }), HtmlLogUniqueId.LoggingHtml());

            return result;
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException) throw;
            return false;
        }
    }

    private async Task DichotomyFindY1Y2Y3Async(AdsYGainsDTOItem forwardOriginItem, AdsYGainsDTOItem reverseOriginItem, CancellationToken cancellationToken)
    {
        var (minY1, maxY1, minY2, maxY2, minY3, maxY3) = (Cache.FindMinY, Cache.FindMaxY, Cache.FindMinY, Cache.FindMaxY, Cache.FindMinY, Cache.FindMaxY);
        var (minY4, maxY4, minY5, maxY5, minY6, maxY6) = (Cache.FindMinY, Cache.FindMaxY, Cache.FindMinY, Cache.FindMaxY, Cache.FindMinY, Cache.FindMaxY);
        bool converged1 = false, converged2 = false, converged3 = false, converged4 = false, converged5 = false, converged6 = false;
        var maxIterations = 15;

        var lastZ1 = forwardOriginItem.PlotZ1OrZ4.Max(p => p.Y) - forwardOriginItem.PlotZ1OrZ4.Min(p => p.Y);
        var lastZ2 = forwardOriginItem.PlotZ2OrZ5.Max(p => p.Y) - forwardOriginItem.PlotZ2OrZ5.Min(p => p.Y);
        var lastZ3 = forwardOriginItem.PlotZ3OrZ6.Max(p => p.Y) - forwardOriginItem.PlotZ3OrZ6.Min(p => p.Y);
        var lastZ4 = reverseOriginItem.PlotZ1OrZ4.Max(p => p.Y) - reverseOriginItem.PlotZ1OrZ4.Min(p => p.Y);
        var lastZ5 = reverseOriginItem.PlotZ2OrZ5.Max(p => p.Y) - reverseOriginItem.PlotZ2OrZ5.Min(p => p.Y);
        var lastZ6 = reverseOriginItem.PlotZ3OrZ6.Max(p => p.Y) - reverseOriginItem.PlotZ3OrZ6.Min(p => p.Y);

        while (maxIterations-- > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var forwardItem = new AdsYGainsDTOItem
            {
                IsPositive = true,
                SpeedYValue = forwardOriginItem.SpeedYValue,
                Y1OrY4 = (minY1 + maxY1) / 2,
                Y2OrY5 = (minY2 + maxY2) / 2,
                Y3OrY6 = (minY3 + maxY3) / 2
            };
            var reverseItem = new AdsYGainsDTOItem
            {
                IsPositive = false,
                SpeedYValue = reverseOriginItem.SpeedYValue,
                Y1OrY4 = (minY4 + maxY4) / 2,
                Y2OrY5 = (minY5 + maxY5) / 2,
                Y3OrY6 = (minY6 + maxY6) / 2
            };

            await GetAndSetDataAsync(forwardItem, cancellationToken);
            LogDichotomy(forwardItem);
            await GetAndSetDataAsync(reverseItem, cancellationToken);
            LogDichotomy(reverseItem);

            Calibrating.CalibrateItems =
            [
                .. Calibrating.CalibrateItems,
                forwardItem,
                reverseItem
            ];

            UpdateBounds(forwardOriginItem, forwardItem, y => y.IsUpwardZ1OrZ4, y => y.PlotZ1OrZ4, y => y.Y1OrY4, ref minY1, ref maxY1, ref lastZ1, ref converged1);
            UpdateBounds(forwardOriginItem, forwardItem, y => y.IsUpwardZ2OrZ5, y => y.PlotZ2OrZ5, y => y.Y2OrY5, ref minY2, ref maxY2, ref lastZ2, ref converged2);
            UpdateBounds(forwardOriginItem, forwardItem, y => y.IsUpwardZ3OrZ6, y => y.PlotZ3OrZ6, y => y.Y3OrY6, ref minY3, ref maxY3, ref lastZ3, ref converged3);
            UpdateBounds(reverseOriginItem, reverseItem, y => y.IsUpwardZ1OrZ4, y => y.PlotZ1OrZ4, y => y.Y1OrY4, ref minY4, ref maxY4, ref lastZ4, ref converged4);
            UpdateBounds(reverseOriginItem, reverseItem, y => y.IsUpwardZ2OrZ5, y => y.PlotZ2OrZ5, y => y.Y2OrY5, ref minY5, ref maxY5, ref lastZ5, ref converged5);
            UpdateBounds(reverseOriginItem, reverseItem, y => y.IsUpwardZ3OrZ6, y => y.PlotZ3OrZ6, y => y.Y3OrY6, ref minY6, ref maxY6, ref lastZ6, ref converged6);

            if (converged1 && converged2 && converged3 && converged4 && converged5 && converged6) break;
        }

        void UpdateBounds(
            AdsYGainsDTOItem origin,
            AdsYGainsDTOItem current,
            Func<AdsYGainsDTOItem, bool> isUpwardSelector,
            Func<AdsYGainsDTOItem, IReadOnlyList<Point>> plotSelector,
            Func<AdsYGainsDTOItem, int> xSelector,
            ref int min, ref int max, ref double lastZ, ref bool converged)
        {
            if (converged) return;

            var isSame = isUpwardSelector(origin) == isUpwardSelector(current);
            var currentZ = plotSelector(current).Max(p => p.Y) - plotSelector(current).Min(p => p.Y);
            var isMutated = Math.Abs(currentZ) - Math.Abs(lastZ) > 200;

            switch (isSame, isMutated)
            {
                case (true, true):
                    max = xSelector(current);
                    min = Cache.FindMinY;
                    break;
                case (true, false): min = xSelector(current); break;
                case (false, _): max = xSelector(current); break;
            }

            lastZ = currentZ;

            if (Math.Abs(max - min) <= 1) converged = true;
        }
    }

    private async Task GetAndSetDataAsync(AdsYGainsDTOItem item, CancellationToken cancellationToken)
    {
        var (isSuccess, transBuffer) = await GetTraceBuffeAsync(item, cancellationToken).ConfigureAwait(false);

        if (isSuccess == false) Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Get Hrp Failed!"), HtmlLogUniqueId.LoggingHtml());

        var speedChangedList = transBuffer.Y_Speed.ToPoints().Where(t => Math.Round(Math.Abs(t.Y) / item.SpeedYValue, 2) > 0.5).ToList();

        if (speedChangedList.Count == 0) return;

        var ySpeedStartIndex = Convert.ToInt32(speedChangedList[0].X);
        var ySpeedEndIndex = Convert.ToInt32(speedChangedList[^1].X);

        var listZ1 = transBuffer.Z_ECS0.Take(ySpeedEndIndex).ToList();
        var listZ2 = transBuffer.Z_ECS1.Take(ySpeedEndIndex).ToList();
        var listZ3 = transBuffer.Z_ECS2.Take(ySpeedEndIndex).ToList();

        GetOpeningDirection(item, listZ1, listZ2, listZ3, ySpeedStartIndex);

        item.PlotH = transBuffer.Height.Skip(ySpeedStartIndex).Take(ySpeedEndIndex - ySpeedStartIndex).ToPoints();
        item.PlotR = transBuffer.Roll.Skip(ySpeedStartIndex).Take(ySpeedEndIndex - ySpeedStartIndex).ToPoints();
        item.PlotP = transBuffer.Pitch.Skip(ySpeedStartIndex).Take(ySpeedEndIndex - ySpeedStartIndex).ToPoints();
        item.PlotZ1OrZ4 = transBuffer.Z_ECS0.ToPoints();
        item.PlotZ2OrZ5 = transBuffer.Z_ECS1.ToPoints();
        item.PlotZ3OrZ6 = transBuffer.Z_ECS2.ToPoints();
        item.Speeds = transBuffer.Y_Speed.ToPoints();
    }

    private void GetOpeningDirection(AdsYGainsDTOItem item, List<double> pointZ1, List<double> pointZ2, List<double> pointZ3, int startIndex)
    {
        var beforeZ1OrZ4Avg = pointZ1.Take(startIndex).Average();
        var afterZ1OrZ4Avg = pointZ1.Skip(startIndex).Average();
        item.IsUpwardZ1OrZ4 = afterZ1OrZ4Avg - beforeZ1OrZ4Avg < 0;

        var beforeZ2OrZ5Avg = pointZ2.Take(startIndex).Average();
        var afterZ2OrZ5Avg = pointZ2.Skip(startIndex).Average();
        item.IsUpwardZ2OrZ5 = afterZ2OrZ5Avg - beforeZ2OrZ5Avg < 0;

        var beforeZ3OrZ6Avg = pointZ3.Take(startIndex).Average();
        var afterZ3OrZ6Avg = pointZ3.Skip(startIndex).Average();
        item.IsUpwardZ3OrZ6 = afterZ3OrZ6Avg - beforeZ3OrZ6Avg < 0;
    }

    private void InvokeAdsService(Action action, CancellationToken cancellationToken)
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

        throw new CugaException($"Ads Service Invoke Error! {action.Method.Name}");
    }

    private void LogDichotomy(AdsYGainsDTOItem item)
    {
        var (index1, index2, index3) = item.IsPositive ? ("1", "2", "3") : ("4", "5", "6");
        Logger.LogHtmlInformation($"(Y{index1},Y{index2},Y{index3}): ({item.Y1OrY4},{item.Y2OrY5},{item.Y3OrY6})", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
        {
            HeightMax = item.H,
            RollMax = item.R,
            PitchMax = item.P,
            PlotZ = item.ZPlotDataSource.GetHtmlPlot2DLinesChart(0),
            PlotHRP = item.HrpPlotDataSource.GetHtmlPlot2DLinesChart(0)
        }), HtmlLogUniqueId.LoggingHtml());
    }

    private void LogYTable(string title, HtmlHeaderLevelEnum headerLevel, AdsYGainsDTOItem[] list)
    {
        Logger.LogHtmlInformation(title, headerLevel, new HtmlBullet(new
        {
            Cache.Threshold,
            Table = new HtmlTable(list.Select(item =>
            {
                var y1OrY4 = item.Y1OrY4 + " " + (item.IsUpwardZ1OrZ4 ? "↑" : "↓");
                var y2OrY5 = item.Y2OrY5 + " " + (item.IsUpwardZ2OrZ5 ? "↑" : "↓");
                var y3OrY6 = item.Y3OrY6 + " " + (item.IsUpwardZ3OrZ6 ? "↑" : "↓");
                return new { item.SpeedYValue, y1OrY4, y2OrY5, y3OrY6, item.H, item.R, item.P };
            }).ToList())
        }), HtmlLogUniqueId.LoggingHtml());
    }

    private bool Save(AdsYGainsDTO dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        ApplicationCookieService.SetCalibration(dto, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase calibration, CancellationToken cancellationToken)
    {
        var temp = Guard.IsAssignableToTypeAndReturn<AdsYGainsDTO>(calibration);
        var status = Entry.Status;

        Calibration = temp;

        status.TotalCalibrationCount = 1;
        status.CalibratedCount = Calibration.IsCalibrated ? 1 : 0;
        status.VerifiedCount = Calibration.IsVerified ? 1 : 0;
        status.Details = [];
    }

    #endregion 校准
}