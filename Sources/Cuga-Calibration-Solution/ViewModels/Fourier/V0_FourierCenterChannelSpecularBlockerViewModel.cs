using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Fourier;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Fourier.PupilCameraAlignment;
using Core.Models.Models.Fourier.V0CenterChannelFlexibleAperture;
using Core.Models.Models.Fourier.V0CenterChannelSpecularBlocker;
using Core.Models.Models.Microscope.CalChip;
using Microsoft.Extensions.Hosting;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Fourier;

[IOCAppService(ServiceType = typeof(V0FourierCenterChannelSpecularBlockerViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class V0FourierCenterChannelSpecularBlockerViewModel : CalibrationViewModelBase<V0FourierCenterChannelSpecularBlockerCache>
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Param" },
        new() { StepName = "Find Shiny Wafer Position" },
        new() { StepName = "Channel 3" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial V0FourierCenterChannelSpecularBlockerDTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<ProductivityInformationStatus> CalibratingStatuses { get; set; } = [];

    [ObservableProperty]
    public partial V0FourierCenterChannelFlexibleApertureDTOItem?[] YRodItems { get; set; } = [];

    [ObservableProperty]
    public partial V0FourierCenterChannelFlexibleApertureDTOItem? SelectedYRodItem { get; set; } = null;

    #endregion Calibrate

    [ObservableProperty]
    public partial IReadOnlyList<V0FourierCenterChannelSpecularBlockerDTO> Reviews { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<V0FourierCenterChannelSpecularBlockerDTO> SelectedReviewItems { get; set; } = [];

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial V0FourierCenterChannelSpecularBlockerCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial V0FourierCenterChannelSpecularBlockerDTO[] Calibrations { get; set; } = [];

    [ObservableProperty]
    public partial FourierPupilCameraAlignmentDTO FourierPupilCameraAlignment { get; set; } = new();

    [ObservableProperty]
    public partial V0FourierCenterChannelFlexibleApertureDTO FourierCenterChannelFlexibleAperture { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        FourierPupilCameraAlignment = ApplicationCookieService.GetCalibration<FourierPupilCameraAlignmentDTO>(cancellationToken);
        FourierCenterChannelFlexibleAperture = ApplicationCookieService.GetCalibration<V0FourierCenterChannelFlexibleApertureDTO>(cancellationToken);
        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
        YRodItems = [null, .. FourierCenterChannelFlexibleAperture.YRodItems];

        Cache = ApplicationCookieService.GetCache<V0FourierCenterChannelSpecularBlockerCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<V0FourierCenterChannelSpecularBlockerDTO>(cancellationToken);

        UpdateEntryStatus(Unsafe.As<CalibrationDTOBase[]>(Calibrations), cancellationToken);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Reviews =
        [
            .. Calibrations.OrderBy(t => t.ProductivityInformation)
        ];

        foreach (var review in Reviews)
        {
            review.Channel3Item.Review();
            review.Channel3Item.ReviewCIB();
        }

        return Reviews.Count > 0;
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        switch (CalibrationStepIndex)
        {
            case 0:
                return true;

            case 1:
                return true;

            case 2:
                return true;

            case 3:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.Item.MicroscopeLensInformation, cancellationToken: cancellationToken);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.ShinyWaferFindBFMachinePosition), CalChipSiteModelEnum.ShinyWaferModel);

                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        switch (CalibrationStepIndex)
        {
            case 0:
                CalibratingItem.Dispose();
                CalibratingItem = new V0FourierCenterChannelSpecularBlockerDTO
                {
                    ProductivityInformation = Cache.ProductivityInformation
                };

                return true;

            case 1:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.Item.MicroscopeLensInformation, cancellationToken: cancellationToken);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.Item.ShinyWaferFindBFMachinePosition != Point.Origin
                        ? Cache.Item.ShinyWaferFindBFMachinePosition
                        : MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.ShinyWaferModel)), CalChipSiteModelEnum.ShinyWaferModel);

                return true;

            case 2:
                return true;

            case 3:
                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> CancelingAsync()
    {
        await base.CancelingAsync().ConfigureAwait(false);

        CalibratingItem.Dispose();
        foreach (var review in Reviews) review.Dispose();
        foreach (var calibration in Calibrations) calibration.Dispose();

        if (HostEnvironment.IsDevelopment())
        {
            FourierViewModel.UseSimulatorImage(null);
            CIBViewModel.UseSimulatorImages(null);
        }

        return true;
    }

    #endregion 控制校准业务

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            if (FourierPupilCameraAlignment.IsOk == false)
            {
                const string comment = "Fourier Pupil Camera Alignment is not calibrated!";
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment(comment), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(comment, DialogButtonsEnum.OK, DialogIconEnum.Error);

                return false;
            }

            if (FourierCenterChannelFlexibleAperture.IsOk == false)
            {
                const string comment = "Fourier Center Channel Flexible Aperture is not calibrated!";
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment(comment), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(comment, DialogButtonsEnum.OK, DialogIconEnum.Error);

                return false;
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.OpticsMagTypeProductivityInformations.Contains(Cache.ProductivityInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.Item.ScanLength
            }), HtmlLogUniqueId.LoggingHtml());

            LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
            LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, Cache.Item.LaserLightInformation.Coefficient);
            LaserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);
            OpticsViewModel.SetOpticsConfiguration(Cache.Item.OpticsConfiguration);

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.Item.MicroscopeLensInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Guard.IsEqualTo(Cache.Item.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            StageViewModel.SetAbsoluteStageTheta(0d);
            Cache.Item.ShinyWaferFindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.Item.ScanLength,
                Cache.Item.ShinyWaferFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.Channel3Item, cancellationToken));
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
            Guard.IsTrue(ApplicationCookie.LaserLightInformations.Contains(Cache.VerifyLaserLightInformation));

            var errorMessageStringBuilder = new StringBuilder();
            foreach (var selectedReviewItem in SelectedReviewItems.OrderBy(t => t.ProductivityInformation))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var title = selectedReviewItem.ProductivityInformation.ToString();

                Cache.ProductivityInformation = selectedReviewItem.ProductivityInformation;

                Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                selectedReviewItem.IsVerified = false;
                if (selectedReviewItem.IsCalibrated)
                {
                    await InvokeVerifyAsync(selectedReviewItem, cancellationToken);

                    selectedReviewItem.IsVerified = Math.Abs(selectedReviewItem.Channel3Item.ExtinctionRatio) <= Cache.ExtinctionRatioThreshold;
                }

                var item = selectedReviewItem.Channel3Item;
                Logger.LogHtmlInformation("Channel3 ROI", HtmlHeaderLevelEnum.Header4, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

                var htmlQuote = new HtmlQuote(new
                {
                    item.Step0CIBImageAverageValue,
                    item.Step1CIBImageAverageValue,
                    item.ExtinctionRatio
                });

                if (selectedReviewItem.IsVerified)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, htmlQuote, HtmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, htmlQuote, HtmlLogUniqueId.LoggingHtml());

                if (selectedReviewItem.IsOk == false) errorMessageStringBuilder.AppendLine($"{title}: Error");
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
        }).ConfigureAwait(false);
    }

    private async Task<bool> InvokeCalibrateAsync(V0FourierCenterChannelSpecularBlockerDTOItem item, CancellationToken cancellationToken)
    {
        var detectImageDirectory = ImageFileDirectory;

        var fourierPupilCameraAlignmentItem = FourierPupilCameraAlignment.Channel3Item;
        var config = FourierViewModel.GetFourierConfig();

        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Cache.ProductivityInformation,
            Cache.Item.MicroscopeLensInformation,
            Cache.Item.LaserLightInformation,
            OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
            Cache.Item.ScanLength,
            Cache.Item.ShinyWaferFindBFMachinePosition,
            fourierPupilCameraAlignmentItem.ROI.ImageROI,
            config.CH3_X_PPOSMin,
            config.CH3_X_PPOSMax,
            config.CH3_Y_LPOSMin,
            config.CH3_Y_LPOSMax,
            detectImageDirectory
        }), HtmlLogUniqueId.LoggingHtml());

        Guard.IsNotNullOrWhiteSpace(fourierPupilCameraAlignmentItem.ROIChannelImageFilePath);
        Guard.IsTrue(fourierPupilCameraAlignmentItem.ROI.ImageROI is { Width: > 0d, Height: > 0d });

        item.Reset();

        var shinyBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.ShinyWaferFindBFMachinePosition);
        var startCurrentShinyBFPosition = CIBViewModel.GetCIBInformationPosition(
            StageCoordinateSystemEnum.Dark,
            Cache.ProductivityInformation,
            CalibrationSetting.SettingCommonParam.MainCIBInformation,
            shinyBFPosition,
            Cache.Item.MicroscopeLensInformation);

        StageViewModel.SetAbsoluteStageTheta(0d);
        StageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(startCurrentShinyBFPosition, CalChipSiteModelEnum.ShinyWaferModel);
        AfViewModel.ToggleDarkFieldEnable(true);

        try
        {
            FourierViewModel.Home(3);

            Grab(0);

            Logger.LogHtmlInformation("Image", HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToImageHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                await item.CalibratingAsync(
                    SelectedYRodItem,
                    FourierCenterChannelFlexibleAperture.XPushItem,
                    (config.CH3_Y_LPOSMin, config.CH3_Y_LPOSMax),
                    (config.CH3_X_PPOSMin, config.CH3_X_PPOSMax),
                    cancellationToken);

                Guard.IsEqualTo(item.YRodROI.IsSelected, SelectedYRodItem is not null);
                if (item.YRodROI.IsSelected)
                {
                    FourierViewModel.SetFFRPOS_CH3(FFCH.Ch3_Y, item.YRodROI.AngleMotorAbsoluteValue);
                    FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_Y, item.YRodROI.MotorAbsoluteValue);
                }

                Guard.IsTrue(item.XPushROI.IsSelected);
                FourierViewModel.SetFFPPOS_CH3(FFCH.Ch3_X, item.XPushROI.MotorAbsoluteValue);

                Grab(1);

                item.Review();

                isSuccess = true;
            }
            finally
            {
                if (isSuccess) Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
                else Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            }

            CalibratingItem.IsCalibrated = true;

            Guard.IsTrue(Save([CalibratingItem], cancellationToken));

            return true;
        }
        finally
        {
            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetBrightFieldAbsoluteStageXy(startCurrentShinyBFPosition, CalChipSiteModelEnum.ShinyWaferModel);
        }

        void Grab(int stepIndex)
        {
            if (HostEnvironment.IsDevelopment())
            {
                FourierViewModel.UseSimulatorImage(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    @"Assets\Data\Fourier",
                    nameof(V0FourierCenterChannelSpecularBlockerViewModel),
                    $"Channel3_Step{stepIndex}.jpg"));
            }

            using var bitmapImage = FourierViewModel.GetImage(
                Cache.ProductivityInformation,
                Cache.Item.LaserLightInformation,
                shinyBFPosition,
                Cache.Item.ScanLength,
                3);
            var imageFilePath = Path.Combine(detectImageDirectory, "Channel3", $"Step{stepIndex}_Fourier_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
            DirectoryHelper.CreateFileDirectoryIfNotExists(imageFilePath);
            bitmapImage.SaveImage(imageFilePath);
            var roiChannelImageFilePath = Path.Combine(Path.GetDirectoryName(imageFilePath) ?? string.Empty, $"{Path.GetFileNameWithoutExtension(imageFilePath)}_ROI_{fourierPupilCameraAlignmentItem.ROI.ImageROI}{Path.GetExtension(imageFilePath)}");

            using var roiBitmapImageDrawable = bitmapImage.ToROI(fourierPupilCameraAlignmentItem.ROI.ImageROI);
            roiBitmapImageDrawable.SaveImage(roiChannelImageFilePath);

            switch (stepIndex)
            {
                case 0:
                    item.Step0FourierImageFilePath = roiChannelImageFilePath;

                    break;

                case 1:
                    item.Step1FourierImageFilePath = roiChannelImageFilePath;

                    break;

                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stepIndex));

                    break;
            }
        }
    }

    private async Task InvokeVerifyAsync(V0FourierCenterChannelSpecularBlockerDTO dto, CancellationToken cancellationToken)
    {
        var detectImageDirectory = ImageFileDirectory;
        V0FourierCenterChannelSpecularBlockerDTOItem[] items = [dto.Channel3Item];
        CIBInformation[] cibInformations =
        [
            ApplicationCookie.CIBInformations.Single(t =>
                t.PMTId == CalibrationSetting.SettingCommonParam.MainCIBInformation.PMTId && t.ChannelId == 3)
        ];

        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
        {
            Cache.ProductivityInformation,
            Cache.VerifyLaserLightInformation,
            VerifyCIBConfiguration = new HtmlQuote(Cache.VerifyCIBConfiguration.ToHtmlAnonymous()),
            Cache.VerifyImageWidth,
            Cache.Item.MicroscopeLensInformation,
            Cache.Item.LaserLightInformation,
            OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
            Cache.Item.ScanLength,
            Cache.Item.ShinyWaferFindBFMachinePosition,
            Cache.ExtinctionRatioThreshold,
            CIBInformations = new HtmlTable([.. cibInformations.Select(t => t.ToHtmlAnonymous())])
        }), HtmlLogUniqueId.LoggingHtml());

        await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.Item.MicroscopeLensInformation, cancellationToken: cancellationToken);
        var shinyBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.ShinyWaferFindBFMachinePosition);
        var startCurrentShinyBFPosition = CIBViewModel.GetCIBInformationPosition(
            StageCoordinateSystemEnum.Dark,
            Cache.ProductivityInformation,
            CalibrationSetting.SettingCommonParam.MainCIBInformation,
            shinyBFPosition,
            Cache.Item.MicroscopeLensInformation);

        StageViewModel.SetAbsoluteStageTheta(0d);
        StageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(startCurrentShinyBFPosition, CalChipSiteModelEnum.ShinyWaferModel);
        AfViewModel.ToggleDarkFieldEnable(true);

        try
        {
            foreach (var item in items)
            {
                item.ResetCIB();

                FourierViewModel.Home(3);
            }

            await GrabAsync(0);

            foreach (var item in items)
            {
                if (item.YRodROI.IsSelected)
                {
                    FourierViewModel.SetFFRPOS_CH3(FFCH.Ch3_Y, item.YRodROI.AngleMotorAbsoluteValue);
                    FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_Y, item.YRodROI.MotorAbsoluteValue);
                }

                Guard.IsTrue(item.XPushROI.IsSelected);
                FourierViewModel.SetFFPPOS_CH3(FFCH.Ch3_X, item.XPushROI.MotorAbsoluteValue);
            }

            await GrabAsync(1);

            foreach (var item in items)
            {
                Guard.IsGreaterThan(item.Step0CIBImageAverageValue, 0d);

                item.ExtinctionRatio = item.Step1CIBImageAverageValue / item.Step0CIBImageAverageValue;

                Logger.LogHtmlInformation("Channel3 Image", HtmlHeaderLevelEnum.Header4, new HtmlQuote(item.ToCIBHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            }

            return;

            async Task GrabAsync(int stepIndex)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (HostEnvironment.IsDevelopment())
                {
                    CIBViewModel.UseSimulatorImages(
                    [
                        Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            @"Assets\Data\Fourier",
                            nameof(V0FourierCenterChannelSpecularBlockerViewModel),
                            $"Channel3_Verify_Step{stepIndex}_CIB.raw")
                    ]);
                }

                var darkFieldImages = await CIBViewModel.GetPMTImagesAsync(
                    Cache.ProductivityInformation,
                    StageCoordinateSystemEnum.Dark,
                    shinyBFPosition,
                    Cache.VerifyImageWidth,
                    cibInformations,
                    (false, CalChipSiteModelEnum.ShinyWaferModel),
                    (false, Cache.Item.OpticsConfiguration),
                    (false, Cache.VerifyCIBConfiguration),
                    (false, Cache.VerifyLaserLightInformation),
                    false,
                    cancellationToken);

                Guard.IsEqualTo(darkFieldImages.Count, items.Length);

                foreach (var item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using var darkFieldImage = darkFieldImages.Single(t => t.CIBInformation.ChannelId == 3);

                    var cibImageFilePath = Path.Combine(detectImageDirectory, "Channel3", $"Verify_Step{stepIndex}_CIB_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                    DirectoryHelper.CreateFileDirectoryIfNotExists(cibImageFilePath);
                    darkFieldImage.Image.SaveImage(cibImageFilePath);

                    switch (stepIndex)
                    {
                        case 0:
                            item.RawStep0CIBImageFilePath = darkFieldImage.RawImageFilePath;
                            item.Step0CIBImageFilePath = cibImageFilePath;
                            item.Step0CIBImageAverageValue = darkFieldImage.Image.GetIntensity().Average;

                            break;

                        case 1:
                            item.RawStep1CIBImageFilePath = darkFieldImage.RawImageFilePath;
                            item.Step1CIBImageFilePath = cibImageFilePath;
                            item.Step1CIBImageAverageValue = darkFieldImage.Image.GetIntensity().Average;

                            break;

                        default:
                            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stepIndex));

                            break;
                    }

                    item.ReviewCIB();
                }
            }
        }
        finally
        {
            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetBrightFieldAbsoluteStageXy(startCurrentShinyBFPosition, CalChipSiteModelEnum.ShinyWaferModel);
        }
    }

    private bool Save(IReadOnlyList<V0FourierCenterChannelSpecularBlockerDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                dto.Clone(),
                .. Calibrations.Where(t => t.ProductivityInformation != dto.ProductivityInformation)
            ];
        }

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<V0FourierCenterChannelSpecularBlockerDTO[]>(calibrations);
        var status = Entry.Status;

        CalibratingStatuses =
        [
            .. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(t => new ProductivityInformationStatus { SelectedItem = t, IsCalibrated = false })
        ];

        Calibrations =
        [
            .. temps
                .Where(t => ApplicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation))
                .DistinctBy(t => t.ProductivityInformation)
                .Select(t =>
                {
                    CalibratingStatuses.Single(tt => tt.SelectedItem == t.ProductivityInformation).IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        status.TotalCalibrationCount = ApplicationCookie.OpticsMagTypeProductivityInformations.Count;
        status.CalibratedCount = Calibrations.Count(t => t.IsCalibrated);
        status.VerifiedCount = Calibrations.Count(t => t.IsVerified);
        status.Details =
        [
            .. ApplicationCookie.OpticsMagTypeProductivityInformations.Select(productivityInformation =>
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