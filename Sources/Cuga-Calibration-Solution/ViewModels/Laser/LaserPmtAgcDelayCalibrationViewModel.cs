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
using Core.Models.Models.Setting;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Extensions;
using Net.Utilities.Helper.Enum;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserPmtAgcDelayCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserPmtAgcDelayCalibrationViewModel(CalibrationSetting calibrationSetting) : CalibrationViewModelBase
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

        return isHasCache || CacheProvider.Set(Cache, cancellationToken);
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Empty);

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
        ];

        if (ReviewList.All(t => t.IsCalibrated == false))
            return false;

        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Empty);

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
            try
            {
                LaserPmtAgcDelayItemDtoList = [];

                using var semaphore = new SemaphoreSlim(Cache.ConcurrentCount, Cache.ConcurrentCount);

                var pmtConfig = calibrationSetting.SettingPmtConfigParam.PmtConfigList;

                Cache.PmtIdList = LaserViewModel.GetUsedPmtIdList();
                var darkFieldPmtDelayDtos = LaserViewModel.GetPmtDelayList();

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.OpticsMagTypeEnum,
                    Cache.CatchCount,
                    Cache.RetryCount,
                    Cache.ConcurrentCount,
                    Cache.Threshold
                }), HtmlLogUniqueId.LoggingHtml());

                StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Empty);
                LaserViewModel.SendOpticsMagType(Cache.OpticsMagTypeEnum);

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

                return (await Task.WhenAll(LaserPmtAgcDelayItemDtoList.Select(item => Task.Run(async () =>
                {
                    return await GetPmtAgcDelayAsync(
                        item,
                        darkFieldPmtDelayDtos.Single(t => t.PmtId == item.PmtId && t.ChannelId == 1),
                        darkFieldPmtDelayDtos.Single(t => t.PmtId == item.PmtId && t.ChannelId == 2),
                        darkFieldPmtDelayDtos.Single(t => t.PmtId == item.PmtId && t.ChannelId == 3),
                        // ReSharper disable once AccessToDisposedClosure
                        semaphore,
                        cancellationToken);
                }, cancellationToken)))).All(b => b);
            }
            finally
            {
                LaserViewModel.ToggleEnableAutoGain(false);
                LaserViewModel.ToggleEnableMarkMode(false);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(async () =>
        {
            if (SelectReviewList.Count == 0)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            try
            {
                using var semaphore = new SemaphoreSlim(Cache.ConcurrentCount, Cache.ConcurrentCount);

                var darkFieldPmtDelayDtos = LaserViewModel.GetPmtDelayList();

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.OpticsMagTypeEnum,
                    Cache.CatchCount,
                    Cache.RetryCount,
                    Cache.ConcurrentCount,
                    Cache.Threshold
                }), HtmlLogUniqueId.LoggingHtml());

                StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Empty);
                LaserViewModel.SendOpticsMagType(Cache.OpticsMagTypeEnum);

                Logger.LogHtmlInformation("Find Pmt Agc Delay", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var result = (await Task.WhenAll(SelectReviewList.Select(item => Task.Run(async () =>
                {
                    item.IsVerified = false;
                    var isOk = await GetPmtAgcDelayAsync(
                        item,
                        darkFieldPmtDelayDtos.Single(t => t.PmtId == item.PmtId && t.ChannelId == 1),
                        darkFieldPmtDelayDtos.Single(t => t.PmtId == item.PmtId && t.ChannelId == 2),
                        darkFieldPmtDelayDtos.Single(t => t.PmtId == item.PmtId && t.ChannelId == 3),
                        // ReSharper disable once AccessToDisposedClosure
                        semaphore,
                        cancellationToken,
                        true);
                    item.IsVerified = isOk;

                    return isOk;
                }, cancellationToken)))).All(b => b);

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
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken,
        bool isReview = false)
    {
        var lockToken = false;
        var ch1DelayClone = ch1Delay.Clone();
        var ch2DelayClone = ch2Delay.Clone();
        var ch3DelayClone = ch3Delay.Clone();
        try
        {
            lockToken = await semaphore.WaitAsync(int.MaxValue, cancellationToken);

            Logger.LogHtmlInformation($"Pmt ID: {item.PmtId}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

            var height = LaserViewModel.GetDarkFieldLineScanImageYPixelHeight(Cache.OpticsMagTypeEnum);
            var baseIndex = height / 2d;

            var count = 1;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ch1Delay.AgcDelay = item.Channel1AgcDelay;
                ch2Delay.AgcDelay = item.Channel2AgcDelay;
                ch3Delay.AgcDelay = item.Channel3AgcDelay;

                LaserViewModel.SetPmtDelayList([ch1Delay, ch2Delay, ch3Delay]);

                LaserViewModel.ToggleEnableAutoGain(true);
                LaserViewModel.ToggleEnableMarkMode(true);

                await Task.Delay(3000, cancellationToken);

                await Task.WhenAll(
                    Task.Run(() => item.Channel1SenseData = LaserViewModel.GetPmtSenseDataList(item.PmtId, 1, Cache.CatchCount), cancellationToken),
                    Task.Run(() => item.Channel2SenseData = LaserViewModel.GetPmtSenseDataList(item.PmtId, 2, Cache.CatchCount), cancellationToken),
                    Task.Run(() => item.Channel3SenseData = LaserViewModel.GetPmtSenseDataList(item.PmtId, 3, Cache.CatchCount), cancellationToken)
                );

                item.Channel1AgcOffset = baseIndex - item.Channel1SenseData.Select(t => Vector<double>.Build.Dense([.. t]).MinimumIndex()).Average();
                item.Channel2AgcOffset = baseIndex - item.Channel2SenseData.Select(t => Vector<double>.Build.Dense([.. t]).MinimumIndex()).Average();
                item.Channel3AgcOffset = baseIndex - item.Channel3SenseData.Select(t => Vector<double>.Build.Dense([.. t]).MinimumIndex()).Average();

                var channel1IsOk = Math.Abs(item.Channel1AgcOffset) <= Cache.Threshold;
                var channel2IsOk = Math.Abs(item.Channel2AgcOffset) <= Cache.Threshold;
                var channel3IsOk = Math.Abs(item.Channel3AgcOffset) <= Cache.Threshold;
                var isOk = channel1IsOk && channel2IsOk && channel3IsOk;

                var htmlBullet = new HtmlBullet(new
                {
                    item.Channel1AgcDelay,
                    item.Channel1AgcOffset,
                    channel1IsOk,
                    item.Channel2AgcDelay,
                    item.Channel2AgcOffset,
                    channel2IsOk,
                    item.Channel3AgcDelay,
                    item.Channel3AgcOffset,
                    channel3IsOk,
                    Channel1SenseData = new HtmlPlot2DLinesChart([.. item.Channel1SenseData.Select((t, i) => (i.ToString(), t.ToPoints()))], "Channel 1 Sense Data"),
                    Channel2SenseData = new HtmlPlot2DLinesChart([.. item.Channel2SenseData.Select((t, i) => (i.ToString(), t.ToPoints()))], "Channel 2 Sense Data"),
                    Channel3SenseData = new HtmlPlot2DLinesChart([.. item.Channel3SenseData.Select((t, i) => (i.ToString(), t.ToPoints()))], "Channel 3 Sense Data")
                });

                if (isReview)
                {
                    if (isOk)
                        Logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                    else
                        Logger.LogHtmlError("Failed", HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());

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
                    Logger.LogHtmlInformation($"time: {count} OK", HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                    return true;
                }

                Logger.LogHtmlInformation($"time: {count}", HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                if (++count > Cache.RetryCount) ThrowHelper.ThrowInvalidOperationException("Pmt Delay Retry Limit Exceeded");
            }
        }
        finally
        {
            if (lockToken) semaphore.Release();
            LaserViewModel.SetPmtDelayList([ch1DelayClone, ch2DelayClone, ch3DelayClone]);
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

        return CacheProvider.SetArray(Calibrations, cancellationToken)
               && CacheProvider.Set(Cache, cancellationToken)
               && EnableDependedCalibrationItems(cancellationToken);
    });

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