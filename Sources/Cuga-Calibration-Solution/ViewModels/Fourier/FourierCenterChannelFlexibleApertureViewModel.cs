using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Fourier;
using Core.Models.Models.Fourier.CenterChannelFlexibleAperture;
using Core.Models.Models.Fourier.PupilCameraAlignment;
using Core.Models.Models.Microscope.CalChip;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using System.IO;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Fourier;

[IOCAppService(ServiceType = typeof(FourierCenterChannelFlexibleApertureViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class FourierCenterChannelFlexibleApertureViewModel : CalibrationViewModelBase<FourierCenterChannelFlexibleApertureCache>
{
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

    [ObservableProperty]
    public partial FourierCenterChannelFlexibleApertureDTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial FourierCenterChannelFlexibleApertureDTO Review { get; set; } = new();

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

    public override string CalibrateDirectoryName => "CenterChannelFlexibleAperture";

    public override string CalibrateFileName => "CenterChannelFlexibleAperture";

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        FourierPupilCameraAlignment = ApplicationCookieService.GetCalibration<FourierPupilCameraAlignmentDTO>(cancellationToken);
        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);
        Cache = ApplicationCookieService.GetCache<FourierCenterChannelFlexibleApertureCache>(cancellationToken);
        Calibration = ApplicationCookieService.GetCalibration<FourierCenterChannelFlexibleApertureDTO>(cancellationToken);

        if (Cache.PositionErrorThreshold <= 0d) Cache.PositionErrorThreshold = 1d;
        if (Cache.CircleCenterErrorThreshold <= 0d) Cache.CircleCenterErrorThreshold = 1d;
        if (Cache.CircleRadiusErrorThreshold <= 0d) Cache.CircleRadiusErrorThreshold = 1d;

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

        Review = Calibration.Clone();
        Review.XLightHoleItem.Review();
        Review.YLightHoleItem.Review();
        foreach (var item in Review.MotionItems) item.Review();

        return Review.IsCalibrated;
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (CalibrationStepIndex == 2)
        {
            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.HazeFindBFMachinePosition), CalChipSiteModelEnum.HazeModel);
        }

        return true;
    }

    protected override async Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        switch (CalibrationStepIndex)
        {
            case 0:
                CalibratingItem.Dispose();
                CalibratingItem = new FourierCenterChannelFlexibleApertureDTO();
                SetDefaultMotionPositions(CalibratingItem);
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.MicroscopeLensInformation, cancellationToken: cancellationToken);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.HazeFindBFMachinePosition != Point.Origin
                        ? Cache.HazeFindBFMachinePosition
                        : MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.HazeModel)), CalChipSiteModelEnum.HazeModel);

                return true;

            case 12:
                Guard.IsTrue(CalibratingItem.IsComplete);
                CalibratingItem.IsCalibrated = true;
                CalibratingItem.IsVerified = false;
                Guard.IsTrue(Save(CalibratingItem, cancellationToken));
                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

                return true;

            default:
                return true;
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

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            if (FourierPupilCameraAlignment.IsOk == false)
            {
                DialogWindowProvider.ShowDialog("Fourier Pupil Camera Alignment is not calibrated.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            Guard.IsGreaterThan(Cache.XMaxMotorAbsoluteValue, Cache.XMinMotorAbsoluteValue);
            Guard.IsGreaterThan(Cache.YMaxMotorAbsoluteValue, Cache.YMinMotorAbsoluteValue);
            Guard.IsGreaterThan(Cache.PushMaxMotorAbsoluteValue, Cache.PushMinMotorAbsoluteValue);
            Guard.IsTrue(ApplicationCookie.ProductivityInformations.Contains(Cache.ProductivityInformation));
            Guard.IsTrue(ApplicationCookie.MicroscopeLensInformations.Contains(Cache.MicroscopeLensInformation));
            Guard.IsTrue(ApplicationCookie.LaserLightInformations.Contains(Cache.LaserLightInformation));

            LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
            LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, Cache.LaserLightInformation.Coefficient);
            LaserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);
            OpticsViewModel.SetOpticsConfiguration(Cache.OpticsConfiguration);

            return true;
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

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step2Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateLightHoleAsync(CalibratingItem.XLightHoleItem, FFCH.Ch3_X, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step3Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.X120Item, FFCH.Ch3_X, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step4Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.X180Item, FFCH.Ch3_X, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step5Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.X240Item, FFCH.Ch3_X, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step6Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.X300Item, FFCH.Ch3_X, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step7Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.PushItem, FFCH.Ch3_X, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step8Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateLightHoleAsync(CalibratingItem.YLightHoleItem, FFCH.Ch3_Y, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step9Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.Y120Item, FFCH.Ch3_Y, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step10Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.Y180Item, FFCH.Ch3_Y, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step11Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.Y240Item, FFCH.Ch3_Y, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step12Async(CancellationToken cancellationToken) => InvokeCalibrateAsync(async () => await CalibrateMotionAsync(CalibratingItem.Y300Item, FFCH.Ch3_Y, cancellationToken).ConfigureAwait(false));

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(() =>
        {
            Guard.IsTrue(Calibration.IsCalibrated);
            Guard.IsGreaterThan(Cache.PositionErrorThreshold, 0d);
            Guard.IsGreaterThan(Cache.CircleCenterErrorThreshold, 0d);
            Guard.IsGreaterThan(Cache.CircleRadiusErrorThreshold, 0d);
            Guard.IsTrue(Calibration.IsComplete);

            Review = Calibration.Clone();
            Review.XLightHoleItem.Review();
            Review.YLightHoleItem.Review();
            foreach (var item in Review.MotionItems) item.Review();

            Review.IsVerified = true;
            Guard.IsTrue(Save(Review, cancellationToken));
            DialogWindowProvider.ShowDialog("Verify : OK");

            return true;
        }).ConfigureAwait(false);
    }

    private async Task<bool> CalibrateLightHoleAsync(FourierCenterChannelFlexibleApertureLightHoleItem item, FFCH axis, CancellationToken cancellationToken)
    {
        var (hazeBFPosition, startCurrentPosition) = PrepareHazePosition();

        try
        {
            FourierViewModel.Home(3);
            FourierViewModel.SetFFRPOS_CH3(axis, 60d);
            FourierViewModel.SetFFLPOS_CH3(axis, 0d);
            FourierViewModel.SetFFPPOS_CH3(FFCH.Ch3_X, 0d);

            var hasPoints = item.Points.Length == 4
                            && item.Points.Any(t => t != Point.Origin)
                            && item.Points.All(t => double.IsFinite(t.X) && double.IsFinite(t.Y));
            if (string.IsNullOrWhiteSpace(item.ChannelImageFilePath) || item.IsCalculated == false || hasPoints == false)
            {
                item.ChannelImageFilePath = CaptureImage(hazeBFPosition, axis == FFCH.Ch3_X ? "X60" : "Y60");
                await item.CalibratingAsync(cancellationToken).ConfigureAwait(false);
            }

            if (item.Points.Length != 4 || item.Points.Any(t => double.IsFinite(t.X) == false || double.IsFinite(t.Y) == false)) return false;

            item.Calculate(CalibrationAlgorithmService.FitCircle);
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new { item.Axis, item.Center, item.Radius }), HtmlLogUniqueId.LoggingHtml());

            return item.IsCalculated;
        }
        finally
        {
            RestoreHazePosition(startCurrentPosition);
        }
    }

    private async Task<bool> CalibrateMotionAsync(FourierCenterChannelFlexibleApertureMotionItem item, FFCH axis, CancellationToken cancellationToken)
    {
        var (hazeBFPosition, startCurrentPosition) = PrepareHazePosition();

        try
        {
            if (item.IsFirstCompleted == false)
            {
                ApplyMotion(item, axis, item.FirstPosition);
                var imageFilePath = CaptureImage(hazeBFPosition, $"{item.Kind}_{item.NominalAngle ?? 0d}_First");
                await item.FirstAsync(imageFilePath, item.ActualAngle, item.FirstPosition, cancellationToken).ConfigureAwait(false);
                Logger.LogHtmlInformation("First ROI", HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

                return false;
            }

            ApplyMotion(item, axis, item.SecondPosition);
            var secondImageFilePath = CaptureImage(hazeBFPosition, $"{item.Kind}_{item.NominalAngle ?? 0d}_Second");
            await item.SecondAsync(secondImageFilePath, item.SecondPosition, cancellationToken).ConfigureAwait(false);
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            return item.IsSecondCompleted;
        }
        finally
        {
            RestoreHazePosition(startCurrentPosition);
        }
    }

    private (Point HazeBFPosition, Point StartCurrentPosition) PrepareHazePosition()
    {
        var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.HazeFindBFMachinePosition);
        var startCurrentPosition = CIBViewModel.GetCIBInformationPosition(
            StageCoordinateSystemEnum.Dark,
            Cache.ProductivityInformation,
            CalibrationSetting.SettingCommonParam.MainCIBInformation,
            hazeBFPosition,
            Cache.MicroscopeLensInformation);

        StageViewModel.SetAbsoluteStageTheta(0d);
        StageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(startCurrentPosition, CalChipSiteModelEnum.HazeModel);
        AfViewModel.ToggleDarkFieldEnable(true);

        return (hazeBFPosition, startCurrentPosition);
    }

    private void RestoreHazePosition(Point startCurrentPosition)
    {
        StageViewModel.SetAbsoluteStageTheta(0d);
        StageViewModel.SetBrightFieldAbsoluteStageXy(startCurrentPosition, CalChipSiteModelEnum.HazeModel);
        FourierViewModel.UseSimulatorImage(null);
    }

    private string CaptureImage(Point hazeBFPosition, string name)
    {
        var imageFilePath = Path.Combine(ImageFileDirectory, name, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
        DirectoryHelper.CreateFileDirectoryIfNotExists(imageFilePath);

        using var bitmapImage = FourierViewModel.GetImage(Cache.ProductivityInformation, Cache.LaserLightInformation, hazeBFPosition, Cache.ScanLength, 3);
        var alignmentItem = FourierPupilCameraAlignment.Channel3Item;
        Guard.IsTrue(alignmentItem.ImageROI is { Width: > 0d, Height: > 0d });
        using var roiBitmapImage = bitmapImage.ToROI(alignmentItem.ImageROI);
        var roiImageFilePath = Path.Combine(
            Path.GetDirectoryName(imageFilePath) ?? string.Empty,
            $"{Path.GetFileNameWithoutExtension(imageFilePath)}_ROI_{alignmentItem.ImageROI}{Path.GetExtension(imageFilePath)}");
        roiBitmapImage.SaveImage(roiImageFilePath);

        return roiImageFilePath;
    }

    private void ApplyMotion(FourierCenterChannelFlexibleApertureMotionItem item, FFCH axis, double position)
    {
        FourierViewModel.Home(3);

        switch (item.Kind)
        {
            case CenterChannelMotionKindEnum.X:
                FourierViewModel.SetFFRPOS_CH3(FFCH.Ch3_X, (item.IsFirstCompleted ? item.FirstSampleAngle : item.ActualAngle) ?? item.NominalAngle ?? 0d);
                FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_X, position);
                FourierViewModel.SetFFPPOS_CH3(FFCH.Ch3_X, 0d);

                break;

            case CenterChannelMotionKindEnum.Y:
                FourierViewModel.SetFFRPOS_CH3(FFCH.Ch3_Y, (item.IsFirstCompleted ? item.FirstSampleAngle : item.ActualAngle) ?? item.NominalAngle ?? 0d);
                FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_Y, position);
                FourierViewModel.SetFFPPOS_CH3(FFCH.Ch3_X, 0d);

                break;

            case CenterChannelMotionKindEnum.Push:
                FourierViewModel.SetFFRPOS_CH3(FFCH.Ch3_X, 0d);
                FourierViewModel.SetFFLPOS_CH3(FFCH.Ch3_X, 0d);
                FourierViewModel.SetFFPPOS_CH3(FFCH.Ch3_X, position);

                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void SetDefaultMotionPositions(FourierCenterChannelFlexibleApertureDTO dto)
    {
        foreach (var item in dto.XItems)
        {
            item.FirstPosition = Cache.XMinMotorAbsoluteValue;
            item.SecondPosition = Cache.XMaxMotorAbsoluteValue;
        }

        foreach (var item in dto.YItems)
        {
            item.FirstPosition = Cache.YMinMotorAbsoluteValue;
            item.SecondPosition = Cache.YMaxMotorAbsoluteValue;
        }

        dto.PushItem.FirstPosition = Cache.PushMinMotorAbsoluteValue;
        dto.PushItem.SecondPosition = Cache.PushMaxMotorAbsoluteValue;
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
        Calibration = temp;
        Entry.Status.TotalCalibrationCount = CalibrationSteps.Count;
        Entry.Status.CalibratedCount = Calibration.IsCalibrated ? CalibrationSteps.Count : 0;
        Entry.Status.VerifiedCount = Calibration.IsVerified ? CalibrationSteps.Count : 0;
        Entry.Status.Details = [];
    }
}