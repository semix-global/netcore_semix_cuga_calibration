using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.BrightFieldStageMap;
using Core.Models.Models.Chuck.Center;
using Core.Models.Models.Chuck.DarkFieldStageMap;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Laser.AodDelay;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.LineCentricity;
using Core.Models.Models.Laser.OpticalPower;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Laser.PrescanChirpAodAlignment;
using Core.Models.Models.Laser.XPixelSize;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using CugaCalibration.ViewModels.Common.Windows.File.Setting;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon;
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

[IOCAppService(ServiceType = typeof(ChuckDarkFieldStageMapCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckDarkFieldStageMapCalibrationViewModel(
    AlignmentWindowBrightFieldViewModel alignmentWindowBrightFieldViewModel,
    AlignmentWindowDarkFieldViewModel alignmentWindowDarkFieldViewModel,
    CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel,
    SettingWindowViewModel settingWindowViewModel,
    ChuckBrightFieldStageMapCalibrationViewModel chuckBrightFieldStageMapCalibrationViewModel) : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => $"{EnumHelper.ToDescriptionString(Cache.MicroscopeMagnificationEnum)}-{EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum)}";

    public override string CalibrateFileName => $"{EnumHelper.ToDescriptionString(Cache.MicroscopeMagnificationEnum)}-{EnumHelper.ToDescriptionString(Cache.OpticsMagTypeEnum)}";

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "P5" },
        new() { StepName = "Param" },
        new() { StepName = "Find Start Point" },
        new() { StepName = "Stage Map" },
        new() { StepName = "Expand To BF" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private bool _isDarkField;

    [ObservableProperty]
    private ChuckDarkFieldStageMapDto _resultChuckDarkFieldStageMapDto = new();

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ChuckDarkFieldStageMapDto? _reviewDto;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private ChuckDarkFieldStageMapCache _cache = new();

    [ObservableProperty]
    private ChuckDarkFieldStageMapDto _calibration = new();

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    [ObservableProperty]
    private AlignmentCacheDarkField _alignmentCacheDarkField = new();

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

    [ObservableProperty]
    private ChuckCenterObjDto _chuckCenter = new();

    [ObservableProperty]
    private ChuckGlobalScaleErrorDto _chuckGlobalScaleError = new();

    [ObservableProperty]
    private ChuckBrightFieldStageMapCache _chuckBrightFieldStageMapCache = new();

    [ObservableProperty]
    private ChuckBrightFieldStageMapDto _chuckBrightFieldStageMap = new();

    [ObservableProperty]
    private LaserPixelSizeItemDto[] _laserPixelSizeItems = [];

    [ObservableProperty]
    private LaserXPixelSizeItemDto[] _laserXPixelSizeItems = [];

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

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckCenterObjDto>(out var chuckCenter, out errorMessage) == false)
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

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckBrightFieldStageMapDto>(out var chuckBrightFieldStageMap, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        ChuckBrightFieldStageMapCache = RecipeCacheProvider.GetOrDefault<ChuckBrightFieldStageMapCache>();
        ChuckBrightFieldStageMap = chuckBrightFieldStageMap;

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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserOpticalPowerDto>(out _, out errorMessage) == false)
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

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserXPixelSizeItemDto>(out var laserXPixelSizeItems, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        LaserXPixelSizeItems = laserXPixelSizeItems;

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<LaserLineCentricityItemDto>(out var laserLineCentricityItems, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        LaserLineCentricityItems = laserLineCentricityItems;

        if (DialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question) == true || dialogResult == DialogResultEnum.Yes)
        {
            IsDarkField = true;
        }
        else
        {
            IsDarkField = false;
        }

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<ChuckDarkFieldStageMapCache>();
        Calibration = CacheProvider.GetOrDefault<ChuckDarkFieldStageMapDto>();
        AlignmentCacheDarkField = RecipeCacheProvider.GetOrDefault<AlignmentCacheDarkField>();
        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();

        return isHasCache || RecipeCacheProvider.Set(Cache, cancellationToken);
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (IsDarkField)
        {
            if (AlignmentCacheDarkField.IsOk) return true;

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
            if (AlignmentCacheBrightField.IsOk) return true;

            var showDialog = WindowManagerService.ShowDialog(alignmentWindowBrightFieldViewModel);

            if (showDialog == false)
            {
                DialogWindowProvider.ShowDialog("Alignment Setting is Empty", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            AlignmentCacheBrightField = alignmentWindowBrightFieldViewModel.Cache;
            StageViewModel.SetBrightFieldAbsoluteStageXy(AlignmentCacheBrightField.LowSite1.Location);
        }

        StageViewModel.SetEnableStageMap(false);

        return true;
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
            case 0:
                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
                return true;

            case 1:
            case 2:
                StageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(Cache.FirstStageMapPosition);
                return true;

            case 3:
                return true;

            case 4:
                ResultChuckDarkFieldStageMapDto.IsCalibrated = true;
                ResultChuckDarkFieldStageMapDto.VerifyDarkFieldStageMap = ResultChuckDarkFieldStageMapDto.CalibrationStageMap.Clone();
                ResultChuckDarkFieldStageMapDto.VerifyBrightFieldStageMap = ChuckBrightFieldStageMap.CalibrationStageMap.Clone();
                if (Save(ResultChuckDarkFieldStageMapDto, cancellationToken) == false)
                {
                    ResultChuckDarkFieldStageMapDto.IsCalibrated = false;
                    Logger.LogError("{@Name} Error: Save Failed!", Name);
                    return false;
                }

                IsCalibrated = true;

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
            StageViewModel.SetEnableStageMap(false);

            AlignmentResultDto alignmentResultDto;
            if (IsDarkField)
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
            }
            else
            {
                alignmentResultDto = StageViewModel.Alignment(
                    AlignmentCacheBrightField.LowSite1,
                    AlignmentCacheBrightField.LowSite2,
                    AlignmentCacheBrightField.HighSite1,
                    AlignmentCacheBrightField.HighSite2,
                    AlignmentCacheBrightField.LowMag,
                    AlignmentCacheBrightField.HighMag,
                    AlignmentCacheBrightField.AlgorithmWaferTypeEnum);
            }

            Cache.P5Angle = alignmentResultDto.Degrees;

            StageViewModel.SetEnableStageMap(false);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.P5Angle,
                Cache.MicroscopeMagnificationEnum
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var (isSuccess, errorMessage) = Cache.Step1Verify();
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Cache varify error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            StageViewModel.SetEnableStageMap(false);

            if (Cache is { RowNumber: <= 0, ColumnNumber: <= 0, ColumnCellWidth: <= 0, RowCellHeight: <= 0 })
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{nameof(Cache.RowNumber)} > 0 and {nameof(Cache.ColumnNumber)} > 0 and {nameof(Cache.ColumnCellWidth)} > 0 and {nameof(Cache.RowCellHeight)} > 0"),
                    HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            var temp = StageViewModel.GetBrightFieldStagePosition();
            var centerPosition = StageViewModel.DarkFieldToMachinePosition(temp);
            var laserLineCentricityItemDto = LaserLineCentricityItems.Single(t => t is
            {
                PmtId: CalibrationConstantsHelper.MainPmtId,
                OpticsMagTypeEnum: CalibrationConstantsHelper.MainOpticsMagTypeEnum,
                StageSpeedEnum: CalibrationConstantsHelper.MainStageSpeedEnum
            });

            ResultChuckDarkFieldStageMapDto.CalibrationStageMap = new StageMapDto(Cache.RowNumber, Cache.ColumnNumber, Cache.RowCellHeight, Cache.ColumnCellWidth);
            ResultChuckDarkFieldStageMapDto.CalibrationStageMap.GenerateByCenterPosition(
                HostEnvironment.IsDevelopment()
                    ? laserLineCentricityItemDto.ForwardDarkMachineCenterPosition
                    : centerPosition,
                laserLineCentricityItemDto.ForwardDarkMachineCenterPosition,
                Cache.WaferDiameter);

            Cache.FirstStageMapPosition = HostEnvironment.IsDevelopment()
                ? laserLineCentricityItemDto.ForwardDarkMachineCenterPosition
                : centerPosition;

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

            OnPropertyChanged(nameof(ResultChuckDarkFieldStageMapDto.CalibrationStageMap));

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var darkFieldImageDto = LaserViewModel.GetDarkFieldLineScanImage(
                CalChipSiteModelEnum.ChuckModel,
                Cache.FirstStageMapPosition,
                (false, 0.85),
                false,
                null,
                Cache.XWidthPixel,
                Cache.OpticsMagTypeEnum,
                Cache.StageSpeedEnum,
                stageCoordinateSystemEnum: StageCoordinateSystemEnum.Machine);
            var detectImageDirectory = ImageFileDirectory;
            using var _ = darkFieldImageDto;

            Cache.TemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.MicroscopeMagnificationEnum}_{Guid.NewGuid()}";
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
                HalconHelper.Save(darkFieldImageDto.Image, filePath);
                createDarkImageTemplateWindowViewModel.ImageFilePath = filePath;
                createDarkImageTemplateWindowViewModel.TemplateFilePath = Cache.TemplateFilePath;

                var showDialog = WindowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel);

                if (showDialog == false)
                {
                    DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }
            }

            Cache.TemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.TemplateFilePath);

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

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3CalibrateActionAsync(CancellationToken cancellationToken)
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

            var detectImageDirectory = ImageFileDirectory;

            StageViewModel.SetEnableStageMap(false);

            ResultChuckDarkFieldStageMapDto.CalibrationStageMap.Reset();
            OnPropertyChanged(nameof(ResultChuckDarkFieldStageMapDto.CalibrationStageMap));

            var (ecs, height) = settingWindowViewModel.HighMagSettingDarkFieldAutoFocusViewModel.ChuckAfAutoRtfc(StageViewModel.MachineToDarkFieldPosition(Cache.FirstStageMapPosition), HtmlLogUniqueId);
            settingWindowViewModel.SaveSetting();

            var (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                xAxisTemperature,
                yAxisTemperature,
                reviewCamTemperature,
                cibTemperature,
                RtfcPosition = Cache.FirstStageMapPosition,
                RtfcEcs = ecs,
                RtfcHeight = height,
                Cache.P5Angle,
                Cache.MicroscopeMagnificationEnum,
                Cache.AlgorithmTemplateTypeEnum,
                Cache.CalculateContainRowMinCout,
                Cache.CalculateContainColumnMinCount,
                Cache.CalibrationAlignmentThreshold,
                Cache.CalibrationGantryThreshold,
                Cache.CalibrationScaleThreshold,
                Cache.FirstStageMapPosition
            }), HtmlLogUniqueId.LoggingHtml());

            GetStageMap(ResultChuckDarkFieldStageMapDto.CalibrationStageMap, detectImageDirectory, () => OnPropertyChanged(nameof(ResultChuckDarkFieldStageMapDto.CalibrationStageMap)), cancellationToken);

            var middleFileDateTimeFormat = DateTimeHelper.DateTime2String(DateTime.Now, Constants.MiddleFileDateTimeFormat);
            ResultChuckDarkFieldStageMapDto.CalibrationStageMap.IdealCsvFilePath = $"{CsvFileDirectory}\\Calibration\\{middleFileDateTimeFormat}\\Ideal_Guid({HtmlLogUniqueId}).csv";
            ResultChuckDarkFieldStageMapDto.CalibrationStageMap.RealCsvFilePath = $"{CsvFileDirectory}\\Calibration\\{middleFileDateTimeFormat}\\Real_Guid({HtmlLogUniqueId}).csv";
            ResultChuckDarkFieldStageMapDto.CalibrationStageMap.RealIsInWaferOkCsvFilePath = $"{CsvFileDirectory}\\Calibration\\{middleFileDateTimeFormat}\\RealIsInWafer_Guid({HtmlLogUniqueId}).csv";
            ResultChuckDarkFieldStageMapDto.CalibrationStageMap.RealIsMatchOkCsvFilePath = $"{CsvFileDirectory}\\Calibration\\{middleFileDateTimeFormat}\\RealIsMatchOk_Guid({HtmlLogUniqueId}).csv";
            ResultChuckDarkFieldStageMapDto.CalibrationStageMap.ErrorCsvFilePath = $"{CsvFileDirectory}\\Calibration\\{middleFileDateTimeFormat}\\Error_Guid({HtmlLogUniqueId}).csv";
            ResultChuckDarkFieldStageMapDto.CalibrationStageMap.SaveIdealCsv(ResultChuckDarkFieldStageMapDto.CalibrationStageMap.IdealCsvFilePath);
            ResultChuckDarkFieldStageMapDto.CalibrationStageMap.SaveRealCsv(ResultChuckDarkFieldStageMapDto.CalibrationStageMap.RealCsvFilePath);
            ResultChuckDarkFieldStageMapDto.CalibrationStageMap.SaveIsInWaferOkCsv(ResultChuckDarkFieldStageMapDto.CalibrationStageMap.RealIsInWaferOkCsvFilePath);
            ResultChuckDarkFieldStageMapDto.CalibrationStageMap.SaveIsMatchOkCsv(ResultChuckDarkFieldStageMapDto.CalibrationStageMap.RealIsMatchOkCsvFilePath);

            var tryCalculateStageMapError = CalibrationAlgorithmService.CalculateChuckStageMapError(
                ResultChuckDarkFieldStageMapDto.CalibrationStageMap,
                HtmlLogUniqueId,
                Cache.CalculateContainRowMinCout,
                Cache.CalculateContainColumnMinCount,
                Cache.CalibrationAlignmentThreshold,
                Cache.CalibrationGantryThreshold,
                Cache.CalibrationScaleThreshold,
                Cache.WaferDiameter);

            OnPropertyChanged(nameof(ResultChuckDarkFieldStageMapDto.CalibrationStageMap));
            ResultChuckDarkFieldStageMapDto.CalibrationStageMap.SaveErrorCsv(ResultChuckDarkFieldStageMapDto.CalibrationStageMap.ErrorCsvFilePath);

            (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();

            if (tryCalculateStageMapError == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Error = "Calculate Stage Map Error Failed!",
                    xAxisTemperature,
                    yAxisTemperature,
                    reviewCamTemperature,
                    cibTemperature,
                    Cache.MicroscopeMagnificationEnum,
                    Cache.AlgorithmTemplateTypeEnum,
                    ResultChuckDarkFieldStageMapDto.CalibrationStageMap.IdealCsvFilePath,
                    ResultChuckDarkFieldStageMapDto.CalibrationStageMap.RealCsvFilePath,
                    ResultChuckDarkFieldStageMapDto.CalibrationStageMap.RealIsMatchOkCsvFilePath,
                    ResultChuckDarkFieldStageMapDto.CalibrationStageMap.ErrorCsvFilePath
                }), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                xAxisTemperature,
                yAxisTemperature,
                cibTemperature,
                Cache.MicroscopeMagnificationEnum,
                Cache.AlgorithmTemplateTypeEnum,
                ResultChuckDarkFieldStageMapDto.CalibrationStageMap.IdealCsvFilePath,
                ResultChuckDarkFieldStageMapDto.CalibrationStageMap.RealCsvFilePath,
                ResultChuckDarkFieldStageMapDto.CalibrationStageMap.ErrorCsvFilePath
            }), HtmlLogUniqueId.LoggingHtml());

            return tryCalculateStageMapError;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step4CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var darkFieldStageMap = ResultChuckDarkFieldStageMapDto.CalibrationStageMap;
            var brightFieldStageMapDto = ChuckBrightFieldStageMap.CalibrationStageMap;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                darkFieldStageMapRowNumber = darkFieldStageMap.RowNumber,
                darkFieldStageMapColumnNumber = darkFieldStageMap.ColumnNumber,
                darkFieldStageMapRowCellHeight = darkFieldStageMap.RowCellHeight,
                darkFieldStageMapColumnCellWidth = darkFieldStageMap.ColumnCellWidth,
                brightFieldStageMapDtoRowNumber = brightFieldStageMapDto.RowNumber,
                brightFieldStageMapDtoColumnNumber = brightFieldStageMapDto.ColumnNumber,
                brightFieldStageMapDtoRowCellHeight = brightFieldStageMapDto.RowCellHeight,
                brightFieldStageMapDtoColumnCellWidth = brightFieldStageMapDto.ColumnCellWidth,
            }), HtmlLogUniqueId.LoggingHtml());

            var expandStageMapDto = CalibrationAlgorithmService.ExpandStageMapDto(darkFieldStageMap, brightFieldStageMapDto, HtmlLogUniqueId);
            ResultChuckDarkFieldStageMapDto.ExpandStageMapDto = expandStageMapDto;
            OnPropertyChanged(nameof(ResultChuckDarkFieldStageMapDto.ExpandStageMapDto));

            var middleFileDateTimeFormat = DateTimeHelper.DateTime2String(DateTime.Now, Constants.MiddleFileDateTimeFormat);
            ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.IdealCsvFilePath = $"{CsvFileDirectory}\\Expand\\{middleFileDateTimeFormat}\\Ideal_Guid({HtmlLogUniqueId}).csv";
            ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.RealCsvFilePath = $"{CsvFileDirectory}\\Expand\\{middleFileDateTimeFormat}\\Real_Guid({HtmlLogUniqueId}).csv";
            ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.RealIsInWaferOkCsvFilePath = $"{CsvFileDirectory}\\Expand\\{middleFileDateTimeFormat}\\RealIsInWafer_Guid({HtmlLogUniqueId}).csv";
            ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.RealIsMatchOkCsvFilePath = $"{CsvFileDirectory}\\Expand\\{middleFileDateTimeFormat}\\RealIsMatchOk_Guid({HtmlLogUniqueId}).csv";
            ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.ErrorCsvFilePath = $"{CsvFileDirectory}\\Expand\\{middleFileDateTimeFormat}\\Error_Guid({HtmlLogUniqueId}).csv";
            ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.SaveIdealCsv(ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.IdealCsvFilePath);
            ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.SaveRealCsv(ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.RealCsvFilePath);
            ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.SaveIsInWaferOkCsv(ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.RealIsInWaferOkCsvFilePath);
            ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.SaveIsMatchOkCsv(ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.RealIsMatchOkCsvFilePath);
            ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.SaveErrorCsv(ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.ErrorCsvFilePath);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                expandStageMapDtoRowNumber = expandStageMapDto.RowNumber,
                expandStageMapDtoColumnNumber = expandStageMapDto.ColumnNumber,
                expandStageMapDtoRowCellHeight = expandStageMapDto.RowCellHeight,
                expandStageMapDtoColumnCellWidth = expandStageMapDto.ColumnCellWidth,
                ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.IdealCsvFilePath,
                ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.RealCsvFilePath,
                ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.RealIsInWaferOkCsvFilePath,
                ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.RealIsMatchOkCsvFilePath,
                ResultChuckDarkFieldStageMapDto.ExpandStageMapDto.ErrorCsvFilePath
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        if (ReviewDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        var (isSuccess, errorMessage) = Cache.Verify();
        if (isSuccess == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Cache varify error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
            DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(() =>
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
                    Cache.MicroscopeMagnificationEnum,
                    Cache.AlgorithmTemplateTypeEnum,
                    Cache.CalculateContainRowMinCout,
                    Cache.CalculateContainColumnMinCount,
                    Cache.VerifyAlignmentThreshold,
                    Cache.VerifyGantryThreshold,
                    Cache.VerifyScaleThreshold,
                    Cache.FirstStageMapPosition
                }), HtmlLogUniqueId.LoggingHtml());

                StageViewModel.SetStageMap(ReviewDto.ExpandStageMapDto);
                StageViewModel.SetEnableStageMap(true);

                ReviewDto.IsVerifyDarkField = false;
                ReviewDto.IsVerifyBrightField = false;
                ReviewDto.IsVerified = false;

                ReviewDto.VerifyDarkFieldStageMap.Reset();
                ReviewDto.VerifyBrightFieldStageMap.Reset();
                OnPropertyChanged(nameof(ReviewDto.VerifyDarkFieldStageMap));
                OnPropertyChanged(nameof(ReviewDto.VerifyBrightFieldStageMap));

                #region 暗场验证

                Logger.LogHtmlInformation("1. Dark Field", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                GetStageMap(ReviewDto.VerifyDarkFieldStageMap, detectImageDirectory, () => OnPropertyChanged(nameof(ReviewDto.VerifyDarkFieldStageMap)), cancellationToken);

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
                    Cache.MicroscopeMagnificationEnum,
                    Cache.AlgorithmTemplateTypeEnum,
                    ReviewDto.VerifyDarkFieldStageMap.IdealCsvFilePath,
                    ReviewDto.VerifyDarkFieldStageMap.RealCsvFilePath,
                    ReviewDto.VerifyDarkFieldStageMap.RealIsInWaferOkCsvFilePath,
                    ReviewDto.VerifyDarkFieldStageMap.RealIsMatchOkCsvFilePath,
                    ReviewDto.VerifyDarkFieldStageMap.ErrorCsvFilePath
                });

                var tryCalculateStageMapError = CalibrationAlgorithmService.CalculateChuckStageMapError(
                    ReviewDto.VerifyDarkFieldStageMap,
                    HtmlLogUniqueId,
                    Cache.CalculateContainRowMinCout,
                    Cache.CalculateContainColumnMinCount,
                    Cache.VerifyAlignmentThreshold,
                    Cache.VerifyGantryThreshold,
                    Cache.VerifyScaleThreshold,
                    Cache.WaferDiameter);

                OnPropertyChanged(nameof(ReviewDto.VerifyDarkFieldStageMap));
                ReviewDto.VerifyDarkFieldStageMap.SaveErrorCsv(ReviewDto.VerifyDarkFieldStageMap.ErrorCsvFilePath);
                if (tryCalculateStageMapError == false)
                {
                    Logger.LogHtmlInformation("Calculate Stage Map Error Failed!", HtmlHeaderLevelEnum.Header3, htmlQuoteList, HtmlLogUniqueId.LoggingHtml());
                }

                var result = ReviewDto.VerifyDarkFieldStageMap.ErrorMatrix
                    .SelectMany(t => t)
                    .All(t => t.ToOriginLength < Cache.Threshold.ToOriginLength);
                ReviewDto.IsVerifyDarkField = result;

                Logger.LogHtmlInformation($"Dark Field Stage Map Verify {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, htmlQuoteList, HtmlLogUniqueId.LoggingHtml());

                if (result)
                    DialogWindowProvider.ShowDialog("Verify Dark Field OK");
                else
                    DialogWindowProvider.ShowDialog("Verify Dark Field Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                #endregion 暗场验证

                #region 明场验证

                Logger.LogHtmlInformation("1. Bright Field", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var alignmentResultDto = StageViewModel.Alignment(
                    AlignmentCacheBrightField.LowSite1,
                    AlignmentCacheBrightField.LowSite2,
                    AlignmentCacheBrightField.HighSite1,
                    AlignmentCacheBrightField.HighSite2,
                    AlignmentCacheBrightField.LowMag,
                    AlignmentCacheBrightField.HighMag,
                    AlignmentCacheBrightField.AlgorithmWaferTypeEnum);

                Logger.LogHtmlInformation("Bright Field Alignment Ok", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    alignmentResultDto.Degrees,
                }), HtmlLogUniqueId.LoggingHtml());

                chuckBrightFieldStageMapCalibrationViewModel.HtmlLogUniqueId = HtmlLogUniqueId;
                chuckBrightFieldStageMapCalibrationViewModel.MicroscopePixelSizeItems = MicroscopePixelSizeItems;
                chuckBrightFieldStageMapCalibrationViewModel.Cache = ChuckBrightFieldStageMapCache;
                chuckBrightFieldStageMapCalibrationViewModel.GetStageMap(ReviewDto.VerifyBrightFieldStageMap, detectImageDirectory, () => OnPropertyChanged(nameof(ReviewDto.VerifyBrightFieldStageMap)), cancellationToken);

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
                    Cache.MicroscopeMagnificationEnum,
                    Cache.AlgorithmTemplateTypeEnum,
                    ReviewDto.VerifyBrightFieldStageMap.IdealCsvFilePath,
                    ReviewDto.VerifyBrightFieldStageMap.RealCsvFilePath,
                    ReviewDto.VerifyBrightFieldStageMap.RealIsInWaferOkCsvFilePath,
                    ReviewDto.VerifyBrightFieldStageMap.RealIsMatchOkCsvFilePath,
                    ReviewDto.VerifyBrightFieldStageMap.ErrorCsvFilePath
                });

                tryCalculateStageMapError = CalibrationAlgorithmService.CalculateChuckStageMapError(
                    ReviewDto.VerifyBrightFieldStageMap,
                    HtmlLogUniqueId,
                    ChuckBrightFieldStageMapCache.CalculateContainRowMinCout,
                    ChuckBrightFieldStageMapCache.CalculateContainColumnMinCount,
                    ChuckBrightFieldStageMapCache.VerifyAlignmentThreshold,
                    ChuckBrightFieldStageMapCache.VerifyGantryThreshold,
                    ChuckBrightFieldStageMapCache.VerifyScaleThreshold,
                    ChuckBrightFieldStageMapCache.WaferDiameter);

                OnPropertyChanged(nameof(ReviewDto.VerifyBrightFieldStageMap));

                ReviewDto.VerifyBrightFieldStageMap.SaveErrorCsv(ReviewDto.VerifyBrightFieldStageMap.ErrorCsvFilePath);
                if (tryCalculateStageMapError == false)
                {
                    Logger.LogHtmlInformation("Calculate Stage Map Error Failed!", HtmlHeaderLevelEnum.Header3, htmlQuoteList, HtmlLogUniqueId.LoggingHtml());
                }

                result = ReviewDto.VerifyBrightFieldStageMap.ErrorMatrix
                    .SelectMany(t => t)
                    .All(t => t.ToOriginLength < Cache.Threshold.ToOriginLength);
                ReviewDto.IsVerifyBrightField = result;

                Logger.LogHtmlInformation($"Bright Field Stage Map Verify {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, htmlQuoteList, HtmlLogUniqueId.LoggingHtml());

                if (result)
                    DialogWindowProvider.ShowDialog("Bright Field Verify OK");
                else
                    DialogWindowProvider.ShowDialog("Bright Field Verify Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                #endregion 明场验证

                ReviewDto.IsVerified = ReviewDto.IsVerifyDarkField && ReviewDto.IsVerifyBrightField;
                if (Save(ReviewDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    ReviewDto.IsVerified = false;
                    return false;
                }

                if (ReviewDto.IsVerified)
                    DialogWindowProvider.ShowDialog("Verify OK");
                else
                    DialogWindowProvider.ShowDialog("Verify Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return ReviewDto.IsVerified;
            }
            finally
            {
                StageViewModel.SetEnableStageMap(false);
            }
        }).ConfigureAwait(false);
    }

    private void GetStageMap(StageMapDto stageMapDto, string detectImageDirectory, Action notifyAction, CancellationToken cancellationToken)
    {
        var templateXId = HalconHelper.EmptyHTuple;
        var templateYId = HalconHelper.EmptyHTuple;
        var templateId = HalconHelper.EmptyHTuple;

        if (Cache.AlgorithmTemplateTypeEnum == AlgorithmTemplateTypeEnum.Projection)
        {
            if (CalibrationAlgorithmService.TryReadProjectionTemplate(Cache.TemplateFilePath, out templateXId, out templateYId) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment("Read Template Failed!"), HtmlLogUniqueId.LoggingHtml());
                return;
            }
        }
        else
        {
            if (CalibrationAlgorithmService.TryReadTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.TemplateFilePath, out templateId) == false)
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

            for (var row = 0; row < idealStageMapItemMatrix.Length; row++)
            {
                var isInWaferRowList = idealStageMapItemMatrix[row]
                    .Select((t, i) => (Index: i, Item: t))
                    .Where(t => t.Item.IsInWafer)
                    .ToList();
                var points = isInWaferRowList.Select(t => t.Item.Point).ToList();
                if (points.Count == 0) continue;

                Logger.LogHtmlInformation($"{row + 1} row", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                var darkImageRepeatList = new List<List<DarkFieldImageDto>>();
                foreach (var _ in Enumerable.Range(1, Cache.RepeatCount))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var rowDarkFieldImageDtoList = LaserViewModel.GetChuckDarkFieldRowLineScanImage(
                        points,
                        (false, CalibrationSetting.SettingCommonParam.MainCoefficient),
                        false,
                        Cache.XWidthPixel,
                        Cache.OpticsMagTypeEnum,
                        Cache.StageSpeedEnum,
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

                var plotDic = new Dictionary<(int RepeatIndex, int ColIndex), Point>();
                for (var column = 0; column < idealStageMapItemMatrix[row].Length; column++)
                {
                    var stageMapItem = idealStageMapItemMatrix[row][column];
                    if (stageMapItem.IsInWafer == false) continue;

                    Logger.LogHtmlInformation($"{column + 1} column", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());
                    cancellationToken.ThrowIfCancellationRequested();

                    stageMapItem.Reset();
                    stageMapItem.TemplateFilePath = Cache.TemplateFilePath;
                    stageMapItem.TemplateImageFilePath = Cache.TemplateImageFilePath;

                    try
                    {
                        foreach (var (index, rowDarkFieldImageDtoList) in darkImageRepeatList.Select((t, i) => (Index: i, RowDarkFieldImageDtoList: t)))
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            using var darkFieldImageDto = rowDarkFieldImageDtoList.ElementAt(column - isInWaferRowList[0].Index);
                            var ySizePerPixel = LaserPixelSizeItems.Single(t => t.PmtId == CalibrationConstantsHelper.MainPmtId && t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum && t.IsOk).YPixelSize;
                            var xSizePerPixel = LaserXPixelSizeItems.Single(t => t.OpticsMagTypeEnum == Cache.OpticsMagTypeEnum && t.XStageSpeedEnum == Cache.StageSpeedEnum && t.IsOk).XPixelSize;
                            var originImageFilePath = $"{detectImageDirectory}\\row({row})_col({column})_index({index})_Guid({HtmlLogUniqueId}_{Guid.NewGuid()}).jpg";
                            HalconHelper.Save(darkFieldImageDto.Image, originImageFilePath);

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
                                    Cache.OpticsMagTypeEnum,
                                    CalibrationConstantsHelper.MainStageSpeedEnum,
                                    point,
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
                                Cache.OpticsMagTypeEnum,
                                CalibrationConstantsHelper.MainStageSpeedEnum,
                                point,
                                offset,
                                actualOffset,
                                OriginPosition = stageMapItem.Point,
                                HtmlTab = new HtmlTab(new
                                {
                                    OriginImage = new HtmlImage(originImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(point)]),
                                    TemplateImage = new HtmlImage(Cache.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
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

    private bool Save(ChuckDarkFieldStageMapDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        dto.MicroscopeMagnificationEnum = ChuckBrightFieldStageMap.MicroscopeMagnificationEnum;
        dto.OpticsMagTypeEnum = Cache.OpticsMagTypeEnum;
        dto.StageSpeedEnum = Cache.StageSpeedEnum;

        Calibration = dto.Clone();

        return CacheProvider.Set(dto, cancellationToken)
               && RecipeCacheProvider.Set(Cache, cancellationToken)
               && EnableDependedCalibrationItems(cancellationToken);
    });

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependDarkStageMapCalibrations(false, cancellationToken, out var errorMsg) == false)
        {
            Logger.LogError("Toggle {@Name} Enable Status Failed!", errorMsg);
            return false;
        }

        return true;
    }

    #endregion 校准
}