using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.Focus;
using MathNet.Numerics;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
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

[IOCAppService(ServiceType = typeof(MicroscopeFocusViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MicroscopeFocusViewModel : CalibrationViewModelBase<MicroscopeFocusCache>
{
    #region 属性

    public override string CalibrateDirectoryName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override string CalibrateFileName => EnumHelper.ToDescriptionString(Cache.MicroscopeLensInformation.LensName);

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select a lens" },
        new() { StepName = "Find Position" },
        new() { StepName = "Find Focus" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial MicroscopeFocusDTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<MicroscopeLensInformationStatus> CalibratingStatuses { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeFocusDTOItem? SelectedItem { get; set; }

    #endregion Calibrate

    #region Review

    [ObservableProperty]
    public partial IReadOnlyList<MicroscopeFocusDTO> Reviews { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeFocusDTO? SelectedReviewItem { get; set; }

    #endregion Review

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial MicroscopeFocusCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial MicroscopeFocusDTO[] Calibrations { get; set; } = [];

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务重载

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Cache = ApplicationCookieService.GetCache<MicroscopeFocusCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<MicroscopeFocusDTO>(cancellationToken);

        UpdateEntryStatus(Unsafe.As<CalibrationDTOBase[]>(Calibrations), cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        AfViewModel.ToggleBrightFieldEnable(false);
        AfViewModel.ToggleCalChipSiteModelEnum(CalChipSiteModelEnum.ChuckModel);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Reviews =
        [
            .. Calibrations
                .Select(t => t.Clone())
                .Where(t => t.IsCalibrated)
                .OrderBy(t => t.LensInformation.ObjectiveMagnification)
                .ThenBy(t => t.LensInformation.LensCode)
        ];

        if (Reviews.Count == 0)
            return false;

        AfViewModel.ToggleBrightFieldEnable(false);
        AfViewModel.ToggleCalChipSiteModelEnum(CalChipSiteModelEnum.ChuckModel);
        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                CalibratingItem = new()
                {
                    LensInformation = Cache.MicroscopeLensInformation
                };
                await MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocusAsync(Cache.MicroscopeLensInformation, isMoveToMicroscopeCenter: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                return true;

            case 1:
                StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(Cache.Item.FindFocusPosition, CalChipSiteModelEnum.ChuckModel);
                return true;

            case 2:
                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> CancelingAsync()
    {
        AfViewModel.ToggleCalChipSiteModelEnum(CalChipSiteModelEnum.ChuckModel);

        await MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocusAsync(Cache.MicroscopeLensInformation, isMoveToMicroscopeCenter: false).ConfigureAwait(false);

        StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(Point.Origin, CalChipSiteModelEnum.ChuckModel);
        return true;
    }

    #endregion 控制校准业务重载

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.MicroscopeLensInformation);
        });
    }


    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeCalibrateAsync(() =>
        {
            Cache.Item.FindFocusPosition = StageViewModel.GetBrightFieldStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation,
                Cache.Item.FindFocusPosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task<bool> Step2Async(CancellationToken cancellationToken)
    {
        return await InvokeCalibrateAsync(async () =>
        {
            CalibratingItem.IsCalibrated = false;
            CalibratingItem.Items = [];

            var detectImageDirectory = ImageFileDirectory;

            var (isSuccessVerify, errorMessage) = Cache.CalibrationVerify();
            if (!isSuccessVerify)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment($"Error:{errorMessage}"), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(errorMessage, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            AfViewModel.ToggleBrightFieldEnable(false);
            await MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocusAsync(Cache.MicroscopeLensInformation, isMoveToMicroscopeCenter: false, cancellationToken: cancellationToken).ConfigureAwait(false);

            StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(Cache.Item.FindFocusPosition, CalChipSiteModelEnum.ChuckModel);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.MicroscopeLensInformation,
                Cache.Item.FindFocusPosition,
                Cache.Item.StartECS,
                Cache.Item.StopECS,
                Cache.Item.StepECS,
                Cache.Threshold,
                Cache.Item.SetVoltageAfErrorThreshold,
                ImageFileDirectory = detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("Get Quality", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            var listDownResult = new List<bool>();
            double? downQualityValue = null;
            foreach (var (index, ecs) in Generate.LinearRange(Cache.Item.StartECS, Cache.Item.StepECS, Cache.Item.StopECS).Select((t, i) => (i, t)))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var focusDTOItem = new MicroscopeFocusDTOItem
                {
                    Index = index,
                    EcsValue = ecs
                };

                AfViewModel.SetSensorEcsValue(ecs);

                GetQuality(focusDTOItem);

                CalibratingItem.Items = [.. CalibratingItem.Items, focusDTOItem];

                if (downQualityValue is not null) listDownResult.Add(downQualityValue.Value < focusDTOItem.Quality);
                downQualityValue = focusDTOItem.Quality;
                if (listDownResult.HasConsecutiveEqual(30, false)) break; // 连续30个下降说明已经到了最低点
            }

            SelectedItem = CalibratingItem.Items.Maxima(t => t.Quality).First();

            Logger.LogHtmlInformation("Find ECS Result", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Ecs = SelectedItem.EcsValue,
                ImageQuality = SelectedItem.Quality,
                Image = new HtmlImage(SelectedItem.FilePath),
                Plot = new HtmlContainer([.. CalibratingItem.PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("Set Voltage To AfError Zero", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            AfViewModel.SetSensorEcsValue(SelectedItem.EcsValue);
            var (isSuccess, voltage, afErrorAverage) = MicroscopeViewModel.SetVoltageToAfErrorZeroFast(
                Cache.Item.SetVoltageAfErrorThreshold,
                20,
                HtmlLogUniqueId,
                Name,
                cancellationToken);
            if (isSuccess == false)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("Error: Set Voltage To AfError Zero Failed!"), HtmlLogUniqueId.LoggingHtml());
                return false;
            }

            SelectedItem.TransBufferAfErrorValue = afErrorAverage;
            SelectedItem.MicroscopeVoltage = voltage;

            CalibratingItem.Result = SelectedItem.Clone();
            CalibratingItem.IsCalibrated = true;
            Guard.IsTrue(Save([CalibratingItem], cancellationToken));

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                CalibratingItem.LensInformation,
                CalibratingItem.Result.EcsValue,
                CalibratingItem.Result.Quality,
                CalibratingItem.Result.MicroscopeVoltage,
                CalibratingItem.Result.TransBufferAfErrorValue,
                Image = new HtmlImage(SelectedItem.FilePath)
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(async () =>
        {
            if (SelectedReviewItem is null)
            {
                DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var detectImageDirectory = ImageFileDirectory;

            SelectedReviewItem.IsVerified = false;
            Cache.MicroscopeLensInformation = SelectedReviewItem.LensInformation;

            Logger.LogHtmlInformation($"{Cache.MicroscopeLensInformation}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                SelectedReviewItem.Result.EcsValue,
                SelectedReviewItem.Result.MicroscopeVoltage,
                Cache.MicroscopeLensInformation,
                Cache.Item.FindFocusPosition,
                Cache.Threshold,
                Cache.ParfocalThreshold,
                ImageFileDirectory = detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            AfViewModel.ToggleBrightFieldEnable(false);

            await MicroscopeViewModel.SwitchMicroscopeLensInformationNotAutoFocusAsync(Cache.MicroscopeLensInformation, isMoveToMicroscopeCenter: false, cancellationToken: cancellationToken).ConfigureAwait(false);

            StageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(Cache.Item.FindFocusPosition, CalChipSiteModelEnum.ChuckModel);
            MicroscopeViewModel.SetAFParams(SelectedReviewItem.LensInformation, SelectedReviewItem.Result.EcsValue, SelectedReviewItem.Result.MicroscopeVoltage);

            AfViewModel.ToggleBrightFieldEnable(true);

            var currentEcs = AfViewModel.GetSensorAverageEcsValue();
            var verifyItem = new MicroscopeFocusDTOItem
            {
                Index = 0,
                Quality = 0,
                EcsValue = currentEcs,
                MicroscopeVoltage = SelectedReviewItem.Result.MicroscopeVoltage,
                FilePath = detectImageDirectory
            };
            GetQuality(verifyItem);

            AfViewModel.ToggleBrightFieldEnable(false);

            Cache.VerifyResultQuality = verifyItem.Quality;
            Cache.VerifyResultError = verifyItem.Quality - SelectedReviewItem.Result.Quality;
            var result = Math.Abs(Cache.VerifyResultError) < Cache.Threshold;

            SelectedReviewItem.IsVerified = result;
            Guard.IsTrue(Save([SelectedReviewItem], cancellationToken));

            Logger.LogHtmlInformation($"Verify {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
            {
                CalibrationECS = SelectedReviewItem.Result.EcsValue,
                CalibrationQuality = SelectedReviewItem.Result.Quality,
                CalibrationVoltage = SelectedReviewItem.Result.MicroscopeVoltage,
                VerifyECS = verifyItem.EcsValue,
                VerifyQuality = verifyItem.Quality,
                Cache.Threshold,
            }), HtmlLogUniqueId.LoggingHtml());

            DialogWindowProvider.ShowDialog($"Verify {(result ? "OK" : "Failed")}, " +
                                            $"Verify Quality: ({verifyItem.Quality:f3}) Calibration Quality: ({SelectedReviewItem.Result.Quality:f3}) " +
                                            $"Error: ({Cache.VerifyResultError:f3})", DialogButtonsEnum.OK,
                result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            if (result == false) return false;

            // 同焦验证
            var varifiedList = Calibrations.Where(t => t.IsOk).Select(t => t.Result).ToList();
            if (varifiedList.Count >= 2)
            {
                var parfocalOffset = varifiedList.Max(t => t.EcsValue) - varifiedList.Min(t => t.EcsValue);
                result = Math.Abs(parfocalOffset) <= Cache.ParfocalThreshold;
                if (result == false)
                {
                    DialogWindowProvider.ShowDialog($"Parfocal {(result ? "OK" : "Failed")}, Offset: {parfocalOffset}", DialogButtonsEnum.OK, result ? DialogIconEnum.Information : DialogIconEnum.Warning);
                    SelectedReviewItem.IsVerified = false;

                    Guard.IsTrue(Save([SelectedReviewItem], cancellationToken));
                }

                Logger.LogHtmlInformation($"Parfocal {(result ? "OK" : "Failed")}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    Cache.ParfocalThreshold,
                    parfocalOffset,
                    EcsResult = new HtmlTable([.. Calibrations.Select(t => new { t.IsVerified, t.LensInformation, t.Result.EcsValue })])
                }), HtmlLogUniqueId.LoggingHtml());
            }

            return result;
        }).ConfigureAwait(false);
    }


    private void GetQuality(MicroscopeFocusDTOItem microscopeFocusDTOItem)
    {
        if (microscopeFocusDTOItem.Index == 0) Thread.Sleep(3000);
        using var image = ReviewViewModel.GetBrightFieldImage();

        microscopeFocusDTOItem.FilePath =
            $"{microscopeFocusDTOItem.FilePath}\\Index({microscopeFocusDTOItem.Index})_Ecs({microscopeFocusDTOItem.EcsValue:F3})_Quality({microscopeFocusDTOItem.Quality:F3})_Guid({HtmlLogUniqueId}).jpg";

        image.SaveImage(microscopeFocusDTOItem.FilePath);
        var quality = ReviewViewModel.GetQuality(image, HtmlLogUniqueId);
        microscopeFocusDTOItem.Quality = quality;

        var htmlBulletList = new HtmlBullet(new
        {
            microscopeFocusDTOItem.EcsValue,
            ImageQuality = microscopeFocusDTOItem.Quality,
            Image = new HtmlImage(microscopeFocusDTOItem.FilePath)
        });

        Logger.LogHtmlInformation($"Get Quality, Ecs:{microscopeFocusDTOItem.EcsValue}", HtmlHeaderLevelEnum.Header4, htmlBulletList, HtmlLogUniqueId.LoggingHtml());
    }

    private bool Save(IReadOnlyList<MicroscopeFocusDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                dto.Clone(),
                .. Calibrations.Where(t => t.LensInformation != dto.LensInformation)
            ];
        }

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<MicroscopeFocusDTO[]>(calibrations);
        var status = Entry.Status;

        CalibratingStatuses =
        [
            .. ApplicationCookie.MicroscopeLensInformations.Select(t => new MicroscopeLensInformationStatus { SelectedItem = t, IsCalibrated = false })
        ];

        Calibrations =
        [
            .. temps
                .Where(t => ApplicationCookie.MicroscopeLensInformations.Contains(t.LensInformation))
                .DistinctBy(t => t.LensInformation)
                .Select(t =>
                {
                    CalibratingStatuses.Single(tt => tt.SelectedItem == t.LensInformation).IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        status.TotalCalibrationCount = ApplicationCookie.MicroscopeLensInformations.Count;
        status.CalibratedCount = Calibrations.Count(t => t.IsCalibrated);
        status.VerifiedCount = Calibrations.Count(t => t.IsVerified);
        status.Details =
        [
            .. ApplicationCookie.MicroscopeLensInformations.Select(productivityInformation =>
            {
                var item = Calibrations.SingleOrDefault(t => t.LensInformation == productivityInformation);

                return new CalibrationViewModelStatus.Detail(
                    productivityInformation.ToString(),
                    item?.IsCalibrated,
                    item?.IsVerified);
            })
        ];
    }

    #endregion 校准
}