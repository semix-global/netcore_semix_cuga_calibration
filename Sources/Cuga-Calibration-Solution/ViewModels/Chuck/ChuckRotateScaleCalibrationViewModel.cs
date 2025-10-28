using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Chuck.RotateScaleError;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.NoSQL.DB.Providers.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Chuck;

[Net.Utilities.Attributes.IOCAppService(ServiceType = typeof(ChuckRotateScaleCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckRotateScaleCalibrationViewModel(
    IHostEnvironment hostEnvironment,
    AlignmentWindowBrightFieldViewModel alignmentWindowBrightFieldViewModel) : CalibrationViewModelBase
{
    #region 属性

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "P5" },
        new() { StepName = "Find Low Base Position" },
        new() { StepName = "Top LowSite" },
        new() { StepName = "Top HighSite" },
        new() { StepName = "Bottom LowSite" },
        new() { StepName = "Bottom HighSite" },
        new() { StepName = "Left LowSite" },
        new() { StepName = "Left HighSite" },
        new() { StepName = "Right LowSite" },
        new() { StepName = "Right HighSite" },
        new() { StepName = "Calibration" }
    ];

    #region 界面相关

    #region Calibration

    [ObservableProperty]
    private ChuckRotateScaleErrorDto? _resultRotateScaleErrorDto = new();

    [ObservableProperty]
    private ObservableCollection<ChuckRotateScaleErrorDto> _rotateErrorDtoItemDtoList = [];

    #endregion Calibration

    #region Review

    [ObservableProperty]
    private ChuckRotateScaleErrorDto? _selectRotateScaleErrorDto = new();

    [ObservableProperty]
    private ChuckRotateScaleErrorDto _reviewDto = new();

    private bool _isFastMode = true;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private ChuckRotateScaleErrorCache _cache = new();

    [ObservableProperty]
    private ChuckGlobalScaleErrorCache _ideaPositionCache = new();

    [ObservableProperty]
    private ChuckRotateScaleErrorDto _calibration = new();

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

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckPrealignerObjDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        (var isHasIdealCache, IdeaPositionCache) = RecipeCacheProvider.TryGetOrDefault<ChuckGlobalScaleErrorCache>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<ChuckRotateScaleErrorCache>();
        Calibration = CacheProvider.GetOrDefault<ChuckRotateScaleErrorDto>();
        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();

        if (IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation == MicroscopeLensInformation.Default) IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();
        if (IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation == MicroscopeLensInformation.Default) IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation = CalibrationSetting.SettingCommonParam.HighMicroscopeLensInformation.Clone();

        if (isHasIdealCache == false) RecipeCacheProvider.Set(IdeaPositionCache, cancellationToken);
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

        if (IsRecipeCalibrate)
        {
            if (CalibrationRecipeService.GetCorrectWaferMapByOffset(true) == false) return false;
            if (await AutomationRecipeInformationAsync(string.Empty) == false) return false;
        }

        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);
        return true;
    }

    protected override async Task<bool> CancelingAsync()
    {
        if (CacheProvider.GetOrDefault<ChuckRotateScaleErrorDto>().IsOk == false)
            StageViewModel.ResetTScale();

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
                StageViewModel.SetBrightFieldAbsoluteStageXy(IdeaPositionCache.BaseLowSiteFindPosition);
                break;

            case 3:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(IdeaPositionCache.GetPosition(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation));
                break;

            case 4:
                IdeaPositionCache.SiteDirection = StageDirectionTypeEnum.Up;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(IdeaPositionCache.GetPosition(IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation));
                break;

            case 5:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(IdeaPositionCache.GetPosition(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation));
                break;

            case 6:
                IdeaPositionCache.SiteDirection = StageDirectionTypeEnum.Down;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(IdeaPositionCache.GetPosition(IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation));
                break;

            case 7:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(IdeaPositionCache.GetPosition(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation));
                break;

            case 8:
                IdeaPositionCache.SiteDirection = StageDirectionTypeEnum.Left;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(IdeaPositionCache.GetPosition(IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation));
                break;

            case 9:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(IdeaPositionCache.GetPosition(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation));
                break;

            case 10:
                IdeaPositionCache.SiteDirection = StageDirectionTypeEnum.Right;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(IdeaPositionCache.GetPosition(IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation));
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
                MicroscopeViewModel.SwitchMicroscopeLensInformation(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(IdeaPositionCache.BaseLowSiteFindPosition);
                return true;

            case 2 or 4 or 6 or 8:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation);
                return true;

            case 1:
                IdeaPositionCache.SiteDirection = StageDirectionTypeEnum.Up;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(IdeaPositionCache.GetPosition(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation));
                return true;

            case 3:
                IdeaPositionCache.SiteDirection = StageDirectionTypeEnum.Down;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(IdeaPositionCache.GetPosition(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation));
                return true;

            case 5:
                IdeaPositionCache.SiteDirection = StageDirectionTypeEnum.Left;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(IdeaPositionCache.GetPosition(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation));
                return true;

            case 7:
                IdeaPositionCache.SiteDirection = StageDirectionTypeEnum.Right;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(IdeaPositionCache.GetPosition(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation));
                return true;

            case 10:
                if (ResultRotateScaleErrorDto is null)
                {
                    DialogWindowProvider.TryShowDialog("Calibration result is Empty!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                    if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
                }
                else
                {
                    ResultRotateScaleErrorDto.IsCalibrated = true;
                    if (Save(ResultRotateScaleErrorDto, cancellationToken) == false)
                    {
                        ResultRotateScaleErrorDto.IsCalibrated = false;
                        StageViewModel.ResetTScale();
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

            IdeaPositionCache.P5Angle = alignmentResultDto.Degrees;
            Cache.P5ResultAngle = StageViewModel.GetMachineStageTheta();
            Logger.LogHtmlInformation("P5 OK", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new { IdeaPositionCache.P5Angle }), HtmlLogUniqueId.LoggingHtml());

            result = true;
            return result;
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        if (IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation.LensCode <= IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation.LensCode)
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
            var baseBrightFieldPosition = IdeaPositionCache.BaseLowSiteFindPosition = position;

            if (baseBrightFieldPosition.ToOriginLength >= IdeaPositionCache.WaferDiameter / 2)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Base Position is Out of Wafer!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.AlgorithmTemplateTypeEnum,
                IdeaPositionCache.WaferMaskTypeEnum,
                LowMagnification = IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation.LensName,
                HighMagnification = IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation.LensName,
                IdeaPositionCache.ColumnCellWidth,
                IdeaPositionCache.RowCellHeight,
                IdeaPositionCache.WaferDiameter
            }), HtmlLogUniqueId.LoggingHtml());

            if (IsRecipeCalibrate == false)
            {
                (var isSuccess, IdeaPositionCache.LowGlobalScaleErrorCacheItem.LeftPosition, IdeaPositionCache.LowGlobalScaleErrorCacheItem.RightPosition) = GetIdeaBrightFieldPosition(true, baseBrightFieldPosition.Y);
                if (isSuccess == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Get X Axis Idea Position Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                (isSuccess, IdeaPositionCache.LowGlobalScaleErrorCacheItem.BottomPosition, IdeaPositionCache.LowGlobalScaleErrorCacheItem.TopPosition) = GetIdeaBrightFieldPosition(false, baseBrightFieldPosition.X);
                if (isSuccess == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Get Y Axis Idea Position Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }
            }

            Logger.LogHtmlInformation("Idea Bright Field Positions", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                IdeaPositionCache.BaseLowSiteFindPosition,
                IdeaPositionCache.LowGlobalScaleErrorCacheItem.TopPosition,
                IdeaPositionCache.LowGlobalScaleErrorCacheItem.BottomPosition,
                IdeaPositionCache.LowGlobalScaleErrorCacheItem.LeftPosition,
                IdeaPositionCache.LowGlobalScaleErrorCacheItem.RightPosition
            }), HtmlLogUniqueId.LoggingHtml());

            result = true;
            return result;

            (bool isSuccess, Point negativePosition, Point positivePosition) GetIdeaBrightFieldPosition(bool isAxisX, double otherAxisValue)
            {
                try
                {
                    // 靠近边缘位置的理想位置可能拍不全，总长度截去一个die
                    var actualWaferRadius = IdeaPositionCache.GetActualWaferDiameter(isAxisX) / 2;
                    var interval = isAxisX ? IdeaPositionCache.ColumnCellWidth : IdeaPositionCache.RowCellHeight;
                    var basePositionValue = isAxisX ? baseBrightFieldPosition.X : baseBrightFieldPosition.Y;

                    var negativePositionError = (int)((actualWaferRadius + basePositionValue) / interval) * interval;
                    var positivePositionError = (int)((actualWaferRadius - basePositionValue) / interval) * interval;

                    var negativeCurrentAxisValue = basePositionValue - negativePositionError;
                    var positiveCurrentAxisValue = basePositionValue + positivePositionError;

                    var negativeResultPosition = isAxisX ? new Point(negativeCurrentAxisValue, otherAxisValue) : new Point(otherAxisValue, negativeCurrentAxisValue);
                    var positiveResultPosition = isAxisX ? new Point(positiveCurrentAxisValue, otherAxisValue) : new Point(otherAxisValue, positiveCurrentAxisValue);

                    return (true, negativeResultPosition, positiveResultPosition);
                }
                catch
                {
                    return (false, default, default);
                }
            }
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            if (IsRecipeCalibrate == false)
            {
                var result = StageViewModel.GetBrightFieldStagePosition();
                IdeaPositionCache.SetPosition(result, IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation);

                var templateFilePath = $"{TemplateFileDirectory}\\{IdeaPositionCache.SiteDirection}Site_{IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation.LensName}_{Guid.NewGuid()}";
                var generateTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, templateFilePath, Cache.AlgorithmTemplateSizeEnum);
                if (generateTemplate == false)
                {
                    DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                var templateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath);
                IdeaPositionCache.SetTemplate(templateFilePath, templateImageFilePath, IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation);
            }

            var position = IdeaPositionCache.GetPosition(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation);
            var (templateFilePathLog, templateImageFilePathLog) = IdeaPositionCache.GetTemplate(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation);

            StageViewModel.SetBrightFieldAbsoluteStageXy(position);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                LensName = IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation.LensName,
                IdeaPositionCache.SiteDirection,
                LowSite = position,
                templateFilePathLog,
                HtmlTab = new HtmlTab(new
                {
                    LowTemplateImage = new HtmlImage(templateImageFilePathLog, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
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
            if (IsRecipeCalibrate == false)
            {
                var result = StageViewModel.GetBrightFieldStagePosition();
                if (IdeaPositionCache.SiteDirection is StageDirectionTypeEnum.Up) IdeaPositionCache.LowToHighMagnificationOffset = result - (Vector)IdeaPositionCache.LowGlobalScaleErrorCacheItem.TopPosition;

                IdeaPositionCache.SetPosition(result, IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation);

                var templateFilePath = $"{TemplateFileDirectory}\\{IdeaPositionCache.SiteDirection}Site_{IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation.LensName}_{Guid.NewGuid()}";
                var generateTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, templateFilePath, Cache.AlgorithmTemplateSizeEnum);
                if (generateTemplate == false)
                {
                    DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                var templateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath);
                IdeaPositionCache.SetTemplate(templateFilePath, templateImageFilePath, IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation);
            }

            var position = IdeaPositionCache.GetPosition(IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation);
            var (templateFilePathLog, templateImageFilePathLog) = IdeaPositionCache.GetTemplate(IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation);

            StageViewModel.SetBrightFieldAbsoluteStageXy(position);
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                LensName = IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation.LensName,
                IdeaPositionCache.SiteDirection,
                HighSite = position,
                templateFilePathLog,
                HtmlTab = new HtmlTab(new
                {
                    HighTemplateImage = new HtmlImage(templateImageFilePathLog, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step4CalibrateActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = false;
            await InvokeCalibrateAsync(() =>
            {
                var (isSuccess, errorMessage) = Cache.Verify();
                if (isSuccess == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Cache varify error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                    DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                ClearCalibrationTemp();
                _isFastMode = true;
                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.RotateAngle,
                    Cache.Threshold,
                    LowMagnification = IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation.LensName,
                    HighMagnification = IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation.LensName,
                    IdeaPositionCache.AlgorithmTemplateTypeEnum,
                    IdeaPositionCache.BaseLowSiteFindPosition,
                    IdeaPositionCache.LowToHighMagnificationOffset
                }), HtmlLogUniqueId.LoggingHtml());

                var idealPositionDictionary = new Dictionary<StageDirectionTypeEnum, Point>
                {
                    { StageDirectionTypeEnum.Up, IdeaPositionCache.HighGlobalScaleErrorCacheItem.TopPosition },
                    { StageDirectionTypeEnum.Left, IdeaPositionCache.HighGlobalScaleErrorCacheItem.LeftPosition },
                    { StageDirectionTypeEnum.Down, IdeaPositionCache.HighGlobalScaleErrorCacheItem.BottomPosition },
                    { StageDirectionTypeEnum.Right, IdeaPositionCache.HighGlobalScaleErrorCacheItem.RightPosition }
                };

                var calibrationResult = false;
                List<double> arcLengthErrorList = [];
                List<double> angleErrorList = [];
                foreach (var times in Enumerable.Range(1, 5))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Logger.LogHtmlInformation($"Calibration Times: {times}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                    var calibrationItemDto = new ChuckRotateScaleErrorDto();
                    SynchronizationContextProvider.Send(() => RotateErrorDtoItemDtoList.Add(calibrationItemDto));
                    // 校准
                    if (GetResult(idealPositionDictionary, ref calibrationItemDto, cancellationToken) == false)
                    {
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Calibration Result Failed!"), HtmlLogUniqueId.LoggingHtml());
                        return false;
                    }

                    arcLengthErrorList.Add(calibrationItemDto.ScaleErrorValueAverage);
                    angleErrorList.Add(calibrationItemDto.RealAngleErrorsAverage);

                    Logger.LogHtmlInformation($"Applied Times: {times}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                    // 应用
                    if (GetResult(idealPositionDictionary, ref calibrationItemDto, cancellationToken) == false)
                    {
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Get Applied Result Failed!"), HtmlLogUniqueId.LoggingHtml());
                        return false;
                    }
                }

                ResultRotateScaleErrorDto = RotateErrorDtoItemDtoList.Last().Clone();
                var resultList = RotateErrorDtoItemDtoList.Select(t => t)
                    .Where(t => t != RotateErrorDtoItemDtoList.Minima(t => t.ScaleErrorValueAverage).First()
                                && t != RotateErrorDtoItemDtoList.Maxima(t => t.ScaleErrorValueAverage).First())
                    .ToList();

                ResultRotateScaleErrorDto.AppliedScaleT = resultList.Average(t => t.AppliedScaleT);
                ResultRotateScaleErrorDto.ResultScaleT = resultList.Average(t => t.ResultScaleT);
                ResultRotateScaleErrorDto.RealAngleErrorsAverage = resultList.Average(t => t.RealAngleErrorsAverage);
                ResultRotateScaleErrorDto.ScaleErrorValueAverage = resultList.Average(t => t.ScaleErrorValueAverage);
                calibrationResult = ResultRotateScaleErrorDto.ScaleErrorValueAverage < Cache.Threshold;

                Logger.LogHtmlInformation($"Calibration {(calibrationResult ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ResultRotateScaleErrorDto.AppliedScaleT,
                    ResultRotateScaleErrorDto.ResultScaleT,
                    ResultRotateScaleErrorDto.RealAngleErrorsAverage,
                    ResultRotateScaleErrorDto.ScaleErrorValueAverage,
                    ScaleResultCurve = new HtmlPlot2DLinesChart([
                        ("Times-CalibrationScaleResult", RotateErrorDtoItemDtoList.Select(t => t.AppliedScaleT).ToList().ToPoints()),
                        ("Times-AppliedScaleResult", RotateErrorDtoItemDtoList.Select(t => t.ResultScaleT).ToList().ToPoints())
                    ], "ScaleResultCurve"),
                    ArcLengthErrorCurve = new HtmlPlot2DLinesChart([
                        ("Times-CalibrationArcLengthError", arcLengthErrorList.ToPoints()),
                        ("Times-AppliedArcLengthError", RotateErrorDtoItemDtoList.Select(t => t.ScaleErrorValueAverage).ToList().ToPoints())
                    ], "ArcLengthErrorCurve"),
                    AngleErrorValueCurve = new HtmlPlot2DLinesChart([
                        ("Times-CalibrationAngleErrorValue", angleErrorList.Select(t => Cache.RotateAngle * 2 - t).ToList().ToPoints()),
                        ("Times-AppliedAngleErrorValue", RotateErrorDtoItemDtoList.Select(t => Cache.RotateAngle * 2 - t.RealAngleErrorsAverage).ToList().ToPoints())
                    ], "AngleErrorValueCurve")
                }), HtmlLogUniqueId.LoggingHtml());

                StageViewModel.SetAbsoluteStageTheta(Cache.P5ResultAngle);
                result = calibrationResult;
                return calibrationResult;
            });
            return result;
        }
        catch
        {
            return false;
        }
        finally
        {
            StageViewModel.SetAbsoluteStageTheta(0);
        }
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
        try
        {
            var result = false;
            await Task.Run(() =>
            {
                var (isSuccess, errorMessage) = Cache.Verify();
                if (isSuccess == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Cache varify error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                    DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                ReviewDto.IsVerified = false;
                SelectRotateScaleErrorDto = ReviewDto.Clone();
                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.RotateAngle,
                    Cache.Threshold,
                    LowMagnification = IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation.LensName,
                    HighMagnification = IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation.LensName,
                    IdeaPositionCache.AlgorithmTemplateTypeEnum,
                    IdeaPositionCache.BaseLowSiteFindPosition,
                    IdeaPositionCache.LowToHighMagnificationOffset,
                    ReviewDto.AppliedScaleT,
                    ReviewDto.ResultScaleT,
                    ReviewDto.RealAngleErrorsAverage,
                    ReviewDto.ScaleErrorValueAverage
                }), HtmlLogUniqueId.LoggingHtml());

                var idealPositionDictionary = new Dictionary<StageDirectionTypeEnum, Point>
                {
                    { StageDirectionTypeEnum.Up, IdeaPositionCache.HighGlobalScaleErrorCacheItem.TopPosition },
                    { StageDirectionTypeEnum.Left, IdeaPositionCache.HighGlobalScaleErrorCacheItem.LeftPosition },
                    { StageDirectionTypeEnum.Down, IdeaPositionCache.HighGlobalScaleErrorCacheItem.BottomPosition },
                    { StageDirectionTypeEnum.Right, IdeaPositionCache.HighGlobalScaleErrorCacheItem.RightPosition }
                };

                StageViewModel.SetTScale(ReviewDto.AppliedScaleT);
                var verifyItemDto = SelectRotateScaleErrorDto;

                _isFastMode = !IsAutoCalibrate;
                isSuccess = GetResult(idealPositionDictionary, ref verifyItemDto, cancellationToken);
                if (isSuccess == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Get Result Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                ReviewDto.IsVerified = Math.Abs(verifyItemDto.ScaleErrorValueAverage) < Cache.Threshold;
                Logger.LogHtmlInformation($"Verify {(isSuccess ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ReviewDto.AppliedScaleT,
                    VerifyScaleT = SelectRotateScaleErrorDto.ResultScaleT,
                    VefiryAngleError = SelectRotateScaleErrorDto.RealAngleErrorsAverage,
                    VerifyScaleErrorValue = SelectRotateScaleErrorDto.ScaleErrorValueAverage
                }), HtmlLogUniqueId.LoggingHtml());

                if (Save(ReviewDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    ReviewDto.IsVerified = false;
                    return false;
                }

                StageViewModel.SetAbsoluteStageTheta(Cache.P5ResultAngle);

                result = ReviewDto.IsVerified;

                if (IsAutoCalibrate == false)
                    DialogWindowProvider.ShowDialog($"Verify {(result ? "Success" : "Failed")}!", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);
                return result;
            });
            return result;
        }
        catch
        {
            return false;
        }
        finally
        {
            StageViewModel.SetAbsoluteStageTheta(0);
        }
    }

    private bool Save(ChuckRotateScaleErrorDto dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(IdeaPositionCache);
        update(Cache);

        dto.LowMicroscopeLensInformation = IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation;
        dto.HighMicroscopeLensInformation = IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation;

        Calibration = dto.Clone();

        CacheProvider.Set(dto, cancellationToken);
        RecipeCacheProvider.Set(IdeaPositionCache, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(RotateErrorDtoItemDtoList.Clear);
        ResultRotateScaleErrorDto = null;
    }

    #endregion 校准

    #region 算法

    private bool GetResult(Dictionary<StageDirectionTypeEnum, Point> idealPositionDictionary, ref ChuckRotateScaleErrorDto tempRotateScaleErrorDto, CancellationToken cancellationToken)
    {
        try
        {
            StageViewModel.SetTScale(tempRotateScaleErrorDto.AppliedScaleT);
            // 正向
            Logger.LogHtmlInformation("Positive Rotate", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
            var degreeAngle = -Cache.RotateAngle + Cache.P5ResultAngle;
            StageViewModel.SetAbsoluteStageTheta(Cache.RotateAngle);
            foreach (var ideaPosition in idealPositionDictionary)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (MatchTemplate(ideaPosition.Key, ideaPosition.Value, degreeAngle, ref tempRotateScaleErrorDto) == false)
                    return false;

                SelectRotateScaleErrorDto = tempRotateScaleErrorDto.Clone();
            }

            // 反向
            Logger.LogHtmlInformation("Negative Rotate", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
            degreeAngle = Cache.RotateAngle + Cache.P5ResultAngle;
            StageViewModel.SetAbsoluteStageTheta(-Cache.RotateAngle);
            foreach (var ideaPosition in idealPositionDictionary)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (MatchTemplate(ideaPosition.Key, ideaPosition.Value, degreeAngle, ref tempRotateScaleErrorDto) == false)
                    return false;

                SelectRotateScaleErrorDto = tempRotateScaleErrorDto.Clone();
            }

            // chuck center
            tempRotateScaleErrorDto.ChuckCenterBrightFieldPosition = CalibrationAlgorithmService.GetChuckCenter(
                tempRotateScaleErrorDto.PositiveTopHighSiteRealPosition,
                tempRotateScaleErrorDto.NegativeTopHighSiteRealPosition,
                tempRotateScaleErrorDto.NegativeRightHighSiteRealPosition,
                tempRotateScaleErrorDto.PositiveRightHighSiteRealPosition,
                tempRotateScaleErrorDto.PositiveBottomHighSiteRealPosition,
                tempRotateScaleErrorDto.NegativeBottomHighSiteRealPosition,
                tempRotateScaleErrorDto.NegativeLeftHighSiteRealPosition,
                tempRotateScaleErrorDto.PositiveLeftHighSiteRealPosition
            );
            var scaleResult = GetAverageScaleResult(ref tempRotateScaleErrorDto);
            if (hostEnvironment.IsProduction() && (scaleResult >= 2 || scaleResult == 0)) // 防止下发异常值
            {
                DialogWindowProvider.ShowDialog("Result scale is illegal !", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            tempRotateScaleErrorDto.ResultScaleT = scaleResult;
            GetScaleErrorValue(ref tempRotateScaleErrorDto);

            Logger.LogHtmlInformation($"Get Result OK", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                tempRotateScaleErrorDto.AppliedScaleT,
                tempRotateScaleErrorDto.ResultScaleT,
                tempRotateScaleErrorDto.ChuckCenterBrightFieldPosition,
                tempRotateScaleErrorDto.RealAngleErrorsAverage,
                tempRotateScaleErrorDto.ScaleErrorValueAverage,
                tempRotateScaleErrorDto.PositiveTopHighSiteRealPosition,
                tempRotateScaleErrorDto.NegativeTopHighSiteRealPosition,
                tempRotateScaleErrorDto.PositiveBottomHighSiteRealPosition,
                tempRotateScaleErrorDto.NegativeBottomHighSiteRealPosition,
                tempRotateScaleErrorDto.PositiveLeftHighSiteRealPosition,
                tempRotateScaleErrorDto.NegativeLeftHighSiteRealPosition,
                tempRotateScaleErrorDto.PositiveRightHighSiteRealPosition,
                tempRotateScaleErrorDto.NegativeRightHighSiteRealPosition
            }), HtmlLogUniqueId.LoggingHtml());

            if (tempRotateScaleErrorDto.AppliedScaleT == 1.0)
                tempRotateScaleErrorDto.AppliedScaleT = scaleResult;
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Result Failed", Name);
            return false;
        }

        bool MatchTemplate(StageDirectionTypeEnum stageDirection, Point ideaPosition, double degreeAngle, ref ChuckRotateScaleErrorDto resultDto)
        {
            resultDto.IsPositive = degreeAngle < 0;
            var affineIdeaPosition = ideaPosition.DegreeAngleByOrigin(degreeAngle);
            var highIdeaPosition = resultDto.GetRealPosition(stageDirection);
            IdeaPositionCache.SiteDirection = stageDirection;
            if (highIdeaPosition == Point.Origin || _isFastMode == false)
            {
                var (lowTemplateFilePath, _) = IdeaPositionCache.GetTemplate(IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation);
                if (ReviewViewModel.TryGetMatchPosition(IdeaPositionCache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, affineIdeaPosition - (Vector)IdeaPositionCache.LowToHighMagnificationOffset, IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation, lowTemplateFilePath, ImageFileDirectory,
                        null, Name,
                        $"Low Magnification {stageDirection} Site", out var lowResultPosition, out _, out _, out _, out _) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Low Magnification {stageDirection} Site Matching Failed!"), HtmlLogUniqueId.LoggingHtml());
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(lowResultPosition), "Low Magnification Matching Failed!");
                }

                highIdeaPosition = lowResultPosition + (Vector)IdeaPositionCache.LowToHighMagnificationOffset;
            }

            var (highTemplateFilePath, _) = IdeaPositionCache.GetTemplate(IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation);
            if (ReviewViewModel.TryGetMatchPosition(IdeaPositionCache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, highIdeaPosition, IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation, highTemplateFilePath, ImageFileDirectory,
                    null, Name,
                    $"High Magnification {stageDirection} Site", out var highResultPosition, out _, out _, out var highResultImageFilePath, out _) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: High Magnification {stageDirection} Site Matching Failed!"), HtmlLogUniqueId.LoggingHtml());
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(highResultPosition), "High Magnification Matching Failed!");
            }

            resultDto.SetMatchResultInfo(stageDirection, highResultPosition, highResultImageFilePath);

            Logger.LogHtmlInformation($"{stageDirection} site Result", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                affineIdeaPosition,
                FindResultPosition = highResultPosition,
                HtmlTab = new HtmlTab(new
                {
                    HighMatchImage = new HtmlImage(highResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        }
    }

    private double GetAverageScaleResult(ref ChuckRotateScaleErrorDto tempRotateScaleErrorDto)
    {
        var (positiveFindPosition, negativeFindPosition) = tempRotateScaleErrorDto.GetCoupleRealPosition(StageDirectionTypeEnum.Up);
        var realAngleUp = GetRealRotateAngle(positiveFindPosition, negativeFindPosition, tempRotateScaleErrorDto.ChuckCenterBrightFieldPosition);
        var scaleUp = Math.Abs((Cache.RotateAngle * 2) / realAngleUp);

        (positiveFindPosition, negativeFindPosition) = tempRotateScaleErrorDto.GetCoupleRealPosition(StageDirectionTypeEnum.Down);
        var realAngleBottom = GetRealRotateAngle(positiveFindPosition, negativeFindPosition, tempRotateScaleErrorDto.ChuckCenterBrightFieldPosition);
        var scaleBottom = Math.Abs((Cache.RotateAngle * 2) / realAngleBottom);

        (positiveFindPosition, negativeFindPosition) = tempRotateScaleErrorDto.GetCoupleRealPosition(StageDirectionTypeEnum.Left);
        var realAngleLeft = GetRealRotateAngle(positiveFindPosition, negativeFindPosition, tempRotateScaleErrorDto.ChuckCenterBrightFieldPosition);
        var scaleLeft = Math.Abs((Cache.RotateAngle * 2) / realAngleLeft);

        (positiveFindPosition, negativeFindPosition) = tempRotateScaleErrorDto.GetCoupleRealPosition(StageDirectionTypeEnum.Right);
        var realAngleRight = GetRealRotateAngle(positiveFindPosition, negativeFindPosition, tempRotateScaleErrorDto.ChuckCenterBrightFieldPosition);
        var scaleRight = Math.Abs((Cache.RotateAngle * 2) / realAngleRight);

        var realAngleAverage = (realAngleUp + realAngleBottom + realAngleLeft + realAngleRight) / 4;
        var scaleAverage = (Cache.RotateAngle * 2) / realAngleAverage;

        tempRotateScaleErrorDto.RealAngleErrorsAverage = realAngleAverage;

        Logger.LogHtmlInformation("Rotate Real Result", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
        {
            realAngleUp,
            realAngleBottom,
            realAngleLeft,
            realAngleRight,
            realAngleAverage,
            scaleUp,
            scaleBottom,
            scaleLeft,
            scaleRight,
            scaleAverage
        }), HtmlLogUniqueId.LoggingHtml());
        return scaleAverage;

        static double GetRealRotateAngle(Point positivePosition, Point negativePosition, Point rotateCenterPosition)
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

    private void GetScaleErrorValue(ref ChuckRotateScaleErrorDto tempRotateScaleErrorDto)
    {
        var diameter = IdeaPositionCache.WaferDiameter;
        var angle = Cache.RotateAngle * 2 * (1 - tempRotateScaleErrorDto.ResultScaleT);
        var radian = (Math.PI / 180) * angle;
        tempRotateScaleErrorDto.ScaleErrorValueAverage = diameter * radian;
    }

    #endregion 算法

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
        try
        {
            foreach (var stepItem in AutoCalibrationStepList.Select((t, index) => (t, index)))
            {
                switch (stepItem.index)
                {
                    case 0:
                        if (await LoadedingAsync(cancellationToken) == false) return false;
                        if (await NextingAsync(cancellationToken) == false) return false;
                        await InvokeCalibrateAsync(() =>
                        {
                            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                            {
                                LowMagnification = IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation.LensName,
                                HighMagnification = IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation.LensName
                            }), HtmlLogUniqueId.LoggingHtml());
                            return true;
                        });
                        if (await AutoNextingAsync(cancellationToken) == false) return false;
                        break;

                    case 1:
                        if (await AutomationRecipeInformationAsync("2") == false) return false;
                        if (await Step4CalibrateActionAsync(cancellationToken) == false) return false;
                        CalibrationStepIndex = CalibrationStepList.Count - 1;
                        if (await NextingAsync(cancellationToken) == false) return false;
                        if (await AutoNextingAsync(cancellationToken) == false) return false;
                        break;

                    case 2:
                        AutoReviewCalibrationStepIndex = AutoCalibrationStepList.Count - 1;
                        if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false) return false;
                        await InvokeCalibrateAsync(async () =>
                        {
                            if (await VerifyCalibrationAsync(cancellationToken) == false) return false;
                            result = true;
                            return result;
                        });
                        if (await AutoNextingAsync(cancellationToken) == false) return false;
                        break;
                }

                AutoCalibrationProgress = AutoCalibrationStepIndex / (double)AutoCalibrationStepList.Count * 100;
            }
        }
        catch (Exception ex)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"{Name} Error: Calibration Failed! Error massage:{ex.Message}"), HtmlLogUniqueId.LoggingHtml());
        }

        return result;
    }

    public override async Task<bool> AutomationRecipeInformationAsync(string stepName)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (IsRecipeCalibrate == false)
            return true;

        if (CalibrationRecipeDto is null)
        {
            DialogWindowProvider.ShowDialog("Revise wafer map is empty!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        var originReticle = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleModel.Single(t => t.Index is { X: 0, Y: 0 });
        var reticleRows = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleModel
            .Where(t => t.Index.X == 0)
            .OrderBy(t => t.Index.Y).ToList();
        var reticleCols = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleModel
            .Where(t => t.Index.Y == 0)
            .OrderBy(t => t.Index.X).ToList();
        var reticleTop = reticleRows.ElementAt(reticleRows.Count - 2);
        var reticleRight = reticleCols.ElementAt(reticleCols.Count - 2);
        var reticleBottom = reticleRows.ElementAt(1);
        var reticleLeft = reticleCols.ElementAt(1);

        if (CalibrationRecipeService.GetChuckReticleMaskInfo(IdeaPositionCache.WaferMaskTypeEnum, IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation, null, out var baseLowMaskInfo) == false)
            return false;
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticle, baseLowMaskInfo, out var lowPosition);
        IdeaPositionCache.BaseLowSiteFindPosition = lowPosition;

        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleTop, baseLowMaskInfo, out var topLowSitePosition);
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleBottom, baseLowMaskInfo, out var bottomLowSitePosition);
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleLeft, baseLowMaskInfo, out var leftLowSitePosition);
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleRight, baseLowMaskInfo, out var rightLowSitePosition);
        IdeaPositionCache.SetPosition(topLowSitePosition, IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Up);
        IdeaPositionCache.SetPosition(bottomLowSitePosition, IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Down);
        IdeaPositionCache.SetPosition(leftLowSitePosition, IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Left);
        IdeaPositionCache.SetPosition(rightLowSitePosition, IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Right);
        IdeaPositionCache.SetTemplate(baseLowMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath, baseLowMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath, IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Up);
        IdeaPositionCache.SetTemplate(baseLowMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath, baseLowMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath, IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Down);
        IdeaPositionCache.SetTemplate(baseLowMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath, baseLowMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath, IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Left);
        IdeaPositionCache.SetTemplate(baseLowMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath, baseLowMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath, IdeaPositionCache.LowGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Right);

        if (CalibrationRecipeService.GetChuckReticleMaskInfo(IdeaPositionCache.WaferMaskTypeEnum, IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation, null, out var baseHighMaskInfo) == false)
            return false;
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleTop, baseHighMaskInfo, out var topHighSitePosition);
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleBottom, baseHighMaskInfo, out var bottomHighSitePosition);
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleLeft, baseHighMaskInfo, out var leftHighSitePosition);
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleRight, baseHighMaskInfo, out var rightHighSitePosition);
        IdeaPositionCache.SetPosition(topHighSitePosition, IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Up);
        IdeaPositionCache.SetPosition(bottomHighSitePosition, IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Down);
        IdeaPositionCache.SetPosition(leftHighSitePosition, IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Left);
        IdeaPositionCache.SetPosition(rightHighSitePosition, IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Right);
        IdeaPositionCache.SetTemplate(baseHighMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath, baseHighMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath, IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Up);
        IdeaPositionCache.SetTemplate(baseHighMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath, baseHighMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath, IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Down);
        IdeaPositionCache.SetTemplate(baseHighMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath, baseHighMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath, IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Left);
        IdeaPositionCache.SetTemplate(baseHighMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath, baseHighMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath, IdeaPositionCache.HighGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Right);

        var waferMapDataInfo = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.DieBuilder.DiePitchSize;
        IdeaPositionCache.RowCellHeight = waferMapDataInfo.Height;
        IdeaPositionCache.ColumnCellWidth = waferMapDataInfo.Width;
        IdeaPositionCache.WaferDiameter = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.Wafer.Circle.Diameter;

        return true;
    }

    private async Task<bool> AutoNextingAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            CalibrationStepName = AutoCalibrationStepList[AutoCalibrationStepIndex].StepName;
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

                if (await VerifyCalibrationAsync(cancellationToken) == false)
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