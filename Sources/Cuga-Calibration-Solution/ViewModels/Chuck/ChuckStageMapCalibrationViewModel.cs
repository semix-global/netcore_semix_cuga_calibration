using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.CenterAndTheta;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.CIB.LineCentricity;
using Core.Models.Models.CIB.XPixelSize;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Microscope.PixelSize;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.SQL.Cache.Providers.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
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

    [RecipeCache]
    [ObservableProperty]
    private ChuckStageMapCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private ChuckStageMapDto _calibration = new();

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    [ObservableProperty]
    private AlignmentCacheDarkField _alignmentCacheDarkField = new();

    [ObservableProperty]
    private AlignmentCacheDarkField[] _alignmentCacheDarkFields = [];

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

    [ObservableProperty]
    private ChuckCenterAndThetaItemDto _chuckCenter = new();

    [ObservableProperty]
    private ChuckGlobalScaleErrorDto _chuckGlobalScaleError = new();

    [ObservableProperty]
    private CIBYPixelSizeDTO[] _laserPixelSizeItems = [];

    [ObservableProperty]
    private CIBLineCentricityDTO[] _laserLineCentricityItems = [];

    [ObservableProperty]
    private CIBXPixelSizeDTO[] _cIBXPixelSizeItems = [];

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        MicroscopePixelSizeItems = CalibrationStatusService.GetCalibrations<MicroscopePixelSizeItemDto>();

        ChuckCenter = CalibrationStatusService.GetCalibration<ChuckCenterAndThetaItemDto>();

        CIBXPixelSizeItems = CalibrationStatusService.GetCalibrations<CIBXPixelSizeDTO>();

        LaserPixelSizeItems = CalibrationStatusService.GetCalibrations<CIBYPixelSizeDTO>();

        LaserLineCentricityItems = CalibrationStatusService.GetCalibrations<CIBLineCentricityDTO>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<ChuckStageMapCache>();
        Calibration = CacheProvider.GetOrDefault<ChuckStageMapDto>();
        AlignmentCacheDarkFields = RecipeCacheProvider.GetOrDefaultArray<AlignmentCacheDarkField>();
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
                CIBProfileTypeEnum = Cache.CIBConfiguration.CIBProfileMode,
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.CIBConfiguration.ToHtmlAnonymous())
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
                AlignmentCacheDarkField = AlignmentCacheDarkFields.SingleOrDefault(t =>
                                              t.OpticsIlluminationModeEnum == Cache.OpticsIlluminationModeEnum &&
                                              t.ProductivityInformation == Cache.ProductivityInformation)
                                          ?? new AlignmentCacheDarkField();
                if (AlignmentCacheDarkField.IsOk)
                {
                    alignmentResultDto = StageViewModel.AlignmentDarkField(
                        AlignmentCacheDarkField.LowSite1,
                        AlignmentCacheDarkField.LowSite2,
                        AlignmentCacheDarkField.HighSite1,
                        AlignmentCacheDarkField.HighSite2,
                        Cache.ProductivityInformation,
                        AlignmentCacheDarkField.LowMag,
                        AlignmentCacheDarkField.AlgorithmWaferTypeEnum,
                        opticsIlluminationModeEnum: Cache.OpticsIlluminationModeEnum);
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
                if (BrightFieldStep1Action() == false) return false;
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

    private bool BrightFieldStep1Action()
    {
        Cache.TemplateFilePath = Cache.BrightFieldTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.HighMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
        var generateTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.TemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
        if (generateTemplate == false)
        {
            DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        Cache.TemplateImageFilePath = Cache.BrightFieldTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.TemplateFilePath);

        var centerPosition = StageViewModel.GetMachineStagePosition();
        Cache.FirstStageMapPosition = Cache.BrightFieldFirstStageMapPosition = HostEnvironment.IsDevelopment()
            ? ChuckCenter.NewBFCenterStagePosition
            : centerPosition;

        return true;
    }

    private async Task<bool> DarkFieldStep1ActionAsync()
    {
        var brightFieldPosition = StageViewModel.GetBrightFieldStagePosition();
        var centerPosition = StageViewModel.DarkFieldToMachinePosition(brightFieldPosition);

        var laserLineCentricityItemDto = LaserLineCentricityItems.Single(t => t.PmtId == CalibrationConstantsHelper.MainPmtId
                                                                              && t.ProductivityInformation == Cache.ProductivityInformation);

        var darkFieldImageDto = await CIBViewModel.GetPMTImageAsync(
            Cache.ProductivityInformation,
            StageCoordinateSystemEnum.Bright,
            brightFieldPosition,
            Cache.XWidthPixel,
            CalibrationSetting.SettingCommonParam.MainCIBInformation,
            (false, CalChipSiteModelEnum.ChuckModel),
            (false, Cache.OpticsConfiguration),
            (false, Cache.CIBConfiguration),
            (false, CalibrationSetting.SettingCommonParam.MainLaserLightInformation),
            false,
            CancellationToken.None);
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
            ? laserLineCentricityItemDto.DFMachineCenterPosition
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
                ? laserLineCentricityItemDto.DFMachineCenterPosition
                : Cache.DarkFieldFirstStageMapPosition,
            laserLineCentricityItemDto.DFMachineCenterPosition,
            Cache.WaferDiameter);

        OnPropertyChanged(nameof(ResultChuckStageMapDto.CalibrationDarkFieldStageMap));
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
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
                await DarkFieldGetStageMapAsync(ResultChuckStageMapDto.CalibrationDarkFieldStageMap, detectImageDirectory, () => OnPropertyChanged(nameof(ResultChuckStageMapDto.CalibrationDarkFieldStageMap)), cancellationToken);
            }

            var calibrationStageMap = Cache.IsDarkField == false
                ? ResultChuckStageMapDto.CalibrationBrightFieldStageMap
                : ResultChuckStageMapDto.CalibrationDarkFieldStageMap;

            var middleFileDateTimeFormat = DateTimeHelper.DateTime2String(DateTime.Now, Constants.MiddleFileDateTimeFormat);
            calibrationStageMap.IdealCsvFilePath = $@"{CsvFileDirectory}\Calibration\{opticsMode}\{middleFileDateTimeFormat}\Ideal_Guid({HtmlLogUniqueId}).csv";
            calibrationStageMap.RealCsvFilePath = $@"{CsvFileDirectory}\Calibration\{opticsMode}\{middleFileDateTimeFormat}\Real_Guid({HtmlLogUniqueId}).csv";
            calibrationStageMap.RealIsInWaferOkCsvFilePath = $@"{CsvFileDirectory}\Calibration\{opticsMode}\{middleFileDateTimeFormat}\RealIsInWafer_Guid({HtmlLogUniqueId}).csv";
            calibrationStageMap.RealIsMatchOkCsvFilePath = $@"{CsvFileDirectory}\Calibration\{opticsMode}\{middleFileDateTimeFormat}\RealIsMatchOk_Guid({HtmlLogUniqueId}).csv";
            calibrationStageMap.ErrorCsvFilePath = $@"{CsvFileDirectory}\Calibration\{opticsMode}\{middleFileDateTimeFormat}\Error_Guid({HtmlLogUniqueId}).csv";
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
            ResultChuckStageMapDto.ExpandStageMapDto.IdealCsvFilePath = $@"{CsvFileDirectory}\Expand\{middleFileDateTimeFormat}\Ideal_Guid({HtmlLogUniqueId}).csv";
            ResultChuckStageMapDto.ExpandStageMapDto.RealCsvFilePath = $@"{CsvFileDirectory}\Expand\{middleFileDateTimeFormat}\Real_Guid({HtmlLogUniqueId}).csv";
            ResultChuckStageMapDto.ExpandStageMapDto.RealIsInWaferOkCsvFilePath = $@"{CsvFileDirectory}\Expand\{middleFileDateTimeFormat}\RealIsInWafer_Guid({HtmlLogUniqueId}).csv";
            ResultChuckStageMapDto.ExpandStageMapDto.RealIsMatchOkCsvFilePath = $@"{CsvFileDirectory}\Expand\{middleFileDateTimeFormat}\RealIsMatchOk_Guid({HtmlLogUniqueId}).csv";
            ResultChuckStageMapDto.ExpandStageMapDto.ErrorCsvFilePath = $@"{CsvFileDirectory}\Expand\{middleFileDateTimeFormat}\Error_Guid({HtmlLogUniqueId}).csv";
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
                        Cache.ProductivityInformation,
                        AlignmentCacheDarkField.LowMag,
                        AlignmentCacheDarkField.AlgorithmWaferTypeEnum,
                        opticsIlluminationModeEnum: Cache.OpticsIlluminationModeEnum);

                Logger.LogHtmlInformation("Get Stage Map", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                await DarkFieldGetStageMapAsync(ReviewDto.VerifyDarkFieldStageMap, detectImageDirectory, () => OnPropertyChanged(nameof(ReviewDto.VerifyDarkFieldStageMap)), cancellationToken, true);

                var middleFileDateTimeFormat = DateTimeHelper.DateTime2String(DateTime.Now, Constants.MiddleFileDateTimeFormat);
                ReviewDto.VerifyDarkFieldStageMap.IdealCsvFilePath = $@"{CsvFileDirectory}\ReviewDarkField\{middleFileDateTimeFormat}\Ideal_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyDarkFieldStageMap.RealCsvFilePath = $@"{CsvFileDirectory}\ReviewDarkField\{middleFileDateTimeFormat}\Real_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyDarkFieldStageMap.RealIsInWaferOkCsvFilePath = $@"{CsvFileDirectory}\ReviewDarkField\{middleFileDateTimeFormat}\RealIsInWafer_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyDarkFieldStageMap.RealIsMatchOkCsvFilePath = $@"{CsvFileDirectory}\ReviewDarkField\{middleFileDateTimeFormat}\RealIsMatchOk_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyDarkFieldStageMap.ErrorCsvFilePath = $@"{CsvFileDirectory}\ReviewDarkField\{middleFileDateTimeFormat}\Error_Guid({HtmlLogUniqueId}).csv";
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

                Logger.LogHtmlInformation("Get Stage Map", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                BrightFieldGetStageMap(ReviewDto.VerifyBrightFieldStageMap, detectImageDirectory, () => OnPropertyChanged(nameof(ReviewDto.VerifyBrightFieldStageMap)), cancellationToken);

                middleFileDateTimeFormat = DateTimeHelper.DateTime2String(DateTime.Now, Constants.MiddleFileDateTimeFormat);
                ReviewDto.VerifyBrightFieldStageMap.IdealCsvFilePath = $@"{CsvFileDirectory}\ReviewBrightField\{middleFileDateTimeFormat}\Ideal_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyBrightFieldStageMap.RealCsvFilePath = $@"{CsvFileDirectory}\ReviewBrightField\{middleFileDateTimeFormat}\Real_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyBrightFieldStageMap.RealIsInWaferOkCsvFilePath = $@"{CsvFileDirectory}\ReviewBrightField\{middleFileDateTimeFormat}\RealIsInWafer_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyBrightFieldStageMap.RealIsMatchOkCsvFilePath = $@"{CsvFileDirectory}\ReviewBrightField\{middleFileDateTimeFormat}\RealIsMatchOk_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyBrightFieldStageMap.ErrorCsvFilePath = $@"{CsvFileDirectory}\ReviewBrightField\{middleFileDateTimeFormat}\Error_Guid({HtmlLogUniqueId}).csv";
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

                DialogWindowProvider.ShowDialog($"Verify Bright Field {(result ? "OK" : "Failed")}!", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);

                #endregion 明场验证

                ReviewDto.IsVerified = ReviewDto.IsVerifyDarkField && ReviewDto.IsVerifyBrightField;
                if (Save(ReviewDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    ReviewDto.IsVerified = false;
                    return false;
                }

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

    private async Task DarkFieldGetStageMapAsync(StageMapDto stageMapDto, string detectImageDirectory, Action notifyAction, CancellationToken cancellationToken, bool isReview = false)
    {
        if (isReview) Guard.IsNotNull(ReviewDto);

        var (xDirection, _) = StageViewModel.GetMachineDirection();

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

            var darkImageRepeatList = new List<IReadOnlyList<DarkFieldImageDTO>>();
            foreach (var _ in Enumerable.Range(1, Cache.RepeatCount))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var rowDarkFieldImageDtoList = await CIBViewModel.GetPMTImagesAsync(
                        Cache.ProductivityInformation,
                        StageCoordinateSystemEnum.Machine,
                        points,
                        Cache.XWidthPixel,
                        CalibrationSetting.SettingCommonParam.MainCIBInformation,
                        (false, CalChipSiteModelEnum.ChuckModel),
                        (false, Cache.OpticsConfiguration),
                        (false, Cache.CIBConfiguration),
                        (false, CalibrationSetting.SettingCommonParam.MainLaserLightInformation),
                        false,
                        cancellationToken);
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

                        if (CIBViewModel.TryGetMatchPosition(
                                Cache.ProductivityInformation,
                                StageCoordinateSystemEnum.Machine,
                                stageMapItem.Point,
                                CalibrationSetting.SettingCommonParam.MainCIBInformation,
                                Cache.AlgorithmTemplateTypeEnum,
                                darkFieldImageDto,
                                Cache.DarkFieldTemplateFilePath,
                                FileHelper.GetFileFullName(Cache.DarkFieldTemplateFilePath),
                                HtmlLogUniqueId,
                                out var position,
                                out _,
                                out _,
                                out var resultImageFilePath) == false)
                        {
                            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header6, new HtmlComment("Error: Get Match Position Failed!"), HtmlLogUniqueId.LoggingHtml());
                            continue;
                        }

                        var actualOffset = (Point)(position - stageMapItem.Point);

                        plotDic.Add((index, column), actualOffset);

                        stageMapItem.FilePath = resultImageFilePath;
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

    #endregion 校准
}