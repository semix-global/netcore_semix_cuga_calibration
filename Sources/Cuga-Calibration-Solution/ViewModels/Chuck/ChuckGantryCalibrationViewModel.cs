using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.IO;

namespace CugaCalibration.ViewModels.Chuck;

[IOCAppService(ServiceType = typeof(ChuckGantryCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckGantryCalibrationViewModel(AlignmentWindowBrightFieldViewModel alignmentWindowBrightFieldViewModel) : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.HighMicroscopeLensInformation.LensName);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.HighMicroscopeLensInformation.LensName);

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "P5", StepIsNextEnable = true },
        new() { StepName = "Select Low Mag Base Position" },
        new() { StepName = "Select Top Low Mag Position" },
        new() { StepName = "Select Top High Mag Position" },
        new() { StepName = "Select Bottom Low Mag Position" },
        new() { StepName = "Select Bottom High Mag Position" },
        new() { StepName = "Offset" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ChuckGantryDto _resultChuckGantryDto = new();

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ChuckGantryDto? _reviewDto;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private ChuckGantryCache _cache = new();

    [ObservableProperty]
    private ChuckGantryDto _calibration = new();

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

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

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<ChuckGantryCache>();
        Calibration = CacheProvider.GetOrDefault<ChuckGantryDto>();
        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();

        if (Cache.LowMicroscopeLensInformation.LensCode == -1) Cache.LowMicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformationList[0];
        if (Cache.HighMicroscopeLensInformation.LensCode == -1)
            Cache.HighMicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformationList.Count <= 2
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

        if (IsRecipeCalibrate)
        {
            if (CalibrationRecipeService.GetCorrectWaferMapByOffset(true) == false)
                return false;
            if (await AutomationRecipeInformationAsync("0") == false) return false;
            if (await AutomationRecipeInformationAsync("1") == false) return false;
        }

        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.BaseLowFindPosition);
        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewDto = Calibration.Clone();

        return ReviewDto.IsCalibrated;
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        switch (CalibrationStepIndex)
        {
            case 2:
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.BaseLowFindPosition);
                return true;

            case 3:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowTopPosition);
                return true;

            case 4:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.HighTopPosition);
                return true;

            case 5:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowBottomPosition);
                return true;

            case 6:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.HighBottomPosition);

                return true;

            default:
                return true;
        }
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetGantryOffset(0);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.BaseLowFindPosition);
                return true;

            case 1:
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowTopPosition);
                return true;

            case 2:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                return File.Exists(Cache.AlgorithmTemplateTypeEnum.ToFullFilePath(Cache.LowTopTemplateFilePath))
                       && File.Exists(Cache.LowTopTemplateImageFilePath);

            case 3:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowBottomPosition);
                return File.Exists(Cache.AlgorithmTemplateTypeEnum.ToFullFilePath(Cache.HighTopTemplateFilePath)) &&
                       File.Exists(Cache.HighTopTemplateImageFilePath);

            case 4:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                return File.Exists(Cache.AlgorithmTemplateTypeEnum.ToFullFilePath(Cache.LowBottomTemplateFilePath))
                       && File.Exists(Cache.LowBottomTemplateImageFilePath);

            case 5:
                return File.Exists(Cache.AlgorithmTemplateTypeEnum.ToFullFilePath(Cache.HighBottomTemplateFilePath))
                       && File.Exists(Cache.HighBottomTemplateImageFilePath);

            case 6:
                ResultChuckGantryDto.IsCalibrated = true;
                if (Save(ResultChuckGantryDto, cancellationToken) == false)
                {
                    ResultChuckGantryDto.IsCalibrated = false;
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
            if (Cache.HighMicroscopeLensInformation.LensCode <= Cache.LowMicroscopeLensInformation.LensCode)
            {
                DialogWindowProvider.ShowDialog("The high magnification less than or equal low magnification! Please select correct magnification!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var result = StageViewModel.GetBrightFieldStagePosition();
            Cache.BaseLowFindPosition = result;

            (var isSuccess, Cache.LowTopPosition, Cache.LowBottomPosition) = GetIdeaBrightFieldPosition(Cache.BaseLowFindPosition);
            if (isSuccess == false) return false;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.AlgorithmTemplateTypeEnum,
                Cache.WaferMaskTypeEnum,
                LowMagnification = Cache.LowMicroscopeLensInformation.LensName,
                HighMagnification = Cache.HighMicroscopeLensInformation.LensName,
                Cache.BaseLowFindPosition,
                Cache.WaferDiameter,
                Cache.RowCellHeight
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });

        (bool isSuccess, Point negativePosition, Point positivePosition) GetIdeaBrightFieldPosition(Point findPosition)
        {
            try
            {
                // 靠近边缘位置的理想位置可能拍不全，总长度截去一个die
                var actualWaferRadius = (Cache.WaferDiameter - Cache.RowCellHeight) / 2d;
                var interval = Cache.RowCellHeight;
                var baseValue = findPosition.Y;

                var topError = (int)((actualWaferRadius - baseValue) / interval) * interval;
                var bottomError = (int)((actualWaferRadius + baseValue) / interval) * interval;

                var topPoint = new Point(findPosition.X, baseValue + topError);
                var bottomPoint = new Point(findPosition.X, baseValue - bottomError);

                return (true, topPoint, bottomPoint);
            }
            catch (Exception ex)
            {
                Logger.LogError("Get idea bright field position failed:{ex}", ex);
                return (false, default, default);
            }
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var result = StageViewModel.GetBrightFieldStagePosition();
            Cache.LowTopPosition = result;

            Cache.LowTopTemplateFilePath = $"{TemplateFileDirectory}\\Top_{Cache.LowMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            var generateTemplateLow = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.LowTopTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
            if (generateTemplateLow == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            else Cache.LowTopTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.LowTopTemplateFilePath);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.LowTopPosition,
                Cache.LowTopTemplateFilePath,
                HtmlTab = new HtmlTab(new
                {
                    LowTemplateImage = new HtmlImage(Cache.LowTopTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
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
            var result = StageViewModel.GetBrightFieldStagePosition();
            Cache.HighTopPosition = result;

            Cache.HighTopTemplateFilePath = $"{TemplateFileDirectory}\\Top_{Cache.HighMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            var generateTemplateHigh = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.HighTopTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
            if (generateTemplateHigh == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            else Cache.HighTopTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.HighTopTemplateFilePath);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.HighTopPosition,
                Cache.HighTopTemplateFilePath,
                HtmlTab = new HtmlTab(new
                {
                    HighTemplateImage = new HtmlImage(Cache.HighTopTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step4CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var result = StageViewModel.GetBrightFieldStagePosition();
            Cache.LowBottomPosition = result;

            Cache.LowBottomTemplateFilePath = $"{TemplateFileDirectory}\\Bottom_{Cache.LowMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            var generateTemplateLow = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.LowBottomTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
            if (generateTemplateLow == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            else Cache.LowBottomTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.LowBottomTemplateFilePath);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.LowBottomPosition,
                Cache.LowBottomTemplateFilePath,
                HtmlTab = new HtmlTab(new
                {
                    LowTemplateImage = new HtmlImage(Cache.LowBottomTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step5CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var result = StageViewModel.GetBrightFieldStagePosition();
            Cache.HighBottomPosition = result;

            Cache.HighBottomTemplateFilePath = $"{TemplateFileDirectory}\\Bottom_{Cache.HighMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            var generateTemplateHigh = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.HighBottomTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
            if (generateTemplateHigh == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            else Cache.HighBottomTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.HighBottomTemplateFilePath);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.HighBottomPosition,
                Cache.HighBottomTemplateFilePath,
                HtmlTab = new HtmlTab(new
                {
                    HighTemplateImage = new HtmlImage(Cache.HighBottomTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step6CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await InvokeCalibrateAsync(() =>
        {
            var detectImageDirectory = ImageFileDirectory;

            var (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                xAxisTemperature,
                yAxisTemperature,
                reviewCamTemperature,
                cibTemperature,
                HtmlTab = new HtmlTab(new
                {
                    LowTopTemplateImage = new HtmlImage(Cache.LowTopTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighTopTemplateImage = new HtmlImage(Cache.HighTopTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    LowBottomTemplateImage = new HtmlImage(Cache.LowBottomTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighBottomTemplateImage = new HtmlImage(Cache.HighBottomTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                }),
                ImageFileDirectory = detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            var chuckGantryObjDto = new ChuckGantryDto
            {
                FilePath1 = detectImageDirectory,
                FilePath2 = detectImageDirectory,
                H = CalibrationConstantsHelper.CalibrationGantryHLength
            };

            if (MatchTemplate(chuckGantryObjDto) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Match Template Failed."), HtmlLogUniqueId.LoggingHtml());
                result = false;
                return false;
            }

            ResultChuckGantryDto = chuckGantryObjDto.Clone();
            StageViewModel.SetGantryOffset(ResultChuckGantryDto.Offset);

            (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();

            Logger.LogHtmlInformation("OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                xAxisTemperature,
                yAxisTemperature,
                reviewCamTemperature,
                cibTemperature,
                Cache.HighMicroscopeLensInformation.LensName,
                ResultChuckGantryDto.Position1,
                ResultChuckGantryDto.Position2,
                Score1 = ResultChuckGantryDto.TemplateScore1,
                Score2 = ResultChuckGantryDto.TemplateScore2,
                Angle1 = ResultChuckGantryDto.TemplateAngle1,
                Angle2 = ResultChuckGantryDto.TemplateAngle2,
                ResultChuckGantryDto.Offset,
                ResultChuckGantryDto.H,
                HtmlTab = new HtmlTab(new
                {
                    ResultImage1 = new HtmlImage(ResultChuckGantryDto.FilePath1,
                        htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    ResultImage2 = new HtmlImage(ResultChuckGantryDto.FilePath2,
                        htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                })
            }), HtmlLogUniqueId.LoggingHtml());
            result = true;
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
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK,
                DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(async () =>
        {
            if (await VerifyCalibrationAsync(ReviewDto, cancellationToken) == false) result = false;
            return result;
        }).ConfigureAwait(false);
    }

    private async Task<bool> VerifyCalibrationAsync(ChuckGantryDto selectReviewItemDto,
        CancellationToken cancellationToken)
    {
        var result = true;
        await Task.Run(() =>
        {
            var detectImageDirectory = ImageFileDirectory;
            var (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) =
                MonitorViewModel.GetHardwareTemperature();
            selectReviewItemDto.IsVerified = false;
            Logger.LogHtmlInformation("P5 Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                xAxisTemperature,
                yAxisTemperature,
                reviewCamTemperature,
                cibTemperature,
                LowMagnification = Cache.LowMicroscopeLensInformation.LensName,
                HighMagnification = Cache.HighMicroscopeLensInformation.LensName,
                Cache.AlgorithmTemplateTypeEnum,
                Cache.LowTopPosition,
                Cache.LowBottomPosition,
                Cache.HighTopPosition,
                Cache.HighBottomPosition,
                Cache.WaferDiameter,
                Cache.RowCellHeight,
                HtmlTab = new HtmlTab(new
                {
                    LowTopTemplateImage = new HtmlImage(Cache.LowTopTemplateImageFilePath,
                        htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighTopTemplateImage = new HtmlImage(Cache.HighTopTemplateImageFilePath,
                        htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    LowBottomTemplateImage = new HtmlImage(Cache.LowBottomTemplateImageFilePath,
                        htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighBottomTemplateImage = new HtmlImage(Cache.HighBottomTemplateImageFilePath,
                        htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                }),
                ImageFileDirectory = detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());
            var alignmentResultDto = StageViewModel.Alignment(
                AlignmentCacheBrightField.LowSite1,
                AlignmentCacheBrightField.LowSite2,
                AlignmentCacheBrightField.HighSite1,
                AlignmentCacheBrightField.HighSite2,
                AlignmentCacheBrightField.LowMag,
                AlignmentCacheBrightField.HighMag,
                AlignmentCacheBrightField.AlgorithmWaferTypeEnum);

            Cache.P5Angle = alignmentResultDto.Degrees;
            Logger.LogHtmlInformation("P5 OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new { Cache.P5Angle }), HtmlLogUniqueId.LoggingHtml());

            var chuckGantryObjDto = new ChuckGantryDto
            {
                FilePath1 = detectImageDirectory,
                FilePath2 = detectImageDirectory,
                H = CalibrationConstantsHelper.CalibrationGantryHLength
            };

            if (MatchTemplate(chuckGantryObjDto) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Match Template Failed!"), HtmlLogUniqueId.LoggingHtml());
                result = false;
                return;
            }

            Cache.VerifyResultOffset = chuckGantryObjDto.Offset;
            result = Math.Abs(chuckGantryObjDto.Offset) < Cache.Threshold;
            (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) = MonitorViewModel.GetHardwareTemperature();

            Logger.LogHtmlInformation($"Verify {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                xAxisTemperature,
                yAxisTemperature,
                reviewCamTemperature,
                cibTemperature,
                NewOffset = chuckGantryObjDto.Offset,
                OldOffset = selectReviewItemDto.Offset,
                Cache.Threshold,
                Cache.HighMicroscopeLensInformation.LensName,
                chuckGantryObjDto.Position1,
                chuckGantryObjDto.Position2,
                Score1 = chuckGantryObjDto.TemplateScore1,
                Score2 = chuckGantryObjDto.TemplateScore2,
                Angle1 = chuckGantryObjDto.TemplateAngle1,
                Angle2 = chuckGantryObjDto.TemplateAngle2,
                chuckGantryObjDto.H,
                HtmlTab = new HtmlTab(new
                {
                    ResultImage1 = new HtmlImage(chuckGantryObjDto.FilePath1,
                        htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    ResultImage2 = new HtmlImage(chuckGantryObjDto.FilePath2,
                        htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            selectReviewItemDto.IsVerified = result;
            if (Save(selectReviewItemDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                selectReviewItemDto.IsVerified = false;
                result = false;
            }

            if (!result || !IsAutoCalibrate)
            {
                DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, New Offset: ({chuckGantryObjDto.Offset:f3}) Old Offset: ({selectReviewItemDto.Offset:f3})", DialogButtonsEnum.OK,
                    result ? DialogIconEnum.Information : DialogIconEnum.Warning);
            }
        }, cancellationToken);
        return result;
    }

    private bool MatchTemplate(ChuckGantryDto chuckGantryDto)
    {
        Logger.LogHtmlInformation("Match Template", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.LowTopPosition, Cache.LowMicroscopeLensInformation, Cache.LowTopTemplateFilePath, chuckGantryDto.FilePath1, HtmlLogUniqueId, Name,
                "Low Magnification 1", out var lowPosition1, out _, out _, out _, out _) == false) return false;
        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, lowPosition1 + (Vector)Cache.LowToHighPoint, Cache.HighMicroscopeLensInformation, Cache.HighTopTemplateFilePath, chuckGantryDto.FilePath1, HtmlLogUniqueId, Name,
                "High Magnification 1", out var highPosition1, out var highScore1, out var highAngle1, out var highImageFilePath1, out _) == false) return false;
        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.LowBottomPosition, Cache.LowMicroscopeLensInformation, Cache.LowBottomTemplateFilePath, chuckGantryDto.FilePath2, HtmlLogUniqueId, Name,
                "Low Magnification 2", out var lowPosition2, out _, out _, out _, out _) == false) return false;
        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, lowPosition2 + (Vector)Cache.LowToHighPoint, Cache.HighMicroscopeLensInformation, Cache.HighBottomTemplateFilePath, chuckGantryDto.FilePath2, HtmlLogUniqueId, Name,
                "High Magnification 2", out var highPosition2, out var highScore2, out var highAngle2, out var highImageFilePath2, out _) == false) return false;

        chuckGantryDto.Position1 = highPosition1;
        chuckGantryDto.TemplateScore1 = highScore1;
        chuckGantryDto.TemplateAngle1 = highAngle1;
        chuckGantryDto.FilePath1 = highImageFilePath1;

        chuckGantryDto.Position2 = highPosition2;
        chuckGantryDto.TemplateScore2 = highScore2;
        chuckGantryDto.TemplateAngle2 = highAngle2;
        chuckGantryDto.FilePath2 = highImageFilePath2;

        chuckGantryDto.Offset = -chuckGantryDto.H * Math.Tan(chuckGantryDto.Slope); // 由于坐标系(机械坐标系和笛卡尔坐标系相同)不同，需要取反

        return true;
    }

    private bool Save(ChuckGantryDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        dto.LowMicroscopeLensInformation = Cache.LowMicroscopeLensInformation;
        dto.HighMicroscopeLensInformation = Cache.HighMicroscopeLensInformation;

        Calibration = dto.Clone();

        return CacheProvider.Set(dto, cancellationToken)
               && RecipeCacheProvider.Set(Cache, cancellationToken)
               && EnableDependedCalibrationItems(cancellationToken);
    });

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependGantryCalibrations(false, cancellationToken, out var errorMsg) == false)
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
            new() { StepName = "Calibration" },
            new() { StepName = "Review" }
        ];
    }

    public override async Task<bool> AutomationActionAsync(CancellationToken cancellationToken)
    {
        GetAutoCalibrationStep();
        await base.AutomationActionAsync(cancellationToken);
        var result = false;
        if (await AutomationRecipeInformationAsync("0") == false) return false;
        if (await AutomationRecipeInformationAsync("1") == false) return false;
        foreach (var stepItem in AutoCalibrationStepList.Select((t, index) => (t, index)))
        {
            switch (stepItem.index)
            {
                case 0:
                    if (await LoadedingAsync(cancellationToken) == false) return false;
                    await InvokeCalibrateAsync(() =>
                    {
                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                        {
                            Cache.AlgorithmTemplateTypeEnum,
                        }), HtmlLogUniqueId.LoggingHtml());
                        return true;
                    });
                    CalibrationStepIndex = 0;
                    if (await NextingAsync(cancellationToken) == false) return false;
                    if (await AutoNextingAsync(cancellationToken) == false) return false;
                    break;

                case 1:
                    if (await Step6CalibrateActionAsync(cancellationToken) == false) return false;
                    CalibrationStepIndex = 6;
                    if (await NextingAsync(cancellationToken) == false) return false;
                    if (await AutoNextingAsync(cancellationToken) == false) return false;
                    break;

                case 2:
                    if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false) return false;
                    await InvokeCalibrateAsync(async () =>
                    {
                        ReviewDto = Calibration.Clone();
                        if (await VerifyCalibrationAsync(ReviewDto, cancellationToken) == false) return false;
                        result = true;
                        return result;
                    });
                    AutoCalibrationStepIndex++;
                    break;
            }
        }

        return result;
    }

    public override async Task<bool> AutomationRecipeInformationAsync(string chuckName)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (IsRecipeCalibrate == false)
            return true;

        if (CalibrationRecipeDto is null)
        {
            DialogWindowProvider.ShowDialog("Revise wafer map is empty!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var reticleRows = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleModel
            .Where(t => t.Index.X == 0)
            .OrderBy(t => t.Index.Y).ToList();
        var reticleTop = reticleRows.ElementAt(reticleRows.Count - 2);
        var reticleBottom = reticleRows.ElementAt(1);

        switch (chuckName)
        {
            case "0":
                if (CalibrationRecipeService.GetChuckReticleMaskInfo(Cache.WaferMaskTypeEnum, Cache.LowMicroscopeLensInformation, null, out var maskInfoLow) == false)
                    return false;
                CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleTop, maskInfoLow, out var lowPosition1);
                Cache.LowTopPosition = lowPosition1;

                CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleBottom, maskInfoLow, out var lowPosition2);
                Cache.LowBottomPosition = lowPosition2;
                Cache.LowTopTemplateFilePath = maskInfoLow.RecipeBrightFieldTemplateDto.TemplateFilePath;
                Cache.LowTopTemplateImageFilePath = maskInfoLow.RecipeBrightFieldTemplateDto.TemplateImageFilePath;

                break;

            case "1":
                if (CalibrationRecipeService.GetChuckReticleMaskInfo(Cache.WaferMaskTypeEnum, Cache.HighMicroscopeLensInformation, null, out var maskInfoHigh) == false)
                    return false;

                CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleTop, maskInfoHigh, out var highPosition1);
                Cache.HighTopPosition = highPosition1;

                CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleBottom, maskInfoHigh, out var highPosition2);
                Cache.HighBottomPosition = highPosition2;

                Cache.HighTopTemplateFilePath = maskInfoHigh.RecipeBrightFieldTemplateDto.TemplateFilePath;
                Cache.HighTopTemplateImageFilePath = maskInfoHigh.RecipeBrightFieldTemplateDto.TemplateImageFilePath;

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

    #endregion 自动化校准
}