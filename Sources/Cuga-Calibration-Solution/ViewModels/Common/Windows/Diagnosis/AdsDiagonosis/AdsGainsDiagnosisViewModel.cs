using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Events;
using Core.Models.Models.Ads.XGains;
using Core.Models.Models.Ads.YGains;
using Core.Utilities;
using Local.SQL.Cache.Providers.Extensions;
using Local.SQL.Cache.Providers.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniExcelLibs;
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

namespace CugaCalibration.ViewModels.Common.Windows.Diagnosis.AdsDiagonosis;

[IOCAppService(ServiceType = typeof(AdsGainsDiagnosisViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class AdsGainsDiagnosisViewModel(
    IDialogWindowProvider dialogWindowProvider,
    IHostEnvironment hostEnvironment,
    StageViewModel stageViewModel,
    AdsViewModel adsViewModel,
    ICacheProvider cacheProvider,
    IOptions<ApplicationSetting> options,
    ILogger<AdsGainsDiagnosisViewModel> logger,
    IMessenger messenger)
    : AdsDiagnosisViewModelBase(dialogWindowProvider, logger, options)
{
    private readonly IDialogWindowProvider _dialogWindowProvider = dialogWindowProvider;

    #region 属性

    /// <summary>
    /// 校准Html日志文件路径名称
    /// </summary>
    public override string LogHtmlFileName => "AdsDiagnosis_Gains";

    /// <summary>
    /// Gains诊断运动方向
    /// </summary>
    [ObservableProperty]
    private bool _isPositive = true;

    [ObservableProperty]
    private bool _isX = true;

    /// <summary>
    /// X Gains 诊断对象集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AdsXGainsItemDto> _xGainsItemDtoList = [];

    /// <summary>
    /// Y Gains 诊断对象集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AdsYGainsItemDto> _yGainsItemDtoList = [];

    #region 界面

    [ObservableProperty]
    private AdsXGainsItemDto? _selectedXGainsItemDto;

    [ObservableProperty]
    private AdsYGainsItemDto? _selectedYGainsItemDto;

    [ObservableProperty]
    private double _singleDiagnosisSpeed = 90;

    [ObservableProperty]
    private AdsXGainsItemDto _singleDiagnosisXGainsDto = new();

    [ObservableProperty]
    private AdsYGainsItemDto _singleDiagnosisYGainsDto = new();

    [ObservableProperty]
    private AdsXGainsCache _xGainCache = new();

    [ObservableProperty]
    private AdsYGainsCache _yGainCache = new();

    [ObservableProperty]
    private Point _startPosition;

    [ObservableProperty]
    private Point _endPosition;

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
            logger.LogError(ex, "{@Name}: Loaded Failed", nameof(AdsGainsDiagnosisViewModel));
        }
    }

    [RelayCommand(CanExecute = nameof(IsEnableWindow))]
    private async Task GetPointAsync(string name)
    {
        try
        {
            await Task.Run(() =>
            {
                var result = stageViewModel.GetMachineStagePosition();
                if (name == "StartPosition")
                {
                    StartPosition = result;
                }
                else if (name == "EndPosition")
                {
                    EndPosition = result;
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: Get Point Failed", nameof(AdsGainsDiagnosisViewModel));
        }
    }

    [RelayCommand(CanExecute = nameof(IsEnableWindow))]
    private async Task GotoPointAsync(string name)
    {
        try
        {
            await Task.Run(() => stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(name == "StartPosition" ? StartPosition : EndPosition)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: Move Point Failed", nameof(AdsGainsDiagnosisViewModel));
        }
    }

    [RelayCommand]
    private async Task OnceDianosisActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (IsX)
            {
                var x1 = XGainCache.GetX1();
                var x2 = XGainCache.GetX2();
                stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(XGainCache.GetStartPosition());

                adsViewModel.SetSensorXSpeedFeedForwardValue(XGainCache.IsPositive, (x1, x2));

                stageViewModel.SetXSpeedValue(SingleDiagnosisSpeed);
                stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(XGainCache.GetStartPosition());
                await Task.Delay(hostEnvironment.IsDevelopment() ? 1000 : 10000, cancellationToken);
                stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(XGainCache.GetEndPosition());
            }
            else
            {
                var y1 = YGainCache.GetY1();
                var y2 = YGainCache.GetY2();
                var y3 = YGainCache.GetY3();

                stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(YGainCache.GetStartPosition());

                adsViewModel.SetSensorYSpeedFeedForwardValue(YGainCache.IsPositive, (y1, y2, y3));

                stageViewModel.SetYSpeedValue(SingleDiagnosisSpeed);

                stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(YGainCache.GetStartPosition());
                await Task.Delay(hostEnvironment.IsDevelopment() ? 1000 : 10000, cancellationToken);
                stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(YGainCache.GetEndPosition());
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: Once Diagnosis Action Failed", nameof(AdsGainsDiagnosisViewModel));
        }
    }

    #region 诊断业务

    public override async Task<bool> DiagnosisActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            PlotListZ.Clear();
            if (IsX)
            {
                if (await XGainsDiagnosisAsync(cancellationToken).ConfigureAwait(false) == false)
                    return false;
            }
            else
            {
                if (await YGainsDiagnosisAsync(cancellationToken).ConfigureAwait(false) == false)
                    return false;
            }

            return true;
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

    private async Task<bool> XGainsDiagnosisAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await Task.Run(async () =>
            {
                if (XGainsItemDtoList.Count == 0)
                {
                    _dialogWindowProvider.ShowDialog("Diagnosis Config Is Empty! Please Import Config First!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Start"), HtmlLogUniqueId.LoggingHtml());
                messenger.Send(ToggleCalibrateEventFactory.UpdateWindowEnable(false));
                IsEnableWindow = false;

                XGainCache.IsPositive = IsPositive;
                XGainCache.SetStartPosition(StartPosition);
                XGainCache.SetEndPosition(EndPosition);

                TracebufferList.Clear();

                foreach (var t in XGainsItemDtoList) t.IsCalibrated = false;
                foreach (var adsXGainsItemDto in XGainsItemDtoList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SelectedXGainsItemDto = adsXGainsItemDto;
                    if (await GetHrpAsync(SelectedXGainsItemDto).ConfigureAwait(false) == false)
                        return false;
                    var tempPlotList = new List<WpfPlotModel>(PlotListZ);
                    TracebufferList.Add(tempPlotList);
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

        async Task<bool> GetHrpAsync(AdsXGainsItemDto adsXGainsItemDto)
        {
            adsXGainsItemDto.IsPositive = XGainCache.IsPositive;
            var x1 = XGainCache.GetX1();
            var x2 = XGainCache.GetX2();
            // todo: 这里需要优化，暂时先用固定值,判断正反向
            //var speedXValue = 70;
            //var x1 = adsXGainsItemDto.PositiveX1P1 * speedXValue * speedXValue + adsXGainsItemDto.PositiveX1P2 * speedXValue + adsXGainsItemDto.PositiveX1P3;
            //var x2 = adsXGainsItemDto.PositiveX2P1 * speedXValue * speedXValue + adsXGainsItemDto.PositiveX2P2 * speedXValue + adsXGainsItemDto.PositiveX2P3;

            stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(XGainCache.GetStartPosition());

            adsViewModel.SetSensorXSpeedFeedForwardValue(XGainCache.IsPositive, (x1, x2));

            stageViewModel.SetXSpeedValue(SingleDiagnosisSpeed);
            stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(XGainCache.GetStartPosition());

            Thread.Sleep(hostEnvironment.IsDevelopment() ? 1000 : 10000);

            var task = Task.Run(() => adsViewModel.GetSensorSpeedZ1Z2Z3TraceBufferList(TimeSpan.FromSeconds(WaitTime)));
            Thread.Sleep(hostEnvironment.IsDevelopment() ? 1000 : 3000);
            stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(XGainCache.GetEndPosition());

            var transBuffer = await task.ConfigureAwait(false);

            var z1Points = transBuffer[0].ToPoints();
            var z2Points = transBuffer[1].ToPoints();

            PlotListZ.Clear();
            PlotListZ = [.. PlotListZ, new WpfPlotModel($"(X1:{x1},X2:{x2})Z1", [.. z1Points]), new WpfPlotModel($"(X1:{x1},X2:{x2})Z2", [.. z2Points])];

            SelectedXGainsItemDto!.IsCalibrated = true;
            return true;
        }
    }

    private async Task<bool> YGainsDiagnosisAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await Task.Run(async () =>
            {
                if (YGainsItemDtoList.Count == 0)
                {
                    _dialogWindowProvider.ShowDialog("Diagnosis Config Is Empty! Please Import Config First!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                messenger.Send(ToggleCalibrateEventFactory.UpdateWindowEnable(false));
                IsEnableWindow = false;

                YGainCache.IsPositive = IsPositive;
                YGainCache.SetStartPosition(StartPosition);
                YGainCache.SetEndPosition(EndPosition);

                TracebufferList.Clear();

                foreach (var t in YGainsItemDtoList) t.IsCalibrated = false;
                foreach (var adsYGainsItemDto in YGainsItemDtoList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SelectedYGainsItemDto = adsYGainsItemDto;
                    if (await GetHrpAsync(SelectedYGainsItemDto).ConfigureAwait(false) == false)
                        return false;
                    var tempPlotList = new List<WpfPlotModel>(PlotListZ);
                    TracebufferList.Add(tempPlotList);
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

        async Task<bool> GetHrpAsync(AdsYGainsItemDto adsYGainsItemDto)
        {
            adsYGainsItemDto.IsPositive = YGainCache.IsPositive;
            var y1 = YGainCache.GetY1();
            var y2 = YGainCache.GetY2();
            var y3 = YGainCache.GetY3();

            stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(YGainCache.GetStartPosition());

            adsViewModel.SetSensorYSpeedFeedForwardValue(YGainCache.IsPositive, (y1, y2, y3));

            stageViewModel.SetYSpeedValue(SingleDiagnosisSpeed);

            stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(YGainCache.GetStartPosition());

            Thread.Sleep(hostEnvironment.IsDevelopment() ? 1000 : 10000);

            var task = Task.Run(() => adsViewModel.GetSensorSpeedZ1Z2Z3TraceBufferList(TimeSpan.FromSeconds(WaitTime)));
            Thread.Sleep(hostEnvironment.IsDevelopment() ? 1000 : 3000);
            stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(YGainCache.GetEndPosition());

            var transBuffer = await task.ConfigureAwait(false);

            var z1Points = transBuffer[0].ToPoints();
            var z2Points = transBuffer[1].ToPoints();
            var z3Points = transBuffer[2].ToPoints();

            PlotListZ.Clear();
            PlotListZ = [.. PlotListZ, new WpfPlotModel($"(Y1:{y1},Y2:{y2},Y3:{y3})Z1", [.. z1Points]), new WpfPlotModel($"(Y1:{y1},Y2:{y2},Y3:{y3})Z2", [.. z2Points]), new WpfPlotModel($"(Y1:{y1},Y2:{y2},Y3:{y3})Z3", [.. z3Points])];

            SelectedYGainsItemDto!.IsCalibrated = true;
            return true;
        }
    }

    #endregion 诊断业务

    #region 文件读写

    public override bool ImportingConfig(string filePath)
    {
        try
        {
            if (IsX)
            {
                var rows = MiniExcel.Query<AdsXGainsItemDto>(filePath, "XGains");
                XGainsItemDtoList = [.. rows.ToList()];
            }
            else
            {
                var rows = MiniExcel.Query<AdsYGainsItemDto>(filePath, "YGains");
                YGainsItemDtoList = [.. rows.ToList()];
            }

            return true;
        }
        catch (Exception ex)
        {
            _dialogWindowProvider.ShowDialog("Import Config Failed! Please Check Config File Format And Try Again!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "{@Name} Error : Import {@Axis} Gains Config Failed", nameof(AdsGainsDiagnosisViewModel), IsX ? "X" : "Y");
            return false;
        }
    }

    #endregion 文件读写
}