using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.AutoFocus.CalChipFocusOffset;
using Core.Models.Models.Microscope.CalChip;
using Core.Utilities.SourceGenerators.Attributes;
using Local.SQL.Cache.Providers.Extensions;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Enums;
using System.Text;

namespace CugaCalibration.ViewModels.AutoFocus;

[IOCAppService(ServiceType = typeof(AutoFocusCalChipFocusOffsetViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AutoFocusCalChipFocusOffsetViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "DSW Param" },
        new() { StepName = "DSW RTFC" },
        new() { StepName = "Haze Param" },
        new() { StepName = "Haze RTFC" },
        new() { StepName = "Chuck Param" },
        new() { StepName = "Chuck RTFC" },
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private AutoFocusCalChipFocusOffsetDTO _calibratingItem = new();

    #endregion Calibrate

    [ObservableProperty]
    private AutoFocusCalChipFocusOffsetDTO _review = new();

    [ObservableProperty]
    private AutoFocusCalChipFocusOffsetDTO? _selectReview;

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private AutoFocusCalChipFocusOffsetCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private AutoFocusCalChipFocusOffsetDTO _calibration = new();

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDTO>();

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<AutoFocusCalChipFocusOffsetCache>();
        Calibration = CacheProvider.GetOrDefault<AutoFocusCalChipFocusOffsetDTO>();

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

        return true;
    }

    protected override async Task<bool> CalibratingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

        StageViewModel.SetAbsoluteStageTheta(MicroscopeCalChip.DSWAlignmentDegree);
        StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.CalChipRTFCBrightFieldMachinePosition == Point.Origin
            ? MicroscopeCalChip.DSWBrightFieldMachineAffinePosition
            : Cache.Item.CalChipRTFCBrightFieldMachinePosition));

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Review = Calibration.Clone();

        return Review.IsCalibrated;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                CalibratingItem = new AutoFocusCalChipFocusOffsetDTO
                {
                    ProductivityInformation = Cache.ProductivityInformation
                };
                return true;

            case 1:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.HazeModel;
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.CalChipRTFCBrightFieldMachinePosition == Point.Origin
                    ? MicroscopeCalChip.HazeItem.BrightFieldMachinePosition
                    : Cache.Item.CalChipRTFCBrightFieldMachinePosition));
                return true;

            case 2:
                return true;

            case 3:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
                StageViewModel.SetBrightFieldAbsoluteStageXy(Cache.Item.CalChipRTFCBrightFieldMachinePosition == Point.Origin
                    ? Point.Origin
                    : StageViewModel.MachineToBrightFieldPosition(Cache.Item.CalChipRTFCBrightFieldMachinePosition));
                return true;

            case 4:
                return true;

            case 5:
                IsCalibrated = true;

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
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.DswModel;
                StageViewModel.SetAbsoluteStageTheta(MicroscopeCalChip.DSWAlignmentDegree);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.CalChipRTFCBrightFieldMachinePosition));
                return true;

            case 3:
                return true;

            case 4:
                Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.HazeModel;
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.CalChipRTFCBrightFieldMachinePosition));
                return true;

            case 5:
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
            Cache.Item.CalChipRTFCBrightFieldMachinePosition = StageViewModel.GetMachineStagePosition();
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Item.CalChipRTFCBrightFieldMachinePosition,
                Cache.MicroscopeLensInformation,
                Cache.ProductivityInformation,
                Cache.Item.LaserLightInformation,
                Cache.CIBInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.MicroscopeLensInformation)
                   && ApplicationCookie.ProductivityInformations.Contains(Cache.ProductivityInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation)
                   && ApplicationCookie.CIBInformations.Contains(Cache.CIBInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;

            Cache.OriginAFMotor = AfViewModel.GetDarkFieldAutoFocusMotorAbsoluteValue();
            Cache.OriginRelayMotor = OpticsViewModel.GetRelayMotorAbsoluteValue(Cache.ProductivityInformation.OpticsIlluminationModeEnum);
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.Item.CalChipRTFCBrightFieldMachinePosition,
                Cache.OriginAFMotor,
                Cache.OriginRelayMotor,
                Cache.MotorOffsetThreshold,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            Guard.IsEqualTo(Cache.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            try
            {
                var rtfcResultDTO = await CIBViewModel.RuntimeAFCalibrationAsync(
                    Cache.ProductivityInformation,
                    Cache.CalChipSiteModelEnum,
                    StageCoordinateSystemEnum.Bright,
                    StageViewModel.MachineToBrightFieldPosition(Cache.Item.CalChipRTFCBrightFieldMachinePosition),
                    800,
                    Cache.CIBInformation,
                    Cache.Item.CIBConfiguration,
                    Cache.Item.LaserLightInformation,
                    detectImageDirectory,
                    HtmlLogUniqueId,
                    cancellationToken);

                switch (Cache.CalChipSiteModelEnum)
                {
                    case CalChipSiteModelEnum.DswModel:
                        CalibratingItem.DSWRuntimeAfCalibrationResultDTO = rtfcResultDTO;
                        break;
                    case CalChipSiteModelEnum.HazeModel:
                        CalibratingItem.HazeRuntimeAfCalibrationResultDTO = rtfcResultDTO;
                        break;
                    case CalChipSiteModelEnum.ChuckModel:
                        CalibratingItem.ChuckRuntimeAfCalibrationResultDTO = rtfcResultDTO;
                        break;
                    default:
                        ThrowHelper.ThrowNotSupportedException();
                        break;
                }

                CalibratingItem.IsCalibrated = Math.Abs(CalibratingItem.DswToChuckMotorValue) < Cache.MotorOffsetThreshold
                                               && Math.Abs(CalibratingItem.HazeToChuckMotorValue) < Cache.MotorOffsetThreshold;
                Guard.IsTrue(Save(CalibratingItem, cancellationToken));

                if (CalibrationStepIndex != CalibrationStepList.Count - 1) return true;

                Logger.LogHtmlInformation($"Calibration {(CalibratingItem.IsCalibrated ? "Success" : "Failed")}", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    CalibratingItem.DswToChuckMotorValue,
                    CalibratingItem.HazeToChuckMotorValue,
                    CalibratingItem.DswToChuckEcsValue,
                    CalibratingItem.HazeToChuckEcsValue
                }), HtmlLogUniqueId.LoggingHtml());

                return CalibratingItem.IsCalibrated;
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
        await InvokeVerifyAsync(async () =>
        {
            Cache.OriginAFMotor = AfViewModel.GetDarkFieldAutoFocusMotorAbsoluteValue();
            Cache.OriginRelayMotor = OpticsViewModel.GetRelayMotorAbsoluteValue(Cache.ProductivityInformation.OpticsIlluminationModeEnum);
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
            {
                Cache.VerifyQualityThreshold,
                Cache.OriginAFMotor,
                Cache.OriginRelayMotor,
                Cache.CalChipSiteModelEnum,
                Cache.MicroscopeLensInformation,
                Cache.ProductivityInformation,
                Cache.CIBInformation
            }), HtmlLogUniqueId.LoggingHtml());
            var errorMessageStringBuilder = new StringBuilder();
            try
            {
                Review.IsVerified = false;
                SelectReview = Review.Clone();

                var result = true;
                foreach (var calChipSiteModelEnum in EnumHelper.Enums<CalChipSiteModelEnum>())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var detectImageDirectory = ImageFileDirectory;

                    if (calChipSiteModelEnum is not (CalChipSiteModelEnum.DswModel or CalChipSiteModelEnum.HazeModel or CalChipSiteModelEnum.ChuckModel)) continue;

                    var title = calChipSiteModelEnum.ToDescriptionOrString();

                    Cache.CalChipSiteModelEnum = calChipSiteModelEnum;

                    Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                    {
                        Cache.Item.CalChipRTFCBrightFieldMachinePosition,
                        Cache.Item.LaserLightInformation,
                        CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                        detectImageDirectory
                    }), HtmlLogUniqueId.LoggingHtml());

                    MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
                    StageViewModel.SetAbsoluteStageTheta(calChipSiteModelEnum is CalChipSiteModelEnum.DswModel ? MicroscopeCalChip.DSWAlignmentDegree : 0);
                    StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.CalChipRTFCBrightFieldMachinePosition), calChipSiteModelEnum);

                    var rtfcResultDTO = calChipSiteModelEnum switch
                    {
                        CalChipSiteModelEnum.DswModel => SelectReview.DSWRuntimeAfCalibrationResultDTO,
                        CalChipSiteModelEnum.HazeModel => SelectReview.HazeRuntimeAfCalibrationResultDTO,
                        CalChipSiteModelEnum.ChuckModel => SelectReview.ChuckRuntimeAfCalibrationResultDTO,
                        _ => throw new NotSupportedException()
                    };

                    if (rtfcResultDTO.IsAFServo)
                        AfViewModel.SetDarkField(calChipSiteModelEnum, rtfcResultDTO.ECSValue, rtfcResultDTO.MotorValue);
                    else
                        ThrowHelper.ThrowNotSupportedException("Relay Servo is not supported.");
                    // OpticsViewModel.SetRelayMotorAbsoluteValue(Cache.ProductivityInformation.OpticsIlluminationModeEnum, globalFocusOffsetDTO.RuntimeAfCalibrationResultDTO.MotorValue);

                    using var darkFieldImageDto = await CIBViewModel.GetPMTImageAsync(
                        Cache.ProductivityInformation,
                        StageCoordinateSystemEnum.Bright,
                        StageViewModel.MachineToBrightFieldPosition(Cache.Item.CalChipRTFCBrightFieldMachinePosition),
                        Cache.ImageWidth,
                        Cache.CIBInformation,
                        (true, null),
                        (false, Cache.Item.CIBConfiguration),
                        (false, Cache.Item.LaserLightInformation),
                        false,
                        cancellationToken);

                    var darkFieldFilePath = $"{ImageFileDirectory}\\Verify_({calChipSiteModelEnum})_Guid({HtmlLogUniqueId}).jpg";
                    darkFieldImageDto.Image.Save(darkFieldFilePath);

                    var verifyQuality = CalibrationAlgorithmService.GetDarkFieldQuality(darkFieldImageDto.Image);
                    rtfcResultDTO.DarkFieldFilePath = darkFieldFilePath;

                    var qualityError = Math.Abs(verifyQuality - rtfcResultDTO.DarkFieldQuality);
                    var verifyResult = qualityError < Cache.VerifyQualityThreshold;

                    var htmlBullet = new HtmlBullet(new
                    {
                        rtfcResultDTO.IsAFServo,
                        rtfcResultDTO.ECSValue,
                        rtfcResultDTO.MotorValue,
                        CalibrationQuality = rtfcResultDTO.DarkFieldQuality,
                        VerifyQuality = verifyQuality,
                        qualityError,
                        HtmlTab = new HtmlTab(new
                        {
                            Image = new HtmlImage(darkFieldFilePath, htmlImageOverlays: [new HtmlImageCrossOverlay(true)])
                        })
                    });
                    rtfcResultDTO.DarkFieldQuality = verifyQuality;

                    if (verifyResult)
                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                    else
                    {
                        errorMessageStringBuilder.AppendLine($"{title}: Error,Quality:{qualityError}");
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                    }

                    result = result && verifyResult;
                }

                Review.IsVerified = result;
                Guard.IsTrue(Save(Review, cancellationToken));

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

    private bool Save(AutoFocusCalChipFocusOffsetDTO dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);
        update(dto);

        Calibration = dto.Clone();

        CacheProvider.Set(Calibration, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}