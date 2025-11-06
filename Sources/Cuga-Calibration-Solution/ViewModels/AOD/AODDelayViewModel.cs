using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models;
using Core.Models.Models.AOD.AODDelay;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.AOD;

[IOCAppService(ServiceType = typeof(AODDelayViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AODDelayViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Productivity" },
        new() { StepName = "Find Position" },
        new() { StepName = "AOD Delay" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibratingPoints))]
    private IReadOnlyList<AODDelayDto> _calibratings = [];

    public IReadOnlyList<Point> CalibratingPoints => [.. Calibratings.Select(t => new Point(t.RefinedAODDelay, t.AveragePmtData))];

    [ObservableProperty]
    private AODDelayDto? _selectedCalibratingItem;

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationCalibrationStatus> _calibrationStatuses = [];

    #endregion Calibrate

    [ObservableProperty]
    private ObservableCollection<AODDelayDto> _reviews = [];

    [ObservableProperty]
    private AODDelayDto? _selectedReviewItem;

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private AODDelayCache _cache = new();

    [ObservableProperty]
    private AODDelayDto[] _calibrations = [];

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

        if (CalibrationStatuses.Count == 0)
            CalibrationStatuses =
            [
                .. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(t => new ProductivityInformationCalibrationStatus { ProductivityInformation = t, IsCalibrated = false })
            ];

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<AODDelayCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<AODDelayDto>();

        Calibrations =
        [
            ..Calibrations.Where(t => ApplicationCookie.ProductivityInformations.Contains(t.ProductivityInformation))
                .Select(t =>
                {
                    t.IsCalibrated = CalibrationStatuses.Single(tt => tt.ProductivityInformation == t.ProductivityInformation).IsCalibrated;
                    return t;
                })
        ];

        if (isHasCache == false) CacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Reviews =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .OrderBy(t => t.ProductivityInformation)
        ];

        return Reviews.Any(t => t.IsCalibrated);
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.HazeBrightFieldMachinePosition));

                return true;

            case 1:
                return true;

            case 2:
                CalibrationStatuses.Single(t => t.ProductivityInformation == Cache.ProductivityInformation).IsCalibrated = true;
                DialogWindowProvider.ShowDialog($"AOD Delay Offset {Cache.ProductivityInformation} Ok!");

                IsCalibrated = CalibrationStatuses.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            StageViewModel.SetAbsoluteStageTheta(0);
            Cache.Item.FindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.FindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            try
            {
                Calibratings = [];

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.ProductivityInformation,
                    CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                    Cache.Item.WaitTime,
                    Cache.Item.PMTDataCount,
                    LaserLightInformation = new HtmlQuote(Cache.Item.LaserLightInformation.ToHtmlAnonymous()),
                    Cache.Item.PMTId,
                    Cache.Item.ChannelId,
                    Cache.Item.FindBFMachinePosition,
                    Cache.Item.RoughStartAODDelay,
                    Cache.Item.RoughStepAODDelay,
                    Cache.Item.RoughStopAODDelay,
                    Cache.Item.RefinedRangeAODDelay,
                    Cache.Item.RefinedStepAODDelay
                }), HtmlLogUniqueId.LoggingHtml());

                StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition));
                LaserViewModel.ToggleCIBControlModeAndProfileType(Cache.Item.CIBConfiguration, Constants.NegInt32Value, Constants.NegInt32Value);
                LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
                LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Coefficient);
                LaserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);
                AfViewModel.ToggleDarkFieldEnable(true);
                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

                Logger.LogHtmlInformation("AOD Delay", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var aodDelays = Generate.LinearRange(Cache.Item.RoughStartAODDelay, Cache.Item.RoughStepAODDelay, Cache.Item.RoughStopAODDelay);
                Guard.IsNotEmpty(aodDelays, nameof(aodDelays));

                foreach (var aodDelay in aodDelays)
                {
                    var aodDelayDto = new AODDelayDto
                    {
                        ProductivityInformation = Cache.ProductivityInformation,
                        RoughAODDelay = aodDelay,
                        RefinedAODDelay = aodDelay
                    };
                    if (await GetAODDelayAsync(aodDelayDto, cancellationToken).ConfigureAwait(false) == false) return false;

                    Calibratings = [.. Calibratings, aodDelayDto];
                    Calibratings = [.. Calibratings.OrderBy(t => t.RefinedAODDelay)];
                }

                var roughAODDelay = GuardUtils.IsNotNullAndReturn(Calibratings.MaxBy(t => t.AveragePmtData)).RoughAODDelay;

                aodDelays = Generate.LinearRange(
                    roughAODDelay - Cache.Item.RefinedRangeAODDelay,
                    Cache.Item.RefinedStepAODDelay,
                    roughAODDelay + Cache.Item.RefinedRangeAODDelay);
                Guard.IsNotEmpty(aodDelays, nameof(aodDelays));

                foreach (var aodDelay in aodDelays)
                {
                    var aodDelayDto = new AODDelayDto
                    {
                        ProductivityInformation = Cache.ProductivityInformation,
                        RoughAODDelay = roughAODDelay,
                        RefinedAODDelay = aodDelay
                    };

                    if (await GetAODDelayAsync(aodDelayDto, cancellationToken).ConfigureAwait(false) == false) return false;

                    Calibratings = [.. Calibratings, aodDelayDto];
                    Calibratings = [.. Calibratings.OrderBy(t => t.RefinedAODDelay)];
                }

                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Close);

                SelectedCalibratingItem = GuardUtils.IsNotNullAndReturn(Calibratings.MaxBy(t => t.AveragePmtData));
                SelectedCalibratingItem.IsCalibrated = true;
                if (Save(SelectedCalibratingItem, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    SelectedCalibratingItem.IsCalibrated = false;

                    return false;
                }

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    SelectedCalibratingItem.RefinedAODDelay,
                    SelectedCalibratingItem.RefinedPrescanAODDelay,
                    SelectedCalibratingItem.RefinedChirpAODDelay,
                    SelectedCalibratingItem.AveragePmtData,
                    MaxAveragePmtList = new HtmlPlot2DLinesChart([("Average Pmt", CalibratingPoints)], string.Empty)
                }), HtmlLogUniqueId.LoggingHtml());

                return true;
            }
            finally
            {
                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Close);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        if (SelectedReviewItem is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(async () =>
        {
            try
            {
                Cache.ProductivityInformation = SelectedReviewItem.ProductivityInformation;

                Calibratings = [];

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.ProductivityInformation,
                    CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                    Cache.Item.WaitTime,
                    Cache.Item.PMTDataCount,
                    LaserLightInformation = new HtmlQuote(Cache.Item.LaserLightInformation.ToHtmlAnonymous()),
                    Cache.Item.PMTId,
                    Cache.Item.ChannelId,
                    Cache.Item.FindBFMachinePosition,
                    Cache.Item.RefinedRangeAODDelay,
                    Cache.Item.RefinedStepAODDelay,
                    Cache.Threshold
                }), HtmlLogUniqueId.LoggingHtml());

                SelectedReviewItem.IsVerified = false;

                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindBFMachinePosition));
                LaserViewModel.ToggleCIBControlModeAndProfileType(Cache.Item.CIBConfiguration, Constants.NegInt32Value, Constants.NegInt32Value);
                LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
                LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Coefficient);
                LaserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);
                AfViewModel.ToggleDarkFieldEnable(true);
                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Through);

                Logger.LogHtmlInformation("AOD Delay", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var aodDelays = Generate.LinearRange(
                    SelectedReviewItem.RefinedAODDelay - Cache.Item.RefinedRangeAODDelay,
                    Cache.Item.RefinedStepAODDelay,
                    SelectedReviewItem.RefinedAODDelay + Cache.Item.RefinedRangeAODDelay);
                Guard.IsNotEmpty(aodDelays, nameof(aodDelays));

                foreach (var aodDelay in aodDelays)
                {
                    var aodDelayDto = new AODDelayDto
                    {
                        ProductivityInformation = Cache.ProductivityInformation,
                        RoughAODDelay = SelectedReviewItem.RefinedAODDelay,
                        RefinedAODDelay = aodDelay
                    };

                    if (await GetAODDelayAsync(aodDelayDto, cancellationToken).ConfigureAwait(false) == false) return false;

                    Calibratings = [.. Calibratings, aodDelayDto];
                    Calibratings = [.. Calibratings.OrderBy(t => t.RefinedAODDelay)];
                }

                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Close);

                SelectedCalibratingItem = GuardUtils.IsNotNullAndReturn(Calibratings.MaxBy(t => t.AveragePmtData));

                var error = Math.Abs(SelectedReviewItem.RefinedAODDelay - SelectedCalibratingItem.RefinedAODDelay);
                var result = error < Cache.Threshold;

                Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    OldRefinedAODDelay = SelectedReviewItem.RefinedAODDelay,
                    OldRefinedPrescanAODDelay = SelectedReviewItem.RefinedPrescanAODDelay,
                    OldRefinedChirpAODDelay = SelectedReviewItem.RefinedChirpAODDelay,
                    NewRefinedAODDelay = SelectedCalibratingItem.RefinedAODDelay,
                    NewRefinedPrescanAODDelay = SelectedCalibratingItem.RefinedPrescanAODDelay,
                    NewRefinedChirpAODDelay = SelectedCalibratingItem.RefinedChirpAODDelay,
                    Error = error,
                    MaxAveragePmtList = new HtmlPlot2DLinesChart([("Average Pmt", CalibratingPoints)], string.Empty)
                }), HtmlLogUniqueId.LoggingHtml());

                SelectedReviewItem.IsVerified = result;
                if (Save(SelectedReviewItem, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    SelectedReviewItem.IsVerified = false;

                    return false;
                }

                DialogWindowProvider.ShowDialog($"""
                                                 Verify {(result ? "OK" : "Failed")}
                                                 New Offset: ({SelectedCalibratingItem.RefinedAODDelay:0.###})
                                                 Old Offset: ({SelectedReviewItem.RefinedAODDelay:0.###})
                                                 Error: ({error:0.###})
                                                 """, DialogButtonsEnum.OK,
                    result ? DialogIconEnum.Information : DialogIconEnum.Warning);

                return result;
            }
            finally
            {
                LaserViewModel.ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum.Close);
            }
        }).ConfigureAwait(false);
    }

    private async Task<bool> GetAODDelayAsync(AODDelayDto aodDelayDto, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        LaserViewModel.SetAODDelayValue(Cache.ProductivityInformation, aodDelayDto.RefinedPrescanAODDelay, aodDelayDto.RefinedChirpAODDelay);

        await Task.Delay(TimeSpan.FromSeconds(Cache.Item.WaitTime), cancellationToken).ConfigureAwait(false);

        var pmtDataList = LaserViewModel.GetCIBOfPMTDataList(Cache.Item.PMTDataCount, Cache.Item.PMTId, Cache.Item.ChannelId);
        var result = Enumerable.Range(0, pmtDataList[0].Count)
            .Select(t => pmtDataList.Select(tt => tt[t]).Average())
            .ToList();

        aodDelayDto.PmtData = result;

        Logger.LogHtmlInformation($"Delay: {aodDelayDto.RefinedAODDelay}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
        {
            aodDelayDto.RefinedAODDelay,
            aodDelayDto.RefinedPrescanAODDelay,
            aodDelayDto.RefinedChirpAODDelay,
            aodDelayDto.AveragePmtData,
            PmtValueList = new HtmlPlot2DLinesChart(
                [.. pmtDataList.Index().Select<(int Index, IReadOnlyList<double> Item), (string Name, IReadOnlyList<Point> Points)>(t => (t.Index.ToString(), [.. t.Item.Index().Select(tt => new Point(tt.Index, tt.Item))]))],
                string.Empty)
        }), HtmlLogUniqueId.LoggingHtml());

        return true;
    }

    private bool Save(AODDelayDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibrations =
        [
            .. Calibrations.Where(t => t.ProductivityInformation != dto.ProductivityInformation),
            dto.Clone()
        ];

        CacheProvider.SetArray(Calibrations, cancellationToken);
        CacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}