using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.PixelSize;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using System.Runtime.CompilerServices;

namespace CugaCalibration.ViewModels.Microscope;

[IOCAppService(ServiceType = typeof(MicroscopeCentricityViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeCentricityViewModel : CalibrationViewModelBase<MicroscopeCentricityCache>
{
    #region 属性

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } = new List<CalibrationItemStep>();

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial MicroscopeCentricityDTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCentricityDTOItem? SelectedItem { get; set; }

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    public partial IReadOnlyList<MicroscopeCentricityDTO> Reviews { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeCentricityDTO? SelectedReviewItem { get; set; }

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial MicroscopeCentricityCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial MicroscopeCentricityDTO[] Calibrations { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopePixelSizeDTO[] MicroscopePixelSizes { get; set; } = [];

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
        MicroscopePixelSizes = ApplicationCookieService.GetCalibrations<MicroscopePixelSizeDTO>(cancellationToken);

        SynchronizationContextProvider.Post(() =>
        {
            Guard.IsAssignableToTypeAndReturn<List<CalibrationItemStep>>(CalibrationSteps).AddRange([
                new CalibrationItemStep { StepName = "Select a location" },
                .. ApplicationCookie.MicroscopeLensInformations
                    .Select(t => t)
                    .OrderByDescending(t => t.ObjectiveMagnification)
                    .ThenByDescending(t => t.LensCode)
                    .Select(info => new CalibrationItemStep { StepName = info.LensName })
            ]);
            OnPropertyChanged(nameof(CalibrationSteps));
        });

        Cache = ApplicationCookieService.GetCache<MicroscopeCentricityCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<MicroscopeCentricityDTO>(cancellationToken);

        Cache.MicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformations
            .OrderBy(t => t.ObjectiveMagnification)
            .ThenBy(t => t.LensCode)
            .First();

        UpdateEntryStatus(Unsafe.As<CalibrationDTOBase[]>(Calibrations), cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);

        CalibratingItem = new();

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Reviews =
        [
            .. Calibrations
                .Where(t => t.IsCalibrated)
                .Select(t => t.Clone())
                .OrderBy(t => t.MicroscopeLensInformation.ObjectiveMagnification)
                .ThenBy(t => t.MicroscopeLensInformation.LensCode)
        ];

        return Reviews.Count > 0;
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        Cache.MicroscopeLensInformation = CalibrationStepIndex switch
        {
            > 1 => ApplicationCookie.MicroscopeLensInformations
                .Select(t => t)
                .OrderByDescending(t => t.ObjectiveMagnification)
                .ThenByDescending(t => t.LensCode)
                .ElementAt(CalibrationStepIndex - 2),
            1 => ApplicationCookie.MicroscopeLensInformations[0],
            _ => throw new ArgumentOutOfRangeException()
        };

        await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
        StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.FindPosition, Cache.CalChipSiteModelEnum);
        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        if (CalibrationStepIndex < CalibrationSteps.Count - 1)
        {
            Cache.MicroscopeLensInformation = ApplicationCookie.MicroscopeLensInformations
                .Select(t => t)
                .OrderByDescending(t => t.ObjectiveMagnification)
                .ThenByDescending(t => t.LensCode)
                .ElementAt(CalibrationStepIndex);

            await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        CalibratingItem.MicroscopeLensInformation = Cache.MicroscopeLensInformation;
        CalibratingItem.Items = [];
        CalibratingItem.IsCalibrated = false;

        return true;
    }

    protected override async Task<bool> CancelingAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);

        SynchronizationContextProvider.Send(() => Guard.IsAssignableToTypeAndReturn<List<CalibrationItemStep>>(CalibrationSteps).Clear());

        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

        return true;
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step0Async(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(() =>
        {
            Cache.Item.FindPosition = StageViewModel.GetBrightFieldStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation,
                Cache.AlgorithmTemplateTypeEnum,
                Cache.CalChipSiteModelEnum,
                Cache.Item.WaferMaskTypeEnum,
                Cache.Item.FindPosition
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(() =>
        {
            Cache.Item.FindPosition = StageViewModel.GetBrightFieldStagePosition();
            Cache.Item.TemplateFilePath = $"{TemplateFileDirectory}\\{Cache.MicroscopeLensInformation.LensName}_{Guid.NewGuid()}";

            var generateTemplateLow1 = ReviewViewModel.TryGenerateTemplate(Cache.AlgorithmTemplateTypeEnum, Cache.Item.TemplateFilePath, Cache.AlgorithmTemplateSizeEnum, HtmlLogUniqueId);

            if (generateTemplateLow1 == false) DialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            else Cache.Item.TemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(Cache.Item.TemplateFilePath);

            var (isSuccess, errorMessage) = Cache.Verify();
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            CalibratingItem.Items = [];
            CalibratingItem.IsCalibrated = false;

            StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.FindPosition);
            if (MatchTemplate(cancellationToken) == false) return false;

            var averageX = CalibratingItem.Items.Average(t => t.CentricityPosition.X);
            var averageY = CalibratingItem.Items.Average(t => t.CentricityPosition.Y);
            var averageCentricityPosition = new Point(averageX, averageY);
            StageViewModel.SetBrightFieldAbsoluteStageXy(averageCentricityPosition, Cache.CalChipSiteModelEnum);

            SelectedItem = CalibratingItem.Items.OrderBy(t => (t.CentricityPosition - (Vector)averageCentricityPosition).ToOriginLength).First();
            CalibratingItem.Result = SelectedItem.Clone();
            CalibratingItem.Result.CentricityPosition = averageCentricityPosition;

            var maxMagnification = Cache.Items.OrderBy(t => t.Value.LensInformation.ObjectiveMagnification).Last().Value.LensInformation;

            CalibratingItem.Result.Offset = CalibratingItem.MicroscopeLensInformation != maxMagnification
                ? CalibratingItem.Result.CentricityPosition - (Vector)Calibrations.Single(t => t.MicroscopeLensInformation == maxMagnification).Result.CentricityPosition
                : Point.Origin;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                CalibratingItem.MicroscopeLensInformation,
                CalibratingItem.Result.CentricityPosition,
                CalibratingItem.Result.Offset,
                HtmlTab = new HtmlTab(new
                {
                    ResultImage = new HtmlImage(CalibratingItem.Result.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    TemplateImage = new HtmlImage(Cache.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                }),
                Plot = new HtmlContainer(CalibratingItem.PlotDataSource.GetAllHtmlPlot2DLinesCharts())
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.IsCalibrated = true;
            Guard.IsTrue(Save([CalibratingItem.Clone()], cancellationToken));

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> VerifyAsync(CancellationToken cancellationToken)
    {
        return await InvokeVerifyAsync(async () =>
        {
            if (SelectedReviewItem is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            Cache.MicroscopeLensInformation = SelectedReviewItem.MicroscopeLensInformation;

            Logger.LogHtmlInformation($"{Cache.MicroscopeLensInformation.LensName}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            SelectedReviewItem.IsVerified = false;

            var maxMagnificationLens = Cache.Items.OrderBy(t => t.Value.LensInformation.ObjectiveMagnification).Last().Value.LensInformation;
            var centricityItemMaxDto = Reviews.SingleOrDefault(t => t.MicroscopeLensInformation == maxMagnificationLens);

            if (centricityItemMaxDto is null)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, new HtmlComment("Error: Please calibrate max magnification first!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            StageViewModel.SetBrightFieldAbsoluteStageXy(centricityItemMaxDto.Result.CentricityPosition, Cache.CalChipSiteModelEnum);

            await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
            StageViewModel.MoveRelativeStageXy(SelectedReviewItem.Result.Offset);

            var originalPosition = StageViewModel.GetBrightFieldStagePosition();

            if (MatchTemplate(cancellationToken, 1) == false) return false;

            var microscopeCentricityItem = CalibratingItem.Items[0];
            var newPosition = microscopeCentricityItem.CentricityPosition;
            var offset = microscopeCentricityItem.Offset;
            var result = offset.ToOriginLength < Cache.Threshold.ToOriginLength;
            Cache.VerifyResultPosition = newPosition;
            Cache.VerifyResultError = offset;

            Logger.LogHtmlInformation($"Verify {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                Cache.Threshold,
                Cache.MicroscopeLensInformation,
                microscopeCentricityItem.CentricityPosition,
                microscopeCentricityItem.Offset,
                HtmlTab = new HtmlTab(new
                {
                    ResultImage = new HtmlImage(microscopeCentricityItem.FilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    TemplateImage = new HtmlImage(Cache.Item.TemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            SelectedReviewItem.IsVerified = result;

            Guard.IsTrue(Save([SelectedReviewItem], cancellationToken));
            DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, New Offset: ({newPosition}) Old Offset: ({originalPosition}) Error: ({offset})", DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            if (result == false) return false;

            // 同心验证
            var verifiedDtoList = Calibrations.Where(t => t.IsOk).ToList();
            if (verifiedDtoList.Count >= 2)
            {
                var concentricOffset = verifiedDtoList.Max(t => t.Result.Offset.ToOriginLength) - verifiedDtoList.Min(t => t.Result.Offset.ToOriginLength);
                result = Math.Abs(concentricOffset) <= Cache.ConcentricThreshold;
                if (result == false)
                {
                    DialogWindowProvider.ShowDialog($"Concentric {(result ? "OK" : "Failed")}, Offset: {concentricOffset}", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);
                    SelectedReviewItem.IsVerified = false;
                    Guard.IsTrue(Save([SelectedReviewItem], cancellationToken));
                }

                Logger.LogHtmlInformation($"Concentric {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    Cache.ConcentricThreshold,
                    concentricOffset,
                    MinEcsMicroscopeType = Calibrations.Minima(t => t.Result.Offset.ToOriginLength).Single().MicroscopeLensInformation.LensName,
                    MaxEcsMicroscopeType = Calibrations.Maxima(t => t.Result.Offset.ToOriginLength).Single().MicroscopeLensInformation.LensName,
                    DistanceResult = new HtmlTable([.. Calibrations.Select(t => new { t.IsVerified, t.MicroscopeLensInformation.LensName, t.Result.CentricityPosition, t.Result.Offset, Distance = t.Result.Offset.ToOriginLength })])
                }), HtmlLogUniqueId.LoggingHtml());
            }

            return result;
        }).ConfigureAwait(false);
    }

    private bool MatchTemplate(CancellationToken cancellationToken, int repeatCount = 5)
    {
        var detectImageDirectory = ImageFileDirectory;
        var originalPosition = StageViewModel.GetBrightFieldStagePosition();

        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
        {
            Cache.MicroscopeLensInformation.LensName,
            Cache.AlgorithmTemplateTypeEnum,
            originalPosition,
            detectImageDirectory
        }), HtmlLogUniqueId.LoggingHtml());

        foreach (var times in Enumerable.Range(1, repeatCount))
        {
            cancellationToken.ThrowIfCancellationRequested();
            Logger.LogHtmlInformation($"Repeat Time:{times}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

            if (ReviewViewModel.TryGetMatchPosition(
                    Cache.AlgorithmTemplateTypeEnum,
                    MicroscopePixelSizes,
                    originalPosition,
                    Cache.MicroscopeLensInformation,
                    Cache.Item.TemplateFilePath,
                    detectImageDirectory,
                    HtmlLogUniqueId,
                    Name,
                    string.Empty,
                    out var resultPosition,
                    out var score,
                    out var angle,
                    out var resultImageFilePath,
                    out _,
                    Cache.CalChipSiteModelEnum) == false) return false;

            var microscopeCentricityItem = new MicroscopeCentricityDTOItem
            {
                Index = times - 1,
                CentricityPosition = resultPosition,
                FilePath = resultImageFilePath,
                Offset = resultPosition - (Vector)originalPosition
            };

            Logger.LogHtmlInformation("Match Template Result", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
            {
                Cache.MicroscopeLensInformation,
                OriginalPosition = originalPosition,
                NewPosition = microscopeCentricityItem.CentricityPosition,
                microscopeCentricityItem.Offset
            }), HtmlLogUniqueId.LoggingHtml());

            StageViewModel.SetBrightFieldAbsoluteStageXy(originalPosition, Cache.CalChipSiteModelEnum);

            CalibratingItem.Items = [.. CalibratingItem.Items, microscopeCentricityItem];
        }

        return true;
    }

    private bool Save(IReadOnlyList<MicroscopeCentricityDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                dto.Clone(),
                .. Calibrations.Where(t => t.MicroscopeLensInformation != dto.MicroscopeLensInformation)
            ];
        }

        update(Cache);

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<MicroscopeCentricityDTO[]>(calibrations);
        var status = Entry.Status;

        Calibrations =
        [
            .. temps
                .Where(t => ApplicationCookie.MicroscopeLensInformations.Contains(t.MicroscopeLensInformation))
                .DistinctBy(t => t.MicroscopeLensInformation)
        ];

        status.TotalCalibrationCount = ApplicationCookie.MicroscopeLensInformations.Count;
        status.CalibratedCount = Calibrations.Count(t => t.IsCalibrated);
        status.VerifiedCount = Calibrations.Count(t => t.IsVerified);
        status.Details =
        [
            .. ApplicationCookie.MicroscopeLensInformations.Select(microscopeLensInformation =>
            {
                var item = Calibrations.SingleOrDefault(t => t.MicroscopeLensInformation == microscopeLensInformation);

                return new CalibrationViewModelStatus.Detail(
                    microscopeLensInformation.ToString(),
                    item?.IsCalibrated,
                    item?.IsVerified);
            })
        ];
    }

    #endregion 校准
}