using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Enums.Optics;
using Core.Models.Events;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.Chuck.AlignmentDegreeOffset;
using Core.Models.Models.Chuck.CenterAndTheta;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.NoSQL.DB.Providers.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.Helper;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Chuck;

[IOCAppService(ServiceType = typeof(ChuckAlignmentDegreeOffsetCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckAlignmentDegreeOffsetCalibrationViewModel(
    AlignmentWindowBrightFieldViewModel alignmentWindowBrightFieldViewModel,
    AlignmentWindowDarkFieldViewModel alignmentWindowDarkFieldViewModel) : CalibrationViewModelBase
{
    #region 属性

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Optics Illumination Mode" },
        new() { StepName = "Select Productivity" },
        new() { StepName = "Bright Field P5" },
        new() { StepName = "Dark Field P5" }
    ];

    #region 界面相关

    [ObservableProperty]
    private ChuckAlignmentDegreeOffsetItemDto _calibratingItem = new();

    [ObservableProperty]
    private IReadOnlyList<OpticsIlluminationModeAndProductivityInformationCalibrationStatus> _calibrationStatuses = [];

    #region Review

    [ObservableProperty]
    private ObservableCollection<ChuckAlignmentDegreeOffsetItemDto> _reviews = [];

    [ObservableProperty]
    private ObservableCollection<ChuckAlignmentDegreeOffsetItemDto> _selectReviews = [];

    #endregion Review

    [ObservableProperty]
    private bool _isDarkFieldAlignment;

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private ChuckAlignmentDegreeOffsetCache _cache = new();

    [ObservableProperty]
    private ChuckAlignmentDegreeOffsetItemDto[] _calibrations = [];

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    [ObservableProperty]
    private AlignmentCacheDarkField _alignmentCacheDarkField = new();

    [ObservableProperty]
    private AlignmentCacheDarkField[] _alignmentCacheDarkFields = [];

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

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckCenterAndThetaItemDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<ChuckPrealignerObjDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();
        AlignmentCacheDarkFields = RecipeCacheProvider.GetOrDefaultArray<AlignmentCacheDarkField>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<ChuckAlignmentDegreeOffsetCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<ChuckAlignmentDegreeOffsetItemDto>();

        CalibrationStatuses =
        [
            ..EnumHelper.Enums<OpticsIlluminationModeEnum>()
                .Select(t => new OpticsIlluminationModeAndProductivityInformationCalibrationStatus
                {
                    SelectedItem = t,
                    ProductivityInformationCalibrationStatusList = [.. ProductivityInformationCalibrationStatus.CreateList(ApplicationCookie.GetProductivityInformations(t))]
                })
        ];

        foreach (var calibrationStatus in Calibrations)
        {
            var opticsIlluminationModeEnumStatus = CalibrationStatuses.Single(t => t.SelectedItem == calibrationStatus.OpticsIlluminationMode);
            var status = opticsIlluminationModeEnumStatus
                .ProductivityInformationCalibrationStatusList
                .SingleOrDefault(t => t.SelectedItem == calibrationStatus.ProductivityInformation);
            if (status is not null) status.IsCalibrated = calibrationStatus.IsCalibrated;
        }

        if (Cache.LowMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.LowMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();
        if (Cache.HighMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.HighMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.HighMicroscopeLensInformation.Clone();

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        StageViewModel.SetAbsoluteStageTheta(0);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Reviews =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .OrderBy(t => t.OpticsIlluminationMode)
                .ThenBy(t => t.ProductivityInformation)
        ];

        if (Reviews.All(t => t.IsCalibrated == false))
            return false;

        StageViewModel.SetAbsoluteStageTheta(0);
        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

        return true;
    }

    protected override async Task<bool> CancelingAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);

        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);
        StageViewModel.SetAbsoluteStageTheta(0);
        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 1:
                IsDarkFieldAlignment = false;
                return true;

            case 2:
                IsDarkFieldAlignment = true;
                return true;

            case 3:
                Calibrations =
                [
                    .. Calibrations
                        .Where(t => t.OpticsIlluminationMode != Cache.OpticsIlluminationModeEnum
                                    || t.ProductivityInformation != Cache.ProductivityInformation)
                ];

                CalibrationStatuses.Single(t => t.SelectedItem == Cache.OpticsIlluminationModeEnum)
                    .ProductivityInformationCalibrationStatusList
                    .Single(t => t.SelectedItem == Cache.ProductivityInformation).IsCalibrated = true;

                IsCalibrated = CalibrationStatuses.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

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
            case 3:
                IsDarkFieldAlignment = false;
                return true;

            default:
                return true;
        }
    }

    #endregion 控制校准业务重载

    #region 校准

    [RelayCommand]
    private Task Step0CalibrateActionAsync()
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.OpticsIlluminationModeEnum
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            AlignmentCacheDarkField = AlignmentCacheDarkFields.SingleOrDefault(t =>
                                          t.OpticsIlluminationModeEnum == Cache.OpticsIlluminationModeEnum &&
                                          t.ProductivityInformation == Cache.ProductivityInformation)
                                      ?? new();
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            StageViewModel.SetAbsoluteStageTheta(0);
            CalibratingItem = new ChuckAlignmentDegreeOffsetItemDto
            {
                OpticsIlluminationMode = Cache.OpticsIlluminationModeEnum,
                ProductivityInformation = Cache.ProductivityInformation.Clone(),
                BrightFieldAlignmentDegree = StageViewModel.GetMachineStageTheta()
            };

            if (AlignmentCacheBrightField.IsOk)
            {
                StageViewModel.Alignment(
                    AlignmentCacheBrightField.LowSite1,
                    AlignmentCacheBrightField.LowSite2,
                    AlignmentCacheBrightField.HighSite1,
                    AlignmentCacheBrightField.HighSite2,
                    AlignmentCacheBrightField.LowMag,
                    AlignmentCacheBrightField.HighMag,
                    AlignmentCacheBrightField.AlgorithmWaferTypeEnum);
            }
            else
            {
                var showDialog = WindowManagerService.ShowDialog(alignmentWindowBrightFieldViewModel);
                if (showDialog == false)
                {
                    DialogWindowProvider.ShowDialog("Alignment Setting is Empty", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return false;
                }

                AlignmentCacheBrightField = alignmentWindowBrightFieldViewModel.Cache;
            }

            CalibratingItem.BrightFieldAlignmentDegree = StageViewModel.GetMachineStageTheta();

            var lowSiteTemplateFilePath = $"{TemplateFileDirectory}\\BrightField_{Cache.LowMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            var lowSiteTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(lowSiteTemplateFilePath);
            BitmapSourceHelper.Save(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(AlignmentCacheBrightField.LowSite1.Template.Thumb), lowSiteTemplateImageFilePath);

            var highSiteTemplateFilePath = $"{TemplateFileDirectory}\\BrightField_{Cache.HighMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            var highSiteTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(highSiteTemplateFilePath);
            BitmapSourceHelper.Save(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(AlignmentCacheBrightField.HighSite1.Template.Thumb), highSiteTemplateImageFilePath);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.LowMicroscopeLensInformation,
                Cache.HighMicroscopeLensInformation,
                LowSite1 = AlignmentCacheBrightField.LowSite1.Location,
                LowSite2 = AlignmentCacheBrightField.LowSite2.Location,
                HighSite1 = AlignmentCacheBrightField.HighSite1.Location,
                HighSite2 = AlignmentCacheBrightField.HighSite2.Location,
                CalibratingItem.BrightFieldAlignmentDegree,
                HtmlTab = new HtmlTab(new
                {
                    LowSiteTemplate = new HtmlImage(lowSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                    HighSiteTemplate = new HtmlImage(highSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                })
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step3CalibrateActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await InvokeCalibrateAsync(() =>
            {
                var result = false;
                StageViewModel.SetAbsoluteStageTheta(0);
                if (AlignmentCacheDarkField.IsOk)
                {
                    DialogWindowProvider.TryShowDialog("Alignment cache is already exist,do you want reset? ", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                    if (dialogResult == DialogResultEnum.Yes)
                        if (DarkFieldAlignmentCacheReset() == false)
                            return false;
                }
                else if (DarkFieldAlignmentCacheReset() == false) return false;

                StageViewModel.AlignmentDarkField(
                    AlignmentCacheDarkField.LowSite1,
                    AlignmentCacheDarkField.LowSite2,
                    AlignmentCacheDarkField.HighSite1,
                    AlignmentCacheDarkField.HighSite2,
                    Cache.ProductivityInformation,
                    AlignmentCacheDarkField.LowMag,
                    AlignmentCacheDarkField.AlgorithmWaferTypeEnum,
                    opticsIlluminationModeEnum: Cache.OpticsIlluminationModeEnum);

                CalibratingItem.DarkFieldAlignmentDegree = StageViewModel.GetMachineStageTheta();
                result = Math.Abs(CalibratingItem.DegreeOffset) < Cache.TeachingThreshold;

                var lowSiteTemplateFilePath = $"{TemplateFileDirectory}\\DarkField_{Cache.LowMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
                var lowSiteTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(lowSiteTemplateFilePath);
                BitmapSourceHelper.Save(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(AlignmentCacheDarkField.LowSite1.Template.Thumb), lowSiteTemplateImageFilePath);

                var highSiteTemplateFilePath = $"{TemplateFileDirectory}\\DarkField_{Cache.HighMicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
                var highSiteTemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(highSiteTemplateFilePath);
                BitmapSourceHelper.Save(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(AlignmentCacheDarkField.HighSite1.Template.Thumb), highSiteTemplateImageFilePath);

                Logger.LogHtmlInformation($"Calibration {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.LowMicroscopeLensInformation,
                    Cache.HighMicroscopeLensInformation,
                    LowSite1 = AlignmentCacheDarkField.LowSite1.Location,
                    LowSite2 = AlignmentCacheDarkField.LowSite2.Location,
                    HighSite1 = AlignmentCacheDarkField.HighSite1.Location,
                    HighSite2 = AlignmentCacheDarkField.HighSite2.Location,
                    CalibratingItem.BrightFieldAlignmentDegree,
                    CalibratingItem.DarkFieldAlignmentDegree,
                    CalibratingItem.DegreeOffset,
                    HtmlTab = new HtmlTab(new
                    {
                        LowSiteTemplate = new HtmlImage(lowSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)]),
                        HighSiteTemplate = new HtmlImage(highSiteTemplateImageFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                    })
                }), HtmlLogUniqueId.LoggingHtml());

                if (result == false)
                    DialogWindowProvider.ShowDialog("Degree offset result is out of threshold!", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                CalibratingItem.IsCalibrated = true;
                Guard.IsTrue(Save(CalibratingItem, cancellationToken));

                return result;
            });
        }
        finally
        {
            Messenger.Send(ToggleToolsEventFactory.RefreshToolsWindowEnableStatus(true));
        }

        bool DarkFieldAlignmentCacheReset()
        {
            alignmentWindowDarkFieldViewModel.Cache.OpticsIlluminationModeEnum = Cache.OpticsIlluminationModeEnum;
            alignmentWindowDarkFieldViewModel.Cache.ProductivityInformation = Cache.ProductivityInformation.Clone();

            Messenger.Send(ToggleToolsEventFactory.RefreshToolsWindowEnableStatus(false));

            var showDialog = WindowManagerService.ShowDialog(alignmentWindowDarkFieldViewModel);
            if (showDialog == false)
            {
                DialogWindowProvider.ShowDialog("Alignment Setting is Empty", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            AlignmentCacheDarkField = alignmentWindowDarkFieldViewModel.Cache;

            return true;
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(() =>
        {
            var result = true;

            if (SelectReviews.Count == 0)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            foreach (var selectReview in SelectReviews)
            {
                StageViewModel.SetAbsoluteStageTheta(0);
                if (VerifyCalibration(selectReview, cancellationToken) == false) result = false;
            }

            return result;
        }).ConfigureAwait(false);
    }

    private bool VerifyCalibration(ChuckAlignmentDegreeOffsetItemDto reviewDto, CancellationToken cancellationToken)
    {
        var chuckAlignmentDegreeOffsetItemDto = reviewDto.Clone();

        Cache.OpticsIlluminationModeEnum = reviewDto.OpticsIlluminationMode;
        Cache.ProductivityInformation = reviewDto.ProductivityInformation;

        AlignmentCacheDarkField = GuardUtils.IsNotNullAndReturn(AlignmentCacheDarkFields.SingleOrDefault(t =>
            t.OpticsIlluminationModeEnum == Cache.OpticsIlluminationModeEnum &&
            t.ProductivityInformation == Cache.ProductivityInformation));

        StageViewModel.Alignment(
            AlignmentCacheBrightField.LowSite1,
            AlignmentCacheBrightField.LowSite2,
            AlignmentCacheBrightField.HighSite1,
            AlignmentCacheBrightField.HighSite2,
            AlignmentCacheBrightField.LowMag,
            AlignmentCacheBrightField.HighMag,
            AlignmentCacheBrightField.AlgorithmWaferTypeEnum);
        chuckAlignmentDegreeOffsetItemDto.BrightFieldAlignmentDegree = StageViewModel.GetMachineStageTheta();

        var darkFieldAlignmentOriginDegree = chuckAlignmentDegreeOffsetItemDto.BrightFieldAlignmentDegree + chuckAlignmentDegreeOffsetItemDto.DegreeOffset;
        StageViewModel.SetAbsoluteStageTheta(darkFieldAlignmentOriginDegree);

        StageViewModel.AlignmentDarkField(
            AlignmentCacheDarkField.LowSite1,
            AlignmentCacheDarkField.LowSite2,
            AlignmentCacheDarkField.HighSite1,
            AlignmentCacheDarkField.HighSite2,
            AlignmentCacheDarkField.ProductivityInformation,
            AlignmentCacheDarkField.LowMag,
            AlignmentCacheDarkField.AlgorithmWaferTypeEnum,
            opticsIlluminationModeEnum: chuckAlignmentDegreeOffsetItemDto.OpticsIlluminationMode);
        chuckAlignmentDegreeOffsetItemDto.DarkFieldAlignmentDegree = StageViewModel.GetMachineStageTheta();

        var darkFieldAlignmentVerifyResult = chuckAlignmentDegreeOffsetItemDto.DarkFieldAlignmentDegree - darkFieldAlignmentOriginDegree;
        reviewDto.DarkFieldAlignmentVerifyResult = darkFieldAlignmentVerifyResult;

        var result = Math.Abs(darkFieldAlignmentVerifyResult) < Cache.VerifyThreshold;

        Logger.LogHtmlInformation($"{Cache.OpticsIlluminationModeEnum.ToDescriptionOrString()}-{Cache.ProductivityInformation}-{(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            Cache.VerifyThreshold,
            CalibrationBrightFieldAlignmentDegree = reviewDto.BrightFieldAlignmentDegree,
            CalibrationDarkFieldAlignmentDegree = reviewDto.DarkFieldAlignmentDegree,
            CalibrationBfToDfDegreeOffset = reviewDto.DegreeOffset,
            VerifyBrightFieldAlignmentDegree = chuckAlignmentDegreeOffsetItemDto.BrightFieldAlignmentDegree,
            VerifyDarkFieldAlignmentDegree = chuckAlignmentDegreeOffsetItemDto.DarkFieldAlignmentDegree,
            VerifyBfToDfDegreeOffset = chuckAlignmentDegreeOffsetItemDto.DegreeOffset,
            DarkFieldAlignmentVerifyResult = darkFieldAlignmentVerifyResult
        }), HtmlLogUniqueId.LoggingHtml());

        reviewDto.IsVerified = result;
        if (Save(reviewDto, cancellationToken) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Save Failed!"), HtmlLogUniqueId.LoggingHtml());
            reviewDto.IsVerified = false;
            return false;
        }

        if (EnableDependedCalibrationItems(cancellationToken) == false)
        {
            Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Enable Depended Calibration Items Failed!"), HtmlLogUniqueId.LoggingHtml());
            return false;
        }

        return result;
    }

    private bool Save(ChuckAlignmentDegreeOffsetItemDto itemDto, CancellationToken cancellationToken, bool isSave = true) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibrations =
        [
            .. Calibrations
                .Where(t => (
                    t.ProductivityInformation == itemDto.ProductivityInformation
                    && t.OpticsIlluminationMode == itemDto.OpticsIlluminationMode) == false),
            itemDto.Clone()
        ];
        if (isSave == false) return;

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    }) && EnableDependedCalibrationItems(cancellationToken);

    protected override bool EnableDependedCalibrationItems(CancellationToken cancellationToken)
    {
        return true;
    }

    #endregion 校准
}