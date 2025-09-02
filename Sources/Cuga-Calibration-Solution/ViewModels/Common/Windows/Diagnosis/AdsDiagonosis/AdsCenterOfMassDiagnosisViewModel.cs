using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Events;
using Core.Models.Exceptions;
using Core.Models.Models.Ads.CenterOfMass;
using Core.Models.Models.Ads.XGains;
using Core.Models.Models.Ads.YGains;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Interfaces;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniExcelLibs;
using MoreLinq;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Behaviors;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.Diagnosis.AdsDiagonosis;

[IOCAppService(ServiceType = typeof(AdsCenterOfMassDiagnosisViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class AdsCenterOfMassDiagnosisViewModel(
    IDialogWindowProvider dialogWindowProvider,
    ISynchronizationContextProvider contextProvider,
    StageViewModel stageViewModel,
    AdsViewModel adsViewModel,
    IHostEnvironment hostEnvironment,
    ICacheProvider cacheProvider,
    IOptions<ApplicationSetting> options,
    IMessenger messenger,
    ILogger<AdsCenterOfMassDiagnosisViewModel> logger)
    : AdsDiagnosisViewModelBase(dialogWindowProvider, logger, options)
{
    private readonly IDialogWindowProvider _dialogWindowProvider = dialogWindowProvider;

    #region 属性

    /// <summary>
    /// 校准Html日志文件路径名称
    /// </summary>
    public override string LogHtmlFileName => "AdsDiagnosis_CenterOfMass";

    /// <summary>
    /// 诊断对象集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AdsCenterOfMassItemDto> _centerOfMassItemDtoList = [];

    /// <summary>
    /// 诊断缓存集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AdsCenterOfMassDiagonosisDto> _centerOfMassDiagnosisList = [];

    #region 界面

    [ObservableProperty]
    private AdsCenterOfMassItemDto? _selectedItemDto;

    [ObservableProperty]
    private AdsCenterOfMassDiagonosisDto? _selectedDiagnosisDtoItem;

    [ObservableProperty]
    private AdsCenterOfMassCache _cache = new();

    /// <summary>
    /// tracebuffer界面显示
    /// </summary>
    [ObservableProperty]
    private List<WpfPlotModel> _plotListZ = [];

    #endregion 界面

    #region 缓存

    private AdsXGainsItemDto[] _adsXGainsVerifyList = [];

    private AdsYGainsItemDto[] _adsYGainsVerifyList = [];

    #endregion 缓存

    #endregion 属性

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            await Task.CompletedTask.ConfigureAwait(false);

            _adsXGainsVerifyList = cacheProvider.GetOrDefaultArray<AdsXGainsItemDto>();
            _adsYGainsVerifyList = cacheProvider.GetOrDefaultArray<AdsYGainsItemDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: Loaded Failed", nameof(AdsCenterOfMassDiagnosisViewModel));
        }
    }

    #region 诊断业务

    public override async Task<bool> DiagnosisActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            PlotListZ.Clear();
            return await CenterOfMassDiagnosisAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            SetValueByVerifyResult();
        }

        void SetValueByVerifyResult()
        {
            try
            {
                foreach (var item in _adsXGainsVerifyList)
                {
                    if (item.IsOk)
                    {
                        //todo
                        //adsViewModel.SetSensorXSpeedFeedForwardValue(true, (item.PositiveX1, item.PositiveX2));
                        //adsViewModel.SetSensorXSpeedFeedForwardValue(false, (item.NegativeX3, item.NegativeX4));
                        break;
                    }
                }

                foreach (var item in _adsYGainsVerifyList)
                {
                    if (item.IsOk)
                    {
                        //adsViewModel.SetSensorYSpeedFeedForwardValue(true, (item.PositiveY1, item.PositiveY2, item.PositiveY3));
                        //adsViewModel.SetSensorYSpeedFeedForwardValue(false, (item.NegativeY4, item.NegativeY5, item.NegativeY6));
                        break;
                    }
                }
            }
            catch
            {
                _dialogWindowProvider.ShowDialog("Set Sensor Feed Forward By Verify Result Failed! ", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        }
    }

    private async Task<bool> CenterOfMassDiagnosisAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await Task.Run(async () =>
            {
                if (CenterOfMassDiagnosisList.Count == 0)
                {
                    _dialogWindowProvider.ShowDialog("Diagnosis Config Is Empty! Please Import Config First!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                CenterOfMassDiagnosisList.ForEach(t =>
                {
                    t.Cache.StartPositionFindX = Cache.StartPositionFindX;
                    t.Cache.EndPositionFindX = Cache.EndPositionFindX;
                    t.Cache.StartPositionFindY = Cache.StartPositionFindY;
                    t.Cache.EndPositionFindY = Cache.EndPositionFindY;
                });
                messenger.Send(ToggleCalibrateEventFactory.UpdateWindowEnable(false));
                IsEnableWindow = false;
                contextProvider.Send(() => { CenterOfMassItemDtoList.Clear(); });
                foreach (var diagnosisItem in CenterOfMassDiagnosisList.Where(t => t.IsSelected))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    diagnosisItem.Cache.IsFindX = Cache.IsFindX;
                    SelectedDiagnosisDtoItem = diagnosisItem;
                    logger.LogHtmlInformation($"Find {(Cache.IsFindX ? "X" : "Y")}: Speed {(Cache.IsFindX ? diagnosisItem.Cache.YspeedValue : diagnosisItem.Cache.XspeedValue)}", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
                    logger.LogHtmlInformation($"Find {(Cache.IsFindX ? "X" : "Y")} Coordinate Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        FindCount = diagnosisItem.Cache.IsFindX
                            ? diagnosisItem.Cache.FindCountX
                            : diagnosisItem.Cache.FindCountY,
                        WaitTime,
                        XSpeed = diagnosisItem.Cache.XspeedValue,
                        YSpeed = diagnosisItem.Cache.YspeedValue,
                        XPositiveSpeedForward1 = diagnosisItem.Cache.XSpeedForwardValuePositiveX1,
                        XPositiveSpeedForward2 = diagnosisItem.Cache.XSpeedForwardValuePositiveX2,
                        XNegativeSpeedForward1 = diagnosisItem.Cache.XSpeedForwardValueNegativeX3,
                        XNegativeSpeedForward2 = diagnosisItem.Cache.XSpeedForwardValueNegativeX4,
                        YPositiveSpeedForward1 = diagnosisItem.Cache.YSpeedForwardValuePositiveY1,
                        YPositiveSpeedForward2 = diagnosisItem.Cache.YSpeedForwardValuePositiveY2,
                        YPositiveSpeedForward3 = diagnosisItem.Cache.YSpeedForwardValuePositiveY3,
                        YNegativeSpeedForward1 = diagnosisItem.Cache.YSpeedForwardValueNegativeY4,
                        YNegativeSpeedForward2 = diagnosisItem.Cache.YSpeedForwardValueNegativeY5,
                        YNegativeSpeedForward3 = diagnosisItem.Cache.YSpeedForwardValueNegativeY6,
                        StartPosition = diagnosisItem.Cache.GetStartPosition(),
                        EndPosition = diagnosisItem.Cache.GetEndPosition()
                    }), HtmlLogUniqueId.LoggingHtml());

                    if (GetAdsCenterOfMassItemDtoList(diagnosisItem.Cache) == false)
                    {
                        return false;
                    }

                    foreach (var (index, dto) in CenterOfMassItemDtoList.Select((t, i) => (index: i, dto: t)))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        logger.LogHtmlInformation($"Find {(Cache.IsFindX ? "X" : "Y")} Times:{index}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                        SelectedItemDto = dto;
                        if (await GetXyDeltaValueAsync(dto, diagnosisItem.Cache, cancellationToken).ConfigureAwait(false) == false)
                            return false;
                        SelectedItemDto.IsCalibrated = true;

                        //_tracebufferList.Add(tempPlotList);
                    }

                    var adsCenterOfMassItemList = CenterOfMassItemDtoList.Select(t => t.Clone()).ToList();
                    var isSuccess = FitRelationalFunctions(adsCenterOfMassItemList);
                    if (isSuccess == false)
                    {
                        logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Error: Fit Relational functions Failed!"), HtmlLogUniqueId.LoggingHtml());
                        return false;
                    }
                }

                return true;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _dialogWindowProvider.ShowDialog("Diagnosis Failed! Please Check Config And Try Again!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }
        finally
        {
            messenger.Send(ToggleCalibrateEventFactory.UpdateWindowEnable(true));
            IsEnableWindow = true;
        }
    }

    private bool GetAdsCenterOfMassItemDtoList(AdsCenterOfMassCache cache)
    {
        try
        {
            contextProvider.Send(() => CenterOfMassItemDtoList.Clear());
            var startPosition = cache.GetStartPosition();
            var endPosition = cache.GetEndPosition();

            var interval = Cache.IsFindX
                ? (endPosition.X - startPosition.X) / (cache.FindCountX - 1)
                : (endPosition.Y - startPosition.Y) / (cache.FindCountY - 1);

            foreach (var i in Enumerable.Range(0, Cache.IsFindX ? cache.FindCountX : cache.FindCountY))
            {
                var newStartPosition = Cache.IsFindX
                    ? new Point(startPosition.X + i * interval, startPosition.Y)
                    : new Point(startPosition.X, startPosition.Y + i * interval);

                var newEndPosition = Cache.IsFindX
                    ? new Point(startPosition.X + i * interval, endPosition.Y)
                    : new Point(endPosition.X, startPosition.Y + i * interval);

                var adsCenterOfMassItemDto = new AdsCenterOfMassItemDto
                {
                    Index = i,
                    IsFindX = Cache.IsFindX,
                    StartPosition = newStartPosition,
                    EndPosition = newEndPosition,
                    IsCalibrated = false
                };
                adsCenterOfMassItemDto.GetIsPositive();

                var negativeAdsCenterOfMassItemDto = new AdsCenterOfMassItemDto
                {
                    Index = i,
                    IsFindX = Cache.IsFindX,
                    StartPosition = newEndPosition,
                    EndPosition = newStartPosition,
                    IsCalibrated = false
                };
                negativeAdsCenterOfMassItemDto.GetIsPositive();

                contextProvider.Send(() => CenterOfMassItemDtoList.Add(adsCenterOfMassItemDto));
                contextProvider.Send(() => CenterOfMassItemDtoList.Add(negativeAdsCenterOfMassItemDto));
            }

            return CenterOfMassItemDtoList.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> GetXyDeltaValueAsync(AdsCenterOfMassItemDto adsCenterOfMassDto, AdsCenterOfMassCache cache, CancellationToken cancellationToken)
    {
        try
        {
            stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(adsCenterOfMassDto.StartPosition);

            if (Cache.IsFindX)
            {
                stageViewModel.SetYSpeedValue(cache.YspeedValue);
                stageViewModel.SetXSpeedValue(300);
            }
            else
            {
                stageViewModel.SetXSpeedValue(cache.XspeedValue);
                stageViewModel.SetYSpeedValue(200);
            }

            adsViewModel.SetAdsXyEnabled(true);

            stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(adsCenterOfMassDto.StartPosition, false);

            var isPositive = adsCenterOfMassDto.IsPositive;
            var (forwardValue1, forwardValue2, forwardValue3) = cache.GetForwardValue(isPositive);
            if (Cache.IsFindX)
                InvokeAdsService(() => adsViewModel.SetSensorYSpeedFeedForwardValue(isPositive, (forwardValue1, forwardValue2, forwardValue3)), cancellationToken);
            else
                InvokeAdsService(() => adsViewModel.SetSensorXSpeedFeedForwardValue(isPositive, (forwardValue1, forwardValue2)), cancellationToken);

            Thread.Sleep(hostEnvironment.IsDevelopment() ? 1000 : 10000);

            var task = Task.Run(() => adsViewModel.GetSensorSpeedX0X1Y0Y1WithSpeedTraceBufferList(!adsCenterOfMassDto.IsFindX, TimeSpan.FromSeconds(Cache.WaitTime)));
            Thread.Sleep(hostEnvironment.IsDevelopment() ? 1000 : 3000);

            stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(adsCenterOfMassDto.EndPosition, false);

            var traceBuffer = await task.ConfigureAwait(false);

            var xyX1 = traceBuffer[0].ToPoints();
            var xyX2 = traceBuffer[1].ToPoints();
            var xyY1 = traceBuffer[2].ToPoints();
            var xyY2 = traceBuffer[3].ToPoints();
            var speedBuffer = traceBuffer[4].ToPoints();
            var startIndex = traceBuffer[4].FindIndex(t => t != 0);

            (adsCenterOfMassDto.XErrorMin, adsCenterOfMassDto.XErrorMax, var x1BuffersWithoutBackground, var x2BuffersWithoutBackground, var x1X2ErrorBuffers) = GetTracebufferDeltaValueWithoutBackground(xyX1, xyX2, startIndex, true);
            (adsCenterOfMassDto.YErrorMin, adsCenterOfMassDto.YErrorMax, var y1BuffersWithoutBackground, var y2BuffersWithoutBackground, var y1Y2ErrorBuffers) = GetTracebufferDeltaValueWithoutBackground(xyY1, xyY2, startIndex, false);

            var plotsList = new List<WpfPlotModel>
            {
                new($"{(adsCenterOfMassDto.IsFindX ? "Y" : "X")} Speed", speedBuffer),
                new("XyX1", xyX1),
                new("XyX2", xyX2),
                new("XyY1", xyY1),
                new("XyY2", xyY2),
                new("X1-Background", x1BuffersWithoutBackground),
                new("X2-Background", x2BuffersWithoutBackground),
                new("Y1-Background", y1BuffersWithoutBackground),
                new("Y2-Background", y2BuffersWithoutBackground),
                new("X1-X2", x1X2ErrorBuffers),
                new("Y2-Y1", y1Y2ErrorBuffers)
            };
            TracebufferList.Add(plotsList);
            PlotListZ.Clear();
            PlotListZ =
            [
                .. PlotListZ,
                new WpfPlotModel("Speed", speedBuffer),
                new WpfPlotModel("X1", xyX1),
                new WpfPlotModel("X2", xyX2),
                new WpfPlotModel("Y1", xyY1),
                new WpfPlotModel("Y2", xyY2)
            ];

            logger.LogHtmlInformation($"X/Y TraceBuffer Index{adsCenterOfMassDto.Index},{(adsCenterOfMassDto.IsPositive ? "Positive" : "Negative")}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                adsCenterOfMassDto.IsFindX,
                adsCenterOfMassDto.IsPositive,
                adsCenterOfMassDto.StartPosition,
                adsCenterOfMassDto.EndPosition,
                adsCenterOfMassDto.XErrorMin,
                adsCenterOfMassDto.XErrorMax,
                adsCenterOfMassDto.YErrorMin,
                adsCenterOfMassDto.YErrorMax,
                adsCenterOfMassDto.DeltaX,
                adsCenterOfMassDto.DeltaY,
                XTraceBuffer = new HtmlPlot2DLinesChart([
                    ("XyX1", xyX1),
                    ("XyX2", xyX2),
                    ($"{(adsCenterOfMassDto.IsFindX ? "Y" : "X")} Speed", speedBuffer)
                ], "XTraceBuffer"),
                XBufferWitoutBackground = new HtmlPlot2DLinesChart([
                    ("X1-Background", x1BuffersWithoutBackground),
                    ("X2-Background", x2BuffersWithoutBackground)
                ], "XTraceBuffer"),
                YTraceBuffer = new HtmlPlot2DLinesChart([
                    ("XyY1", xyY1),
                    ("XyY2", xyY2),
                    ($"{(adsCenterOfMassDto.IsFindX ? "Y" : "X")} Speed", speedBuffer)
                ], "YTraceBuffer"),
                YBufferWitoutBackground = new HtmlPlot2DLinesChart([
                    ("Y1-Background", y1BuffersWithoutBackground),
                    ("Y2-Background", y2BuffersWithoutBackground)
                ], "YTraceBuffer"),
                ErrorTraceBuffer = new HtmlPlot2DLinesChart([
                    ("X1-X2", x1X2ErrorBuffers),
                    ("Y2-Y1", y1Y2ErrorBuffers)
                ], "YTraceBuffer")
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        }
        catch
        {
            logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("Error: Get X/Y Delta Value Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        (double errorMin,
            double errorMax,
            Point[] buffers1RemoveBackground,
            Point[] buffers2RemoveBackground,
            Point[] errorBuffers) // X1/X2 or Y1/Y2 去掉平均背景值后的X1-X2或Y1-Y2buffer（第一次去背景）
            GetTracebufferDeltaValueWithoutBackground(Point[] tracebufferAry0, Point[] tracebufferAry1, int startIndex, bool isGetDeltaX)
        {
            var backGround0 = tracebufferAry0.Skip(0).Take(startIndex).ToArray().Average(t => t.Y);
            var backGround1 = tracebufferAry1.Skip(0).Take(startIndex).ToArray().Average(t => t.Y);

            var removeBackgroundbuffers0 = tracebufferAry0.Select(t => t.Y - backGround0).ToList();
            var removeBackgroundbuffers1 = tracebufferAry1.Select(t => t.Y - backGround1).ToList();

            // 去掉背景1后X1-X2或Y1-Y2buffer
            var errorBuffersWithoutBackgroud = isGetDeltaX
                ? removeBackgroundbuffers0.Select((t, i) => t - removeBackgroundbuffers1[i]).ToPoints()
                : removeBackgroundbuffers1.Select((t, i) => t - removeBackgroundbuffers0[i]).ToPoints();

            // 去除坏点
            var buffers = errorBuffersWithoutBackgroud.Where(t => Math.Abs(t.Y) < 1000).ToList();

            var minValue = buffers.Min(t => t.Y);
            var maxValue = buffers.Max(t => t.Y);

            return (minValue, maxValue, removeBackgroundbuffers0.ToPoints(), removeBackgroundbuffers1.ToPoints(), errorBuffersWithoutBackgroud);
        }
    }

    /// <summary>
    /// 求deltaX/Y 拟合函数
    /// </summary>
    /// <param name="adsCenterOfMassDtoList"></param>
    /// <returns></returns>
    private bool FitRelationalFunctions(List<AdsCenterOfMassItemDto> adsCenterOfMassDtoList)
    {
        try
        {
            var positiveDtoItems = adsCenterOfMassDtoList.Where(t => t.IsPositive).ToList();
            var negativeDtoItems = adsCenterOfMassDtoList.Where(t => t.IsPositive == false).ToList();
            // 拟合关于Delta函数
            var isSuccess = GetFitRelationalFunction(positiveDtoItems, false, true);
            if (isSuccess == false) return false;

            isSuccess = GetFitRelationalFunction(negativeDtoItems, false, false);
            return isSuccess;
        }
        catch
        {
            return false;
        }

        bool GetFitRelationalFunction(List<AdsCenterOfMassItemDto> plotList, bool isGetDeltaX, bool isPositive)
        {
            List<Point> fitFuncList = [];

            var listRow = plotList.Select(t => Cache.IsFindX ? t.StartPosition.X : t.StartPosition.Y).ToList();
            var listCol = plotList.Select(t => isGetDeltaX ? t.DeltaX : t.DeltaY).ToList();

            var (fitLineK, fitLineB, _, _) = PolyFit.Poly1Fit(Vector<double>.Build.DenseOfEnumerable(listRow), Vector<double>.Build.DenseOfEnumerable(listCol));
            fitFuncList.AddRange(listRow.Select(t => new Point(t, fitLineK * t + fitLineB)));

            var isSuccess = fitLineK != 0;
            logger.LogHtmlInformation($"Fit Function Of Delta {(isGetDeltaX ? "X" : "Y")},{(isPositive ? "Positive" : "Negative")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Data = new HtmlPlot2DLinesChart([
                        ("OriginData", listRow.Select((t, i) => new Point(t, listCol[i])).ToArray()),
                        ("FitFunction", fitFuncList.ToArray())
                    ], $"{(Cache.IsFindX ? "X" : "Y")}-Delta{(isGetDeltaX ? "X" : "Y")}")
            }), HtmlLogUniqueId.LoggingHtml());

            return isSuccess;
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
                logger.LogError(ex, "InvokeSetValue failed, retrying {RetryCount} times", i + 1);
            }
        }

        throw new CugaException($"Ads Service Invoke Error! {nameof(action.Method.Name)}");
    }

    #endregion 诊断业务

    #region 文件读写

    public override bool ImportingConfig(string filePath)
    {
        try
        {
            List<AdsCenterOfMassCache> rows;
            if (Cache.IsFindX)
            {
                rows = [.. MiniExcel.Query<AdsCenterOfMassCache>(filePath, "FindX")];
            }
            else
            {
                rows = [.. MiniExcel.Query<AdsCenterOfMassCache>(filePath, "FindY")];
            }

            CenterOfMassDiagnosisList = [.. rows.Select(t => new AdsCenterOfMassDiagonosisDto { Cache = t })];

            return true;
        }
        catch (Exception ex)
        {
            _dialogWindowProvider.ShowDialog("Import Config Failed! Please Check Config File Format And Try Again!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Import Find {@XOrY} Config Failed", Cache.IsFindX ? "X" : "Y");
            return false;
        }
    }

    #endregion 文件读写

    public partial class AdsCenterOfMassDiagonosisDto : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected = true;

        [ObservableProperty]
        private AdsCenterOfMassCache _cache = new();
    }
}