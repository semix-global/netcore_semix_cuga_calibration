using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Laser.AodDelay;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.DOEAngle;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.LineCentricity;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Laser.PrescanChirpAodAlignment;
using Core.Models.Models.Laser.XPixelSize;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Models.Models.Setting;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using MoreLinq.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserDOEAngleCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserDOEAngleCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "CIB Config" },
        new() { StepName = "PMT Enable Config" },
        new() { StepName = "Find a Position" },
        new() { StepName = "DOE Angle Calibration" }
    ];

    #region 界面相关

    [ObservableProperty]
    private Point[] _afOffsetPoints = [];

    #endregion

    #region 校准相关

    #region Calibration

    [ObservableProperty]
    private LaserDOEAngleDto? _resultLaserDOEAngleDto;

    [ObservableProperty]
    private ObservableCollection<DarkFieldRTFCDto> _darkFieldRTFCDtoList = [];

    [ObservableProperty]
    private DarkFieldRTFCDto? _selectDarkFieldRTFCItemDto;

    [ObservableProperty]
    private ObservableCollection<LaserDOEAngleDto> _laserDOEAngleDtoItems = [];

    [ObservableProperty]
    private LaserDOEAngleDto? _selectLaserDOEAngleDto;

    #endregion

    #region Review

    [ObservableProperty]
    private LaserDOEAngleDto? _reviewDto;

    #endregion

    #region 缓存

    [ObservableProperty]
    private LaserDOEAngleCache _cache = new();

    [ObservableProperty]
    private LaserDOEAngleDto _calibration = new();

    [ObservableProperty]
    private LaserDOEAngleDto _laserDOEAngleDto = new();

    #endregion

    #endregion

    #endregion

    #region 校准控制业务

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

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<MicroscopeCalChipDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<MicroscopePixelSizeItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<MicroscopeCentricityItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserPixelSizeItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserLineCentricityItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserXPixelSizeItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<LaserDOEAngleCache>();
        Calibration = CacheProvider.GetOrDefault<LaserDOEAngleDto>();

        if (Cache.MicroscopeLensInformation.LensCode == -1) Cache.MicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformationList[0];

        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
        Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
        AfViewModel.ToggleCalChipSiteModelEnum(Cache.CalChipSiteModelEnum);
        StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition), Cache.CalChipSiteModelEnum);

        if (Cache.PmtConfigList.Count == 0) Cache.PmtConfigList = [.. CalibrationSetting.SettingPmtConfigParam.PmtConfigList.Select(t => t.Clone())];
        return isHasCache || CacheProvider.Set(Cache, cancellationToken);
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Cache.OriginDOEAngle = LaserViewModel.ReadDOECurrentAngle();

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewDto = Calibration.Clone();
        return ReviewDto.IsCalibrated;
    }

    protected override async Task<bool> CancelingAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (Calibration.IsOk == false)
            LaserViewModel.SetDOEAngle(Cache.OriginDOEAngle);
        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 3:
                if (ResultLaserDOEAngleDto is null)
                {
                    DialogWindowProvider.ShowDialog("Please calibration first!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                var isCalibrated = CalibrationStepIndex == 3;

                ResultLaserDOEAngleDto.IsCalibrated = isCalibrated;
                if (Save(ResultLaserDOEAngleDto, cancellationToken) == false)
                {
                    ResultLaserDOEAngleDto.IsCalibrated = false;
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name}Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    DialogWindowProvider.ShowDialog("Save Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                IsCalibrated = isCalibrated;
                return true;

            default:
                return true;
        }
    }

    #endregion

    #region 校准

    [RelayCommand]
    private async Task MagnificationSelectedAsync(object obj)
    {
        try
        {
            if (obj is not MicroscopeLensInformation)
            {
                Logger.LogError("{@Name}: Select magnification illegal!", Name);
                return;
            }

            await Task.Run(() => MicroscopeViewModel.SwitchMicroscopeLensInformation(ApplicationCookie.MicroscopeLensInformationList.Single(t => t == (MicroscopeLensInformation)obj))
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
        }
    }

    [RelayCommand]
    private Task ConfigStepActionAsync()
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                IsAutoGain = Cache.CIBConfiguration.IsAutoGainControl,
                DcGainVoltage = Cache.CIBConfiguration.Gain,
                IsL0k = Cache.CIBConfiguration.IsL0K,
                CIBProfileTypeEnum = Cache.CIBConfiguration.CIBProfileMode
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand]
    private Task PmtConfigStepActionAsync()
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                PMTEnableList = new HtmlTable([.. Cache.PmtConfigList.Select(t => (t.Id, t.Enabled))])
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand]
    private void ChangeAllSelection(object isSelectAll)
    {
        try
        {
            var isEnabled = Convert.ToBoolean(isSelectAll);
            Cache.PmtConfigList.ForEach(t => t.Enabled = isEnabled);
        }
        catch
        {
            throw new ArgumentException("Command Parameter Convert to Boolean Invalid!");
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Cache.FindPosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                Cache.CalChipSiteModelEnum,
                FindMachinePosition = Cache.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            try
            {
                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.OriginDOEAngle,
                    Cache.ObliqueAngle,
                    Cache.PmtInterval,
                    Cache.EcsPerAfOffset,
                    Cache.UmPerEcs,
                    Cache.RetryCount
                }), HtmlLogUniqueId.LoggingHtml());

                SynchronizationContextProvider.Send(() => LaserDOEAngleDtoItems.Clear());

                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition), Cache.CalChipSiteModelEnum);
                LaserViewModel.SetDOEAngle(Cache.OriginDOEAngle);

                var result = false;
                for (var times = 0; times < Cache.RetryCount; times++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Logger.LogHtmlInformation($"Multiple PMT RTFC:Times {times}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    if (MultipleLightRuntimeAfCalibration(cancellationToken) == false)
                        return false;

                    SynchronizationContextProvider.Send(() => LaserDOEAngleDtoItems.Add(LaserDOEAngleDto.Clone()));
                    if (LaserDOEAngleDto.AfPosError < Cache.Threshold)
                    {
                        result = true;
                        break;
                    }

                    if (Math.Abs(LaserDOEAngleDto.DOEReviseAngle) < 0.01) break;

                    LaserViewModel.SetDOEAngle(LaserDOEAngleDto.DOEAngle + Math.Round(LaserDOEAngleDto.DOEReviseAngle, 2));
                }

                // 迭代后出现振荡时取斜率最小的两次平均角度
                if (LaserDOEAngleDtoItems.Count == Cache.RetryCount && result == false)
                    LaserDOEAngleDto = LaserDOEAngleDtoItems.OrderBy(t => t.AfPosError).First();

                ResultLaserDOEAngleDto = LaserDOEAngleDto.Clone();
                ResultLaserDOEAngleDto.IsCalibrated = result;

                Logger.LogHtmlInformation("Calibration OK", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.OriginDOEAngle,
                    ResultLaserDOEAngleDto.DOEAngle,
                    ResultLaserDOEAngleDto.AfPosError,
                    ResultLaserDOEAngleDto.MultiRtfcFitSlope,
                    RtfcResult = new HtmlPlot2DLinesChart([
                        ("DOEAngle-AFPosError", LaserDOEAngleDtoItems.Select(t => new Point(t.DOEAngle, t.AfPosError)).ToArray())
                    ], "RtfcResult")
                }), HtmlLogUniqueId.LoggingHtml());
                return true;
            }
            catch (Exception e)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name}Error: Calibration Failed! {e.Message}"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
            finally
            {
                LaserViewModel.SetDOEAngle(Cache.OriginDOEAngle);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        if (ReviewDto is null)
        {
            DialogWindowProvider.ShowDialog("Please calibration first!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(() =>
        {
            try
            {
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.FindPosition), Cache.CalChipSiteModelEnum);

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.ObliqueAngle,
                    Cache.PmtInterval,
                    Cache.EcsPerAfOffset,
                    Cache.UmPerEcs,
                    Cache.Threshold,
                    Cache.OriginDOEAngle,
                    ReviewDto.DOEAngle
                }), HtmlLogUniqueId.LoggingHtml());

                LaserViewModel.SetDOEAngle(ReviewDto.DOEAngle);
                if (MultipleLightRuntimeAfCalibration(cancellationToken) == false)
                    return false;

                var result = ReviewDto.IsVerified = LaserDOEAngleDto.AfPosError < Cache.Threshold;
                if (Save(ReviewDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed."), HtmlLogUniqueId.LoggingHtml());
                    ReviewDto.IsVerified = false;
                    return false;
                }

                DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")},Multiple Rtfc Af Pos Error {LaserDOEAngleDto.AfPosError}", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);

                if (result == false)
                    LaserViewModel.SetDOEAngle(Cache.OriginDOEAngle);

                Logger.LogHtmlInformation($"Verify{(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    LaserDOEAngleDto.DOEReviseAngle,
                    LaserDOEAngleDto.AfPosError,
                    LaserDOEAngleDto.MultiRtfcFitSlope
                }), HtmlLogUniqueId.LoggingHtml());

                return result;
            }
            catch (Exception e)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name}Error: Verify Failed! {e.Message}"), HtmlLogUniqueId.LoggingHtml());
                LaserViewModel.SetDOEAngle(Cache.OriginDOEAngle);
                return false;
            }
        }).ConfigureAwait(false);
    }

    private bool MultipleLightRuntimeAfCalibration(CancellationToken cancellationToken)
    {
        try
        {
            ClearCalibrationTemp();

            var pmtConfig = CalibrationSetting.SettingPmtConfigParam.PmtConfigList;

            var (_, yDirection) = StageViewModel.GetMachineDirection();

            LaserViewModel.ToggleCIBControlModeAndProfileType(Cache.CIBConfiguration, -1, -1);

            // 前8倒叙计算
            for (var i = 8; i >= 1; i--)
            {
                if (pmtConfig.Single(t => t.Id == i).Enabled == false || Cache.PmtConfigList.Single(t => t.Id == i).Enabled == false)
                    continue;
                var pmt = new DarkFieldRTFCDto
                {
                    PmtId = i,
                    Position = Cache.FindPosition - (Vector)new Point(0, yDirection * Cache.PmtInterval * (8 - i))
                };
                SynchronizationContextProvider.Send(() => DarkFieldRTFCDtoList.Add(pmt));
            }

            // 后7正序计算
            for (var i = 9; i <= 15; i++)
            {
                if (pmtConfig.Single(t => t.Id == i).Enabled == false || Cache.PmtConfigList.Single(t => t.Id == i).Enabled == false)
                    continue;
                var pmt = new DarkFieldRTFCDto
                {
                    PmtId = i,
                    Position = Cache.FindPosition + (Vector)new Point(0, yDirection * Cache.PmtInterval * (i - 8))
                };
                SynchronizationContextProvider.Send(() => DarkFieldRTFCDtoList.Add(pmt));
            }

            var darkFieldRTFCDtoList = DarkFieldRTFCDtoList.OrderBy(t => t.PmtId);
            foreach (var darkFieldRtfcDto in darkFieldRTFCDtoList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SelectDarkFieldRTFCItemDto = darkFieldRtfcDto;
                var (afEcs, afOffset) = LaserViewModel.RuntimeAfCalibration(calChipSiteModelEnum: Cache.CalChipSiteModelEnum, pmtId: darkFieldRtfcDto.PmtId);
                darkFieldRtfcDto.AfEcs = afEcs;
                darkFieldRtfcDto.AfOffset = afOffset;
                SynchronizationContextProvider.Send(() => AfOffsetPoints = [.. AfOffsetPoints, new Point((darkFieldRtfcDto.PmtId - 1) * Cache.PmtInterval, darkFieldRtfcDto.AfOffset)]);
                Logger.LogHtmlInformation($"PMT {darkFieldRtfcDto.PmtId} RTFC Result", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    darkFieldRtfcDto.AfEcs,
                    darkFieldRtfcDto.AfOffset
                }), HtmlLogUniqueId.LoggingHtml());
            }

            var xVector = Vector<double>.Build.DenseOfEnumerable([.. darkFieldRTFCDtoList.Select(t => (t.PmtId - 1) * Cache.PmtInterval)]);
            var yVector = Vector<double>.Build.DenseOfEnumerable([.. darkFieldRTFCDtoList.Select(t => t.AfOffset * Cache.EcsPerAfOffset * Cache.UmPerEcs)]);
            var (slope, intercept, _, _) = PolyFit.Poly1Fit(xVector, yVector);

            var doeReviseAngle = Math.Atan(slope / Math.Sin(Cache.ObliqueAngle * Math.PI / 180)) * 180 / Math.PI;

            var currentDOEAngle = LaserViewModel.ReadDOECurrentAngle();
            LaserDOEAngleDto.DOEAngle = currentDOEAngle;
            LaserDOEAngleDto.DOEReviseAngle = doeReviseAngle;
            LaserDOEAngleDto.MultiRtfcFitSlope = slope;
            LaserDOEAngleDto.AfPosError = Math.Abs((darkFieldRTFCDtoList.Last().AfOffset - darkFieldRTFCDtoList.First().AfOffset) * Cache.EcsPerAfOffset * Cache.UmPerEcs);

            Logger.LogHtmlInformation("Multiple RTFC Result", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                Cache.Threshold,
                Cache.OriginDOEAngle,
                Cache.EcsPerAfOffset,
                Cache.UmPerEcs,
                L = Math.Abs(Cache.PmtInterval * (DarkFieldRTFCDtoList.Count - 1)),
                LaserDOEAngleDto.MultiRtfcFitSlope,
                intercept,
                DOECurrentAngle = LaserDOEAngleDto.DOEAngle,
                LaserDOEAngleDto.DOEReviseAngle,
                LaserDOEAngleDto.AfPosError,
                RtfcResult = new HtmlPlot2DLinesChart([
                    ("Pmt-AfPos", darkFieldRTFCDtoList.Select(t => new Point((t.PmtId - 1) * Cache.PmtInterval, t.AfOffset * Cache.EcsPerAfOffset * Cache.UmPerEcs)).ToArray()),
                    ("Pmt-AfPos-Plot1Fit", darkFieldRTFCDtoList.Select(t => new Point((t.PmtId - 1) * Cache.PmtInterval, slope * (t.PmtId - 1) * Cache.PmtInterval + intercept)).ToArray())
                ], "RtfcResult")
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        }
        catch (Exception e)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment($"Multiple Light Runtime Af Calibration Error: {e.Message}"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }
    }

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(() =>
        {
            DarkFieldRTFCDtoList.Clear();
            AfOffsetPoints = [];
        });
        ResultLaserDOEAngleDto = null;
        LaserDOEAngleDto = new();
        SelectLaserDOEAngleDto = null;
    }

    private bool Save(LaserDOEAngleDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        return CacheProvider.Set(dto, cancellationToken) && CacheProvider.Set(Cache, cancellationToken);
    });

    #endregion
}