using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Exceptions;
using Core.Models.Models;
using Core.Models.Models.Ads.XGains;
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
using static Core.Models.Models.Ads.XGains.AdsXGainsDTO;

namespace CugaCalibration.ViewModels.Ads;

[IOCAppService(ServiceType = typeof(AdsXGainsCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AdsXGainsCalibrationViewModel : CalibrationViewModelBase<AdsXGainsCache>
{
    #region 属性

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select a location" },
        new() { StepName = "X Positive And Negative Gains" },
        new() { StepName = "X Positive And Negative HPR" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial AdsXGainsDTO Calibrating { get; set; } = new();

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    public partial AdsXGainsDTO ReviewDTO { get; set; } = new();

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial AdsXGainsCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial AdsXGainsDTO Calibration { get; set; } = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Cache = ApplicationCookieService.GetCache<AdsXGainsCache>(cancellationToken);
        Calibration = ApplicationCookieService.GetCalibration<AdsXGainsDTO>(cancellationToken);

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
                Cache.EndPosition,
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var (iniX1, iniX2) = AdsViewModel.GetSensorXSpeedFeedForwardValue(true);
            var (iniX3, iniX4) = AdsViewModel.GetSensorXSpeedFeedForwardValue(false);
            try
            {
                Calibrating.CalibrateItems = [];
                foreach (var speedValue in Cache.XSpeeds)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Logger.LogHtmlInformation($"Speed{speedValue}mm/s", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        Cache.FindMinX,
                        Cache.FindMaxX,
                        SpeedXValue = speedValue
                    }), HtmlLogUniqueId.LoggingHtml());

                    var forwardOriginItem = new AdsXGainsDTOItem { IsPositive = true, SpeedXValue = speedValue, X1OrX3 = Cache.FindMinX, X2OrX4 = Cache.FindMinX, };
                    await GetAndSetDataAsync(forwardOriginItem, cancellationToken);
                    LogDichotomy(forwardOriginItem);
                    var reverseOriginItem = new AdsXGainsDTOItem { IsPositive = false, SpeedXValue = speedValue, X1OrX3 = Cache.FindMinX, X2OrX4 = Cache.FindMinX, };
                    await GetAndSetDataAsync(reverseOriginItem, cancellationToken);
                    LogDichotomy(reverseOriginItem);

                    Calibrating.CalibrateItems =
                    [
                        .. Calibrating.CalibrateItems,
                        forwardOriginItem,
                        reverseOriginItem
                    ];

                    await DichotomyFindX1X2Async(forwardOriginItem, reverseOriginItem, cancellationToken);

                    var x1X2List = Calibrating.CalibrateItems.Where(t => t.IsPositive && t.SpeedXValue.Equals(speedValue)).ToArray();
                    var x3X4List = Calibrating.CalibrateItems.Where(t => t.IsPositive == false & t.SpeedXValue.Equals(speedValue)).ToArray();
                    LogHrpPoint3DChart(x1X2List, true);
                    LogHrpPoint3DChart(x3X4List, false);

                    LogXTable($"Speed{speedValue}X1X2Table", HtmlHeaderLevelEnum.Header4, x1X2List);
                    LogXTable($"Speed{speedValue}X3X4Table", HtmlHeaderLevelEnum.Header4, x3X4List);
                }

                return true;
            }
            catch (Exception ex)
            {
                DialogWindowProvider.ShowDialog("Calibrate Z1Z2 Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlError(ex, "Error", HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: {ex.Message}!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
            finally
            {
                AdsViewModel.SetSensorXSpeedFeedForwardValue(true, (iniX1, iniX2));
                AdsViewModel.SetSensorXSpeedFeedForwardValue(false, (iniX3, iniX4));
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
                    .GroupBy(t => (t.IsPositive, t.SpeedXValue))
                    .Select(g => g.OrderBy(t =>
                    {
                        double[] arr = [t.H, t.R, t.P];
                        var mean = arr.Average();
                        var varVal = arr.PopulationVariance();
                        return mean + varVal;
                    }).First()).ToArray();

                var lookup = bestList.ToLookup(t => t.IsPositive);
                Calibrating.BestX1X2Items = lookup[true].OrderBy(t => t.SpeedXValue).ToArray();
                Calibrating.BestX3X4Items = lookup[false].OrderBy(t => t.SpeedXValue).ToArray();

                LogXTable("BestX1X2ValueTable", HtmlHeaderLevelEnum.Header3, Calibrating.BestX1X2Items);
                LogXTable("BestX3X4ValueTable", HtmlHeaderLevelEnum.Header3, Calibrating.BestX3X4Items);

                LogXHtml(true);
                LogXHtml(false);

                Guard.IsTrue(Save(Calibrating, cancellationToken));
            }
            catch (Exception ex)
            {
                DialogWindowProvider.ShowDialog("Calibrate HRP Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlError(ex, "Error", HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: {ex.Message}!"), HtmlLogUniqueId.LoggingHtml());
                Calibrating.IsCalibrated = false;
            }

            return Calibrating.IsCalibrated;
        });

        void LogXHtml(bool isPositive)
        {
            var (index1, index2, plotSource) = isPositive ? ("1", "2", Calibrating.X1X2PlotDataSource) : ("3", "4", Calibrating.X3X4PlotDataSource);
            var title = Calibrating.IsCalibrated ? "OK" : "Failed";
            Logger.LogHtmlInformation($"(X{index1},X{index2}): " + title, HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                XPlots = plotSource.GetHtmlPlot2DLinesChart(0)
            }), HtmlLogUniqueId.LoggingHtml());
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(async () =>
        {
            var (iniX1, iniX2) = AdsViewModel.GetSensorXSpeedFeedForwardValue(true);
            var (iniX3, iniX4) = AdsViewModel.GetSensorXSpeedFeedForwardValue(false);
            try
            {
                ReviewDTO.IsVerified = true;
                var verifySpeedValueList = Cache.XSpeeds.Zip(Cache.XSpeeds.Skip(1), (t1, t2) => (t1 + t2) / 2).ToList();
                foreach (var speedValueItem in verifySpeedValueList)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var forwardItem = new AdsXGainsDTOItem
                    {
                        IsPositive = true,
                        SpeedXValue = speedValueItem,
                        X1OrX3 = (int)GetXValue(ReviewDTO.X1P0, ReviewDTO.X1P1, ReviewDTO.X1P2, speedValueItem),
                        X2OrX4 = (int)GetXValue(ReviewDTO.X2P0, ReviewDTO.X2P1, ReviewDTO.X2P2, speedValueItem)
                    };
                    var reverseItem = new AdsXGainsDTOItem
                    {
                        IsPositive = false,
                        SpeedXValue = speedValueItem,
                        X1OrX3 = (int)GetXValue(ReviewDTO.X3P0, ReviewDTO.X3P1, ReviewDTO.X3P2, speedValueItem),
                        X2OrX4 = (int)GetXValue(ReviewDTO.X4P0, ReviewDTO.X4P1, ReviewDTO.X4P2, speedValueItem)
                    };

                    Logger.LogHtmlInformation($"Speed{speedValueItem}mm/s", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        SpeedXValue = speedValueItem,
                        Cache.VerifyThreshold,
                    }), HtmlLogUniqueId.LoggingHtml());

                    var result1 = await GetHrpAsync(forwardItem, cancellationToken).ConfigureAwait(false);
                    var result2 = result1 && await GetHrpAsync(reverseItem, cancellationToken).ConfigureAwait(false);

                    ReviewDTO.VerifyItems =
                    [
                        ..ReviewDTO.VerifyItems,
                        forwardItem,
                        reverseItem
                    ];

                    if (result2 == false)
                    {
                        ReviewDTO.IsVerified = false;
                        break;
                    }
                }

                LogXTable("X1X2ValueTable", HtmlHeaderLevelEnum.Header3, ReviewDTO.VerifyItems.Where(t => t.IsPositive).ToArray());
                LogXTable("X3X4ValueTable", HtmlHeaderLevelEnum.Header3, ReviewDTO.VerifyItems.Where(t => t.IsPositive == false).ToArray());
            }
            catch (Exception ex)
            {
                Logger.LogHtmlError(ex, "Error", HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: {ex.Message}!"), HtmlLogUniqueId.LoggingHtml());
                ReviewDTO.IsVerified = false;
            }
            finally
            {
                AdsViewModel.SetSensorXSpeedFeedForwardValue(true, (iniX1, iniX2));
                AdsViewModel.SetSensorXSpeedFeedForwardValue(false, (iniX3, iniX4));
            }

            Guard.IsTrue(Save(ReviewDTO, cancellationToken));

            DialogWindowProvider.ShowDialog($"Verify {(ReviewDTO.IsVerified ? "OK" : "Failed")}", DialogButtonsEnum.OK, ReviewDTO.IsVerified ? DialogIconEnum.Information : DialogIconEnum.Warning);
            return ReviewDTO.IsVerified;
        }).ConfigureAwait(false);

        double GetXValue(double p0, double p1, double p2, double speed)
        {
            return Math.Round(p2 * speed * speed + p1 * speed + p0);
        }
    }

    private async Task<(bool, (List<double> Z_ECS0, List<double> Z_ECS1, List<double> Z_ECS2, List<double> Height, List<double> Roll, List<double> Pitch, List<double> X_Speed, List<double> Y_Speed))> GetTraceBuffeAsync(AdsXGainsDTOItem item, CancellationToken cancellationToken, int repeatCount = 1)
    {
        (List<double> Z_ECS0, List<double> Z_ECS1, List<double> Z_ECS2, List<double> Height, List<double> Roll, List<double> Pitch, List<double> X_Speed, List<double> Y_Speed) transBuffer = default;
        try
        {
            var (startPos, endPos) = item.IsPositive ? (Cache.StartPosition, Cache.EndPosition) : (Cache.EndPosition, Cache.StartPosition);
            StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(startPos);
            InvokeAdsService(() => AdsViewModel.SetSensorXSpeedFeedForwardValue(item.IsPositive, (item.X1OrX3, item.X2OrX4)), cancellationToken);
            StageViewModel.SetXSpeedValue(item.SpeedXValue);

            using var cancellationTokenSource = new CancellationTokenSource();
            await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 30000, cancellationToken);
            var task = AdsViewModel.GetSensorSpeedZ1Z2Z3TraceBufferListAsync(cancellationTokenSource.Token);
            await Task.Delay(HostEnvironment.IsDevelopment() ? 100 : 3000, cancellationToken);
            StageViewModel.SetMachineAbsoluteStageXyByFixedSpeed(endPos);
            cancellationTokenSource.CancelAfter(Cache.WaitTime);
            transBuffer = await task.ConfigureAwait(false);

            if (repeatCount > 5) return (false, transBuffer);

            var buffers = new[] { transBuffer.Z_ECS0, transBuffer.Z_ECS1, transBuffer.Height, transBuffer.Roll, transBuffer.Pitch };
            if (buffers.Any(b => b.Count == 0)) return await GetTraceBuffeAsync(item, cancellationToken, repeatCount + 1);
            var dataIsError = buffers.All(b => HasConsecutiveZeros(b, 50));

            if (dataIsError) return await GetTraceBuffeAsync(item, cancellationToken, repeatCount + 1).ConfigureAwait(false);

            return (true, transBuffer);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException) throw;
            if (repeatCount > 5) return (false, transBuffer);

            Logger.LogError(ex, "GetZ1Z2CurveAsync Error!");
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

    private async Task<bool> GetHrpAsync(AdsXGainsDTOItem item, CancellationToken cancellationToken)
    {
        try
        {
            await GetAndSetDataAsync(item, cancellationToken);
            var result = item.H < Cache.VerifyThreshold && item.R < Cache.VerifyThreshold && item.P < Cache.VerifyThreshold;
            var (z1, z2, _) = AdsViewModel.GetSensorSpeedZ1Z2Z3Value();

            var (index1, index2) = item.IsPositive ? ("1", "2") : ("3", "4");
            Logger.LogHtmlInformation($"(X{index1},X{index2}): ({item.X1OrX3},{item.X2OrX4})" + (result ? "OK" : "Failed"),
                HtmlHeaderLevelEnum.Header4,
                new HtmlBullet(new
                {
                    Z = $"Z{index1}: {z1:F2} Z{index2}: {z2:F2}",
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

    private async Task DichotomyFindX1X2Async(AdsXGainsDTOItem forwardOriginItem, AdsXGainsDTOItem reverseOriginItem, CancellationToken cancellationToken)
    {
        var (minX1, maxX1, minX2, maxX2) = (Cache.FindMinX, Cache.FindMaxX, Cache.FindMinX, Cache.FindMaxX);
        var (minX3, maxX3, minX4, maxX4) = (Cache.FindMinX, Cache.FindMaxX, Cache.FindMinX, Cache.FindMaxX);
        bool converged1 = false, converged2 = false, converged3 = false, converged4 = false;
        var maxIterations = 15;

        var lastZ1 = forwardOriginItem.PlotZ1OrZ3.Max(p => p.Y) - forwardOriginItem.PlotZ1OrZ3.Min(p => p.Y);
        var lastZ2 = forwardOriginItem.PlotZ2OrZ4.Max(p => p.Y) - forwardOriginItem.PlotZ2OrZ4.Min(p => p.Y);
        var lastZ3 = reverseOriginItem.PlotZ1OrZ3.Max(p => p.Y) - reverseOriginItem.PlotZ1OrZ3.Min(p => p.Y);
        var lastZ4 = reverseOriginItem.PlotZ2OrZ4.Max(p => p.Y) - reverseOriginItem.PlotZ2OrZ4.Min(p => p.Y);
        while (maxIterations-- > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var forwardItem = new AdsXGainsDTOItem
            {
                IsPositive = true,
                SpeedXValue = forwardOriginItem.SpeedXValue,
                X1OrX3 = (minX1 + maxX1) / 2,
                X2OrX4 = (minX2 + maxX2) / 2,
            };
            var reverseItem = new AdsXGainsDTOItem
            {
                IsPositive = false,
                SpeedXValue = reverseOriginItem.SpeedXValue,
                X1OrX3 = (minX3 + maxX3) / 2,
                X2OrX4 = (minX4 + maxX4) / 2,
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

            UpdateBounds(forwardOriginItem, forwardItem, x => x.IsUpwardZ1OrZ3, x => x.PlotZ1OrZ3, x => x.X1OrX3, ref minX1, ref maxX1, ref lastZ1, ref converged1);
            UpdateBounds(forwardOriginItem, forwardItem, x => x.IsUpwardZ2OrZ4, x => x.PlotZ2OrZ4, x => x.X2OrX4, ref minX2, ref maxX2, ref lastZ2, ref converged2);
            UpdateBounds(reverseOriginItem, reverseItem, x => x.IsUpwardZ1OrZ3, x => x.PlotZ1OrZ3, x => x.X1OrX3, ref minX3, ref maxX3, ref lastZ3, ref converged3);
            UpdateBounds(reverseOriginItem, reverseItem, x => x.IsUpwardZ2OrZ4, x => x.PlotZ2OrZ4, x => x.X2OrX4, ref minX4, ref maxX4, ref lastZ4, ref converged4);

            if (converged1 && converged2 && converged3 && converged4) break;
        }

        void UpdateBounds(
            AdsXGainsDTOItem origin,
            AdsXGainsDTOItem current,
            Func<AdsXGainsDTOItem, bool> isUpwardSelector,
            Func<AdsXGainsDTOItem, IReadOnlyList<Point>> plotSelector,
            Func<AdsXGainsDTOItem, int> xSelector,
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
                    min = Cache.FindMinX;
                    break;
                case (true, false): min = xSelector(current); break;
                case (false, _): max = xSelector(current); break;
            }

            lastZ = currentZ;

            if (Math.Abs(max - min) <= 1) converged = true;
        }
    }

    private async Task GetAndSetDataAsync(AdsXGainsDTOItem item, CancellationToken cancellationToken)
    {
        var (isSuccess, transBuffer) = await GetTraceBuffeAsync(item, cancellationToken).ConfigureAwait(false);

        if (isSuccess == false) Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Get Hrp Failed!"), HtmlLogUniqueId.LoggingHtml());

        var speedChangedList = transBuffer.X_Speed.ToPoints().Where(t => Math.Round(Math.Abs(t.Y) / item.SpeedXValue, 2) > 0.5).ToList();

        if (speedChangedList.Count == 0) return;

        var xSpeedStartIndex = Convert.ToInt32(speedChangedList[0].X);
        var xSpeedEndIndex = Convert.ToInt32(speedChangedList[^1].X);
  
        var listZ1 = transBuffer.Z_ECS0.Take(xSpeedEndIndex).ToList();
        var listZ2 = transBuffer.Z_ECS1.Take(xSpeedEndIndex).ToList();

        GetOpeningDirection(item, listZ1, listZ2, xSpeedStartIndex);

        item.PlotH = transBuffer.Height.Skip(xSpeedStartIndex).Take(xSpeedEndIndex - xSpeedStartIndex).ToPoints();
        item.PlotR = transBuffer.Roll.Skip(xSpeedStartIndex).Take(xSpeedEndIndex - xSpeedStartIndex).ToPoints();
        item.PlotP = transBuffer.Pitch.Skip(xSpeedStartIndex).Take(xSpeedEndIndex - xSpeedStartIndex).ToPoints();
        item.PlotZ1OrZ3 = transBuffer.Z_ECS0.ToPoints();
        item.PlotZ2OrZ4 = transBuffer.Z_ECS1.ToPoints();
        item.Speeds = transBuffer.X_Speed.ToPoints();
    }

    private void GetOpeningDirection(AdsXGainsDTOItem item, List<double> pointZ1, List<double> pointZ2, int startIndex)
    {
        var beforeZ1OrZ3Avg = pointZ1.Take(startIndex).Average();
        var afterZ1OrZ3Avg = pointZ1.Skip(startIndex).Average();
        item.IsUpwardZ1OrZ3 = afterZ1OrZ3Avg - beforeZ1OrZ3Avg < 0;

        var beforeZ2OrZ4Avg = pointZ2.Take(startIndex).Average();
        var afterZ2OrZ4Avg = pointZ2.Skip(startIndex).Average();
        item.IsUpwardZ2OrZ4 = afterZ2OrZ4Avg - beforeZ2OrZ4Avg < 0;
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

    private void LogDichotomy(AdsXGainsDTOItem item)
    {
        var (index1, index2) = item.IsPositive ? ("1", "2") : ("3", "4");
        Logger.LogHtmlInformation($"(X{index1},X{index2}): ({item.X1OrX3},{item.X2OrX4})", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
        {
            HeightMax = item.H,
            RollMax = item.R,
            PitchMax = item.P,
            PlotZ = item.ZPlotDataSource.GetHtmlPlot2DLinesChart(0),
            PlotHRP = item.HrpPlotDataSource.GetHtmlPlot2DLinesChart(0),
        }), HtmlLogUniqueId.LoggingHtml());
    }

    private void LogXTable(string title, HtmlHeaderLevelEnum headerLevel, AdsXGainsDTOItem[] list)
    {
        Logger.LogHtmlInformation(title, headerLevel, new HtmlBullet(new
        {
            Cache.Threshold,
            Table = new HtmlTable(list.Select(item =>
            {
                var x1OrX3 = item.X1OrX3 + " " + (item.IsUpwardZ1OrZ3 ? "↑" : "↓");
                var x2OrX4 = item.X2OrX4 + " " + (item.IsUpwardZ2OrZ4 ? "↑" : "↓");
                return new { item.SpeedXValue, x1OrX3, x2OrX4, item.H, item.R, item.P };
            }).ToList())
        }), HtmlLogUniqueId.LoggingHtml());
    }

    private void LogHrpPoint3DChart(AdsXGainsDTOItem[] list, bool isPositive)
    {
        var hPoint3DList = list.Select(t => new Point3D(t.X1OrX3, t.X2OrX4, t.H)).ToList();
        var rPoint3DList = list.Select(t => new Point3D(t.X1OrX3, t.X2OrX4, t.R)).ToList();
        var pPoint3DList = list.Select(t => new Point3D(t.X1OrX3, t.X2OrX4, t.P)).ToList();

        var (index1, index2) = isPositive ? ("1", "2") : ("3", "4");
        Logger.LogHtmlInformation($"HRP3D:(X{index1},X{index2})", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
        {
            HPoint3DList = new HtmlPlot3DChart([.. hPoint3DList], $"HPlot3D(X: X1 - Y: X2 - Z: Height)", HtmlPlot3DType.Bar3D),
            RPoint3DList = new HtmlPlot3DChart([.. rPoint3DList], $"RPlot3D(X: X1 - Y: X2 - Z: Roll)", HtmlPlot3DType.Bar3D),
            PPoint3DList = new HtmlPlot3DChart([.. pPoint3DList], $"PPlot3D(X: X1 - Y: X2 - Z: Pitch)", HtmlPlot3DType.Bar3D)
        }), HtmlLogUniqueId.LoggingHtml());
    }

    private bool Save(AdsXGainsDTO dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        ApplicationCookieService.SetCalibration(dto, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase calibration, CancellationToken cancellationToken)
    {
        var temp = Guard.IsAssignableToTypeAndReturn<AdsXGainsDTO>(calibration);
        var status = Entry.Status;

        Calibration = temp;

        status.TotalCalibrationCount = 1;
        status.CalibratedCount = Calibration.IsCalibrated ? 1 : 0;
        status.VerifiedCount = Calibration.IsVerified ? 1 : 0;
        status.Details = [];
    }

    #endregion 校准
}