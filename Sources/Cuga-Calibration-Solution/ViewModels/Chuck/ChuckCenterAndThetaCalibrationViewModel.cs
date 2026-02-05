using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.CenterAndTheta;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.PixelSize;
using Core.Utilities.SourceGenerators.Attributes;
using Local.NoSQL.DB.Providers.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WaferMap.WPF.Primitives.Builders;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;
using System.IO;

namespace CugaCalibration.ViewModels.Chuck;

[IOCAppService(ServiceType = typeof(ChuckCenterAndThetaCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckCenterAndThetaCalibrationViewModel(IHostEnvironment hostEnvironment) : CalibrationViewModelBase
{
    #region 属性

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "P5" },
        new() { StepName = "Low Mag Base Position And Template" },
        new() { StepName = "High Mag Base Position And Template" },
        new() { StepName = "Top Low Mag Position" },
        new() { StepName = "Bottom Low Mag Position" },
        new() { StepName = "Left Low Mag Position" },
        new() { StepName = "Right Low Mag Position" },
        new() { StepName = "Calibration" }
    ];

    #region Calibration

    [ObservableProperty]
    private ChuckCenterAndThetaItemDto? _resultCenterAndThetaItemDto = new();

    [ObservableProperty]
    private ObservableCollection<ChuckCenterAndThetaItemDto> _chuckCenterAndThetaItemDtoList = [];

    #endregion Calibration

    #region Review

    [ObservableProperty]
    private ChuckCenterAndThetaItemDto? _reviewDto;

    #endregion Review

    #region 缓存

    [ObservableProperty]
    private ChuckCenterAndThetaItemDto? _selectCenterAndThetaItemDto;

    [RecipeCache]
    [ObservableProperty]
    private ChuckCenterAndThetaCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private ChuckCenterAndThetaItemDto _calibration = new();

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    [ObservableProperty]
    private ChuckGantryDto _chuckGantry = new();

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

    #endregion 缓存

    #endregion 属性

    private readonly StageDirectionTypeEnum[] _stageDirectionTypeEnums = [StageDirectionTypeEnum.Up, StageDirectionTypeEnum.Down, StageDirectionTypeEnum.Left, StageDirectionTypeEnum.Right];

    #region 控制校准业务重载

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopePixelSizeItems = CalibrationStatusService.GetCalibrations<MicroscopePixelSizeItemDto>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<ChuckCenterAndThetaCache>();
        Calibration = CacheProvider.GetOrDefault<ChuckCenterAndThetaItemDto>();
        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();

        if (Cache.LowMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.LowMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();
        if (Cache.HighMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.HighMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.HighMicroscopeLensInformation.Clone();

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
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

        StageViewModel.SetBrightFieldAbsoluteStageXy(ReviewDto.ChuckCenterPosition);
        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.BaseLowSiteFindPosition);
                return true;

            case 1:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                return File.Exists(Cache.AlgorithmTemplateTypeEnum.ToFullFilePath(Cache.LowBaseTemplateFilePath))
                       && File.Exists(Cache.LowBaseTemplateImageFilePath);

            case 2:
                Cache.SiteDirection = StageDirectionTypeEnum.Up;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.TopLowSitePosition);
                return true;

            case 3:
                Cache.SiteDirection = StageDirectionTypeEnum.Down;
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.BottomLowSitePosition);
                return true;

            case 4:
                Cache.SiteDirection = StageDirectionTypeEnum.Left;
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LeftLowSitePosition);
                return true;

            case 5:
                Cache.SiteDirection = StageDirectionTypeEnum.Right;
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.RightLowSitePosition);
                return true;

            case 7:
                if (ResultCenterAndThetaItemDto is null)
                {
                    DialogWindowProvider.TryShowDialog("Calibration result is Empty!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    ResultCenterAndThetaItemDto.IsCalibrated = true;
                    if (Save(ResultCenterAndThetaItemDto, cancellationToken) == false)
                    {
                        ResultCenterAndThetaItemDto.IsCalibrated = false;
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        DialogWindowProvider.ShowDialog("Save Failed!", DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                        return false;
                    }
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
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.BaseLowSiteFindPosition);
                break;

            case 3:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.BaseHighSiteFindPosition);
                break;

            case 4:
                Cache.SiteDirection = StageDirectionTypeEnum.Up;
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.TopLowSitePosition);
                break;

            case 5:
                Cache.SiteDirection = StageDirectionTypeEnum.Down;
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.BottomLowSitePosition);
                break;

            case 6:
                Cache.SiteDirection = StageDirectionTypeEnum.Left;
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LeftLowSitePosition);
                break;

            case 7:
                Cache.SiteDirection = StageDirectionTypeEnum.Right;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.RightLowSitePosition);
                break;
        }

        return true;
    }

    #endregion 控制校准业务重载

    #region 校准

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

            Cache.ThetaAngle = StageViewModel.GetMachineStageTheta();
            Logger.LogHtmlInformation("P5 OK", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.P5Angle,
                Cache.ThetaAngle
            }),
                HtmlLogUniqueId.LoggingHtml());
            result = true;
            return result;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        if (Cache.HighMicroscopeLensInformation.LensCode <= Cache.LowMicroscopeLensInformation.LensCode)
        {
            DialogWindowProvider.ShowDialog("The high magnification less than or equal low magnification! Please select correct magnification!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var (isSuccess, errorMessage) = Cache.Step1Verify();
        if (isSuccess == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Cache varify error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
            DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var result = false;
        await InvokeCalibrateAsync(() =>
        {
            var position = StageViewModel.GetBrightFieldStagePosition();
            var baseBrightFieldPosition = Cache.BaseLowSiteFindPosition = position;

            if (baseBrightFieldPosition.ToOriginLength >= Cache.WaferRadius * 2)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Base Position is Out of Wafer!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.AlgorithmTemplateTypeEnum,
                Cache.WaferMaskTypeEnum,
                LowMagnification = Cache.LowMicroscopeLensInformation.LensName,
                HighMagnification = Cache.HighMicroscopeLensInformation.LensName,
                Cache.DiePitchWidth,
                Cache.DiePitchHeight,
                Cache.ReticleDieCountX,
                Cache.ReticleDieCountY,
                Cache.WaferRadius
            }), HtmlLogUniqueId.LoggingHtml());

            if (IsRecipeCalibrate == false)
            {
                // 用BuildDie的方式BuildReticle，防止取到圆外
                var waferMapReticleBuilder = new WaferMapDieBuilder
                {
                    DiePitchSize = new Size(Cache.DiePitchWidth * Cache.ReticleDieCountX, Cache.DiePitchHeight * Cache.ReticleDieCountY),
                    OriginalDiePoint = baseBrightFieldPosition
                };

                var reticles = waferMapReticleBuilder.BuildDie(new Circle(Point.Origin, Cache.WaferRadius));

                var currentColReticles = reticles
                    .Where(t => t.Index.X == 0)
                    .OrderBy(t => t.Index.Y).ToArray();
                var imageCount = currentColReticles.Length;
                Guard.IsGreaterThan(imageCount, 2);

                Cache.TopLowSitePosition = currentColReticles[^1].Rect.Point;
                Cache.BottomLowSitePosition = currentColReticles[0].Rect.Point;

                var currentRowReticles = reticles
                    .Where(t => t.Index.Y == 0)
                    .OrderBy(t => t.Index.X).ToArray();
                imageCount = currentRowReticles.Length;
                Guard.IsGreaterThan(imageCount, 2);

                Cache.LeftLowSitePosition = currentRowReticles[0].Rect.Point;
                Cache.RightLowSitePosition = currentRowReticles[^1].Rect.Point;

                Cache.LowBaseTemplateFilePath = $"{TemplateFileDirectory}\\Base_Low_{Cache.LowMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
                var generateTemplateHigh = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.LowBaseTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
                if (generateTemplateHigh == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                else Cache.LowBaseTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.LowBaseTemplateFilePath);
            }

            Logger.LogHtmlInformation("Idea Bright Field Positions", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.BaseLowSiteFindPosition,
                Cache.BaseHighSiteFindPosition,
                Cache.TopLowSitePosition,
                Cache.BottomLowSitePosition,
                Cache.LeftLowSitePosition,
                Cache.RightLowSitePosition,
                Cache.LowBaseTemplateFilePath,
                HtmlTab = new HtmlTab(new
                {
                    LowBaseTemplateImage = new HtmlImage(Cache.LowBaseTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            result = true;
            return result;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Cache.BaseHighSiteFindPosition = StageViewModel.GetBrightFieldStagePosition();

            Cache.HighBaseTemplateFilePath = $"{TemplateFileDirectory}\\Base_High_{Cache.HighMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            var generateTemplateHigh = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.HighBaseTemplateFilePath, Cache.AlgorithmTemplateSizeEnum);
            if (generateTemplateHigh == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            else Cache.HighBaseTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.HighBaseTemplateFilePath);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.BaseHighSiteFindPosition,
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

            Cache.SetPosition(lowPosition);

            Logger.LogHtmlInformation("Result", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.SiteDirection,
                Position = result
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step4CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
        await InvokeCalibrateAsync(() =>
        {
            var (isSuccess, errorMessage) = Cache.Step4Verify();
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Cache varify error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                result = false;
                return false;
            }

            ClearCalibrationTemp();

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.RotateAngle,
                Cache.CenterCalibrationThreshold,
                Cache.RotateScaleThreshold,
                Cache.BaseLowSiteFindPosition,
                Cache.BaseHighSiteFindPosition,
                Cache.LowToHighMagnificationOffset,
                Cache.TopLowSitePosition,
                Cache.BottomLowSitePosition,
                Cache.LeftLowSitePosition,
                Cache.RightLowSitePosition
            }), HtmlLogUniqueId.LoggingHtml());

            // 初始化
            var calibrationItemDto = new ChuckCenterAndThetaItemDto
            {
                LowMicroscopeLensInformation = Cache.LowMicroscopeLensInformation,
                HighMicroscopeLensInformation = Cache.HighMicroscopeLensInformation,
                AppliedScaleT = 1d,
                PositiveMatchResult = new ChuckGlobalTemplateMatchDtoItem
                {
                    LensInformation = Cache.HighMicroscopeLensInformation,
                    TopPosition = Cache.TopLowSitePosition.DegreeAngleByOrigin(Cache.ThetaAngle - Cache.RotateAngle),
                    BottomPosition = Cache.BottomLowSitePosition.DegreeAngleByOrigin(Cache.ThetaAngle - Cache.RotateAngle),
                    LeftPosition = Cache.LeftLowSitePosition.DegreeAngleByOrigin(Cache.ThetaAngle - Cache.RotateAngle),
                    RightPosition = Cache.RightLowSitePosition.DegreeAngleByOrigin(Cache.ThetaAngle - Cache.RotateAngle)
                },
                NegativeMatchResult = new ChuckGlobalTemplateMatchDtoItem
                {
                    LensInformation = Cache.HighMicroscopeLensInformation,
                    TopPosition = Cache.TopLowSitePosition.DegreeAngleByOrigin(Cache.ThetaAngle + Cache.RotateAngle),
                    BottomPosition = Cache.BottomLowSitePosition.DegreeAngleByOrigin(Cache.ThetaAngle + Cache.RotateAngle),
                    LeftPosition = Cache.LeftLowSitePosition.DegreeAngleByOrigin(Cache.ThetaAngle + Cache.RotateAngle),
                    RightPosition = Cache.RightLowSitePosition.DegreeAngleByOrigin(Cache.ThetaAngle + Cache.RotateAngle)
                }
            };

            var scaleCalibrationResult = false;
            foreach (var time in Enumerable.Range(1, Cache.Times))
            {
                cancellationToken.ThrowIfCancellationRequested();

                Logger.LogHtmlInformation($"Calibration Times: {time}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                var selectChuckCenterAndThetaItemDto = SelectCenterAndThetaItemDto = calibrationItemDto.Clone();
                SynchronizationContextProvider.Send(() => ChuckCenterAndThetaItemDtoList.Add(selectChuckCenterAndThetaItemDto));
                // 校准
                if (GetChuckCenterAndThetaScaleResult(selectChuckCenterAndThetaItemDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Calibration Result Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                if (Math.Abs(selectChuckCenterAndThetaItemDto.ScaleErrorUmAverage) < Cache.RotateScaleThreshold)
                {
                    scaleCalibrationResult = true;
                    break;
                }

                calibrationItemDto.AppliedScaleT *= selectChuckCenterAndThetaItemDto.ResultScaleT;
            }

            var chuckCenterPosition = new Point(
                ChuckCenterAndThetaItemDtoList.Average(t => t.ChuckCenterPosition.X),
                ChuckCenterAndThetaItemDtoList.Average(t => t.ChuckCenterPosition.Y));

            var bFCenterStagePosition = StageViewModel.GetBrightFieldCenterMachineStagePosition();
            var (xDirection, yDirection) = StageViewModel.GetMachineDirection();

            ResultCenterAndThetaItemDto = ChuckCenterAndThetaItemDtoList.Last();
            ResultCenterAndThetaItemDto.ChuckCenterPosition = chuckCenterPosition;
            ResultCenterAndThetaItemDto.BFCenterStagePosition = bFCenterStagePosition;
            ResultCenterAndThetaItemDto.NewBFCenterStagePosition = new Point(bFCenterStagePosition.X + xDirection * chuckCenterPosition.X, bFCenterStagePosition.Y + yDirection * chuckCenterPosition.Y);

            var chuckCenterCalibrationResult = chuckCenterPosition != Point.Origin
                                               && Math.Abs(chuckCenterPosition.X) < Cache.CenterCalibrationThreshold
                                               && Math.Abs(chuckCenterPosition.Y) < Cache.CenterCalibrationThreshold;

            result = scaleCalibrationResult && chuckCenterCalibrationResult;

            Logger.LogHtmlInformation($"Calibration Result {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                ChuckCenterIsOK = chuckCenterCalibrationResult,
                ThetaScaleIsOK = scaleCalibrationResult,
                XDirection = xDirection,
                YDirection = yDirection,
                ResultCenterAndThetaItemDto.ChuckCenterPosition,
                ResultCenterAndThetaItemDto.BFCenterStagePosition,
                ChuckCenterResult = ResultCenterAndThetaItemDto.NewBFCenterStagePosition,
                ResultCenterAndThetaItemDto.AppliedScaleT,
                ResultCenterAndThetaItemDto.RealAngleOffsetAverage,
                ResultCenterAndThetaItemDto.ScaleErrorUmAverage,
                ResultChuckCenterCurve = new HtmlPlot2DLinesChart([
                    ("PointX-PointY", ChuckCenterAndThetaItemDtoList.Select(t => t.ChuckCenterPosition).ToList())
                ], "ResultChuckCenterCurve"),
                ResultScaleValueCurve = new HtmlPlot2DLinesChart([
                    ("Times-ScaleResult", ChuckCenterAndThetaItemDtoList.Select(t => t.ResultScaleT).ToList().ToPoints())
                ], "ResultScaleValueCurve"),
                ResultScaleErrorUmCurve = new HtmlPlot2DLinesChart([
                    ("Times-ScaleErrorUm", ChuckCenterAndThetaItemDtoList.Select(t => t.ScaleErrorUmAverage).ToList().ToPoints())
                ], "ArcLengthErrorCurve"),
                ResultAngleOffsetCurve = new HtmlPlot2DLinesChart([
                    ("Times-RealAngleOffset", ChuckCenterAndThetaItemDtoList.Select(t => t.RealAngleOffsetAverage).ToList().ToPoints())
                ], "RealAngleOffset")
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

    private async Task<bool> VerifyCalibrationAsync(ChuckCenterAndThetaItemDto chuckCenterAndThetaItemDto, CancellationToken cancellationToken)
    {
        var result = true;
        await Task.Run(() =>
        {
            try
            {
                chuckCenterAndThetaItemDto.IsVerified = false;
                StageViewModel.SetBrightFieldCenterMachinePositionValue(chuckCenterAndThetaItemDto.NewBFCenterStagePosition);

                SelectCenterAndThetaItemDto = chuckCenterAndThetaItemDto.Clone();

                RecipeCacheProvider.Set(Cache, cancellationToken);

                if (GetChuckCenterAndThetaScaleResult(SelectCenterAndThetaItemDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Calibration Result Failed!"), HtmlLogUniqueId.LoggingHtml());
                    result = false;
                    return;
                }

                var bFCenterStagePosition = StageViewModel.GetBrightFieldCenterMachineStagePosition();
                var (xDirection, yDirection) = StageViewModel.GetMachineDirection();

                var chuckCenterPosition = SelectCenterAndThetaItemDto.ChuckCenterPosition;
                SelectCenterAndThetaItemDto.BFCenterStagePosition = bFCenterStagePosition;
                SelectCenterAndThetaItemDto.NewBFCenterStagePosition = new Point(bFCenterStagePosition.X + xDirection * chuckCenterPosition.X, bFCenterStagePosition.Y + yDirection * chuckCenterPosition.Y);

                var chuckCenterResult = chuckCenterPosition != Point.Origin
                                        && Math.Abs(chuckCenterPosition.X) < Cache.CenterVerifyThreshold
                                        && Math.Abs(chuckCenterPosition.Y) < Cache.CenterVerifyThreshold;

                var scaleResult = Math.Abs(SelectCenterAndThetaItemDto.ScaleErrorUmAverage) < Cache.RotateScaleThreshold;

                result = chuckCenterResult && scaleResult;

                Logger.LogHtmlInformation($"Calibration Result {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ChuckCenterIsOK = chuckCenterResult,
                    ThetaScaleIsOK = scaleResult,
                    XDirection = xDirection,
                    YDirection = yDirection,
                    SelectCenterAndThetaItemDto.ChuckCenterPosition,
                    SelectCenterAndThetaItemDto.BFCenterStagePosition,
                    ChuckCenterVerifyResult = SelectCenterAndThetaItemDto.NewBFCenterStagePosition,
                    SelectCenterAndThetaItemDto.AppliedScaleT,
                    SelectCenterAndThetaItemDto.RealAngleOffsetAverage,
                    SelectCenterAndThetaItemDto.ScaleErrorUmAverage
                }), HtmlLogUniqueId.LoggingHtml());

                chuckCenterAndThetaItemDto.IsVerified = result;
                if (Save(chuckCenterAndThetaItemDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    chuckCenterAndThetaItemDto.IsVerified = false;
                    result = false;
                    return;
                }

                StageViewModel.SetBrightFieldCenterMachinePositionValue(result ? chuckCenterAndThetaItemDto.NewBFCenterStagePosition : chuckCenterAndThetaItemDto.BFCenterStagePosition);

                if (!IsAutoCalibrate)
                {
                    DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, New ChuckCenter offset: ({SelectCenterAndThetaItemDto.ChuckCenterPosition})", DialogButtonsEnum.OK,
                        result ? DialogIconEnum.Information : DialogIconEnum.Warning);
                }

                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetBrightFieldAbsoluteStageXy(new Point(0, 0));
            }
            catch (Exception ex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Verify exception!" + ex.Message), HtmlLogUniqueId.LoggingHtml());

                StageViewModel.SetBrightFieldCenterMachinePositionValue(chuckCenterAndThetaItemDto.BFCenterStagePosition);
            }
        }, cancellationToken);
        return result;
    }

    private bool GetChuckCenterAndThetaScaleResult(ChuckCenterAndThetaItemDto chuckCenterAndThetaItemDto, CancellationToken cancellationToken)
    {
        try
        {
            StageViewModel.SetTScale(chuckCenterAndThetaItemDto.AppliedScaleT);
            // 正向
            Logger.LogHtmlInformation("Positive Rotate", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
            StageViewModel.SetAbsoluteStageTheta(Cache.RotateAngle);
            chuckCenterAndThetaItemDto.IsPositive = true;
            foreach (var siteDirectionTypeEnum in _stageDirectionTypeEnums)
            {
                cancellationToken.ThrowIfCancellationRequested();

                chuckCenterAndThetaItemDto.SiteDirection = siteDirectionTypeEnum;
                if (MatchTemplate() == false) return false;
            }

            // 反向
            Logger.LogHtmlInformation("Negative Rotate", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
            StageViewModel.SetAbsoluteStageTheta(-Cache.RotateAngle);
            chuckCenterAndThetaItemDto.IsPositive = false;
            foreach (var siteDirectionTypeEnum in _stageDirectionTypeEnums)
            {
                cancellationToken.ThrowIfCancellationRequested();

                chuckCenterAndThetaItemDto.SiteDirection = siteDirectionTypeEnum;
                if (MatchTemplate() == false) return false;
            }

            #region Center

            var chuckCenterPosition = CalibrationAlgorithmService.GetChuckCenter(
                chuckCenterAndThetaItemDto.PositiveMatchResult.TopPosition,
                chuckCenterAndThetaItemDto.NegativeMatchResult.TopPosition,
                chuckCenterAndThetaItemDto.NegativeMatchResult.RightPosition,
                chuckCenterAndThetaItemDto.PositiveMatchResult.RightPosition,
                chuckCenterAndThetaItemDto.PositiveMatchResult.BottomPosition,
                chuckCenterAndThetaItemDto.NegativeMatchResult.BottomPosition,
                chuckCenterAndThetaItemDto.NegativeMatchResult.LeftPosition,
                chuckCenterAndThetaItemDto.PositiveMatchResult.LeftPosition
            );

            chuckCenterAndThetaItemDto.ChuckCenterPosition = chuckCenterPosition;

            #endregion Center

            #region Scale

            if (GetAverageScaleResult() == false) return false;

            if (hostEnvironment.IsProduction() && chuckCenterAndThetaItemDto.ResultScaleT is >= 2 or 0) // 防止下发异常值
            {
                DialogWindowProvider.ShowDialog("Result scale is illegal !", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            #endregion Scale

            Logger.LogHtmlInformation("Get Result OK", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                PositiveTopHighSiteFindPosition = chuckCenterAndThetaItemDto.PositiveMatchResult.TopPosition,
                PositiveBottomHighSiteFindPosition = chuckCenterAndThetaItemDto.PositiveMatchResult.BottomPosition,
                PositiveLeftHighSiteFindPosition = chuckCenterAndThetaItemDto.PositiveMatchResult.LeftPosition,
                PositiveRightHighSiteFindPosition = chuckCenterAndThetaItemDto.PositiveMatchResult.RightPosition,
                NegativeTopHighSiteFindPosition = chuckCenterAndThetaItemDto.NegativeMatchResult.TopPosition,
                NegativeBottomHighSiteFindPosition = chuckCenterAndThetaItemDto.NegativeMatchResult.BottomPosition,
                NegativeLeftHighSiteFindPosition = chuckCenterAndThetaItemDto.NegativeMatchResult.LeftPosition,
                NegativeRightHighSiteFindPosition = chuckCenterAndThetaItemDto.NegativeMatchResult.RightPosition,
                chuckCenterAndThetaItemDto.ChuckCenterPosition,
                chuckCenterAndThetaItemDto.AppliedScaleT,
                chuckCenterAndThetaItemDto.ResultScaleT,
                chuckCenterAndThetaItemDto.RealAngleOffsetAverage,
                PositiveMatchResultTab = new HtmlTab(new
                {
                    TopHighSiteResultImage = new HtmlImage(chuckCenterAndThetaItemDto.PositiveMatchResult.TopFindResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    BottomHighSiteResultImage = new HtmlImage(chuckCenterAndThetaItemDto.PositiveMatchResult.BottomFindResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    LeftHighSiteResultImage = new HtmlImage(chuckCenterAndThetaItemDto.PositiveMatchResult.LeftFindResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    RightHighSiteResultImage = new HtmlImage(chuckCenterAndThetaItemDto.PositiveMatchResult.RightFindResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                }),
                NegativeMatchResultTab = new HtmlTab(new
                {
                    TopHighSiteResultImage = new HtmlImage(chuckCenterAndThetaItemDto.NegativeMatchResult.TopFindResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    BottomHighSiteResultImage = new HtmlImage(chuckCenterAndThetaItemDto.NegativeMatchResult.BottomFindResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    LeftHighSiteResultImage = new HtmlImage(chuckCenterAndThetaItemDto.NegativeMatchResult.LeftFindResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    RightHighSiteResultImage = new HtmlImage(chuckCenterAndThetaItemDto.NegativeMatchResult.RightFindResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Result Failed", Name);
            return false;
        }

        bool MatchTemplate()
        {
            var stageDirection = chuckCenterAndThetaItemDto.SiteDirection;
            var affineIdeaPosition = chuckCenterAndThetaItemDto.GetPosition();

            if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, affineIdeaPosition, Cache.LowMicroscopeLensInformation, Cache.LowBaseTemplateFilePath, ImageFileDirectory,
                    null, Name,
                    $"Low Magnification {stageDirection} Site", out var lowResultPosition, out _, out _, out var lowResultImageFilePath, out _) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Low Magnification {stageDirection} Site Matching Failed!"), HtmlLogUniqueId.LoggingHtml());
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(lowResultPosition), "Low Magnification Matching Failed!");
            }

            if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, lowResultPosition + (Vector)Cache.LowToHighMagnificationOffset, Cache.HighMicroscopeLensInformation, Cache.HighBaseTemplateFilePath, ImageFileDirectory,
                    null, Name,
                    $"High Magnification {stageDirection} Site", out var highResultPosition, out _, out _, out var highResultImageFilePath, out _) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: High Magnification {stageDirection} Site Matching Failed!"), HtmlLogUniqueId.LoggingHtml());
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(highResultPosition), "High Magnification Matching Failed!");
            }

            chuckCenterAndThetaItemDto.SetMatchResultInfo(highResultPosition, highResultImageFilePath);

            Logger.LogHtmlInformation($"{stageDirection} site Result", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                affineIdeaPosition,
                FindLowSiteResultPosition = lowResultPosition,
                FindHighSiteResultPosition = highResultPosition,
                HtmlTab = new HtmlTab(new
                {
                    LowMatchImage = new HtmlImage(lowResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighMatchImage = new HtmlImage(highResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        }

        bool GetAverageScaleResult()
        {
            try
            {
                List<double> realAngleOffsetList = [];
                foreach (var stageDirectionTypeEnum in _stageDirectionTypeEnums)
                {
                    var (positiveFindPosition, negativeFindPosition) = chuckCenterAndThetaItemDto.GetCoupleRealPosition(stageDirectionTypeEnum);
                    var realAngleOffset = GetRealRotateAngle(positiveFindPosition, negativeFindPosition, chuckCenterAndThetaItemDto.ChuckCenterPosition);
                    realAngleOffsetList.Add(realAngleOffset);
                }

                var realAngleOffsetAverage = realAngleOffsetList.Average();
                var scaleValueAverage = Cache.RotateAngle * 2 / realAngleOffsetAverage;

                var angleError = Cache.RotateAngle * 2 * (1 - scaleValueAverage);
                var radianError = (Math.PI / 180) * angleError;
                chuckCenterAndThetaItemDto.ResultScaleT = (Cache.RotateAngle * 2) / realAngleOffsetAverage;
                chuckCenterAndThetaItemDto.RealAngleOffsetAverage = realAngleOffsetAverage;
                chuckCenterAndThetaItemDto.ScaleErrorUmAverage = Cache.WaferRadius * 2 * radianError;
                Logger.LogHtmlInformation("Theta Scale Result", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                {
                    chuckCenterAndThetaItemDto.AppliedScaleT,
                    chuckCenterAndThetaItemDto.ResultScaleT,
                    chuckCenterAndThetaItemDto.RealAngleOffsetAverage,
                    chuckCenterAndThetaItemDto.ScaleErrorUmAverage
                }), HtmlLogUniqueId.LoggingHtml());

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Get Average Scale Result Failed!{ex.Message}"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            double GetRealRotateAngle(Point positivePosition, Point negativePosition, Point rotateCenterPosition)
            {
                // 向量AB和AC
                var abX = positivePosition.X - rotateCenterPosition.X;
                var abY = positivePosition.Y - rotateCenterPosition.Y;
                var acX = negativePosition.X - rotateCenterPosition.X;
                var acY = negativePosition.Y - rotateCenterPosition.Y;

                // 向量的点积
                var dotProduct = abX * acX + abY * acY;

                // 向量的模长
                var magnitudeAb = Math.Sqrt(abX * abX + abY * abY);
                var magnitudeAc = Math.Sqrt(acX * acX + acY * acY);

                // 计算角度的余弦值
                var cosTheta = dotProduct / (magnitudeAb * magnitudeAc);

                // 通过反余弦函数计算角度（弧度）
                var angleRadians = Math.Acos(cosTheta);

                // 将角度转换为度
                var angleDegrees = angleRadians * (180.0 / Math.PI);
                return angleDegrees;
            }
        }
    }

    private bool Save(ChuckCenterAndThetaItemDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        dto.LowMicroscopeLensInformation = Cache.LowMicroscopeLensInformation;
        dto.HighMicroscopeLensInformation = Cache.HighMicroscopeLensInformation;

        Calibration = dto.Clone();

        CacheProvider.Set(dto, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    }) && EnableDependedCalibrationItems(cancellationToken);

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(ChuckCenterAndThetaItemDtoList.Clear);
        ResultCenterAndThetaItemDto = null;
    }

    #endregion 校准

    #region 自动化校准

    public override void GetAutoCalibrationStep()
    {
        AutoCalibrationStepList =
        [
            new CalibrationItemStep { StepName = "loading" },
            new CalibrationItemStep { StepName = "Find Positive Point" },
            new CalibrationItemStep { StepName = "Find Negative Point" },
            new CalibrationItemStep { StepName = "Review" }
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
                    if (await Step4CalibrateActionAsync(cancellationToken) == false) return false;
                    CalibrationStepIndex = 5;
                    if (await NextingAsync(cancellationToken) == false) return false;
                    if (await AutoNextingAsync(cancellationToken) == false) return false;
                    break;

                case 2:
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
        var centerReticle = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleModel
            .Single(t => t.Index is { X: 0, Y: 0 });

        Cache.BaseLowSiteFindPosition = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleBuilder.OriginalDiePoint;
        Cache.TopLowSitePosition = reticleRows[^2].Rect.Point;
        Cache.RightLowSitePosition = reticleCols[^2].Rect.Point;
        Cache.BottomLowSitePosition = reticleRows[1].Rect.Point;
        Cache.LeftLowSitePosition = reticleCols[1].Rect.Point;

        #region 低倍

        if (CalibrationRecipeService.GetChuckReticleMaskInfo(Cache.WaferMaskTypeEnum, Cache.LowMicroscopeLensInformation, opticsMagType: null, out var maskInfoLow) == false)
            return false;
        Cache.LowBaseTemplateFilePath = maskInfoLow.RecipeBrightFieldTemplateDto.TemplateFilePath;
        Cache.LowBaseTemplateImageFilePath = maskInfoLow.RecipeBrightFieldTemplateDto.TemplateImageFilePath;

        #endregion 低倍

        #region 高倍

        if (CalibrationRecipeService.GetChuckReticleMaskInfo(Cache.WaferMaskTypeEnum, Cache.HighMicroscopeLensInformation, opticsMagType: null, out var maskInfoHigh) == false)
            return false;
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(centerReticle, maskInfoHigh, out var highTopPosition);
        Cache.BaseHighSiteFindPosition = highTopPosition;
        Cache.HighBaseTemplateFilePath = maskInfoHigh.RecipeBrightFieldTemplateDto.TemplateFilePath;
        Cache.HighBaseTemplateImageFilePath = maskInfoHigh.RecipeBrightFieldTemplateDto.TemplateImageFilePath;

        #endregion 高倍

        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.TopLowSitePosition);
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