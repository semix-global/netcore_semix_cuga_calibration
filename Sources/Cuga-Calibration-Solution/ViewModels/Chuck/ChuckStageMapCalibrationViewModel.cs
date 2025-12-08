using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.AOD.AODAlignment;
using Core.Models.Models.AOD.AODDelay;
using Core.Models.Models.Chuck.CenterAndTheta;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.LineCentricity;
using Core.Models.Models.Laser.OpticalPowerMeter;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Laser.XPixelSize;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.NoSQL.DB.Providers.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;

namespace CugaCalibration.ViewModels.Chuck;

[IOCAppService(ServiceType = typeof(ChuckStageMapCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckStageMapCalibrationViewModel(
    ApplicationCookie applicationCookie,
    AlignmentWindowBrightFieldViewModel alignmentWindowBrightFieldViewModel,
    AlignmentWindowDarkFieldViewModel alignmentWindowDarkFieldViewModel,
    CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel) : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => $"{EnumHelper.ToDescriptionString(Cache.HighMicroscopeLensInformation.LensName)}-{Cache.ProductivityInformation}";

    public override string CalibrateFileName => $"{EnumHelper.ToDescriptionString(Cache.HighMicroscopeLensInformation.LensName)}-{Cache.ProductivityInformation}";

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Config" },
        new() { StepName = "BF P5", StepIndex = 1 },
        new() { StepName = "BF Find Start Point", StepIndex = 2 },
        new() { StepName = "BF Param", StepIndex = 3 },
        new() { StepName = "BF Stage Map", StepIndex = 4 },
        new() { StepName = "DF P5", StepIndex = 5 },
        new() { StepName = "DF Find Start Point", StepIndex = 6 },
        new() { StepName = "DF Param", StepIndex = 7 },
        new() { StepName = "DF Stage Map", StepIndex = 8 },
        new() { StepName = "Expand To BF", StepIndex = 9 }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private bool _isDarkFieldAlignment;

    [ObservableProperty]
    private ChuckStageMapDto _resultChuckStageMapDto = new();

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ChuckStageMapDto? _reviewDto;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private ChuckStageMapCache _cache = new();

    [ObservableProperty]
    private ChuckStageMapDto _calibration = new();

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    [ObservableProperty]
    private AlignmentCacheDarkField _alignmentCacheDarkField = new();

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

    [ObservableProperty]
    private ChuckCenterAndThetaItemDto _chuckCenter = new();

    [ObservableProperty]
    private ChuckGlobalScaleErrorDto _chuckGlobalScaleError = new();

    [ObservableProperty]
    private LaserPixelSizeItemDto[] _laserPixelSizeItems = [];

    [ObservableProperty]
    private LaserLineCentricityItemDto[] _laserLineCentricityItems = [];

    [ObservableProperty]
    private LaserXPixelSizeItemDto[] _laserXPixelSizeItems = [];

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

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckGantryDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckGlobalScaleErrorDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckCenterAndThetaItemDto>(out var chuckCenter, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        ChuckCenter = chuckCenter;

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckPrealignerObjDto>(out _, out errorMessage) == false)
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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserOpticalPowerMeterDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<AODDelayDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<AODAlignmentDto>(out _, out errorMessage) == false)
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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserXPixelSizeItemDto>(out var laserXPixelSizeItems, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        LaserXPixelSizeItems = laserXPixelSizeItems;

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserPixelSizeItemDto>(out var laserPixelSizeItems, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        LaserPixelSizeItems = laserPixelSizeItems;

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserLineCentricityItemDto>(out var laserLineCentricityItems, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        LaserLineCentricityItems = laserLineCentricityItems;

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<ChuckStageMapCache>();
        Calibration = CacheProvider.GetOrDefault<ChuckStageMapDto>();
        AlignmentCacheDarkField = RecipeCacheProvider.GetOrDefault<AlignmentCacheDarkField>();
        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();
        Cache.IsDarkField = false;

        Cache.ProductivityInformation = applicationCookie.NILowProductivityInformation.Clone();

        if (Cache.HighMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.HighMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.HighMicroscopeLensInformation.Clone();

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        StageViewModel.SetEnableStageMap(false);

        return !IsRecipeCalibrate || CalibrationRecipeService.GetCorrectWaferMapByOffset(true);
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewDto = Calibration.Clone();
        OnPropertyChanged(nameof(ReviewDto.VerifyDarkFieldStageMap));
        OnPropertyChanged(nameof(ReviewDto.VerifyBrightFieldStageMap));

        return ReviewDto.IsCalibrated;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 1:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);
                return true;

            case 2 or 6:
                Cache.GetParam();
                return true;

            case 4:
                Cache.SetParam();
                Cache.IsDarkField = true;

                ResultChuckStageMapDto.IsCalibrationBrightField = true;
                ResultChuckStageMapDto.VerifyBrightFieldStageMap = ResultChuckStageMapDto.CalibrationBrightFieldStageMap.Clone();
                if (Save(ResultChuckStageMapDto, cancellationToken) == false)
                {
                    ResultChuckStageMapDto.IsCalibrationBrightField = false;
                    Logger.LogError("{@Name} Error: Save Bright Field Cache Failed!", Name);
                    return false;
                }

                DialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;
                return true;

            case 5:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                StageViewModel.SetMachineAbsoluteStageXy(Cache.FirstStageMapPosition);
                return true;

            case 8:
                if (ResultChuckStageMapDto.IsCalibrationBrightField == false)
                {
                    Logger.LogError("{@Name} Error:Please Calibration Bright Field Calibration !", Name);
                    return false;
                }

                return true;

            case 9:
                ResultChuckStageMapDto.IsCalibrated = true;
                ResultChuckStageMapDto.VerifyDarkFieldStageMap = ResultChuckStageMapDto.CalibrationDarkFieldStageMap.Clone();
                Cache.SetParam();
                if (Save(ResultChuckStageMapDto, cancellationToken) == false)
                {
                    ResultChuckStageMapDto.IsCalibrated = false;
                    Logger.LogError("{@Name} Error: Save Failed!", Name);
                    return false;
                }

                IsCalibrated = true;

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
            case 3 or 4:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                StageViewModel.SetMachineAbsoluteStageXy(Cache.BrightFieldFirstStageMapPosition);
                return true;

            case 5:
                Cache.IsDarkField = false;
                IsDarkFieldAlignment = false;
                ResultChuckStageMapDto.IsCalibrationBrightField = false;
                Cache.GetParam();

                return true;

            case 7 or 8:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                var position = StageViewModel.MachineToDarkFieldPosition(Cache.DarkFieldFirstStageMapPosition);
                StageViewModel.SetBrightFieldAbsoluteStageXy(position);
                return true;

            default:
                return true;
        }
    }

    #endregion 控制校准业务

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

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            AlignmentResultDto alignmentResultDto = new();

            if (IsDarkFieldAlignment)
            {
                if (AlignmentCacheDarkField.IsOk)
                {
                    alignmentResultDto = StageViewModel.AlignmentDarkField(
                        AlignmentCacheDarkField.LowSite1,
                        AlignmentCacheDarkField.LowSite2,
                        AlignmentCacheDarkField.HighSite1,
                        AlignmentCacheDarkField.HighSite2,
                        AlignmentCacheDarkField.HighDarkFieldOpticsMagTypeEnum,
                        AlignmentCacheDarkField.HighDarkFieldStageSpeedEnum,
                        AlignmentCacheDarkField.LowMag,
                        AlignmentCacheDarkField.AlgorithmWaferTypeEnum);
                    return true;
                }

                var showDialog = WindowManagerService.ShowDialog(alignmentWindowDarkFieldViewModel);

                if (showDialog == false)
                {
                    DialogWindowProvider.ShowDialog("Alignment Setting is Empty", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                AlignmentCacheDarkField = alignmentWindowDarkFieldViewModel.Cache;
                StageViewModel.SetBrightFieldAbsoluteStageXy(AlignmentCacheDarkField.LowSite1.Location);
            }
            else
            {
                if (AlignmentCacheBrightField.IsOk)
                {
                    alignmentResultDto = StageViewModel.Alignment(
                        AlignmentCacheBrightField.LowSite1,
                        AlignmentCacheBrightField.LowSite2,
                        AlignmentCacheBrightField.HighSite1,
                        AlignmentCacheBrightField.HighSite2,
                        AlignmentCacheBrightField.LowMag,
                        AlignmentCacheBrightField.HighMag,
                        AlignmentCacheBrightField.AlgorithmWaferTypeEnum);
                    return true;
                }

                var showDialog = WindowManagerService.ShowDialog(alignmentWindowBrightFieldViewModel);

                if (showDialog == false)
                {
                    DialogWindowProvider.ShowDialog("Alignment Setting is Empty", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                AlignmentCacheBrightField = alignmentWindowBrightFieldViewModel.Cache;
                StageViewModel.SetBrightFieldAbsoluteStageXy(AlignmentCacheBrightField.LowSite1.Location);
            }

            Cache.P5Angle = alignmentResultDto.Degrees;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.P5Angle,
                LensName = Cache.HighMicroscopeLensInformation.LensName
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            if (Cache.IsDarkField == false)
            {
                if (await BrightFieldStep1ActionAsync() == false) return false;
            }
            else
            {
                if (await DarkFieldStep1ActionAsync() == false) return false;
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.AlgorithmTemplateTypeEnum,
                Cache.FirstStageMapPosition,
                HtmlTab = new HtmlTab(new
                {
                    TemplateImage = new HtmlImage(Cache.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    private async Task<bool> BrightFieldStep1ActionAsync()
    {
        if (IsRecipeCalibrate)
        {
            MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
            if (await AutomationRecipeInformationAsync("0") == false) return false;
            var findPosition = StageViewModel.MachineToBrightFieldPosition(Cache.BrightFieldFirstStageMapPosition);
            if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, findPosition, Cache.HighMicroscopeLensInformation, Cache.TemplateFilePath, TemplateFileDirectory, HtmlLogUniqueId, Name, string.Empty,
                    out _, out _, out _, out _, out _) == false) return false;
        }
        else
        {
            Cache.TemplateFilePath = Cache.BrightFieldTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.HighMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            var generateTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.TemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
            if (generateTemplate == false)
            {
                DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            Cache.TemplateImageFilePath = Cache.BrightFieldTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.TemplateFilePath);
        }

        var centerPosition = StageViewModel.GetMachineStagePosition();
        Cache.FirstStageMapPosition = Cache.BrightFieldFirstStageMapPosition = HostEnvironment.IsDevelopment()
            ? ChuckCenter.NewBFCenterStagePosition
            : centerPosition;

        return true;
    }

    private async Task<bool> DarkFieldStep1ActionAsync()
    {
        if (IsRecipeCalibrate)
        {
            MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
            if (await AutomationRecipeInformationAsync("1") == false) return false;
            var findPosition = StageViewModel.MachineToDarkFieldPosition(Cache.DarkFieldFirstStageMapPosition);
            if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, findPosition, Cache.HighMicroscopeLensInformation, Cache.TemplateFilePath, TemplateFileDirectory, HtmlLogUniqueId, Name, string.Empty,
                    out _, out _, out _, out _, out _) == false) return false;
        }

        var brightFieldPosition = StageViewModel.GetBrightFieldStagePosition();
        var centerPosition = StageViewModel.DarkFieldToMachinePosition(brightFieldPosition);

        var laserLineCentricityItemDto = LaserLineCentricityItems.Single(t => t.PmtId == CalibrationConstantsHelper.MainPmtId
                                                                              && t.ProductivityInformation == Cache.ProductivityInformation);


        var darkFieldImageDto = LaserViewModel.GetDarkFieldLineScanImage(
            CalChipSiteModelEnum.ChuckModel,
            brightFieldPosition,
            (false, CalibrationSetting.SettingCommonParam.MainLaserLightInformation),
            false,
            Cache.CIBConfiguration,
            Cache.ProductivityInformation,
            Cache.OpticsIlluminationModeEnum,
            Cache.XWidthPixel,
            stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);
        var detectImageDirectory = ImageFileDirectory;
        using var image = darkFieldImageDto;

        Cache.TemplateFilePath = Cache.DarkFieldTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.HighMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
        if (Cache.AlgorithmTemplateTypeEnum == AlgorithmTemplateTypeEnum.Projection)
        {
            if (ReviewViewModel.TryGenerateProjectionTemplate(darkFieldImageDto.Image, Cache.TemplateFilePath) == false)
            {
                DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }
        }
        else
        {
            var filePath = $"{detectImageDirectory}\\Guid({HtmlLogUniqueId}_{Guid.NewGuid()}).jpg";
            darkFieldImageDto.Image.Save(filePath);
            createDarkImageTemplateWindowViewModel.ImageFilePath = filePath;
            createDarkImageTemplateWindowViewModel.TemplateFilePath = Cache.TemplateFilePath;

            var showDialog = WindowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel);

            if (showDialog == false)
            {
                DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }
        }

        Cache.TemplateImageFilePath = Cache.DarkFieldTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.TemplateFilePath);
        Cache.FirstStageMapPosition = Cache.DarkFieldFirstStageMapPosition = HostEnvironment.IsDevelopment()
            ? laserLineCentricityItemDto.DarkMachineCenterPosition
            : centerPosition;
        return true;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var (isSuccess, errorMessage) = Cache.Step2Verify();
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Cache varify error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            if (Cache is { RowNumber: <= 0, ColumnNumber: <= 0, ColumnCellWidth: <= 0, RowCellHeight: <= 0 })
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{nameof(Cache.RowNumber)} > 0 and {nameof(Cache.ColumnNumber)} > 0 and {nameof(Cache.ColumnCellWidth)} > 0 and {nameof(Cache.RowCellHeight)} > 0"),
                    HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            if (Cache.IsDarkField == false)
                BrightFieldStep2Action();
            else
                DarkFieldStep2Action();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.AlgorithmTemplateTypeEnum,
                Cache.RowNumber,
                Cache.ColumnNumber,
                Cache.ColumnCellWidth,
                Cache.RowCellHeight,
                Cache.WaferDiameter,
                Cache.FirstStageMapPosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    private void BrightFieldStep2Action()
    {
        StageViewModel.SetMachineAbsoluteStageXy(Cache.BrightFieldFirstStageMapPosition);

        ResultChuckStageMapDto.CalibrationBrightFieldStageMap = new StageMapDto(Cache.RowNumber, Cache.ColumnNumber, Cache.RowCellHeight, Cache.ColumnCellWidth);
        ResultChuckStageMapDto.CalibrationBrightFieldStageMap.GenerateByCenterPosition(
            HostEnvironment.IsDevelopment()
                ? ChuckCenter.NewBFCenterStagePosition
                : Cache.BrightFieldFirstStageMapPosition,
            ChuckCenter.NewBFCenterStagePosition,
            Cache.WaferDiameter);

        OnPropertyChanged(nameof(ResultChuckStageMapDto.CalibrationBrightFieldStageMap));
    }

    private void DarkFieldStep2Action()
    {
        var position = StageViewModel.MachineToDarkFieldPosition(Cache.DarkFieldFirstStageMapPosition);
        StageViewModel.SetBrightFieldAbsoluteStageXy(position);

        var laserLineCentricityItemDto = LaserLineCentricityItems.Single(t => t.PmtId == CalibrationConstantsHelper.MainPmtId
                                                                              && t.ProductivityInformation == Cache.ProductivityInformation);

        ResultChuckStageMapDto.CalibrationDarkFieldStageMap = new StageMapDto(Cache.RowNumber, Cache.ColumnNumber, Cache.RowCellHeight, Cache.ColumnCellWidth);
        ResultChuckStageMapDto.CalibrationDarkFieldStageMap.GenerateByCenterPosition(
            HostEnvironment.IsDevelopment()
                ? laserLineCentricityItemDto.DarkMachineCenterPosition
                : Cache.DarkFieldFirstStageMapPosition,
            laserLineCentricityItemDto.DarkMachineCenterPosition,
            Cache.WaferDiameter);

        OnPropertyChanged(nameof(ResultChuckStageMapDto.CalibrationDarkFieldStageMap));
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var (isSuccess, errorMessage) = Cache.Step3Verify();
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Cache varify error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var opticsMode = Cache.IsDarkField ? "DarkField" : "BrightField";
            var (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                xAxisTemperature,
                yAxisTemperature,
                reviewCamTemperature,
                cibTemperature,
                Cache.P5Angle,
                opticsMode,
                LensName = Cache.HighMicroscopeLensInformation.LensName,
                Cache.CalculateContainRowMinCount,
                Cache.CalculateContainColumnMinCount,
                Cache.CalibrationAlignmentThreshold,
                Cache.CalibrationGantryThreshold,
                Cache.CalibrationScaleThreshold,
                Cache.FirstStageMapPosition,
                ImageFileDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            var detectImageDirectory = ImageFileDirectory;

            StageViewModel.SetEnableStageMap(false);

            Logger.LogHtmlInformation("Get Stage Map", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            if (Cache.IsDarkField == false)
            {
                ResultChuckStageMapDto.IsCalibrationBrightField = false;
                ResultChuckStageMapDto.CalibrationBrightFieldStageMap.Reset();
                OnPropertyChanged(nameof(ResultChuckStageMapDto.CalibrationBrightFieldStageMap));
                BrightFieldGetStageMap(ResultChuckStageMapDto.CalibrationBrightFieldStageMap, detectImageDirectory, () => OnPropertyChanged(nameof(ResultChuckStageMapDto.CalibrationBrightFieldStageMap)), cancellationToken);
            }
            else
            {
                ResultChuckStageMapDto.CalibrationDarkFieldStageMap.Reset();
                //var (ecs, height) = settingWindowViewModel.HighMagSettingDarkFieldAutoFocusViewModel.ChuckAfAutoRtfc(StageViewModel.MachineToDarkFieldPosition(Cache.FirstStageMapPosition), HtmlLogUniqueId);
                //settingWindowViewModel.SaveSetting();
                OnPropertyChanged(nameof(ResultChuckStageMapDto.CalibrationDarkFieldStageMap));
                //Logger.LogHtmlInformation("Rtfc Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                //{
                //    RtfcPosition = Cache.FirstStageMapPosition,
                //    RtfcEcs = ecs,
                //    RtfcHeight = height,
                //}), HtmlLogUniqueId.LoggingHtml());
                DarkFieldGetStageMap(ResultChuckStageMapDto.CalibrationDarkFieldStageMap, detectImageDirectory, () => OnPropertyChanged(nameof(ResultChuckStageMapDto.CalibrationDarkFieldStageMap)), cancellationToken);
            }

            var calibrationStageMap = Cache.IsDarkField == false
                ? ResultChuckStageMapDto.CalibrationBrightFieldStageMap
                : ResultChuckStageMapDto.CalibrationDarkFieldStageMap;

            var middleFileDateTimeFormat = DateTimeHelper.DateTime2String(DateTime.Now, Constants.MiddleFileDateTimeFormat);
            calibrationStageMap.IdealCsvFilePath = $"{CsvFileDirectory}\\Calibration\\{opticsMode}\\{middleFileDateTimeFormat}\\Ideal_Guid({HtmlLogUniqueId}).csv";
            calibrationStageMap.RealCsvFilePath = $"{CsvFileDirectory}\\Calibration\\{opticsMode}\\{middleFileDateTimeFormat}\\Real_Guid({HtmlLogUniqueId}).csv";
            calibrationStageMap.RealIsInWaferOkCsvFilePath = $"{CsvFileDirectory}\\Calibration\\{opticsMode}\\{middleFileDateTimeFormat}\\RealIsInWafer_Guid({HtmlLogUniqueId}).csv";
            calibrationStageMap.RealIsMatchOkCsvFilePath = $"{CsvFileDirectory}\\Calibration\\{opticsMode}\\{middleFileDateTimeFormat}\\RealIsMatchOk_Guid({HtmlLogUniqueId}).csv";
            calibrationStageMap.ErrorCsvFilePath = $"{CsvFileDirectory}\\Calibration\\{opticsMode}\\{middleFileDateTimeFormat}\\Error_Guid({HtmlLogUniqueId}).csv";
            calibrationStageMap.SaveIdealCsv(calibrationStageMap.IdealCsvFilePath);
            calibrationStageMap.SaveRealCsv(calibrationStageMap.RealCsvFilePath);
            calibrationStageMap.SaveIsInWaferOkCsv(calibrationStageMap.RealIsInWaferOkCsvFilePath);
            calibrationStageMap.SaveIsMatchOkCsv(calibrationStageMap.RealIsMatchOkCsvFilePath);

            var tryCalculateStageMapError = CalibrationAlgorithmService.CalculateChuckStageMapError(
                calibrationStageMap,
                Cache.IsDarkField,
                HtmlLogUniqueId,
                Cache.CalculateContainRowMinCount,
                Cache.CalculateContainColumnMinCount,
                Cache.CalibrationAlignmentThreshold,
                Cache.CalibrationGantryThreshold,
                Cache.CalibrationScaleThreshold,
                Cache.WaferDiameter);

            calibrationStageMap.SaveErrorCsv(calibrationStageMap.ErrorCsvFilePath);

            (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();
            if (tryCalculateStageMapError == false && HostEnvironment.IsDevelopment() == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Error = "Calculate Stage Map Error Failed!",
                    xAxisTemperature,
                    yAxisTemperature,
                    reviewCamTemperature,
                    cibTemperature,
                    LensName = Cache.HighMicroscopeLensInformation.LensName,
                    Cache.AlgorithmTemplateTypeEnum,
                    calibrationStageMap.IdealCsvFilePath,
                    calibrationStageMap.RealCsvFilePath,
                    calibrationStageMap.RealIsMatchOkCsvFilePath,
                    calibrationStageMap.ErrorCsvFilePath
                }), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                xAxisTemperature,
                yAxisTemperature,
                cibTemperature,
                opticsMode,
                LensName = Cache.HighMicroscopeLensInformation.LensName,
                Cache.AlgorithmTemplateTypeEnum,
                calibrationStageMap.IdealCsvFilePath,
                calibrationStageMap.RealCsvFilePath,
                calibrationStageMap.RealIsMatchOkCsvFilePath,
                calibrationStageMap.ErrorCsvFilePath
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step4CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var darkFieldStageMap = ResultChuckStageMapDto.CalibrationDarkFieldStageMap;
            var brightFieldStageMapDto = ResultChuckStageMapDto.CalibrationBrightFieldStageMap;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                darkFieldStageMapRowNumber = darkFieldStageMap.RowNumber,
                darkFieldStageMapColumnNumber = darkFieldStageMap.ColumnNumber,
                darkFieldStageMapRowCellHeight = darkFieldStageMap.RowCellHeight,
                darkFieldStageMapColumnCellWidth = darkFieldStageMap.ColumnCellWidth,
                brightFieldStageMapDtoRowNumber = brightFieldStageMapDto.RowNumber,
                brightFieldStageMapDtoColumnNumber = brightFieldStageMapDto.ColumnNumber,
                brightFieldStageMapDtoRowCellHeight = brightFieldStageMapDto.RowCellHeight,
                brightFieldStageMapDtoColumnCellWidth = brightFieldStageMapDto.ColumnCellWidth
            }), HtmlLogUniqueId.LoggingHtml());

            var expandStageMapDto = CalibrationAlgorithmService.ExpandStageMapDto(darkFieldStageMap, brightFieldStageMapDto, HtmlLogUniqueId);
            ResultChuckStageMapDto.ExpandStageMapDto = expandStageMapDto;
            OnPropertyChanged(nameof(ResultChuckStageMapDto.ExpandStageMapDto));

            var middleFileDateTimeFormat = DateTimeHelper.DateTime2String(DateTime.Now, Constants.MiddleFileDateTimeFormat);
            ResultChuckStageMapDto.ExpandStageMapDto.IdealCsvFilePath = $"{CsvFileDirectory}\\Expand\\{middleFileDateTimeFormat}\\Ideal_Guid({HtmlLogUniqueId}).csv";
            ResultChuckStageMapDto.ExpandStageMapDto.RealCsvFilePath = $"{CsvFileDirectory}\\Expand\\{middleFileDateTimeFormat}\\Real_Guid({HtmlLogUniqueId}).csv";
            ResultChuckStageMapDto.ExpandStageMapDto.RealIsInWaferOkCsvFilePath = $"{CsvFileDirectory}\\Expand\\{middleFileDateTimeFormat}\\RealIsInWafer_Guid({HtmlLogUniqueId}).csv";
            ResultChuckStageMapDto.ExpandStageMapDto.RealIsMatchOkCsvFilePath = $"{CsvFileDirectory}\\Expand\\{middleFileDateTimeFormat}\\RealIsMatchOk_Guid({HtmlLogUniqueId}).csv";
            ResultChuckStageMapDto.ExpandStageMapDto.ErrorCsvFilePath = $"{CsvFileDirectory}\\Expand\\{middleFileDateTimeFormat}\\Error_Guid({HtmlLogUniqueId}).csv";
            ResultChuckStageMapDto.ExpandStageMapDto.SaveIdealCsv(ResultChuckStageMapDto.ExpandStageMapDto.IdealCsvFilePath);
            ResultChuckStageMapDto.ExpandStageMapDto.SaveRealCsv(ResultChuckStageMapDto.ExpandStageMapDto.RealCsvFilePath);
            ResultChuckStageMapDto.ExpandStageMapDto.SaveIsInWaferOkCsv(ResultChuckStageMapDto.ExpandStageMapDto.RealIsInWaferOkCsvFilePath);
            ResultChuckStageMapDto.ExpandStageMapDto.SaveIsMatchOkCsv(ResultChuckStageMapDto.ExpandStageMapDto.RealIsMatchOkCsvFilePath);
            ResultChuckStageMapDto.ExpandStageMapDto.SaveErrorCsv(ResultChuckStageMapDto.ExpandStageMapDto.ErrorCsvFilePath);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                expandStageMapDtoRowNumber = expandStageMapDto.RowNumber,
                expandStageMapDtoColumnNumber = expandStageMapDto.ColumnNumber,
                expandStageMapDtoRowCellHeight = expandStageMapDto.RowCellHeight,
                expandStageMapDtoColumnCellWidth = expandStageMapDto.ColumnCellWidth,
                ResultChuckStageMapDto.ExpandStageMapDto.IdealCsvFilePath,
                ResultChuckStageMapDto.ExpandStageMapDto.RealCsvFilePath,
                ResultChuckStageMapDto.ExpandStageMapDto.RealIsInWaferOkCsvFilePath,
                ResultChuckStageMapDto.ExpandStageMapDto.RealIsMatchOkCsvFilePath,
                ResultChuckStageMapDto.ExpandStageMapDto.ErrorCsvFilePath
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
        if (ReviewDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK,
                DialogIconEnum.Warning);
            return;
        }

        var (isSuccess, errorMessage) = Cache.Verify();
        if (isSuccess == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Cache varify error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
            DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(async () =>
        {
            if (await VerifyCalibrationAsync(cancellationToken) == false) result = false;
            return result;
        }).ConfigureAwait(false);
    }

    private async Task<bool> VerifyCalibrationAsync(CancellationToken cancellationToken, bool isAutoReview = false)
    {
        await Task.Run(async () =>
        {
            try
            {
                var detectImageDirectory = ImageFileDirectory;

                var (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();
                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    xAxisTemperature,
                    yAxisTemperature,
                    reviewCamTemperature,
                    cibTemperature,
                    Cache.P5Angle,
                    LensName = Cache.HighMicroscopeLensInformation.LensName,
                    Cache.AlgorithmTemplateTypeEnum,
                    Cache.CalculateContainRowMinCount,
                    Cache.CalculateContainColumnMinCount,
                    Cache.VerifyAlignmentThreshold,
                    Cache.VerifyGantryThreshold,
                    Cache.VerifyScaleThreshold,
                    Cache.BrightFieldFirstStageMapPosition,
                    Cache.DarkFieldFirstStageMapPosition
                }), HtmlLogUniqueId.LoggingHtml());

                StageViewModel.SetStageMap(ReviewDto!.ExpandStageMapDto);
                StageViewModel.SetEnableStageMap(true);

                ReviewDto.IsVerifyBrightField = false;
                ReviewDto.IsVerified = false;
                ReviewDto.VerifyBrightFieldStageMap.Reset();
                OnPropertyChanged(nameof(ReviewDto.VerifyBrightFieldStageMap));

                ReviewDto.IsVerifyDarkField = false;
                ReviewDto.VerifyDarkFieldStageMap.Reset();
                OnPropertyChanged(nameof(ReviewDto.VerifyDarkFieldStageMap));

                #region 暗场验证

                Cache.IsDarkField = true;
                Cache.GetParam();

                Logger.LogHtmlInformation("Dark Field", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                if (IsRecipeCalibrate)
                {
                    if (await DarkFieldStep1ActionAsync() == false) return false;
                    DarkFieldStep2Action();
                    ReviewDto.VerifyDarkFieldStageMap = ResultChuckStageMapDto.CalibrationDarkFieldStageMap.Clone();
                }
                else
                {
                    if (IsDarkFieldAlignment == false)
                        StageViewModel.Alignment(
                            AlignmentCacheBrightField.LowSite1,
                            AlignmentCacheBrightField.LowSite2,
                            AlignmentCacheBrightField.HighSite1,
                            AlignmentCacheBrightField.HighSite2,
                            AlignmentCacheBrightField.LowMag,
                            AlignmentCacheBrightField.HighMag,
                            AlignmentCacheBrightField.AlgorithmWaferTypeEnum);
                    else
                        StageViewModel.AlignmentDarkField(
                            AlignmentCacheDarkField.LowSite1,
                            AlignmentCacheDarkField.LowSite2,
                            AlignmentCacheDarkField.HighSite1,
                            AlignmentCacheDarkField.HighSite2,
                            AlignmentCacheDarkField.HighDarkFieldOpticsMagTypeEnum,
                            AlignmentCacheDarkField.HighDarkFieldStageSpeedEnum,
                            AlignmentCacheDarkField.LowMag,
                            AlignmentCacheDarkField.AlgorithmWaferTypeEnum);
                }

                Logger.LogHtmlInformation("Get Stage Map", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                DarkFieldGetStageMap(ReviewDto.VerifyDarkFieldStageMap, detectImageDirectory, () => OnPropertyChanged(nameof(ReviewDto.VerifyDarkFieldStageMap)), cancellationToken, true);

                var middleFileDateTimeFormat = DateTimeHelper.DateTime2String(DateTime.Now, Constants.MiddleFileDateTimeFormat);
                ReviewDto.VerifyDarkFieldStageMap.IdealCsvFilePath = $"{CsvFileDirectory}\\ReviewDarkField\\{middleFileDateTimeFormat}\\Ideal_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyDarkFieldStageMap.RealCsvFilePath = $"{CsvFileDirectory}\\ReviewDarkField\\{middleFileDateTimeFormat}\\Real_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyDarkFieldStageMap.RealIsInWaferOkCsvFilePath = $"{CsvFileDirectory}\\ReviewDarkField\\{middleFileDateTimeFormat}\\RealIsInWafer_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyDarkFieldStageMap.RealIsMatchOkCsvFilePath = $"{CsvFileDirectory}\\ReviewDarkField\\{middleFileDateTimeFormat}\\RealIsMatchOk_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyDarkFieldStageMap.ErrorCsvFilePath = $"{CsvFileDirectory}\\ReviewDarkField\\{middleFileDateTimeFormat}\\Error_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyDarkFieldStageMap.SaveIdealCsv(ReviewDto.VerifyDarkFieldStageMap.IdealCsvFilePath);
                ReviewDto.VerifyDarkFieldStageMap.SaveRealCsv(ReviewDto.VerifyDarkFieldStageMap.RealCsvFilePath);
                ReviewDto.VerifyDarkFieldStageMap.SaveIsInWaferOkCsv(ReviewDto.VerifyDarkFieldStageMap.RealIsInWaferOkCsvFilePath);
                ReviewDto.VerifyDarkFieldStageMap.SaveIsMatchOkCsv(ReviewDto.VerifyDarkFieldStageMap.RealIsMatchOkCsvFilePath);

                (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();

                var htmlQuoteList = new HtmlQuote(new
                {
                    xAxisTemperature,
                    yAxisTemperature,
                    reviewCamTemperature,
                    cibTemperature,
                    LensName = Cache.HighMicroscopeLensInformation.LensName,
                    Cache.AlgorithmTemplateTypeEnum,
                    ReviewDto.VerifyDarkFieldStageMap.IdealCsvFilePath,
                    ReviewDto.VerifyDarkFieldStageMap.RealCsvFilePath,
                    ReviewDto.VerifyDarkFieldStageMap.RealIsInWaferOkCsvFilePath,
                    ReviewDto.VerifyDarkFieldStageMap.RealIsMatchOkCsvFilePath,
                    ReviewDto.VerifyDarkFieldStageMap.ErrorCsvFilePath
                });

                var tryCalculateStageMapError = CalibrationAlgorithmService.CalculateChuckStageMapError(
                    ReviewDto.VerifyDarkFieldStageMap,
                    true,
                    HtmlLogUniqueId,
                    Cache.CalculateContainRowMinCount,
                    Cache.CalculateContainColumnMinCount,
                    Cache.VerifyAlignmentThreshold,
                    Cache.VerifyGantryThreshold,
                    Cache.VerifyScaleThreshold,
                    Cache.WaferDiameter);

                OnPropertyChanged(nameof(ReviewDto.VerifyDarkFieldStageMap));
                ReviewDto.VerifyDarkFieldStageMap.SaveErrorCsv(ReviewDto.VerifyDarkFieldStageMap.ErrorCsvFilePath);
                if (tryCalculateStageMapError == false)
                {
                    Logger.LogHtmlInformation("Calculate Stage Map Error Failed!", HtmlHeaderLevelEnum.Header4, htmlQuoteList, HtmlLogUniqueId.LoggingHtml());
                }

                var result = ReviewDto.VerifyDarkFieldStageMap.ErrorMatrix
                    .SelectMany(t => t)
                    .All(t => t.ToOriginLength < Cache.Threshold.ToOriginLength);
                ReviewDto.IsVerifyDarkField = result;

                Logger.LogHtmlInformation($"Dark Field Stage Map Verify {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, htmlQuoteList, HtmlLogUniqueId.LoggingHtml());

                if (IsAutoCalibrate == false)
                    DialogWindowProvider.ShowDialog($"Verify Dark Field {(result ? "OK" : "Failed")}!", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);

                if (Save(ReviewDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Save Dark Field Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                #endregion 暗场验证

                #region 明场验证

                Cache.IsDarkField = false;
                Cache.GetParam();

                Logger.LogHtmlInformation("Bright Field", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                if (IsRecipeCalibrate)
                {
                    if (await BrightFieldStep1ActionAsync() == false) return false;
                    BrightFieldStep2Action();
                    ReviewDto!.VerifyBrightFieldStageMap = ResultChuckStageMapDto.CalibrationBrightFieldStageMap.Clone();
                }

                Logger.LogHtmlInformation("Get Stage Map", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                BrightFieldGetStageMap(ReviewDto.VerifyBrightFieldStageMap, detectImageDirectory, () => OnPropertyChanged(nameof(ReviewDto.VerifyBrightFieldStageMap)), cancellationToken);

                middleFileDateTimeFormat = DateTimeHelper.DateTime2String(DateTime.Now, Constants.MiddleFileDateTimeFormat);
                ReviewDto.VerifyBrightFieldStageMap.IdealCsvFilePath = $"{CsvFileDirectory}\\ReviewBrightField\\{middleFileDateTimeFormat}\\Ideal_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyBrightFieldStageMap.RealCsvFilePath = $"{CsvFileDirectory}\\ReviewBrightField\\{middleFileDateTimeFormat}\\Real_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyBrightFieldStageMap.RealIsInWaferOkCsvFilePath = $"{CsvFileDirectory}\\ReviewBrightField\\{middleFileDateTimeFormat}\\RealIsInWafer_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyBrightFieldStageMap.RealIsMatchOkCsvFilePath = $"{CsvFileDirectory}\\ReviewBrightField\\{middleFileDateTimeFormat}\\RealIsMatchOk_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyBrightFieldStageMap.ErrorCsvFilePath = $"{CsvFileDirectory}\\ReviewBrightField\\{middleFileDateTimeFormat}\\Error_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyBrightFieldStageMap.SaveIdealCsv(ReviewDto.VerifyBrightFieldStageMap.IdealCsvFilePath);
                ReviewDto.VerifyBrightFieldStageMap.SaveRealCsv(ReviewDto.VerifyBrightFieldStageMap.RealCsvFilePath);
                ReviewDto.VerifyBrightFieldStageMap.SaveIsInWaferOkCsv(ReviewDto.VerifyBrightFieldStageMap.RealIsInWaferOkCsvFilePath);
                ReviewDto.VerifyBrightFieldStageMap.SaveIsMatchOkCsv(ReviewDto.VerifyBrightFieldStageMap.RealIsMatchOkCsvFilePath);

                (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();

                htmlQuoteList = new HtmlQuote(new
                {
                    xAxisTemperature,
                    yAxisTemperature,
                    reviewCamTemperature,
                    cibTemperature,
                    LensName = Cache.HighMicroscopeLensInformation.LensName,
                    Cache.AlgorithmTemplateTypeEnum,
                    ReviewDto.VerifyBrightFieldStageMap.IdealCsvFilePath,
                    ReviewDto.VerifyBrightFieldStageMap.RealCsvFilePath,
                    ReviewDto.VerifyBrightFieldStageMap.RealIsInWaferOkCsvFilePath,
                    ReviewDto.VerifyBrightFieldStageMap.RealIsMatchOkCsvFilePath,
                    ReviewDto.VerifyBrightFieldStageMap.ErrorCsvFilePath
                });

                tryCalculateStageMapError = CalibrationAlgorithmService.CalculateChuckStageMapError(
                    ReviewDto.VerifyBrightFieldStageMap,
                    false,
                    HtmlLogUniqueId,
                    Cache.CalculateContainRowMinCount,
                    Cache.CalculateContainColumnMinCount,
                    Cache.VerifyAlignmentThreshold,
                    Cache.VerifyGantryThreshold,
                    Cache.VerifyScaleThreshold,
                    Cache.WaferDiameter);

                OnPropertyChanged(nameof(ReviewDto.VerifyBrightFieldStageMap));

                ReviewDto.VerifyBrightFieldStageMap.SaveErrorCsv(ReviewDto.VerifyBrightFieldStageMap.ErrorCsvFilePath);
                if (tryCalculateStageMapError == false)
                {
                    Logger.LogHtmlInformation("Calculate Stage Map Error Failed!", HtmlHeaderLevelEnum.Header4, htmlQuoteList, HtmlLogUniqueId.LoggingHtml());
                }

                result = ReviewDto.VerifyBrightFieldStageMap.ErrorMatrix
                    .SelectMany(t => t)
                    .All(t => t.ToOriginLength < Cache.Threshold.ToOriginLength);
                ReviewDto.IsVerifyBrightField = result;

                Logger.LogHtmlInformation($"Bright Field Stage Map Verify {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, htmlQuoteList, HtmlLogUniqueId.LoggingHtml());

                if (IsAutoCalibrate == false)
                    DialogWindowProvider.ShowDialog($"Verify Bright Field {(result ? "OK" : "Failed")}!", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);

                #endregion 明场验证

                ReviewDto.IsVerified = ReviewDto.IsVerifyDarkField && ReviewDto.IsVerifyBrightField;
                if (Save(ReviewDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    ReviewDto.IsVerified = false;
                    return false;
                }

                if (IsAutoCalibrate == false)
                    DialogWindowProvider.ShowDialog($"Verify {(ReviewDto.IsVerified ? "OK" : "Failed")}!", DialogButtonsEnum.OK, ReviewDto.IsVerified ? DialogIconEnum.Information : DialogIconEnum.Warning);

                return ReviewDto.IsVerified;
            }
            finally
            {
                StageViewModel.SetEnableStageMap(false);
            }
        }, cancellationToken);
        return ReviewDto!.IsVerified;
    }

    private void BrightFieldGetStageMap(StageMapDto stageMapDto, string detectImageDirectory, Action notifyAction, CancellationToken cancellationToken)
    {
        var temperatureList = new List<(double reviewCam, double cibTemperature, double xAxis, double yAxis)>();

        var idealStageMapItemMatrix = stageMapDto.IdealStageMapItemMatrix;
        var realMatrix = stageMapDto.RealMatrix;
        var errorItemList = stageMapDto.ErrorMatrix;
        for (var row = 0; row < stageMapDto.RowNumber; row++)
        {
            if (row % 2 == 0)
            {
                // Left to right
                for (var column = 0; column < stageMapDto.ColumnNumber; column++)
                {
                    if (idealStageMapItemMatrix[row][column].IsInWafer == false) continue;

                    idealStageMapItemMatrix[row][column].IsMatchOk = true;

                    //Invoke(row, column);
                }
            }
            else
            {
                // Right to left
                for (var column = stageMapDto.ColumnNumber - 1; column > -1; column--)
                {
                    if (idealStageMapItemMatrix[row][column].IsInWafer == false) continue;

                    idealStageMapItemMatrix[row][column].IsMatchOk = true;

                    //Invoke(row, column);
                }
            }
        }

        Logger.LogHtmlInformation("Temperature", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
        {
            Temperature = new HtmlPlot2DLinesChart(
            [
                ("Review Cam", temperatureList.Select((t, i) => new Point(i, t.reviewCam)).ToArray()),
                ("Cib Temperature", temperatureList.Select((t, i) => new Point(i, t.cibTemperature)).ToArray()),
                ("X Axis", temperatureList.Select((t, i) => new Point(i, t.xAxis)).ToArray()),
                ("Y Axis", temperatureList.Select((t, i) => new Point(i, t.yAxis)).ToArray())
            ], "Temperature")
        }), HtmlLogUniqueId.LoggingHtml());

        return;

        void Invoke(int row, int column)
        {
            try
            {
                var stageMapItem = idealStageMapItemMatrix[row][column];

                var (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();
                temperatureList.Add((reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature));

                Logger.LogHtmlInformation($"({row},{column})", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                cancellationToken.ThrowIfCancellationRequested();

                stageMapItem.Reset();
                stageMapItem.TemplateFilePath = Cache.BrightFieldTemplateFilePath;
                stageMapItem.TemplateImageFilePath = Cache.BrightFieldTemplateImageFilePath;

                StageViewModel.SetMachineAbsoluteStageXy(stageMapItem.Point);
                Thread.Sleep(500);
                var tempPosition = StageViewModel.GetBrightFieldStagePosition();
                if (ReviewViewModel.TryGetMatchPosition(
                        Cache.AlgorithmTemplateTypeEnum,
                        MicroscopePixelSizeItems,
                        tempPosition,
                        Cache.HighMicroscopeLensInformation,
                        Cache.BrightFieldTemplateFilePath,
                        $"{detectImageDirectory}\\row({row})_column({column})",
                        HtmlLogUniqueId,
                        Name,
                        $"High Magnification row({row}) column({column})",
                        out var resultPosition,
                        out var resultScore,
                        out var resultAngle,
                        out var resultImageFilePath, out _))
                {
                    var result = StageViewModel.BrightFieldToMachinePosition(resultPosition);

                    stageMapItem.IsMatchOk = true;

                    realMatrix[row][column] = result;
                    errorItemList[row][column] = result - (Vector)stageMapItem.Point;
                }

                stageMapItem.TemplateScore = resultScore;
                stageMapItem.TemplateAngle = resultAngle;
                stageMapItem.FilePath = resultImageFilePath;

                Logger.LogHtmlInformation($"({row},{column}),{(stageMapItem.IsMatchOk ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                {
                    OriginPosition = stageMapItem.Point,
                    ResultPosition = realMatrix[row][column],
                    Error = errorItemList[row][column],
                    resultScore,
                    resultAngle
                }), HtmlLogUniqueId.LoggingHtml());
            }
            finally
            {
                notifyAction.Invoke();
            }
        }
    }

    private void DarkFieldGetStageMap(StageMapDto stageMapDto, string detectImageDirectory, Action notifyAction, CancellationToken cancellationToken, bool isReview = false)
    {
        if (isReview) Guard.IsNotNull(ReviewDto);

        var templateXId = HalconFactory.EmptyHTuple;
        var templateYId = HalconFactory.EmptyHTuple;
        var templateId = HalconFactory.EmptyHTuple;

        if (Cache.AlgorithmTemplateTypeEnum == AlgorithmTemplateTypeEnum.Projection)
        {
            if (CalibrationAlgorithmService.TryReadProjectionTemplate(Cache.DarkFieldTemplateFilePath, out templateXId, out templateYId) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("Read Template Failed!"), HtmlLogUniqueId.LoggingHtml());
                return;
            }
        }
        else
        {
            if (CalibrationAlgorithmService.TryReadTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.DarkFieldTemplateFilePath, out templateId) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("Read Template Failed!"), HtmlLogUniqueId.LoggingHtml());
                return;
            }
        }

        var (xDirection, _) = StageViewModel.GetMachineDirection();

        using var _1 = templateXId;
        using var _2 = templateYId;
        using var _3 = templateId;

        try
        {
            var idealStageMapItemMatrix = stageMapDto.IdealStageMapItemMatrix;
            var realMatrix = stageMapDto.RealMatrix;
            var errorItemList = stageMapDto.ErrorMatrix;

            var idealMatrix = isReview && ReviewDto is not null ? new Point[ReviewDto.ExpandStageMapDto.RowNumber, ReviewDto.ExpandStageMapDto.ColumnNumber] : MatrixUtils.EmptyMatrix<Point>();
            var valueIsOkMatrix = isReview && ReviewDto is not null ? new bool[ReviewDto.ExpandStageMapDto.RowNumber, ReviewDto.ExpandStageMapDto.ColumnNumber] : MatrixUtils.EmptyMatrix<bool>();
            var valueMatrix = isReview && ReviewDto is not null ? new Point[ReviewDto.ExpandStageMapDto.RowNumber, ReviewDto.ExpandStageMapDto.ColumnNumber] : MatrixUtils.EmptyMatrix<Point>();
            if (isReview && ReviewDto is not null)
            {
                for (var i = 0; i < ReviewDto.ExpandStageMapDto.RowNumber; i++)
                {
                    for (var j = 0; j < ReviewDto.ExpandStageMapDto.ColumnNumber; j++)
                    {
                        idealMatrix[i, j] = ReviewDto.ExpandStageMapDto.IdealStageMapItemMatrix[i][j].Point;
                        valueIsOkMatrix[i, j] = true;
                        valueMatrix[i, j] = ReviewDto.ExpandStageMapDto.ErrorMatrix[i][j];
                    }
                }
            }

            for (var row = 0; row < idealStageMapItemMatrix.Length; row++)
            {
                var isInWaferRowList = idealStageMapItemMatrix[row]
                    .Select((t, i) => (Index: i, Item: t))
                    .Where(t => t.Item.IsInWafer)
                    .ToList();
                var points = isInWaferRowList.Select(t => t.Item.Point).ToList();
                if (points.Count == 0) continue;

                Logger.LogHtmlInformation($"{row + 1} row", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                if (isReview && ReviewDto is not null)
                {
                    Logger.LogHtmlInformation("error", HtmlHeaderLevelEnum.Header4, new HtmlComment(string.Join(Environment.NewLine, points)), HtmlLogUniqueId.LoggingHtml());
                    var strings = new List<string>();
                    for (var i = 0; i < points.Count; i++)
                    {
                        var item = isInWaferRowList[i];
                        var isok = Interpolator.TryBilinear(idealMatrix, valueIsOkMatrix, valueMatrix, points[i], out var value);

                        var error = ReviewDto.CalibrationDarkFieldStageMap.ErrorMatrix[row][item.Index];

                        strings.Add($"{row}, {item.Index}, {points[i]}, {error}, current: {value}, {isok} {value.ToString() == error.ToString()} {points[i] == ReviewDto.CalibrationDarkFieldStageMap.IdealStageMapItemMatrix[row][item.Index].Point}");
                        if (isok == false)
                        {
                            value = error;
                        }

                        points[i] = new Point(points[i].X + value.X, points[i].Y);
                    }

                    Logger.LogHtmlInformation("error", HtmlHeaderLevelEnum.Header4, new HtmlComment(string.Join(Environment.NewLine, strings) + Environment.NewLine + string.Join(Environment.NewLine, points)), HtmlLogUniqueId.LoggingHtml());
                }

                var darkImageRepeatList = new List<List<DarkFieldImageDto>>();
                foreach (var _ in Enumerable.Range(1, Cache.RepeatCount))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var rowDarkFieldImageDtoList = LaserViewModel.GetChuckDarkFieldRowLineScanImage(
                            points,
                            (false, CalibrationSetting.SettingCommonParam.MainLaserLightInformation),
                            false,
                            Cache.CIBConfiguration,
                            Cache.ProductivityInformation,
                            Cache.OpticsIlluminationModeEnum,
                            Cache.XWidthPixel,
                            CalibrationConstantsHelper.MainPmtId,
                            CalibrationConstantsHelper.MainChannelId,
                            StageCoordinateSystemEnum.Machine);
                        if (isInWaferRowList.Count != rowDarkFieldImageDtoList.Count)
                        {
                            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("Get Dark Field ChuckRow Line Scan Image List Failed!"), HtmlLogUniqueId.LoggingHtml());
                            continue;
                        }

                        darkImageRepeatList.Add(rowDarkFieldImageDtoList);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Get Dark Field ChuckRow Line Scan Image List Failed!Error:{ex.Message}"), HtmlLogUniqueId.LoggingHtml());
                    }
                }

                var plotDic = new Dictionary<(int RepeatIndex, int ColIndex), Point>();
                for (var column = 0; column < idealStageMapItemMatrix[row].Length; column++)
                {
                    var stageMapItem = idealStageMapItemMatrix[row][column];
                    if (stageMapItem.IsInWafer == false) continue;

                    Logger.LogHtmlInformation($"{column + 1} column", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());
                    cancellationToken.ThrowIfCancellationRequested();

                    stageMapItem.Reset();
                    stageMapItem.TemplateFilePath = Cache.DarkFieldTemplateFilePath;
                    stageMapItem.TemplateImageFilePath = Cache.DarkFieldTemplateImageFilePath;

                    try
                    {
                        foreach (var (index, rowDarkFieldImageDtoList) in darkImageRepeatList.Select((t, i) => (Index: i, RowDarkFieldImageDtoList: t)))
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            using var darkFieldImageDto = rowDarkFieldImageDtoList.ElementAt(column - isInWaferRowList[0].Index);
                            var ySizePerPixel = LaserPixelSizeItems.Single(t => t.PmtId == CalibrationConstantsHelper.MainPmtId && t.ProductivityInformation == Cache.ProductivityInformation && t.IsOk).YPixelSize;
                            var xSizePerPixel = LaserXPixelSizeItems.Single(t => t.ProductivityInformation == Cache.ProductivityInformation && t.IsOk).XPixelSize;

                            var originImageFilePath = $"{detectImageDirectory}\\row({row})_col({column})_index({index})_Guid({HtmlLogUniqueId}_{Guid.NewGuid()}).jpg";
                            darkFieldImageDto.Image.Save(originImageFilePath);

                            if ((Cache.AlgorithmTemplateTypeEnum == AlgorithmTemplateTypeEnum.Projection
                                    ? CalibrationAlgorithmService.TryProjectionTemplateMatchToOffset(darkFieldImageDto.Image, templateXId, templateYId, out var point, out var offset)
                                    : CalibrationAlgorithmService.TryTemplateMatchToOffset(Cache.AlgorithmTemplateTypeEnum, darkFieldImageDto.Image, templateId, out point, out offset, out _, out _)) == false)
                            {
                                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                                {
                                    Error = $"Try {EnumHelper.ToDescriptionString(Cache.AlgorithmTemplateTypeEnum)} Match To Offset Failed",
                                    darkFieldImageDto.PmtId,
                                    darkFieldImageDto.ChannelId,
                                    darkFieldImageDto.Width,
                                    Cache.ProductivityInformation,
                                    CalibrationConstantsHelper.MainStageSpeedEnum,
                                    point,
                                    offset,
                                    OriginPosition = stageMapItem.Point,
                                    HtmlTab = new HtmlTab(new
                                    {
                                        OriginImage = new HtmlImage(originImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(point)]),
                                        TemplateImage = new HtmlImage(Cache.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                                    })
                                }), HtmlLogUniqueId.LoggingHtml());
                                continue;
                            }

                            offset = new Point(xDirection * offset.X, offset.Y);
                            var actualOffset = new Point(offset.X * xSizePerPixel, offset.Y * ySizePerPixel);
                            plotDic.Add((index, column), actualOffset);

                            stageMapItem.FilePath = originImageFilePath;

                            Logger.LogHtmlInformation($"{index + 1}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                            {
                                darkFieldImageDto.PmtId,
                                darkFieldImageDto.ChannelId,
                                darkFieldImageDto.Width,
                                Cache.ProductivityInformation,
                                CalibrationConstantsHelper.MainStageSpeedEnum,
                                point,
                                offset,
                                actualOffset,
                                OriginPosition = stageMapItem.Point,
                                HtmlTab = new HtmlTab(new
                                {
                                    OriginImage = new HtmlImage(originImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(point)]),
                                    TemplateImage = new HtmlImage(Cache.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                                })
                            }), HtmlLogUniqueId.LoggingHtml());
                        }

                        var actualOffsetList = plotDic
                            .Where(kvp => kvp.Key.ColIndex == column)
                            .Select(kvp => kvp.Value).ToList();

                        if (actualOffsetList.Count == 0) continue;

                        var offsetX = actualOffsetList.Average(t => t.X);
                        var offsetY = actualOffsetList.Average(t => t.Y);

                        var offsetResult = new Point(offsetX, offsetY);
                        var resultPosition = stageMapItem.Point + (Vector)offsetResult;
                        stageMapItem.IsMatchOk = true;
                        realMatrix[row][column] = resultPosition;
                        errorItemList[row][column] = resultPosition - (Vector)stageMapItem.Point;

                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                        {
                            offsetResult,
                            OriginPosition = stageMapItem.Point,
                            ResultPosition = resultPosition,
                            Plot = new HtmlPlot2DLinesChart([("Error X", actualOffsetList.Select(t => t.X).ToPoints()), ("Error Y", actualOffsetList.Select(t => t.Y).ToPoints())], "Error")
                        }), HtmlLogUniqueId.LoggingHtml());
                    }
                    finally
                    {
                        notifyAction.Invoke();
                    }
                }

                var plotDicGroup = (from kvp in plotDic
                                    group kvp.Value by kvp.Key.RepeatIndex
                    into g
                                    select (RepeatCount: $"{g.Key + 1}", Points: g.ToArray())).ToList();
                if (plotDicGroup.Count == 0)
                    continue;

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    PlotX = new HtmlPlot2DLinesChart([.. plotDicGroup.Select(kvp => (kvp.RepeatCount, kvp.Points.Select(t => t.X).ToPoints()))], "Error X"),
                    PlotY = new HtmlPlot2DLinesChart([.. plotDicGroup.Select(kvp => (kvp.RepeatCount, kvp.Points.Select(t => t.Y).ToPoints()))], "Error Y")
                }), HtmlLogUniqueId.LoggingHtml());
            }
        }
        finally
        {
            if (Cache.AlgorithmTemplateTypeEnum == AlgorithmTemplateTypeEnum.Projection)
            {
                if (CalibrationAlgorithmService.TryCleanProjectionTemplate(templateXId, templateYId) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("Clean Template Failed!"), HtmlLogUniqueId.LoggingHtml());
                }
            }
            else
            {
                if (CalibrationAlgorithmService.TryCleanTemplate(Cache.AlgorithmTemplateTypeEnum, templateId) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("Clean Template Failed!"), HtmlLogUniqueId.LoggingHtml());
                }
            }
        }
    }

    private bool Save(ChuckStageMapDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        dto.HighMicroscopeLensInformation = Cache.HighMicroscopeLensInformation;
        dto.ProductivityInformation = Cache.ProductivityInformation;

        Calibration = dto.Clone();

        CacheProvider.Set(dto, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    }) && EnableDependedCalibrationItems(cancellationToken);

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependBrightStageMapCalibrations(false, cancellationToken, out var errorMsg) == false)
        {
            Logger.LogError("Toggle {@Name} Enable Status Failed!", errorMsg);
            return false;
        }

        if (CalibrationStatusService.EnableDependDarkStageMapCalibrations(false, cancellationToken, out errorMsg) == false)
        {
            Logger.LogError("Toggle {@Name} Enable Status Failed!", errorMsg);
            return false;
        }

        return true;
    }

    #endregion 校准

    #region 自动化校准

    public override void GetAutoCalibrationStep()
    {
        AutoCalibrationStepList =
        [
            new() { StepName = "loading", StepIndex = 0 },
            new() { StepName = "BF P5", StepIndex = 1 },
            new() { StepName = "BF Find Start Point", StepIndex = 2 },
            new() { StepName = "BF Param", StepIndex = 3 },
            new() { StepName = "BF Stage Map", StepIndex = 4 },
            new() { StepName = "DF P5", StepIndex = 5 },
            new() { StepName = "DF Find Start Point", StepIndex = 6 },
            new() { StepName = "DF Param", StepIndex = 7 },
            new() { StepName = "DF Stage Map", StepIndex = 8 },
            new() { StepName = "Expand To BF", StepIndex = 9 },
            new() { StepName = "Review", StepIndex = 10 }
        ];
    }

    public override async Task<bool> AutomationActionAsync(CancellationToken cancellationToken)
    {
        GetAutoCalibrationStep();
        await base.AutomationActionAsync(cancellationToken);
        CalibrationStepIndex = -1;
        try
        {
            foreach (var item in AutoCalibrationStepList)
            {
                switch (item.StepIndex)
                {
                    case 0:
                        if (await LoadedingAsync(cancellationToken) == false) return false;
                        await InvokeCalibrateAsync(() =>
                        {
                            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                            {
                                Cache.AlgorithmTemplateTypeEnum,
                                LensName = Cache.HighMicroscopeLensInformation.LensName,
                                Cache.ProductivityInformation
                            }), HtmlLogUniqueId.LoggingHtml());
                            return true;
                        });
                        break;

                    case 2 or 6:
                        if (await Step1CalibrateActionAsync(cancellationToken) == false) return false;
                        break;

                    case 3 or 7:
                        if (await Step2CalibrateActionAsync(cancellationToken) == false) return false;
                        break;

                    case 4 or 8:
                        if (await Step3CalibrateActionAsync(cancellationToken) == false) return false;
                        break;

                    case 9:
                        if (await Step4CalibrateActionAsync(cancellationToken) == false) return false;
                        break;

                    case 10:
                        if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false) return false;
                        return await InvokeCalibrateAsync(async () =>
                        {
                            ReviewDto = Calibration.Clone();
                            return await VerifyCalibrationAsync(cancellationToken);
                        });
                }

                await Task.Delay(2000, cancellationToken);
                if (await AutoNextingAsync(cancellationToken) == false) return false;
                CalibrationStepIndex++;
                if (await NextingAsync(cancellationToken) == false) return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Auto Stage Map Failed!Error:{ex.Message}"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }
    }

    public override async Task<bool> AutomationRecipeInformationAsync(string stepName)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (IsRecipeCalibrate == false)
            return true;

        if (CalibrationRecipeDto is null)
        {
            DialogWindowProvider.ShowDialog("Revise wafer map is empty!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var originReticle = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleModel.Single(t => t.Index is { X: 0, Y: 0 });

        switch (stepName)
        {
            case "0":
                if (CalibrationRecipeService.GetChuckReticleMaskInfo(Cache.WaferMaskTypeEnum, Cache.HighMicroscopeLensInformation, Cache.ProductivityInformation, out var maskInfoBrightField) == false)
                    return false;
                CalibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticle, maskInfoBrightField, out var positionBright);
                Cache.FirstStageMapPosition = Cache.BrightFieldFirstStageMapPosition = StageViewModel.BrightFieldToMachinePosition(positionBright);
                Cache.TemplateFilePath = Cache.BrightFieldTemplateFilePath = maskInfoBrightField.RecipeBrightFieldTemplateDto.TemplateFilePath;
                Cache.TemplateImageFilePath = Cache.BrightFieldTemplateImageFilePath = maskInfoBrightField.RecipeBrightFieldTemplateDto.TemplateImageFilePath;
                break;

            case "1":
                if (CalibrationRecipeService.GetChuckReticleMaskInfo(Cache.WaferMaskTypeEnum, Cache.HighMicroscopeLensInformation, Cache.ProductivityInformation, out var maskInfoDarkField) == false)
                    return false;
                CalibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticle, maskInfoDarkField, out var positionDark);
                Cache.FirstStageMapPosition = Cache.DarkFieldFirstStageMapPosition = StageViewModel.DarkFieldToMachinePosition(positionDark);
                Cache.TemplateFilePath = Cache.DarkFieldTemplateFilePath = maskInfoDarkField.RecipeBrightFieldTemplateDto.TemplateFilePath;
                Cache.TemplateImageFilePath = Cache.DarkFieldTemplateImageFilePath = maskInfoDarkField.RecipeBrightFieldTemplateDto.TemplateImageFilePath;
                break;
        }

        return true;
    }

    private async Task<bool> AutoNextingAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            CalibrationStepName = AutoCalibrationStepList[AutoCalibrationStepIndex + 1].StepName;
            AutoCalibrationStepIndex++;
        }, cancellationToken);
        return true;
    }

    public override async Task<bool> AutomationReviewActionAsync(CancellationToken cancellationToken)
    {
        GetAutoCalibrationStep();
        await base.AutomationReviewActionAsync(cancellationToken);
        if (await LoadedingAsync(cancellationToken) == false) return false;
        if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false)
        {
            DialogWindowProvider.ShowDialog($"Please Calibration!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var result = await InvokeVerifyAsync(async () =>
        {
            if (await VerifyCalibrationAsync(cancellationToken, true) == false)
            {
                DialogWindowProvider.ShowDialog("Auto Calibration Review Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            return true;
        });
        AutoCalibrationProgress = (AutoCalibrationStepIndex + 1) / (double)AutoCalibrationStepList.Count * 100;
        return result;
    }

    #endregion 自动化校准
}