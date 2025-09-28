using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Extensions;
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

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override List<CalibrationItemStep> CalibrationStepList { get; } = [];

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
    private MicroscopeCentricityCacheItem _selectMicroscopeCentricityCacheItem = new();

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

        SynchronizationContextProvider.Send(() =>
        {
            CalibrationStepList.Clear();
            CalibrationStepList.AddRange([
                new() { StepName = "Select a location" },
                .. ApplicationCookie.MicroscopeLensInformationList
                    .Select(t => t)
                    .OrderByDescending(t => t.ObjectiveMagnification)
                    .ThenByDescending(t => t.LensCode)
                    .Select(info => new CalibrationItemStep { StepName = info.LensName })
            ]);
        });
        MicroscopePixelSizeItems = microscopePixelSizeItems;
        (_, Cache) = RecipeCacheProvider.TryGetOrDefault<MicroscopeCentricityCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<MicroscopeCentricityItemDto>();

        Calibrations = [.. Calibrations.Where(t => ApplicationCookie.MicroscopeLensInformationList.Contains(t.LensInformation))]; // 过滤掉变更静态配置后原来的缓存
        if (Cache.MicroscopeLensInformation.LensCode == -1)
            Cache.MicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformationList[0];

        if (Cache.InitializeCacheList(ApplicationCookie.MicroscopeLensInformationList) == false)
        {
            Logger.LogError("{@Name} Error: Initialize Cache List Failed!", Name);
        }

        RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        if (IsRecipeCalibrate)
        {
            if (CalibrationRecipeService.GetCorrectWaferMapByOffset(true) == false)
                return false;
            if (await AutomationRecipeInformationAsync(Cache.MicroscopeLensInformation.LensName) == false) return false;
        }

        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
        SelectMicroscopeCentricityCacheItem = Cache.GetSelectedCacheItem();
        StageViewModel.SetBrightFieldAbsoluteStageXy(SelectMicroscopeCentricityCacheItem.FindPosition);

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
                .OrderBy(t => t.LensInformation.ObjectiveMagnification)
                .ThenBy(t => t.LensInformation.LensCode)
        ];

        return ReviewList.Count > 0;
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Cache.MicroscopeLensInformation = CalibrationStepIndex switch
        {
            > 1 => ApplicationCookie.MicroscopeLensInformationList
                .Select(t => t)
                .OrderByDescending(t => t.ObjectiveMagnification)
                .ThenByDescending(t => t.LensCode)
                .ElementAt(CalibrationStepIndex - 1),
            1 => ApplicationCookie.MicroscopeLensInformationList[0],
            _ => throw new ArgumentOutOfRangeException()
        };

        SelectMicroscopeCentricityCacheItem = Cache.GetSelectedCacheItem();
        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
        StageViewModel.SetBrightFieldAbsoluteStageXy(SelectMicroscopeCentricityCacheItem.FindPosition);
        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        var result = true;
        if (CalibrationStepIndex != 0)
            if (SaveResult() == false)
                result = false;

        if (CalibrationStepIndex == CalibrationStepList.Count - 1)
        {
            IsCalibrated = true;
            return result;
        }

        Cache.MicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformationList
            .Select(t => t)
            .OrderByDescending(t => t.ObjectiveMagnification)
            .ThenByDescending(t => t.LensCode)
            .ElementAt(CalibrationStepIndex);

        if (IsRecipeCalibrate || IsAutoCalibrate)
            if (await AutomationRecipeInformationAsync(Cache.MicroscopeLensInformation.LensName) == false)
                result = false;

        SelectMicroscopeCentricityCacheItem = Cache.GetSelectedCacheItem();
        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);

        return result;

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
        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);
        return Task.FromResult(true);
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(() =>
        {
            var result = StageViewModel.GetBrightFieldStagePosition();
            Cache.SetFindPosition(result);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                LensName = Cache.MicroscopeLensInformation.LensName,
                Cache.AlgorithmTemplateTypeEnum,
                SelectMicroscopeCentricityCacheItem.WaferMaskTypeEnum,
                SelectMicroscopeCentricityCacheItem.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
        return true;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        var result = false;
        await InvokeCalibrateAsync(() =>
        {
            try
            {
                ClearCalibrationTemp();

                var position = StageViewModel.GetBrightFieldStagePosition();
                Cache.SetFindPosition(position);

                Cache.SetTemplateFilePath($"{TemplateFileDirectory}\\{Cache.MicroscopeLensInformation.LensName}_{Guid.NewGuid()}");

                var generateTemplateLow1 = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, SelectMicroscopeCentricityCacheItem.TemplateFilePath, Cache.AlgorithmTemplateSizeEnum);

                if (generateTemplateLow1 == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                else Cache.SetTemplateImageFilePath(CalibrationConstantsHelper.TemplatePathToTemplateImagePath(SelectMicroscopeCentricityCacheItem.TemplateFilePath));

                var (isSuccess, errorMessage) = Cache.Verify();
                if (isSuccess == false)
                {
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                    DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                var detectImageDirectory = ImageFileDirectory;
                var cacheGetTemplateFindPosition = SelectMicroscopeCentricityCacheItem.FindPosition;
                var templateFilePath = SelectMicroscopeCentricityCacheItem.TemplateFilePath;

                if (MatchTemplate(cacheGetTemplateFindPosition, templateFilePath, detectImageDirectory, cancellationToken) == false) return false;

                var averageX = MicroscopeCentricityItemDtoList.Average(t => t.CentricityPosition.X);
                var averageY = MicroscopeCentricityItemDtoList.Average(t => t.CentricityPosition.Y);
                var averageCentricityPosition = new Point(averageX, averageY);
                StageViewModel.SetBrightFieldAbsoluteStageXy(averageCentricityPosition);

                SelectMicroscopeCentricityItemDto = MicroscopeCentricityItemDtoList.OrderBy(t => (t.CentricityPosition - (Vector)averageCentricityPosition).ToOriginLength).First();
                ResultMicroscopeCentricityItemDto = SelectMicroscopeCentricityItemDto.Clone();
                ResultMicroscopeCentricityItemDto.CentricityPosition = averageCentricityPosition;

                var maxMagnification = Cache.MicroscopeCentricityCacheItem.Last().LensInformation;

                ResultMicroscopeCentricityItemDto.Offset = ResultMicroscopeCentricityItemDto.LensInformation != maxMagnification
                    ? ResultMicroscopeCentricityItemDto.CentricityPosition - (Vector)Calibrations.Single(t => t.LensInformation == maxMagnification).CentricityPosition
                    : Point.Origin;

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ResultMicroscopeCentricityItemDto.LensInformation.LensName,
                    ResultMicroscopeCentricityItemDto.CentricityPosition,
                    ResultMicroscopeCentricityItemDto.Offset,
                    Score = ResultMicroscopeCentricityItemDto.TemplateScore,
                    Angle = ResultMicroscopeCentricityItemDto.TemplateAngle,
                    HtmlTab = new HtmlTab(new
                    {
                        ResultImage = new HtmlImage(ResultMicroscopeCentricityItemDto.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        TemplateImage = new HtmlImage(ResultMicroscopeCentricityItemDto.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
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
                Cache.MicroscopeLensInformation = selectReviewItemDto.LensInformation;
                SelectMicroscopeCentricityCacheItem = Cache.GetSelectedCacheItem();

                Logger.LogHtmlInformation($"{Cache.MicroscopeLensInformation.LensName}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                ClearCalibrationTemp();
                var detectImageDirectory = ImageFileDirectory;

                selectReviewItemDto.IsVerified = false;

                var centricityItemMaxDto = ReviewList.Single(t => t.LensInformation == Cache.MicroscopeCentricityCacheItem.Last().LensInformation);

                if (IsAutoCalibrate) // 定位最高倍的位置
                {
                    if (selectReviewItemDto.LensInformation == ApplicationCookie.MicroscopeLensInformationList[0])
                    {
                        //if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.GetFindPosition(magnificationInfos[0]), magnificationInfos[0], Cache.GetTemplateFilePath(magnificationInfos[0]), detectImageDirectory, HtmlLogUniqueId, Name, "Low Magnification",
                        //    out var resultPositionLow, out _, out _, out _, out _) == false) return;

                        //var highMagnificationInfo = magnificationInfos[magnificationInfos.Count < 3 ? magnificationInfos.Count - 1 : 2];
                        //if (magnificationInfos.Count == 1 || ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, resultPositionLow, highMagnificationInfo, Cache.GetTemplateFilePath(highMagnificationInfo), detectImageDirectory, HtmlLogUniqueId, Name, "High Magnification",
                        //        out resultPositionLow, out _, out _, out _, out _) == false) return;

                        if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, Cache.GetFindPosition(centricityItemMaxDto!.LensInformation), centricityItemMaxDto.LensInformation, Cache.GetTemplateFilePath(centricityItemMaxDto.LensInformation), detectImageDirectory, HtmlLogUniqueId, Name, "Max Magnification",
                                out var maxMatchResultPosition, out _, out _, out _, out _) == false)
                        {
                            result = false;
                            return;
                        }

                        Cache.MicroscopeCentricityCacheItem.Single(t => t.LensInformation == centricityItemMaxDto.LensInformation).FindPosition = maxMatchResultPosition;
                    }

                    StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.MicroscopeCentricityCacheItem.Single(t => t.LensInformation == centricityItemMaxDto!.LensInformation).FindPosition);
                }
                else
                {
                    if (centricityItemMaxDto is null)
                    {
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Please calibrate max magnification first!"), HtmlLogUniqueId.LoggingHtml());
                        result = false;
                        return;
                    }
                    else
                        StageViewModel.SetBrightFieldAbsoluteStageXy(centricityItemMaxDto.CentricityPosition);
                }

                var templateFilePath = SelectMicroscopeCentricityCacheItem.TemplateFilePath;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
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
                    microscopeCentricityItem.LensInformation.LensName,
                    microscopeCentricityItem.CentricityPosition,
                    microscopeCentricityItem.Offset,
                    Score = microscopeCentricityItem.TemplateScore,
                    Angle = microscopeCentricityItem.TemplateAngle,
                    HtmlTab = new HtmlTab(new
                    {
                        ResultImage = new HtmlImage(microscopeCentricityItem.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        TemplateImage = new HtmlImage(microscopeCentricityItem.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
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
                        MinEcsMicroscopeType = Calibrations.Minima(t => t.Offset.ToOriginLength).Single().LensInformation.LensName,
                        MaxEcsMicroscopeType = Calibrations.Maxima(t => t.Offset.ToOriginLength).Single().LensInformation.LensName,
                        DistanceResult = new HtmlTable([.. Calibrations.Select(t => new { t.IsVerified, LensName = t.LensInformation.LensName, t.CentricityPosition, t.Offset, Distance = t.Offset.ToOriginLength }).Cast<object>()])
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
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation.LensName,
                Cache.AlgorithmTemplateTypeEnum,
                FindFocusPosition = position,
                ImageFileDirectory = detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            var maxMagnificationCacheItem = Cache.MicroscopeCentricityCacheItem.Last();
            foreach (var times in Enumerable.Range(1, repeatCount))
            {
                cancellationToken.ThrowIfCancellationRequested();
                Logger.LogHtmlInformation($"Repeat Time:{times}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, position, Cache.MicroscopeLensInformation, templatePath, detectImageDirectory, HtmlLogUniqueId, Name, string.Empty,
                        out var resultPosition, out var score, out var angle, out var resultImageFilePath, out _) == false) return false;

                var microscopeCentricityItemDto = new MicroscopeCentricityItemDto
                {
                    LensInformation = Cache.MicroscopeLensInformation,
                    CentricityPosition = resultPosition,
                    FilePath = resultImageFilePath,
                    TemplateFilePath = templatePath,
                    TemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templatePath),
                    Offset = resultPosition - (Vector)position,
                    TemplateScore = score,
                    TemplateAngle = angle
                };

                Logger.LogHtmlInformation("Match Template Result", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                {
                    microscopeCentricityItemDto.LensInformation.LensName,
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
                .Where(t => t.LensInformation != itemDto.LensInformation),
            itemDto.Clone()
        ];

        ReviewList =
        [
            .. ReviewList
                .Where(t => t.LensInformation != itemDto.LensInformation)
                .Concat([itemDto.Clone()])
        ];

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
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
        SynchronizationContextProvider.Send(() =>
        {
            AutoCalibrationStepList.Clear();
            AutoCalibrationStepList.AddRange([
                new() { StepName = "loading" },
                .. ApplicationCookie.MicroscopeLensInformationList
                    .Select(t => t)
                    .OrderByDescending(t => t.ObjectiveMagnification)
                    .ThenByDescending(t => t.LensCode)
                    .Select(info => new CalibrationItemStep { StepName = info.LensName }),
                new() { StepName = "Review" }
            ]);
        });
    }

    public override async Task<bool> AutomationActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            GetAutoCalibrationStep();
            await base.AutomationActionAsync(cancellationToken);
            foreach (var (_, stepIndex) in AutoCalibrationStepList.Select((t, index) => (t, index)))
            {
                Func<Task<bool>> autoStepAction = stepIndex switch
                {
                    0 => async () =>
                    {
                        if (await LoadedingAsync(cancellationToken) == false) return false;
                        if (await NextingAsync(cancellationToken) == false) return false;
                        await InvokeCalibrateAsync(() =>
                        {
                            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                            {
                                Cache.AlgorithmTemplateTypeEnum
                            }), HtmlLogUniqueId.LoggingHtml());
                            return true;
                        });
                        CalibrationStepIndex++;

                        return await AutoNextingAsync(cancellationToken).ConfigureAwait(false);
                    }
                    ,
                    var index when index == AutoCalibrationStepList.Count - 1 => async () =>
                    {
                        AutoReviewCalibrationStepIndex = AutoCalibrationStepList.Count - 1;
                        if (await ReviewingAsync(cancellationToken).ConfigureAwait(false) == false) return false;

                        if (await InvokeCalibrateAsync(async () =>
                            {
                                foreach (var itemReview in ReviewList.Select(t => t.Clone()).OrderBy(t => t.LensInformation.LensCode))
                                {
                                    SelectReviewItemDto = itemReview;
                                    if (await VerifyCalibrationAsync(SelectReviewItemDto, cancellationToken) == false) return false;
                                }

                                return true;
                            }) == false) return false;
                        AutoCalibrationStepIndex++;
                        return true;
                    }
                    ,
                    _ => async () =>
                    {
                        if (await AutoActionStepAsync(cancellationToken) == false)
                        {
                            DialogWindowProvider.ShowDialog($"Auto Calibration {Cache.MicroscopeLensInformation.LensName} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                            return false;
                        }

                        return true;
                    }
                };
                if (await autoStepAction().ConfigureAwait(false) == false) return false;
                if (await AutoStepAsync() == false) return false;

                AutoCalibrationProgress = AutoCalibrationStepIndex / (double)AutoCalibrationStepList.Count * 100;
            }

            return true;
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

        Cache.MicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformationList.Single(t => t.LensName == microscopeName);
        SelectMicroscopeCentricityCacheItem = Cache.GetSelectedCacheItem();

        var originReticle = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleModel.Single(t => t.Index is { X: 0, Y: 0 });

        if (CalibrationRecipeService.GetMicroscopeReticleMaskInfo(SelectMicroscopeCentricityCacheItem.WaferMaskTypeEnum, Cache.MicroscopeLensInformation, null, out var maskInfo) == false)
            return false;
        CalibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticle, maskInfo, out var position);
        Cache.SetFindPosition(position);
        Cache.SetTemplateFilePath(maskInfo.RecipeBrightFieldTemplateDto.TemplateFilePath);
        Cache.SetTemplateImageFilePath(maskInfo.RecipeBrightFieldTemplateDto.TemplateImageFilePath);

        return true;
    }

    private async Task<bool> AutoActionStepAsync(CancellationToken cancellationToken)
    {
        if (await Step1CalibrateActionAsync(cancellationToken) == false) return false;
        if (await NextingAsync(cancellationToken) == false) return false;
        Thread.Sleep(2000);
        CalibrationStepIndex++;
        return await AutoNextingAsync(cancellationToken);
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

        foreach (var item in Cache.MicroscopeCentricityCacheItem)
        {
            if (await AutomationRecipeInformationAsync(item.LensInformation.LensName) == false)
                return false;
        }

        var result = false;
        await InvokeVerifyAsync(async () =>
        {
            try
            {
                foreach (var itemReview in ReviewList.Select(t => t.Clone()).OrderBy(t => t.LensInformation.LensCode))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SelectReviewItemDto = itemReview;
                    if (await VerifyCalibrationAsync(SelectReviewItemDto, cancellationToken) == false)
                    {
                        DialogWindowProvider.ShowDialog($"Auto Calibration Review {SelectReviewItemDto.LensInformation.LensName} Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
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