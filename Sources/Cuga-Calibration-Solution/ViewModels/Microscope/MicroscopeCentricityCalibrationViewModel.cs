using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.PixelSize;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace CugaCalibration.ViewModels.Microscope;

[IOCAppService(ServiceType = typeof(MicroscopeCentricityCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeCentricityCalibrationViewModel : CalibrationViewModelBase<MicroscopeCentricityCache>
{
    #region 属性

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } = new List<CalibrationItemStep>();

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial ObservableCollection<MicroscopeCentricityItemDto> MicroscopeCentricityItemDtoList { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeCentricityItemDto? SelectMicroscopeCentricityItemDto { get; set; }

    [ObservableProperty]
    public partial MicroscopeCentricityItemDto? ResultMicroscopeCentricityItemDto { get; set; }

    [ObservableProperty]
    public partial MicroscopeCentricityItemDto MicroscopeCentricityItemDto150X { get; set; } = new();

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    public partial ObservableCollection<MicroscopeCentricityItemDto> ReviewList { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeCentricityItemDto? SelectReviewItemDto { get; set; }

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial MicroscopeCentricityCache Cache { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCentricityCacheItem SelectMicroscopeCentricityCacheItem { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial MicroscopeCentricityItemDto[] Calibrations { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopePixelSizeItemDto[] MicroscopePixelSizeItems { get; set; } = [];

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);


        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
        MicroscopePixelSizeItems = ApplicationCookieService.GetCalibrations<MicroscopePixelSizeItemDto>(cancellationToken);

        SynchronizationContextProvider.Send(() =>
        {
            Guard.IsAssignableToTypeAndReturn<List<CalibrationItemStep>>(CalibrationSteps).AddRange([
                new CalibrationItemStep { StepName = "Select a location" },
                .. ApplicationCookie.MicroscopeLensInformations
                    .Select(t => t)
                    .OrderByDescending(t => t.ObjectiveMagnification)
                    .ThenByDescending(t => t.LensCode)
                    .Select(info => new CalibrationItemStep { StepName = info.LensName })
            ]);
        });

        Cache = ApplicationCookieService.GetCache<MicroscopeCentricityCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<MicroscopeCentricityItemDto>(cancellationToken);

        if (Cache.MicroscopeLensInformation == MicroscopeLensInformation.Default)
            Cache.MicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();

        UpdateEntryStatus(Unsafe.As<CalibrationDTOBase[]>(Calibrations), cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
        SelectMicroscopeCentricityCacheItem = Cache.CurrentCalibrationCacheItem;

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
            > 1 => ApplicationCookie.MicroscopeLensInformations
                .Select(t => t)
                .OrderByDescending(t => t.ObjectiveMagnification)
                .ThenByDescending(t => t.LensCode)
                .ElementAt(CalibrationStepIndex - 1),
            1 => ApplicationCookie.MicroscopeLensInformations[0],
            _ => throw new ArgumentOutOfRangeException()
        };

        SelectMicroscopeCentricityCacheItem = Cache.CurrentCalibrationCacheItem;
        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
        StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(SelectMicroscopeCentricityCacheItem.FindPosition, Cache.CalChipSiteModelEnum);
        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        var result = true;
        if (CalibrationStepIndex != 0)
            if (SaveResult() == false)
                result = false;

        if (CalibrationStepIndex == CalibrationSteps.Count - 1)
        {
            return result;
        }

        Cache.MicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformations
            .Select(t => t)
            .OrderByDescending(t => t.ObjectiveMagnification)
            .ThenByDescending(t => t.LensCode)
            .ElementAt(CalibrationStepIndex);

        SelectMicroscopeCentricityCacheItem = Cache.CurrentCalibrationCacheItem;
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

    protected override async Task<bool> CancelingAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);

        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

        return true;
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
                Cache.MicroscopeLensInformation.LensName,
                Cache.AlgorithmTemplateTypeEnum,
                Cache.CalChipSiteModelEnum,
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
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(averageCentricityPosition, Cache.CalChipSiteModelEnum);

                SelectMicroscopeCentricityItemDto = MicroscopeCentricityItemDtoList.OrderBy(t => (t.CentricityPosition - (Vector)averageCentricityPosition).ToOriginLength).First();
                ResultMicroscopeCentricityItemDto = SelectMicroscopeCentricityItemDto.Clone();
                ResultMicroscopeCentricityItemDto.CentricityPosition = averageCentricityPosition;

                var maxMagnification = Cache.MicroscopeCentricityCacheItemDic.OrderBy(t => t.Value.LensInformation.ObjectiveMagnification).Last().Value.LensInformation;

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
        if (SelectReviewItemDto is null)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        return await InvokeVerifyAsync(async () => await VerifyCalibrationAsync(SelectReviewItemDto, cancellationToken)).ConfigureAwait(false);
    }

    private async Task<bool> VerifyCalibrationAsync(MicroscopeCentricityItemDto? selectReviewItemDto, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            if (selectReviewItemDto is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            Cache.MicroscopeLensInformation = selectReviewItemDto.LensInformation;
            SelectMicroscopeCentricityCacheItem = Cache.CurrentCalibrationCacheItem;

            Logger.LogHtmlInformation($"{Cache.MicroscopeLensInformation.LensName}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            ClearCalibrationTemp();
            var detectImageDirectory = ImageFileDirectory;

            selectReviewItemDto.IsVerified = false;

            var centricityItemMaxDto = ReviewList.Single(t => t.LensInformation == Cache.MicroscopeCentricityCacheItemDic.OrderBy(t => t.Value.LensInformation.ObjectiveMagnification).Last().Value.LensInformation);


            if (centricityItemMaxDto is null)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Please calibrate max magnification first!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(centricityItemMaxDto.CentricityPosition, Cache.CalChipSiteModelEnum);


            var templateFilePath = SelectMicroscopeCentricityCacheItem.TemplateFilePath;
            MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
            StageViewModel.MoveRelativeStageXy(selectReviewItemDto.Offset);

            var oldPosition = StageViewModel.GetBrightFieldStagePosition();

            if (MatchTemplate(oldPosition, templateFilePath, detectImageDirectory, cancellationToken, 1) == false) return false;

            var microscopeCentricityItem = MicroscopeCentricityItemDtoList[0];
            var newPosition = microscopeCentricityItem.CentricityPosition;
            var error = newPosition - (Vector)oldPosition;
            var result = error.ToOriginLength < Cache.Threshold.ToOriginLength;
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
                return false;
            }

            DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, New Offset: ({newPosition}) Old Offset: ({oldPosition}) Error: ({error})", DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);


            if (result == false) return false;

            // 同心验证
            var varifiedDtoList = Calibrations.Where(t => t.IsOk).ToList();
            if (varifiedDtoList.Count >= 2)
            {
                var concentricOffset = varifiedDtoList.Max(t => t.Offset.ToOriginLength) - varifiedDtoList.Min(t => t.Offset.ToOriginLength);
                result = Math.Abs(concentricOffset) <= Cache.ConcentricThreshold;
                if (result == false)
                {
                    DialogWindowProvider.ShowDialog($"Concentric {(result ? "OK" : "Failed")}, Offset: {concentricOffset}", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);
                    selectReviewItemDto.IsVerified = false;
                    if (Save(selectReviewItemDto, cancellationToken) == false)
                    {
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
                        return false;
                    }
                }

                Logger.LogHtmlInformation($"Concentric {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    Cache.ConcentricThreshold,
                    concentricOffset,
                    MinEcsMicroscopeType = Calibrations.Minima(t => t.Offset.ToOriginLength).Single().LensInformation.LensName,
                    MaxEcsMicroscopeType = Calibrations.Maxima(t => t.Offset.ToOriginLength).Single().LensInformation.LensName,
                    DistanceResult = new HtmlTable([.. Calibrations.Select(t => new { t.IsVerified, LensName = t.LensInformation.LensName, t.CentricityPosition, t.Offset, Distance = t.Offset.ToOriginLength })])
                }), HtmlLogUniqueId.LoggingHtml());
            }

            return result;
        }, cancellationToken).ConfigureAwait(false);
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

            foreach (var times in Enumerable.Range(1, repeatCount))
            {
                cancellationToken.ThrowIfCancellationRequested();
                Logger.LogHtmlInformation($"Repeat Time:{times}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                if (ReviewViewModel.TryGetMatchPosition(Cache.AlgorithmTemplateTypeEnum, MicroscopePixelSizeItems, position, Cache.MicroscopeLensInformation, templatePath, detectImageDirectory, HtmlLogUniqueId, Name, string.Empty,
                        out var resultPosition, out var score, out var angle, out var resultImageFilePath, out _, Cache.CalChipSiteModelEnum) == false) return false;

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

                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(position, Cache.CalChipSiteModelEnum);

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
            itemDto,
            .. Calibrations
                .Where(t => t.LensInformation != itemDto.LensInformation)
        ];

        ReviewList =
        [
            .. ReviewList
                .Where(t => t.LensInformation != itemDto.LensInformation)
                .Concat([itemDto.Clone()])
        ];

        foreach (var microscopeFocusCacheItem in Cache.MicroscopeCentricityCacheItemDic)
        {
            if (ApplicationCookie.MicroscopeLensInformations.SingleOrDefault(t => t.LensName == microscopeFocusCacheItem.Key) is null)
                Cache.MicroscopeCentricityCacheItemDic.TryRemove(microscopeFocusCacheItem.Key, out _);
        }

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<MicroscopeCentricityItemDto[]>(calibrations);
        var status = Entry.Status;

        Calibrations =
        [
            .. temps
                .Where(t => ApplicationCookie.MicroscopeLensInformations.Contains(t.LensInformation))
                .DistinctBy(t => t.LensInformation)
        ];

        status.TotalCalibrationCount = ApplicationCookie.MicroscopeLensInformations.Count;
        status.CalibratedCount = Calibrations.Count(t => t.IsCalibrated);
        status.VerifiedCount = Calibrations.Count(t => t.IsVerified);
        status.Details =
        [
            .. ApplicationCookie.MicroscopeLensInformations.Select(microscopeLensInformation =>
            {
                var item = Calibrations.SingleOrDefault(t => t.LensInformation == microscopeLensInformation);

                return new CalibrationViewModelStatus.Detail(
                    microscopeLensInformation.ToString(),
                    item?.IsCalibrated,
                    item?.IsVerified);
            })
        ];
    }

    private void ClearCalibrationTemp()
    {
        SynchronizationContextProvider.Send(MicroscopeCentricityItemDtoList.Clear);
        SelectMicroscopeCentricityItemDto = null;
        ResultMicroscopeCentricityItemDto = null;
    }

    #endregion 校准
}