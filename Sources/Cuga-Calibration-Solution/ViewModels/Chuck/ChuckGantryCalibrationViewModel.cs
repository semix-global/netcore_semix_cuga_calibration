using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.PixelSize;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WaferMap.WPF.Primitives.Builders;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using System.IO;

namespace CugaCalibration.ViewModels.Chuck;

[IOCAppService(ServiceType = typeof(ChuckGantryCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckGantryCalibrationViewModel : CalibrationViewModelBase<ChuckGantryCache>
{
    #region 属性

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.HighMicroscopeLensInformation.LensName);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.HighMicroscopeLensInformation.LensName);

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "P5" },
        new() { StepName = "Low Mag Base Position" },
        new() { StepName = "High Mag Base Position" },
        new() { StepName = "Top Low Mag Position" },
        new() { StepName = "Bottom Low Mag Position" },
        new() { StepName = "Offset" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial ChuckGantryDto ResultChuckGantryDto { get; set; } = new();

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    public partial ChuckGantryDto? ReviewDto { get; set; }

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial ChuckGantryCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial ChuckGantryDto Calibration { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopePixelSizeItemDto[] MicroscopePixelSizeItems { get; set; } = [];

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = HostApplication.GetRequiredService<AlignmentUserControlViewModel>();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);


        MicroscopePixelSizeItems = ApplicationCookieService.GetCalibrations<MicroscopePixelSizeItemDto>(cancellationToken);

        if (Cache.LowMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.LowMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();
        if (Cache.HighMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.HighMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.HighMicroscopeLensInformation.Clone();

        Cache = ApplicationCookieService.GetCache<ChuckGantryCache>(cancellationToken);
        Calibration = ApplicationCookieService.GetCalibration<ChuckGantryDto>(cancellationToken);

        UpdateEntryStatus(Calibration, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

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
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.BaseLowFindPosition);
                return true;

            case 3:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.BaseHighFindPosition);
                return true;

            case 4:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowTopPosition);
                return true;

            case 5:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowBottomPosition);
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
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                return File.Exists(Cache.AlgorithmTemplateTypeEnum.ToFullFilePath(Cache.LowBaseTemplateFilePath))
                       && File.Exists(Cache.LowBaseTemplateImageFilePath);

            case 2:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowTopPosition);
                return File.Exists(Cache.AlgorithmTemplateTypeEnum.ToFullFilePath(Cache.HighBaseTemplateFilePath))
                       && File.Exists(Cache.HighBaseTemplateImageFilePath);

            case 3:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowBottomPosition);
                return true;

            case 5:
                ResultChuckGantryDto.IsCalibrated = true;
                if (Save(ResultChuckGantryDto, cancellationToken) == false)
                {
                    ResultChuckGantryDto.IsCalibrated = false;
                    Logger.LogError("{@Name} Error: Save Failed!", Name);
                    return false;
                }

                return true;

            default:
                return true;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
            AlignmentUserControlViewModel.IsDarkFieldAlignment = false;

            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            var alignmentResult = AlignmentUserControlViewModel.AlignmentResult;
            Cache.P5Angle = alignmentResult.Degrees;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                AlignmentUserControlViewModel.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(alignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.BaseLowFindPosition);

            return true;
        });
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

            // 用BuildDie的方式BuildReticle，防止取到圆外
            var waferMapReticleBuilder = new WaferMapDieBuilder
            {
                DiePitchSize = new Size(Cache.DiePitchHeight * Cache.ReticleDieCountY, Cache.DiePitchHeight * Cache.ReticleDieCountY),
                OriginalDiePoint = Cache.BaseLowFindPosition
            };

            var reticles = waferMapReticleBuilder.BuildDie(new Circle(Point.Origin, Cache.WaferRadius));

            var currentColReticles = reticles
                .Where(t => t.Index.X == 0)
                .OrderBy(t => t.Index.Y).ToArray();
            var imageCount = currentColReticles.Length;
            Guard.IsGreaterThan(imageCount, 2);

            Cache.LowTopPosition = currentColReticles[^1].Rect.Point;
            Cache.LowBottomPosition = currentColReticles[0].Rect.Point;

            Cache.LowBaseTemplateFilePath = $"{TemplateFileDirectory}\\Base_Low_{Cache.LowMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            var generateTemplateHigh = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.LowBaseTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
            if (generateTemplateHigh == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            else Cache.LowBaseTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.LowBaseTemplateFilePath);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.AlgorithmTemplateTypeEnum,
                Cache.WaferMaskTypeEnum,
                LowMagnification = Cache.LowMicroscopeLensInformation.LensName,
                HighMagnification = Cache.HighMicroscopeLensInformation.LensName,
                Cache.WaferRadius,
                Cache.DiePitchHeight,
                Cache.ReticleDieCountY,
                Cache.BaseLowFindPosition,
                Cache.LowTopPosition,
                Cache.LowBottomPosition,
                Cache.LowBaseTemplateFilePath,
                HtmlTab = new HtmlTab(new
                {
                    LowBaseTemplateImage = new HtmlImage(Cache.LowBaseTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Cache.BaseHighFindPosition = StageViewModel.GetBrightFieldStagePosition();

            Cache.HighBaseTemplateFilePath = $"{TemplateFileDirectory}\\Base_High_{Cache.HighMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            var generateTemplateHigh = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.HighBaseTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
            if (generateTemplateHigh == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            else Cache.HighBaseTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.HighBaseTemplateFilePath);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.BaseHighFindPosition,
                Cache.HighBaseTemplateFilePath,
                HtmlTab = new HtmlTab(new
                {
                    HighTemplateImage = new HtmlImage(Cache.HighBaseTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
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

            if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, result, Cache.LowMicroscopeLensInformation, Cache.LowBaseTemplateFilePath, out var lowPosition) == false) return false;

            Cache.LowTopPosition = lowPosition;

            Logger.LogHtmlInformation("Result", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.LowTopPosition
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

            if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, result, Cache.LowMicroscopeLensInformation, Cache.LowBaseTemplateFilePath, out var lowPosition) == false) return false;

            Cache.LowBottomPosition = lowPosition;

            Logger.LogHtmlInformation("Result", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.LowBottomPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step5CalibrateActionAsync(CancellationToken cancellationToken)
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
                    LowBaseTemplateImage = new HtmlImage(Cache.LowBaseTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighBaseTemplateImage = new HtmlImage(Cache.HighBaseTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
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
                        htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
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
        await Task.Run(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;
            var (reviewCamTemperature, cibTemperature, xAxisTemperature, yAxisTemperature) =
                MonitorViewModel.GetHardwareTemperature();
            selectReviewItemDto.IsVerified = false;

            StageViewModel.SetGantryOffset(selectReviewItemDto.Offset);

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
                Cache.WaferRadius,
                Cache.DiePitchHeight,
                HtmlTab = new HtmlTab(new
                {
                    LowTopTemplateImage = new HtmlImage(Cache.LowBaseTemplateImageFilePath,
                        htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighTopTemplateImage = new HtmlImage(Cache.HighBaseTemplateImageFilePath,
                        htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                }),
                ImageFileDirectory = detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
            AlignmentUserControlViewModel.IsDarkFieldAlignment = false;
            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            var alignmentResult = AlignmentUserControlViewModel.AlignmentResult;
            Cache.P5Angle = alignmentResult.Degrees;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                AlignmentUserControlViewModel.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(alignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

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

            DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, New Offset: ({chuckGantryObjDto.Offset:f3}) Old Offset: ({selectReviewItemDto.Offset:f3})", DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);
        }, cancellationToken);
        return result;
    }

    private bool MatchTemplate(ChuckGantryDto chuckGantryDto)
    {
        Logger.LogHtmlInformation("Match Template", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.LowTopPosition, Cache.LowMicroscopeLensInformation, Cache.LowBaseTemplateFilePath, chuckGantryDto.FilePath1, HtmlLogUniqueId, Name,
                "Low Magnification 1", out var lowPosition1, out _, out _, out _, out _) == false) return false;
        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, lowPosition1 + (Vector)Cache.LowToHighPoint, Cache.HighMicroscopeLensInformation, Cache.HighBaseTemplateFilePath, chuckGantryDto.FilePath1, HtmlLogUniqueId,
                Name,
                "High Magnification 1", out var highPosition1, out var highScore1, out var highAngle1, out var highImageFilePath1, out _) == false) return false;
        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.LowBottomPosition, Cache.LowMicroscopeLensInformation, Cache.LowBaseTemplateFilePath, chuckGantryDto.FilePath2, HtmlLogUniqueId, Name,
                "Low Magnification 2", out var lowPosition2, out _, out _, out _, out _) == false) return false;
        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, lowPosition2 + (Vector)Cache.LowToHighPoint, Cache.HighMicroscopeLensInformation, Cache.HighBaseTemplateFilePath, chuckGantryDto.FilePath2, HtmlLogUniqueId,
                Name,
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

        ApplicationCookieService.SetCalibration(dto, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase calibration, CancellationToken cancellationToken)
    {
        var temp = Guard.IsAssignableToTypeAndReturn<ChuckGantryDto>(calibration);
        var status = Entry.Status;

        Calibration = temp;

        status.TotalCalibrationCount = 1;
        status.CalibratedCount = Calibration.IsCalibrated ? 1 : 0;
        status.VerifiedCount = Calibration.IsVerified ? 1 : 0;
        status.Details = [];
    }

    #endregion 校准
}