using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.PixelSize;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
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

[IOCAppService(ServiceType = typeof(ChuckGlobalScaleErrorCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckGlobalScaleErrorCalibrationViewModel(AlignmentWindowBrightFieldViewModel alignmentWindowBrightFieldViewModel, IHostEnvironment hostEnvironment) : CalibrationViewModelBase
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

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ChuckGlobalScaleErrorDto? _resultGlobalScaleErrorDto = new();

    [ObservableProperty]
    private ObservableCollection<ChuckGlobalScaleErrorDto> _globalScaleErrorDtoItemDtoList = [];

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ChuckGlobalScaleErrorDto? _selectGlobalScaleErrorDto = new();

    [ObservableProperty]
    private ChuckGlobalScaleErrorDto _reviewDto = new();

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private ChuckGlobalScaleErrorCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private ChuckGlobalScaleErrorDto _calibration = new();

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

    #endregion 缓存

    #endregion 属性

    private readonly StageDirectionTypeEnum[] _stageDirectionTypeEnums = [StageDirectionTypeEnum.Up, StageDirectionTypeEnum.Down, StageDirectionTypeEnum.Left, StageDirectionTypeEnum.Right];

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        MicroscopePixelSizeItems = CalibrationStatusService.GetCalibrations<MicroscopePixelSizeItemDto>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<ChuckGlobalScaleErrorCache>();
        Calibration = CacheProvider.GetOrDefault<ChuckGlobalScaleErrorDto>();
        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();

        if (Cache.LowMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.LowMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();
        if (Cache.HighMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.HighMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.HighMicroscopeLensInformation.Clone();

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
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
        return true;
    }

    protected override async Task<bool> CancelingAsync()
    {
        if (CacheProvider.GetOrDefault<ChuckGlobalScaleErrorDto>().IsOk == false)
            StageViewModel.ResetXYGlobalScale();

        return await base.CancelingAsync().ConfigureAwait(false);
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
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.BaseLowSiteFindPosition);
                break;

            case 3:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.BaseHighSiteFindPosition);
                break;

            case 4:
                Cache.SiteDirection = StageDirectionTypeEnum.Up;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.TopLowSitePosition);
                break;

            case 5:
                Cache.SiteDirection = StageDirectionTypeEnum.Down;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.BottomLowSitePosition);
                break;

            case 6:
                Cache.SiteDirection = StageDirectionTypeEnum.Left;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
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
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.BottomLowSitePosition);
                return true;

            case 4:
                Cache.SiteDirection = StageDirectionTypeEnum.Left;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LeftLowSitePosition);
                return true;

            case 5:
                Cache.SiteDirection = StageDirectionTypeEnum.Right;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.RightLowSitePosition);
                return true;

            case 7:
                if (ResultGlobalScaleErrorDto is null)
                {
                    DialogWindowProvider.TryShowDialog("Calibration result is Empty!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    ResultGlobalScaleErrorDto.IsCalibrated = true;
                    if (Save(ResultGlobalScaleErrorDto, cancellationToken) == false)
                    {
                        ResultGlobalScaleErrorDto.IsCalibrated = false;
                        StageViewModel.ResetXYGlobalScale();
                        Logger.LogError("{@Name} Error: Save Failed!", Name);
                        return false;
                    }
                }

                IsCalibrated = true;

                ClearCalibrationTemp();
                return true;

            default:
                return true;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = false;

        await InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                LowSite1 = AlignmentCacheBrightField.LowSite1.Location,
                LowSite2 = AlignmentCacheBrightField.LowSite2.Location,
                HighSite1 = AlignmentCacheBrightField.HighSite1.Location,
                HighSite2 = AlignmentCacheBrightField.HighSite2.Location
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

        var (isSuccess, errorMessage) = Cache.Verify();
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

            if (baseBrightFieldPosition.ToOriginLength >= Cache.WaferRadius)
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
        var result = false;
        await InvokeCalibrateAsync(() =>
        {
            ClearCalibrationTemp();
            StageViewModel.SetXYGlobalScale(1, 1);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Threshold,
                Cache.LowToHighMagnificationOffset
            }), HtmlLogUniqueId.LoggingHtml());

            StageViewModel.ResetXYGlobalScale();

            // 初始化
            var calibrationItemDto = new ChuckGlobalScaleErrorDto
            {
                LowMicroscopeLensInformation = Cache.LowMicroscopeLensInformation,
                HighMicroscopeLensInformation = Cache.HighMicroscopeLensInformation,
                HighSiteMatchResult = new ChuckGlobalTemplateMatchDtoItem
                {
                    LensInformation = Cache.HighMicroscopeLensInformation,
                    TopPosition = Cache.TopLowSitePosition,
                    BottomPosition = Cache.BottomLowSitePosition,
                    LeftPosition = Cache.LeftLowSitePosition,
                    RightPosition = Cache.RightLowSitePosition
                }
            };

            var calibrationResult = false;
            foreach (var time in Enumerable.Range(1, Cache.Times))
            {
                cancellationToken.ThrowIfCancellationRequested();

                Logger.LogHtmlInformation($"Calibration Times: {time}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                var selectGlobalScaleErrorDto = SelectGlobalScaleErrorDto = calibrationItemDto.Clone();
                SynchronizationContextProvider.Send(() => GlobalScaleErrorDtoItemDtoList.Add(selectGlobalScaleErrorDto));
                // 校准
                if (GetChuckGlobalScaleResult(selectGlobalScaleErrorDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Calibration Result Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                if (Math.Abs(selectGlobalScaleErrorDto.ScaleErrorValue.X) < Cache.Threshold.X
                    && Math.Abs(selectGlobalScaleErrorDto.ScaleErrorValue.Y) < Cache.Threshold.Y)
                {
                    calibrationResult = true;
                    break;
                }

                calibrationItemDto.AppliedScaleXY = new System.Windows.Point
                (
                    calibrationItemDto.AppliedScaleXY.X * selectGlobalScaleErrorDto.ResultScaleXY.X,
                    calibrationItemDto.AppliedScaleXY.Y * selectGlobalScaleErrorDto.ResultScaleXY.Y
                );
            }

            ResultGlobalScaleErrorDto = GlobalScaleErrorDtoItemDtoList.Last().Clone();

            Logger.LogHtmlInformation($"Calibration {(calibrationResult ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                ResultGlobalScaleErrorDto.AppliedScaleXY,
                ResultGlobalScaleErrorDto.ResultScaleXY,
                ResultGlobalScaleErrorDto.ScaleErrorValue,
                ScaleXYErrorUmCurve = new HtmlPlot2DLinesChart([
                    ("Times-AxisXScaleError(Um)", GlobalScaleErrorDtoItemDtoList.Select(t => t.ScaleErrorValue.X).ToList().ToPoints()),
                    ("Times-AxisYScaleError(Um)", GlobalScaleErrorDtoItemDtoList.Select(t => t.ScaleErrorValue.Y).ToList().ToPoints())
                ], "ScaleXYErrorUmCurve"),
                AppliedScaleXYCurve = new HtmlPlot2DLinesChart([
                    ("Times-AxisXAppliedScale", GlobalScaleErrorDtoItemDtoList.Select(t => t.AppliedScaleXY.X).ToList().ToPoints()),
                    ("Times-AxisYAppliedScale", GlobalScaleErrorDtoItemDtoList.Select(t => t.AppliedScaleXY.Y).ToList().ToPoints())
                ], "AppliedScaleXYCurve"),
                ResultScaleXYCurve = new HtmlPlot2DLinesChart([
                    ("Times-AxisXResultScale", GlobalScaleErrorDtoItemDtoList.Select(t => t.ResultScaleXY.X).ToList().ToPoints()),
                    ("Times-AxisYResultScale", GlobalScaleErrorDtoItemDtoList.Select(t => t.ResultScaleXY.Y).ToList().ToPoints())
                ], "AppliedScaleXYCurve")
            }), HtmlLogUniqueId.LoggingHtml());

            result = calibrationResult;
            return calibrationResult;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        var result = true;

        await InvokeVerifyAsync(async () =>
        {
            if (await VerifyCalibrationAsync(cancellationToken) == false) result = false;
            return result;
        }).ConfigureAwait(false);
    }

    private async Task<bool> VerifyCalibrationAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await Task.Run(() =>
        {
            ReviewDto.IsVerified = false;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                LowMagnification = Cache.LowMicroscopeLensInformation.LensName,
                HighMagnification = Cache.HighMicroscopeLensInformation.LensName,
                Cache.AlgorithmTemplateTypeEnum,
                Cache.BaseLowSiteFindPosition,
                Cache.BaseHighSiteFindPosition,
                Cache.LowToHighMagnificationOffset,
                Cache.Threshold,
                Cache.TopLowSitePosition,
                Cache.BottomLowSitePosition,
                Cache.LeftLowSitePosition,
                Cache.RightLowSitePosition,
                CalibrationAppliedScaleXY = ReviewDto.AppliedScaleXY,
                ReviewDto.ScaleErrorValue
            }), HtmlLogUniqueId.LoggingHtml());

            SelectGlobalScaleErrorDto = ReviewDto.Clone();

            if (GetChuckGlobalScaleResult(SelectGlobalScaleErrorDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Calibration Result Failed!"), HtmlLogUniqueId.LoggingHtml());
                result = false;
                return false;
            }

            var verifyResult = Math.Abs(SelectGlobalScaleErrorDto.ScaleErrorValue.X) < Cache.Threshold.X
                               && Math.Abs(SelectGlobalScaleErrorDto.ScaleErrorValue.Y) < Cache.Threshold.Y;

            ReviewDto.IsVerified = verifyResult;

            Logger.LogHtmlInformation($"Verify {(verifyResult ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                VerifyAppliedScaleXY = SelectGlobalScaleErrorDto.AppliedScaleXY,
                VerifyResultScaleXY = SelectGlobalScaleErrorDto.ResultScaleXY,
                VerifyScaleXYUmErrorResult = SelectGlobalScaleErrorDto.ScaleErrorValue
            }), HtmlLogUniqueId.LoggingHtml());

            if (Save(ReviewDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                ReviewDto.IsVerified = false;
                return false;
            }

            result = ReviewDto.IsVerified;

            DialogWindowProvider.ShowDialog($"Verify {(result ? "Success" : "Failed")}!", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        });
        return result;
    }

    private bool Save(ChuckGlobalScaleErrorDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        CacheProvider.Set(dto, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    }) && EnableDependedCalibrationItems(cancellationToken);

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(GlobalScaleErrorDtoItemDtoList.Clear);
        ResultGlobalScaleErrorDto = null;
    }

    #endregion 校准

    #region 算法

    private bool GetChuckGlobalScaleResult(ChuckGlobalScaleErrorDto chuckGlobalScaleErrorDto, CancellationToken cancellationToken)
    {
        try
        {
            StageViewModel.SetXYGlobalScale(chuckGlobalScaleErrorDto.AppliedScaleXY.X, chuckGlobalScaleErrorDto.AppliedScaleXY.Y);
            foreach (var siteDirectionTypeEnum in _stageDirectionTypeEnums)
            {
                cancellationToken.ThrowIfCancellationRequested();

                chuckGlobalScaleErrorDto.SiteDirection = siteDirectionTypeEnum;
                if (MatchTemplate() == false) return false;
            }

            #region Scale

            if (GetAverageScaleResult() == false) return false;

            if (hostEnvironment.IsProduction() &&
                (chuckGlobalScaleErrorDto.ResultScaleXY.X == 0
                 || chuckGlobalScaleErrorDto.ResultScaleXY.X >= 2
                 || chuckGlobalScaleErrorDto.ResultScaleXY.Y == 0
                 || chuckGlobalScaleErrorDto.ResultScaleXY.Y >= 2)) // 防止下发异常值
            {
                DialogWindowProvider.ShowDialog("Result scale is illegal !", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            #endregion Scale

            Logger.LogHtmlInformation("Get Result OK", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                TopHighSiteFindPosition = chuckGlobalScaleErrorDto.HighSiteMatchResult.TopPosition,
                BottomHighSiteFindPosition = chuckGlobalScaleErrorDto.HighSiteMatchResult.BottomPosition,
                LeftHighSiteFindPosition = chuckGlobalScaleErrorDto.HighSiteMatchResult.LeftPosition,
                RightHighSiteFindPosition = chuckGlobalScaleErrorDto.HighSiteMatchResult.RightPosition,
                chuckGlobalScaleErrorDto.AppliedScaleXY,
                chuckGlobalScaleErrorDto.ResultScaleXY,
                chuckGlobalScaleErrorDto.ScaleErrorValue,
                HighSiteMatchResultTab = new HtmlTab(new
                {
                    TopHighSiteResultImage = new HtmlImage(chuckGlobalScaleErrorDto.HighSiteMatchResult.TopFindResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    BottomHighSiteResultImage = new HtmlImage(chuckGlobalScaleErrorDto.HighSiteMatchResult.BottomFindResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    LeftHighSiteResultImage = new HtmlImage(chuckGlobalScaleErrorDto.HighSiteMatchResult.LeftFindResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    RightHighSiteResultImage = new HtmlImage(chuckGlobalScaleErrorDto.HighSiteMatchResult.RightFindResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
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
            var stageDirection = chuckGlobalScaleErrorDto.SiteDirection;
            var ideaLowSitePosition = chuckGlobalScaleErrorDto.GetPosition(stageDirection);

            if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, ideaLowSitePosition, Cache.LowMicroscopeLensInformation, Cache.LowBaseTemplateFilePath, ImageFileDirectory,
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

            chuckGlobalScaleErrorDto.SetMatchResultInfo(highResultPosition, highResultImageFilePath);

            Logger.LogHtmlInformation($"{stageDirection} site Result", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                ideaLowSitePosition,
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
                var xRealError = chuckGlobalScaleErrorDto.HighSiteMatchResult.RightPosition - chuckGlobalScaleErrorDto.HighSiteMatchResult.LeftPosition;
                var yRealError = chuckGlobalScaleErrorDto.HighSiteMatchResult.TopPosition - chuckGlobalScaleErrorDto.HighSiteMatchResult.BottomPosition;

                var resultScaleX = Math.Abs(xRealError.X / Cache.IdeaWidth);
                var resultScaleY = Math.Abs(yRealError.Y / Cache.IdeaHeight);

                chuckGlobalScaleErrorDto.ScaleErrorValue = new Point
                (
                    Math.Abs(Cache.WaferRadius * 2 * (1 - resultScaleX)),
                    Math.Abs(Cache.WaferRadius * 2 * (1 - resultScaleY))
                );

                chuckGlobalScaleErrorDto.ResultScaleXY = new System.Windows.Point(resultScaleX, resultScaleY);

                Logger.LogHtmlInformation("XY Scale Result", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                {
                    chuckGlobalScaleErrorDto.AppliedScaleXY,
                    chuckGlobalScaleErrorDto.ResultScaleXY,
                    chuckGlobalScaleErrorDto.ScaleErrorValue
                }), HtmlLogUniqueId.LoggingHtml());

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Get Scale Result Failed!{ex.Message}"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }
        }
    }

    #endregion 算法
}