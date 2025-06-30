using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Microscope;

[IOCAppService(ServiceType = typeof(MicroscopeCentricityCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeCentricityCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.MicroscopeMagnificationEnum);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.MicroscopeMagnificationEnum);

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select a location" },
        new() { StepName = "150X" },
        new() { StepName = "100X" },
        new() { StepName = "50X" },
        new() { StepName = "10X" },
        new() { StepName = "5X" }
    ];


    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private ObservableCollection<MicroscopeCentricityItemDto> _microscopeCentricityItemDtoList = [];

    [ObservableProperty]
    private MicroscopeCentricityItemDto? _selectMicroscopeCentricityItemDto;

    [ObservableProperty]
    private MicroscopeCentricityItemDto? _resultMicroscopeCentricityItemDto;

    [ObservableProperty]
    private MicroscopeCentricityItemDto _microscopeCentricityItemDto150X = new();

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    private ObservableCollection<MicroscopeCentricityItemDto> _reviewList = [];

    [ObservableProperty]
    private MicroscopeCentricityItemDto? _selectReviewItemDto;

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private MicroscopeCentricityCache _cache = new();

    [ObservableProperty]
    private MicroscopeCentricityItemDto[] _calibrations = [];

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
        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<MicroscopeCentricityCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<MicroscopeCentricityItemDto>();

        Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification5X;

        return isHasCache || RecipeCacheProvider.Set(Cache, cancellationToken);
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
        if (IsRecipeCalibrate)
        {
            if (CalibrationRecipeService.GetCorrectWaferMapByOffset(true) == false)
                return false;
            if (await AutomationRecipeInformationAsync(((int)Cache.MicroscopeMagnificationEnum).ToString()) == false) return false;
        }

        MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        ReviewList =
        [
            .. Calibrations
                .Where(t => t.IsCalibrated)
                .Select(t => t.Clone())
                .OrderBy(t => t.MicroscopeMagnificationEnum)
        ];

        return ReviewList.Count > 0;
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 1:
                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification5X;
                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);

                return true;

            case 2:
                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification150X;
                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
                return true;

            case 3:
                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification100X;
                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
                return true;

            case 4:
                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification50X;
                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
                return true;

            case 5:
                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification10X;
                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        var result = true;
        if (IsRecipeCalibrate || IsAutoCalibrate)
        {
            if (await AutomationRecipeInformationAsync(CalibrationStepIndex.ToString()) == false) result = false;
        }

        switch (CalibrationStepIndex)
        {
            case 0:
                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification150X;
                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition);
                return true;

            case 1:
                if (SaveResult() == false) result = false;

                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification100X;
                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
                return result;

            case 2:
                if (SaveResult() == false) result = false;

                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification50X;
                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
                return result;

            case 3:
                if (SaveResult() == false) return false;

                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification10X;
                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
                return result;

            case 4:
                if (SaveResult() == false) result = false;

                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification5X;
                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
                return result;

            case 5:
                if (SaveResult() == false) result = false;

                IsCalibrated = true;

                return result;

            default:
                return false;
        }

        bool SaveResult()
        {
            if (ResultMicroscopeCentricityItemDto is null)
            {
                DialogWindowProvider.TryShowDialog("Please find centricity size!", out var dialogButtonsEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
                if (dialogButtonsEnum == DialogResultEnum.Retry) return false;
            }
            else
            {
                ResultMicroscopeCentricityItemDto.IsCalibrated = true;
                if (Save(ResultMicroscopeCentricityItemDto, cancellationToken) == false)
                {
                    ResultMicroscopeCentricityItemDto.IsCalibrated = false;
                    Logger.LogError("{@Name} Error: SaveResult Failed!", Name);
                    return false;
                }
            }

            ClearCalibrationTemp();

            return true;
        }
    }

    protected override Task<bool> CancelingAsync()
    {
        StageViewModel.SetBrightFieldAbsoluteStageXy(new Point(0, 0));
        return Task.FromResult(true);
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand]
    private async Task GetFindPointAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                var result = StageViewModel.GetBrightFieldStagePosition();

                Cache.FindPosition = result;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Point Failed", Name);
        }
    }

    [RelayCommand]
    private async Task GotoFindPointAsync()
    {
        try
        {
            await Task.Run(() => StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.FindPosition)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
        }
    }

    [RelayCommand]
    private async Task GetPointAsync(string name)
    {
        try
        {
            await Task.Run(() =>
            {
                var result = StageViewModel.GetBrightFieldStagePosition();

                switch (name)
                {
                    case nameof(Cache.TemplateFindPosition150X):
                        Cache.TemplateFindPosition150X = result;

                        Cache.TemplateFilePath150X = $"{TemplateFileDirectory}\\{Cache.MicroscopeMagnificationEnum}_{Guid.NewGuid()}";
                        var generateTemplateLow1 = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.TemplateFilePath150X, Cache.AlgorithmTemplateSizeEnum);
                        if (generateTemplateLow1 == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        else Cache.TemplateImageFilePath150X = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.TemplateFilePath150X);

                        break;

                    case nameof(Cache.TemplateFindPosition100X):
                        Cache.TemplateFindPosition100X = result;

                        Cache.TemplateFilePath100X = $"{TemplateFileDirectory}\\{Cache.MicroscopeMagnificationEnum}_{Guid.NewGuid()}";
                        var generateTemplateLow2 = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.TemplateFilePath100X, Cache.AlgorithmTemplateSizeEnum);
                        if (generateTemplateLow2 == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        else Cache.TemplateImageFilePath100X = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.TemplateFilePath100X);

                        break;

                    case nameof(Cache.TemplateFindPosition50X):
                        Cache.TemplateFindPosition50X = result;

                        Cache.TemplateFilePath50X = $"{TemplateFileDirectory}\\{Cache.MicroscopeMagnificationEnum}_{Guid.NewGuid()}";
                        var generateTemplateLow3 = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.TemplateFilePath50X, Cache.AlgorithmTemplateSizeEnum);
                        if (generateTemplateLow3 == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        else Cache.TemplateImageFilePath50X = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.TemplateFilePath50X);

                        break;

                    case nameof(Cache.TemplateFindPosition10X):
                        Cache.TemplateFindPosition10X = result;

                        Cache.TemplateFilePath10X = $"{TemplateFileDirectory}\\{Cache.MicroscopeMagnificationEnum}_{Guid.NewGuid()}";
                        var generateTemplateLow4 = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.TemplateFilePath10X, Cache.AlgorithmTemplateSizeEnum);
                        if (generateTemplateLow4 == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        else Cache.TemplateImageFilePath10X = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.TemplateFilePath10X);

                        break;

                    case nameof(Cache.TemplateFindPosition5X):
                        Cache.TemplateFindPosition5X = result;

                        Cache.TemplateFilePath5X = $"{TemplateFileDirectory}\\{Cache.MicroscopeMagnificationEnum}_{Guid.NewGuid()}";
                        var generateTemplateLow5 = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.TemplateFilePath5X, Cache.AlgorithmTemplateSizeEnum);
                        if (generateTemplateLow5 == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        else Cache.TemplateImageFilePath5X = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.TemplateFilePath5X);

                        break;
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Get Point Failed", Name);
        }
    }

    [RelayCommand]
    private async Task GotoPointAsync(string name)
    {
        try
        {
            await Task.Run(() => StageViewModel.SetBrightFieldAbsoluteStageXy((Point)Cache.GetType().GetProperty(name)!.GetValue(Cache))).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Move Point Failed", Name);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    public async Task<bool> Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeMagnificationEnum,
                Cache.AlgorithmTemplateTypeEnum,
                Cache.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
        return true;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    public async Task<bool> Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await InvokeCalibrateAsync(() =>
        {
            try
            {
                ClearCalibrationTemp();
                var (isSuccess, errorMessage) = Cache.Verify();
                if (isSuccess == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                    DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                var detectImageDirectory = ImageFileDirectory;
                var cacheGetTemplateFindPosition = Cache.GetTemplateFindPosition();
                var templateFilePath = Cache.GetTemplateFilePath();

                if (MatchTemplate(cacheGetTemplateFindPosition, templateFilePath, detectImageDirectory, cancellationToken) == false) return false;

                var averageX = MicroscopeCentricityItemDtoList.Average(t => t.CentricityPosition.X);
                var averageY = MicroscopeCentricityItemDtoList.Average(t => t.CentricityPosition.Y);
                var averageCentricityPosition = new Point(averageX, averageY);
                StageViewModel.SetBrightFieldAbsoluteStageXy(averageCentricityPosition);

                SelectMicroscopeCentricityItemDto = MicroscopeCentricityItemDtoList.OrderBy(t => (t.CentricityPosition - (Vector)averageCentricityPosition).ToOriginLength).First();
                ResultMicroscopeCentricityItemDto = SelectMicroscopeCentricityItemDto.Clone();
                ResultMicroscopeCentricityItemDto.CentricityPosition = averageCentricityPosition;
                switch (CalibrationStepIndex)
                {
                    case 1:
                        ResultMicroscopeCentricityItemDto.Offset = Cache.TemplateFindPosition150X - (Vector)ResultMicroscopeCentricityItemDto.CentricityPosition;
                        MicroscopeCentricityItemDto150X.CentricityPosition = ResultMicroscopeCentricityItemDto.CentricityPosition;
                        break;

                    case 2 or 3 or 4 or 5:
                        ResultMicroscopeCentricityItemDto.Offset = ResultMicroscopeCentricityItemDto.CentricityPosition - (Vector)MicroscopeCentricityItemDto150X.CentricityPosition;
                        break;
                }

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    MicroscopeMagnification = ResultMicroscopeCentricityItemDto.MicroscopeMagnificationEnum,
                    ResultMicroscopeCentricityItemDto.CentricityPosition,
                    ResultMicroscopeCentricityItemDto.Offset,
                    Score = ResultMicroscopeCentricityItemDto.TemplateScore,
                    Angle = ResultMicroscopeCentricityItemDto.TemplateAngle,
                    HtmlTab = new HtmlTab(new
                    {
                        ResultImage = new HtmlImage(ResultMicroscopeCentricityItemDto.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        TemplateImage = new HtmlImage(Cache.MicroscopeMagnificationEnum == MicroscopeMagnificationEnum.Magnification150X
                            ? ResultMicroscopeCentricityItemDto.FilePath
                            : ResultMicroscopeCentricityItemDto.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                }), HtmlLogUniqueId.LoggingHtml());
                result = true;
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{@Name}: Step1CalibrateActionAsync Failed", Name);
                result = false;
                return result;
            }
        });
        return result;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> VerifyActionAsync(CancellationToken cancellationToken)
    {
        var result = true;
        if (SelectReviewItemDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        await InvokeVerifyAsync(async () =>
        {
            if (await VerifyCalibrationAsync(SelectReviewItemDto, cancellationToken) == false) result = false;
            return result;
        }).ConfigureAwait(false);
        return result;
    }

    private async Task<bool> VerifyCalibrationAsync(MicroscopeCentricityItemDto selectReviewItemDto, CancellationToken cancellationToken)
    {
        var result = true;
        await Task.Run(() =>
        {
            if (selectReviewItemDto is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                result = false;
            }
            else
            {
                Cache.MicroscopeMagnificationEnum = selectReviewItemDto.MicroscopeMagnificationEnum;
                Logger.LogHtmlInformation($"{Cache.MicroscopeMagnificationEnum}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                ClearCalibrationTemp();
                var detectImageDirectory = ImageFileDirectory;

                selectReviewItemDto.IsVerified = false;

                var centricityItemDto150X = ReviewList.SingleOrDefault(t => t.MicroscopeMagnificationEnum == MicroscopeMagnificationEnum.Magnification150X);

                if (IsAutoCalibrate)
                {
                    if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.TemplateFindPosition50X, MicroscopeMagnificationEnum.Magnification50X, Cache.TemplateFilePath50X, detectImageDirectory, HtmlLogUniqueId, Name, string.Empty,
                            out var resultPosition50X, out _, out _, out _, out _) == false) return;
                    if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, resultPosition50X, MicroscopeMagnificationEnum.Magnification150X, Cache.TemplateFilePath150X, detectImageDirectory, HtmlLogUniqueId, Name, string.Empty,
                            out _, out _, out _, out _, out _) == false) return;
                }
                else
                {
                    if (centricityItemDto150X is null)
                    {
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Please calibrate 150X first!"), HtmlLogUniqueId.LoggingHtml());
                        result = false;
                    }
                    else
                    {
                        StageViewModel.SetBrightFieldAbsoluteStageXy(centricityItemDto150X.CentricityPosition);
                    }
                }

                var templateFilePath = Cache.GetTemplateFilePath();
                MicroscopeViewModel.SwitchMagnification(Cache.MicroscopeMagnificationEnum);
                StageViewModel.MoveRelativeStageXy(selectReviewItemDto.Offset);

                var oldPosition = StageViewModel.GetBrightFieldStagePosition();

                if (MatchTemplate(oldPosition, templateFilePath, detectImageDirectory, cancellationToken, 1) == false)
                {
                    result = false;
                    return;
                }

                var microscopeCentricityItem = MicroscopeCentricityItemDtoList[0];
                var newPosition = microscopeCentricityItem.CentricityPosition;
                var error = newPosition - (Vector)oldPosition;
                result = error.ToOriginLength < Cache.Threshold.ToOriginLength;
                Cache.VerifyResultPosition = newPosition;
                Cache.VerifyResultError = error;

                Logger.LogHtmlInformation($"Verify {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    NewPosition = newPosition,
                    OldPosition = oldPosition,
                    Error = error,
                    Cache.Threshold,
                    MicroscopeMagnification = microscopeCentricityItem.MicroscopeMagnificationEnum,
                    microscopeCentricityItem.CentricityPosition,
                    microscopeCentricityItem.Offset,
                    Score = microscopeCentricityItem.TemplateScore,
                    Angle = microscopeCentricityItem.TemplateAngle,
                    HtmlTab = new HtmlTab(new
                    {
                        ResultImage = new HtmlImage(microscopeCentricityItem.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        TemplateImage = new HtmlImage(Cache.MicroscopeMagnificationEnum == MicroscopeMagnificationEnum.Magnification150X
                            ? microscopeCentricityItem.FilePath
                            : microscopeCentricityItem.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                selectReviewItemDto.IsVerified = result;
                if (Save(selectReviewItemDto, cancellationToken) == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                    selectReviewItemDto.IsVerified = false;
                    result = false;
                    return;
                }

                if (!IsAutoCalibrate)
                {
                    DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, New Offset: ({newPosition}) Old Offset: ({oldPosition}) Error: ({error})", DialogButtonsEnum.OK,
                        result ? DialogIconEnum.Information : DialogIconEnum.Warning);
                }

                if (result == false)
                    return;

                // 同心验证
                var varifiedDtoList = Calibrations.Where(t => t.IsOk).ToList();
                if (varifiedDtoList.Count >= 2)
                {
                    var concentricOffset = Calibrations.Max(t => t.Offset.ToOriginLength) - Calibrations.Min(t => t.Offset.ToOriginLength);
                    result = Math.Abs(concentricOffset) <= Cache.ConcentricThreshold;
                    if (result == false)
                    {
                        DialogWindowProvider.ShowDialog($"Concentric {(result ? "OK" : "Failed")}, Offset: {concentricOffset}", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);
                        selectReviewItemDto.IsVerified = false;
                        if (Save(selectReviewItemDto, cancellationToken) == false)
                        {
                            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                            return;
                        }
                    }

                    Logger.LogHtmlInformation($"Concentric {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        Cache.ConcentricThreshold,
                        concentricOffset,
                        MinEcsMicroscopeType = Calibrations.Minima(t => t.Offset.ToOriginLength).Single().MicroscopeMagnificationEnum,
                        MaxEcsMicroscopeType = Calibrations.Maxima(t => t.Offset.ToOriginLength).Single().MicroscopeMagnificationEnum,
                        DistanceResult = new HtmlTable([.. Calibrations.Select(t => new { t.IsVerified, t.MicroscopeMagnificationEnum, t.CentricityPosition, t.Offset, Distance = t.Offset.ToOriginLength }).Cast<object>()])
                    }), HtmlLogUniqueId.LoggingHtml());
                }
            }
        }, cancellationToken);
        return result;
    }

    private bool MatchTemplate(Point position, string templatePath, string detectImageDirectory, CancellationToken cancellationToken, int repeatCount = 5)
    {
        try
        {
            Logger.LogHtmlInformation($"Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                MicroscopeMagnification = Cache.MicroscopeMagnificationEnum,
                Cache.AlgorithmTemplateTypeEnum,
                FindFocusPosition = position,
                ImageFileDirectory = detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            foreach (var times in Enumerable.Range(1, repeatCount))
            {
                cancellationToken.ThrowIfCancellationRequested();
                Logger.LogHtmlInformation($"Repeat Time:{times}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, position, Cache.MicroscopeMagnificationEnum, templatePath, detectImageDirectory, HtmlLogUniqueId, Name, string.Empty,
                        out var resultPosition, out var score, out var angle, out var resultImageFilePath, out _) == false) return false;

                var microscopeCentricityItemDto = new MicroscopeCentricityItemDto
                {
                    MicroscopeMagnificationEnum = Cache.MicroscopeMagnificationEnum,
                    CentricityPosition = resultPosition,
                    FilePath = resultImageFilePath,
                    TemplateFilePath = templatePath,
                    TemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templatePath),
                    Offset = Cache.MicroscopeMagnificationEnum == MicroscopeMagnificationEnum.Magnification150X ? Point.Origin : resultPosition - (Vector)position,
                    TemplateScore = score,
                    TemplateAngle = angle
                };

                Logger.LogHtmlInformation("Match Template Result", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                {
                    MicroscopeMagnification = microscopeCentricityItemDto.MicroscopeMagnificationEnum,
                    ResultPosition = microscopeCentricityItemDto.CentricityPosition
                }), HtmlLogUniqueId.LoggingHtml());

                StageViewModel.SetBrightFieldAbsoluteStageXy(position);

                SynchronizationContextProvider.Send(() => MicroscopeCentricityItemDtoList.Add(microscopeCentricityItemDto));
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: MatchTemplate Failed", Name);
            return false;
        }
    }

    private bool Save(MicroscopeCentricityItemDto itemDto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibrations =
        [
            .. Calibrations
                .Where(t => t.MicroscopeMagnificationEnum != itemDto.MicroscopeMagnificationEnum),
            itemDto.Clone(),
        ];

        ReviewList =
        [
            .. ReviewList
                .Where(t => t.MicroscopeMagnificationEnum != itemDto.MicroscopeMagnificationEnum)
                .Concat([itemDto.Clone()])
        ];

        return CacheProvider.SetArray(Calibrations, cancellationToken) && RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(MicroscopeCentricityItemDtoList.Clear);
        SelectMicroscopeCentricityItemDto = null;
        ResultMicroscopeCentricityItemDto = null;
    }

    #endregion 校准

    #region 自动化校准

    public override void GetAutoCalibrationStep()
    {
        AutoCalibrationStepList =
        [
            new() { StepName = "loading" },
            new() { StepName = "150X" },
            new() { StepName = "100X" },
            new() { StepName = "50X" },
            new() { StepName = "10X" },
            new() { StepName = "5X" },
            new() { StepName = "Review" }
        ];
    }

    public override async Task<bool> AutomationActionAsync(CancellationToken cancellationToken)
    {
        try
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
                        if (await AutoStepAsync() == false) return false;
                        if (await NextingAsync(cancellationToken) == false) return false;
                        await InvokeCalibrateAsync(() =>
                        {
                            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                            {
                                Cache.MicroscopeMagnificationEnum,
                                Cache.AlgorithmTemplateTypeEnum,
                                Cache.FindPosition
                            }), HtmlLogUniqueId.LoggingHtml());
                            return true;
                        });
                        CalibrationStepIndex++;
                        AutoCalibrationStepIndex++;
                        CalibrationStepName = AutoCalibrationStepList[AutoCalibrationStepIndex].StepName;
                        break;

                    case 1:
                        if (await AutoActionStepAsync(cancellationToken) == false)
                        {
                            DialogWindowProvider.ShowDialog($"Auto Calibration {Cache.MicroscopeMagnificationEnum} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            return false;
                        }

                        break;

                    case 2:
                        if (await AutoActionStepAsync(cancellationToken) == false)
                        {
                            DialogWindowProvider.ShowDialog($"Auto Calibration {Cache.MicroscopeMagnificationEnum} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            return false;
                        }

                        break;

                    case 3:
                        if (await AutoActionStepAsync(cancellationToken) == false)
                        {
                            DialogWindowProvider.ShowDialog($"Auto Calibration {Cache.MicroscopeMagnificationEnum} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            return false;
                        }

                        break;

                    case 4:
                        if (await AutoActionStepAsync(cancellationToken) == false)
                        {
                            DialogWindowProvider.ShowDialog($"Auto Calibration {Cache.MicroscopeMagnificationEnum} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            return false;
                        }

                        break;

                    case 5:
                        if (await AutoActionStepAsync(cancellationToken) == false)
                        {
                            DialogWindowProvider.ShowDialog($"Auto Calibration {Cache.MicroscopeMagnificationEnum} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            return false;
                        }

                        break;

                    case 6:
                        AutoReviewCalibrationStepIndex = AutoCalibrationStepList.Count - 1;
                        if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false) return false;
                        if (await InvokeCalibrateAsync(async () =>
                            {
                                foreach (var itemReview in ReviewList)
                                {
                                    SelectReviewItemDto = itemReview;
                                    if (await VerifyCalibrationAsync(SelectReviewItemDto, cancellationToken) == false) return false;
                                }

                                result = true;
                                return result;
                            }) == false) return false;
                        AutoCalibrationStepIndex++;
                        if (await AutoStepAsync() == false) return false;
                        break;
                }

                AutoCalibrationProgress = AutoCalibrationStepIndex / (double)AutoCalibrationStepList.Count * 100;
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{@Name}: Auto Calibration Failed", Name);
            return false;
        }
    }

    public override async Task<bool> AutomationRecipeInformationAsync(string microscopeName)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (IsRecipeCalibrate == false)
            return true;

        if (CalibrationRecipeDto is null)
        {
            DialogWindowProvider.ShowDialog("Revise wafer map is empty!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationRecipeService.GetCorrectWaferMapByOffset(false) == false)
            return false;

        var originReticle = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleModel.Single(t => t.Index is { X: 0, Y: 0 });
        switch (microscopeName)
        {
            case "0":
                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification150X;
                if (CalibrationRecipeService.GetMicroscopeReticleMaskInfo(WaferMaskTypeEnum.DieCorner, Cache.MicroscopeMagnificationEnum, null, out var maskInfo150) == false)
                    return false;
                CalibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticle, maskInfo150, out var position150);
                Cache.TemplateFindPosition150X = position150;
                Cache.TemplateFilePath150X = maskInfo150.RecipeBrightFieldTemplateDto.TemplateFilePath;
                Cache.TemplateImageFilePath150X = maskInfo150.RecipeBrightFieldTemplateDto.TemplateImageFilePath;
                Cache.FindPosition = Cache.TemplateFindPosition150X;
                break;

            case "1":
                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification100X;
                if (CalibrationRecipeService.GetMicroscopeReticleMaskInfo(WaferMaskTypeEnum.DieCorner, Cache.MicroscopeMagnificationEnum, null, out var maskInfo100) == false)
                    return false;
                CalibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticle, maskInfo100, out var position100);
                Cache.TemplateFindPosition100X = position100;
                Cache.TemplateFilePath100X = maskInfo100.RecipeBrightFieldTemplateDto.TemplateFilePath;
                Cache.TemplateImageFilePath100X = maskInfo100.RecipeBrightFieldTemplateDto.TemplateImageFilePath;
                Cache.FindPosition = Cache.TemplateFindPosition100X;
                break;

            case "2":
                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification50X;
                if (CalibrationRecipeService.GetMicroscopeReticleMaskInfo(WaferMaskTypeEnum.DieCorner, Cache.MicroscopeMagnificationEnum, null, out var maskInfo50) == false)
                    return false;
                CalibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticle, maskInfo50, out var position50);
                Cache.TemplateFindPosition50X = position50;
                Cache.TemplateFilePath50X = maskInfo50.RecipeBrightFieldTemplateDto.TemplateFilePath;
                Cache.TemplateImageFilePath50X = maskInfo50.RecipeBrightFieldTemplateDto.TemplateImageFilePath;
                Cache.FindPosition = Cache.TemplateFindPosition50X;
                break;

            case "3":
                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification10X;
                if (CalibrationRecipeService.GetMicroscopeReticleMaskInfo(WaferMaskTypeEnum.DieCorner, Cache.MicroscopeMagnificationEnum, null, out var maskInfo10) == false)
                    return false;
                CalibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticle, maskInfo10, out var position10);
                Cache.TemplateFindPosition10X = position10;
                Cache.TemplateFilePath10X = maskInfo10.RecipeBrightFieldTemplateDto.TemplateFilePath;
                Cache.TemplateImageFilePath10X = maskInfo10.RecipeBrightFieldTemplateDto.TemplateImageFilePath;
                Cache.FindPosition = Cache.TemplateFindPosition10X;
                break;

            case "4":
                Cache.MicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification5X;
                if (CalibrationRecipeService.GetMicroscopeReticleMaskInfo(WaferMaskTypeEnum.DieCorner, Cache.MicroscopeMagnificationEnum, null, out var maskInfo5) == false)
                    return false;
                CalibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticle, maskInfo5, out var position5);
                Cache.TemplateFindPosition5X = position5;
                Cache.TemplateFilePath5X = maskInfo5.RecipeBrightFieldTemplateDto.TemplateFilePath;
                Cache.TemplateImageFilePath5X = maskInfo5.RecipeBrightFieldTemplateDto.TemplateImageFilePath;
                Cache.FindPosition = Cache.TemplateFindPosition5X;
                break;
        }

        return true;
    }

    private async Task<bool> AutoActionStepAsync(CancellationToken cancellationToken)
    {
        if (await Step1CalibrateActionAsync(cancellationToken) == false) return false;
        Thread.Sleep(2000);
        if (await NextingAsync(cancellationToken) == false) return false;
        CalibrationStepIndex++;
        if (await AutoNextingAsync(cancellationToken) == false) return false;
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
                if (await AutomationRecipeInformationAsync("0") == false) return false;
                if (await AutomationRecipeInformationAsync("2") == false) return false;
                foreach (var (_, itemReview) in ReviewList.Select((t, i) => (index: i, itemReview: t)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SelectReviewItemDto = itemReview;
                    if (await VerifyCalibrationAsync(SelectReviewItemDto, cancellationToken) == false)
                    {
                        DialogWindowProvider.ShowDialog($"Auto Calibration Review {SelectReviewItemDto.MicroscopeMagnificationEnum} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                        return false;
                    }
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