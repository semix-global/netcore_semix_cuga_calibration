using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models;
using Core.Models.Models.Laser.AodDelay;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.PmtGain;
using Core.Models.Models.Laser.PrescanChirpAodAlignment;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Core.Utilities;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserPmtGainCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserPmtGainCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Move Laser Power Meter" },
        new() { StepName = "PMTGain Calibration" },
        new() { StepName = "Calibrate SendCIB" },
        new() { StepName = "Calibrate AgingMeasure" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<LaserPmtGainDto> _laserPmtGainDtoList = [];

    [ObservableProperty]
    private ObservableCollection<LaserPmtGainDto> _resultLaserPmtGainDtoList = [];

    [ObservableProperty]
    private LaserPmtGainDto? _selectLaserPmtGainDto;

    [ObservableProperty]
    private LaserPmtGainDto? _plotLaserPmtGainDto;

    [ObservableProperty]
    private LaserPmtGainDto? _resultLaserPmtGainDto;

    [ObservableProperty]
    private StringBuilder? _powerBuilder;

    [ObservableProperty]
    private List<(string Title, Point[])> _pointList = [];

    private bool isAllProtect;

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<LaserPmtGainDto> _reviewList = [];

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private LaserPmtGainCache _cache = new();

    [ObservableProperty]
    private LaserPmtGainDto[] _calibrations = [];

    [ObservableProperty]
    private MicroscopeCalChipDto _microscopeCalChip = new();

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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<MicroscopeFocusItemDto>(out _, out var errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<MicroscopeCalChipDto>(out var microscopeCalChip, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        MicroscopeCalChip = microscopeCalChip;

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<LaserAutoFocusDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<LaserBeamStabilizerObjDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserAodDelayItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserPrescanChirpAodAlignmentDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserXYAstigmatismCalibrationItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserIlluminationProfileItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserXTCCalibrationItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<LaserPmtGainCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserPmtGainDto>();

        return isHasCache || CacheProvider.Set(Cache, cancellationToken);
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Cache.FindPosition = MicroscopeCalChip.HazePosition;

        StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(Cache.FindPosition);
        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewList =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .OrderBy(t => t.PmtId)
        ];
        //默认展示PMT1的数据
        if (SelectLaserPmtGainDto == null && ReviewList.Count > 0)
        {
            SelectLaserPmtGainDto = ReviewList[0];
        }

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.FindPosition);
                return true;

            case 1:
                if (ResultLaserPmtGainDtoList.Count <= 0)
                {
                    DialogWindowProvider.TryShowDialog("Please find Pmt Gain!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    ReviewList =
                    [
                        .. Calibrations
                            .Select(t => t.Clone())
                            .OrderBy(t => t.PmtId)
                    ];
                    foreach (var laserPmtGainDto in ReviewList)
                    {
                        if (!ResultLaserPmtGainDtoList.Any(t => t.PmtId == laserPmtGainDto.PmtId && t.Channel == laserPmtGainDto.Channel))
                        {
                            ResultLaserPmtGainDtoList.Add(laserPmtGainDto);
                        }
                    }

                    foreach (var (index, laserPmtGainItemDto) in ResultLaserPmtGainDtoList.OrderBy(t => t.PmtId).Select((dto, i) => (i, dto)))
                    {
                        laserPmtGainItemDto.IsCalibrated = true;

                        if (Save(laserPmtGainItemDto, cancellationToken, index == ResultLaserPmtGainDtoList.Count - 1)) continue;

                        laserPmtGainItemDto.IsCalibrated = false;
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        return false;
                    }
                }

                return true;

            case 2:
                return true;

            case 3:
                DialogWindowProvider.ShowDialog("PMTGain Measure Is Ok!");
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
            await Task.Run(() => StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(Cache.FindPosition)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
        }
    }

    [RelayCommand]
    private void Review(LaserPmtGainDto laserPmtGainDto)
    {
        if (laserPmtGainDto.Plot.Count > 0)
        {
            PointList.Clear();
            for (var k = 0; k < laserPmtGainDto.Plot.Count; k++)
            {
                var point = (laserPmtGainDto.Plot[k].MeasurePower, laserPmtGainDto.Plot[k].VoltageLightListPoint.ToArray());
                PointList.Add(point);
            }
        }

        DialogWindowProvider.ShowPlot(PointList);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                postion = Cache.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(() =>
        {
            ClearCalibrationTemp();

            Logger.LogHtmlInformation("Parms", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                pmtList = Cache.PmtList,
                measuerPowerList = Cache.PowerSettings,
                startGain = Cache.VoltageMin,
                endGain = Cache.VoltageMax,
                gainInterval = Cache.VoltageInterval,
                waitTime = Cache.WaitTime,
                ProtectValue = Cache.PmtProtectValue,
                Mag = Cache.OpticsMagTypeEnum.ToString()
            }), HtmlLogUniqueId.LoggingHtml());

            LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Through);

            PowerBuilder = new StringBuilder();
            var laserPmtGainList = new ObservableCollection<LaserPmtGainDto>();
            var pmtIdList = Cache.PmtList.Split(',').ToList();
            for (var j = 0; j < pmtIdList.Count; j++)
            {
                for (var k = 1; k < 4; k++)
                {
                    var laserPmtGainDto = new LaserPmtGainDto
                    {
                        MeasurePosition = Cache.FindPosition,
                        PmtId = int.Parse(pmtIdList[j]),
                        Channel = k
                    };
                    laserPmtGainList.Add(laserPmtGainDto);
                }
            }

            var pmtList = new List<LaserPmtGainDto>();
            var powerList = Cache.PowerSettings.Split(',').ToList();
            for (var i = 0; i < powerList.Count; i++)
            {
                var pmt = new LaserPmtGainDto
                {
                    MeasurePower = powerList[i],
                    MeasurePosition = Cache.FindPosition
                };
                pmtList.Add(pmt);
            }

            foreach (var laserPmtGainItemDto in pmtList)
            {
                //设置mag
                LaserViewModel.SendOpticsMagType(Cache.OpticsMagTypeEnum);
                //设置饱和值
                LaserViewModel.SendSaturationValue(Cache.PmtProtectValue);

                if (GetPmtGain(laserPmtGainItemDto, laserPmtGainList, cancellationToken) == false) return false;
            }

            foreach (var itemPmtGainDto in LaserPmtGainDtoList)
            {
                var plotlists = new List<(string Title, Point[])>();
                foreach (var itemPlot in itemPmtGainDto.Plot)
                {
                    var plotlist = new List<Point>();
                    foreach (var itemPoint in itemPlot.VoltageLightListPoint)
                    {
                        var point = new Point(itemPoint.X, itemPoint.Y);
                        plotlist.Add(point);
                    }

                    var points = (itemPlot.MeasurePower, plotlist.ToArray());
                    plotlists.Add(points);
                }

                Logger.LogHtmlInformation($"PMT ID: {itemPmtGainDto.PmtId},Channel: {itemPmtGainDto.Channel} OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    itemPmtGainDto.PmtId,
                    itemPmtGainDto.Channel,
                    itemPmtGainDto.MeasurePosition,
                    itemPmtGainDto.MeasurePower,
                    PowerPlot = new HtmlPlot2DLinesChart([.. plotlists.Select(t => (t.Title, t.Item2))], itemPmtGainDto.PmtId.ToString() + "_" + itemPmtGainDto.Channel.ToString())
                }), HtmlLogUniqueId.LoggingHtml());
                SynchronizationContextProvider.Send(() => ResultLaserPmtGainDtoList.Add(itemPmtGainDto));
            }

            return true;

            bool GetPmtGain(LaserPmtGainDto laserPmtGainDto, ObservableCollection<LaserPmtGainDto> laserPmtGainList, CancellationToken cancellationToken)
            {
                if (!isAllProtect)
                {
                    LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Scan);
                    LaserViewModel.SetGain(Cache.VoltageMin);

                    Thread.Sleep(1000 * Cache.WaitTime);
                    LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Through);
                }

                if (PowerBuilder?.Length == 0)
                {
                    PowerBuilder.Append(laserPmtGainDto.MeasurePower);
                }
                else
                {
                    PowerBuilder?.Append(',');
                    PowerBuilder?.Append(laserPmtGainDto.MeasurePower);
                }

                //设置功率
                LaserViewModel.SendPrescanByCoefficient(Cache.OpticsMagTypeEnum, double.Parse(laserPmtGainDto.MeasurePower) * 0.005);

                foreach (var item in Enumerable.Range(0, Convert.ToInt32((Cache.VoltageMax - Cache.VoltageMin) / Cache.VoltageInterval + 1)).Select(t => Cache.VoltageMin + t * Cache.VoltageInterval))
                {
                    //是否达到饱和
                    isAllProtect = false;
                    cancellationToken.ThrowIfCancellationRequested();
                    //获取下发的电压
                    var voltage = Math.Round(item, 2);

                    //下发电压
                    LaserViewModel.SetGain(voltage);

                    Thread.Sleep(50);
                    //获取45个PMT测量平均值
                    var pmtData = LaserViewModel.GetPmtDataList();

                    foreach (var pmtGainItem in laserPmtGainList)
                    {
                        if (PowerBuilder != null)
                        {
                            pmtGainItem.MeasurePower = PowerBuilder.ToString();
                        }

                        var point = new Point();
                        if (pmtData.Where(t => t.PmtId == pmtGainItem.PmtId && t.Channel == pmtGainItem.Channel).ToList().Count > 0)
                        {
                            var averagePmt = pmtData.FirstOrDefault(t => t.PmtId == pmtGainItem.PmtId && t.Channel == pmtGainItem.Channel).AvgData;
                            ////判断是否饱和
                            if (averagePmt < Cache.PmtProtectValue)
                            {
                                isAllProtect = true;
                            }

                            point = new Point(voltage, averagePmt);
                            if (pmtGainItem.Plot.Where(t => t.MeasurePower == laserPmtGainDto.MeasurePower).ToList().Count > 0)
                            {
                                pmtGainItem.Plot.FirstOrDefault(t => t.MeasurePower == laserPmtGainDto.MeasurePower).VoltageLightListPoint.Add(point);
                            }
                            else
                            {
                                var points = new List<Point> { point };
                                var pmtItem = new LaserPmtGainItemDto
                                {
                                    MeasurePower = laserPmtGainDto.MeasurePower,
                                    VoltageLightListPoint = points
                                };
                                pmtGainItem.Plot.Add(pmtItem);
                            }

                            LaserPmtGainDtoList = laserPmtGainList;
                            //默认展示PMT1的数据
                            if (SelectLaserPmtGainDto == null && laserPmtGainList.Count > 0)
                            {
                                SelectLaserPmtGainDto = laserPmtGainList[0];
                            }
                        }
                        else
                        {
                            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment($"{Name} Get 45 PMT Data  Error. "), HtmlLogUniqueId.LoggingHtml());
                            return false;
                        }
                    }

                    OnPropertyChanged(nameof(SelectLaserPmtGainDto));
                    if (!isAllProtect)
                    {
                        break;
                    }
                }

                return true;
            }
        }).ConfigureAwait(false);
    }

    //下发CIB接口
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await InvokeCalibrateAsync(() => SendCIB(LaserPmtGainDtoList)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Send CIB Failed", Name);
        }
    }

    //老化验证
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await InvokeCalibrateAsync(() =>
            {
                if (SelectLaserPmtGainDto is null) return false;
                Logger.LogHtmlInformation($"Params: {SelectLaserPmtGainDto.PmtId}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    SelectLaserPmtGainDto.PmtId,
                    SelectLaserPmtGainDto.Channel,
                    SelectLaserPmtGainDto.MeasurePosition,
                    SelectLaserPmtGainDto.MeasurePower
                }), HtmlLogUniqueId.LoggingHtml());
                var pmtName = "PMT" + SelectLaserPmtGainDto.PmtId.ToString() + "_Channel" + SelectLaserPmtGainDto.Channel.ToString();
                var path = Path.Combine(TemplateFileDirectory, "reference", $"{pmtName}.xlsx");
                if (!File.Exists(path))
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: {path} not found,Please Output {pmtName}.xlsx"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                using var dt = ExcelHelper.ReadExcelToDataTable(path, "sheetName1");
                var dicPower = new Dictionary<string, string>();
                if (dt.Rows.Count > 0)
                {
                    var rowIndex = 0;
                    var listName = new List<string>();
                    for (var i = 0; i < dt.Rows.Count; i++)
                    {
                        listName.Add(dt.Rows[i][0].ToString());
                    }

                    if (listName.Where(t => t.Equals(Cache.VoltageAging.ToString())).ToList().Count > 0)
                    {
                        rowIndex = listName.IndexOf(Cache.VoltageAging.ToString());
                    }

                    var dr = dt.Rows[rowIndex];
                    for (var j = 0; j < dt.Columns.Count; j++)
                    {
                        dicPower.Add(dt.Columns[j].ColumnName.ToString(), dr[j].ToString());
                    }

                    foreach (var laserPmtGainItemDto in SelectLaserPmtGainDto.Plot)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        laserPmtGainItemDto.CurrentVoltage = Cache.VoltageAging;
                        if (laserPmtGainItemDto.VoltageLightListPoint.Where(t => t.X == Cache.VoltageAging).ToList().Count > 0)
                        {
                            laserPmtGainItemDto.CurrentPower = laserPmtGainItemDto.VoltageLightListPoint.FirstOrDefault(t => t.X == Cache.VoltageAging).Y;
                        }
                        else
                        {
                            laserPmtGainItemDto.CurrentPower = Cache.PmtProtectValue;
                        }

                        var AgingPowerValue = dicPower[laserPmtGainItemDto.MeasurePower];
                        if (AgingPowerValue == "-") AgingPowerValue = Cache.PmtProtectValue.ToString();
                        laserPmtGainItemDto.AgingPower = double.Parse(AgingPowerValue);
                        if (laserPmtGainItemDto.CurrentPower / double.Parse(AgingPowerValue) * 100 <= Cache.RatioAging)
                        {
                            laserPmtGainItemDto.IsAging = true;
                        }
                        else
                        {
                            laserPmtGainItemDto.IsAging = false;
                        }
                    }

                    var plotlists = new List<(string Title, List<Point>)>();
                    var plotlist1 = new List<Point>();
                    var plotlist2 = new List<Point>();
                    foreach (var itemPlot in SelectLaserPmtGainDto.Plot)
                    {
                        var point1 = new Point(double.Parse(itemPlot.MeasurePower), itemPlot.CurrentPower);
                        plotlist1.Add(point1);
                        var point2 = new Point(double.Parse(itemPlot.MeasurePower), itemPlot.AgingPower);
                        plotlist2.Add(point2);
                    }

                    var plotlists1 = ("CurrentVoltage:" + Cache.VoltageAging, plotlist1);
                    var plotlists2 = ("AgingVoltage:" + Cache.VoltageAging, plotlist2);
                    plotlists.Add(plotlists1);
                    plotlists.Add(plotlists2);
                    Logger.LogHtmlInformation($"Get PMTAging Success PMT ID: {SelectLaserPmtGainDto.PmtId} OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        SelectLaserPmtGainDto.PmtId,
                        SelectLaserPmtGainDto.Channel,
                        SelectLaserPmtGainDto.MeasurePosition,
                        SelectLaserPmtGainDto.MeasurePower,
                        path,
                        PowerPlot = new HtmlPlot2DLinesChart([.. plotlists.Select(t => (t.Title, t.Item2.ToArray()))], SelectLaserPmtGainDto.PmtId.ToString() + "_" + SelectLaserPmtGainDto.Channel.ToString())
                    }), HtmlLogUniqueId.LoggingHtml());
                }

                return true;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get PMTAging Failed", Name);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await InvokeVerifyAsync(() => SendCIB(ReviewList)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Send CIB Failed", Name);
        }
    }

    private bool Save(LaserPmtGainDto itemDto, CancellationToken cancellationToken, bool isSave = true) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibrations =
        [
            .. Calibrations
                .Where(t => (t.PmtId == itemDto.PmtId && t.Channel == itemDto.Channel) == false),
            itemDto.Clone(),
        ];

        if (isSave == false) return true;

        return CacheProvider.SetArray(Calibrations, cancellationToken) && CacheProvider.Set(Cache, cancellationToken);
    });

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(ResultLaserPmtGainDtoList.Clear);
    }

    private bool SendCIB(ObservableCollection<LaserPmtGainDto> laserPmtGainDtos)
    {
        var pmtIdList = new List<string>();
        if (Cache.PmtList.Contains(","))
        {
            pmtIdList = [.. Cache.PmtList.Split(',')];
        }
        else
        {
            pmtIdList.Add(Cache.PmtList);
        }

        var channelList = new List<string>();
        if (Cache.ChannelList.Contains(","))
        {
            channelList = [.. Cache.ChannelList.Split(',')];
        }
        else
        {
            channelList.Add(Cache.ChannelList);
        }

        Logger.LogHtmlInformation("Params", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            Cache.PmtList,
            Cache.ChannelList,
            Cache.LineValue,
            Cache.MinValue,
            Cache.MaxValue
        }), HtmlLogUniqueId.LoggingHtml());

        foreach (var itemPmtGainDto in laserPmtGainDtos)
        {
            if (pmtIdList.Any(t => t == itemPmtGainDto.PmtId.ToString()) && channelList.Any(t => t == itemPmtGainDto.Channel.ToString()))
            {
                var dicData = new Dictionary<int, List<int>>();
                foreach (var itemPlot in itemPmtGainDto.Plot)
                {
                    var datalist = new List<int>();
                    foreach (var itemPoint in itemPlot.VoltageLightListPoint)
                    {
                        datalist.Add(Convert.ToInt16(itemPoint.Y));
                    }

                    dicData.Add(int.Parse(itemPlot.MeasurePower), datalist);
                }

                var (datavge, data) = CalibrationAlgorithmService.GetPmtGain(dicData, Cache.LineValue, Cache.MinValue, Cache.MaxValue);

                var path = Path.Combine(TemplateFileDirectory, "CibData", itemPmtGainDto.PmtId.ToString(), itemPmtGainDto.Channel.ToString(), Cache.LineValue.ToString());
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var datavgePath = Path.Combine(path, "datavge.txt");
                var dataPath = Path.Combine(path, "data.txt");
                File.WriteAllLines(datavgePath, datavge);
                File.WriteAllLines(dataPath, data);
                LaserViewModel.SendPmtGain(data, datavge, itemPmtGainDto.PmtId, itemPmtGainDto.Channel);
                Logger.LogHtmlInformation($"Send Cib PMT ID: {itemPmtGainDto.PmtId},Channel: {itemPmtGainDto.Channel} OK", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    itemPmtGainDto.PmtId,
                    itemPmtGainDto.Channel,
                    itemPmtGainDto.MeasurePosition,
                    itemPmtGainDto.MeasurePower,
                    datavgePath,
                    dataPath
                }), HtmlLogUniqueId.LoggingHtml());
            }
        }

        return true;
    }

    #endregion 校准
}