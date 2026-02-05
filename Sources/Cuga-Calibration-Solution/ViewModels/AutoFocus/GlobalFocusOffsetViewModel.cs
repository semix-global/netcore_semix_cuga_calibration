using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.AOD.Alignment;
using Core.Models.Models.AOD.Delay;
using Core.Models.Models.AutoFocus.GlobalFocusOffset;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Local.NoSQL.DB.Providers.Extensions;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Text;
using Core.Utilities.SourceGenerators.Attributes;

namespace CugaCalibration.ViewModels.AutoFocus;

[IOCAppService(ServiceType = typeof(GlobalFocusOffsetViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class GlobalFocusOffsetViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Productivity" },
        new() { StepName = "Image Param" },
        new() { StepName = "Find RTFC Position" },
        new() { StepName = "GFO" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private GlobalFocusOffsetDTO _calibratingItem = new();

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationStatus> _calibrationStatuses = [];

    #endregion Calibrate

    [ObservableProperty]
    private IReadOnlyList<GlobalFocusOffsetDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<GlobalFocusOffsetDTO> _selectedReviewItems = [];

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private GlobalFocusOffsetCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private GlobalFocusOffsetDTO[] _calibrations = [];

    [ObservableProperty]
    private MicroscopeCalChipDto _microscopeCalChip = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDto>();

        if (CalibrationStatuses.Count == 0)
            CalibrationStatuses = [.. ApplicationCookie.ProductivityInformations.Select(t => new ProductivityInformationStatus { SelectedItem = t })];

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<GlobalFocusOffsetCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<GlobalFocusOffsetDTO>();

        Calibrations =
        [
            .. Calibrations
                .Where(t => ApplicationCookie.ProductivityInformations.Contains(t.ProductivityInformation))
                .Select(t =>
                {
                    CalibrationStatuses
                        .Single(tt => tt.SelectedItem == t.ProductivityInformation)
                        .IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

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

        return Reviews.Count > 0;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                Cache.OriginAFMotor = AfViewModel.GetDarkFieldAutoFocusMotorAbsoluteValue();
                Cache.OriginRelayMotor = OpticsViewModel.GetRelayMotorAbsoluteValue(Cache.ProductivityInformation.OpticsIlluminationModeEnum);
                return true;
            case 1:
                Cache.Item.RTFCBrightFieldMachinePosition = MicroscopeCalChip.DSWDarkFieldMachineAffinePosition;
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(MicroscopeCalChip.DSWAlignmentDegree);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.DSWBrightFieldMachineAffinePosition));

                return true;
            case 2:
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.RTFCBrightFieldMachinePosition));

                return true;

            case 3:
                CalibrationStatuses.Single(t => t.SelectedItem == Cache.ProductivityInformation).IsCalibrated = true;
                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

                IsCalibrated = CalibrationStatuses.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                return true;

            case 1:
                return true;

            case 2:
                return true;

            case 3:
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.RTFCBrightFieldMachinePosition));

                return true;

            default:
                return false;
        }
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.ProductivityInformations.Contains(Cache.ProductivityInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.Item.MicroscopeLensInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation)
                   && ApplicationCookie.CIBInformations.Contains(Cache.Item.CIBInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        Cache.Item.RTFCBrightFieldMachinePosition = StageViewModel.GetMachineStagePosition();
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Item.RTFCBrightFieldMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            Guard.IsEqualTo(Cache.Item.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            var detectImageDirectory = ImageFileDirectory;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Item.MicroscopeLensInformation,
                Cache.ProductivityInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.CalChipSiteModelEnum,
                Position = Cache.Item.RTFCBrightFieldMachinePosition,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            try
            {
                CalibratingItem = new GlobalFocusOffsetDTO
                {
                    ProductivityInformation = Cache.ProductivityInformation,
                    CalChipSiteModelEnum = Cache.CalChipSiteModelEnum,
                };

                var rtfcResultDTO = await CIBViewModel.RuntimeAfCalibrationAsync(
                    Cache.Item.CIBConfiguration,
                    Cache.Item.CIBInformation,
                    StageViewModel.DarkFieldToMachinePosition(StageViewModel.MachineToBrightFieldPosition(Cache.Item.RTFCBrightFieldMachinePosition)),
                    Cache.Item.LaserLightInformation,
                    Cache.ProductivityInformation,
                    cancellationToken,
                    isAppliedDefaultRtfcParam: false,
                    calChipSiteModelEnum: Cache.CalChipSiteModelEnum,
                    stageCoordinateSystemEnum: StageCoordinateSystemEnum.Machine,
                    saveImageFileDirectory: detectImageDirectory,
                    logGuid: HtmlLogUniqueId,
                    logName: Cache.CalChipSiteModelEnum.ToDescriptionOrString());

                CalibratingItem.RuntimeAfCalibrationResultDTO = rtfcResultDTO;
                CalibratingItem.IsCalibrated = true;
                Guard.IsTrue(Save([CalibratingItem], cancellationToken));
                return true;
            }
            finally
            {
                // OpticsViewModel.SetRelayMotorAbsoluteValue(Cache.ProductivityInformation.OpticsIlluminationModeEnum, Cache.OriginRelayMotor);
                AfViewModel.SetDarkFieldAutoFocusMotorAbsoluteValue(Cache.OriginAFMotor);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        if (SelectedReviewItems.Count == 0)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        await InvokeVerifyAsync(async () =>
        {
            var errorMessageStringBuilder = new StringBuilder();
            try
            {
                foreach (var selectedReviewItem in SelectedReviewItems.OrderBy(t => t.ProductivityInformation))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var title = selectedReviewItem.ProductivityInformation.ToString();

                    Cache.ProductivityInformation = selectedReviewItem.ProductivityInformation;

                    Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                    {
                        Cache.ProductivityInformation,
                        Cache.QualityThreshold,
                        Cache.Item.RTFCBrightFieldMachinePosition,
                        selectedReviewItem.CalChipSiteModelEnum,
                        CalibrationRTFCResult = new HtmlQuote(selectedReviewItem.RuntimeAfCalibrationResultDTO.ToHtmlAnonymous()),
                    }), HtmlLogUniqueId.LoggingHtml());

                    MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                    StageViewModel.SetAbsoluteStageTheta(MicroscopeCalChip.DSWAlignmentDegree);
                    StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.RTFCBrightFieldMachinePosition));

                    selectedReviewItem.IsVerified = false;
                    var globalFocusOffsetDTO = selectedReviewItem.Clone();

                    if (globalFocusOffsetDTO.RuntimeAfCalibrationResultDTO.IsAFServo)
                        AfViewModel.SetDarkField(globalFocusOffsetDTO.CalChipSiteModelEnum, globalFocusOffsetDTO.RuntimeAfCalibrationResultDTO.ECSValue, globalFocusOffsetDTO.RuntimeAfCalibrationResultDTO.MotorValue);
                    else
                        ThrowHelper.ThrowNotSupportedException("Relay Servo is not supported.");
                    // OpticsViewModel.SetRelayMotorAbsoluteValue(Cache.ProductivityInformation.OpticsIlluminationModeEnum, globalFocusOffsetDTO.RuntimeAfCalibrationResultDTO.MotorValue);

                    using var darkFieldImageDto = await CIBViewModel.GetPMTImageAsync(
                        globalFocusOffsetDTO.ProductivityInformation,
                        StageCoordinateSystemEnum.Machine,
                        StageViewModel.DarkFieldToMachinePosition(StageViewModel.MachineToBrightFieldPosition(Cache.Item.RTFCBrightFieldMachinePosition)),
                        Cache.Item.CIBInformation,
                        CalibrationConstantsHelper.MainXWidthPixel,
                        (false, Cache.CalChipSiteModelEnum),
                        (false, Cache.Item.CIBConfiguration),
                        (false, Cache.Item.LaserLightInformation),
                        false,
                        cancellationToken);

                    var darkFieldFilePath = $"{ImageFileDirectory}\\Verify_({globalFocusOffsetDTO.ProductivityInformation})_Guid({HtmlLogUniqueId}).jpg";
                    darkFieldImageDto.Image.Save(darkFieldFilePath);

                    var verifyQuality = CalibrationAlgorithmService.GetDarkFieldQuality(darkFieldImageDto.Image);
                    selectedReviewItem.RuntimeAfCalibrationResultDTO.DarkFieldFilePath = darkFieldFilePath;

                    if (selectedReviewItem.IsCalibrated) selectedReviewItem.IsVerified = Math.Abs(verifyQuality - selectedReviewItem.RuntimeAfCalibrationResultDTO.DarkFieldQuality) < Cache.QualityThreshold;

                    var htmlBullet = new HtmlBullet(new
                    {
                        CalibrationQuality = selectedReviewItem.RuntimeAfCalibrationResultDTO.DarkFieldQuality,
                        VerifyQuality = verifyQuality,
                        HtmlTab = new HtmlTab(new
                        {
                            Image = new HtmlImage(darkFieldFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    });

                    if (selectedReviewItem.IsOk)
                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                    else
                    {
                        errorMessageStringBuilder.AppendLine($"{title}: Error");
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                    }
                }

                Guard.IsTrue(Save(SelectedReviewItems, cancellationToken));

                var result = SelectedReviewItems.All(t => t.IsOk);

                DialogWindowProvider.ShowDialog($"""
                                                 Verify : {(result ? "OK" : "Failed")}
                                                 {errorMessageStringBuilder}
                                                 """,
                    DialogButtonsEnum.OK,
                    result ? DialogIconEnum.Information : DialogIconEnum.Warning);

                return result;
            }
            finally
            {
                // OpticsViewModel.SetRelayMotorAbsoluteValue(Cache.ProductivityInformation.OpticsIlluminationModeEnum, Cache.OriginRelayMotor);
                AfViewModel.SetDarkFieldAutoFocusMotorAbsoluteValue(Cache.OriginAFMotor);
            }
        }).ConfigureAwait(false);
    }

    private bool Save(IReadOnlyList<GlobalFocusOffsetDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                .. Calibrations.Where(t => t.ProductivityInformation != dto.ProductivityInformation),
                dto.Clone()
            ];
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}