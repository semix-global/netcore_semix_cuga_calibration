using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.AOD.Alignment;
using Core.Models.Models.AOD.Delay;
using Core.Models.Models.CIB.XPixelSize;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.DOEAngle;
using Core.Models.Models.Laser.LineCentricity;


using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Setting;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using System.Collections.ObjectModel;
using Net.Utilities.Algorithms.Modules.CurveFitting;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserDOEAngleCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserDOEAngleCalibrationViewModel(CalibrationSetting calibrationSetting) : CalibrationViewModelBase
{
    #region 属性

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "CIB Config" },
        new() { StepName = "PMT Enable Config" },
        new() { StepName = "Alignment" },
        new() { StepName = "Find a Position" },
        new() { StepName = "DOE Angle Calibration" }
    ];

    #region 界面相关

    [ObservableProperty]
    private Point[] _afOffsetPoints = [];

    [ObservableProperty]
    private IReadOnlyList<OpticsIlluminationModeAndProductivityInformationStatus> _calibrationStatuses = [];

    #endregion 界面相关

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

    #endregion Calibration

    #region Review

    [ObservableProperty]
    private LaserDOEAngleDto? _reviewDto;

    #endregion Review

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private LaserDOEAngleCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private LaserDOEAngleDto _calibration = new();

    [ObservableProperty]
    private LaserDOEAngleDto _laserDOEAngleDto = new();

    [ObservableProperty]
    private MicroscopeCalChipDto _microscopeCalChip = new();

    [ObservableProperty]
    private MicroscopeCalChipCache _microscopeCalChipCache = new();

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    [ObservableProperty]
    private AlignmentCacheDarkField _alignmentCacheDarkField = new();

    [ObservableProperty]
    private AlignmentCacheDarkField[] _alignmentCacheDarkFields = [];

    [ObservableProperty]
    private AlignmentWindowBrightFieldViewModel _alignmentWindowBrightFieldViewModel = HostApplication.GetRequiredService<AlignmentWindowBrightFieldViewModel>();

    [ObservableProperty]
    private AlignmentWindowDarkFieldViewModel _alignmentWindowDarkFieldViewModel = HostApplication.GetRequiredService<AlignmentWindowDarkFieldViewModel>();

    #endregion 缓存

    #endregion 校准相关

    #endregion 属性

    #region 校准控制业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDto>();

        AlignmentCacheDarkFields = RecipeCacheProvider.GetOrDefaultArray<AlignmentCacheDarkField>();
        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();
        MicroscopeCalChipCache = RecipeCacheProvider.GetOrDefault<MicroscopeCalChipCache>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<LaserDOEAngleCache>();
        Calibration = CacheProvider.GetOrDefault<LaserDOEAngleDto>();

        CalibrationStatuses =
        [
            ..EnumHelper.Enums<OpticsIlluminationModeEnum>()
                .Select(t => new OpticsIlluminationModeAndProductivityInformationStatus
                {
                    SelectedItem = t,
                    ProductivityInformationStatusList = [.. ApplicationCookie.NIOpticsMagTypeProductivityInformations.Select(tt => new ProductivityInformationStatus { SelectedItem = tt, IsCalibrated = false })]
                })
        ];

        if (Cache.MicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.MicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();

        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
        Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
        AfViewModel.ToggleCalChipSiteModelEnum(Cache.CalChipSiteModelEnum);
        StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindPosition), Cache.CalChipSiteModelEnum);

        if (Cache.PmtConfigList.Count == 0) Cache.PmtConfigList = [.. CalibrationSetting.SettingPmtConfigParam.PmtConfigList.Select(t => t.Clone())];
        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
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

        // todo:防呆
        if (Calibration.IsOk == false)
            LaserViewModel.SetDOEAngle(Cache.OriginDOEAngle);

        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 2:
                DialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                Cache.Item.IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;
                return true;

            case 3:
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.CalChipSiteModelEnum switch
                    {
                        CalChipSiteModelEnum.ChuckModel => StageViewModel.BrightFieldToMachinePosition(Cache.Item.FindPosition),
                        CalChipSiteModelEnum.DswModel => MicroscopeCalChip.DSWBrightFieldMachineAffinePosition,
                        _ => ThrowHelper.ThrowNotSupportedException<Point>("Current CalChip Mode Is Not Supported!")
                    }), Cache.CalChipSiteModelEnum);
                return true;

            case 5:
                if (ResultLaserDOEAngleDto is null)
                {
                    DialogWindowProvider.ShowDialog("Please calibration first!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                var isCalibrated = CalibrationStepIndex == 5;

                ResultLaserDOEAngleDto.IsCalibrated = isCalibrated;
                if (Save(ResultLaserDOEAngleDto, cancellationToken) == false)
                {
                    ResultLaserDOEAngleDto.IsCalibrated = false;
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name}Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    DialogWindowProvider.ShowDialog("Save Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                CalibrationStatuses.Single(t => t.SelectedItem == Cache.OpticsIlluminationModeEnum)
                    .ProductivityInformationStatusList
                    .Single(t => t.SelectedItem == Cache.ProductivityInformation).IsCalibrated = true;

                IsCalibrated = CalibrationStatuses.All(s => s.IsCalibrated);
                //IsCalibrated = isCalibrated;
                return true;

            default:
                return true;
        }
    }

    #endregion 校准控制业务

    #region 校准

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
            foreach (var t in Cache.PmtConfigList) t.Enabled = isEnabled;
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
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            AlignmentResultDto alignmentResult;
            if (Cache.Item.IsDarkFieldAlignment)
            {
                AlignmentCacheDarkField = AlignmentCacheDarkFields.SingleOrDefault(t => t.OpticsIlluminationModeEnum == Cache.ProductivityInformation.OpticsIlluminationModeEnum
                                                                                        && t.ProductivityInformation == Cache.ProductivityInformation, new AlignmentCacheDarkField());
                if (AlignmentCacheDarkField.IsOk == false)
                {
                    var alignmentWindowDarkFieldViewModel = AlignmentWindowDarkFieldViewModel;
                    Guard.IsTrue(WindowManagerService.ShowDialog(alignmentWindowDarkFieldViewModel) == true, nameof(alignmentWindowDarkFieldViewModel));
                    AlignmentCacheDarkFields = [.. AlignmentCacheDarkFields, alignmentWindowDarkFieldViewModel.Cache];
                }

                alignmentResult = StageViewModel.AlignmentDarkField(
                    AlignmentCacheDarkField.LowSite1,
                    AlignmentCacheDarkField.LowSite2,
                    AlignmentCacheDarkField.HighSite1,
                    AlignmentCacheDarkField.HighSite2,
                    Cache.ProductivityInformation,
                    AlignmentCacheDarkField.LowMag,
                    AlignmentCacheDarkField.AlgorithmWaferTypeEnum,
                    opticsIlluminationModeEnum: Cache.ProductivityInformation.OpticsIlluminationModeEnum);
            }
            else
            {
                if (Cache.CalChipSiteModelEnum is CalChipSiteModelEnum.ChuckModel)
                {
                    if (AlignmentCacheBrightField.IsOk == false)
                    {
                        var alignmentWindowBrightFieldViewModel = AlignmentWindowBrightFieldViewModel;
                        Guard.IsTrue(WindowManagerService.ShowDialog(alignmentWindowBrightFieldViewModel) == true, nameof(alignmentWindowBrightFieldViewModel));
                        AlignmentCacheBrightField = alignmentWindowBrightFieldViewModel.Cache;
                    }

                    alignmentResult = StageViewModel.Alignment(
                        AlignmentCacheBrightField.LowSite1,
                        AlignmentCacheBrightField.LowSite2,
                        AlignmentCacheBrightField.HighSite1,
                        AlignmentCacheBrightField.HighSite2,
                        AlignmentCacheBrightField.LowMag,
                        AlignmentCacheBrightField.HighMag,
                        AlignmentCacheBrightField.AlgorithmWaferTypeEnum,
                        Cache.CalChipSiteModelEnum);
                }
                else
                {
                    alignmentResult = StageViewModel.Alignment(
                        MicroscopeCalChipCache.LowSite1,
                        MicroscopeCalChipCache.LowSite2,
                        MicroscopeCalChipCache.HighSite1,
                        MicroscopeCalChipCache.HighSite2,
                        MicroscopeCalChipCache.LowMicroscopeLensInformation,
                        MicroscopeCalChipCache.HighMicroscopeLensInformation,
                        AlignmentCacheBrightField.AlgorithmWaferTypeEnum,
                        Cache.CalChipSiteModelEnum);
                }
            }

            Cache.Item.P5Angle = alignmentResult.Degrees;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.CalChipSiteModelEnum,
                Cache.Item.IsDarkFieldAlignment,
                Cache.Item.P5Angle
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Cache.Item.FindPosition = StageViewModel.GetBrightFieldStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                Cache.CalChipSiteModelEnum,
                FindMachinePosition = Cache.Item.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            try
            {
                Cache.Item.EcsPerAfOffset = AfViewModel.GetEcsPerOffsetMotorMm();
                Cache.UmPerEcs = AfViewModel.GetNmPerEcs() / 1000;
                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.OriginDOEAngle,
                    Cache.Item.ObliqueAngle,
                    Cache.PmtInterval,
                    Cache.Item.EcsPerAfOffset,
                    Cache.UmPerEcs,
                    Cache.Item.RetryCount
                }), HtmlLogUniqueId.LoggingHtml());

                SynchronizationContextProvider.Send(() => LaserDOEAngleDtoItems.Clear());

                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.Item.FindPosition, Cache.CalChipSiteModelEnum);
                LaserViewModel.SetDOEAngle(Cache.OriginDOEAngle);

                var result = false;
                for (var times = 0; times < Cache.Item.RetryCount; times++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Logger.LogHtmlInformation($"Multiple PMT RTFC:Times {times}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    if (MultipleLightRuntimeAfCalibration(cancellationToken) == false)
                        return false;

                    SynchronizationContextProvider.Send(() => LaserDOEAngleDtoItems.Add(LaserDOEAngleDto.Clone()));
                    if (LaserDOEAngleDto.AfPosError < Cache.Item.Threshold)
                    {
                        result = true;
                        break;
                    }

                    if (Math.Abs(LaserDOEAngleDto.DOEReviseAngle) < 0.01) break;

                    LaserViewModel.SetDOEAngle(LaserDOEAngleDto.DOEAngle - Math.Round(LaserDOEAngleDto.DOEReviseAngle, 2));
                    // LaserViewModel.SetDOEAngle(LaserDOEAngleDto.DOEAngle + Math.Round(LaserDOEAngleDto.DOEReviseAngle, 2));
                }

                // 迭代后出现振荡时取斜率最小的两次平均角度
                if (LaserDOEAngleDtoItems.Count == Cache.Item.RetryCount && result == false)
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
                if (Cache.Item.IsDarkFieldAlignment == false)
                {
                    if (Cache.CalChipSiteModelEnum is CalChipSiteModelEnum.ChuckModel)
                    {
                        StageViewModel.Alignment(
                            AlignmentCacheBrightField.LowSite1,
                            AlignmentCacheBrightField.LowSite2,
                            AlignmentCacheBrightField.HighSite1,
                            AlignmentCacheBrightField.HighSite2,
                            AlignmentCacheBrightField.LowMag,
                            AlignmentCacheBrightField.HighMag,
                            AlignmentCacheBrightField.AlgorithmWaferTypeEnum,
                            Cache.CalChipSiteModelEnum);
                    }
                    else
                    {
                        StageViewModel.Alignment(
                            MicroscopeCalChipCache.LowSite1,
                            MicroscopeCalChipCache.LowSite2,
                            MicroscopeCalChipCache.HighSite1,
                            MicroscopeCalChipCache.HighSite2,
                            MicroscopeCalChipCache.LowMicroscopeLensInformation,
                            MicroscopeCalChipCache.HighMicroscopeLensInformation,
                            AlignmentCacheBrightField.AlgorithmWaferTypeEnum,
                            Cache.CalChipSiteModelEnum);
                    }
                }
                else
                {
                    StageViewModel.AlignmentDarkField(
                        AlignmentCacheDarkField.LowSite1,
                        AlignmentCacheDarkField.LowSite2,
                        AlignmentCacheDarkField.HighSite1,
                        AlignmentCacheDarkField.HighSite2,
                        Cache.ProductivityInformation,
                        AlignmentCacheDarkField.LowMag,
                        AlignmentCacheDarkField.AlgorithmWaferTypeEnum,
                        opticsIlluminationModeEnum: Cache.OpticsIlluminationModeEnum);
                }

                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.FindPosition), Cache.CalChipSiteModelEnum);

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.CalChipSiteModelEnum,
                    Cache.Item.IsDarkFieldAlignment,
                    Cache.Item.P5Angle,
                    Cache.Item.ObliqueAngle,
                    Cache.PmtInterval,
                    Cache.Item.EcsPerAfOffset,
                    Cache.UmPerEcs,
                    Cache.Item.Threshold,
                    Cache.OriginDOEAngle,
                    ReviewDto.DOEAngle
                }), HtmlLogUniqueId.LoggingHtml());

                LaserViewModel.SetDOEAngle(ReviewDto.DOEAngle);
                if (MultipleLightRuntimeAfCalibration(cancellationToken) == false)
                    return false;

                var result = ReviewDto.IsVerified = LaserDOEAngleDto.AfPosError < Cache.Item.Threshold;
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

            CIBViewModel.SetCIBConfiguration(ApplicationCookie.CIBInformations, Cache.CIBConfiguration);

            // 前8倒叙计算
            for (var i = 8; i >= 1; i--)
            {
                if (pmtConfig.Single(t => t.Id == i).Enabled == false || Cache.PmtConfigList.Single(t => t.Id == i).Enabled == false)
                    continue;
                var pmt = new DarkFieldRTFCDto
                {
                    PmtId = i,
                    Position = Cache.Item.FindPosition - (Vector)new Point(0, Cache.PmtInterval * (8 - i))
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
                    Position = Cache.Item.FindPosition + (Vector)new Point(0, Cache.PmtInterval * (i - 8))
                };
                SynchronizationContextProvider.Send(() => DarkFieldRTFCDtoList.Add(pmt));
            }

            var darkFieldRTFCDtoList = DarkFieldRTFCDtoList.OrderBy(t => t.PmtId).ToList();
            foreach (var darkFieldRtfcDto in darkFieldRTFCDtoList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SelectDarkFieldRTFCItemDto = darkFieldRtfcDto;
                var (afEcs, afOffset) = LaserViewModel.RuntimeAfCalibration(
                    Cache.CIBConfiguration,
                    darkFieldRtfcDto.Position,
                    calibrationSetting.SettingCommonParam.MainLaserLightInformation,
                    out _,
                    calChipSiteModelEnum: Cache.CalChipSiteModelEnum,
                    pmtId: darkFieldRtfcDto.PmtId,
                    saveImageFileDirectory: ImageFileDirectory,
                    logGuid: HtmlLogUniqueId,
                    logName: $"PMT {darkFieldRtfcDto.PmtId}");
                darkFieldRtfcDto.AfEcs = afEcs;
                darkFieldRtfcDto.AfOffset = afOffset;
                SynchronizationContextProvider.Send(() => AfOffsetPoints = [.. AfOffsetPoints, new Point((darkFieldRtfcDto.PmtId - 1) * Cache.PmtInterval, darkFieldRtfcDto.AfOffset)]);
            }

            var xVector = Vector<double>.Build.DenseOfEnumerable([.. darkFieldRTFCDtoList.Select(t => (t.PmtId - darkFieldRTFCDtoList[0].PmtId) * Cache.PmtInterval)]);
            // var xVector = Vector<double>.Build.DenseOfEnumerable([.. darkFieldRTFCDtoList.Select(t => (t.PmtId - 1) * Cache.PmtInterval)]);
            var yVector = Vector<double>.Build.DenseOfEnumerable([.. darkFieldRTFCDtoList.Select(t => (t.AfEcs - darkFieldRTFCDtoList[0].AfEcs) * Cache.UmPerEcs)]);
            // var yVector = Vector<double>.Build.DenseOfEnumerable([.. darkFieldRTFCDtoList.Select(t => t.AfOffset * Cache.Item.EcsPerAfOffset * Cache.UmPerEcs)]);
            var (slope, intercept, _, _) = PolynomialCurve.Fit1(xVector, yVector);

            var doeReviseAngle = Math.Atan(slope / Math.Sin(Cache.Item.ObliqueAngle * Math.PI / 180)) * 180 / Math.PI;

            var currentDOEAngle = LaserViewModel.ReadDOECurrentAngle();
            LaserDOEAngleDto.DOEAngle = currentDOEAngle;
            LaserDOEAngleDto.DOEReviseAngle = doeReviseAngle;
            LaserDOEAngleDto.MultiRtfcFitSlope = slope;
            LaserDOEAngleDto.AfPosError = Math.Abs((darkFieldRTFCDtoList.Last().AfEcs - darkFieldRTFCDtoList.First().AfEcs) * Cache.UmPerEcs);
            // LaserDOEAngleDto.AfPosError = Math.Abs((darkFieldRTFCDtoList.Last().AfOffset - darkFieldRTFCDtoList.First().AfOffset) * Cache.Item.EcsPerAfOffset * Cache.UmPerEcs);

            Logger.LogHtmlInformation("Multiple RTFC Result", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                Cache.Item.Threshold,
                Cache.OriginDOEAngle,
                Cache.Item.EcsPerAfOffset,
                Cache.UmPerEcs,
                L = Math.Abs(Cache.PmtInterval * (DarkFieldRTFCDtoList.Count - 1)),
                LaserDOEAngleDto.MultiRtfcFitSlope,
                intercept,
                DOECurrentAngle = LaserDOEAngleDto.DOEAngle,
                LaserDOEAngleDto.DOEReviseAngle,
                FocusOffset = LaserDOEAngleDto.AfPosError,
                RtfcResult = new HtmlPlot2DLinesChart([
                    // ("Pmt-AfPos", darkFieldRTFCDtoList.Select(t => new Point((t.PmtId - 1) * Cache.PmtInterval, t.AfOffset * Cache.Item.EcsPerAfOffset * Cache.UmPerEcs)).ToArray()),
                    // ("Pmt-AfPos-Plot1Fit", darkFieldRTFCDtoList.Select(t => new Point((t.PmtId - 1) * Cache.PmtInterval, slope * (t.PmtId - 1) * Cache.PmtInterval + intercept)).ToArray())
                    ("Pmt-AfPos", darkFieldRTFCDtoList.Select(t => new Point((t.PmtId - darkFieldRTFCDtoList[0].Id) * Cache.PmtInterval, (t.AfEcs - darkFieldRTFCDtoList[0].AfEcs) * Cache.UmPerEcs)).ToArray()),
                    ("Pmt-AfPos-Plot1Fit", darkFieldRTFCDtoList.Select(t => new Point((t.PmtId - darkFieldRTFCDtoList[0].Id) * Cache.PmtInterval, slope * (t.PmtId - darkFieldRTFCDtoList[0].Id) * Cache.PmtInterval + intercept)).ToArray())
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
        LaserDOEAngleDto = new LaserDOEAngleDto();
        SelectLaserDOEAngleDto = null;
    }

    private bool Save(LaserDOEAngleDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        CacheProvider.Set(dto, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}