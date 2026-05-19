using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Enums.Stage;
using Core.Models.Events;
using Core.Models.Models;
using Core.Models.Models.Chuck.AlignmentDegreeOffset;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.PixelSize;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace CugaCalibration.ViewModels.Chuck;

[IOCAppService(ServiceType = typeof(ChuckAlignmentDegreeOffsetCalibrationViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ChuckAlignmentDegreeOffsetCalibrationViewModel : CalibrationViewModelBase
{
    #region 属性

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Bright Field P5" },
        new() { StepName = "Dark Field P5" }
    ];

    #region 界面相关

    [ObservableProperty]
    private ChuckAlignmentDegreeOffsetItemDto _calibratingItem = new();

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationStatus> _calibratingStatuses = [];

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

    [RecipeCache]
    [ObservableProperty]
    private ChuckAlignmentDegreeOffsetCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private ChuckAlignmentDegreeOffsetItemDto[] _calibrations = [];

    [ObservableProperty]
    private MicroscopePixelSizeItemDto[] _microscopePixelSizeItems = [];

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = HostApplication.GetRequiredService<AlignmentUserControlViewModel>();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务重载

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        MicroscopePixelSizeItems = ApplicationCookieService.GetCalibrations<MicroscopePixelSizeItemDto>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<ChuckAlignmentDegreeOffsetCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<ChuckAlignmentDegreeOffsetItemDto>(cancellationToken);

        if (Cache.LowMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.LowMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.LowMicroscopeLensInformation.Clone();
        if (Cache.HighMicroscopeLensInformation == MicroscopeLensInformation.Default) Cache.HighMicroscopeLensInformation = CalibrationSetting.SettingCommonParam.HighMicroscopeLensInformation.Clone();

        StageViewModel.SetAbsoluteStageTheta(0d);

        UpdateEntryStatus(Unsafe.As<CalibrationDTOBase[]>(Calibrations), cancellationToken);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Reviews =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .OrderBy(t => t.ProductivityInformation)
        ];

        if (Reviews.All(t => t.IsCalibrated == false))
            return false;

        StageViewModel.SetAbsoluteStageTheta(0d);
        MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.LowMicroscopeLensInformation);
        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);

        return true;
    }

    protected override async Task<bool> CancelingAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);

        StageViewModel.SetBrightFieldAbsoluteStageXy(Point.Origin);
        StageViewModel.SetAbsoluteStageTheta(0d);
        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                IsDarkFieldAlignment = false;
                return true;

            case 1:
                IsDarkFieldAlignment = true;
                return true;

            case 2:
                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

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
                IsDarkFieldAlignment = false;
                return true;

            default:
                return true;
        }
    }

    #endregion 控制校准业务重载

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());
            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            StageViewModel.SetAbsoluteStageTheta(0d);
            CalibratingItem = new ChuckAlignmentDegreeOffsetItemDto
            {
                ProductivityInformation = Cache.ProductivityInformation.Clone(),
                BrightFieldAlignmentDegree = StageViewModel.GetMachineStageTheta()
            };

            AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
            AlignmentUserControlViewModel.IsDarkFieldAlignment = false;

            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            var alignmentResult = AlignmentUserControlViewModel.AlignmentResult;

            CalibratingItem.BrightFieldAlignmentDegree = StageViewModel.GetMachineStageTheta();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.LowMicroscopeLensInformation,
                Cache.HighMicroscopeLensInformation,
                CalibratingItem.BrightFieldAlignmentDegree,
                AlignmentResult = new HtmlQuote(alignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2CalibrateActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await InvokeCalibrateAsync(async () =>
            {
                var result = false;
                StageViewModel.SetAbsoluteStageTheta(0d);

                AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;
                AlignmentUserControlViewModel.IsDarkFieldAlignment = true;

                await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

                var alignmentResult = AlignmentUserControlViewModel.AlignmentResult;

                CalibratingItem.DarkFieldAlignmentDegree = StageViewModel.GetMachineStageTheta();
                result = Math.Abs(CalibratingItem.DegreeOffset) < Cache.TeachingThreshold;

                Logger.LogHtmlInformation($"Calibration {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.LowMicroscopeLensInformation,
                    Cache.HighMicroscopeLensInformation,
                    CalibratingItem.BrightFieldAlignmentDegree,
                    CalibratingItem.DarkFieldAlignmentDegree,
                    CalibratingItem.DegreeOffset,
                    AlignmentResult = new HtmlQuote(alignmentResult.ToHtmlAnonymous())
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
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyActionAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(async () =>
        {
            if (SelectReviews.Count == 0)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            foreach (var selectReview in SelectReviews)
            {
                StageViewModel.SetAbsoluteStageTheta(0d);
                if (await VerifyCalibrationAsync(selectReview, cancellationToken).ConfigureAwait(false) == false)
                    return false;
            }

            return true;
        }).ConfigureAwait(false);
    }

    private async Task<bool> VerifyCalibrationAsync(ChuckAlignmentDegreeOffsetItemDto reviewDto, CancellationToken cancellationToken)
    {
        var chuckAlignmentDegreeOffsetItemDto = reviewDto.Clone();

        Cache.ProductivityInformation = reviewDto.ProductivityInformation;

        AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
        AlignmentUserControlViewModel.IsDarkFieldAlignment = false;
        await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

        chuckAlignmentDegreeOffsetItemDto.BrightFieldAlignmentDegree = StageViewModel.GetMachineStageTheta();

        var darkFieldAlignmentOriginDegree = chuckAlignmentDegreeOffsetItemDto.BrightFieldAlignmentDegree + chuckAlignmentDegreeOffsetItemDto.DegreeOffset;
        StageViewModel.SetAbsoluteStageTheta(darkFieldAlignmentOriginDegree);

        AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;
        AlignmentUserControlViewModel.IsDarkFieldAlignment = true;
        await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

        chuckAlignmentDegreeOffsetItemDto.DarkFieldAlignmentDegree = StageViewModel.GetMachineStageTheta();

        var darkFieldAlignmentVerifyResult = chuckAlignmentDegreeOffsetItemDto.DarkFieldAlignmentDegree - darkFieldAlignmentOriginDegree;
        reviewDto.DarkFieldAlignmentVerifyResult = darkFieldAlignmentVerifyResult;

        var result = Math.Abs(darkFieldAlignmentVerifyResult) < Cache.VerifyThreshold;

        Logger.LogHtmlInformation($"{Cache.ProductivityInformation}-{(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
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

        return result;
    }

    private bool Save(ChuckAlignmentDegreeOffsetItemDto itemDto, CancellationToken cancellationToken, bool isSave = true) => InvokeSave(update =>
    {
        update(itemDto);
        update(Cache);

        Calibrations =
        [
            itemDto,
            .. Calibrations
                .Where(t => t.ProductivityInformation != itemDto.ProductivityInformation)
        ];
        if (isSave == false) return;

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<ChuckAlignmentDegreeOffsetItemDto[]>(calibrations);
        var status = Entry.Status;

        CalibratingStatuses =
        [
            .. ApplicationCookie.ProductivityInformations.Select(t => new ProductivityInformationStatus { SelectedItem = t, IsCalibrated = false })
        ];

        Calibrations =
        [
            .. temps
                .Where(t => ApplicationCookie.ProductivityInformations.Contains(t.ProductivityInformation))
                .DistinctBy(t => t.ProductivityInformation)
                .Select(t =>
                {
                    CalibratingStatuses.Single(tt => tt.SelectedItem == t.ProductivityInformation).IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        status.TotalCalibrationCount = ApplicationCookie.ProductivityInformations.Count;
        status.CalibratedCount = Calibrations.Count(t => t.IsCalibrated);
        status.VerifiedCount = Calibrations.Count(t => t.IsVerified);
        status.Details =
        [
            .. ApplicationCookie.ProductivityInformations.Select(productivityInformation =>
            {
                var item = Calibrations.SingleOrDefault(t => t.ProductivityInformation == productivityInformation);

                return new CalibrationViewModelStatus.Detail(
                    productivityInformation.ToString(),
                    item?.IsCalibrated,
                    item?.IsVerified);
            })
        ];
    }

    #endregion 校准
}