using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.FocusShift;
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
using CugaCalibration.ViewModels.Common.Windows.Tools;
using Local.NoSQL.DB.Providers.Extensions;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;
using Core.Models.Models.AOD.AODDelay;

namespace CugaCalibration.ViewModels.Laser;

[IOCAppService(ServiceType = typeof(LaserFocusShiftCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class LaserFocusShiftCalibrationViewModel(CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel) : CalibrationViewModelBase
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
        new() { StepName = "Find Dark Field Position" },
        new() { StepName = "Calibration" }
    ];

    #region 界面相关

    #region Calibrate

    /// <summary>
    /// 焦点位移对象集合
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<FocusShiftDto> _focusShiftDtoItems = [];

    [ObservableProperty]
    private FocusShiftDto _resultFocusShiftDto = new();

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
    private ObservableCollection<FocusShiftDto> _reviewList = [];

    [ObservableProperty]
    private FocusShiftDto? _selectReviewDto;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private FocusShiftCache _cache = new();

    [ObservableProperty]
    private FocusShiftDto[] _calibrations = [];

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

    [ObservableProperty]
    private LaserPixelSizeItemDto[] _laserPixelSizeItems = [];

    [ObservableProperty]
    private LaserLineCentricityItemDto[] _laserLineCentricityItems = [];

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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<AODDelayDto>(out _, out errorMessage) == false)
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

        LaserViewModel.ToggleEnableAutoGainControl(true);
        LaserViewModel.ToggleEnableL0K(false);

        StageViewModel.SetAbsoluteStageTheta(0);
        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
        AfViewModel.ToggleCalChipSiteModelEnum(Cache.CalChipSiteModelEnum);
        StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(Cache.LowSiteFindPosition);

        (var isHasCache, Cache) = CacheProvider.TryGetOrDefault<FocusShiftCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<FocusShiftDto>();

        if (Cache.LowMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.LowMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();
        if (Cache.HighMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.HighMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.HighMicroscopeLensInformation.Clone();

        foreach (var calibrationStatus in Calibrations)
        {
            CalibrationStatusList
                .Single(t => t.OpticsMagTypeEnum == calibrationStatus.OpticsMagTypeEnum)
                .IsCalibrated = calibrationStatus.IsCalibrated;
        }

        if (isHasCache == false) CacheProvider.Set(Cache, cancellationToken);

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
        return ReviewList.Any(t => t.IsCalibrated);
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(Cache.LowSiteFindPosition);
                return true;

            case 1 or 2 or 3:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(Cache.HighSiteFindPosition);
                return true;

            case 4:
                if (ResultFocusShiftDto is null)
                {
                    DialogWindowProvider.TryShowDialog("Please find focus shift!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    ResultFocusShiftDto.IsCalibrated = true;
                    if (Save(ResultFocusShiftDto, cancellationToken) == false)
                    {
                        ResultFocusShiftDto.IsCalibrated = false;
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
            case 2:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(Cache.LowSiteFindPosition);
                return true;

            case 3 or 4:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(Cache.HighSiteFindPosition);
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
                            Cache.LowSiteFindPosition = result;
                            Cache.HighSiteFindPosition = result;

                            Cache.LowSiteTemplateFilePath = $"{TemplateFileDirectory}\\{Cache.LowMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
                            var generateTemplateLow = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.LowSiteTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
                            if (generateTemplateLow == false) DialogWindowProvider.ShowDialog("Generate Low Site Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            else Cache.LowSiteTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.LowSiteTemplateFilePath);

                            break;

                        case "BaseHighFindPosition":
                            {
                                Logger.LogHtmlInformation("Template", HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

                                Cache.HighSiteFindPosition = result;
                                Cache.HighSiteTemplateFilePath = $"{TemplateFileDirectory}\\{Cache.HighMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
                                var generateTemplateHigh = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.HighSiteTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
                                if (generateTemplateHigh == false)
                                {
                                    DialogWindowProvider.ShowDialog("Generate High Site Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                    return;
                                }
                                else Cache.HighSiteTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.HighSiteTemplateFilePath);
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
    private Task<bool> Step0CalibrateActionAsync()
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
    private Task<bool> Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.LowMicroscopeLensInformation.LensName,
                Cache.LowSiteFindPosition,
                HtmlTab = new HtmlTab(new
                {
                    LowSiteTemplateImage = new HtmlImage(Cache.LowSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
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
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.HighMicroscopeLensInformation.LensName,
                Cache.HighSiteFindPosition,
                HtmlTab = new HtmlTab(new
                {
                    HighSiteTemplateImage = new HtmlImage(Cache.HighSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
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
            var settingDarkFieldAutoFocusParam = Cache.GetDarkFieldAutoFocusParam();
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.CalChipSiteModelEnum,
                Cache.AlgorithmTemplateSizeEnum,
                Cache.AlgorithmTemplateTypeEnum,
                Cache.OpticsMagTypeEnum,
                Cache.StageSpeedEnum,
                Pmt = 8,
                xPixelWidth = 800,
                Cache.LowSiteFindPosition,
                Cache.HighSiteFindPosition,
                settingDarkFieldAutoFocusParam.IsEnableDsw,
                settingDarkFieldAutoFocusParam.DswEcsValue,
                settingDarkFieldAutoFocusParam.DswMotorValue
            }), HtmlLogUniqueId.LoggingHtml());

            var darkFieldImageDto = LaserViewModel.GetDarkFieldLineScanImage(
                Cache.CalChipSiteModelEnum,
                Cache.HighSiteFindPosition,
                (false, Cache.LaserLightInformation),
                false,
                Cache.CIBConfiguration,
                800,
                Cache.OpticsMagTypeEnum,
                Cache.StageSpeedEnum,
                stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);
            var detectImageDirectory = ImageFileDirectory;
            using var image = darkFieldImageDto;

            Cache.DarkFiledTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.OpticsMagTypeEnum}_{Guid.NewGuid()}";
            if (Cache.AlgorithmTemplateTypeEnum == AlgorithmTemplateTypeEnum.Projection)
            {
                if (ReviewViewModel.TryGenerateProjectionTemplate(darkFieldImageDto.Image, Cache.DarkFiledTemplateFilePath) == false)
                {
                    DialogWindowProvider.ShowDialog("Generate Dark Field Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }
            }
            else
            {
                var filePath = $"{detectImageDirectory}\\DarkFieldTemplateOriginImage_{Guid.NewGuid()}).jpg";
                darkFieldImageDto.Image.Save(filePath);
                createDarkImageTemplateWindowViewModel.ImageFilePath = filePath;
                createDarkImageTemplateWindowViewModel.TemplateFilePath = Cache.DarkFiledTemplateFilePath;

                var showDialog = WindowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel);

                if (showDialog == false)
                {
                    DialogWindowProvider.ShowDialog("Generate Dark Field Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }
            }

            Cache.DarkFiledTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.DarkFiledTemplateFilePath);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                darkFieldFindPosition = Cache.HighSiteFindPosition,
                HtmlTab = new HtmlTab(new
                {
                    DarkFieldTemplateImage = new HtmlImage(Cache.DarkFiledTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
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
            var settingDarkFieldAutoFocusParam = Cache.GetDarkFieldAutoFocusParam();
            var result = await CalibrationAsync(settingDarkFieldAutoFocusParam, cancellationToken);
            DialogWindowProvider.ShowDialog($"Focus Shift Calibration {(result ? "OK" : "Failed")}", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);
            return result;
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

            SelectReviewDto.IsVerified = false;
            Cache.OpticsMagTypeEnum = SelectReviewDto.OpticsMagTypeEnum;
            var settingDarkFieldAutoFocusParam = SelectReviewDto.SettingDarkFieldAutoFocusParam.Clone();

            if (await CalibrationAsync(settingDarkFieldAutoFocusParam, cancellationToken) == false) return false;
            var result = Math.Abs(ResultFocusShiftDto.FocusShiftOffset) < Cache.FocusShiftThreshold;
            if (result)
            {
                Cache.SetDarkFieldAutoFocusParam(ResultFocusShiftDto.SettingDarkFieldAutoFocusParam);
                SelectReviewDto = ResultFocusShiftDto.Clone();
            }

            SelectReviewDto.IsCalibrated = true;
            SelectReviewDto.IsVerified = result;

            if (Save(SelectReviewDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                SelectReviewDto.IsVerified = false;
                return false;
            }

            await ReviewingAsync(cancellationToken).ConfigureAwait(false);
            Logger.LogHtmlInformation($"Verify {(result ? "Ok" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.FocusShiftThreshold,
                ResultFocusShiftDto.AutoFocusEcs,
                RealEcs = ResultFocusShiftDto.DarkFieldEcsValue,
                ResultFocusShiftDto.FocusShiftOffset,
                ResultFocusShiftDto.SettingDarkFieldAutoFocusParam.DswEcsValue,
                ResultFocusShiftDto.SettingDarkFieldAutoFocusParam.DswMotorValue
            }), HtmlLogUniqueId.LoggingHtml());

            DialogWindowProvider.ShowDialog($"Focus Shift Verify {(result ? "OK" : "Failed")}", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }).ConfigureAwait(false);
    }

    public async Task<bool> CalibrationAsync(SettingDarkFieldAutoFocusParam settingDarkFieldAutoFocusParam, CancellationToken cancellationToken)
    {
        try
        {
            ClearCalibrationTemp();
            var detectImageDirectory = ImageFileDirectory;

            var (findFocusMin, findFocusMax, findFocusInterval) = Cache.GetSteppingRangeParam();

            if (findFocusMin <= 0 || findFocusMax <= 0 || findFocusInterval == 0)
            {
                DialogWindowProvider.ShowDialog("Please set the correct parameters!(Focus Min > 0 and Focs Max > 0 and Focus Interval > 0)", DialogButtonsEnum.OK,
                    DialogIconEnum.Warning);
                return false;
            }

            Logger.LogHtmlInformation("1. Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.CalChipSiteModelEnum,
                Cache.AlgorithmTemplateSizeEnum,
                Cache.AlgorithmTemplateTypeEnum,
                Cache.OpticsMagTypeEnum,
                Cache.StageSpeedEnum,
                Pmt = 8,
                xPixelWidth = 800,
                Cache.LowSiteFindPosition,
                Cache.HighSiteFindPosition,
                settingDarkFieldAutoFocusParam.IsEnableDsw,
                settingDarkFieldAutoFocusParam.DswEcsValue,
                settingDarkFieldAutoFocusParam.DswMotorValue,
                findFocusMin,
                findFocusMax,
                findFocusInterval,
                HtmlTab = new HtmlTab(new
                {
                    LowSiteTemplateImage = new HtmlImage(Cache.LowSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighSiteTemplateImage = new HtmlImage(Cache.HighSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    DarkFieldTemplateImage = new HtmlImage(Cache.DarkFiledTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            #region Bright Field

            Logger.LogHtmlInformation("2. Bright Field", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.LowSiteFindPosition, Cache.LowMicroscopeLensInformation, Cache.LowSiteTemplateFilePath, ImageFileDirectory,
                    HtmlLogUniqueId, Name,
                    "Low Magnification", out var lowResultPosition, out _, out _, out _, out _, Cache.CalChipSiteModelEnum) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Low Magnification Matching Failed!"), HtmlLogUniqueId.LoggingHtml());
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(lowResultPosition), "Low Magnification Matching Failed!");
            }

            Cache.LowSiteFindPosition = lowResultPosition;
            if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.LowSiteFindPosition, Cache.HighMicroscopeLensInformation, Cache.HighSiteTemplateFilePath, ImageFileDirectory,
                    HtmlLogUniqueId, Name,
                    "High Magnification", out var highResultPosition, out _, out _, out _, out _, Cache.CalChipSiteModelEnum) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: High Magnification Matching Failed!"), HtmlLogUniqueId.LoggingHtml());
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(highResultPosition), "High Magnification Matching Failed!");
            }

            Cache.HighSiteFindPosition = highResultPosition;

            //获得明场Af模式下的ECS、机械坐标
            StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(Cache.HighSiteFindPosition, Cache.CalChipSiteModelEnum);
            var brightFieldMachinePosition = StageViewModel.BrightFieldToMachinePosition(Cache.HighSiteFindPosition);
            await Task.Delay(2000, cancellationToken);
            var afEcs = AfViewModel.GetSensorAverageEcsValue();
            Logger.LogHtmlInformation("Result", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                highSiteFindPosition = Cache.HighSiteFindPosition,
                brightFieldMachinePosition,
                afEcs
            }), HtmlLogUniqueId.LoggingHtml());

            #endregion Bright Field

            #region Dark Field

            Logger.LogHtmlInformation("3. Dark Field", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            var originImagePath = $"{detectImageDirectory}\\DarkFieldMatchOriginImage_{Guid.NewGuid()}).jpg";
            if (LaserViewModel.TryGetMatchPositionByScanImage(
                    Cache.AlgorithmTemplateTypeEnum,
                    Cache.CalChipSiteModelEnum,
                    8,
                    Cache.HighSiteFindPosition,
                    Cache.DarkFiledTemplateFilePath,
                    originImagePath,
                    HtmlLogUniqueId,
                    string.Empty,
                    string.Empty,
                    Cache.CIBConfiguration,
                    out var darkFieldResultPosition,
                    out _,
                    out _,
                    out _,
                    true,
                    800,
                    Cache.OpticsMagTypeEnum,
                    Cache.StageSpeedEnum,
                    StageCoordinateSystemEnum.Bright,
                    Cache.LaserLightInformation) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            var darkFieldMachinePosition = StageViewModel.DarkFieldToMachinePosition(darkFieldResultPosition);
            SynchronizationContextProvider.Send(() =>
            {
                EcsPoints = [];
                FocusShiftDtoItems.Clear();
            });

            // 获得NSC模式下的当前的ECS、NSC值
            StageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(darkFieldResultPosition, Cache.CalChipSiteModelEnum);
            var isAutoFocus = AfViewModel.SetDarkFieldAutoFocus(settingDarkFieldAutoFocusParam, Cache.OpticsMagTypeEnum, Cache.CalChipSiteModelEnum);
            if (isAutoFocus == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Dark Field Auto Focus Enable Is Closed!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            AfViewModel.ToggleDarkFieldEnable(true);
            var nscBuffers = AfViewModel.GetSensorNscTraceBufferList(TimeSpan.FromSeconds(2));
            var autoFocusNsc = nscBuffers.Average();
            var autoFocusEcs = AfViewModel.GetSensorAverageEcsValue();
            Logger.LogHtmlInformation("Auto Focus", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                DarkFieldFindPosition = darkFieldResultPosition,
                darkFieldMachinePosition,
                autoFocusEcs,
                autoFocusNsc,
                NscTraceBuffers = new HtmlPlot2DLinesChart([
                    ("Time-Nsc", nscBuffers.ToPoints())
                ], "NscTraceBuffers")
            }), HtmlLogUniqueId.LoggingHtml());

            #endregion Dark Field

            #region Ecs Offset

            Logger.LogHtmlInformation("4. Ecs Offset Calibration", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            // 初始化
            var focusShiftList = new List<FocusShiftDto>();
            var minEcs = autoFocusEcs - findFocusMin;
            var maxEcs = autoFocusEcs + findFocusMax;
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
            {
                autoFocusEcs,
                FindFocusMin = minEcs,
                FindFocusMax = maxEcs,
                findFocusInterval,
                FindPosition = darkFieldResultPosition,
                ImageFileDirectory
            }), HtmlLogUniqueId.LoggingHtml());
            foreach (var (index, ecsValueTemp) in Enumerable.Range(0, Convert.ToInt32((findFocusMax + findFocusMin) / findFocusInterval) + 1)
                         .Select(x => Math.Min(minEcs + x * findFocusInterval, maxEcs))
                         .Select((d, i) => (i, d)))
            {
                focusShiftList.Add(new FocusShiftDto
                {
                    Index = index,
                    BrightFieldFindPosition = brightFieldMachinePosition,
                    DarkFieldFindPosition = darkFieldMachinePosition,
                    BrightFieldEcsValue = afEcs,
                    DarkFieldEcsValue = ecsValueTemp,
                    NscValue = 0,
                    DarkFieldQuality = 0,
                    DarkFieldImageFilePath = ImageFileDirectory
                });
            }

            // Quality
            var listDownResult = new List<bool>();
            double? downQualityValue = null;
            foreach (var focusShiftDtoItem in focusShiftList.OrderBy(t => t.Index))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var focusShiftDto = focusShiftDtoItem.Clone();
                if (GetQuality(ref focusShiftDto) == false) return false;
                SynchronizationContextProvider.Send(() =>
                {
                    FocusShiftDtoItems.Add(focusShiftDto);
                    EcsPoints = [.. EcsPoints, new Point(focusShiftDto.DarkFieldEcsValue, focusShiftDto.DarkFieldQuality)];
                });

                if (downQualityValue is not null) listDownResult.Add(downQualityValue.Value < focusShiftDto.DarkFieldQuality);
                downQualityValue = focusShiftDto.DarkFieldQuality;
                if (listDownResult.HasConsecutiveEqual(10, false)) break; // 连续10个下降说明已经到了最低点
            }

            ResultFocusShiftDto = FocusShiftDtoItems.Select(s => s.Clone()).Maxima(s => s.DarkFieldQuality).Single();
            ResultFocusShiftDto.EcsOffset = ResultFocusShiftDto.DarkFieldEcsValue - ResultFocusShiftDto.BrightFieldEcsValue;
            Logger.LogHtmlInformation("Ecs Offset Result", HtmlHeaderLevelEnum.Header5, new HtmlQuote(new
            {
                ResultFocusShiftDto.DarkFieldEcsValue,
                ResultFocusShiftDto.BrightFieldEcsValue,
                ResultFocusShiftDto.DarkFieldQuality,
                ResultFocusShiftDto.EcsOffset,
                HtmlTab = new HtmlTab(new
                {
                    ResultImage = new HtmlImage(ResultFocusShiftDto.DarkFieldImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                }),
                EcsQuality = new HtmlPlot2DLinesChart([
                    ("Ecs-Quality", FocusShiftDtoItems.Select(t => new Point(t.DarkFieldEcsValue, t.DarkFieldQuality)).ToArray())
                ], "EcsQuality")
            }), HtmlLogUniqueId.LoggingHtml());

            #endregion Ecs Offset

            #region BF To DF Machine Offset

            Logger.LogHtmlInformation("5.BF To DF Machine Offset Calibration", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            // 监控NSC值
            StageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(darkFieldResultPosition, Cache.CalChipSiteModelEnum);
            AfViewModel.ToggleBrightFieldEnable(false);
            AfViewModel.SetSensorEcsValue(ResultFocusShiftDto.DarkFieldEcsValue);
            await Task.Delay(5000, cancellationToken);
            nscBuffers = AfViewModel.GetSensorNscTraceBufferList(TimeSpan.FromSeconds(2));
            ResultFocusShiftDto.NscValue = nscBuffers.Average();
            ResultFocusShiftDto.AutoFocusEcs = autoFocusEcs;
            ResultFocusShiftDto.AutoFocusNsc = autoFocusNsc;

            // 获得照明焦点偏移量
            if (LaserViewModel.TryGetMatchPositionByScanImage(
                    Cache.AlgorithmTemplateTypeEnum,
                    Cache.CalChipSiteModelEnum,
                    8,
                    darkFieldResultPosition,
                    Cache.DarkFiledTemplateFilePath,
                    ResultFocusShiftDto.DarkFieldImageFilePath,
                    HtmlLogUniqueId,
                    string.Empty,
                    string.Empty,
                    Cache.CIBConfiguration,
                    out var resultPosition,
                    out _,
                    out _,
                    out _,
                    true,
                    800,
                    Cache.OpticsMagTypeEnum,
                    Cache.StageSpeedEnum,
                    StageCoordinateSystemEnum.Bright,
                    Cache.LaserLightInformation,
                    isAutoFocus: false) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Dark Field Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            ResultFocusShiftDto.DarkFieldFindPosition = StageViewModel.DarkFieldToMachinePosition(resultPosition);
            Logger.LogHtmlInformation("Position Offset", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                ResultFocusShiftDto.BrightFieldFindPosition,
                ResultFocusShiftDto.DarkFieldFindPosition,
                BrightFiedlToDarkFieldOffset = ResultFocusShiftDto.BrightFieldToDarkFieldOffset,
                RealNscEcs = ResultFocusShiftDto.DarkFieldEcsValue,
                RealNsc = ResultFocusShiftDto.NscValue,
                NscTraceBuffers = new HtmlPlot2DLinesChart([
                    ("Time-Nsc", nscBuffers.ToPoints())
                ], "NscTraceBuffers")
            }), HtmlLogUniqueId.LoggingHtml());

            #endregion BF To DF Machine Offset

            ResultFocusShiftDto.AfMotorOffset = ResultFocusShiftDto.FocusShiftOffset / Cache.AfEcsRelation;
            ResultFocusShiftDto.SettingDarkFieldAutoFocusParam = settingDarkFieldAutoFocusParam.Clone();
            if (Math.Abs(ResultFocusShiftDto.FocusShiftOffset) > Cache.AfMotorReviseThreshold)
            {
                ResultFocusShiftDto.SettingDarkFieldAutoFocusParam.DswMotorValue -= ResultFocusShiftDto.AfMotorOffset;
                AfViewModel.SetDarkFieldAutoFocusMotorAbsoluteValue(ResultFocusShiftDto.SettingDarkFieldAutoFocusParam.DswMotorValue);
                StageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(darkFieldResultPosition, Cache.CalChipSiteModelEnum);
                AfViewModel.ToggleDarkFieldEnable(true);
                autoFocusEcs = AfViewModel.GetSensorAverageEcsValue();
                ResultFocusShiftDto.SettingDarkFieldAutoFocusParam.DswEcsValue = autoFocusEcs;
            }

            var (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();
            Logger.LogHtmlInformation("Result Ok", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                xAxisTemperature,
                yAxisTemperature,
                reviewCamTemperature,
                cibTemperature,
                Cache.AfEcsRelation,
                Cache.AfMotorReviseThreshold,
                ResultFocusShiftDto.BrightFieldFindPosition,
                ResultFocusShiftDto.DarkFieldFindPosition,
                AfEcs = ResultFocusShiftDto.BrightFieldEcsValue,
                ResultFocusShiftDto.AutoFocusEcs,
                ResultFocusShiftDto.AutoFocusNsc,
                RealEcs = ResultFocusShiftDto.DarkFieldEcsValue,
                RealEcsNsc = ResultFocusShiftDto.NscValue,
                BrightFiedlToDarkFieldOffset = ResultFocusShiftDto.BrightFieldToDarkFieldOffset,
                ResultFocusShiftDto.EcsOffset,
                ResultFocusShiftDto.FocusShiftOffset,
                ResultFocusShiftDto.AfMotorOffset,
                RevieseDswEcs = ResultFocusShiftDto.SettingDarkFieldAutoFocusParam.DswEcsValue,
                RevieseDswMotor = ResultFocusShiftDto.SettingDarkFieldAutoFocusParam.DswMotorValue,
                ResultFocusShiftDto.SettingDarkFieldAutoFocusParam.IsEnableDsw,
                resultAfMotor = ResultFocusShiftDto.SettingDarkFieldAutoFocusParam.DswMotorValue,
                RealNscTraceBuffers = new HtmlPlot2DLinesChart([
                    ("Time-Nsc", nscBuffers.ToPoints())
                ], "NscTraceBuffers")
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Focus Shift Calibration Failed!");
            DialogWindowProvider.ShowDialog($"Focus Shift Calibration Failed: {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Error);
            return false;
        }
    }

    public bool GetQuality(ref FocusShiftDto focusShiftDto)
    {
        try
        {
            Logger.LogHtmlInformation($"Get Quality,Ecs:{focusShiftDto.DarkFieldEcsValue}", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());
            // 防止移动后明场af模式打开
            AfViewModel.ToggleBrightFieldEnable(false);

            if (focusShiftDto.Index == 0) Thread.Sleep(3000);
            AfViewModel.SetSensorEcsValue(focusShiftDto.DarkFieldEcsValue);
            Thread.Sleep(1000);
            var darkFieldPosition = StageViewModel.MachineToDarkFieldPosition(focusShiftDto.DarkFieldFindPosition);
            using var darkFieldImageDto = LaserViewModel.GetDarkFieldLineScanImage(
                Cache.CalChipSiteModelEnum,
                darkFieldPosition,
                (false, Cache.LaserLightInformation),
                true,
                Cache.CIBConfiguration,
                800,
                Cache.OpticsMagTypeEnum,
                StageSpeedEnum.Low,
                8,
                3,
                StageCoordinateSystemEnum.Dark,
                isAutoFocus: false);

            using var scaleImage = darkFieldImageDto.Image.ScaleImageTo8Bit();
            var xQuality = CalibrationAlgorithmService.GetDarkFieldQuality(scaleImage);
            //var path = $"{ImageFileDirectory}\\ECS({focusShiftDto.DarkFieldEcsValue})_Guid({HtmlLogUniqueId}).hobj";
            //HOperatorSet.WriteObject(scaleImage, path);
            var qualityX = xQuality;
            focusShiftDto.DarkFieldImageFilePath =
                $"{ImageFileDirectory}\\ECS({focusShiftDto.DarkFieldEcsValue})_Guid({HtmlLogUniqueId}).jpg";
            focusShiftDto.DarkFieldOriginImageFilePath = CalibrationConstantsHelper.ImagePathToRawImagePath(focusShiftDto.DarkFieldImageFilePath);
            focusShiftDto.DarkFieldQuality = qualityX;

            FileHelper.Save(darkFieldImageDto.Bytes, focusShiftDto.DarkFieldOriginImageFilePath);
            darkFieldImageDto.Image.Save(focusShiftDto.DarkFieldImageFilePath);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
            {
                focusShiftDto.DarkFieldEcsValue,
                ImageQuality = focusShiftDto.DarkFieldQuality,
                HtmlTab = new HtmlTab(new
                {
                    Image = new HtmlImage(focusShiftDto.DarkFieldImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
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

    private bool Save(FocusShiftDto itemDto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        itemDto.OpticsMagTypeEnum = Cache.OpticsMagTypeEnum;
        Calibrations =
        [
            .. Calibrations
                .Where(t => t.OpticsMagTypeEnum != itemDto.OpticsMagTypeEnum),
            itemDto.Clone()
        ];

        CacheProvider.SetArray(Calibrations, cancellationToken);
        CacheProvider.Set(Cache, cancellationToken);
    }) && EnableDependedCalibrationItems(cancellationToken);

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependLaserFocusShiftCalibrations(false, cancellationToken, out var errorMsg) == false)
        {
            Logger.LogError("Toggle {@Name} Enable Status Failed!", errorMsg);
            return false;
        }

        return true;
    }

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(FocusShiftDtoItems.Clear);
    }

    #endregion 校准
}