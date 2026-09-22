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
using System.Globalization;
using System.IO;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Fourier;

[IOCAppService(ServiceType = typeof(FourierCenterChannelFlexibleApertureViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class FourierCenterChannelFlexibleApertureViewModel : CalibrationViewModelBase<FourierCenterChannelFlexibleApertureCache>
{
    private const int ChannelId = 3;
    private const double LightHoleAngle = 60d;

    #region 属性

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Param" },
        new() { StepName = "Find Haze Position" },
        new() { StepName = "CH3 X 60 Degree Light Hole" },
        new() { StepName = "CH3 X 120 Degree Rod" },
        new() { StepName = "CH3 X 180 Degree Rod" },
        new() { StepName = "CH3 X 240 Degree Rod" },
        new() { StepName = "CH3 X 300 Degree Rod" },
        new() { StepName = "CH3 X Push" },
        new() { StepName = "CH3 Y 60 Degree Light Hole" },
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

    public override string CalibrateDirectoryName => "CenterChannelFlexibleAperture";

    public override string CalibrateFileName => "CenterChannelFlexibleAperture";

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

        Review.Dispose();
        Review = Calibration.Clone();
        Review.XLightHoleItem.Review();
        foreach (var item in Review.XItems) item.Review();
        Review.PushItem.Review();
        Review.YLightHoleItem.Review();
        foreach (var item in Review.YItems) item.Review();

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
            case 4:
            case 5:
            case 6:
            case 7:
            case 8:
            case 9:
            case 10:
            case 11:
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
                // 重新进入校准时只清空结果, 保留界面输入的角度及两步电机位置.
                CalibratingItem.IsCalibrated = false;
                CalibratingItem.IsVerified = false;
                CalibratingItem.XLightHoleItem.Reset();
                CalibratingItem.YLightHoleItem.Reset();
                foreach (var item in CalibratingItem.XItems.Concat(CalibratingItem.YItems).Append(CalibratingItem.PushItem)) item.Reset();

                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.HazeFindBFMachinePosition != Point.Origin
                        ? Cache.HazeFindBFMachinePosition
                        : MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.HazeModel)), CalChipSiteModelEnum.HazeModel);

                return true;

            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
            case 8:
            case 9:
            case 10:
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
            if (!HostEnvironment.IsDevelopment() && FourierPupilCameraAlignment.IsOk == false)
            {
                const string comment = "Fourier Pupil Camera Alignment is not calibrated!";
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment(comment), HtmlLogUniqueId.LoggingHtml());

                DialogWindowProvider.ShowDialog(comment, DialogButtonsEnum.OK, DialogIconEnum.Error);

                return false;
            }

            Guard.IsGreaterThan(Cache.XMaxMotorAbsoluteValue, Cache.XMinMotorAbsoluteValue);
            Guard.IsGreaterThan(Cache.YMaxMotorAbsoluteValue, Cache.YMinMotorAbsoluteValue);
            Guard.IsGreaterThan(Cache.PushMaxMotorAbsoluteValue, Cache.PushMinMotorAbsoluteValue);

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
    private Task<bool> Step2Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateLightHoleAsync(CalibratingItem.XLightHoleItem, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step3Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.XItems[0], 120d, false, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step4Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.XItems[1], 180d, false, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step5Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.XItems[2], 240d, false, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step6Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.XItems[3], 300d, false, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step7Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.PushItem, 0d, false, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step8Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateLightHoleAsync(CalibratingItem.YLightHoleItem, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step9Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.YItems[0], 120d, false, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step10Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.YItems[1], 180d, false, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step11Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.YItems[2], 240d, false, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step12Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.YItems[3], 300d, true, cancellationToken).ConfigureAwait(false));

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

            Logger.LogHtmlInformation("X Light Hole", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Review.XLightHoleItem.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            foreach (var item in Review.XItems) Logger.LogHtmlInformation($"X {item.DefaultAngleMotorAbsoluteValue}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation("Push", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Review.PushItem.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            Logger.LogHtmlInformation("Y Light Hole", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Review.YLightHoleItem.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            foreach (var item in Review.YItems) Logger.LogHtmlInformation($"Y {item.DefaultAngleMotorAbsoluteValue}", HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            Review.IsVerified = true;
            Guard.IsTrue(Save(Review, cancellationToken));

            DialogWindowProvider.ShowDialog("Verify : OK");

            return true;
        }).ConfigureAwait(false);
    }

    private async Task<bool> CalibrateLightHoleAsync(FourierCenterChannelFlexibleApertureLightHoleItem item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Guard.IsTrue(item.CenterChannelMotionKindEnum is CenterChannelMotionKindEnum.X or CenterChannelMotionKindEnum.Y);

        var alignmentItem = FourierPupilCameraAlignment.Channel3Item;
        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            item.CenterChannelMotionKindEnum,
            LightHoleAngle,
            Cache.ProductivityInformation,
            Cache.MicroscopeLensInformation,
            Cache.LaserLightInformation,
            OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
            Cache.ScanLength,
            Cache.HazeFindBFMachinePosition,
            alignmentItem.ImageROI
        }), HtmlLogUniqueId.LoggingHtml());

        if (!HostEnvironment.IsDevelopment())
        {
            Guard.IsNotNullOrWhiteSpace(alignmentItem.ROIChannelImageFilePath);
            Guard.IsTrue(alignmentItem.ImageROI is { Width: > 0d, Height: > 0d });
        }

        CalibratingItem.IsCalibrated = false;
        CalibratingItem.IsVerified = false;
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
            cancellationToken.ThrowIfCancellationRequested();
            var axis = item.CenterChannelMotionKindEnum == CenterChannelMotionKindEnum.X ? FFCH.Ch3_X : FFCH.Ch3_Y;
            FourierViewModel.Home(ChannelId);
            FourierViewModel.SetFFRPOS_CH3(axis, LightHoleAngle);
            FourierViewModel.SetFFLPOS_CH3(axis, 0d);
            FourierViewModel.SetFFPPOS_CH3(FFCH.Ch3_X, 0d);

            cancellationToken.ThrowIfCancellationRequested();
            item.ChannelImageFilePath = CaptureRoiImage(hazeBFPosition, alignmentItem, $"Channel3_{item.CenterChannelMotionKindEnum}_LightHole_Step0.jpg", $"{item.CenterChannelMotionKindEnum}/LightHole");
            cancellationToken.ThrowIfCancellationRequested();
            Logger.LogHtmlInformation("Image", HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToImageHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                await item.CalibratingAsync(CalibrationAlgorithmService.FitCircle, cancellationToken);

                isSuccess = true;
            }
            finally
            {
                if (isSuccess) Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
                else Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            }

            return true;
        }
        finally
        {
            if (HostEnvironment.IsDevelopment()) FourierViewModel.UseSimulatorImage(null);

            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetBrightFieldAbsoluteStageXy(startCurrentHazeBFPosition, CalChipSiteModelEnum.HazeModel);
        }
    }

    private async Task<bool> CalibrateMotionAsync(
        FourierCenterChannelFlexibleApertureMotionItem item,
        double expectedDefaultAngle,
        bool saveWhenSuccess,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Guard.IsEqualTo(item.DefaultAngleMotorAbsoluteValue, expectedDefaultAngle);

        var (minMotorAbsoluteValue, maxMotorAbsoluteValue) = GetMotorRange(item.CenterChannelMotionKindEnum);
        var alignmentItem = FourierPupilCameraAlignment.Channel3Item;
        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            item.CenterChannelMotionKindEnum,
            item.DefaultAngleMotorAbsoluteValue,
            item.AngleMotorAbsoluteValue,
            item.Step0MotorAbsoluteValue,
            item.Step1MotorAbsoluteValue,
            minMotorAbsoluteValue,
            maxMotorAbsoluteValue,
            Cache.ProductivityInformation,
            Cache.MicroscopeLensInformation,
            Cache.LaserLightInformation,
            OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
            Cache.ScanLength,
            Cache.HazeFindBFMachinePosition,
            alignmentItem.ImageROI
        }), HtmlLogUniqueId.LoggingHtml());

        if (!HostEnvironment.IsDevelopment())
        {
            Guard.IsNotNullOrWhiteSpace(alignmentItem.ROIChannelImageFilePath);
            Guard.IsTrue(alignmentItem.ImageROI is { Width: > 0d, Height: > 0d });
        }
        Guard.IsBetweenOrEqualTo(item.Step0MotorAbsoluteValue, minMotorAbsoluteValue, maxMotorAbsoluteValue);
        Guard.IsBetweenOrEqualTo(item.Step1MotorAbsoluteValue, minMotorAbsoluteValue, maxMotorAbsoluteValue);
        Guard.IsNotEqualTo(item.Step0MotorAbsoluteValue, item.Step1MotorAbsoluteValue);
        Guard.IsTrue(double.IsFinite(item.AngleMotorAbsoluteValue));

        CalibratingItem.IsCalibrated = false;
        CalibratingItem.IsVerified = false;
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
            var angleName = item.AngleMotorAbsoluteValue.ToString(CultureInfo.InvariantCulture);
            cancellationToken.ThrowIfCancellationRequested();
            ApplyMotion(item, item.Step0MotorAbsoluteValue);
            cancellationToken.ThrowIfCancellationRequested();
            item.Step0ChannelImageFilePath = CaptureRoiImage(
                hazeBFPosition,
                alignmentItem,
                HostEnvironment.IsDevelopment() ? GetSimulatorMotionFileName(item, item.Step0MotorAbsoluteValue) : string.Empty,
                $"{item.CenterChannelMotionKindEnum}/{angleName}_Step0");

            cancellationToken.ThrowIfCancellationRequested();
            ApplyMotion(item, item.Step1MotorAbsoluteValue);
            cancellationToken.ThrowIfCancellationRequested();
            item.Step1ChannelImageFilePath = CaptureRoiImage(
                hazeBFPosition,
                alignmentItem,
                HostEnvironment.IsDevelopment() ? GetSimulatorMotionFileName(item, item.Step1MotorAbsoluteValue) : string.Empty,
                $"{item.CenterChannelMotionKindEnum}/{angleName}_Step1");

            cancellationToken.ThrowIfCancellationRequested();
            Logger.LogHtmlInformation("Image", HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToImageHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                await item.CalibratingAsync(cancellationToken);

                isSuccess = true;
            }
            finally
            {
                if (isSuccess) Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
                else Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
            }

            if (saveWhenSuccess)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Guard.IsTrue(!CalibratingItem.XLightHoleItem.Circle.IsEmpty && !CalibratingItem.YLightHoleItem.Circle.IsEmpty);
                foreach (var motionItem in CalibratingItem.XItems.Concat(CalibratingItem.YItems).Append(CalibratingItem.PushItem))
                {
                    Guard.IsTrue(double.IsFinite(motionItem.PixelSize) && motionItem.PixelSize != 0d);
                    Guard.IsTrue(motionItem.Step0FindItem.ImageROI is { Width: > 0d, Height: > 0d });
                    Guard.IsTrue(motionItem.Step1FindItem.ImageROI is { Width: > 0d, Height: > 0d });
                }

                CalibratingItem.IsCalibrated = true;
                CalibratingItem.IsVerified = false;
                Guard.IsTrue(Save(CalibratingItem, cancellationToken));
            }

            return true;
        }
        finally
        {
            if (HostEnvironment.IsDevelopment()) FourierViewModel.UseSimulatorImage(null);

            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetBrightFieldAbsoluteStageXy(startCurrentHazeBFPosition, CalChipSiteModelEnum.HazeModel);
        }
    }

    private string CaptureRoiImage(
        Point hazeBFPosition,
        FourierPupilCameraAlignmentDTOItem alignmentItem,
        string simulatorFileName,
        string imageName)
    {
        if (HostEnvironment.IsDevelopment())
        {
            var simulatorImageFilePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"Assets\Data\Fourier",
                nameof(FourierCenterChannelFlexibleApertureViewModel),
                simulatorFileName);
            if (!File.Exists(simulatorImageFilePath))
                throw new FileNotFoundException("No recorded simulator image matches the selected angle and motor position.", simulatorImageFilePath);
            FourierViewModel.UseSimulatorImage(simulatorImageFilePath);
        }

        using var bitmapImage = FourierViewModel.GetImage(
            Cache.ProductivityInformation,
            Cache.LaserLightInformation,
            hazeBFPosition,
            Cache.ScanLength,
            ChannelId);
        var imageFilePath = Path.Combine(ImageFileDirectory, $"{imageName}_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
        DirectoryHelper.CreateFileDirectoryIfNotExists(imageFilePath);
        bitmapImage.SaveImage(imageFilePath);

        // 日志图片已是原手动流程裁出的通道图, 回放时不能再次按当前相机校准ROI裁剪.
        if (HostEnvironment.IsDevelopment()) return imageFilePath;

        var roiChannelImageFilePath = Path.Combine(
            Path.GetDirectoryName(imageFilePath) ?? string.Empty,
            $"{Path.GetFileNameWithoutExtension(imageFilePath)}_ROI_{alignmentItem.ImageROI}{Path.GetExtension(imageFilePath)}");
        using var roiBitmapImage = bitmapImage.ToROI(alignmentItem.ImageROI);
        roiBitmapImage.SaveImage(roiChannelImageFilePath);

        return roiChannelImageFilePath;
    }

    private static string GetSimulatorMotionFileName(FourierCenterChannelFlexibleApertureMotionItem item, double motorAbsoluteValue)
    {
        // 文件按步骤命名, 仍按实际角度和电机位置匹配记录样本, 避免互换位置后取错图片.
        var stepIndex = (item.CenterChannelMotionKindEnum, item.AngleMotorAbsoluteValue, motorAbsoluteValue) switch
        {
            (CenterChannelMotionKindEnum.X, 120d, -6d) => 0,
            (CenterChannelMotionKindEnum.X, 120d, -8d) => 1,
            (CenterChannelMotionKindEnum.X, 180d or 240d or 300d, -8d) => 0,
            (CenterChannelMotionKindEnum.X, 180d or 240d or 300d, -10d) => 1,
            (CenterChannelMotionKindEnum.Y, 120d, 2d) => 0,
            (CenterChannelMotionKindEnum.Y, 120d, 4d) => 1,
            (CenterChannelMotionKindEnum.Y, 180d, 4d) => 0,
            (CenterChannelMotionKindEnum.Y, 180d, 6d) => 1,
            (CenterChannelMotionKindEnum.Y, 240d, 6d) => 0,
            (CenterChannelMotionKindEnum.Y, 240d, 8d) => 1,
            (CenterChannelMotionKindEnum.Y, 300d, 8d) => 0,
            (CenterChannelMotionKindEnum.Y, 300d, 10d) => 1,
            (CenterChannelMotionKindEnum.Push, 0d, 42d) => 0,
            (CenterChannelMotionKindEnum.Push, 0d, 43d) => 1,
            (CenterChannelMotionKindEnum.Push, 300d, 2d) => 0,
            (CenterChannelMotionKindEnum.Push, 300d, 42d) => 1,
            (CenterChannelMotionKindEnum.Push, 300d, 43d) => 2,
            _ => ThrowHelper.ThrowInvalidOperationException<int>($"No recorded simulator image matches {item.CenterChannelMotionKindEnum}, angle {item.AngleMotorAbsoluteValue}, motor position {motorAbsoluteValue}.")
        };
        var angle = item.AngleMotorAbsoluteValue.ToString(CultureInfo.InvariantCulture);

        return $"Channel3_{item.CenterChannelMotionKindEnum}_{angle}_Step{stepIndex}.jpg";
    }

    private void ApplyMotion(FourierCenterChannelFlexibleApertureMotionItem item, double motorAbsoluteValue)
    {
        FourierViewModel.Home(ChannelId);

        switch (item.CenterChannelMotionKindEnum)
        {
            case CenterChannelMotionKindEnum.X:
                FourierViewModel.SetFFRPOS_CH3(FFCH.Ch3_X, item.AngleMotorAbsoluteValue);
                FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_X, motorAbsoluteValue);
                FourierViewModel.SetFFPPOS_CH3(FFCH.Ch3_X, 0d);

                break;

            case CenterChannelMotionKindEnum.Y:
                FourierViewModel.SetFFRPOS_CH3(FFCH.Ch3_Y, item.AngleMotorAbsoluteValue);
                FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_Y, motorAbsoluteValue);
                FourierViewModel.SetFFPPOS_CH3(FFCH.Ch3_X, 0d);

                break;

            case CenterChannelMotionKindEnum.Push:
                FourierViewModel.SetFFRPOS_CH3(FFCH.Ch3_X, item.AngleMotorAbsoluteValue);
                FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_X, 0d);
                FourierViewModel.SetFFPPOS_CH3(FFCH.Ch3_X, motorAbsoluteValue);

                break;

            default:
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(item.CenterChannelMotionKindEnum));

                break;
        }
    }

    private (double Min, double Max) GetMotorRange(CenterChannelMotionKindEnum centerChannelMotionKindEnum) => centerChannelMotionKindEnum switch
    {
        CenterChannelMotionKindEnum.X => (Cache.XMinMotorAbsoluteValue, Cache.XMaxMotorAbsoluteValue),
        CenterChannelMotionKindEnum.Y => (Cache.YMinMotorAbsoluteValue, Cache.YMaxMotorAbsoluteValue),
        CenterChannelMotionKindEnum.Push => (Cache.PushMinMotorAbsoluteValue, Cache.PushMaxMotorAbsoluteValue),
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<(double Min, double Max)>(nameof(centerChannelMotionKindEnum))
    };

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

        var total = temp.XItems.Length + temp.YItems.Length + 3;
        status.TotalCalibrationCount = total;
        status.CalibratedCount = Calibration.IsCalibrated ? total : 0;
        status.VerifiedCount = Calibration.IsVerified ? total : 0;
        status.Details = [];
    }

    #endregion 校准
}