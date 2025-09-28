using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.PmtAgcDelay;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Local.NoSQL.DB.Providers.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Enums.Loggings;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using SourceGenerator.AssemblyMetadata;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserPmtAgcDelayCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserPmtAgcDelayCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum);

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select a Mag" },
        new() { StepName = "Agc Delay" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private LaserPmtAgcDelayItemDto[] _laserPmtAgcDelayItemDtoList = [];

    [ObservableProperty]
    private LaserPmtAgcDelayItemDto? _selectCalibrationLaserPmtAgcDelayItemDto;

    [ObservableProperty]
    private ObservableCollection<OpticsMagTypeEnumCalibrationStatus> _calibrationStatusList =
    [
        .. EnumHelper.Enums<OpticsMagTypeEnum>().Select(t => new OpticsMagTypeEnumCalibrationStatus { OpticsMagTypeEnum = t, IsCalibrated = false })
    ];

    #endregion Calibrate

    [ObservableProperty]
    private LaserPmtAgcDelayItemDto[] _reviewList = [];

    [ObservableProperty]
    private ObservableCollection<LaserPmtAgcDelayItemDto> _selectReviewList = [];

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private LaserPmtAgcDelayCache _cache = new();

    [ObservableProperty]
    private LaserPmtAgcDelayItemDto[] _calibrations = [];

    [ObservableProperty]
    private MicroscopeCalChipDto _microscopeCalChip = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

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

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<LaserPmtAgcDelayCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserPmtAgcDelayItemDto>();

        foreach (var calibrationStatus in Calibrations)
        {
            CalibrationStatusList
                .Single(t => t.OpticsMagTypeEnum == calibrationStatus.OpticsMagTypeEnum)
                .IsCalibrated = calibrationStatus.IsCalibrated;
        }

        if (isHasCache == false) CacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewList =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .OrderBy(t => t.OpticsMagTypeEnum)
                .ThenBy(t => t.PmtId)
        ];

        if (ReviewList.All(t => t.IsCalibrated == false))
            return false;

        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                return true;

            case 1:
                LaserPmtAgcDelayItemDtoList.ForEach(t => t.IsCalibrated = true);
                if (Save([.. LaserPmtAgcDelayItemDtoList], cancellationToken) == false)
                {
                    LaserPmtAgcDelayItemDtoList.ForEach(t => t.IsCalibrated = false);

                    Logger.LogError("{@Name} Error: Save Failed!", Name);
                    return false;
                }

                CalibrationStatusList.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).IsCalibrated = true;
                DialogWindowProvider.ShowDialog("Find Offset Ok!");

                IsCalibrated = CalibrationStatusList.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand]
    private Task Step0CalibrateActionAsync()
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
        return InvokeCalibrateAsync(async () =>
        {
            LaserPmtAgcDelayItemDtoList = [];

            var pmtConfig = CalibrationSetting.SettingPmtConfigParam.PmtConfigList;

            Cache.PmtIdList = [.. LaserViewModel.GetIsUsedCIBConfigList().Select(t => t.PmtId)];
            var darkFieldPmtDelayDtos = LaserViewModel.GetCIBDelayList();

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsMagTypeEnum,
                Cache.CatchCount,
                Cache.RetryCount,
                Cache.ConcurrentCount,
                Cache.Threshold
            }), HtmlLogUniqueId.LoggingHtml());

            StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);
            LaserViewModel.ToggleOpticsMagType(Cache.OpticsMagTypeEnum);

            Logger.LogHtmlInformation("Find Pmt Agc Delay", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            foreach (var pmtId in Cache.PmtIdList)
            {
                if (pmtConfig.Single(t => t.Id == pmtId).Enabled == false) continue;

                var laserPmtAgcDelayItemDto = new LaserPmtAgcDelayItemDto
                {
                    OpticsMagTypeEnum = Cache.OpticsMagTypeEnum,
                    PmtId = pmtId
                };

                LaserPmtAgcDelayItemDtoList = [.. LaserPmtAgcDelayItemDtoList, laserPmtAgcDelayItemDto];
            }

            var results = new List<bool>();

            foreach (var agcDelayItemList in LaserPmtAgcDelayItemDtoList
                         .OrderBy(t => t.PmtId)
                         .Batch(Cache.ConcurrentCount))
            {
                SelectCalibrationLaserPmtAgcDelayItemDto = agcDelayItemList.FirstOrDefault();

                results.AddRange(await Task.WhenAll(agcDelayItemList.Select(item => Task.Run(async () =>
                {
                    return await GetPmtAgcDelayAsync(
                        item,
                        darkFieldPmtDelayDtos.Single(t => t.PmtId == item.PmtId && t.ChannelId == 1),
                        darkFieldPmtDelayDtos.Single(t => t.PmtId == item.PmtId && t.ChannelId == 2),
                        darkFieldPmtDelayDtos.Single(t => t.PmtId == item.PmtId && t.ChannelId == 3),
                        cancellationToken);
                }, cancellationToken))));
            }

            return results.All(b => b);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(async () =>
        {
            if (SelectReviewList.Any(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum) == false)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            try
            {
                var darkFieldPmtDelayDtos = LaserViewModel.GetCIBDelayList();

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.OpticsMagTypeEnum,
                    Cache.CatchCount,
                    Cache.RetryCount,
                    Cache.ConcurrentCount,
                    Cache.Threshold
                }), HtmlLogUniqueId.LoggingHtml());

                StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);
                LaserViewModel.ToggleOpticsMagType(Cache.OpticsMagTypeEnum);

                Logger.LogHtmlInformation("Find Pmt Agc Delay", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var results = new List<bool>();

                foreach (var agcDelayItemList in SelectReviewList
                             .Where(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum)
                             .OrderBy(t => t.PmtId)
                             .Batch(Cache.ConcurrentCount))
                {
                    SelectCalibrationLaserPmtAgcDelayItemDto = agcDelayItemList.FirstOrDefault();

                    results.AddRange(await Task.WhenAll(agcDelayItemList.Select(item => Task.Run(async () =>
                    {
                        return await GetPmtAgcDelayAsync(
                            item,
                            darkFieldPmtDelayDtos.Single(t => t.PmtId == item.PmtId && t.ChannelId == 1),
                            darkFieldPmtDelayDtos.Single(t => t.PmtId == item.PmtId && t.ChannelId == 2),
                            darkFieldPmtDelayDtos.Single(t => t.PmtId == item.PmtId && t.ChannelId == 3),
                            cancellationToken);
                    }, cancellationToken))));
                }

                var result = results.All(b => b);

                if (Save([.. SelectReviewList], cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    SelectReviewList.ForEach(t => t.IsVerified = false);

                    return false;
                }

                DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);

                return result;
            }
            finally
            {
                LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Close);
            }
        }).ConfigureAwait(false);
    }

    private async Task<bool> GetPmtAgcDelayAsync(
        LaserPmtAgcDelayItemDto item,
        DarkFieldPmtDelayDto ch1Delay,
        DarkFieldPmtDelayDto ch2Delay,
        DarkFieldPmtDelayDto ch3Delay,
        CancellationToken cancellationToken,
        bool isReview = false)
    {
        var ch1DelayClone = ch1Delay.Clone();
        var ch2DelayClone = ch2Delay.Clone();
        var ch3DelayClone = ch3Delay.Clone();
        try
        {
            var htmlElementList = new List<HtmlHeader>();

            Logger.LogInformation("Pmt ID: {ItemPmtId}", item.PmtId);

            try
            {
                var height = LaserViewModel.GetDarkFieldLineScanImageYPixelHeight(Cache.OpticsMagTypeEnum, false);
                // 从1开始
                var baseIndex = (height + 1) / 2d;
                if (isReview == false)
                {
                    item.Channel1AgcDelay = baseIndex;
                    item.Channel2AgcDelay = baseIndex;
                    item.Channel3AgcDelay = baseIndex;
                }

                var count = 1;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ch1Delay.AgcDelay = item.Channel1AgcDelay;
                    ch2Delay.AgcDelay = item.Channel2AgcDelay;
                    ch3Delay.AgcDelay = item.Channel3AgcDelay;

                    LaserViewModel.ToggleEnableAutoGainControl(false, item.PmtId);
                    LaserViewModel.ToggleEnableMarkMode(false, item.PmtId);

                    LaserViewModel.SetCIBDelayList([ch1Delay, ch2Delay, ch3Delay]);

                    LaserViewModel.ToggleEnableAutoGainControl(true, item.PmtId);
                    LaserViewModel.ToggleEnableMarkMode(true, item.PmtId);

                    await Task.Delay(3000, cancellationToken);

                    var result = await LaserViewModel.GetCIBOfSenseDataListAsync(Cache.CatchCount, item.PmtId);
                    item.Channel1SenseData = result[0];
                    item.Channel2SenseData = result[1];
                    item.Channel3SenseData = result[2];

                    // 从1开始
                    var channel1Index = item.Channel1SenseData.Select(GetMiddleIndex).Average() + 1;
                    var channel2Index = item.Channel2SenseData.Select(GetMiddleIndex).Average() + 1;
                    var channel3Index = item.Channel3SenseData.Select(GetMiddleIndex).Average() + 1;

                    item.Channel1AgcOffset = baseIndex - channel1Index;
                    item.Channel2AgcOffset = baseIndex - channel2Index;
                    item.Channel3AgcOffset = baseIndex - channel3Index;

                    var channel1IsOk = Math.Abs(item.Channel1AgcOffset) <= Cache.Threshold;
                    var channel2IsOk = Math.Abs(item.Channel2AgcOffset) <= Cache.Threshold;
                    var channel3IsOk = Math.Abs(item.Channel3AgcOffset) <= Cache.Threshold;
                    var isOk = channel1IsOk && channel2IsOk && channel3IsOk;

                    var htmlLog = new HtmlLog(
                        isOk ? LogLevelEnum.Info : LogLevelEnum.Error,
                        DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat),
                        CugaCalibrationCoreWcfAssemblyMetadata.Version,
                        [
                            new HtmlBullet(new
                            {
                                item.OpticsMagTypeEnum,
                                item.PmtId,
                                ch1DelayCurrentPmtDelay = ch1DelayClone.PmtDelay,
                                ch1DelayCurrentSenseDelay = ch1DelayClone.SenseDelay,
                                ch1DelayOldAgcDelay = ch1DelayClone.AgcDelay,
                                ch1DelayCurrentAgcDelay = item.Channel1AgcDelay,
                                ch2DelayCurrentPmtDelay = ch2DelayClone.PmtDelay,
                                ch2DelayCurrentSenseDelay = ch2DelayClone.SenseDelay,
                                ch2DelayOldAgcDelay = ch2DelayClone.AgcDelay,
                                ch2DelayCurrentAgcDelay = item.Channel2AgcDelay,
                                ch3DelayCurrentPmtDelay = ch3DelayClone.PmtDelay,
                                ch3DelayCurrentSenseDelay = ch3DelayClone.SenseDelay,
                                ch3DelayOldAgcDelay = ch3DelayClone.AgcDelay,
                                ch3DelayCurrentAgcDelay = item.Channel3AgcDelay,
                                baseIndex,
                                channel1Index,
                                channel2Index,
                                channel3Index,
                                item.Channel1AgcOffset,
                                channel1IsOk,
                                item.Channel2AgcOffset,
                                channel2IsOk,
                                item.Channel3AgcOffset,
                                channel3IsOk,
                                Channel1SenseData = new HtmlPlot2DLinesChart([.. item.Channel1SenseData.Select((t, i) => (i.ToString(), t.ToPoints()))], "Channel 1 Sense Data"),
                                Channel2SenseData = new HtmlPlot2DLinesChart([.. item.Channel2SenseData.Select((t, i) => (i.ToString(), t.ToPoints()))], "Channel 2 Sense Data"),
                                Channel3SenseData = new HtmlPlot2DLinesChart([.. item.Channel3SenseData.Select((t, i) => (i.ToString(), t.ToPoints()))], "Channel 3 Sense Data")
                            })
                        ]);

                    if (isReview)
                    {
                        var endHeader = new HtmlHeader(
                            isOk ? "OK" : "Failed",
                            HtmlHeaderLevelEnum.Header5,
                            htmlLog.LogLevelEnum,
                            htmlLog);
                        htmlElementList.Add(endHeader);

                        if (isOk)
                        {
                            Logger.LogInformation($"{{{nameof(endHeader)}}}", endHeader.ToViewString());
                        }
                        else
                        {
                            Logger.LogError($"{{{nameof(endHeader)}}}", endHeader.ToViewString());
                        }

                        return isOk;
                    }

                    if (channel1IsOk == false)
                    {
                        item.Channel1AgcDelay += item.Channel1AgcOffset;
                        if (item.Channel1AgcDelay < 0) item.Channel1AgcDelay += height;
                    }

                    if (channel2IsOk == false)
                    {
                        item.Channel2AgcDelay += item.Channel2AgcOffset;
                        if (item.Channel2AgcDelay < 0) item.Channel2AgcDelay += height;
                    }

                    if (channel3IsOk == false)
                    {
                        item.Channel3AgcDelay += item.Channel3AgcOffset;
                        if (item.Channel3AgcDelay < 0) item.Channel3AgcDelay += height;
                    }

                    if (isOk)
                    {
                        var endHeader = new HtmlHeader(
                            $"time: {count} OK",
                            HtmlHeaderLevelEnum.Header5,
                            htmlLog.LogLevelEnum,
                            htmlLog);
                        htmlElementList.Add(endHeader);

                        Logger.LogInformation($"{{{nameof(endHeader)}}}", endHeader.ToViewString());

                        return true;
                    }

                    var elementHtmlHeader = new HtmlHeader(
                        $"time: {count}",
                        HtmlHeaderLevelEnum.Header5,
                        htmlLog.LogLevelEnum,
                        htmlLog);
                    htmlElementList.Add(elementHtmlHeader);

                    Logger.LogInformation($"{{{nameof(elementHtmlHeader)}}}", elementHtmlHeader.ToViewString());

                    if (++count > Cache.RetryCount) ThrowHelper.ThrowInvalidOperationException("Pmt Delay Retry Limit Exceeded");
                }
            }
            finally
            {
                Logger.LogHtmlInformation([.. htmlElementList], HtmlLogUniqueId.LoggingHtml());
            }
        }
        finally
        {
            LaserViewModel.ToggleEnableAutoGainControl(false, item.PmtId);
            LaserViewModel.ToggleEnableMarkMode(false, item.PmtId);
            LaserViewModel.SetCIBDelayList([ch1DelayClone, ch2DelayClone, ch3DelayClone]);
        }

        int GetMiddleIndex(IReadOnlyList<double> values)
        {
            if (HostEnvironment.IsDevelopment()) return 10;

            var targetValue = values.Min() + (values.Max() - values.Min()) * 2d / 3d;
            var changedList = values.ToPoints().Where(t => t.Y < targetValue).ToList();

            var startIndex = Convert.ToInt32(changedList[0].X);
            var endIndex = Convert.ToInt32(changedList[^1].X);

            return (startIndex + endIndex) / 2;
        }
    }

    private bool Save(LaserPmtAgcDelayItemDto[] itemDtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        foreach (var itemDto in itemDtos)
        {
            update(itemDto);

            Calibrations =
            [
                .. Calibrations
                    .Where(t => (t.OpticsMagTypeEnum == itemDto.OpticsMagTypeEnum && t.PmtId == itemDto.PmtId) == false),
                itemDto.Clone()
            ];
        }

        update(Cache);

        CacheProvider.SetArray(Calibrations, cancellationToken);
        CacheProvider.Set(Cache, cancellationToken);
    }) && EnableDependedCalibrationItems(cancellationToken);

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependLaserPmtAgcDelayCalibrations(false, cancellationToken, out var errorMsg) == false)
        {
            Logger.LogError("Toggle {@Name} Enable Status Failed!", errorMsg);
            return false;
        }

        return true;
    }

    #endregion 校准
}