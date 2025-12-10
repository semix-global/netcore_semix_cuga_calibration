using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.CenterAndTheta;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using Local.NoSQL.DB.Providers.Extensions;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.Helper;

namespace CugaCalibration.ViewModels.Chuck;

[IOCAppService(ServiceType = typeof(ChuckPrealignerCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckPrealignerCalibrationViewModel(EFEMWindowViewModel efemWindowViewModel, FindWaferCenterWindowFieldViewModel findWaferCenterWindowFieldViewModel) : CalibrationViewModelBase
{
    #region 属性

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Center offset" },
        new() { StepName = "Low MarkSite1" },
        new() { StepName = "Low MarkSite2" },
        new() { StepName = "High MarkSite1" },
        new() { StepName = "High MarkSite2" },
        new() { StepName = "Calibration Result" }
    ];

    #region 界面相关

    [ObservableProperty]
    private ChuckPrealignerObjDto _calibratingItem = new();

    [ObservableProperty]
    private bool _isReviewLoadWafer;

    #endregion 界面相关

    #region Review

    [ObservableProperty]
    private ChuckPrealignerObjDto? _reviewDto;

    #endregion Review

    #region 缓存

    [ObservableProperty]
    private ChuckPrealignerCache _cache = new();

    [ObservableProperty]
    private ChuckPrealignerObjDto _calibration = new();

    [ObservableProperty]
    private ChuckCenterAndThetaItemDto _chuckCenter = new();

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务重载

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

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckCenterAndThetaItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<ChuckPrealignerCache>();
        Calibration = CacheProvider.GetOrDefault<ChuckPrealignerObjDto>();
        CalibrationStepList[0].StepIsNextEnable = false;

        if (Cache.LowMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.LowMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();
        if (Cache.HighMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.HighMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.HighMicroscopeLensInformation.Clone();

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        Cache.FindWaferCenterOffset1 = Point.Origin;
        Cache.FindWaferCenterOffset2 = Point.Origin;
        Cache.FindWaferCenterOffset3 = Point.Origin;
        Cache.FindWaferCenterOffset4 = Point.Origin;
        Cache.FindWaferCenterOffset5 = Point.Origin;
        Cache.FindWaferCenterOffset6 = Point.Origin;
        Cache.FindWaferCenterOffset7 = Point.Origin;
        Cache.FindWaferCenterOffset8 = Point.Origin;
        Cache.WaferCenterThumb1 = [];
        Cache.WaferCenterThumb2 = [];
        Cache.WaferCenterThumb3 = [];
        Cache.WaferCenterThumb4 = [];
        Cache.WaferCenterThumb5 = [];
        Cache.WaferCenterThumb6 = [];
        Cache.WaferCenterThumb7 = [];
        Cache.WaferCenterThumb8 = [];
        IsReviewLoadWafer = false;
        if (await ReloadWaferAsync(IsReviewLoadWafer) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Please Reload Wafer!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        if (IsRecipeCalibrate)
            if (await AutomationRecipeInformationAsync() == false)
                return false;
        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewDto = Calibration.Clone();

        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                StageViewModel.SetAbsoluteStageTheta(0);
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowSite1.Location);
                return true;
            case 1:
                Cache.LowSite2.Location = Cache.LowSite1.Location + (Vector)new Point(Cache.DiePitchWidth * Cache.ReticleDieCountX, 0);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowSite2.Location);
                return true;

            case 2:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                Cache.HighSite1.Location = Cache.LowSite1.Location;
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.HighSite1.Location);
                return true;

            case 3:
                Cache.HighSite2.Location = Cache.LowSite2.Location;
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.HighSite2.Location);
                return true;

            case 5:
                CalibratingItem.IsCalibrated = true;
                if (Save(CalibratingItem, cancellationToken) == false)
                {
                    CalibratingItem.IsCalibrated = false;
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
            case 2:
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowSite1.Location);
                return true;

            case 3:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowSite2.Location);
                return true;

            case 4:
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.HighSite1.Location);
                return true;

            case 5:
                StageViewModel.SetAbsoluteStageTheta(0);
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.HighSite2.Location);
                return true;

            default:
                return true;
        }
    }

    #endregion 控制校准业务重载

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    public async Task<bool> Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
        await InvokeCalibrateAsync(() =>
        {
            StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);
            List<Point> waferEdgeOffsets =
            [
                Cache.FindWaferCenterOffset1,
                Cache.FindWaferCenterOffset2,
                Cache.FindWaferCenterOffset3,
                Cache.FindWaferCenterOffset4,
                Cache.FindWaferCenterOffset5,
                Cache.FindWaferCenterOffset6,
                Cache.FindWaferCenterOffset7,
                Cache.FindWaferCenterOffset8
            ];
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                PositionErrorThreshold = Cache.VerifyPositionThreshold
            }), HtmlLogUniqueId.LoggingHtml());
            var (offsetPosition, bitmapMemoryBytes) = StageViewModel.FindWaferCenterByManually(Point.Origin, waferEdgeOffsets);
            Cache.OffsetPosition = offsetPosition;

            if (bitmapMemoryBytes.Count > 0)
            {
                Cache.WaferCenterThumb1 = bitmapMemoryBytes[0];
                Cache.WaferCenterThumb2 = bitmapMemoryBytes[1];
                Cache.WaferCenterThumb3 = bitmapMemoryBytes[2];
                Cache.WaferCenterThumb4 = bitmapMemoryBytes[3];
                Cache.WaferCenterThumb5 = bitmapMemoryBytes[4];
                Cache.WaferCenterThumb6 = bitmapMemoryBytes[5];
                Cache.WaferCenterThumb7 = bitmapMemoryBytes[6];
                Cache.WaferCenterThumb8 = bitmapMemoryBytes[7];
                SaveWaferCenterThumbImages(bitmapMemoryBytes);
            }

            if (Math.Abs(Cache.OffsetPosition.X) > Cache.TeachingPositionThreshold || Math.Abs(Cache.OffsetPosition.Y) > Cache.TeachingPositionThreshold)
            {
                CalibrationStepList[CalibrationStepIndex].StepIsNextEnable = false;

                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    PositionThreshold = Cache.TeachingPositionThreshold,
                    OffsetX = Cache.OffsetPosition.X,
                    OffsetY = Cache.OffsetPosition.Y
                }), HtmlLogUniqueId.LoggingHtml());

                DialogWindowProvider.ShowDialog("Chuck Prealigner calibration failed,offset result out of the threshold, please manually adjust EFEM.", DialogButtonsEnum.OK, DialogIconEnum.Error);
                result = false;
                return result;
            }

            var (xDirection, yDirection) = StageViewModel.GetMachineDirection();
            var efemLoadWaferStagePosition = StageViewModel.GetEfemLoadWaferMachineStagePosition();
            CalibratingItem.OffsetPosition = Cache.OffsetPosition;
            CalibratingItem.EfemLoadWaferStagePosition = efemLoadWaferStagePosition;
            CalibratingItem.NewEfemLoadWaferStagePosition = new Point(efemLoadWaferStagePosition.X - xDirection * Cache.OffsetPosition.X, efemLoadWaferStagePosition.Y - yDirection * Cache.OffsetPosition.Y);

            Logger.LogHtmlInformation("Init center result OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                CalibratingItem.OffsetPosition,
                CalibratingItem.EfemLoadWaferStagePosition,
                OffsetPositionCalibrationResult = CalibratingItem.NewEfemLoadWaferStagePosition,
                Cache.FindWaferCenterOffset1,
                Cache.FindWaferCenterOffset2,
                Cache.FindWaferCenterOffset3,
                Cache.FindWaferCenterOffset4,
                Cache.FindWaferCenterOffset5,
                Cache.FindWaferCenterOffset6,
                Cache.FindWaferCenterOffset7,
                Cache.FindWaferCenterOffset8
            }), HtmlLogUniqueId.LoggingHtml());
            StageViewModel.SetBrightFieldAbsoluteStageXy(new Point(0, 0));
            return result;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
        await InvokeCalibrateAsync(() =>
        {
            var position = StageViewModel.GetBrightFieldStagePosition();
            Cache.LowSite1.Location = position;
            var lowTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.LowMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            Cache.LowSiteTemplateFilePath = lowTemplateFilePath;

            var generateTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.LowSiteTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
            if (generateTemplate == false)
            {
                DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var lowTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(lowTemplateFilePath);

            var resultLowSite1 = StageViewModel.MarkAlignSite1(Cache.LowSizeEnum, Cache.AlgorithmTemplateTypeEnum, Cache.AlgorithmWaferTypeEnum);
            if (resultLowSite1.Template is null)
            {
                result = false;
                return false;
            }

            BitmapSourceHelper.Save(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(resultLowSite1.Template.Thumb), lowTemplateImageFilePath);
            Cache.LowSite1 = resultLowSite1;
            Cache.LowSite1.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
            Cache.LowSite1.TemplateMatchScoreThreshold = Cache.NccTypeTemplateMatchScoreThreshold;
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.AlgorithmTemplateTypeEnum,
                Cache.LowMicroscopeLensInformation.LensName,
                Cache.LowSite1.Location,
                HtmlTab = new HtmlTab(new
                {
                    LowTemplate = new HtmlImage(lowTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return result;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
        await InvokeCalibrateAsync(() =>
        {
            var position = StageViewModel.GetBrightFieldStagePosition();
            if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, position, Cache.LowMicroscopeLensInformation, Cache.LowSiteTemplateFilePath, out var lowPositionResult) == false) return false;

            Cache.LowSite2.Location = lowPositionResult;
            var resultLowSite2 = StageViewModel.MarkAlignSite2(Cache.LowSite1, Cache.AlgorithmWaferTypeEnum);
            Cache.LowSite2 = resultLowSite2;
            Cache.LowSite2.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
            Cache.LowSite2.TemplateMatchScoreThreshold = Cache.NccTypeTemplateMatchScoreThreshold;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.AlgorithmTemplateTypeEnum,
                Cache.LowMicroscopeLensInformation.LensName,
                Cache.LowSite2.Location
            }), HtmlLogUniqueId.LoggingHtml());
            return result;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
        await InvokeCalibrateAsync(() =>
        {
            if (Cache.HighMicroscopeLensInformation.LensCode <= Cache.LowMicroscopeLensInformation.LensCode)
            {
                DialogWindowProvider.ShowDialog("The high magnification less than or equal low magnification! Please select correct magnification!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var position = StageViewModel.GetBrightFieldStagePosition();
            Cache.HighSite1.Location = position;

            var highTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.HighMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            Cache.HighSiteTemplateFilePath = highTemplateFilePath;

            var generateTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.HighSiteTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
            if (generateTemplate == false)
            {
                DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var highTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(highTemplateFilePath);

            var resultHighSite1 = StageViewModel.MarkAlignSite1(Cache.HighSizeEnum, Cache.AlgorithmTemplateTypeEnum, Cache.AlgorithmWaferTypeEnum);
            if (resultHighSite1.Template is null)
            {
                result = false;
                return false;
            }

            BitmapSourceHelper.Save(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(resultHighSite1.Template.Thumb), highTemplateImageFilePath);
            Cache.HighSite1 = resultHighSite1;
            Cache.HighSite1.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
            Cache.HighSite1.TemplateMatchScoreThreshold = Cache.NccTypeTemplateMatchScoreThreshold;
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.HighMicroscopeLensInformation.LensName,
                Cache.HighSite1.Location,
                HtmlTab = new HtmlTab(new
                {
                    HighTemplate = new HtmlImage(highTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return result;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step4CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
        await InvokeCalibrateAsync(() =>
        {
            var position = StageViewModel.GetBrightFieldStagePosition();
            if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, position, Cache.HighMicroscopeLensInformation, Cache.HighSiteTemplateFilePath, out var highPositionResult) == false) return false;

            Cache.HighSite2.Location = highPositionResult;
            var resultHighSite2 = StageViewModel.MarkAlignSite2(Cache.HighSite1, Cache.AlgorithmWaferTypeEnum);
            Cache.HighSite2 = resultHighSite2;
            Cache.HighSite2.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
            Cache.HighSite2.TemplateMatchScoreThreshold = Cache.NccTypeTemplateMatchScoreThreshold;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.HighMicroscopeLensInformation.LensName,
                Cache.HighSite2.Location
            }), HtmlLogUniqueId.LoggingHtml());
            return result;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step5CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
        await InvokeCalibrateAsync(async () =>
        {
            StageViewModel.SetAbsoluteStageTheta(0);
            if (await P5CalibrateActionAsync(cancellationToken) == false)
            {
                result = false;
                return result;
            }

            CalibratingItem.EfemLoadWaferChuckAbsoluteAngle = Cache.Degrees;
            Logger.LogHtmlInformation("Result Ok", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                CalibratingItem.OffsetPosition,
                CalibratingItem.EfemLoadWaferStagePosition,
                CenterOffsetCalibrationResult = CalibratingItem.NewEfemLoadWaferStagePosition,
                AngleOffsetCalibrationResult = CalibratingItem.EfemLoadWaferChuckAbsoluteAngle
            }), HtmlLogUniqueId.LoggingHtml());

            if (IsAutoCalibrate == false)
                DialogWindowProvider.ShowDialog($"Chuck Prealigner Calibration {(result ? "Success" : "Failed")}!", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);
            return result;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> VerifyActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await InvokeVerifyAsync(async () =>
        {
            IsReviewLoadWafer = true;
            if (ReviewDto is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Please select a review item!"), HtmlLogUniqueId.LoggingHtml());
                result = false;
                return result;
            }

            if (!Calibration.IsOk)
            {
                if (await ReloadWaferAsync(IsReviewLoadWafer) == false)
                {
                    DialogWindowProvider.ShowDialog("Please Reload Wafer!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Please Reload Wafer!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }
            }

            result = await VerifyCalibrationAsync(ReviewDto, cancellationToken);
            if (IsAutoCalibrate == false)
                DialogWindowProvider.ShowDialog($"Chuck Prealigner Verify {(result ? "Success" : "Failed")}!", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);
            return result;
        }).ConfigureAwait(false);
        return result;
    }

    private async Task<bool> VerifyCalibrationAsync(ChuckPrealignerObjDto? selectChuckPrealignerObjDto, CancellationToken cancellationToken)
    {
        var result = true;
        await Task.Run(async () =>
        {
            if (selectChuckPrealignerObjDto is null)
            {
                DialogWindowProvider.ShowDialog("Please Calibration First!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Please Calibration First!"), HtmlLogUniqueId.LoggingHtml());
                result = false;
                return false;
            }

            selectChuckPrealignerObjDto.IsVerified = false;

            if (await P5CalibrateActionAsync(cancellationToken, true) == false)
            {
                result = false;
                return false;
            }

            RecipeCacheProvider.Set(Cache, cancellationToken);

            StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

            var (offsetPosition, _) = StageViewModel.FindWaferCenterByManually(Point.Origin);

            result = Math.Abs(offsetPosition.X) < Cache.VerifyPositionThreshold
                     && Math.Abs(offsetPosition.Y) < Cache.VerifyPositionThreshold;

            Cache.OffsetPosition = offsetPosition;

            Logger.LogHtmlInformation($"Verify {(result ? "Success" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                VerifyPositionOffsetThreshold = Cache.VerifyPositionThreshold,
                VerifyAngleThreshold = Cache.VerifyDegreesThreshold,
                TeachingOffsetPosition = selectChuckPrealignerObjDto.OffsetPosition,
                TeachingAngle = selectChuckPrealignerObjDto.EfemLoadWaferChuckAbsoluteAngle,
                VerifyOffsetPosition = offsetPosition,
                VerifyAngle = Cache.Degrees
            }), HtmlLogUniqueId.LoggingHtml());

            if (result == false)
            {
                DialogWindowProvider.ShowDialog("Verify Chuck Prealigner calibration failed.Position Error Out Of The Threshold!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return result;
            }

            selectChuckPrealignerObjDto.IsVerified = true;
            if (Save(selectChuckPrealignerObjDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                selectChuckPrealignerObjDto.IsVerified = false;
                result = false;
                return result;
            }

            Logger.LogHtmlInformation("Verify Chuck Prealigner calibration OK", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                PositionThreshold = Cache.TeachingPositionThreshold,
                PositionErrorThreshold = Cache.VerifyPositionThreshold,
                AngleThreshold = Cache.VerifyDegreesThreshold,
                OldOffsetPosition = selectChuckPrealignerObjDto.OffsetPosition,
                VerifyOffsetPosition = offsetPosition,
                VerifyOffsetAngle = Cache.Degrees
            }), HtmlLogUniqueId.LoggingHtml());

            return result;
        }, cancellationToken);
        return result;
    }

    private async Task<bool> P5CalibrateActionAsync(CancellationToken cancellationToken, bool isReview = false)
    {
        var result = false;
        await Task.Run(() =>
        {
            var alignmentResultDto = StageViewModel.AlignmentVerify(
                Cache.LowSite1,
                Cache.LowSite2,
                Cache.HighSite1,
                Cache.HighSite2,
                Cache.LowMicroscopeLensInformation,
                Cache.HighMicroscopeLensInformation,
                Cache.AlgorithmWaferTypeEnum);
            Cache.Degrees = alignmentResultDto.Degrees;
            result = Math.Abs(Cache.Degrees) < (isReview ? Cache.VerifyDegreesThreshold : Cache.TeachingDegreesThreshold);

            Logger.LogHtmlInformation("P5 Result", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                AlignmentAngleOffsetResult = Cache.Degrees,
                AlignmentThreshold = Cache.TeachingDegreesThreshold,
                AlignmentVerifyThreshold = Cache.VerifyDegreesThreshold,
                LowMagnification = Cache.LowMicroscopeLensInformation.LensName,
                HighMagnification = Cache.HighMicroscopeLensInformation.LensName,
                Cache.AlgorithmWaferTypeEnum,
                LowLocation1 = Cache.LowSite1.Location,
                LowLocation2 = Cache.LowSite2.Location,
                HighLocation1 = Cache.HighSite1.Location,
                HighLocation2 = Cache.HighSite2.Location
            }), HtmlLogUniqueId.LoggingHtml());
            if (result == false)
            {
                DialogWindowProvider.ShowDialog($"P5 Failed! The Result Out Of Threshold!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: P5 Failed! The Result Out Of Threshold!"), HtmlLogUniqueId.LoggingHtml());
            }
        }, cancellationToken);
        return result;
    }

    // 此方法逻辑慎重修改，谨慎调试
    private async Task<bool> ReloadWaferAsync(bool isReviewLoadWafer)
    {
        var result = false;
        await Task.Run(() =>
        {
            efemWindowViewModel.IsPrealigner = true;
            efemWindowViewModel.PrealignerIsOk = false;

            efemWindowViewModel.OffsetAngle = 0;
            efemWindowViewModel.OffsetPoint = Point.Origin;

            if (isReviewLoadWafer)
            {
                var reviewDto = GuardUtils.IsNotNullAndReturn(ReviewDto);
                if (Math.Abs(reviewDto.EfemLoadWaferChuckAbsoluteAngle) > Cache.TeachingDegreesThreshold)
                {
                    DialogWindowProvider.ShowDialog("The angle of the EFEM load wafer chuck is out of the threshold!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                if (Math.Abs(reviewDto.OffsetPosition.X) > Cache.TeachingPositionThreshold || Math.Abs(reviewDto.OffsetPosition.Y) > Cache.TeachingPositionThreshold)
                {
                    DialogWindowProvider.ShowDialog("The offset of the EFEM load wafer chuck is out of the threshold!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                var (xDirection, yDirection) = StageViewModel.GetMachineDirection();
                var point = new Point(-xDirection * reviewDto.OffsetPosition.X, -yDirection * reviewDto.OffsetPosition.Y);
                efemWindowViewModel.OffsetPoint = point;
                efemWindowViewModel.OffsetAngle = reviewDto.EfemLoadWaferChuckAbsoluteAngle;
            }

            WindowManagerService.ShowDialog(efemWindowViewModel);

            efemWindowViewModel.IsPrealigner = false;

            result = efemWindowViewModel.SelectedFoupItem != null
                     && efemWindowViewModel is { PrealignerIsOk: true, SelectedFoupItem.IsLoadWafer: true };

            Logger.LogHtmlInformation(result ? "ReloadWafer OK" : "ReloadWafer Failed", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                AngleErrorThreshold = Cache.TeachingDegreesThreshold,
                PositionThreshold = Cache.TeachingPositionThreshold,
                efemWindowViewModel.OffsetAngle,
                efemWindowViewModel.OffsetPoint
            }), HtmlLogUniqueId.LoggingHtml());

            return result;
        }).ConfigureAwait(false);

        return result;
    }

    private async Task<bool> FindWaferCenterAsync()
    {
        var result = false;
        await Task.Run(() =>
        {
            findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.WaferCenterThumb1 = Cache.WaferCenterThumb1;
            findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.WaferCenterThumb2 = Cache.WaferCenterThumb2;
            findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.WaferCenterThumb3 = Cache.WaferCenterThumb3;
            findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.WaferCenterThumb4 = Cache.WaferCenterThumb4;
            findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.WaferCenterThumb5 = Cache.WaferCenterThumb5;
            findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.WaferCenterThumb6 = Cache.WaferCenterThumb6;
            findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.WaferCenterThumb7 = Cache.WaferCenterThumb7;
            findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.WaferCenterThumb8 = Cache.WaferCenterThumb8;
            findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.OffsetPosition = Cache.OffsetPosition;

            findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.PositionErrorThreshold = Cache.VerifyPositionThreshold;
            WindowManagerService.ShowDialog(findWaferCenterWindowFieldViewModel);
            result = findWaferCenterWindowFieldViewModel.IsFindWaferCenterOffsetPositionEnabled;
            var waferCenterThumbList = new List<byte[]>
            {
                findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.WaferCenterThumb1,
                findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.WaferCenterThumb2,
                findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.WaferCenterThumb3,
                findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.WaferCenterThumb4,
                findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.WaferCenterThumb5,
                findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.WaferCenterThumb6,
                findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.WaferCenterThumb7,
                findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.WaferCenterThumb8
            };
            SaveWaferCenterThumbImages(waferCenterThumbList);
            Cache.FindWaferCenterOffset1 = findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.FindWaferCenterOffset1;
            Cache.FindWaferCenterOffset2 = findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.FindWaferCenterOffset2;
            Cache.FindWaferCenterOffset3 = findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.FindWaferCenterOffset3;
            Cache.FindWaferCenterOffset4 = findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.FindWaferCenterOffset4;
            Cache.FindWaferCenterOffset5 = findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.FindWaferCenterOffset5;
            Cache.FindWaferCenterOffset6 = findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.FindWaferCenterOffset6;
            Cache.FindWaferCenterOffset7 = findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.FindWaferCenterOffset7;
            Cache.FindWaferCenterOffset8 = findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.FindWaferCenterOffset8;
            Cache.OffsetPosition = findWaferCenterWindowFieldViewModel.AlignmentFindCenterCache.OffsetPosition;
            CalibratingItem.OffsetPosition = Cache.OffsetPosition;
            var (xDirection, yDirection) = StageViewModel.GetMachineDirection();
            var efemLoadWaferStagePosition = StageViewModel.GetEfemLoadWaferMachineStagePosition();
            CalibratingItem.EfemLoadWaferStagePosition = efemLoadWaferStagePosition;
            CalibratingItem.NewEfemLoadWaferStagePosition = new Point(efemLoadWaferStagePosition.X - xDirection * Cache.OffsetPosition.X, efemLoadWaferStagePosition.Y - yDirection * Cache.OffsetPosition.Y);
            Logger.LogHtmlInformation(result ? "find wafer center result OK" : "find wafer center result failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.OffsetPosition,
                CalibratingItem.EfemLoadWaferStagePosition,
                OffsetPositionCalibrationResult = CalibratingItem.NewEfemLoadWaferStagePosition,
                Cache.FindWaferCenterOffset1,
                Cache.FindWaferCenterOffset2,
                Cache.FindWaferCenterOffset3,
                Cache.FindWaferCenterOffset4,
                Cache.FindWaferCenterOffset5,
                Cache.FindWaferCenterOffset6,
                Cache.FindWaferCenterOffset7,
                Cache.FindWaferCenterOffset8
            }), HtmlLogUniqueId.LoggingHtml());
            if (!result) Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Find wafer center Offset Position failed!"), HtmlLogUniqueId.LoggingHtml());
            return result;
        }).ConfigureAwait(false);

        return result;
    }

    private void SaveWaferCenterThumbImages(List<byte[]> bitmapMemoryBytes)
    {
        for (var i = 0; i < bitmapMemoryBytes.Count; i++)
        {
            var waferCenterThumbPath = $"{ImageFileDirectory}\\WaferCenterThumb\\WaferCenterThumb{i + 1}_Guid{HtmlLogUniqueId}.jpg";
            var waferCenterThumbBitmapSource = BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(bitmapMemoryBytes[i]);
            BitmapSourceHelper.Save(waferCenterThumbBitmapSource, waferCenterThumbPath);
            Logger.LogHtmlInformation($"Find center edge image {i}", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                HtmlTab = new HtmlTab(new
                {
                    WaferCenterThumb = new HtmlImage(waferCenterThumbPath, htmlImageOverlays: [new HtmlImageCrossOverlay(false)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
        }
    }

    private bool Save(ChuckPrealignerObjDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        dto.LowMicroscopeLensInformation = Cache.LowMicroscopeLensInformation;
        dto.HighMicroscopeLensInformation = Cache.HighMicroscopeLensInformation;

        Calibration = dto.Clone();

        CacheProvider.Set(dto, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    }) && EnableDependedCalibrationItems(cancellationToken);

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependPrealignerCalibrations(false, cancellationToken, out var errorMsg) == false)
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
            new() { StepName = "loading" },
            new() { StepName = "Init offset" },
            new() { StepName = "Find offset" },
            new() { StepName = "Calibration Result" },
            new() { StepName = "Reload Wafer" },
            new() { StepName = "Review" }
        ];
    }

    public override async Task<bool> AutomationActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            GetAutoCalibrationStep();
            await base.AutomationActionAsync(cancellationToken);
            var result = true;
            foreach (var stepItem in AutoCalibrationStepList.Select((t, index) => (t, index)))
            {
                switch (stepItem.index)
                {
                    case 0:
                        if (await LoadedingAsync(cancellationToken) == false) return false;
                        if (await InvokeCalibrateAsync(() =>
                            {
                                Logger.LogHtmlInformation("Initialize Y offset", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                                {
                                    AngleThreshold = Cache.VerifyDegreesThreshold
                                }), HtmlLogUniqueId.LoggingHtml());

                                return result;
                            }) == false) return false;
                        IsReviewLoadWafer = false;
                        if (await ReloadWaferAsync(IsReviewLoadWafer) == false)
                        {
                            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Please Reload Wafer!"), HtmlLogUniqueId.LoggingHtml());
                            return false;
                        }

                        break;

                    case 1:
                        if (await Step0CalibrateActionAsync(cancellationToken) == false) return false;
                        break;

                    case 2:
                        if (await InvokeCalibrateAsync(async () =>
                            {
                                if (await FindWaferCenterAsync() == false) return false;
                                return result;
                            }) == false) return false;
                        break;

                    case 3:
                        if (await AutomationRecipeInformationAsync() == false) return false;
                        if (await Step5CalibrateActionAsync(cancellationToken) == false) return false;
                        CalibrationStepIndex = 5;
                        if (await NextingAsync(cancellationToken) == false) return false;
                        break;

                    case 4:
                        if (await InvokeCalibrateAsync(async () =>
                            {
                                IsReviewLoadWafer = true;
                                if (await ReloadWaferAsync(IsReviewLoadWafer) == false)
                                {
                                    DialogWindowProvider.ShowDialog("Please Reload Wafer!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Please Reload Wafer!"), HtmlLogUniqueId.LoggingHtml());
                                    return false;
                                }

                                return result;
                            }) == false) return false;
                        break;

                    case 5:
                        if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false) return false;
                        await InvokeCalibrateAsync(async () =>
                        {
                            ReviewDto = Calibration.Clone();
                            if (await VerifyCalibrationAsync(ReviewDto, cancellationToken) == false) return false;
                            return result;
                        });
                        break;
                }

                if (await AutoNextingAsync() == false) return false;
                AutoCalibrationProgress = AutoCalibrationStepIndex / (double)AutoCalibrationStepList.Count * 100;
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Auto {Name} Calibration Failed!");
            return false;
        }
    }

    public override async Task<bool> AutomationRecipeInformationAsync(string position = "")
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (CalibrationRecipeService.GetCorrectWaferMapByOffset(true) == false)
            return false;
        Cache.LowMicroscopeLensInformation = AlignmentCacheBrightField.LowMag;
        Cache.HighMicroscopeLensInformation = AlignmentCacheBrightField.HighMag;
        var lowTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.LowMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
        var lowTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(lowTemplateFilePath);
        var highTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.HighMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
        var highTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(highTemplateFilePath);
        Cache.LowSite1.Location = AlignmentCacheBrightField.LowSite1.Location;
        Cache.LowSite2.Location = AlignmentCacheBrightField.LowSite2.Location;
        Cache.HighSite1.Location = AlignmentCacheBrightField.HighSite1.Location;
        Cache.HighSite2.Location = AlignmentCacheBrightField.HighSite2.Location;
        Cache.LowSite1 = AlignmentCacheBrightField.LowSite1;
        Cache.LowSite2 = AlignmentCacheBrightField.LowSite2;
        Cache.HighSite1 = AlignmentCacheBrightField.HighSite1;
        Cache.HighSite2 = AlignmentCacheBrightField.HighSite2;
        BitmapSourceHelper.Save(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(Cache.LowSite1.Template!.Thumb), lowTemplateImageFilePath);
        BitmapSourceHelper.Save(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(Cache.HighSite1.Template!.Thumb), highTemplateImageFilePath);
        Cache.LowSite1.TemplateMatchScoreThreshold = AlignmentCacheBrightField.LowSite1.TemplateMatchScoreThreshold;
        Cache.LowSite2.TemplateMatchScoreThreshold = AlignmentCacheBrightField.LowSite2.TemplateMatchScoreThreshold;
        Cache.HighSite1.TemplateMatchScoreThreshold = AlignmentCacheBrightField.HighSite1.TemplateMatchScoreThreshold;
        Cache.HighSite2.TemplateMatchScoreThreshold = AlignmentCacheBrightField.HighSite2.TemplateMatchScoreThreshold;
        Cache.AlgorithmWaferTypeEnum = AlignmentCacheBrightField.AlgorithmWaferTypeEnum;
        return true;
    }

    private async Task<bool> AutoNextingAsync()
    {
        await Task.Run(() =>
        {
            //CalibrationStepName = AutoCalibrationStepList[AutoCalibrationStepIndex].StepName.ToString();
            AutoCalibrationStepIndex++;
            Task.Delay(2000).Wait();
        });
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

        var result = false;
        await InvokeVerifyAsync(async () =>
        {
            try
            {
                IsReviewLoadWafer = true;
                ReviewDto = Calibration.Clone();
                if (await AutomationRecipeInformationAsync() == false) return false;
                if (await VerifyCalibrationAsync(ReviewDto!, cancellationToken) == false)
                {
                    DialogWindowProvider.ShowDialog($"Auto Calibration Review Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                result = true;
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Review Failed! Error massage:{ex.Message}"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
        });

        AutoCalibrationProgress = (AutoCalibrationStepIndex + 1) / (double)AutoCalibrationStepList.Count * 100;
        return result;
    }

    #endregion 自动化校准
}