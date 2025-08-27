using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.BrightFieldStageMap;
using Core.Models.Models.Chuck.Center;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;

namespace CugaCalibration.ViewModels.Chuck;

[IOCAppService(ServiceType = typeof(ChuckBrightFieldStageMapCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckBrightFieldStageMapCalibrationViewModel(AlignmentWindowBrightFieldViewModel alignmentWindowBrightFieldViewModel) : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "P5" },
        new() { StepName = "Param" },
        new() { StepName = "Find Start Point" },
        new() { StepName = "Stage Map" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ChuckBrightFieldStageMapDto _resultChuckBrightFieldStageMapDto = new();

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ChuckBrightFieldStageMapDto? _reviewDto;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private ChuckBrightFieldStageMapCache _cache = new();

    [ObservableProperty]
    private ChuckBrightFieldStageMapDto _calibration = new();

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    [ObservableProperty]
    private ChuckCenterObjDto _chuckCenter = new();

    [ObservableProperty]
    private ChuckGlobalScaleErrorDto _chuckGlobalScaleError = new();

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

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

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<ChuckBrightFieldStageMapCache>();
        Calibration = CacheProvider.GetOrDefault<ChuckBrightFieldStageMapDto>();
        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();

        if (Cache.MicroscopeLensInformation.LensCode == -1)
            Cache.MicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformationList.Count <= 2
                ? ApplicationCookie.MicroscopeLensInformationList[^1]
                : ApplicationCookie.MicroscopeLensInformationList[2];

        return isHasCache || RecipeCacheProvider.Set(Cache, cancellationToken);
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (AlignmentCacheBrightField.IsOk == false)
        {
            var showDialog = WindowManagerService.ShowDialog(alignmentWindowBrightFieldViewModel);
            if (showDialog == false)
            {
                DialogWindowProvider.ShowDialog("Alignment Setting is Empty", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            AlignmentCacheBrightField = alignmentWindowBrightFieldViewModel.Cache;
        }

        StageViewModel.SetBrightFieldAbsoluteStageXy(AlignmentCacheBrightField.LowSite1.Location);
        StageViewModel.SetEnableStageMap(false);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewDto = Calibration.Clone();
        OnPropertyChanged(nameof(ReviewDto.VerifyStageMap));

        return ReviewDto.IsCalibrated;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
                return true;

            case 1:
            case 2:
                StageViewModel.SetMachineAbsoluteStageXy(Cache.FirstStageMapPosition);
                return true;

            case 3:
                ResultChuckBrightFieldStageMapDto.IsCalibrated = true;
                ResultChuckBrightFieldStageMapDto.VerifyStageMap = ResultChuckBrightFieldStageMapDto.CalibrationStageMap.Clone();
                if (Save(ResultChuckBrightFieldStageMapDto, cancellationToken) == false)
                {
                    ResultChuckBrightFieldStageMapDto.IsCalibrated = false;
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

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                LowSite1 = AlignmentCacheBrightField.LowSite1.Location,
                LowSite2 = AlignmentCacheBrightField.LowSite2.Location,
                HighSite1 = AlignmentCacheBrightField.HighSite1.Location,
                HighSite2 = AlignmentCacheBrightField.HighSite2.Location
            }), HtmlLogUniqueId.LoggingHtml());

            StageViewModel.SetEnableStageMap(false);

            var alignmentResultDto = StageViewModel.Alignment(AlignmentCacheBrightField.LowSite1, AlignmentCacheBrightField.LowSite2, AlignmentCacheBrightField.HighSite1, AlignmentCacheBrightField.HighSite2,
                AlignmentCacheBrightField.LowMag, AlignmentCacheBrightField.HighMag,
                AlignmentCacheBrightField.AlgorithmWaferTypeEnum);

            Cache.P5Angle = alignmentResultDto.Degrees;

            StageViewModel.SetEnableStageMap(false);

            Logger.LogHtmlInformation("P5 OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new { Cache.P5Angle }), HtmlLogUniqueId.LoggingHtml());

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
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Please set the stage map!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            var centerPosition = StageViewModel.GetMachineStagePosition();

            ResultChuckBrightFieldStageMapDto.CalibrationStageMap = new StageMapDto(Cache.RowNumber, Cache.ColumnNumber, Cache.RowCellHeight, Cache.ColumnCellWidth);
            ResultChuckBrightFieldStageMapDto.CalibrationStageMap.GenerateByCenterPosition(
                HostEnvironment.IsDevelopment()
                    ? ChuckCenter.NewBFCenterStagePosition
                    : centerPosition,
                ChuckCenter.NewBFCenterStagePosition,
                Cache.WaferDiameter);

            Cache.FirstStageMapPosition = HostEnvironment.IsDevelopment()
                ? ChuckCenter.NewBFCenterStagePosition
                : centerPosition;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.RowNumber,
                Cache.ColumnNumber,
                Cache.RowCellHeight,
                Cache.ColumnCellWidth,
                Cache.WaferDiameter,
                Cache.AlgorithmTemplateTypeEnum
            }), HtmlLogUniqueId.LoggingHtml());

            OnPropertyChanged(nameof(ResultChuckBrightFieldStageMapDto));

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Cache.TemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.MicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            var generateTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.TemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
            if (generateTemplate == false)
            {
                DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
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

            ResultChuckBrightFieldStageMapDto.CalibrationStageMap.Reset();
            OnPropertyChanged(nameof(ResultChuckBrightFieldStageMapDto));

            var (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                xAxisTemperature,
                yAxisTemperature,
                reviewCamTemperature,
                cibTemperature,
                LensName = Cache.MicroscopeLensInformation.LensName,
                Cache.CalculateContainRowMinCount,
                Cache.CalculateContainColumnMinCount,
                Cache.CalibrationAlignmentThreshold,
                Cache.CalibrationGantryThreshold,
                Cache.CalibrationScaleThreshold,
                Cache.FirstStageMapPosition,
                ImageFileDirectory = detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            GetStageMap(ResultChuckBrightFieldStageMapDto.CalibrationStageMap, detectImageDirectory, () => OnPropertyChanged(nameof(ResultChuckBrightFieldStageMapDto)), cancellationToken);

            var middleFileDateTimeFormat = DateTimeHelper.DateTime2String(DateTime.Now, Constants.MiddleFileDateTimeFormat);
            ResultChuckBrightFieldStageMapDto.CalibrationStageMap.IdealCsvFilePath = $"{CsvFileDirectory}\\Calibration\\{middleFileDateTimeFormat}\\Ideal_Guid({HtmlLogUniqueId}).csv";
            ResultChuckBrightFieldStageMapDto.CalibrationStageMap.RealCsvFilePath = $"{CsvFileDirectory}\\Calibration\\{middleFileDateTimeFormat}\\Real_Guid({HtmlLogUniqueId}).csv";
            ResultChuckBrightFieldStageMapDto.CalibrationStageMap.RealIsInWaferOkCsvFilePath = $"{CsvFileDirectory}\\Calibration\\{middleFileDateTimeFormat}\\RealIsInWafer_Guid({HtmlLogUniqueId}).csv";
            ResultChuckBrightFieldStageMapDto.CalibrationStageMap.RealIsMatchOkCsvFilePath = $"{CsvFileDirectory}\\Calibration\\{middleFileDateTimeFormat}\\RealIsMatchOk_Guid({HtmlLogUniqueId}).csv";
            ResultChuckBrightFieldStageMapDto.CalibrationStageMap.ErrorCsvFilePath = $"{CsvFileDirectory}\\Calibration\\{middleFileDateTimeFormat}\\Error_Guid({HtmlLogUniqueId}).csv";
            ResultChuckBrightFieldStageMapDto.CalibrationStageMap.SaveIdealCsv(ResultChuckBrightFieldStageMapDto.CalibrationStageMap.IdealCsvFilePath);
            ResultChuckBrightFieldStageMapDto.CalibrationStageMap.SaveRealCsv(ResultChuckBrightFieldStageMapDto.CalibrationStageMap.RealCsvFilePath);
            ResultChuckBrightFieldStageMapDto.CalibrationStageMap.SaveIsInWaferOkCsv(ResultChuckBrightFieldStageMapDto.CalibrationStageMap.RealIsInWaferOkCsvFilePath);
            ResultChuckBrightFieldStageMapDto.CalibrationStageMap.SaveIsMatchOkCsv(ResultChuckBrightFieldStageMapDto.CalibrationStageMap.RealIsMatchOkCsvFilePath);

            var tryCalculateStageMapError = CalibrationAlgorithmService.CalculateChuckStageMapError(
                ResultChuckBrightFieldStageMapDto.CalibrationStageMap,
                HtmlLogUniqueId,
                Cache.CalculateContainRowMinCount,
                Cache.CalculateContainColumnMinCount,
                Cache.CalibrationAlignmentThreshold,
                Cache.CalibrationGantryThreshold,
                Cache.CalibrationScaleThreshold,
                Cache.WaferDiameter);

            OnPropertyChanged(nameof(ResultChuckBrightFieldStageMapDto));
            ResultChuckBrightFieldStageMapDto.CalibrationStageMap.SaveErrorCsv(ResultChuckBrightFieldStageMapDto.CalibrationStageMap.ErrorCsvFilePath);

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
                    LensName = Cache.MicroscopeLensInformation.LensName,
                    Cache.AlgorithmTemplateTypeEnum,
                    ResultChuckBrightFieldStageMapDto.CalibrationStageMap.IdealCsvFilePath,
                    ResultChuckBrightFieldStageMapDto.CalibrationStageMap.RealCsvFilePath,
                    ResultChuckBrightFieldStageMapDto.CalibrationStageMap.RealIsMatchOkCsvFilePath,
                    ResultChuckBrightFieldStageMapDto.CalibrationStageMap.ErrorCsvFilePath
                }), HtmlLogUniqueId.LoggingHtml());
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                xAxisTemperature,
                yAxisTemperature,
                reviewCamTemperature,
                LensName = Cache.MicroscopeLensInformation.LensName,
                Cache.AlgorithmTemplateTypeEnum,
                ResultChuckBrightFieldStageMapDto.CalibrationStageMap.IdealCsvFilePath,
                ResultChuckBrightFieldStageMapDto.CalibrationStageMap.RealCsvFilePath,
                ResultChuckBrightFieldStageMapDto.CalibrationStageMap.RealIsMatchOkCsvFilePath,
                ResultChuckBrightFieldStageMapDto.CalibrationStageMap.ErrorCsvFilePath
            }), HtmlLogUniqueId.LoggingHtml());

            return tryCalculateStageMapError;
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
                    LensName = Cache.MicroscopeLensInformation.LensName,
                    Cache.AlgorithmTemplateTypeEnum,
                    Cache.CalculateContainRowMinCount,
                    Cache.CalculateContainColumnMinCount,
                    Cache.VerifyAlignmentThreshold,
                    Cache.VerifyGantryThreshold,
                    Cache.VerifyScaleThreshold,
                    Cache.FirstStageMapPosition,
                    HtmlTab = new HtmlTab(new
                    {
                        TemplateImage = new HtmlImage(Cache.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    }),
                    ImageFileDirectory = detectImageDirectory
                }), HtmlLogUniqueId.LoggingHtml());

                StageViewModel.SetStageMap(ReviewDto.CalibrationStageMap);
                StageViewModel.SetEnableStageMap(true);

                ReviewDto.IsVerified = false;

                ReviewDto.VerifyStageMap.Reset();
                OnPropertyChanged(nameof(ReviewDto.VerifyStageMap));

                GetStageMap(ReviewDto.VerifyStageMap, detectImageDirectory, () => OnPropertyChanged(nameof(ReviewDto.VerifyStageMap)), cancellationToken);

                var middleFileDateTimeFormat = DateTimeHelper.DateTime2String(DateTime.Now, Constants.MiddleFileDateTimeFormat);
                ReviewDto.VerifyStageMap.IdealCsvFilePath = $"{CsvFileDirectory}\\Review\\{middleFileDateTimeFormat}\\Ideal_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyStageMap.RealCsvFilePath = $"{CsvFileDirectory}\\Review\\{middleFileDateTimeFormat}\\Real_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyStageMap.RealIsInWaferOkCsvFilePath = $"{CsvFileDirectory}\\Review\\{middleFileDateTimeFormat}\\RealIsInWafer_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyStageMap.RealIsMatchOkCsvFilePath = $"{CsvFileDirectory}\\Review\\{middleFileDateTimeFormat}\\RealIsMatchOk_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyStageMap.ErrorCsvFilePath = $"{CsvFileDirectory}\\Review\\{middleFileDateTimeFormat}\\Error_Guid({HtmlLogUniqueId}).csv";
                ReviewDto.VerifyStageMap.SaveIdealCsv(ReviewDto.VerifyStageMap.IdealCsvFilePath);
                ReviewDto.VerifyStageMap.SaveRealCsv(ReviewDto.VerifyStageMap.RealCsvFilePath);
                ReviewDto.VerifyStageMap.SaveIsInWaferOkCsv(ReviewDto.VerifyStageMap.RealIsInWaferOkCsvFilePath);
                ReviewDto.VerifyStageMap.SaveIsMatchOkCsv(ReviewDto.VerifyStageMap.RealIsMatchOkCsvFilePath);

                (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();

                var htmlQuoteList = new HtmlQuote(new
                {
                    xAxisTemperature,
                    yAxisTemperature,
                    reviewCamTemperature,
                    cibTemperature,
                    LensName = Cache.MicroscopeLensInformation.LensName,
                    Cache.AlgorithmTemplateTypeEnum,
                    ReviewDto.VerifyStageMap.IdealCsvFilePath,
                    ReviewDto.VerifyStageMap.RealCsvFilePath,
                    ReviewDto.VerifyStageMap.RealIsInWaferOkCsvFilePath,
                    ReviewDto.VerifyStageMap.RealIsMatchOkCsvFilePath,
                    ReviewDto.VerifyStageMap.ErrorCsvFilePath
                });

                var tryCalculateStageMapError = CalibrationAlgorithmService.CalculateChuckStageMapError(
                    ReviewDto.VerifyStageMap,
                    HtmlLogUniqueId,
                    Cache.CalculateContainRowMinCount,
                    Cache.CalculateContainColumnMinCount,
                    Cache.VerifyAlignmentThreshold,
                    Cache.VerifyGantryThreshold,
                    Cache.VerifyScaleThreshold,
                    Cache.WaferDiameter);

                OnPropertyChanged(nameof(ReviewDto.VerifyStageMap));

                ReviewDto.VerifyStageMap.SaveErrorCsv(ReviewDto.VerifyStageMap.ErrorCsvFilePath);
                if (tryCalculateStageMapError == false)
                {
                    Logger.LogHtmlInformation("Calculate Stage Map Error Failed!", HtmlHeaderLevelEnum.Header3, htmlQuoteList, HtmlLogUniqueId.LoggingHtml());
                }

                var result = ReviewDto.VerifyStageMap.ErrorMatrix
                    .SelectMany(t => t)
                    .All(t => t.ToOriginLength < Cache.Threshold.ToOriginLength);
                ReviewDto.IsVerified = result;

                if (Save(ReviewDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    ReviewDto.IsVerified = false;
                    return false;
                }

                ReviewDto.IsVerified = result;

                Logger.LogHtmlInformation($"Stage Map Verify {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, htmlQuoteList, HtmlLogUniqueId.LoggingHtml());

                if (result)
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

    internal void GetStageMap(StageMapDto stageMapDto, string detectImageDirectory, Action notifyAction, CancellationToken cancellationToken)
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

                    Invoke(row, column);
                }
            }
            else
            {
                // Right to left
                for (var column = stageMapDto.ColumnNumber - 1; column > -1; column--)
                {
                    if (idealStageMapItemMatrix[row][column].IsInWafer == false) continue;

                    Invoke(row, column);
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
                stageMapItem.TemplateFilePath = Cache.TemplateFilePath;
                stageMapItem.TemplateImageFilePath = Cache.TemplateImageFilePath;

                StageViewModel.SetMachineAbsoluteStageXy(stageMapItem.Point);
                Thread.Sleep(500);
                var tempPosition = StageViewModel.GetBrightFieldStagePosition();
                if (ReviewViewModel.TryGetMatchPosition(
                        Cache.AlgorithmTemplateTypeEnum,
                        MicroscopePixelSizeItems,
                        tempPosition,
                        Cache.MicroscopeLensInformation,
                        Cache.TemplateFilePath,
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

    private bool Save(ChuckBrightFieldStageMapDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        dto.MicroscopeLensInformation = Cache.MicroscopeLensInformation;

        Calibration = dto.Clone();

        return CacheProvider.Set(dto, cancellationToken)
               && RecipeCacheProvider.Set(Cache, cancellationToken)
               && EnableDependedCalibrationItems(cancellationToken);
    });

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependBrightStageMapCalibrations(false, cancellationToken, out var errorMsg) == false)
        {
            Logger.LogError("Toggle {@Name} Enable Status Failed!", errorMsg);
            return false;
        }

        return true;
    }

    #endregion 校准
}