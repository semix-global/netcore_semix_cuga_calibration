using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.AutoFocus;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
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
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Chuck;

[IOCAppService(ServiceType = typeof(ChuckGlobalScaleErrorCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckGlobalScaleErrorCalibrationViewModel(
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
        new() { StepName = "Find Real Position" }
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

    [ObservableProperty]
    private ChuckGlobalScaleErrorCache _cache = new();

    [ObservableProperty]
    private ChuckGlobalScaleErrorDto _calibration = new();

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    [ObservableProperty]
    private ChuckAutoFocusDto _chuckAutoFocus = new();

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

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<ChuckGlobalScaleErrorCache>();
        Calibration = CacheProvider.GetOrDefault<ChuckGlobalScaleErrorDto>();
        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();
        if (Cache.LowGlobalScaleErrorCacheItem.LensInformation.LensCode == -1) Cache.LowGlobalScaleErrorCacheItem.LensInformation = ApplicationCookie.MicroscopeLensInformationList[0];
        if (Cache.HighGlobalScaleErrorCacheItem.LensInformation.LensCode == -1)
            Cache.HighGlobalScaleErrorCacheItem.LensInformation = ApplicationCookie.MicroscopeLensInformationList.Count <= 2
                ? ApplicationCookie.MicroscopeLensInformationList[^1]
                : ApplicationCookie.MicroscopeLensInformationList[2];

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
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.BaseLowSiteFindPosition);
                break;

            case 3:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.GetPosition(Cache.LowGlobalScaleErrorCacheItem.LensInformation));
                break;

            case 4:
                Cache.SiteDirection = StageDirectionTypeEnum.Up;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.GetPosition(Cache.HighGlobalScaleErrorCacheItem.LensInformation));
                break;

            case 5:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.GetPosition(Cache.LowGlobalScaleErrorCacheItem.LensInformation));
                break;

            case 6:
                Cache.SiteDirection = StageDirectionTypeEnum.Down;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.GetPosition(Cache.HighGlobalScaleErrorCacheItem.LensInformation));
                break;

            case 7:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.GetPosition(Cache.LowGlobalScaleErrorCacheItem.LensInformation));
                break;

            case 8:
                Cache.SiteDirection = StageDirectionTypeEnum.Left;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.GetPosition(Cache.HighGlobalScaleErrorCacheItem.LensInformation));
                break;

            case 9:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.GetPosition(Cache.LowGlobalScaleErrorCacheItem.LensInformation));
                break;

            case 10:
                Cache.SiteDirection = StageDirectionTypeEnum.Right;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.GetPosition(Cache.HighGlobalScaleErrorCacheItem.LensInformation));
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
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.BaseLowSiteFindPosition);
                return true;

            case 2 or 4 or 6 or 8:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.HighGlobalScaleErrorCacheItem.LensInformation);
                return true;

            case 1:
                Cache.SiteDirection = StageDirectionTypeEnum.Up;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.GetPosition(Cache.LowGlobalScaleErrorCacheItem.LensInformation));
                return true;

            case 3:
                Cache.SiteDirection = StageDirectionTypeEnum.Down;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.GetPosition(Cache.LowGlobalScaleErrorCacheItem.LensInformation));
                return true;

            case 5:
                Cache.SiteDirection = StageDirectionTypeEnum.Left;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.GetPosition(Cache.LowGlobalScaleErrorCacheItem.LensInformation));
                return true;

            case 7:
                Cache.SiteDirection = StageDirectionTypeEnum.Right;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowGlobalScaleErrorCacheItem.LensInformation);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.GetPosition(Cache.LowGlobalScaleErrorCacheItem.LensInformation));
                return true;

            case 10:
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
        if (Cache.HighGlobalScaleErrorCacheItem.LensInformation.LensCode <= Cache.LowGlobalScaleErrorCacheItem.LensInformation.LensCode)
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

            if (baseBrightFieldPosition.ToOriginLength >= Cache.WaferDiameter / 2)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Base Position is Out of Wafer!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.AlgorithmTemplateTypeEnum,
                Cache.WaferMaskTypeEnum,
                LowMagnification = Cache.LowGlobalScaleErrorCacheItem.LensInformation.LensName,
                HighMagnification = Cache.HighGlobalScaleErrorCacheItem.LensInformation.LensName,
                Cache.ColumnCellWidth,
                Cache.RowCellHeight,
                Cache.WaferDiameter
            }), HtmlLogUniqueId.LoggingHtml());

            if (IsRecipeCalibrate == false)
            {
                (var isSuccess, Cache.LowGlobalScaleErrorCacheItem.LeftPosition, Cache.LowGlobalScaleErrorCacheItem.RightPosition) = GetIdeaBrightFieldPosition(true, baseBrightFieldPosition.Y);
                if (isSuccess == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Get X Axis Idea Position Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }

                (isSuccess, Cache.LowGlobalScaleErrorCacheItem.BottomPosition, Cache.LowGlobalScaleErrorCacheItem.TopPosition) = GetIdeaBrightFieldPosition(false, baseBrightFieldPosition.X);
                if (isSuccess == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Get Y Axis Idea Position Failed!"), HtmlLogUniqueId.LoggingHtml());
                    return false;
                }
            }

            Logger.LogHtmlInformation("Idea Bright Field Positions", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.BaseLowSiteFindPosition,
                Cache.LowGlobalScaleErrorCacheItem.TopPosition,
                Cache.LowGlobalScaleErrorCacheItem.BottomPosition,
                Cache.LowGlobalScaleErrorCacheItem.LeftPosition,
                Cache.LowGlobalScaleErrorCacheItem.RightPosition
            }), HtmlLogUniqueId.LoggingHtml());

            result = true;
            return result;

            (bool isSuccess, Point negativePosition, Point positivePosition) GetIdeaBrightFieldPosition(bool isAxisX, double otherAxisValue)
            {
                try
                {
                    // 靠近边缘位置的理想位置可能拍不全，总长度截去一个die
                    var actualWaferRadius = Cache.GetActualWaferDiameter(isAxisX) / 2;
                    var interval = isAxisX ? Cache.ColumnCellWidth : Cache.RowCellHeight;
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
                Cache.SetPosition(result, Cache.LowGlobalScaleErrorCacheItem.LensInformation);

                var templateFilePath = $"{TemplateFileDirectory}\\{Cache.SiteDirection}Site_{Cache.LowGlobalScaleErrorCacheItem.LensInformation.LensName}_{Guid.NewGuid()}";
                var generateTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, templateFilePath, Cache.AlgorithmTemplateSizeEnum);
                if (generateTemplate == false)
                {
                    DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                var templateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath);
                Cache.SetTemplate(templateFilePath, templateImageFilePath, Cache.LowGlobalScaleErrorCacheItem.LensInformation);
            }

            var position = Cache.GetPosition(Cache.LowGlobalScaleErrorCacheItem.LensInformation);
            var (templateFilePathLog, templateImageFilePathLog) = Cache.GetTemplate(Cache.LowGlobalScaleErrorCacheItem.LensInformation);

            StageViewModel.SetBrightFieldAbsoluteStageXy(position);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                LensName = Cache.LowGlobalScaleErrorCacheItem.LensInformation.LensName,
                Cache.SiteDirection,
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
                if (Cache.SiteDirection is StageDirectionTypeEnum.Up) Cache.LowToHighMagnificationOffset = result - (Vector)Cache.LowGlobalScaleErrorCacheItem.TopPosition;

                Cache.SetPosition(result, Cache.HighGlobalScaleErrorCacheItem.LensInformation);

                var templateFilePath = $"{TemplateFileDirectory}\\{Cache.SiteDirection}Site_{Cache.HighGlobalScaleErrorCacheItem.LensInformation.LensName}_{Guid.NewGuid()}";
                var generateTemplate = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, templateFilePath, Cache.AlgorithmTemplateSizeEnum);
                if (generateTemplate == false)
                {
                    DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                var templateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath);
                Cache.SetTemplate(templateFilePath, templateImageFilePath, Cache.HighGlobalScaleErrorCacheItem.LensInformation);
            }

            var position = Cache.GetPosition(Cache.HighGlobalScaleErrorCacheItem.LensInformation);
            var (templateFilePathLog, templateImageFilePathLog) = Cache.GetTemplate(Cache.HighGlobalScaleErrorCacheItem.LensInformation);

            StageViewModel.SetBrightFieldAbsoluteStageXy(position);
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                LensName = Cache.HighGlobalScaleErrorCacheItem.LensInformation.LensName,
                Cache.SiteDirection,
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

            var idealPositionDictionary = new Dictionary<StageDirectionTypeEnum, Point>
            {
                { StageDirectionTypeEnum.Up, Cache.HighGlobalScaleErrorCacheItem.TopPosition },
                { StageDirectionTypeEnum.Down, Cache.HighGlobalScaleErrorCacheItem.BottomPosition },
                { StageDirectionTypeEnum.Left, Cache.HighGlobalScaleErrorCacheItem.LeftPosition },
                { StageDirectionTypeEnum.Right, Cache.HighGlobalScaleErrorCacheItem.RightPosition }
            };

            StageViewModel.ResetXYGlobalScale();

            List<Point> calibrationScaleErrorList = [];
            List<(double x, double y)> calibrationScaleList = [];
            foreach (var times in Enumerable.Range(1, 5))
            {
                cancellationToken.ThrowIfCancellationRequested();

                Logger.LogHtmlInformation($"Get Result :Times {times}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Calibration Result", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                var calibrationItem = times == 1 ? new ChuckGlobalScaleErrorDto() : GlobalScaleErrorDtoItemDtoList.Last().Clone();
                calibrationItem.ScaleX = 1.0;
                calibrationItem.ScaleY = 1.0;
                calibrationItem.ScaleErrorValue = Point.Origin;
                SynchronizationContextProvider.Send(() => GlobalScaleErrorDtoItemDtoList.Add(calibrationItem));
                SelectGlobalScaleErrorDto = calibrationItem;
                var (isSuccess, globalScaleErrorItemDto) = GetResult(idealPositionDictionary, calibrationItem, cancellationToken);
                if (isSuccess == false)
                {
                    Logger.LogError("{@Name} Error: Get Calibration Result Failed!", Name);
                    return false;
                }

                calibrationScaleList.Add((globalScaleErrorItemDto.ScaleX, globalScaleErrorItemDto.ScaleY));
                calibrationScaleErrorList.Add(globalScaleErrorItemDto.ScaleErrorValue);

                Logger.LogHtmlInformation("Applied Result", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                (isSuccess, var resultScaleErrorItemDto) = GetResult(idealPositionDictionary, globalScaleErrorItemDto, cancellationToken);

                Logger.LogHtmlInformation($"Applied {(isSuccess ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    appliedScaleX = globalScaleErrorItemDto.ScaleX,
                    appliedScaleY = globalScaleErrorItemDto.ScaleY,
                    resultScaleErrorItemDto.ScaleErrorValue,
                    resultScaleX = resultScaleErrorItemDto.ScaleX,
                    reslutScaleY = resultScaleErrorItemDto.ScaleY
                }), HtmlLogUniqueId.LoggingHtml());

                if (isSuccess == false)
                {
                    Logger.LogError("{@Name} Error: Get Applied Result Failed!", Name);
                    return false;
                }
            }

            var resultItemDto = GlobalScaleErrorDtoItemDtoList.Last();
            var resultList = GlobalScaleErrorDtoItemDtoList.Select(t => t)
                .Where(t => t != GlobalScaleErrorDtoItemDtoList.Minima(t => t.ScaleErrorValue.ToOriginLength).First()
                            && t != GlobalScaleErrorDtoItemDtoList.Maxima(t => t.ScaleErrorValue.ToOriginLength).First())
                .ToList();

            resultItemDto.ScaleX = resultList.Average(t => t.ScaleX);
            resultItemDto.ScaleY = resultList.Average(t => t.ScaleY);
            var xScaleErrorValueAverage = resultList.Average(t => t.ScaleErrorValue.X);
            var yScaleErrorValueAverage = resultList.Average(t => t.ScaleErrorValue.Y);
            resultItemDto.ScaleErrorValue = new Point(xScaleErrorValueAverage, yScaleErrorValueAverage);

            ResultGlobalScaleErrorDto = resultItemDto.Clone();

            var currentResult = Math.Abs(ResultGlobalScaleErrorDto.ScaleErrorValue.X) <= Cache.Threshold.X
                                && Math.Abs(ResultGlobalScaleErrorDto.ScaleErrorValue.Y) <= Cache.Threshold.Y;

            Logger.LogHtmlInformation($"Calibration {(currentResult ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                ResultGlobalScaleErrorDto.ScaleX,
                ResultGlobalScaleErrorDto.ScaleY,
                ResultGlobalScaleErrorDto.ScaleErrorValue,
                XScaleErrorCurve = new HtmlPlot2DLinesChart([
                    ("Times-CalibrationScaleError", calibrationScaleErrorList.Select(t => t.X).ToPoints()),
                    ("Times-AppliedScaleError", GlobalScaleErrorDtoItemDtoList.Select(t => t.ScaleErrorValue.X).ToList().ToPoints())
                ], "XScaleErrorCurve"),
                YScaleErrorCurve = new HtmlPlot2DLinesChart([
                    ("Times-CalibrationScaleError", calibrationScaleErrorList.Select(t => t.Y).ToPoints()),
                    ("Times-AppliedScaleError", GlobalScaleErrorDtoItemDtoList.Select(t => t.ScaleErrorValue.Y).ToList().ToPoints())
                ], "YScaleErrorCurve"),
                XScaleCurve = new HtmlPlot2DLinesChart([
                    ("Times-CalibrationScale", calibrationScaleList.Select(t => t.x).ToList().ToPoints()),
                    ("Times-AppliedScale", GlobalScaleErrorDtoItemDtoList.Select(t => t.ScaleX).ToList().ToPoints())
                ], "XScaleCurve"),
                YScaleCurve = new HtmlPlot2DLinesChart([
                    ("Times-CalibrationScale", calibrationScaleList.Select(t => t.y).ToList().ToPoints()),
                    ("Times-AppliedScale", GlobalScaleErrorDtoItemDtoList.Select(t => t.ScaleY).ToList().ToPoints())
                ], "YScaleCurve")
            }), HtmlLogUniqueId.LoggingHtml());

            result = currentResult;
            return result;
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
            SelectGlobalScaleErrorDto = new();

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                LowMagnification = Cache.LowGlobalScaleErrorCacheItem.LensInformation.LensName,
                HighMagnification = Cache.HighGlobalScaleErrorCacheItem.LensInformation.LensName,
                Cache.AlgorithmTemplateTypeEnum,
                Cache.BaseLowSiteFindPosition,
                Cache.LowToHighMagnificationOffset,
                Cache.Threshold,
                Cache.HighGlobalScaleErrorCacheItem.TopPosition,
                Cache.HighGlobalScaleErrorCacheItem.BottomPosition,
                Cache.HighGlobalScaleErrorCacheItem.LeftPosition,
                Cache.HighGlobalScaleErrorCacheItem.RightPosition,
                ReviewDto.ScaleX,
                ReviewDto.ScaleY,
                ReviewDto.ScaleErrorValue
            }), HtmlLogUniqueId.LoggingHtml());

            var idealPositionDictionary = new Dictionary<StageDirectionTypeEnum, Point>
            {
                { StageDirectionTypeEnum.Up, Cache.HighGlobalScaleErrorCacheItem.TopPosition },
                { StageDirectionTypeEnum.Down, Cache.HighGlobalScaleErrorCacheItem.BottomPosition },
                { StageDirectionTypeEnum.Left, Cache.HighGlobalScaleErrorCacheItem.LeftPosition },
                { StageDirectionTypeEnum.Right, Cache.HighGlobalScaleErrorCacheItem.RightPosition }
            };

            (var isSuccess, SelectGlobalScaleErrorDto) = GetResult(idealPositionDictionary, ReviewDto, cancellationToken);
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Get Result Failed!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            ReviewDto.IsVerified = isSuccess;
            Logger.LogHtmlInformation($"Verify {(isSuccess ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                VerifyScaleX = ReviewDto.ScaleX,
                VerifyScaleY = ReviewDto.ScaleY,
                VerifyScaleXResult = SelectGlobalScaleErrorDto.ScaleX,
                VerifyScaleYResult = SelectGlobalScaleErrorDto.ScaleY,
                SelectGlobalScaleErrorDto.ScaleErrorValue
            }), HtmlLogUniqueId.LoggingHtml());

            if (Save(ReviewDto, cancellationToken) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                ReviewDto.IsVerified = false;
                return false;
            }

            result = ReviewDto.IsVerified;

            if (IsAutoCalibrate == false)
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

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        if (CalibrationStatusService.EnableDependGlobalScaleErrorCalibrations(false, cancellationToken, out var errorMsg) == false)
        {
            Logger.LogError("Toggle {@Name} Enable Status Failed!", errorMsg);
            return false;
        }

        return true;
    }

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(GlobalScaleErrorDtoItemDtoList.Clear);
        ResultGlobalScaleErrorDto = null;
    }

    #endregion 校准

    #region 算法

    private (bool isSuccess, ChuckGlobalScaleErrorDto resultDto) GetResult(Dictionary<StageDirectionTypeEnum, Point> idealPositionDictionary, ChuckGlobalScaleErrorDto tempGlobalScaleErrorDto, CancellationToken cancellationToken)
    {
        StageViewModel.SetXYGlobalScale(tempGlobalScaleErrorDto.ScaleX, tempGlobalScaleErrorDto.ScaleY);

        foreach (var ideaPosition in idealPositionDictionary)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (MatchTemplate(ideaPosition.Key, ideaPosition.Value, ref tempGlobalScaleErrorDto) == false)
                return (false, tempGlobalScaleErrorDto);

            SelectGlobalScaleErrorDto = tempGlobalScaleErrorDto.Clone();
        }

        var xRealError = tempGlobalScaleErrorDto.RightHighSiteRealPosition - tempGlobalScaleErrorDto.LeftHighSiteRealPosition;
        var yRealError = tempGlobalScaleErrorDto.TopHighSiteRealPosition - tempGlobalScaleErrorDto.BottomHighSiteRealPosition;

        var resultScaleX = Math.Abs(xRealError.X / Cache.IdeaWidth);
        var resultScaleY = Math.Abs(yRealError.Y / Cache.IdeaHeight);

        tempGlobalScaleErrorDto.ScaleErrorValue = new Point(Math.Abs(Cache.IdeaWidth * (1 - resultScaleX)),
            Math.Abs(Cache.IdeaHeight * (1 - resultScaleY)));
        if (GlobalScaleErrorDtoItemDtoList.Count != 0)
        {
            SynchronizationContextProvider.Send(() =>
            {
                GlobalScaleErrorDtoItemDtoList.RemoveAt(GlobalScaleErrorDtoItemDtoList.Count - 1);
                GlobalScaleErrorDtoItemDtoList.Add(tempGlobalScaleErrorDto.Clone());
            });
            SelectGlobalScaleErrorDto = GlobalScaleErrorDtoItemDtoList.Last();
        }

        tempGlobalScaleErrorDto.ScaleX = resultScaleX;
        tempGlobalScaleErrorDto.ScaleY = resultScaleY;

        if (hostEnvironment.IsProduction() && (resultScaleX == 0 || resultScaleX >= 2 || resultScaleY == 0 || resultScaleY >= 2)) // 防止下发异常值
        {
            DialogWindowProvider.ShowDialog("Result scale is illegal !", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return (false, tempGlobalScaleErrorDto);
        }

        Logger.LogHtmlInformation($"Get Result OK", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
        {
            xIdeaError = Cache.IdeaWidth,
            yIdeaError = Cache.IdeaHeight,
            xRealError = xRealError.X,
            yRealError = yRealError.Y,
            resultScaleX,
            resultScaleY,
            tempGlobalScaleErrorDto.ScaleErrorValue
        }), HtmlLogUniqueId.LoggingHtml());

        return (true, tempGlobalScaleErrorDto);

        bool MatchTemplate(StageDirectionTypeEnum stageDirection, Point ideaPosition, ref ChuckGlobalScaleErrorDto resultDto)
        {
            var highSitePosition = stageDirection switch
            {
                StageDirectionTypeEnum.Up => resultDto.TopHighSiteRealPosition,
                StageDirectionTypeEnum.Down => resultDto.BottomHighSiteRealPosition,
                StageDirectionTypeEnum.Left => resultDto.LeftHighSiteRealPosition,
                StageDirectionTypeEnum.Right => resultDto.RightHighSiteRealPosition,
                _ => throw new ArgumentOutOfRangeException(nameof(stageDirection), stageDirection, null)
            };
            Cache.SiteDirection = stageDirection;
            var isFastMatch = true;
            var lowResultPosition = Point.Origin;
            var lowResultImageFilePath = string.Empty;
            if (highSitePosition == Point.Origin) // 快速匹配，避免来回切倍镜
            {
                var (lowTemplateFilePath, _) = Cache.GetTemplate(Cache.LowGlobalScaleErrorCacheItem.LensInformation);
                if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, ideaPosition - (Vector)Cache.LowToHighMagnificationOffset, Cache.LowGlobalScaleErrorCacheItem.LensInformation, lowTemplateFilePath, ImageFileDirectory,
                        null, Name,
                        $"Low Magnification {stageDirection} Site", out lowResultPosition, out _, out _, out lowResultImageFilePath, out _) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: Low Magnification {stageDirection} Site Matching Failed!"), HtmlLogUniqueId.LoggingHtml());
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(lowResultPosition), "Low Magnification Matching Failed!");
                }

                highSitePosition = lowResultPosition + (Vector)Cache.LowToHighMagnificationOffset;
                isFastMatch = false;
            }
            else
            {
                highSitePosition = new Point(highSitePosition.X / resultDto.ScaleX, highSitePosition.Y / resultDto.ScaleY);
            }

            var (highTemplateFilePath, _) = Cache.GetTemplate(Cache.HighGlobalScaleErrorCacheItem.LensInformation);
            if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, highSitePosition, Cache.HighGlobalScaleErrorCacheItem.LensInformation, highTemplateFilePath, ImageFileDirectory,
                    null, Name,
                    $"High Magnification {stageDirection} Site", out var highResultPosition, out _, out _, out var highResultImageFilePath, out _) == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header5, new HtmlComment($"Error: High Magnification {stageDirection} Site Matching Failed!"), HtmlLogUniqueId.LoggingHtml());
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(highResultPosition), "High Magnification Matching Failed!");
            }

            switch (stageDirection)
            {
                case StageDirectionTypeEnum.Up:
                    resultDto = resultDto.Clone();
                    if (isFastMatch == false) resultDto.TopLowSiteRealPosition = lowResultPosition;
                    else lowResultPosition = resultDto.TopLowSiteRealPosition;

                    if (isFastMatch == false) resultDto.TopLowSiteFindResultFilePath = lowResultImageFilePath;
                    else lowResultImageFilePath = resultDto.TopLowSiteFindResultFilePath;

                    resultDto.TopHighSiteRealPosition = highResultPosition;
                    resultDto.TopHighSiteFindResultFilePath = highResultImageFilePath;
                    break;

                case StageDirectionTypeEnum.Down:
                    resultDto = resultDto.Clone();
                    if (isFastMatch == false) resultDto.BottomLowSiteRealPosition = lowResultPosition;
                    else lowResultPosition = resultDto.BottomLowSiteRealPosition;

                    if (isFastMatch == false) resultDto.BottomLowSiteFindResultFilePath = lowResultImageFilePath;
                    else lowResultImageFilePath = resultDto.BottomLowSiteFindResultFilePath;

                    resultDto.BottomHighSiteRealPosition = highResultPosition;
                    resultDto.BottomHighSiteFindResultFilePath = highResultImageFilePath;
                    break;

                case StageDirectionTypeEnum.Left:
                    resultDto = resultDto.Clone();
                    if (isFastMatch == false) resultDto.LeftLowSiteRealPosition = lowResultPosition;
                    else lowResultPosition = resultDto.LeftLowSiteRealPosition;

                    if (isFastMatch == false) resultDto.LeftLowSiteFindResultFilePath = lowResultImageFilePath;
                    else lowResultImageFilePath = resultDto.LeftLowSiteFindResultFilePath;

                    resultDto.LeftHighSiteRealPosition = highResultPosition;
                    resultDto.LeftHighSiteFindResultFilePath = highResultImageFilePath;
                    break;

                case StageDirectionTypeEnum.Right:
                    resultDto = resultDto.Clone();
                    if (isFastMatch == false) resultDto.RightLowSiteRealPosition = lowResultPosition;
                    else lowResultPosition = resultDto.RightLowSiteRealPosition;

                    if (isFastMatch == false) resultDto.RightLowSiteFindResultFilePath = lowResultImageFilePath;
                    else lowResultImageFilePath = resultDto.RightLowSiteFindResultFilePath;
                    resultDto.RightHighSiteRealPosition = highResultPosition;
                    resultDto.RightHighSiteFindResultFilePath = highResultImageFilePath;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(stageDirection), stageDirection, null);
            }

            Logger.LogHtmlInformation($"{stageDirection} site Result", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                ideaPosition,
                lowResultPosition,
                highResultPosition,
                HtmlTab = new HtmlTab(new
                {
                    HighMatchImage = new HtmlImage(highResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    LowMatchImage = new HtmlImage(lowResultImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        }
    }

    #endregion 算法

    #region 自动化校准

    public override void GetAutoCalibrationStep()
    {
        AutoCalibrationStepList =
        [
            new() { StepName = "loading" },
            new() { StepName = "Find Real Position" },
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
                                LowMagnification = Cache.LowGlobalScaleErrorCacheItem.LensInformation.LensName,
                                HighMagnification = Cache.HighGlobalScaleErrorCacheItem.LensInformation.LensName
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

        if (CalibrationRecipeService.GetChuckReticleMaskInfo(Cache.WaferMaskTypeEnum, Cache.LowGlobalScaleErrorCacheItem.LensInformation, null, out var baseLowMaskInfo) == false)
            return false;
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticle, baseLowMaskInfo, out var lowPosition);

        // var (xDirection, yDirection) = StageViewModel.GetMachineDirection();
        //var chuckCenter = CacheProvider.Get<ChuckCenterObjDto>();
        //var machinePosition = chuckCenter!.NewBFCenterStagePosition
        //    + (Vector)new Point(xDirection * lowPosition.X, yDirection * lowPosition.Y);
        //MicroscopeViewModel.SwitchGetCurrentMicroscopeLensInformation(Cache.LowGlobalScaleErrorCacheItem);
        //StageViewModel.SetMachineAbsoluteStageXy(machinePosition);

        Cache.BaseLowSiteFindPosition = lowPosition;

        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleTop, baseLowMaskInfo, out var topLowSitePosition);
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleBottom, baseLowMaskInfo, out var bottomLowSitePosition);
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleLeft, baseLowMaskInfo, out var leftLowSitePosition);
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleRight, baseLowMaskInfo, out var rightLowSitePosition);
        Cache.SetPosition(topLowSitePosition, Cache.LowGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Up);
        Cache.SetPosition(bottomLowSitePosition, Cache.LowGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Down);
        Cache.SetPosition(leftLowSitePosition, Cache.LowGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Left);
        Cache.SetPosition(rightLowSitePosition, Cache.LowGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Right);
        Cache.SetTemplate(baseLowMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath, baseLowMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath, Cache.LowGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Up);
        Cache.SetTemplate(baseLowMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath, baseLowMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath, Cache.LowGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Down);
        Cache.SetTemplate(baseLowMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath, baseLowMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath, Cache.LowGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Left);
        Cache.SetTemplate(baseLowMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath, baseLowMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath, Cache.LowGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Right);

        if (CalibrationRecipeService.GetChuckReticleMaskInfo(Cache.WaferMaskTypeEnum, Cache.HighGlobalScaleErrorCacheItem.LensInformation, null, out var baseHighMaskInfo) == false)
            return false;
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleTop, baseHighMaskInfo, out var topHighSitePosition);
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleBottom, baseHighMaskInfo, out var bottomHighSitePosition);
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleLeft, baseHighMaskInfo, out var leftHighSitePosition);
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleRight, baseHighMaskInfo, out var rightHighSitePosition);
        Cache.SetPosition(topHighSitePosition, Cache.HighGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Up);
        Cache.SetPosition(bottomHighSitePosition, Cache.HighGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Down);
        Cache.SetPosition(leftHighSitePosition, Cache.HighGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Left);
        Cache.SetPosition(rightHighSitePosition, Cache.HighGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Right);
        Cache.SetTemplate(baseHighMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath, baseHighMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath, Cache.HighGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Up);
        Cache.SetTemplate(baseHighMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath, baseHighMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath, Cache.HighGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Down);
        Cache.SetTemplate(baseHighMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath, baseHighMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath, Cache.HighGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Left);
        Cache.SetTemplate(baseHighMaskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath, baseHighMaskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath, Cache.HighGlobalScaleErrorCacheItem.LensInformation, StageDirectionTypeEnum.Right);

        var waferMapDataInfo = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.DieBuilder.DiePitchSize;
        Cache.RowCellHeight = waferMapDataInfo.Height;
        Cache.ColumnCellWidth = waferMapDataInfo.Width;
        Cache.WaferDiameter = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.Wafer.Circle.Diameter;

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