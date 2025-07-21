using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.Center;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Models.Models.Pattern;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
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
        new() { StepName = "Calibration offset" },
        new() { StepName = "Low" },
        new() { StepName = "High" },
        new() { StepName = "Calibration Result" }
    ];


    #region 界面相关

    [ObservableProperty]
    private ChuckPrealignerObjDto _chuckPrealignerObjDto = new();


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
    private ChuckCenterObjDto _chuckCenter = new();

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

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckCenterObjDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<ChuckPrealignerCache>();
        Calibration = CacheProvider.GetOrDefault<ChuckPrealignerObjDto>();
        CalibrationStepList[0].StepIsNextEnable = false;

        if (Cache.LowMicroscopeMagnificationInfo.MagnificationCode == -1) Cache.LowMicroscopeMagnificationInfo = ApplicationCookie.MicroscopeMagnificationInfoList[0];
        if (Cache.HighMicroscopeMagnificationInfo.MagnificationCode == -1)
            Cache.HighMicroscopeMagnificationInfo = ApplicationCookie.MicroscopeMagnificationInfoList.Count <= 2
                ? ApplicationCookie.MicroscopeMagnificationInfoList[^1]
                : ApplicationCookie.MicroscopeMagnificationInfoList[2];
        return isHasCache || RecipeCacheProvider.Set(Cache, cancellationToken);
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

        MicroscopeViewModel.SwitchMagnification(Cache.LowMicroscopeMagnificationInfo);
        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                MicroscopeViewModel.SwitchMagnification(Cache.LowMicroscopeMagnificationInfo);
                return true;
            case 1:
                MicroscopeViewModel.SwitchMagnification(Cache.HighMicroscopeMagnificationInfo);
                Cache.HighFindPosition1 = Cache.LowFindPosition1;
                Cache.HighFindPosition2 = Cache.LowFindPosition2;
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowFindPosition1);
                return true;
            case 2:
                return true;
            case 3:
                ChuckPrealignerObjDto.IsCalibrated = true;
                if (Save(ChuckPrealignerObjDto, cancellationToken) == false)
                {
                    ChuckPrealignerObjDto.IsCalibrated = false;
                    Logger.LogError("{@Name} Error: Save Failed!", Name);
                    return false;
                }

                IsCalibrated = true;
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
            Logger.LogInformation("{@Name}: Get Point Image Start", Name);
            await Task.Run(() =>
            {
                var result = StageViewModel.GetBrightFieldStagePosition();

                switch (parameter)
                {
                    case nameof(Cache.LowFindPosition1):
                        Cache.LowFindPosition1 = result;
                        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.LowFindPosition1);
                        Cache.LowTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.LowMicroscopeMagnificationInfo.MicroscopeMagnificationName}_{Guid.NewGuid()}";
                        Cache.LowTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.LowTemplateFilePath);
                        Cache.LowSite1.Location = Cache.LowFindPosition1;
                        var resultLowSite1 = StageViewModel.MarkAlignSite1(Cache.LowSizeEnum, Cache.AlgorithmTemplateTypeEnum, Cache.AlgorithmWaferTypeEnum);
                        if (resultLowSite1.Template is null) return;
                        BitmapSourceHelper.Save(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(resultLowSite1.Template.Thumb), Cache.LowTemplateImageFilePath);
                        Cache.LowSite1 = resultLowSite1;
                        Cache.LowSite1.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
                        Cache.LowSite1.TemplateMatchScoreThreshold = Cache.NccTypeTemplateMatchScoreThreshold;

                        break;

                    case nameof(Cache.LowFindPosition2):
                        Cache.LowFindPosition2 = result;
                        Cache.LowSite2.Location = Cache.LowFindPosition2;
                        var resultLowSite2 = StageViewModel.MarkAlignSite2(Cache.LowSite1, Cache.AlgorithmWaferTypeEnum);
                        Cache.LowSite2 = resultLowSite2;
                        Cache.LowSite2.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
                        Cache.LowSite2.TemplateMatchScoreThreshold = Cache.NccTypeTemplateMatchScoreThreshold;

                        break;

                    case nameof(Cache.HighFindPosition1):
                        Cache.HighFindPosition1 = result;
                        Cache.HighTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.HighMicroscopeMagnificationInfo.MicroscopeMagnificationName}_{Guid.NewGuid()}";
                        Cache.HighTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.HighTemplateFilePath);
                        Cache.HighSite1.Location = Cache.HighFindPosition1;
                        var resultHighSite1 = StageViewModel.MarkAlignSite1(Cache.HighSizeEnum, Cache.AlgorithmTemplateTypeEnum, Cache.AlgorithmWaferTypeEnum);
                        if (resultHighSite1.Template is null) return;
                        BitmapSourceHelper.Save(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(resultHighSite1.Template.Thumb), Cache.HighTemplateImageFilePath);
                        Cache.HighSite1 = resultHighSite1;
                        Cache.HighSite1.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
                        Cache.HighSite1.TemplateMatchScoreThreshold = Cache.NccTypeTemplateMatchScoreThreshold;

                        break;

                    case nameof(Cache.HighFindPosition2):
                        Cache.HighFindPosition2 = result;
                        Cache.HighSite2.Location = Cache.HighFindPosition2;
                        var resultHighSite2 = StageViewModel.MarkAlignSite2(Cache.HighSite1, Cache.AlgorithmWaferTypeEnum);
                        Cache.HighSite2 = resultHighSite2;
                        Cache.HighSite2.AlgorithmTemplateTypeEnum = Cache.AlgorithmTemplateTypeEnum;
                        Cache.HighSite2.TemplateMatchScoreThreshold = Cache.NccTypeTemplateMatchScoreThreshold;
                        break;

                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(parameter));
                        break;
                }

                Logger.LogInformation("{@Name}: Get Point Image OK!", Name);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Point Image Failed", Name);
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
                    "LowFindPosition1" => Cache.LowFindPosition1,
                    "LowFindPosition2" => Cache.LowFindPosition2,
                    "HighFindPosition1" => Cache.HighFindPosition1,
                    "HighFindPosition2" => Cache.HighFindPosition2,
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
    private async Task MagnificationSelectedAsync(object obj)
    {
        try
        {
            if (obj is not MicroscopeMagnificationInfo)
                Logger.LogError("{@Name}: Select magnification illegal!", Name);

            await Task.Run(() => MicroscopeViewModel.SwitchMagnification(ApplicationCookie.MicroscopeMagnificationInfoList.Single(t => t == (MicroscopeMagnificationInfo)obj))
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
        }
    }

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
                Cache.PositionErrorThreshold
            }), HtmlLogUniqueId.LoggingHtml());
            var (offsetPosition, bitmapMemoryBytes) = StageViewModel.FindWaferCenterByManually(Point.Origin, waferEdgeOffsets);
            Cache.OffsetPosition = offsetPosition;

            if (bitmapMemoryBytes is not null && bitmapMemoryBytes.Count > 0)
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

            if (Math.Abs(Cache.OffsetPosition.X) > Cache.PositionErrorThreshold || Math.Abs(Cache.OffsetPosition.Y) > Cache.PositionErrorThreshold)
            {
                CalibrationStepList[CalibrationStepIndex].StepIsNextEnable = false;

                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    OffsetX = Cache.OffsetPosition.X,
                    OffsetY = Cache.OffsetPosition.Y
                }), HtmlLogUniqueId.LoggingHtml());

                DialogWindowProvider.ShowDialog(" Chuck Prealigner calibration failed, please manually adjust EFEM.", DialogButtonsEnum.OK, DialogIconEnum.Error);
                result = false;
                return result;
            }

            ChuckPrealignerObjDto.OffsetPosition = Cache.OffsetPosition;
            var (xDirection, yDirection) = StageViewModel.GetMachineDirection();
            var efemLoadWaferStagePosition = StageViewModel.GetEfemLoadWaferMachineStagePosition();
            ChuckPrealignerObjDto.EfemLoadWaferStagePosition = efemLoadWaferStagePosition;
            ChuckPrealignerObjDto.NewEfemLoadWaferStagePosition = new Point(efemLoadWaferStagePosition.X - xDirection * Cache.OffsetPosition.X, efemLoadWaferStagePosition.Y - yDirection * Cache.OffsetPosition.Y);
            Logger.LogHtmlInformation("Init center result OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                ChuckPrealignerObjDto.OffsetPosition,
                ChuckPrealignerObjDto.EfemLoadWaferStagePosition,
                OffsetPositionCalibrationResult = ChuckPrealignerObjDto.NewEfemLoadWaferStagePosition,
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
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.AlgorithmTemplateTypeEnum,
                Cache.LowMicroscopeMagnificationInfo.MicroscopeMagnificationName,
                Cache.LowFindPosition1,
                Cache.LowFindPosition2,
                HtmlTab = new HtmlTab(new
                {
                    LowTemplate = new HtmlImage(Cache.LowTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
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
            if (Cache.HighMicroscopeMagnificationInfo.MagnificationCode <= Cache.LowMicroscopeMagnificationInfo.MagnificationCode)
            {
                DialogWindowProvider.ShowDialog("The high magnification less than or equal low magnification! Please select correct magnification!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.HighMicroscopeMagnificationInfo.MicroscopeMagnificationName,
                Cache.HighFindPosition1,
                Cache.HighFindPosition2,
                HtmlTab = new HtmlTab(new
                {
                    HighTemplate = new HtmlImage(Cache.HighTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return result;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
        await InvokeCalibrateAsync(async () =>
        {
            var efemLoadWaferChuckAngle = StageViewModel.GetEfemLoadWaferMachineStageTheta();

            StageViewModel.SetAbsoluteStageTheta(efemLoadWaferChuckAngle);
            if (await P5CalibrateActionAsync(cancellationToken) == false)
            {
                DialogWindowProvider.ShowDialog("P5 Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: P5 Failed!"), HtmlLogUniqueId.LoggingHtml());
                result = false;
                return result;
            }

            ChuckPrealignerObjDto.OffsetAngle = Cache.OffsetAngle;
            ChuckPrealignerObjDto.EfemLoadWaferChuckAngle = efemLoadWaferChuckAngle;
            ChuckPrealignerObjDto.NewEfemLoadWaferChuckAngle = efemLoadWaferChuckAngle + Cache.OffsetAngle;
            Logger.LogHtmlInformation("Result Ok", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                ChuckPrealignerObjDto.OffsetPosition,
                Cache.OffsetAngle,
                ChuckPrealignerObjDto.EfemLoadWaferStagePosition,
                ChuckPrealignerObjDto.EfemLoadWaferChuckAngle,
                OffsetPositionCalibrationResult = ChuckPrealignerObjDto.NewEfemLoadWaferStagePosition,
                OffsetAngleCalibrationResult = ChuckPrealignerObjDto.NewEfemLoadWaferChuckAngle,
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

    private async Task<bool> VerifyCalibrationAsync(ChuckPrealignerObjDto selectChuckPrealignerObjDto, CancellationToken cancellationToken)
    {
        var result = true;
        await Task.Run(async () =>
        {
            if (selectChuckPrealignerObjDto is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Please select a review item!"), HtmlLogUniqueId.LoggingHtml());
                result = false;
                return false;
            }
            else
            {
                selectChuckPrealignerObjDto.IsVerified = false;

                var chuckPrealignerObjDto = selectChuckPrealignerObjDto.Clone();

                if (await P5CalibrateActionAsync(cancellationToken) == false)
                {
                    DialogWindowProvider.ShowDialog("P5 Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: P5 Failed!"), HtmlLogUniqueId.LoggingHtml());
                    result = false;
                    return false;
                }

                if (RecipeCacheProvider.Set(Cache, cancellationToken) == false)
                {
                    DialogWindowProvider.ShowDialog("Save Threshold Failed!", DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    result = false;
                    return result;
                }

                StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

                var (offsetPosition, _) = StageViewModel.FindWaferCenterByManually(Point.Origin);
                Cache.OffsetPosition = offsetPosition;

                Logger.LogHtmlInformation("Find wafer center result OK", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    VerifyOffsetAngle = Cache.OffsetAngle,
                    VerifyOffsetPosition = offsetPosition
                }), HtmlLogUniqueId.LoggingHtml());
                if (Math.Abs(offsetPosition.X) > Cache.PositionThreshold || Math.Abs(offsetPosition.Y) > Cache.PositionThreshold
                                                                         || Math.Abs(Cache.OffsetAngle) > Cache.AngleThreshold)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                    {
                        Cache.PositionThreshold,
                        Cache.AngleThreshold,
                        OldOffsetPosition = selectChuckPrealignerObjDto.OffsetPosition,
                        VerifyOffsetPosition = offsetPosition,
                        OldOffsetAngle = selectChuckPrealignerObjDto.OffsetAngle,
                        VerifyOffsetAngle = Cache.OffsetAngle,
                    }), HtmlLogUniqueId.LoggingHtml());

                    DialogWindowProvider.ShowDialog("Verify Chuck Prealigner calibration failed.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    result = false;
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
                    Cache.PositionThreshold,
                    Cache.AngleThreshold,
                    OldOffsetPosition = selectChuckPrealignerObjDto.OffsetPosition,
                    VerifyOffsetPosition = offsetPosition,
                    OldOffsetAngle = selectChuckPrealignerObjDto.OffsetAngle,
                    VerifyOffsetAngle = Cache.OffsetAngle,
                }), HtmlLogUniqueId.LoggingHtml());
            }

            return result;
        });
        return result;
    }


    private async Task<bool> P5CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await Task.Run(() =>
        {
            Logger.LogHtmlInformation("P5 Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.AngleErrorThreshold,
                LowMagnification = Cache.LowMicroscopeMagnificationInfo.MicroscopeMagnificationName,
                HighMagnification = Cache.HighMicroscopeMagnificationInfo.MicroscopeMagnificationName,
                Cache.AlgorithmWaferTypeEnum,
                LowLocation1 = Cache.LowSite1.Location,
                LowLocation2 = Cache.LowSite2.Location,
                HighLocation1 = Cache.HighSite1.Location,
                HighLocation2 = Cache.HighSite2.Location,
                HtmlTab = new HtmlTab(new
                {
                    LowTemplate = new HtmlImage(Cache.LowTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighTemplate = new HtmlImage(Cache.HighTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            var alignmentResultDto = StageViewModel.Alignment(
                Cache.LowSite1,
                Cache.LowSite2,
                Cache.HighSite1,
                Cache.HighSite2,
                Cache.LowMicroscopeMagnificationInfo,
                Cache.HighMicroscopeMagnificationInfo,
                Cache.AlgorithmWaferTypeEnum);
            Cache.OffsetAngle = alignmentResultDto.Degrees;
            result = Math.Abs(Cache.OffsetAngle) <= Cache.AngleErrorThreshold;
            Logger.LogHtmlInformation(result ? "P5 OK" : "P5 Failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new { Cache.OffsetAngle }), HtmlLogUniqueId.LoggingHtml());
        });
        return result;
    }

    private async Task<bool> ReloadWaferAsync(bool isReviewLoadWafer)
    {
        var result = false;
        await Task.Run(() =>
        {
            efemWindowViewModel.IsPrealigner = true;
            efemWindowViewModel.PrealignerIsOk = false;
            if (Math.Abs(Cache.OffsetAngle) > Cache.AngleThreshold || !isReviewLoadWafer) efemWindowViewModel.OffsetAngle = 0;
            else efemWindowViewModel.OffsetAngle = Cache.OffsetAngle;


            if (Math.Abs(Cache.OffsetPosition.X) > Cache.PositionThreshold || Math.Abs(Cache.OffsetPosition.Y) > Cache.PositionThreshold || !isReviewLoadWafer)
                efemWindowViewModel.OffsetPoint = Point.Origin;
            else
            {
                var (xDirection, yDirection) = StageViewModel.GetMachineDirection();
                var point = new Point(-xDirection * Cache.OffsetPosition.X, -yDirection * Cache.OffsetPosition.Y);
                efemWindowViewModel.OffsetPoint = point;
            }

            Logger.LogHtmlInformation("ReloadWafer", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.AngleThreshold,
                Cache.PositionThreshold,
                efemWindowViewModel.OffsetAngle,
                efemWindowViewModel.OffsetPoint
            }), HtmlLogUniqueId.LoggingHtml());
            WindowManagerService.ShowDialog(efemWindowViewModel);

            efemWindowViewModel.IsPrealigner = false;

            result = efemWindowViewModel.SelectedFoupItem != null
                && efemWindowViewModel is { PrealignerIsOk: true, SelectedFoupItem.IsLoadWafer: true };

            Logger.LogHtmlInformation(result ? "ReloadWafer OK" : "ReloadWafer Failed", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
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
            findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.WaferCenterThumb1 = Cache.WaferCenterThumb1;
            findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.WaferCenterThumb2 = Cache.WaferCenterThumb2;
            findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.WaferCenterThumb3 = Cache.WaferCenterThumb3;
            findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.WaferCenterThumb4 = Cache.WaferCenterThumb4;
            findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.WaferCenterThumb5 = Cache.WaferCenterThumb5;
            findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.WaferCenterThumb6 = Cache.WaferCenterThumb6;
            findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.WaferCenterThumb7 = Cache.WaferCenterThumb7;
            findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.WaferCenterThumb8 = Cache.WaferCenterThumb8;
            findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.OffsetPosition = Cache.OffsetPosition;

            findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.PositionErrorThreshold = Cache.PositionErrorThreshold;
            WindowManagerService.ShowDialog(findWaferCenterWindowFieldViewModel);
            result = findWaferCenterWindowFieldViewModel.IsFindWaferCenterOffsetPositionEnabled;
            var waferCenterThumbList = new List<byte[]>
            {
                findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.WaferCenterThumb1,
                findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.WaferCenterThumb2,
                findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.WaferCenterThumb3,
                findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.WaferCenterThumb4,
                findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.WaferCenterThumb5,
                findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.WaferCenterThumb6,
                findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.WaferCenterThumb7,
                findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.WaferCenterThumb8
            };
            SaveWaferCenterThumbImages(waferCenterThumbList);
            Cache.FindWaferCenterOffset1 = findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.FindWaferCenterOffset1;
            Cache.FindWaferCenterOffset2 = findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.FindWaferCenterOffset2;
            Cache.FindWaferCenterOffset3 = findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.FindWaferCenterOffset3;
            Cache.FindWaferCenterOffset4 = findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.FindWaferCenterOffset4;
            Cache.FindWaferCenterOffset5 = findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.FindWaferCenterOffset5;
            Cache.FindWaferCenterOffset6 = findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.FindWaferCenterOffset6;
            Cache.FindWaferCenterOffset7 = findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.FindWaferCenterOffset7;
            Cache.FindWaferCenterOffset8 = findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.FindWaferCenterOffset8;
            Cache.OffsetPosition = findWaferCenterWindowFieldViewModel.CacheFindWaferCenter.OffsetPosition;
            ChuckPrealignerObjDto.OffsetPosition = Cache.OffsetPosition;
            var (xDirection, yDirection) = StageViewModel.GetMachineDirection();
            var efemLoadWaferStagePosition = StageViewModel.GetEfemLoadWaferMachineStagePosition();
            ChuckPrealignerObjDto.EfemLoadWaferStagePosition = efemLoadWaferStagePosition;
            ChuckPrealignerObjDto.NewEfemLoadWaferStagePosition = new Point(efemLoadWaferStagePosition.X - xDirection * Cache.OffsetPosition.X, efemLoadWaferStagePosition.Y - yDirection * Cache.OffsetPosition.Y);
            Logger.LogHtmlInformation(result ? "find wafer center result OK" : "find wafer center result failed", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.OffsetPosition,
                ChuckPrealignerObjDto.EfemLoadWaferStagePosition,
                OffsetPositionCalibrationResult = ChuckPrealignerObjDto.NewEfemLoadWaferStagePosition,
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

        dto.LowMicroscopeMagnificationInfo = Cache.LowMicroscopeMagnificationInfo;
        dto.HighMicroscopeMagnificationInfo = Cache.HighMicroscopeMagnificationInfo;

        Calibration = dto.Clone();

        return CacheProvider.Set(dto, cancellationToken)
               && RecipeCacheProvider.Set(Cache, cancellationToken)
               && EnableDependedCalibrationItems(cancellationToken);
    });

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
                                    Cache.AngleThreshold
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
                        if (await Step3CalibrateActionAsync(cancellationToken) == false) return false;
                        CalibrationStepIndex = 3;
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

    public override async Task<bool> AutomationRecipeInformationAsync(string Position = "")
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (CalibrationRecipeService.GetCorrectWaferMapByOffset(true) == false)
            return false;
        Cache.LowMicroscopeMagnificationInfo = AlignmentCacheBrightField.LowMag;
        Cache.HighMicroscopeMagnificationInfo = AlignmentCacheBrightField.HighMag;
        Cache.LowTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.LowMicroscopeMagnificationInfo.MicroscopeMagnificationName}_{Guid.NewGuid()}";
        Cache.LowTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.LowTemplateFilePath);
        Cache.HighTemplateFilePath = $"{TemplateFileDirectory}\\1_{Cache.HighMicroscopeMagnificationInfo.MicroscopeMagnificationName}_{Guid.NewGuid()}";
        Cache.HighTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.HighTemplateFilePath);
        Cache.LowFindPosition1 = AlignmentCacheBrightField.LowSite1.Location;
        Cache.LowFindPosition2 = AlignmentCacheBrightField.LowSite2.Location;
        Cache.HighFindPosition1 = AlignmentCacheBrightField.HighSite1.Location;
        Cache.HighFindPosition2 = AlignmentCacheBrightField.HighSite2.Location;
        Cache.LowSite1 = AlignmentCacheBrightField.LowSite1;
        Cache.LowSite2 = AlignmentCacheBrightField.LowSite2;
        Cache.HighSite1 = AlignmentCacheBrightField.HighSite1;
        Cache.HighSite2 = AlignmentCacheBrightField.HighSite2;
        BitmapSourceHelper.Save(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(Cache.LowSite1.Template!.Thumb), Cache.LowTemplateImageFilePath);
        BitmapSourceHelper.Save(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(Cache.HighSite1.Template!.Thumb), Cache.HighTemplateImageFilePath);
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