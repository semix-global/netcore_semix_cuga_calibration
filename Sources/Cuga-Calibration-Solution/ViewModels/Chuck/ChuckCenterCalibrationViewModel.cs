using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.Center;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;

namespace CugaCalibration.ViewModels.Chuck;

[IOCAppService(ServiceType = typeof(ChuckCenterCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckCenterCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "P5" },
        new() { StepName = "Get Template" },
        new() { StepName = "Find Positive Point" },
        new() { StepName = "Find Negative Point" },
        new() { StepName = "Get Chuck center position" }
    ];

    [ObservableProperty]
    private ChuckCenterObjDto _chuckCenterObjDto = new();

    [ObservableProperty]
    private ChuckCenterObjDto _selectChuckCenterObjDto = new();

    [ObservableProperty]
    private double _rotateAngle;

    #region Review

    [ObservableProperty]
    private ChuckCenterObjDto? _reviewDto;

    #endregion Review

    #region 缓存

    [ObservableProperty]
    private ChuckCenterCache _cache = new();

    [ObservableProperty]
    private ChuckCenterObjDto _calibration = new();

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    [ObservableProperty]
    private ChuckGantryDto _chuckGantry = new();

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

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

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<ChuckCenterCache>();
        Calibration = CacheProvider.GetOrDefault<ChuckCenterObjDto>();
        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();

        if (Cache.LowChuckCenterCacheItem.MagnificationInfo.MagnificationCode == -1) Cache.LowChuckCenterCacheItem.MagnificationInfo = ApplicationCookie.MicroscopeMagnificationInfoList[0];
        if (Cache.HighChuckCenterCacheItem.MagnificationInfo.MagnificationCode == -1)
            Cache.HighChuckCenterCacheItem.MagnificationInfo = ApplicationCookie.MicroscopeMagnificationInfoList
            [
                ApplicationCookie.MicroscopeMagnificationInfoList.Count <= 2
                    ? ApplicationCookie.MicroscopeMagnificationInfoList.Count - 1
                    : 2
            ];
        return isHasCache || RecipeCacheProvider.Set(Cache, cancellationToken);
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (IsRecipeCalibrate)
        {
            if (CalibrationRecipeService.GetCorrectWaferMapByOffset(true) == false) return false;
            Cache.ThetaAngle = StageViewModel.GetMachineStageTheta();
            if (await AutomationRecipeInformationAsync() == false) return false;
        }

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewDto = Calibration.Clone();
        if (ReviewDto.IsCalibrated == false) return false;

        StageViewModel.SetBrightFieldAbsoluteStageXy(ChuckCenterObjDto.ChuckCenterPosition);
        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0 or 1 or 2 or 3:
                return true;

            case 4:
                var isCalibrated = CalibrationStepIndex == 4;

                ChuckCenterObjDto.IsCalibrated = isCalibrated;
                if (Save(ChuckCenterObjDto, cancellationToken) == false)
                {
                    ChuckCenterObjDto.IsCalibrated = false;
                    Logger.LogError("{@Name} Error: Save Failed!", Name);
                    DialogWindowProvider.ShowDialog("Save Failed!", DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    return false;
                }

                IsCalibrated = isCalibrated;
                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务重载

    #region 校准

    [RelayCommand]
    private async Task GetPointAsync(string parameter)
    {
        try
        {
            Logger.LogInformation("{@Name}: Get Point Start", Name);
            await Task.Run(() =>
            {
                var result = StageViewModel.GetBrightFieldStagePosition();

                switch (parameter)
                {
                    case "LowTop":
                        Cache.LowChuckCenterCacheItem.TopPosition = result;
                        Cache.LowChuckCenterCacheItem.TopTemplateFilePath = $"{TemplateFileDirectory}\\LowTop-_{Cache.LowChuckCenterCacheItem.MagnificationInfo.MicroscopeMagnificationName}_{Guid.NewGuid()}";
                        var generateLowTopTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.LowChuckCenterCacheItem.TopTemplateFilePath, AlgorithmTemplateSizeEnum.Size256);
                        if (generateLowTopTemplate == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        else Cache.LowChuckCenterCacheItem.TopTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.LowChuckCenterCacheItem.TopTemplateFilePath);

                        break;

                    case "HighTop":
                        Cache.HighChuckCenterCacheItem.TopPosition = result;
                        Cache.HighChuckCenterCacheItem.TopTemplateFilePath = $"{TemplateFileDirectory}\\HighTop-_{Cache.HighChuckCenterCacheItem.MagnificationInfo.MicroscopeMagnificationName}_{Guid.NewGuid()}";
                        var generateHighTopTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.HighChuckCenterCacheItem.TopTemplateFilePath, AlgorithmTemplateSizeEnum.Size256);
                        if (generateHighTopTemplate == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        else Cache.HighChuckCenterCacheItem.TopTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.HighChuckCenterCacheItem.TopTemplateFilePath);

                        break;

                    case "LowRight":
                        Cache.LowChuckCenterCacheItem.RightPosition = result;
                        Cache.LowChuckCenterCacheItem.RightTemplateFilePath = $"{TemplateFileDirectory}\\LowRight-_{Cache.LowChuckCenterCacheItem.MagnificationInfo.MicroscopeMagnificationName}_{Guid.NewGuid()}";
                        var generateLowRightTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.LowChuckCenterCacheItem.RightTemplateFilePath, AlgorithmTemplateSizeEnum.Size256);
                        if (generateLowRightTemplate == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        else Cache.LowChuckCenterCacheItem.RightTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.LowChuckCenterCacheItem.RightTemplateFilePath);

                        break;

                    case "HighRight":
                        Cache.HighChuckCenterCacheItem.RightPosition = result;
                        Cache.HighChuckCenterCacheItem.RightTemplateFilePath = $"{TemplateFileDirectory}\\HighRight-_{Cache.HighChuckCenterCacheItem.MagnificationInfo.MicroscopeMagnificationName}_{Guid.NewGuid()}";
                        var generateHighRightTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.HighChuckCenterCacheItem.RightTemplateFilePath, AlgorithmTemplateSizeEnum.Size256);
                        if (generateHighRightTemplate == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        else Cache.HighChuckCenterCacheItem.RightTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.HighChuckCenterCacheItem.RightTemplateFilePath);

                        break;

                    case "LowBottom":
                        Cache.LowChuckCenterCacheItem.BottomPosition = result;
                        Cache.LowChuckCenterCacheItem.BottomTemplateFilePath = $"{TemplateFileDirectory}\\LowBottom-_{Cache.LowChuckCenterCacheItem.MagnificationInfo.MicroscopeMagnificationName}_{Guid.NewGuid()}";
                        var generateLowBottomLeftTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.LowChuckCenterCacheItem.BottomTemplateFilePath, AlgorithmTemplateSizeEnum.Size256);
                        if (generateLowBottomLeftTemplate == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        else Cache.LowChuckCenterCacheItem.BottomTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.LowChuckCenterCacheItem.BottomTemplateFilePath);

                        break;

                    case "HighBottom":
                        Cache.HighChuckCenterCacheItem.BottomPosition = result;
                        Cache.HighChuckCenterCacheItem.BottomTemplateFilePath = $"{TemplateFileDirectory}\\HighBottom-_{Cache.HighChuckCenterCacheItem.MagnificationInfo.MicroscopeMagnificationName}_{Guid.NewGuid()}";
                        var generateHighBottomTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.HighChuckCenterCacheItem.BottomTemplateFilePath, AlgorithmTemplateSizeEnum.Size256);
                        if (generateHighBottomTemplate == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        else Cache.HighChuckCenterCacheItem.BottomTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.HighChuckCenterCacheItem.BottomTemplateFilePath);

                        break;

                    case "LowLeft":
                        Cache.LowChuckCenterCacheItem.LeftPosition = result;
                        Cache.LowChuckCenterCacheItem.LeftTemplateFilePath = $"{TemplateFileDirectory}\\LowLeft-_{Cache.LowChuckCenterCacheItem.MagnificationInfo.MicroscopeMagnificationName}_{Guid.NewGuid()}";
                        var generateLowLeftTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.LowChuckCenterCacheItem.LeftTemplateFilePath, AlgorithmTemplateSizeEnum.Size256);
                        if (generateLowLeftTemplate == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        else Cache.LowChuckCenterCacheItem.LeftTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.LowChuckCenterCacheItem.LeftTemplateFilePath);

                        break;

                    case "HighLeft":
                        Cache.HighChuckCenterCacheItem.LeftPosition = result;
                        Cache.HighChuckCenterCacheItem.LeftTemplateFilePath = $"{TemplateFileDirectory}\\HighLeft-_{Cache.HighChuckCenterCacheItem.MagnificationInfo.MicroscopeMagnificationName}_{Guid.NewGuid()}";
                        var generateHighLeftTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.HighChuckCenterCacheItem.LeftTemplateFilePath, AlgorithmTemplateSizeEnum.Size256);
                        if (generateHighLeftTemplate == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        else Cache.HighChuckCenterCacheItem.LeftTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.HighChuckCenterCacheItem.LeftTemplateFilePath);

                        break;

                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(parameter));
                        break;
                }

                Logger.LogInformation("{@Name}: Get Point OK!", Name);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Point Failed", Name);
        }
    }

    [RelayCommand]
    private async Task<bool> GotoPointAsync(string parameter)
    {
        try
        {
            Logger.LogInformation("{@Name}: Move Point Start", Name);
            return await Task.Run(() =>
            {
                StageViewModel.SetBrightFieldAbsoluteStageXy(parameter switch
                {
                    "LowTop" => Cache.LowChuckCenterCacheItem.TopPosition,
                    "HighTop" => Cache.HighChuckCenterCacheItem.TopPosition,
                    "LowRight" => Cache.LowChuckCenterCacheItem.RightPosition,
                    "HighRight" => Cache.HighChuckCenterCacheItem.RightPosition,
                    "LowBottom" => Cache.LowChuckCenterCacheItem.BottomPosition,
                    "HighBottom" => Cache.HighChuckCenterCacheItem.BottomPosition,
                    "LowLeft" => Cache.LowChuckCenterCacheItem.LeftPosition,
                    "HighLeft" => Cache.HighChuckCenterCacheItem.LeftPosition,
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(parameter))
                });

                Logger.LogInformation("{@Name}: Move Point OK!", Name);
                return true;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
            return false;
        }
    }

    [RelayCommand]
    private async Task RotateAsync()
    {
        try
        {
            Logger.LogInformation("{@Name}: Rotate to theta Start", Name);
            await Task.Run(() =>
            {
                StageViewModel.SetAbsoluteStageTheta(RotateAngle);
                Logger.LogError("{@Name}: Rotate to theta Failed", Name);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Rotate to theta Failed", Name);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await InvokeCalibrateAsync(() =>
        {
            var alignmentResultDto = StageViewModel.Alignment(
                AlignmentCacheBrightField.LowSite1,
                AlignmentCacheBrightField.LowSite2,
                AlignmentCacheBrightField.HighSite1,
                AlignmentCacheBrightField.HighSite2,
                AlignmentCacheBrightField.LowMag,
                AlignmentCacheBrightField.HighMag,
                AlignmentCacheBrightField.AlgorithmWaferTypeEnum);

            Cache.P5Angle = alignmentResultDto.Degrees;
            Logger.LogHtmlInformation("P5 OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new { Cache.P5Angle }),
                HtmlLogUniqueId.LoggingHtml());
            result = true;
            return result;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.AlgorithmTemplateTypeEnum,
                Cache.WaferMaskTypeEnum,
                LowMagnification = Cache.LowChuckCenterCacheItem.MagnificationInfo.MicroscopeMagnificationName,
                HighMagnification = Cache.HighChuckCenterCacheItem.MagnificationInfo.MicroscopeMagnificationName,
                LowTopPosition = Cache.LowChuckCenterCacheItem.TopPosition,
                HighTopPosition = Cache.HighChuckCenterCacheItem.TopPosition,
                LowBottomPosition = Cache.LowChuckCenterCacheItem.BottomPosition,
                HighBottomPosition = Cache.HighChuckCenterCacheItem.BottomPosition,
                LowLeftPosition = Cache.LowChuckCenterCacheItem.LeftPosition,
                HighLeftPosition = Cache.HighChuckCenterCacheItem.LeftPosition,
                LowRightPosition = Cache.LowChuckCenterCacheItem.RightPosition,
                HighRightPosition = Cache.HighChuckCenterCacheItem.RightPosition,

                HtmlTab = new HtmlTab(new
                {
                    LowTopTemplate = new HtmlImage(Cache.LowChuckCenterCacheItem.TopTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    LowRightTemplate = new HtmlImage(Cache.LowChuckCenterCacheItem.RightTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    LowBottomTemplate = new HtmlImage(Cache.LowChuckCenterCacheItem.BottomTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    LowLeftTemplate = new HtmlImage(Cache.LowChuckCenterCacheItem.LeftTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),

                    HighTopTemplate = new HtmlImage(Cache.HighChuckCenterCacheItem.TopTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighRightTemplate = new HtmlImage(Cache.HighChuckCenterCacheItem.RightTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighBottomTemplate = new HtmlImage(Cache.HighChuckCenterCacheItem.BottomTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighLeftTemplate = new HtmlImage(Cache.HighChuckCenterCacheItem.LeftTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
        await InvokeCalibrateAsync(() =>
        {
            var (isSuccess, errorMessage) = Cache.Step1Verify();
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Cache varify error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                result = false;
                return false;
            }

            isSuccess = FindChuckCenterPosition(ChuckCenterObjDto, Cache.PositiveAngle, true);
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Find Positive Four Points Failed."), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog("Find Positive Four Points Failed.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                result = false;
                return false;
            }

            StageViewModel.SetAbsoluteStageTheta(0);
            return true;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
        await InvokeCalibrateAsync(() =>
        {
            var (isSuccess, errorMessage) = Cache.Step2Verify();
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Cache varify error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                result = false;
                return false;
            }

            isSuccess = FindChuckCenterPosition(ChuckCenterObjDto, Cache.NegativeAngle, false);
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Find Negative Four Points Failed."), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog("Find Negative Four Points Failed.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                result = false;
                return false;
            }

            StageViewModel.SetAbsoluteStageTheta(0);
            StageViewModel.SetBrightFieldAbsoluteStageXy(new Point(0, 0));
            return true;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step4CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
        await InvokeCalibrateAsync(() =>
        {
            var (isSuccess, errorMessage) = Cache.Verify();
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Cache varify error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                result = false;
                return false;
            }

            var chuckCenterPosition = Point.Origin;
            if (ChuckCenterObjDto.PositiveTopPosition != Point.Origin && ChuckCenterObjDto.NegativeTopPosition != Point.Origin && ChuckCenterObjDto.NegativeRightPosition != Point.Origin && ChuckCenterObjDto.PositiveRightPosition != Point.Origin && ChuckCenterObjDto.PositiveBottomPosition != Point.Origin && ChuckCenterObjDto.NegativeBottomPosition != Point.Origin &&
                ChuckCenterObjDto.NegativeLeftPosition != Point.Origin && ChuckCenterObjDto.PositiveLeftPosition != Point.Origin)
            {
                chuckCenterPosition = CalibrationAlgorithmService.GetChuckCenter(
                    ChuckCenterObjDto.PositiveTopPosition,
                    ChuckCenterObjDto.NegativeTopPosition,
                    ChuckCenterObjDto.NegativeRightPosition,
                    ChuckCenterObjDto.PositiveRightPosition,
                    ChuckCenterObjDto.PositiveBottomPosition,
                    ChuckCenterObjDto.NegativeBottomPosition,
                    ChuckCenterObjDto.NegativeLeftPosition,
                    ChuckCenterObjDto.PositiveLeftPosition
                );
            }

            if (chuckCenterPosition == Point.Origin)
            {
                result = false;
                Logger.LogHtmlInformation("Chuck Center calibration result Failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ChuckCenterPosition = chuckCenterPosition
                }), HtmlLogUniqueId.LoggingHtml());
                return result;
            }

            var bFCenterStagePosition = StageViewModel.GetBrightFieldCenterMachineStagePosition();

            var (xDirection, yDirection) = StageViewModel.GetMachineDirection();

            ChuckCenterObjDto.ChuckCenterPosition = chuckCenterPosition;
            ChuckCenterObjDto.BFCenterStagePosition = bFCenterStagePosition;
            ChuckCenterObjDto.NewBFCenterStagePosition = new Point(bFCenterStagePosition.X + xDirection * chuckCenterPosition.X, bFCenterStagePosition.Y + yDirection * chuckCenterPosition.Y);

            Logger.LogHtmlInformation("Chuck Center calibration result OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                XDirection = xDirection,
                YDirection = yDirection,
                ChuckCenterObjDto.ChuckCenterPosition,
                ChuckCenterObjDto.BFCenterStagePosition,
                CalibrationResult = ChuckCenterObjDto.NewBFCenterStagePosition
            }), HtmlLogUniqueId.LoggingHtml());
            return result;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
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
            result = false;
            return;
        }

        await InvokeVerifyAsync(async () =>
        {
            if (await VerifyCalibrationAsync(ReviewDto, cancellationToken) == false) result = false;
            return result;
        }).ConfigureAwait(false);
    }

    private async Task<bool> VerifyCalibrationAsync(ChuckCenterObjDto selectChuckCenterObjDto, CancellationToken cancellationToken)
    {
        var result = true;
        await Task.Run(() =>
        {
            selectChuckCenterObjDto.IsVerified = false;
            StageViewModel.SetBrightFieldCenterMachinePositionValue(selectChuckCenterObjDto.BFCenterStagePosition);

            var chuckCenterObjDto = selectChuckCenterObjDto.Clone();

            if (RecipeCacheProvider.Set(Cache, cancellationToken) == false)
            {
                DialogWindowProvider.ShowDialog("Save Threshold Failed!", DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                result = false;
                return;
            }

            var isPositiveSuccess = FindChuckCenterPosition(chuckCenterObjDto, Cache.PositiveAngle, true);
            if (isPositiveSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Find Positive Four Points Failed."), HtmlLogUniqueId.LoggingHtml());
                result = false;
                return;
            }

            var isNegativeSuccess = FindChuckCenterPosition(chuckCenterObjDto, Cache.NegativeAngle, false);
            if (isNegativeSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Find Negative Four Points Failed."), HtmlLogUniqueId.LoggingHtml());
                result = false;
                return;
            }

            chuckCenterObjDto.ChuckCenterPosition = CalibrationAlgorithmService.GetChuckCenter(
                chuckCenterObjDto.PositiveTopPosition,
                chuckCenterObjDto.NegativeTopPosition,
                chuckCenterObjDto.NegativeRightPosition,
                chuckCenterObjDto.PositiveRightPosition,
                chuckCenterObjDto.PositiveBottomPosition,
                chuckCenterObjDto.NegativeBottomPosition,
                chuckCenterObjDto.NegativeLeftPosition,
                chuckCenterObjDto.PositiveLeftPosition
            );
            var offset = chuckCenterObjDto.ChuckCenterPosition - selectChuckCenterObjDto.ChuckCenterPosition;

            result = Math.Abs(offset.X) <= Cache.Threshold && Math.Abs(offset.Y) <= Cache.Threshold;

            Logger.LogHtmlInformation(result ? "OK" : "Failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                selectChuckCenterObjDto.ChuckCenterPosition,
                VerifyChuckCenterPosition = chuckCenterObjDto.ChuckCenterPosition,
                selectChuckCenterObjDto.BFCenterStagePosition,
                CalibrationResult = new Point(selectChuckCenterObjDto.BFCenterStagePosition.X - selectChuckCenterObjDto.ChuckCenterPosition.X, selectChuckCenterObjDto.BFCenterStagePosition.Y + selectChuckCenterObjDto.ChuckCenterPosition.Y),
                VerifyResult = new Point(selectChuckCenterObjDto.BFCenterStagePosition.X - chuckCenterObjDto.ChuckCenterPosition.X, selectChuckCenterObjDto.BFCenterStagePosition.Y + chuckCenterObjDto.ChuckCenterPosition.Y),
                ChuckCenterThreshold = Cache.Threshold
            }), HtmlLogUniqueId.LoggingHtml());

            selectChuckCenterObjDto.IsVerified = result;
            if (Save(selectChuckCenterObjDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                selectChuckCenterObjDto.IsVerified = false;
                result = false;
                return;
            }

            if (!result || !IsAutoCalibrate)
            {
                DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, New ChuckCenter: ({chuckCenterObjDto.ChuckCenterPosition}) Old ChuckCenter: ({selectChuckCenterObjDto.ChuckCenterPosition}) Error: ({offset})", DialogButtonsEnum.OK,
                    result ? DialogIconEnum.Information : DialogIconEnum.Warning);
            }

            if (result) StageViewModel.SetBrightFieldCenterMachinePositionValue(selectChuckCenterObjDto.NewBFCenterStagePosition);
            StageViewModel.SetAbsoluteStageTheta(0);
            StageViewModel.SetBrightFieldAbsoluteStageXy(new Point(0, 0));
        }, cancellationToken);
        return result;
    }

    private bool FindChuckCenterPosition(ChuckCenterObjDto chuckCenterObjDto, double angle, bool isPositive)
    {
        var detectImageDirectory = ImageFileDirectory;
        StageViewModel.SetAbsoluteStageTheta(angle);
        var angleNew = 0d;
        if (isPositive) angleNew = -angle + Cache.ThetaAngle;
        else angleNew = -angle + Cache.ThetaAngle;
        Logger.LogHtmlInformation($"Params", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
        {
            Angle = angle,
            OffsetAngle = angleNew
        }), HtmlLogUniqueId.LoggingHtml());

        Logger.LogHtmlInformation("Match Template", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

        //TopPosition
        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.LowChuckCenterCacheItem.TopPosition.DegreeAngleByOrigin(angleNew), Cache.LowChuckCenterCacheItem.MagnificationInfo, Cache.LowChuckCenterCacheItem.TopTemplateFilePath,
                detectImageDirectory, HtmlLogUniqueId, Name, "Low Mag Top",
                out var lowTopPosition, out _, out _, out var lowTopResultImageFilePath, out _) == false) return false;
        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, lowTopPosition + (Vector)Cache.LowToHighPointTop, Cache.HighChuckCenterCacheItem.MagnificationInfo, Cache.HighChuckCenterCacheItem.TopTemplateFilePath,
                detectImageDirectory, HtmlLogUniqueId, Name, "High Mag Top",
                out var highTopPosition, out _, out _, out var highTopResultImageFilePath, out _) == false) return false;

        //RightPosition
        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.LowChuckCenterCacheItem.RightPosition.DegreeAngleByOrigin(angleNew), Cache.LowChuckCenterCacheItem.MagnificationInfo, Cache.LowChuckCenterCacheItem.RightTemplateFilePath,
                detectImageDirectory, HtmlLogUniqueId, Name, "Low Mag Right",
                out var lowRightPosition, out _, out _, out var lowRightResultImageFilePath, out _) == false) return false;
        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, lowRightPosition + (Vector)Cache.LowToHighPointRight, Cache.HighChuckCenterCacheItem.MagnificationInfo, Cache.HighChuckCenterCacheItem.RightTemplateFilePath,
                detectImageDirectory, HtmlLogUniqueId, Name, "High Mag Right",
                out var highRightPosition, out _, out _, out var highRightResultImageFilePath, out _) == false) return false;

        //BottomPosition
        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.LowChuckCenterCacheItem.BottomPosition.DegreeAngleByOrigin(angleNew), Cache.LowChuckCenterCacheItem.MagnificationInfo, Cache.LowChuckCenterCacheItem.BottomTemplateFilePath,
                detectImageDirectory, HtmlLogUniqueId, Name, "Low Mag Bottom",
                out var lowBottomPosition, out _, out _, out var lowBottomResultImageFilePath, out _) == false) return false;
        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, lowBottomPosition + (Vector)Cache.LowToHighPointBottom, Cache.HighChuckCenterCacheItem.MagnificationInfo, Cache.HighChuckCenterCacheItem.BottomTemplateFilePath,
                detectImageDirectory, HtmlLogUniqueId, Name, "High Mag Bottom",
                out var highBottomPosition, out _, out _, out var highBottomResultImageFilePath, out _) == false) return false;

        //LeftPosition
        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.LowChuckCenterCacheItem.LeftPosition.DegreeAngleByOrigin(angleNew), Cache.LowChuckCenterCacheItem.MagnificationInfo, Cache.LowChuckCenterCacheItem.LeftTemplateFilePath,
                detectImageDirectory, HtmlLogUniqueId, Name, "Low Mag Left",
                out var lowLeftPosition, out _, out _, out var lowLeftResultImageFilePath, out _) == false) return false;
        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, lowLeftPosition + (Vector)Cache.LowToHighPointLeft, Cache.HighChuckCenterCacheItem.MagnificationInfo, Cache.HighChuckCenterCacheItem.LeftTemplateFilePath,
                detectImageDirectory, HtmlLogUniqueId, Name, "High Mag Left",
                out var highLeftPosition, out _, out _, out var highLeftResultImageFilePath, out _) == false) return false;

        if (isPositive)
        {
            chuckCenterObjDto.PositiveTopPosition = highTopPosition;
            chuckCenterObjDto.PositiveRightPosition = highRightPosition;
            chuckCenterObjDto.PositiveBottomPosition = highBottomPosition;
            chuckCenterObjDto.PositiveLeftPosition = highLeftPosition;
            chuckCenterObjDto.PositiveTopFilePath = highTopResultImageFilePath;
            chuckCenterObjDto.PositiveRightFilePath = highRightResultImageFilePath;
            chuckCenterObjDto.PositiveBottomFilePath = highBottomResultImageFilePath;
            chuckCenterObjDto.PositiveLeftFilePath = highLeftResultImageFilePath;
            Logger.LogHtmlInformation($"Positive-Top Position", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                chuckCenterObjDto.PositiveTopPosition,
                HtmlTab = new HtmlTab(new
                {
                    TemplateImage = new HtmlImage(Cache.LowChuckCenterCacheItem.TopTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    LowResultImage = new HtmlImage(lowTopResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighTemplateImage = new HtmlImage(Cache.HighChuckCenterCacheItem.TopTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighResultImage = new HtmlImage(highTopResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation($"Positive-Right Position", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                chuckCenterObjDto.PositiveRightPosition,
                HtmlTab = new HtmlTab(new
                {
                    HtmlTab = new HtmlTab(new
                    {
                        LowTemplateImage = new HtmlImage(Cache.LowChuckCenterCacheItem.RightTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        LowResultImage = new HtmlImage(lowRightResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        HighTemplateImage = new HtmlImage(Cache.HighChuckCenterCacheItem.RightTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        HighResultImage = new HtmlImage(highRightResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                })
            }), HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation($"Positive-Bottom Position", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                chuckCenterObjDto.PositiveBottomPosition,
                HtmlTab = new HtmlTab(new
                {
                    LowTemplateImage = new HtmlImage(Cache.LowChuckCenterCacheItem.BottomTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    LowResultImage = new HtmlImage(lowBottomResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighTemplateImage = new HtmlImage(Cache.HighChuckCenterCacheItem.BottomTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighResultImage = new HtmlImage(highBottomResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation($"Positive-Left Position", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                chuckCenterObjDto.PositiveLeftPosition,
                HtmlTab = new HtmlTab(new
                {
                    LowTemplateImage = new HtmlImage(Cache.LowChuckCenterCacheItem.LeftTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    LowResultImage = new HtmlImage(lowLeftResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighTemplateImage = new HtmlImage(Cache.HighChuckCenterCacheItem.LeftTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighResultImage = new HtmlImage(highLeftResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
        }
        else
        {
            chuckCenterObjDto.NegativeTopPosition = highTopPosition;
            chuckCenterObjDto.NegativeRightPosition = highRightPosition;
            chuckCenterObjDto.NegativeBottomPosition = highBottomPosition;
            chuckCenterObjDto.NegativeLeftPosition = highLeftPosition;
            chuckCenterObjDto.NegativeTopFilePath = highTopResultImageFilePath;
            chuckCenterObjDto.NegativeRightFilePath = highRightResultImageFilePath;
            chuckCenterObjDto.NegativeBottomFilePath = highBottomResultImageFilePath;
            chuckCenterObjDto.NegativeLeftFilePath = highLeftResultImageFilePath;
            Logger.LogHtmlInformation($"Negative-Top Position", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                chuckCenterObjDto.NegativeTopPosition,
                HtmlTab = new HtmlTab(new
                {
                    TemplateImage = new HtmlImage(Cache.LowChuckCenterCacheItem.TopTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    LowResultImage = new HtmlImage(lowTopResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighTemplateImage = new HtmlImage(Cache.HighChuckCenterCacheItem.TopTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighResultImage = new HtmlImage(highTopResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation($"Negative-Right Position", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                chuckCenterObjDto.NegativeRightPosition,
                HtmlTab = new HtmlTab(new
                {
                    HtmlTab = new HtmlTab(new
                    {
                        LowTemplateImage = new HtmlImage(Cache.LowChuckCenterCacheItem.RightTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        LowResultImage = new HtmlImage(lowRightResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        HighTemplateImage = new HtmlImage(Cache.HighChuckCenterCacheItem.RightTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        HighResultImage = new HtmlImage(highRightResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                })
            }), HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation($"Negative-Bottom Position", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                chuckCenterObjDto.NegativeBottomPosition,
                HtmlTab = new HtmlTab(new
                {
                    LowTemplateImage = new HtmlImage(Cache.LowChuckCenterCacheItem.BottomTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    LowResultImage = new HtmlImage(lowBottomResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighTemplateImage = new HtmlImage(Cache.HighChuckCenterCacheItem.BottomTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighResultImage = new HtmlImage(highBottomResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation($"Negative-Left Position", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                chuckCenterObjDto.NegativeLeftPosition,
                HtmlTab = new HtmlTab(new
                {
                    LowTemplateImage = new HtmlImage(Cache.LowChuckCenterCacheItem.LeftTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    LowResultImage = new HtmlImage(lowLeftResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighTemplateImage = new HtmlImage(Cache.HighChuckCenterCacheItem.LeftTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighResultImage = new HtmlImage(highLeftResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
        }

        return true;
    }

    private bool Save(ChuckCenterObjDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        dto.LowMicroscopeMagnificationInfo = Cache.LowChuckCenterCacheItem.MagnificationInfo;
        dto.HighMicroscopeMagnificationInfo = Cache.HighChuckCenterCacheItem.MagnificationInfo;

        Calibration = dto.Clone();

        return CacheProvider.Set(dto, cancellationToken)
               && RecipeCacheProvider.Set(Cache, cancellationToken)
               && EnableDependedCalibrationItems(cancellationToken);
    });

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependChuckCenterCalibrations(false, cancellationToken, out var errorMsg) == false)
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
            new() { StepName = "Find Positive Point" },
            new() { StepName = "Find Negative Point" },
            new() { StepName = "Get Chuck center" },
            new() { StepName = "Review" }
        ];
    }

    public override async Task<bool> AutomationActionAsync(CancellationToken cancellationToken)
    {
        GetAutoCalibrationStep();
        await base.AutomationActionAsync(cancellationToken);
        var result = false;
        foreach (var stepItem in AutoCalibrationStepList.Select((t, index) => (t, index)))
        {
            switch (stepItem.index)
            {
                case 0:
                    if (await LoadedingAsync(cancellationToken) == false) return false;
                    if (await AutomationRecipeInformationAsync() == false) return false;
                    await InvokeCalibrateAsync(() =>
                    {
                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                        {
                            Cache.AlgorithmTemplateTypeEnum
                        }), HtmlLogUniqueId.LoggingHtml());
                        return true;
                    });
                    if (await AutoNextingAsync(cancellationToken) == false) return false;
                    break;

                case 1:
                    if (await Step2CalibrateActionAsync(cancellationToken) == false) return false;
                    if (await AutoNextingAsync(cancellationToken) == false) return false;
                    break;

                case 2:
                    if (await Step3CalibrateActionAsync(cancellationToken) == false) return false;
                    if (await AutoNextingAsync(cancellationToken) == false) return false;
                    break;

                case 3:
                    if (await Step4CalibrateActionAsync(cancellationToken) == false) return false;
                    CalibrationStepIndex = 4;
                    if (await NextingAsync(cancellationToken) == false) return false;
                    if (await AutoNextingAsync(cancellationToken) == false) return false;
                    break;

                case 4:
                    if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false) return false;
                    await InvokeCalibrateAsync(async () =>
                    {
                        if (ReviewDto is not null)
                        {
                            if (await VerifyCalibrationAsync(ReviewDto, cancellationToken) == false)
                            {
                                DialogWindowProvider.ShowDialog($"chuckCenter Calibration Review  Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                                return false;
                            }
                        }

                        result = true;
                        return result;
                    });
                    AutoCalibrationStepIndex++;
                    break;
            }

            AutoCalibrationProgress = AutoCalibrationStepIndex / (double)AutoCalibrationStepList.Count * 100;
        }

        return result;
    }

    public override async Task<bool> AutomationRecipeInformationAsync(string chuckCenterName = "")
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (IsRecipeCalibrate == false)
            return true;

        if (CalibrationRecipeDto is null)
        {
            DialogWindowProvider.ShowDialog("Revise wafer map is empty!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        Cache.ThetaAngle = StageViewModel.GetMachineStageTheta();

        var reticleRows = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleModel
            .Where(t => t.Index.X == 0)
            .OrderBy(t => t.Index.Y).ToList();
        var reticleCols = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleModel
            .Where(t => t.Index.Y == 0)
            .OrderBy(t => t.Index.X).ToList();

        #region 上低倍

        if (CalibrationRecipeService.GetChuckReticleMaskInfo(Cache.WaferMaskTypeEnum, Cache.LowChuckCenterCacheItem.MagnificationInfo, null, out var maskInfoLow) == false)
            return false;

        var reticleTop = reticleRows.ElementAt(reticleRows.Count - 2);
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleTop, maskInfoLow, out var lowTopPosition);
        Cache.LowChuckCenterCacheItem.TopPosition = lowTopPosition;
        Cache.LowChuckCenterCacheItem.TopTemplateFilePath = maskInfoLow.RecipeBrightFieldTemplateDto.TemplateFilePath;
        Cache.LowChuckCenterCacheItem.TopTemplateImageFilePath = maskInfoLow.RecipeBrightFieldTemplateDto.TemplateImageFilePath;

        #endregion

        #region 右边低倍

        var reticleRight = reticleCols.ElementAt(reticleCols.Count - 2);
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleRight, maskInfoLow, out var lowRightPosition);
        Cache.LowChuckCenterCacheItem.RightPosition = lowRightPosition;
        Cache.LowChuckCenterCacheItem.RightTemplateFilePath = maskInfoLow.RecipeBrightFieldTemplateDto.TemplateFilePath;
        Cache.LowChuckCenterCacheItem.RightTemplateImageFilePath = maskInfoLow.RecipeBrightFieldTemplateDto.TemplateImageFilePath;

        #endregion 右边低倍

        #region 下边低倍

        var reticleBottom = reticleRows.ElementAt(1);
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleBottom, maskInfoLow, out var lowBottomPosition);
        Cache.LowChuckCenterCacheItem.BottomPosition = lowBottomPosition;
        Cache.LowChuckCenterCacheItem.BottomTemplateFilePath = maskInfoLow.RecipeBrightFieldTemplateDto.TemplateFilePath;
        Cache.LowChuckCenterCacheItem.BottomTemplateImageFilePath = maskInfoLow.RecipeBrightFieldTemplateDto.TemplateImageFilePath;

        #endregion 下边低倍

        #region 左边低倍

        var reticleLeft = reticleCols.ElementAt(1);
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleLeft, maskInfoLow, out var lowLeftPosition);
        Cache.LowChuckCenterCacheItem.LeftPosition = lowLeftPosition;
        Cache.LowChuckCenterCacheItem.LeftTemplateFilePath = maskInfoLow.RecipeBrightFieldTemplateDto.TemplateFilePath;
        Cache.LowChuckCenterCacheItem.LeftTemplateImageFilePath = maskInfoLow.RecipeBrightFieldTemplateDto.TemplateImageFilePath;

        #endregion 左边低倍

        #region 上高倍

        if (CalibrationRecipeService.GetChuckReticleMaskInfo(Cache.WaferMaskTypeEnum, Cache.HighChuckCenterCacheItem.MagnificationInfo, null, out var maskInfoHigh) == false)
            return false;

        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleTop, maskInfoHigh, out var highTopPosition);
        Cache.HighChuckCenterCacheItem.TopPosition = highTopPosition;
        Cache.HighChuckCenterCacheItem.TopTemplateFilePath = maskInfoHigh.RecipeBrightFieldTemplateDto.TemplateFilePath;
        Cache.HighChuckCenterCacheItem.TopTemplateImageFilePath = maskInfoHigh.RecipeBrightFieldTemplateDto.TemplateImageFilePath;

        #endregion 上高倍

        #region 右高倍

        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleRight, maskInfoHigh, out var highRightPosition);
        Cache.HighChuckCenterCacheItem.RightPosition = highRightPosition;
        Cache.HighChuckCenterCacheItem.RightTemplateFilePath = maskInfoHigh.RecipeBrightFieldTemplateDto.TemplateFilePath;
        Cache.HighChuckCenterCacheItem.RightTemplateImageFilePath = maskInfoHigh.RecipeBrightFieldTemplateDto.TemplateImageFilePath;

        #endregion 右高倍

        #region 下高倍

        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleBottom, maskInfoHigh, out var highBottomPosition);
        Cache.HighChuckCenterCacheItem.BottomPosition = highBottomPosition;
        Cache.HighChuckCenterCacheItem.BottomTemplateFilePath = maskInfoHigh.RecipeBrightFieldTemplateDto.TemplateFilePath;
        Cache.HighChuckCenterCacheItem.BottomTemplateImageFilePath = maskInfoHigh.RecipeBrightFieldTemplateDto.TemplateImageFilePath;

        #endregion 下高倍

        #region 左高倍

        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleLeft, maskInfoHigh, out var highLeftPosition);
        Cache.HighChuckCenterCacheItem.LeftPosition = highLeftPosition;
        Cache.HighChuckCenterCacheItem.LeftTemplateFilePath = maskInfoHigh.RecipeBrightFieldTemplateDto.TemplateFilePath;
        Cache.HighChuckCenterCacheItem.LeftTemplateImageFilePath = maskInfoHigh.RecipeBrightFieldTemplateDto.TemplateImageFilePath;

        #endregion 左高倍

        MicroscopeViewModel.SwitchMagnification(Cache.LowChuckCenterCacheItem.MagnificationInfo);
        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowChuckCenterCacheItem.TopPosition);
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

        var result = false;
        await InvokeVerifyAsync(async () =>
        {
            try
            {
                if (await AutomationRecipeInformationAsync(string.Empty) == false) return false;
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