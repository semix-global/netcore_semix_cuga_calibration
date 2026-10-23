using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Fourier.PupilCameraAlignment;
using Core.Models.Models.Fourier.CenterChannelFlexibleAperture;
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
        new() { StepName = $"CH3 X {360d / 11d * 9d:0.###} Degree Aperture" },
        new() { StepName = $"CH3 X {360d / 11d * 1d:0.###} Degree X Rod" },
        new() { StepName = $"CH3 X {360d / 11d * 2d:0.###} Degree X Rod" },
        new() { StepName = $"CH3 X {360d / 11d * 3d:0.###} Degree X Rod" },
        new() { StepName = $"CH3 X {360d / 11d * 4d:0.###} Degree X Rod" },
        new() { StepName = "CH3 X Push" },
        new() { StepName = $"CH3 X {360d / 11d * 8d:0.###} Degree Calibration Pattern" },
        new() { StepName = $"CH3 X {360d / 11d * 5d:0.###} Degree Y Rod" },
        new() { StepName = $"CH3 X {360d / 11d * 6d:0.###} Degree Y Rod" },
        new() { StepName = $"CH3 X {360d / 11d * 7d:0.###} Degree Y Rod" },
        new() { StepName = $"CH3 X {360d / 11d * 10d:0.###} Degree Y Rod" },
        new() { StepName = "CH3 X Diffraction Coarse Push" },
        new() { StepName = "CH3 X Diffraction Fine Push" }
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

        var configuration = FourierViewModel.GetMotorConfiguration();
        Cache.XMinMotorAbsoluteValue = configuration.CenterXMinMotorAbsoluteValue;
        Cache.XMaxMotorAbsoluteValue = configuration.CenterXMaxMotorAbsoluteValue;
        Cache.PushMinMotorAbsoluteValue = configuration.CenterPushMinMotorAbsoluteValue;
        Cache.PushMaxMotorAbsoluteValue = configuration.CenterPushMaxMotorAbsoluteValue;
        Cache.DiffractionPushMinMotorAbsoluteValue = configuration.CenterDiffractionPushMinMotorAbsoluteValue;
        Cache.DiffractionPushMaxMotorAbsoluteValue = configuration.CenterDiffractionPushMaxMotorAbsoluteValue;

        Guard.IsGreaterThan(Cache.XMaxMotorAbsoluteValue, Cache.XMinMotorAbsoluteValue);
        Guard.IsGreaterThan(Cache.PushMaxMotorAbsoluteValue, Cache.PushMinMotorAbsoluteValue);
        if (double.IsFinite(Cache.DiffractionPushMinMotorAbsoluteValue) == false
            || double.IsFinite(Cache.DiffractionPushMaxMotorAbsoluteValue) == false
            || Cache.DiffractionPushMaxMotorAbsoluteValue <= Cache.DiffractionPushMinMotorAbsoluteValue)
        {
            ThrowHelper.ThrowNotSupportedException("衍射推杆行程范围不可用, 当前 SDK 尚未提供有效配置.");
        }

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
        Review.XCalibrationPatternItem.Review();
        foreach (var item in Review.YRodItems) item.Review();
        Review.XDiffractionCoarsePushItem.Review();
        Review.XDiffractionFinePushItem.Review();

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

            case 13:
                return true;

            case 14:
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
                return true;

            case 13:
                return true;

            case 14:
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

        if (HostEnvironment.IsDevelopment()) FourierViewModel.UseSimulatorImage(null);

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
                Cache.DiffractionPushMinMotorAbsoluteValue,
                Cache.DiffractionPushMaxMotorAbsoluteValue,
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
                Cache.DiffractionPushMinMotorAbsoluteValue,
                Cache.DiffractionPushMaxMotorAbsoluteValue,
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
    private Task<bool> Step8Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.XCalibrationPatternItem, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step9Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.YRodItems[0], cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step10Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.YRodItems[1], cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step11Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.YRodItems[2], cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step12Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.YRodItems[3], cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step13Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.XDiffractionCoarsePushItem, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step14Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await InvokeCalibrateAsync(CalibratingItem.XDiffractionFinePushItem, cancellationToken).ConfigureAwait(false));

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
                Cache.DiffractionPushMinMotorAbsoluteValue,
                Cache.DiffractionPushMaxMotorAbsoluteValue,
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
            Logger.LogHtmlInformation("X Calibration Pattern", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Review.XCalibrationPatternItem.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            foreach (var item in Review.YRodItems) Logger.LogHtmlInformation($"Y Rod {item.DefaultAngleMotorAbsoluteValue:0.###}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation("X Diffraction Coarse Push", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Review.XDiffractionCoarsePushItem.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation("X Diffraction Fine Push", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Review.XDiffractionFinePushItem.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

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
            Cache.DiffractionPushMinMotorAbsoluteValue,
            Cache.DiffractionPushMaxMotorAbsoluteValue,
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

        Guard.IsNotNullOrWhiteSpace(fourierPupilCameraAlignmentItem.ROIFourierImageFilePath);
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
            try
            {
                FourierViewModel.Home(3, false);

                SetRods(item.Step0MotorAbsoluteValue);
                Grab(0);
                SetRods(item.Step1MotorAbsoluteValue);
                Grab(1);
            }
            finally
            {
                Logger.LogHtmlInformation("Image", HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToImageHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            }

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

            if (ReferenceEquals(item, CalibratingItem.XDiffractionFinePushItem))
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
            CenterChannelMotionKindEnum.XRod or CenterChannelMotionKindEnum.YRod or CenterChannelMotionKindEnum.XAperture or CenterChannelMotionKindEnum.XCalibrationPattern => (Cache.XMinMotorAbsoluteValue, Cache.XMaxMotorAbsoluteValue),
            CenterChannelMotionKindEnum.XPush => (Cache.PushMinMotorAbsoluteValue, Cache.PushMaxMotorAbsoluteValue),
            CenterChannelMotionKindEnum.XDiffractionCoarsePush or CenterChannelMotionKindEnum.XDiffractionFinePush => (Cache.DiffractionPushMinMotorAbsoluteValue, Cache.DiffractionPushMaxMotorAbsoluteValue),
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<(double Min, double Max)>(nameof(centerChannelMotionKindEnum))
        };

        void SetRods(double motorAbsoluteValue)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (item.CenterChannelMotionKindEnum)
            {
                case CenterChannelMotionKindEnum.XAperture:
                case CenterChannelMotionKindEnum.XCalibrationPattern:
                case CenterChannelMotionKindEnum.XRod:
                case CenterChannelMotionKindEnum.YRod:
                    FourierViewModel.SetChannel3XPush(0d);
                    FourierViewModel.SetChannel3XDiffractionPush(0d);
                    FourierViewModel.SetChannel3XTurntable(item.AngleMotorAbsoluteValue, motorAbsoluteValue);

                    break;

                case CenterChannelMotionKindEnum.XPush:
                    FourierViewModel.SetChannel3XTurntable(0d, 0d);
                    FourierViewModel.SetChannel3XDiffractionPush(0d);
                    FourierViewModel.SetChannel3XPush(motorAbsoluteValue);

                    break;

                case CenterChannelMotionKindEnum.XDiffractionCoarsePush:
                case CenterChannelMotionKindEnum.XDiffractionFinePush:
                    FourierViewModel.SetChannel3XTurntable(0d, 0d);
                    FourierViewModel.SetChannel3XPush(0d);
                    FourierViewModel.SetChannel3XDiffractionPush(motorAbsoluteValue);

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
                // 复用既有示例图片, 避免新角度或图案名称找不到文件后退回随机图像.
                var simulatorImageName = item.CenterChannelMotionKindEnum switch
                {
                    CenterChannelMotionKindEnum.XAperture or CenterChannelMotionKindEnum.XCalibrationPattern => "XAperture_60",
                    CenterChannelMotionKindEnum.XRod => $"XRod_{120 + Array.IndexOf(CalibratingItem.XRodItems, item) * 60}",
                    CenterChannelMotionKindEnum.YRod => $"YRod_{120 + Array.IndexOf(CalibratingItem.YRodItems, item) * 60}",
                    CenterChannelMotionKindEnum.XPush or CenterChannelMotionKindEnum.XDiffractionCoarsePush or CenterChannelMotionKindEnum.XDiffractionFinePush => "XPush_0",
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(item.CenterChannelMotionKindEnum))
                };
                FourierViewModel.UseSimulatorImage(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    @"Assets\Data\Fourier",
                    nameof(FourierCenterChannelFlexibleApertureViewModel),
                    $"Channel3_{simulatorImageName}_Step{stepIndex}.jpg"));
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
            var roiFourierImageFilePath = Path.Combine(Path.GetDirectoryName(imageFilePath) ?? string.Empty, $"{Path.GetFileNameWithoutExtension(imageFilePath)}_ROI_{fourierPupilCameraAlignmentItem.ROI.ImageROI}{Path.GetExtension(imageFilePath)}");

            using var roiBitmapImageDrawable = bitmapImage.ToROI(fourierPupilCameraAlignmentItem.ROI.ImageROI);
            roiBitmapImageDrawable.SaveImage(roiFourierImageFilePath);

            switch (stepIndex)
            {
                case 0:
                    item.Step0FourierImageFilePath = roiFourierImageFilePath;

                    break;

                case 1:
                    item.Step1FourierImageFilePath = roiFourierImageFilePath;

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

        var total = temp.XRodItems.Length + temp.YRodItems.Length + 5;
        status.TotalCalibrationCount = total;
        status.CalibratedCount = Calibration.IsCalibrated ? total : 0;
        status.VerifiedCount = Calibration.IsVerified ? total : 0;
        status.Details = [];
    }

    #endregion 校准
}