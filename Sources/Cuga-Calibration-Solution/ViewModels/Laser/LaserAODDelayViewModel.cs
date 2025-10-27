using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AodDelay;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using CugaCalibration.ViewModels.Common;
using Local.NoSQL.DB.Providers.Extensions;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserAODDelayViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserAODDelayViewModel(AfViewModel afViewModel) : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum);

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select a Mag", DefaultIsNextEnable = true },
        new() { StepName = "Find a Position", DefaultIsNextEnable = true },
        new() { StepName = "AOD Delay" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<LaserAODDelayDto> _laserAodDelayItemDtoList = [];

    [ObservableProperty]
    private ObservableCollection<OpticsMagTypeEnumCalibrationStatus> _calibrationStatusList =
    [
        .. EnumHelper.Enums<OpticsMagTypeEnum>().Select(t => new OpticsMagTypeEnumCalibrationStatus { OpticsMagTypeEnum = t, IsCalibrated = false })
    ];

    [ObservableProperty]
    private List<Point> _aodDelayList = [];

    [ObservableProperty]
    private LaserAODDelayDto? _selectedCalibrateItemDto;

    #endregion Calibrate

    [ObservableProperty]
    private ObservableCollection<LaserAODDelayDto> _reviewList = [];

    [ObservableProperty]
    private LaserAODDelayDto? _selectReviewItemDto;

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private LaserAODDelayCache _cache = new();

    [ObservableProperty]
    private LaserAODDelayDto[] _calibrations = [];

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

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<LaserAODDelayCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<LaserAODDelayDto>();

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

        Cache.FindPosition = MicroscopeCalChip.HazeBrightFieldMachinePosition;
        StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));
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
        StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(Cache.FindPosition);
        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        var darkFieldPosition = StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition);
        switch (CalibrationStepIndex)
        {
            case 0:
                StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(darkFieldPosition);
                return true;

            case 1:
                ClearCalibrationTemp();
                StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(darkFieldPosition);
                return true;

            case 2:
                if (SelectedCalibrateItemDto is null)
                {
                    DialogWindowProvider.TryShowDialog("Please find Aod delay!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    SelectedCalibrateItemDto.IsCalibrated = true;
                    if (Save(SelectedCalibrateItemDto, cancellationToken) == false)
                    {
                        SelectedCalibrateItemDto.IsCalibrated = false;
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        return false;
                    }
                }

                CalibrationStatusList.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).IsCalibrated = true;
                DialogWindowProvider.ShowDialog("Find Offset Ok!");

                IsCalibrated = CalibrationStatusList.All(s => s.IsCalibrated);
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
    private async Task GetPointAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                var result = StageViewModel.GetDarkFieldStagePosition();

                Cache.FindPosition = result;
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
            await Task.Run(() => StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition))).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
        }
    }

    [RelayCommand]
    private void Review(List<double> list)
    {
        DialogWindowProvider.ShowPlot([.. list]);
    }

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
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            ClearCalibrationTemp();

            var roughAodDelayMin = Cache.GetRoughAodDelayMin();
            var roughAodDelayMax = Cache.GetRoughAodDelayMax();
            var roughFindInterval = Cache.GetRoughFindInterval();
            var refinedRange = Cache.GetRefinedRange();
            var refinedFindInterval = Cache.GetRefinedFindInterval();

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsMagTypeEnum,
                Cache.FindPosition,
                RoughAodDelayMin = roughAodDelayMin,
                RoughAodDelayMax = roughAodDelayMax,
                RoughFindInterval = roughFindInterval,
                RefinedRange = refinedRange,
                RefinedFindInterval = refinedFindInterval
            }), HtmlLogUniqueId.LoggingHtml());

            StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));

            // 自动聚焦
            var isAutoFocus = afViewModel.SetDarkFieldAutoFocus(null, Cache.OpticsMagTypeEnum, CalChipSiteModelEnum.HazeModel);
            if (isAutoFocus) afViewModel.ToggleDarkFieldEnable(true);

            LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Through);

            if (roughAodDelayMin > roughAodDelayMax || roughFindInterval <= 0 || refinedRange <= 0 || refinedFindInterval <= 0)
            {
                DialogWindowProvider.ShowDialog("Please set the correct parameters!(Rough Aod Delay Min <= Rough Aod  Delay Max and Rough Find Interval > 0 and Refined Range > 0 and Refined Find Interval > 0 )", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            Logger.LogHtmlInformation("Find Aod Delay", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            var index = 1;
            foreach (var aodDelay in ((double[])[roughAodDelayMin, .. Enumerable.Range(1, (int)Math.Floor((roughAodDelayMax - roughAodDelayMin) / roughFindInterval)).Select(x => roughAodDelayMin + x * roughFindInterval), roughAodDelayMax]).Distinct())
            {
                var aodDelayObjDto = new LaserAODDelayDto
                {
                    OpticsMagTypeEnum = Cache.OpticsMagTypeEnum,
                    RoughAodDelayTime = aodDelay,
                    RefinedAodDelayTime = aodDelay,
                    Index = index++
                };
                cancellationToken.ThrowIfCancellationRequested();
                if (await GetAodDelayAsync(aodDelayObjDto, cancellationToken).ConfigureAwait(false) == false) return false;
            }

            var aodDelayItemDtoList = LaserAodDelayItemDtoList.OrderByDescending(t => t.AveragePmtData).ToList();
            if (aodDelayItemDtoList.Count < 0)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Find Aod Delay list Is Empty."), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            var roughAodDelayItemDto = aodDelayItemDtoList[0];
            var refinedMin = roughAodDelayItemDto.RoughAodDelayTime - refinedRange;
            var refinedMax = roughAodDelayItemDto.RoughAodDelayTime + refinedRange;
            foreach (var aodDelay in ((double[])[refinedMin, .. Enumerable.Range(1, (int)Math.Floor((refinedMax - refinedMin) / refinedFindInterval)).Select(x => refinedMin + x * refinedFindInterval), refinedMax]).Distinct())
            {
                if (LaserAodDelayItemDtoList.Any(t => t.RefinedAodDelayTime - aodDelay == 0)) continue;
                var aodDelayObjDto = new LaserAODDelayDto
                {
                    OpticsMagTypeEnum = Cache.OpticsMagTypeEnum,
                    RoughAodDelayTime = roughAodDelayItemDto.RoughAodDelayTime,
                    RefinedAodDelayTime = aodDelay,
                    Index = index++
                };
                cancellationToken.ThrowIfCancellationRequested();
                if (await GetAodDelayAsync(aodDelayObjDto, cancellationToken).ConfigureAwait(false) == false) return false;
            }

            aodDelayItemDtoList = [.. LaserAodDelayItemDtoList.OrderByDescending(t => t.AveragePmtData)];
            if (aodDelayItemDtoList.Count < 0)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Find Aod Delay list Is Empty."), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            SelectedCalibrateItemDto = aodDelayItemDtoList[0];

            LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Close);

            Logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                SelectedCalibrateItemDto.RefinedAodDelayTime,
                SelectedCalibrateItemDto.RefinedPrescanAodDelayTime,
                SelectedCalibrateItemDto.RefinedChirpAodDelayTime,
                SelectedCalibrateItemDto.AveragePmtData,
                MaxAveragePmtList = new HtmlPlot2DLinesChart([
                    ("Average Pmt", LaserAodDelayItemDtoList
                        .Select(t => (t.RefinedAodDelayTime, AveragePmt: t.AveragePmtData))
                        .OrderBy(t => t.RefinedAodDelayTime)
                        .Select(t => new Point(t.RefinedAodDelayTime, t.AveragePmt))
                        .ToArray())
                ], "Average Pmt")
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(async () =>
        {
            if (SelectReviewItemDto is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            ClearCalibrationTemp();

            SelectReviewItemDto.IsVerified = false;

            var roughAodDelayMin = Cache.GetRoughAodDelayMin();
            var roughAodDelayMax = Cache.GetRoughAodDelayMax();
            var roughFindInterval = Cache.GetRoughFindInterval();
            var refinedRange = Cache.GetRefinedRange();
            var refinedFindInterval = Cache.GetRefinedFindInterval();

            Cache.OpticsMagTypeEnum = SelectReviewItemDto.OpticsMagTypeEnum;

            if (roughAodDelayMin > roughAodDelayMax || roughFindInterval <= 0 || refinedRange <= 0 || refinedFindInterval <= 0)
            {
                DialogWindowProvider.ShowDialog("Please set the correct parameters!(Rough Aod Delay Min <= Rough Aod  Delay Max and Rough Find Interval > 0 and Refined Range > 0 and Refined Find Interval > 0 )", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition));

            // 自动聚焦
            var isAutoFocus = afViewModel.SetDarkFieldAutoFocus(null, Cache.OpticsMagTypeEnum, CalChipSiteModelEnum.HazeModel);
            if (isAutoFocus) afViewModel.ToggleDarkFieldEnable(true);

            LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Through);

            var index = 1;
            var refinedMin = SelectReviewItemDto.RefinedAodDelayTime - refinedRange;
            var refinedMax = SelectReviewItemDto.RefinedAodDelayTime + refinedRange;
            foreach (var aodDelay in ((double[])[refinedMin, .. Enumerable.Range(1, (int)Math.Floor((refinedMax - refinedMin) / refinedFindInterval)).Select(x => refinedMin + x * refinedFindInterval), refinedMax]).Distinct())
            {
                if (LaserAodDelayItemDtoList.Any(t => t.RefinedAodDelayTime - aodDelay == 0)) continue;
                var aodDelayObjDto = new LaserAODDelayDto
                {
                    OpticsMagTypeEnum = Cache.OpticsMagTypeEnum,
                    RoughAodDelayTime = SelectReviewItemDto.RoughAodDelayTime,
                    RefinedAodDelayTime = aodDelay,
                    Index = index++
                };
                cancellationToken.ThrowIfCancellationRequested();
                if (await GetAodDelayAsync(aodDelayObjDto, cancellationToken).ConfigureAwait(false) == false) return false;
            }

            var aodDelayItemDtoList = LaserAodDelayItemDtoList.OrderByDescending(t => t.AveragePmtData).ToList();
            if (aodDelayItemDtoList.Count < 0)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Find Aod Delay list Is Empty."), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            LaserViewModel.ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum.Close);

            var refinedAodDelayTime = aodDelayItemDtoList[0].RefinedAodDelayTime;
            var error = SelectReviewItemDto.RefinedAodDelayTime - refinedAodDelayTime;
            var result = Math.Abs(error) < Cache.Threshold;

            Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                NowOffset = refinedAodDelayTime,
                OldOffset = SelectReviewItemDto.RefinedAodDelayTime,
                Error = error,
                MaxAveragePmtList = new HtmlPlot2DLinesChart([
                    ("Average Pmt", LaserAodDelayItemDtoList
                        .Select(t => (t.RefinedAodDelayTime, AveragePmt: t.AveragePmtData))
                        .OrderBy(t => t.RefinedAodDelayTime)
                        .Select(t => new Point(t.RefinedAodDelayTime, t.AveragePmt))
                        .ToArray())
                ], "Average Pmt")
            }), HtmlLogUniqueId.LoggingHtml());

            SelectReviewItemDto.IsVerified = result;
            if (Save(SelectReviewItemDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                SelectReviewItemDto.IsVerified = false;
                return false;
            }

            DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, New Offset: ({refinedAodDelayTime:f3}) Old Offset: ({SelectReviewItemDto.RefinedAodDelayTime:f3}) Error: ({error:f3})", DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }).ConfigureAwait(false);
    }

    private async Task<bool> GetAodDelayAsync(LaserAODDelayDto laserAODDelayDto, CancellationToken cancellationToken)
    {
        LaserViewModel.SetAodDelayValue(Cache.OpticsMagTypeEnum, laserAODDelayDto.RefinedPrescanAodDelayTime, laserAODDelayDto.RefinedChirpAodDelayTime);

        await Task.Delay(TimeSpan.FromSeconds(Cache.WaitTime), cancellationToken).ConfigureAwait(false);

        var pmtDataList = LaserViewModel.GetCIBOfPMTDataList(10, CalibrationConstantsHelper.MainPmtId, CalibrationConstantsHelper.MainChannelId);
        var result = Enumerable.Range(0, pmtDataList.First().Count)
            .Select(t => pmtDataList.Select(tt => tt[t]).Average())
            .ToList();

        laserAODDelayDto.PmtDataList = result;
        SynchronizationContextProvider.Send(() =>
        {
            LaserAodDelayItemDtoList.Add(laserAODDelayDto);
            AodDelayList = [.. AodDelayList, new Point(laserAODDelayDto.RefinedAodDelayTime, laserAODDelayDto.AveragePmtData)];
        });

        Logger.LogHtmlInformation($"time:{laserAODDelayDto.Index}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
        {
            laserAODDelayDto.RefinedAodDelayTime,
            laserAODDelayDto.RefinedPrescanAodDelayTime,
            laserAODDelayDto.RefinedChirpAodDelayTime,
            laserAODDelayDto.AveragePmtData,
            PmtValueList = new HtmlPlot2DLinesChart([
                ("Pmt 8 channel 3 Value", result.Select((t, i) => new Point(i, t)).ToArray()
                )
            ], "Pmt 8 channel 3 Value")
        }), HtmlLogUniqueId.LoggingHtml());

        return true;
    }

    private bool Save(LaserAODDelayDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibrations =
        [
            .. Calibrations
                .Where(t => t.OpticsMagTypeEnum != dto.OpticsMagTypeEnum),
            dto.Clone()
        ];

        CacheProvider.SetArray(Calibrations, cancellationToken);
        CacheProvider.Set(Cache, cancellationToken);
    }) && EnableDependedCalibrationItems(cancellationToken);

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependLaserAodDelayCalibrations(false, cancellationToken, out var errorMsg) == false)
        {
            Logger.LogError("Toggle {@Name} Enable Status Failed!", errorMsg);
            return false;
        }

        return true;
    }

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(() => { LaserAodDelayItemDtoList.Clear(); });
        AodDelayList = [];
        SelectedCalibrateItemDto = null;
    }

    #endregion 校准
}