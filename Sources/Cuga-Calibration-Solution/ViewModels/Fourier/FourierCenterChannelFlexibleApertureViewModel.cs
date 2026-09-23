using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Fourier;
using Core.Models.Models.Fourier.CenterChannelFlexibleAperture;
using Core.Models.Models.Fourier.PupilCameraAlignment;
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
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Fourier;

[IOCAppService(ServiceType = typeof(FourierCenterChannelFlexibleApertureViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class FourierCenterChannelFlexibleApertureViewModel : CalibrationViewModelBase<FourierCenterChannelFlexibleApertureCache>
{
    #region 属性

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Param" },
        new() { StepName = "Find Haze Position" },
        new() { StepName = "CH3 X 60 Degree Aperture" },
        new() { StepName = "CH3 X 120 Degree Rod" },
        new() { StepName = "CH3 X 180 Degree Rod" },
        new() { StepName = "CH3 X 240 Degree Rod" },
        new() { StepName = "CH3 X 300 Degree Rod" },
        new() { StepName = "CH3 X Push" },
        new() { StepName = "CH3 Y 60 Degree Aperture" },
        new() { StepName = "CH3 Y 120 Degree Rod" },
        new() { StepName = "CH3 Y 180 Degree Rod" },
        new() { StepName = "CH3 Y 240 Degree Rod" },
        new() { StepName = "CH3 Y 300 Degree Rod" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial FourierCenterChannelFlexibleApertureDTO CalibratingItem { get; set; } = new();

    #endregion Calibrate

    [ObservableProperty]
    public partial FourierCenterChannelFlexibleApertureDTO Review { get; set; } = new();

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial FourierCenterChannelFlexibleApertureCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial FourierCenterChannelFlexibleApertureDTO Calibration { get; set; } = new();

    [ObservableProperty]
    public partial FourierPupilCameraAlignmentDTO FourierPupilCameraAlignment { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        FourierPupilCameraAlignment = ApplicationCookieService.GetCalibration<FourierPupilCameraAlignmentDTO>(cancellationToken);
        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<FourierCenterChannelFlexibleApertureCache>(cancellationToken);
        Calibration = ApplicationCookieService.GetCalibration<FourierCenterChannelFlexibleApertureDTO>(cancellationToken);

        var config = FourierViewModel.GetFourierConfig();
        Cache.XMinMotorAbsoluteValue = config.CH3_X_LPOSMin;
        Cache.XMaxMotorAbsoluteValue = config.CH3_X_LPOSMax;
        Cache.PushMinMotorAbsoluteValue = config.CH3_X_PPOSMin;
        Cache.PushMaxMotorAbsoluteValue = config.CH3_X_PPOSMax;
        Cache.YMinMotorAbsoluteValue = config.CH3_Y_LPOSMin;
        Cache.YMaxMotorAbsoluteValue = config.CH3_Y_LPOSMax;

        UpdateEntryStatus(Calibration, cancellationToken);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Review = Calibration;
        Review.XApertureItem.Review();
        foreach (var item in Review.XRodItems) item.Review();
        Review.XPushItem.Review();
        Review.YApertureItem.Review();
        foreach (var item in Review.YRodItems) item.Review();

        return Review.IsCalibrated;
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
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.HazeFindBFMachinePosition), CalChipSiteModelEnum.HazeModel);

                return true;

            case 3:
                return true;

            case 4:
                return true;

            case 5:
                return true;

            case 6:
                return true;

            case 7:
                return true;

            case 8:
                return true;

            case 9:
                return true;

            case 10:
                return true;

            case 11:
                return true;

            case 12:
                return true;

            default:
                return false;
        }
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                CalibratingItem.Dispose();
                CalibratingItem = new FourierCenterChannelFlexibleApertureDTO();

                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.HazeFindBFMachinePosition != Point.Origin
                        ? Cache.HazeFindBFMachinePosition
                        : MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.HazeModel)), CalChipSiteModelEnum.HazeModel);

                return true;

            case 1:
                return true;

            case 2:
                return true;

            case 3:
                return true;

            case 4:
                return true;

            case 5:
                return true;

            case 6:
                return true;

            case 7:
                return true;

            case 8:
                return true;

            case 9:
                return true;

            case 10:
                return true;

            case 11:
                return true;

            case 12:
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
        Review.Dispose();
        Calibration.Dispose();

        FourierViewModel.UseSimulatorImage(null);

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

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.XMinMotorAbsoluteValue,
                Cache.XMaxMotorAbsoluteValue,
                Cache.PushMinMotorAbsoluteValue,
                Cache.PushMaxMotorAbsoluteValue,
                Cache.YMinMotorAbsoluteValue,
                Cache.YMaxMotorAbsoluteValue,
                Cache.ProductivityInformation,
                Cache.MicroscopeLensInformation,
                Cache.LaserLightInformation,
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.ScanLength
            }), HtmlLogUniqueId.LoggingHtml());

            LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
            LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, Cache.LaserLightInformation.Coefficient);
            LaserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);
            OpticsViewModel.SetOpticsConfiguration(Cache.OpticsConfiguration);

            return ApplicationCookie.ProductivityInformations.Contains(Cache.ProductivityInformation)
                   && ApplicationCookie.MicroscopeLensInformations.Contains(Cache.MicroscopeLensInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.LaserLightInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Guard.IsEqualTo(Cache.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            StageViewModel.SetAbsoluteStageTheta(0d);
            Cache.HazeFindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.XMinMotorAbsoluteValue,
                Cache.XMaxMotorAbsoluteValue,
                Cache.PushMinMotorAbsoluteValue,
                Cache.PushMaxMotorAbsoluteValue,
                Cache.YMinMotorAbsoluteValue,
                Cache.YMaxMotorAbsoluteValue,
                Cache.ProductivityInformation,
                Cache.MicroscopeLensInformation,
                Cache.LaserLightInformation,
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.ScanLength,
                Cache.HazeFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step2Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.XApertureItem, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step3Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.XRodItems[0], cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step4Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.XRodItems[1], cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step5Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.XRodItems[2], cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step6Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.XRodItems[3], cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step7Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.XPushItem, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step8Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.YApertureItem, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step9Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.YRodItems[0], cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step10Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.YRodItems[1], cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step11Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.YRodItems[2], cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step12Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.YRodItems[3], cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(() =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.XMinMotorAbsoluteValue,
                Cache.XMaxMotorAbsoluteValue,
                Cache.PushMinMotorAbsoluteValue,
                Cache.PushMaxMotorAbsoluteValue,
                Cache.YMinMotorAbsoluteValue,
                Cache.YMaxMotorAbsoluteValue,
                Cache.ProductivityInformation,
                Cache.MicroscopeLensInformation,
                Cache.LaserLightInformation,
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.ScanLength,
                Cache.HazeFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlInformation("X Aperture", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Review.XApertureItem.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            foreach (var item in Review.XRodItems) Logger.LogHtmlInformation($"X Rod {item.DefaultAngleMotorAbsoluteValue:0.###}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation("X Push", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Review.XPushItem.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation("Y Aperture", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Review.YApertureItem.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            foreach (var item in Review.YRodItems) Logger.LogHtmlInformation($"Y Rod {item.DefaultAngleMotorAbsoluteValue:0.###}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            Review.IsVerified = true;
            Guard.IsTrue(Save(Review, cancellationToken));

            DialogWindowProvider.ShowDialog("Verify : OK");

            return true;
        }).ConfigureAwait(false);
    }

    private async Task<bool> InvokeCalibrateAsync(FourierCenterChannelFlexibleApertureDTOItem item, CancellationToken cancellationToken)
    {
        var detectImageDirectory = ImageFileDirectory;
        var (minMotorAbsoluteValue, maxMotorAbsoluteValue) = GetMotorRange(item.CenterChannelMotionKindEnum);
        var fourierPupilCameraAlignmentItem = FourierPupilCameraAlignment.Channel3Item;

        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Cache.XMinMotorAbsoluteValue,
            Cache.XMaxMotorAbsoluteValue,
            Cache.PushMinMotorAbsoluteValue,
            Cache.PushMaxMotorAbsoluteValue,
            Cache.YMinMotorAbsoluteValue,
            Cache.YMaxMotorAbsoluteValue,
            Cache.ProductivityInformation,
            Cache.MicroscopeLensInformation,
            Cache.LaserLightInformation,
            OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
            Cache.ScanLength,
            Cache.HazeFindBFMachinePosition,
            item.CenterChannelMotionKindEnum,
            item.DefaultAngleMotorAbsoluteValue,
            item.AngleMotorAbsoluteValue,
            item.Step0MotorAbsoluteValue,
            item.Step1MotorAbsoluteValue,
            minMotorAbsoluteValue,
            maxMotorAbsoluteValue,
            fourierPupilCameraAlignmentItem.ROI.ImageROI,
            detectImageDirectory
        }), HtmlLogUniqueId.LoggingHtml());

        Guard.IsNotNullOrWhiteSpace(fourierPupilCameraAlignmentItem.ROIChannelImageFilePath);
        Guard.IsTrue(fourierPupilCameraAlignmentItem.ROI.ImageROI is { Width: > 0d, Height: > 0d });

        Guard.IsBetweenOrEqualTo(item.AngleMotorAbsoluteValue, item.DefaultAngleMotorAbsoluteValue - 10d, item.DefaultAngleMotorAbsoluteValue + 10d);
        Guard.IsBetweenOrEqualTo(item.Step0MotorAbsoluteValue, minMotorAbsoluteValue, maxMotorAbsoluteValue);
        Guard.IsBetweenOrEqualTo(item.Step1MotorAbsoluteValue, minMotorAbsoluteValue, maxMotorAbsoluteValue);
        Guard.IsNotEqualTo(item.Step0MotorAbsoluteValue, item.Step1MotorAbsoluteValue);

        item.Reset();

        var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.HazeFindBFMachinePosition);
        var startCurrentHazeBFPosition = CIBViewModel.GetCIBInformationPosition(
            StageCoordinateSystemEnum.Dark,
            Cache.ProductivityInformation,
            CalibrationSetting.SettingCommonParam.MainCIBInformation,
            hazeBFPosition,
            Cache.MicroscopeLensInformation);

        StageViewModel.SetAbsoluteStageTheta(0d);
        StageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(startCurrentHazeBFPosition, CalChipSiteModelEnum.HazeModel);
        AfViewModel.ToggleDarkFieldEnable(true);

        try
        {
            SetRods(item.Step0MotorAbsoluteValue);
            Grab(0);

            SetRods(item.Step0MotorAbsoluteValue);
            Grab(1);

            Logger.LogHtmlInformation("Image", HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToImageHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                await item.CalibratingAsync(cancellationToken);

                isSuccess = true;
            }
            finally
            {
                if (isSuccess) Logger.LogHtmlInformation("ROI", HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
                else Logger.LogHtmlError("ROI", HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            }

            if (item is { CenterChannelMotionKindEnum: CenterChannelMotionKindEnum.YRod, DefaultAngleMotorAbsoluteValue: 300d })
            {
                CalibratingItem.IsCalibrated = true;

                Guard.IsTrue(Save(CalibratingItem, cancellationToken));
            }

            return true;
        }
        finally
        {
            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetBrightFieldAbsoluteStageXy(startCurrentHazeBFPosition, CalChipSiteModelEnum.HazeModel);
        }

        (double Min, double Max) GetMotorRange(CenterChannelMotionKindEnum centerChannelMotionKindEnum) => centerChannelMotionKindEnum switch
        {
            CenterChannelMotionKindEnum.XRod or CenterChannelMotionKindEnum.XAperture => (Cache.XMinMotorAbsoluteValue, Cache.XMaxMotorAbsoluteValue),
            CenterChannelMotionKindEnum.YRod or CenterChannelMotionKindEnum.YAperture => (Cache.YMinMotorAbsoluteValue, Cache.YMaxMotorAbsoluteValue),
            CenterChannelMotionKindEnum.XPush => (Cache.PushMinMotorAbsoluteValue, Cache.PushMaxMotorAbsoluteValue),
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<(double Min, double Max)>(nameof(centerChannelMotionKindEnum))
        };

        void SetRods(double motorAbsoluteValue)
        {
            FourierViewModel.Home(3);

            switch (item.CenterChannelMotionKindEnum)
            {
                case CenterChannelMotionKindEnum.XAperture:
                case CenterChannelMotionKindEnum.XRod:
                    FourierViewModel.SetFFRPOS_CH3(FFCH.Ch3_X, item.AngleMotorAbsoluteValue);
                    FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_X, motorAbsoluteValue);

                    break;

                case CenterChannelMotionKindEnum.XPush:
                    FourierViewModel.SetFFPPOS_CH3(FFCH.Ch3_X, motorAbsoluteValue);

                    break;

                case CenterChannelMotionKindEnum.YAperture:
                case CenterChannelMotionKindEnum.YRod:
                    FourierViewModel.SetFFRPOS_CH3(FFCH.Ch3_Y, item.AngleMotorAbsoluteValue);
                    FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_Y, motorAbsoluteValue);

                    break;

                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(item.CenterChannelMotionKindEnum));

                    break;
            }
        }

        void Grab(int stepIndex)
        {
            if (HostEnvironment.IsDevelopment())
            {
                FourierViewModel.UseSimulatorImage(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    @"Assets\Data\Fourier",
                    nameof(FourierCenterChannelFlexibleApertureViewModel),
                    $"Channel3_{item.CenterChannelMotionKindEnum}_{item.DefaultAngleMotorAbsoluteValue:0.###}_Step{stepIndex}.jpg"));
            }

            using var bitmapImage = FourierViewModel.GetImage(
                Cache.ProductivityInformation,
                Cache.LaserLightInformation,
                hazeBFPosition,
                Cache.ScanLength,
                3);
            var imageFilePath = Path.Combine(detectImageDirectory, "Channel3", $"Step{stepIndex}_{item.CenterChannelMotionKindEnum}_{item.DefaultAngleMotorAbsoluteValue:0.###}_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
            DirectoryHelper.CreateFileDirectoryIfNotExists(imageFilePath);
            bitmapImage.SaveImage(imageFilePath);
            var roiChannelImageFilePath = Path.Combine(Path.GetDirectoryName(imageFilePath) ?? string.Empty, $"{Path.GetFileNameWithoutExtension(imageFilePath)}_ROI_{fourierPupilCameraAlignmentItem.ROI.ImageROI}{Path.GetExtension(imageFilePath)}");

            using var roiBitmapImageDrawable = bitmapImage.ToROI(fourierPupilCameraAlignmentItem.ROI.ImageROI);
            roiBitmapImageDrawable.SaveImage(roiChannelImageFilePath);

            switch (stepIndex)
            {
                case 0:
                    item.Step0ChannelImageFilePath = roiChannelImageFilePath;

                    break;

                case 1:
                    item.Step1ChannelImageFilePath = roiChannelImageFilePath;

                    break;

                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stepIndex));

                    break;
            }
        }
    }

    private bool Save(FourierCenterChannelFlexibleApertureDTO dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        ApplicationCookieService.SetCalibration(dto, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase calibration, CancellationToken cancellationToken)
    {
        var temp = Guard.IsAssignableToTypeAndReturn<FourierCenterChannelFlexibleApertureDTO>(calibration);
        var status = Entry.Status;

        Calibration = temp;

        var total = temp.XRodItems.Length + temp.YRodItems.Length + 3;
        status.TotalCalibrationCount = total;
        status.CalibratedCount = Calibration.IsCalibrated ? total : 0;
        status.VerifiedCount = Calibration.IsVerified ? total : 0;
        status.Details = [];
    }

    #endregion 校准
}