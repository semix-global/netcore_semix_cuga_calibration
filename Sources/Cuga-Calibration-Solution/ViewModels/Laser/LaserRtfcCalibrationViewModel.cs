using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AodDelay;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.FocusShift;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.LineCentricity;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Laser.PrescanChirpAodAlignment;
using Core.Models.Models.Laser.Rtfc;
using Core.Models.Models.Laser.XPixelSize;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Utilities;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserRtfcCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserRtfcCalibrationViewModel(CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel) : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum);

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Config" },
        new() { StepName = "Select a Mag" },
        new() { StepName = "Find Low Site Position" },
        new() { StepName = "Find High Site Position" },
        new() { StepName = "Dark Field Template" },
        new() { StepName = "Calibration" }
    ];

    #region 界面相关

    #region Calibrate

    /// <summary>
    /// 诊断对象集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<RtfcDto> _rtfcDtoItems = [];

    /// <summary>
    /// 诊断对象迭代结果集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<RtfcDto> _rtfcDtoIterationItems = [];

    [ObservableProperty]
    private RtfcDto _resultRtfcDto = new();

    [ObservableProperty]
    private ObservableCollection<OpticsMagTypeEnumCalibrationStatus> _calibrationStatusList =
    [
        ..EnumHelper.Enums<OpticsMagTypeEnum>().Select(t => new OpticsMagTypeEnumCalibrationStatus { OpticsMagTypeEnum = t, IsCalibrated = false })
    ];

    [ObservableProperty]
    private Point[] _ecsPoints = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<RtfcDto> _reviewList = [];

    [ObservableProperty]
    private RtfcDto? _selectReviewDto;

    #endregion Review

    public double NscDiagnosisK { get; set; } = 1;

    #endregion 界面相关

    #region 缓存

    /// <summary>
    /// 焦点偏移缓存
    /// </summary>
    [ObservableProperty]
    private FocusShiftCache _focusShiftCache = new();

    /// <summary>
    /// 校准缓存
    /// </summary>
    [ObservableProperty]
    private RtfcCache _cache = new();

    [ObservableProperty]
    private RtfcDto[] _calibrations = [];

    /// <summary>
    /// 焦点偏移校准结果集合
    /// </summary>
    [ObservableProperty]
    private FocusShiftDto[] _resultFocusShiftDtoItems = [];

    /// <summary>
    /// 当前选择Mag的焦点偏移校准结果
    /// </summary>
    [ObservableProperty]
    private FocusShiftDto _selectFocusShiftDto = new();

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

    [ObservableProperty]
    private LaserPixelSizeItemDto[] _laserPixelSizeItems = [];

    [ObservableProperty]
    private LaserLineCentricityItemDto[] _laserLineCentricityItems = [];

    [ObservableProperty]
    private LaserFocusShiftCalibrationViewModel _focusShiftCalibrationViewModel = HostApplication.GetRequiredService<LaserFocusShiftCalibrationViewModel>();

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

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<MicroscopeCalChipDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<MicroscopePixelSizeItemDto>(out var microscopePixelSizeItems, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        MicroscopePixelSizeItems = microscopePixelSizeItems;

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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserPixelSizeItemDto>(out var laserPixelSizeItems, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        LaserPixelSizeItems = laserPixelSizeItems;

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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<FocusShiftDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        LaserViewModel.ToggleEnableAutoGainControl(true);
        LaserViewModel.ToggleEnableL0K(false);

        StageViewModel.SetAbsoluteStageTheta(0);
        MicroscopeViewModel.SwitchMicroscopeLensInformation(FocusShiftCache.LowMicroscopeLensInformation);
        AfViewModel.ToggleCalChipSiteModelEnum(FocusShiftCache.CalChipSiteModelEnum);
        StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(FocusShiftCache.LowSiteFindPosition);


        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<RtfcCache>();
        (var isHasFocusShiftCache, FocusShiftCache) = CacheProvider.TryGetOrDefault<FocusShiftCache>();
        (_, ResultFocusShiftDtoItems) = CacheProvider.TryGetOrDefaultArray<FocusShiftDto>();
        Calibrations = CacheProvider.GetOrDefaultArray<RtfcDto>();

        FocusShiftCalibrationViewModel.MicroscopePixelSizeItems = MicroscopePixelSizeItems;
        FocusShiftCalibrationViewModel.LaserPixelSizeItems = LaserPixelSizeItems;
        FocusShiftCalibrationViewModel.Cache = FocusShiftCache;

        foreach (var calibrationStatus in Calibrations)
        {
            CalibrationStatusList
                .Single(t => t.OpticsMagTypeEnum == calibrationStatus.OpticsMagTypeEnum)
                .IsCalibrated = calibrationStatus.IsCalibrated;
        }

        return (isHasCache || CacheProvider.Set(Cache, cancellationToken))
               && (isHasFocusShiftCache || CacheProvider.Set(FocusShiftCache, cancellationToken));
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

        return ReviewList.Any(t => t.IsCalibrated);
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 1:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(FocusShiftCache.LowMicroscopeLensInformation);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(FocusShiftCache.LowSiteFindPosition);
                return true;
            case 2 or 3 or 4:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(FocusShiftCache.HighMicroscopeLensInformation);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(FocusShiftCache.HighSiteFindPosition);
                return true;
            case 5:
                if (ResultRtfcDto is null)
                {
                    DialogWindowProvider.TryShowDialog("Please find focus shift!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    ResultRtfcDto.IsCalibrated = true;
                    if (Save(ResultRtfcDto, cancellationToken) == false)
                    {
                        ResultRtfcDto.IsCalibrated = false;
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        return false;
                    }
                }

                CalibrationStatusList.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum).IsCalibrated = true;
                DialogWindowProvider.ShowDialog($"{Cache.OpticsMagTypeEnum} Mag Calibration Ok!");

                IsCalibrated = CalibrationStatusList.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                ClearCalibrationTemp();

                return true;

            default:
                return true;
        }
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 3:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(FocusShiftCache.LowMicroscopeLensInformation);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(FocusShiftCache.LowSiteFindPosition);
                return true;
            case 4 or 5:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(FocusShiftCache.HighMicroscopeLensInformation);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(FocusShiftCache.HighSiteFindPosition);
                return true;
            default:
                return true;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand]
    private async Task GetPointAsync(string parameter)
    {
        try
        {
            Logger.LogInformation("{@Name}: Get Point Image Start", parameter);
            await Task.Run(() =>
            {
                try
                {
                    var result = StageViewModel.GetBrightFieldStagePosition();

                    switch (parameter)
                    {
                        case "BaseLowFindPosition":
                            FocusShiftCache.LowSiteFindPosition = result;
                            FocusShiftCache.HighSiteFindPosition = result;

                            FocusShiftCache.LowSiteTemplateFilePath = $"{TemplateFileDirectory}\\{FocusShiftCache.LowMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
                            var generateTemplateLow = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, FocusShiftCache.LowSiteTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
                            if (generateTemplateLow == false) DialogWindowProvider.ShowDialog("Generate Low Site Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            else FocusShiftCache.LowSiteTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(FocusShiftCache.LowSiteTemplateFilePath);

                            break;

                        case "BaseHighFindPosition":
                            {
                                Logger.LogHtmlInformation("Template", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

                                FocusShiftCache.HighSiteFindPosition = result;
                                FocusShiftCache.HighSiteTemplateFilePath = $"{TemplateFileDirectory}\\{FocusShiftCache.HighMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
                                var generateTemplateHigh = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, FocusShiftCache.HighSiteTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
                                if (generateTemplateHigh == false)
                                {
                                    DialogWindowProvider.ShowDialog("Generate High Site Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                    return;
                                }
                                else FocusShiftCache.HighSiteTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(FocusShiftCache.HighSiteTemplateFilePath);
                            }
                            break;

                        default:
                            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(parameter));
                            break;
                    }

                    Logger.LogInformation("{@Name}: Get Point Image OK!", parameter);
                }
                catch (Exception ex)
                {
                    Logger.LogInformation("{@Name}: Get Point Image OK!{@Exception}", parameter, ex.Message);
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Point Image Failed", parameter);
        }
    }

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
                IsAutoGain = FocusShiftCache.CIBConfiguration.IsAutoGainControl,
                DcGainVoltage = FocusShiftCache.CIBConfiguration.Gain,
                IsL0k = FocusShiftCache.CIBConfiguration.IsL0K,
                CIBProfileTypeEnum = FocusShiftCache.CIBConfiguration.CIBProfileMode
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand]
    private Task<bool> Step0CalibrateActionAsync()
    {
        return InvokeCalibrateAsync(() =>
        {
            var selectFocusShiftDto = ResultFocusShiftDtoItems.SingleOrDefault(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum);
            if (selectFocusShiftDto is null)
            {
                DialogWindowProvider.ShowDialog("Please find focus shift first!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            if (selectFocusShiftDto.IsCalibrated == false)
            {
                DialogWindowProvider.ShowDialog("Please calibration offset first!", DialogButtonsEnum.OK,
                    DialogIconEnum.Warning);
                return false;
            }

            SelectFocusShiftDto = selectFocusShiftDto.Clone();
            FocusShiftCache.OpticsMagTypeEnum = selectFocusShiftDto.OpticsMagTypeEnum;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsMagTypeEnum
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                FocusShiftCache.LowMicroscopeLensInformation.LensName,
                FocusShiftCache.LowSiteFindPosition,
                HtmlTab = new HtmlTab(new
                {
                    LowSiteTemplateImage = new HtmlImage(FocusShiftCache.LowSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            if (FocusShiftCache.HighMicroscopeLensInformation.LensCode <= FocusShiftCache.LowMicroscopeLensInformation.LensCode)
            {
                DialogWindowProvider.ShowDialog("The high magnification less than or equal low magnification! Please select correct magnification!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                FocusShiftCache.HighMicroscopeLensInformation.LensName,
                FocusShiftCache.HighSiteFindPosition,
                HtmlTab = new HtmlTab(new
                {
                    HighSiteTemplateImage = new HtmlImage(FocusShiftCache.HighSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(() =>
        {
            var settingDarkFieldAutoFocusParam = SelectFocusShiftDto.SettingDarkFieldAutoFocusParam.Clone();
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                FocusShiftCache.CalChipSiteModelEnum,
                Cache.AlgorithmTemplateSizeEnum,
                Cache.AlgorithmTemplateTypeEnum,
                Cache.OpticsMagTypeEnum,
                FocusShiftCache.StageSpeedEnum,
                Pmt = 8,
                xPixelWidth = 800,
                FocusShiftCache.LowSiteFindPosition,
                FocusShiftCache.HighSiteFindPosition,
                settingDarkFieldAutoFocusParam.IsEnableDsw,
                settingDarkFieldAutoFocusParam.DswEcsValue,
                settingDarkFieldAutoFocusParam.DswMotorValue,
            }), HtmlLogUniqueId.LoggingHtml());

            AfViewModel.SetDarkFieldAutoFocus(settingDarkFieldAutoFocusParam, FocusShiftCache.OpticsMagTypeEnum, FocusShiftCache.CalChipSiteModelEnum);
            var darkFieldImageDto = LaserViewModel.GetDarkFieldLineScanImage(
                FocusShiftCache.CalChipSiteModelEnum,
                FocusShiftCache.HighSiteFindPosition,
                (false, FocusShiftCache.LightCoefficient),
                false,
                FocusShiftCache.CIBConfiguration,
                800,
                Cache.OpticsMagTypeEnum,
                FocusShiftCache.StageSpeedEnum,
                stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);
            var detectImageDirectory = ImageFileDirectory;
            using var image = darkFieldImageDto;

            FocusShiftCache.DarkFiledTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.OpticsMagTypeEnum}_{Guid.NewGuid()}";
            if (Cache.AlgorithmTemplateTypeEnum == AlgorithmTemplateTypeEnum.Projection)
            {
                if (ReviewViewModel.TryGenerateProjectionTemplate(darkFieldImageDto.Image, FocusShiftCache.DarkFiledTemplateFilePath) == false)
                {
                    DialogWindowProvider.ShowDialog("Generate Dark Field Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }
            }
            else
            {
                var filePath = $"{detectImageDirectory}\\DarkFieldTemplateOriginImage_{Guid.NewGuid()}).jpg";
                HalconHelper.Save(darkFieldImageDto.Image, filePath);
                createDarkImageTemplateWindowViewModel.ImageFilePath = filePath;
                createDarkImageTemplateWindowViewModel.TemplateFilePath = FocusShiftCache.DarkFiledTemplateFilePath;

                var showDialog = WindowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel);

                if (showDialog == false)
                {
                    DialogWindowProvider.ShowDialog("Generate Dark Field Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }
            }

            FocusShiftCache.DarkFiledTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(FocusShiftCache.DarkFiledTemplateFilePath);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                darkFieldFindPosition = FocusShiftCache.HighSiteFindPosition,
                HtmlTab = new HtmlTab(new
                {
                    DarkFieldTemplateImage = new HtmlImage(FocusShiftCache.DarkFiledTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step4CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(async () =>
        {
            try
            {
                var settingDarkFieldAutoFocusParam = SelectFocusShiftDto.SettingDarkFieldAutoFocusParam.Clone();

                var (findFocusMin, findFocusMax, findFocusInterval) = FocusShiftCache.GetSteppingRangeParam();

                if (findFocusMin <= 0 || findFocusMax <= 0 || findFocusInterval == 0)
                {
                    DialogWindowProvider.ShowDialog("Please set the correct parameters!(Focus Min > 0 and Focs Max > 0 and Focus Interval > 0)", DialogButtonsEnum.OK,
                        DialogIconEnum.Warning);
                    return false;
                }

                var (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();
                Logger.LogHtmlInformation("1. Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    settingDarkFieldAutoFocusParam.DswEcsValue,
                    settingDarkFieldAutoFocusParam.DswMotorValue,
                    settingDarkFieldAutoFocusParam.IsEnableDsw,
                    BrightFiedlToDarkFieldOffset = SelectFocusShiftDto.BrightFieldToDarkFieldOffset,
                    SelectFocusShiftDto.EcsOffset,
                    xAxisTemperature,
                    yAxisTemperature,
                    reviewCamTemperature,
                    cibTemperature,
                    FocusShiftCache.AlgorithmTemplateSizeEnum,
                    FocusShiftCache.AlgorithmTemplateTypeEnum,
                    LowMgnification = FocusShiftCache.LowMicroscopeLensInformation.LensName,
                    HighMagnification = FocusShiftCache.HighMicroscopeLensInformation.LensName,
                    FocusShiftCache.CalChipSiteModelEnum,
                    Cache.OpticsMagTypeEnum,
                    FocusShiftCache.StageSpeedEnum,
                    Pmt = 8,
                    xPixelWidth = 800,
                    FocusShiftCache.LowSiteFindPosition,
                    FocusShiftCache.HighSiteFindPosition,
                    findFocusMin,
                    findFocusMax,
                    findFocusInterval,
                    Cache.ObliqueAngle,
                    FocusShiftCache.AfEcsRelation,
                    FocusShiftCache.AfMotorReviseThreshold,
                    Cache.OffsetThreshold,
                    FocusShiftCache.FocusShiftThreshold,
                    HtmlTab = new HtmlTab(new
                    {
                        LowSiteTemplateImage = new HtmlImage(FocusShiftCache.LowSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        HighSiteTemplateImage = new HtmlImage(FocusShiftCache.HighSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        DarkFieldTemplateImage = new HtmlImage(FocusShiftCache.DarkFiledTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                #region Bright Field

                // Bright Field Match
                Logger.LogHtmlInformation($"2. Bright Field Match Template", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                if (ReviewViewModel.TryGetMatchPosition(FocusShiftCache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, FocusShiftCache.LowSiteFindPosition, FocusShiftCache.LowMicroscopeLensInformation, FocusShiftCache.LowSiteTemplateFilePath, ImageFileDirectory,
                        HtmlLogUniqueId, Name,
                        "Low Magnification", out var lowResultPosition, out _, out _, out _, out _, FocusShiftCache.CalChipSiteModelEnum) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Low Magnification Matching Failed!"), HtmlLogUniqueId.LoggingHtml());
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(lowResultPosition), "Low Magnification Matching Failed!");
                }

                if (ReviewViewModel.TryGetMatchPosition(FocusShiftCache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, lowResultPosition, FocusShiftCache.HighMicroscopeLensInformation, FocusShiftCache.HighSiteTemplateFilePath, ImageFileDirectory,
                        HtmlLogUniqueId, Name,
                        "High Magnification", out var highResultPosition, out _, out _, out _, out _, FocusShiftCache.CalChipSiteModelEnum) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: High Magnification Matching Failed!"), HtmlLogUniqueId.LoggingHtml());
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(highResultPosition), "High Magnification Matching Failed!");
                }

                //获得明场Af模式下的ECS值
                var brightFieldMachinePosition = StageViewModel.BrightFieldToMachinePosition(highResultPosition);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(highResultPosition, FocusShiftCache.CalChipSiteModelEnum);
                var afEcs = AfViewModel.GetSensorAverageEcsValue();
                Logger.LogHtmlInformation("Bright Field Result", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    afEcs,
                    highSiteFindPosition = highResultPosition,
                    brightFieldMachinePosition
                }), HtmlLogUniqueId.LoggingHtml());

                #endregion Bright Field

                #region Dark Field

                Logger.LogHtmlInformation($"3. Dark Field", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                // 获得自动聚焦AutoEcs下的AutoEcs
                Cache.IdeaDarkFieldMachinePosition = brightFieldMachinePosition + (Vector)SelectFocusShiftDto.BrightFieldToDarkFieldOffset;
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.IdeaDarkFieldMachinePosition);

                var isAutoFocus = AfViewModel.SetDarkFieldAutoFocus(settingDarkFieldAutoFocusParam, Cache.OpticsMagTypeEnum, FocusShiftCache.CalChipSiteModelEnum);
                if (isAutoFocus == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("Error: Set Dark Field Auto Focus Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                AfViewModel.ToggleDarkFieldEnable(true);
                var nscBuffers = AfViewModel.GetSensorNscTraceBufferList(TimeSpan.FromSeconds(2));
                var autoFocusNsc = nscBuffers.Average();
                var autoFocusEcs = AfViewModel.GetSensorAverageEcsValue();
                (var nscDiagnosis, NscDiagnosisK) = AfViewModel.NscDiagnosis(autoFocusEcs, FocusShiftCache.CalChipSiteModelEnum);

                // 获得理想坐标IdeaEcs的IdeaNsc
                var idealEcs = afEcs + SelectFocusShiftDto.EcsOffset;
                AfViewModel.ToggleBrightFieldEnable(false);
                AfViewModel.SetSensorEcsValue(idealEcs);
                await Task.Delay(5000, cancellationToken);
                var notAutoFocusNscBuffers = AfViewModel.GetSensorNscTraceBufferList(TimeSpan.FromSeconds(2));
                var idealEcsNsc = notAutoFocusNscBuffers.Average();

                // AutoFocus ECS高度的图像
                AfViewModel.ToggleBrightFieldEnable(false);
                AfViewModel.SetSensorEcsValue(autoFocusEcs);
                await Task.Delay(5000, cancellationToken);
                using var darkFieldImageDto = LaserViewModel.GetDarkFieldLineScanImage(
                    FocusShiftCache.CalChipSiteModelEnum,
                    Cache.IdeaDarkFieldMachinePosition,
                    (false, FocusShiftCache.LightCoefficient),
                    true,
                    FocusShiftCache.CIBConfiguration,
                    800,
                    Cache.OpticsMagTypeEnum,
                    FocusShiftCache.StageSpeedEnum,
                    8,
                    3,
                    StageCoordinateSystemEnum.Machine);
                var path = $"{ImageFileDirectory}\\ECS({autoFocusEcs})_AutoFocus_Guid({HtmlLogUniqueId}).jpg";
                var nscDarkFieldImageFilePath =
                    $"{ImageFileDirectory}\\ECS({autoFocusEcs})_AutoFocus_Guid({HtmlLogUniqueId}).jpg";
                HalconHelper.Save(darkFieldImageDto.Image, nscDarkFieldImageFilePath);

                Logger.LogHtmlInformation("Result", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    Cache.IdeaDarkFieldMachinePosition,
                    idealEcs,
                    idealEcsNsc,
                    autoFocusEcs,
                    autoFocusNsc,
                    NscDiagnosisK,
                    NscDiagnosisTraceBuffers = new HtmlPlot2DLinesChart([
                        ("Ecs-Nsc", nscDiagnosis),
                    ], "NscDiagnosisTraceBuffers"),
                    IdeaEcsNscTraceBuffer = new HtmlPlot2DLinesChart([
                        ("Time-nm", notAutoFocusNscBuffers.Select(t => t * NscDiagnosisK).ToPoints()),
                    ], "IdeaEcsNscTraceBuffer"),
                    AutoFocusNscTraceBuffer = new HtmlPlot2DLinesChart([
                        ("Time-nm", nscBuffers.Select(t => t * NscDiagnosisK).ToPoints()),
                    ], "AutoFocusNscTraceBuffer"),
                    HtmlTab = new HtmlTab(new
                    {
                        nscDarkFieldImageFilePath = new HtmlImage(nscDarkFieldImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    }),
                }), HtmlLogUniqueId.LoggingHtml());

                #endregion Dark Field

                #region Reviese Af

                // 修正Af
                Logger.LogHtmlInformation($"4. Revise Af", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                var focusShiftOffset = idealEcs - autoFocusEcs;
                var afMotorOffset = focusShiftOffset / FocusShiftCache.AfEcsRelation;
                if (Math.Abs(focusShiftOffset) > FocusShiftCache.AfMotorReviseThreshold)
                {
                    settingDarkFieldAutoFocusParam.DswMotorValue -= afMotorOffset;
                    AfViewModel.SetDarkFieldAutoFocusMotorAbsoluteValue(settingDarkFieldAutoFocusParam.DswMotorValue);
                    StageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(Cache.IdeaDarkFieldMachinePosition, FocusShiftCache.CalChipSiteModelEnum);
                    AfViewModel.ToggleDarkFieldEnable(true);
                    var dswEcs = AfViewModel.GetSensorAverageEcsValue();
                    settingDarkFieldAutoFocusParam.DswEcsValue = dswEcs;

                    AfViewModel.SetDarkFieldAutoFocus(settingDarkFieldAutoFocusParam, FocusShiftCache.OpticsMagTypeEnum, FocusShiftCache.CalChipSiteModelEnum);
                    nscBuffers = AfViewModel.GetSensorNscTraceBufferList(TimeSpan.FromSeconds(2));
                    autoFocusNsc = nscBuffers.Average();
                    autoFocusEcs = AfViewModel.GetSensorAverageEcsValue();
                    focusShiftOffset = autoFocusEcs - idealEcs;
                    if (Math.Abs(focusShiftOffset) > FocusShiftCache.FocusShiftThreshold)
                    {
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("Error: Revise Af Failed!"), HtmlLogUniqueId.LoggingHtml());
                        return false;
                    }
                }

                Logger.LogHtmlInformation("Result", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    FocusShiftCache.AfMotorReviseThreshold,
                    FocusShiftCache.FocusShiftThreshold,
                    idealEcs,
                    focusShiftOffset,
                    afMotorOffset,
                    settingDarkFieldAutoFocusParam.DswEcsValue,
                    settingDarkFieldAutoFocusParam.DswMotorValue,
                    autoFocusEcs,
                }), HtmlLogUniqueId.LoggingHtml());

                #endregion Reviese Af

                #region Reviese Light Focus

                // 修正照明焦点
                Logger.LogHtmlInformation("5. Revise Light Focus", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                // 迭代修正
                SynchronizationContextProvider.Send(() =>
                {
                    EcsPoints = [];
                    RtfcDtoIterationItems.Clear();
                });
                var iterationIndex = 1;
                var iterationResult = await RevieseLightFocusAsync(cancellationToken);
                if (iterationResult == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Get Optimal Quality Failed!"), HtmlLogUniqueId.LoggingHtml());
                }

                Logger.LogHtmlInformation("Iteration Result", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    Cache.ObliqueAngle,
                    ResultRtfcDto.DarkFieldMatchOffset,
                    ResultRtfcDto.DfOffset,
                    ReviseEcs = ResultRtfcDto.EcsValue,
                    ResultRtfcDto.DeltaEcs,
                    ResultRtfcDto.LightAxisOffset,
                    HtmlTab = new HtmlTab(new
                    {
                        ResultImage = new HtmlImage(ResultRtfcDto.DarkFieldImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        TemplateImage = new HtmlImage(FocusShiftCache.DarkFiledTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    }),
                    IterationCurve = new HtmlPlot2DLinesChart([
                        ("Times-Ecs", RtfcDtoIterationItems.Select(t => t.EcsValue).ToList().ToPoints()),
                        ("Times-DfOffset", RtfcDtoIterationItems.Select(t => t.DfOffset).ToList().ToPoints()),
                        ("Times-XOffset", RtfcDtoIterationItems.Select(t => t.DarkFieldMatchOffset.X).ToList().ToPoints()),
                        ("Times-DeltaZ", RtfcDtoIterationItems.Select(t => t.DeltaEcs).ToList().ToPoints()),
                        ("Times-LightAxisOffset", RtfcDtoIterationItems.Select(t => t.LightAxisOffset).ToList().ToPoints()),
                    ], "IterationCurve"),
                }), HtmlLogUniqueId.LoggingHtml());

                #endregion Reviese Light Focus

                (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();
                ResultRtfcDto.IdealEcs = idealEcs;
                ResultRtfcDto.IdealEcsNsc = idealEcsNsc;
                ResultRtfcDto.AutoFocusEcs = autoFocusEcs;
                ResultRtfcDto.AutoFocusNsc = autoFocusNsc;
                ResultRtfcDto.DarkFieldAutoFocusParam = settingDarkFieldAutoFocusParam;
                ResultRtfcDto.AfMotorOffset = afMotorOffset;

                Logger.LogHtmlInformation($"{(iterationResult ? "Ok" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    xAxisTemperature,
                    yAxisTemperature,
                    reviewCamTemperature,
                    cibTemperature,
                    afEcs,
                    ResultRtfcDto.LightAxisOffset,
                    ResultDeltaZ = ResultRtfcDto.DeltaEcs,
                    RevieseDswEcs = settingDarkFieldAutoFocusParam.DswEcsValue,
                    RevieseDswMotor = settingDarkFieldAutoFocusParam.DswMotorValue,
                    idealEcs,
                    idealEcsNsc,
                    autoFocusEcs,
                    autoFocusNsc,
                    RealEcs = ResultRtfcDto.EcsValue,
                    RealNsc = ResultRtfcDto.NscValue,
                    ResultRtfcDto.DarkFieldMatchOffset,
                    ResultRtfcDto.AfOffset,
                    ResultRtfcDto.DfOffset,
                }), HtmlLogUniqueId.LoggingHtml());

                return iterationResult;

                async Task<bool> RevieseLightFocusAsync(CancellationToken cancellationToken)
                {
                    Logger.LogHtmlInformation($"Iteration Times: {iterationIndex}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                    SynchronizationContextProvider.Send(() =>
                    {
                        EcsPoints = [];
                        RtfcDtoItems.Clear();
                    });
                    cancellationToken.ThrowIfCancellationRequested();

                    var rtfcDtoList = new List<RtfcDto>();
                    var (findFocusMin, findFocusMax, findFocusInterval) = FocusShiftCache.GetSteppingRangeParam();
                    var minEcs = idealEcs - findFocusMin;
                    var maxEcs = idealEcs + findFocusMax;
                    Logger.LogHtmlInformation("Get Quality Param", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
                    {
                        idealEcs,
                        FindFocusMin = minEcs,
                        FindFocusMax = maxEcs,
                        findFocusInterval,
                    }), HtmlLogUniqueId.LoggingHtml());
                    // 初始化
                    var findDarkFieldPosition = StageViewModel.MachineToDarkFieldPosition(Cache.IdeaDarkFieldMachinePosition);
                    var findBrightFieldMachinePosition = StageViewModel.BrightFieldToMachinePosition(findDarkFieldPosition);
                    foreach (var (index, ecsValueTemp) in Enumerable.Range(0, Convert.ToInt32((findFocusMax + findFocusMin) / findFocusInterval) + 1)
                                 .Select(x => Math.Min(minEcs + x * findFocusInterval, maxEcs))
                                 .Select((d, i) => (i, d)))
                    {
                        rtfcDtoList.Add(new RtfcDto
                        {
                            Index = index,
                            BrightFieldFindPosition = findDarkFieldPosition,
                            Quality = 0,
                            EcsValue = ecsValueTemp,
                            AutoFocusEcs = autoFocusEcs,
                            AutoFocusNsc = autoFocusNsc,
                            IdealEcs = idealEcs,
                            IdealEcsNsc = idealEcsNsc,
                            DarkFieldImageFilePath = ImageFileDirectory
                        });
                    }

                    if (GetRealEcs(rtfcDtoList, cancellationToken, out var rtfcDto) == false) return false;
                    ResultRtfcDto = rtfcDto.Clone();

                    Logger.LogHtmlInformation("Calibration Light Focus", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());
                    // 读取最清晰Ecs下的Nsc
                    StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.IdeaDarkFieldMachinePosition);
                    AfViewModel.ToggleBrightFieldEnable(false);
                    AfViewModel.SetSensorEcsValue(ResultRtfcDto.EcsValue);
                    await Task.Delay(5000, cancellationToken);
                    var nscBuffers = AfViewModel.GetSensorNscTraceBufferList(TimeSpan.FromSeconds(2));
                    ResultRtfcDto.NscValue = nscBuffers.Average();
                    // 获得照明焦点偏移量
                    if (LaserViewModel.TryGetMatchPosition(
                            FocusShiftCache.AlgorithmTemplateTypeEnum,
                            FocusShiftCache.CalChipSiteModelEnum,
                            8,
                            findDarkFieldPosition,
                            FocusShiftCache.DarkFiledTemplateFilePath,
                            ResultRtfcDto.DarkFieldImageFilePath,
                            HtmlLogUniqueId,
                            string.Empty,
                            string.Empty,
                            FocusShiftCache.CIBConfiguration,
                            out var resultPosition,
                            out _,
                            out _,
                            out var resultImageFilePath,
                            true,
                            800,
                            Cache.OpticsMagTypeEnum,
                            FocusShiftCache.StageSpeedEnum,
                            StageCoordinateSystemEnum.Bright,
                            FocusShiftCache.LightCoefficient) == false)
                    {
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Dark Field Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
                        return false;
                    }

                    var matchMachinePosition = StageViewModel.DarkFieldToMachinePosition(resultPosition);
                    ResultRtfcDto.BrightFieldFindPosition = brightFieldMachinePosition;
                    ResultRtfcDto.DarkFieldFindPosition = matchMachinePosition;
                    ResultRtfcDto.DarkFieldImageFilePath = resultImageFilePath;
                    ResultRtfcDto.DarkFieldMatchOffset = matchMachinePosition - (Vector)Cache.IdeaDarkFieldMachinePosition;
                    var result = Math.Abs(ResultRtfcDto.DarkFieldMatchOffset.X) < Cache.OffsetThreshold;
                    SynchronizationContextProvider.Send(() => { RtfcDtoIterationItems.Add(ResultRtfcDto); });

                    Logger.LogHtmlInformation($"Reviese Light Focus {(result ? "Ok" : "Failed")}", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
                    {
                        Cache.IdeaDarkFieldMachinePosition,
                        ResultRtfcDto.LightAxisOffset,
                        ResultRtfcDto.DeltaEcs,
                        MatchDarkMachinePosition = matchMachinePosition,
                        ResultRtfcDto.DarkFieldMatchOffset,
                        ResultRtfcDto.EcsValue,
                        ResultRtfcDto.NscValue,
                        NscTraceBuffers = new HtmlPlot2DLinesChart([
                            ("Time-Nsc", nscBuffers.ToPoints()),
                        ], "NscTraceBuffers"),
                        HtmlTab = new HtmlTab(new
                        {
                            ResultImage = new HtmlImage(ResultRtfcDto.DarkFieldImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                            TemplateImage = new HtmlImage(FocusShiftCache.DarkFiledTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        })
                    }), HtmlLogUniqueId.LoggingHtml());

                    if (result) return true;
                    if (iterationIndex == 4) return false; // 迭代次数超过4次则返回失败

                    // 照明修正值(方向未定)
                    ResultRtfcDto.LightAxisOffset = ResultRtfcDto.DarkFieldMatchOffset.X / Math.Sin(Cache.ObliqueAngle);
                    ResultRtfcDto.DeltaEcs = ResultRtfcDto.DarkFieldMatchOffset.X / Math.Tan(Cache.ObliqueAngle);
                    // todo:修正照明轴
                    //AfViewModel.SetSensorEcsValue(ResultRtfcDto.EcsValue+ResultRtfcDto.DeltaEcs);

                    iterationIndex++;
                    return await RevieseLightFocusAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Calibration Failed! {ex.Message}"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
            finally
            {
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(FocusShiftCache.HighSiteFindPosition);
            }
        });
    }


    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> VerifyActionAsync(CancellationToken cancellationToken)
    {
        return await InvokeVerifyAsync(async () =>
        {
            if (SelectReviewDto is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            FocusShiftCalibrationViewModel.HtmlLogUniqueId = HtmlLogUniqueId;
            SelectReviewDto.IsVerified = false;
            FocusShiftCache.OpticsMagTypeEnum = SelectReviewDto.OpticsMagTypeEnum;
            var settingDarkFieldAutoFocusParam = SelectReviewDto.DarkFieldAutoFocusParam.Clone();
            var selectFocusShiftDto = ResultFocusShiftDtoItems.SingleOrDefault(t => t.OpticsMagTypeEnum == SelectReviewDto.OpticsMagTypeEnum);
            if (selectFocusShiftDto is null)
            {
                DialogWindowProvider.ShowDialog("Please find focus shift first!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            if (await FocusShiftCalibrationViewModel.CalibrationAsync(settingDarkFieldAutoFocusParam, cancellationToken) == false) return false;

            ResultRtfcDto.DarkFieldMatchOffset = FocusShiftCalibrationViewModel.ResultFocusShiftDto.DarkFieldFindPosition - (Vector)SelectReviewDto.DarkFieldFindPosition;
            var afEcs = FocusShiftCalibrationViewModel.ResultFocusShiftDto.BrightFieldEcsValue;
            var idealEcs = afEcs + selectFocusShiftDto.EcsOffset;
            var afShift = FocusShiftCalibrationViewModel.ResultFocusShiftDto.AutoFocusEcs - idealEcs;
            var dfShift = FocusShiftCalibrationViewModel.ResultFocusShiftDto.DarkFieldEcsValue - idealEcs;
            var afDfOffset = FocusShiftCalibrationViewModel.ResultFocusShiftDto.FocusShiftOffset;
            var result = SelectReviewDto.IsVerified = Math.Abs(afShift) < FocusShiftCache.FocusShiftThreshold
                                                      && Math.Abs(dfShift) < FocusShiftCache.FocusShiftThreshold
                                                      && Math.Abs(ResultRtfcDto.DarkFieldMatchOffset.X) < Cache.OffsetThreshold;

            if (Save(SelectReviewDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                SelectReviewDto.IsVerified = false;
                return false;
            }

            DialogWindowProvider.ShowDialog($"RTFC Verify {(result ? "OK" : "Failed")}", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);
            Logger.LogHtmlInformation($"Verify {(result ? "Ok" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                FocusShiftCache.FocusShiftThreshold,
                Cache.OffsetThreshold,
                IdealEcs = idealEcs,
                FocusShiftCalibrationViewModel.ResultFocusShiftDto.AutoFocusEcs,
                RealEcs = FocusShiftCalibrationViewModel.ResultFocusShiftDto.DarkFieldEcsValue,
                FocusShiftCalibrationViewModel.ResultFocusShiftDto.FocusShiftOffset,
                afShift,
                dfShift,
                afDfOffset,
                ResultRtfcDto.DarkFieldMatchOffset,
            }), HtmlLogUniqueId.LoggingHtml());
            return result;
        }).ConfigureAwait(false);
    }

    private bool GetRealEcs(List<RtfcDto> rtfcDtoList, CancellationToken cancellationToken, out RtfcDto rtfcDto)
    {
        rtfcDto = new();
        try
        {
            // Quality
            var listDownResult = new List<bool>();
            double? downQualityValue = null;
            Logger.LogHtmlInformation("Get Optimal Quality", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());
            foreach (var rtfcDtoItem in rtfcDtoList.OrderBy(t => t.Index))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var tempItemDto = rtfcDtoItem.Clone();
                if (GetQuality(ref tempItemDto) == false) return false;
                SynchronizationContextProvider.Send(() =>
                {
                    RtfcDtoItems.Add(tempItemDto);
                    EcsPoints = [.. EcsPoints, new Point(tempItemDto.EcsValue, tempItemDto.Quality)];
                });

                if (downQualityValue is not null) listDownResult.Add(downQualityValue.Value < tempItemDto.Quality);
                downQualityValue = tempItemDto.Quality;
                if (EnumerableHelper.HasConsecutiveFalse(listDownResult, 10)) break; // 连续30个下降说明已经到了最低点
            }

            rtfcDto = RtfcDtoItems.Select(s => s.Clone()).Maxima(s => s.Quality).Single();
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, new HtmlQuote(new
            {
                rtfcDto.EcsValue,
                rtfcDto.Quality,
                HtmlTab = new HtmlTab(new
                {
                    ResultImage = new HtmlImage(rtfcDto.DarkFieldImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)]),
                }),
                EcsQuality = new HtmlPlot2DLinesChart([
                    ("Ecs-Quality", RtfcDtoItems.Select(t => new Point(t.EcsValue, t.Quality)).ToArray())
                ], "EcsQuality")
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment($"Error: Get Real ECS Failed! {ex.Message}"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }
    }

    private bool GetQuality(ref RtfcDto rtfcItemDto)
    {
        try
        {
            Logger.LogHtmlInformation($"Get Quality,Ecs:{rtfcItemDto.EcsValue}", HtmlHeaderLevelEnum.Header6, HtmlLogUniqueId.LoggingHtml());
            // 防止移动后明场af模式打开
            AfViewModel.ToggleBrightFieldEnable(false);

            if (rtfcItemDto.Index == 0) Thread.Sleep(2000);
            AfViewModel.SetSensorEcsValue(rtfcItemDto.EcsValue);
            Thread.Sleep(1000);
            using var darkFieldImageDto = LaserViewModel.GetDarkFieldLineScanImage(
                FocusShiftCache.CalChipSiteModelEnum,
                rtfcItemDto.BrightFieldFindPosition,
                (false, FocusShiftCache.LightCoefficient),
                true,
                FocusShiftCache.CIBConfiguration,
                800,
                Cache.OpticsMagTypeEnum,
                StageSpeedEnum.Low,
                8,
                3,
                StageCoordinateSystemEnum.Dark);

            using var scaleImage = HalconHelper.ScaleImageTo8Bit(darkFieldImageDto.Image);
            var xQuality = CalibrationAlgorithmService.GetDarkFieldQuality(scaleImage);
            var path = $"{ImageFileDirectory}\\ECS({rtfcItemDto.EcsValue})_Guid({HtmlLogUniqueId}).hobj";
            //HOperatorSet.WriteObject(scaleImage, path);
            var qualityX = xQuality;
            rtfcItemDto.DarkFieldImageFilePath =
                $"{ImageFileDirectory}\\ECS({rtfcItemDto.EcsValue})_Guid({HtmlLogUniqueId}).jpg";
            rtfcItemDto.DarkFieldOriginImageFilePath = CalibrationConstantsHelper.ImagePathToRawImagePath(rtfcItemDto.DarkFieldImageFilePath);
            rtfcItemDto.Quality = qualityX;

            FileHelper.Save(darkFieldImageDto.Bytes, rtfcItemDto.DarkFieldOriginImageFilePath);
            HalconHelper.Save(darkFieldImageDto.Image, rtfcItemDto.DarkFieldImageFilePath);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
            {
                rtfcItemDto.EcsValue,
                ImageQuality = rtfcItemDto.Quality,
                HtmlTab = new HtmlTab(new
                {
                    Image = new HtmlImage(rtfcItemDto.DarkFieldImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Error: Get Quality Failed! {ex.Message}"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }
    }

    private bool Save(RtfcDto itemDto, CancellationToken cancellationToken, bool isSave = true) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        itemDto.OpticsMagTypeEnum = Cache.OpticsMagTypeEnum;
        Calibrations =
        [
            .. Calibrations
                .Where(t => t.OpticsMagTypeEnum != itemDto.OpticsMagTypeEnum),
            itemDto.Clone(),
        ];

        if (isSave == false) return true;

        return CacheProvider.SetArray(Calibrations, cancellationToken)
               && CacheProvider.Set(Cache, cancellationToken)
               && CacheProvider.Set(FocusShiftCache, cancellationToken)
               && EnableDependedCalibrationItems(cancellationToken);
    });

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependLaserRtfcCalibrations(false, cancellationToken, out var errorMsg) == false)
        {
            Logger.LogError("Toggle {@Name} Enable Status Failed!", errorMsg);
            return false;
        }

        return true;
    }

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(RtfcDtoItems.Clear);
        SynchronizationContextProvider.Send(RtfcDtoIterationItems.Clear);
    }

    public void InvokeCircleService(Action action, CancellationToken cancellationToken)
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
                Logger.LogError(ex, "Invoke failed, retrying {RetryCount} times", i + 1);
            }
        }

        throw new CugaException($"Ads Service Invoke Error! {nameof(action.Method.Name)}");
    }

    #endregion 校准
}