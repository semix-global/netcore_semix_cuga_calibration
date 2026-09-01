using System.IO;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Fourier;
using Core.Models.Models.Fourier.PupilCameraAlignment;
using Core.Models.Models.Microscope.CalChip;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Flourier;

[IOCAppService(ServiceType = typeof(PupilCameraAlignmentViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class PupilCameraAlignmentViewModel : CalibrationViewModelBase<PupilCameraAlignmentCache>
{
    #region 属性

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Param" },
        new() { StepName = "Find Haze Position" },
        new() { StepName = "Channel 1" },
        new() { StepName = "Channel 2" },
        new() { StepName = "Channel 3" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial PupilCameraAlignmentDTO CalibratingItem { get; set; } = new();

    #endregion Calibrate

    [ObservableProperty]
    public partial PupilCameraAlignmentDTO Review { get; set; } = new();

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial PupilCameraAlignmentCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial PupilCameraAlignmentDTO Calibration { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<PupilCameraAlignmentCache>(cancellationToken);
        Calibration = ApplicationCookieService.GetCalibration<PupilCameraAlignmentDTO>(cancellationToken);

        UpdateEntryStatus(Calibration, cancellationToken);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Review = Calibration;
        Review.Channel1Item.Review();
        Review.Channel2Item.Review();
        Review.Channel3Item.Review();

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
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.HazeFindBFMachinePosition), CalChipSiteModelEnum.HazeModel);

                return true;

            case 3:
                return true;

            case 4:
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
                CalibratingItem = new PupilCameraAlignmentDTO();

                return true;

            case 1:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.MicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.HazeFindBFMachinePosition != Point.Origin
                        ? Cache.HazeFindBFMachinePosition
                        : MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.HazeModel)), CalChipSiteModelEnum.HazeModel);

                return true;

            case 2:

                return true;

            case 3:

                return true;

            case 4:
                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

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
                Cache.ProductivityInformation,
                Cache.MicroscopeLensInformation,
                Cache.LaserLightInformation,
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.ScanImageWidth
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
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Guard.IsEqualTo(Cache.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            StageViewModel.SetAbsoluteStageTheta(0d);
            Cache.HazeFindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.MicroscopeLensInformation,
                Cache.LaserLightInformation,
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.HazeFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.MicroscopeLensInformation,
                Cache.LaserLightInformation,
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.HazeFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.HazeFindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(hazeBFPosition);

            try
            {
                FourierViewModel.SetFFHome(FFCH.Ch1);
                using var bitmapImage = FourierViewModel.GetFFReviewImgForTrigger(
                    0,
                    Cache.ProductivityInformation,
                    Cache.LaserLightInformation.Level,
                    StageViewModel.MachineToBrightFieldPosition(Cache.HazeFindBFMachinePosition),
                    Cache.ScanImageWidth);
                var imageFilePath = Path.Combine(detectImageDirectory, "Channel1", $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                bitmapImage.SaveImage(imageFilePath);

                await CalibratingItem.Channel1Item.CalibratingAsync(imageFilePath, cancellationToken);

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlImage(imageFilePath, htmlImageOverlays:
                [
                    new HtmlImageRectangleOverlay(CalibratingItem.Channel1Item.ImageROI)
                ]), HtmlLogUniqueId.LoggingHtml());

                return true;
            }
            finally
            {
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(hazeBFPosition);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.MicroscopeLensInformation,
                Cache.LaserLightInformation,
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.HazeFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.HazeFindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(hazeBFPosition);

            try
            {
                FourierViewModel.SetFFHome(FFCH.Ch2);
                using var bitmapImage = FourierViewModel.GetFFReviewImgForTrigger(
                    1,
                    Cache.ProductivityInformation,
                    Cache.LaserLightInformation.Level,
                    StageViewModel.MachineToBrightFieldPosition(Cache.HazeFindBFMachinePosition),
                    Cache.ScanImageWidth);
                var imageFilePath = Path.Combine(detectImageDirectory, "Channel2", $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                bitmapImage.SaveImage(imageFilePath);

                await CalibratingItem.Channel2Item.CalibratingAsync(imageFilePath, cancellationToken);

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlImage(imageFilePath, htmlImageOverlays:
                [
                    new HtmlImageRectangleOverlay(CalibratingItem.Channel2Item.ImageROI)
                ]), HtmlLogUniqueId.LoggingHtml());

                return true;
            }
            finally
            {
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(hazeBFPosition);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step4Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.MicroscopeLensInformation,
                Cache.LaserLightInformation,
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.HazeFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.HazeFindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(hazeBFPosition);

            try
            {
                FourierViewModel.SetFFHome(FFCH.Ch3_X);
                FourierViewModel.SetFFHome(FFCH.Ch3_Y);
                using var bitmapImage = FourierViewModel.GetFFReviewImgForTrigger(
                    2,
                    Cache.ProductivityInformation,
                    Cache.LaserLightInformation.Level,
                    StageViewModel.MachineToBrightFieldPosition(Cache.HazeFindBFMachinePosition),
                    Cache.ScanImageWidth);
                var imageFilePath = Path.Combine(detectImageDirectory, "Channel3", $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                bitmapImage.SaveImage(imageFilePath);

                await CalibratingItem.Channel3Item.CalibratingAsync(imageFilePath, cancellationToken);

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlImage(imageFilePath, htmlImageOverlays:
                [
                    new HtmlImageRectangleOverlay(CalibratingItem.Channel3Item.ImageROI)
                ]), HtmlLogUniqueId.LoggingHtml());

                Guard.IsTrue(Save(CalibratingItem, cancellationToken));

                return true;
            }
            finally
            {
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(hazeBFPosition);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(() =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.MicroscopeLensInformation,
                Cache.LaserLightInformation,
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.HazeFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Channel1 = new HtmlImage(Review.Channel1Item.ChannelImageFilePath, htmlImageOverlays:
                [
                    new HtmlImageRectangleOverlay(Review.Channel1Item.ImageROI)
                ]),
                Channel2 = new HtmlImage(Review.Channel2Item.ChannelImageFilePath, htmlImageOverlays:
                [
                    new HtmlImageRectangleOverlay(Review.Channel2Item.ImageROI)
                ]),
                Channel3 = new HtmlImage(Review.Channel3Item.ChannelImageFilePath, htmlImageOverlays:
                [
                    new HtmlImageRectangleOverlay(Review.Channel3Item.ImageROI)
                ])
            }), HtmlLogUniqueId.LoggingHtml());

            Review.IsVerified = true;
            Guard.IsTrue(Save(Review, cancellationToken));

            DialogWindowProvider.ShowDialog("Verify : OK");

            return true;
        }).ConfigureAwait(false);
    }

    private bool Save(PupilCameraAlignmentDTO dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        ApplicationCookieService.SetCalibration(dto, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase calibration, CancellationToken cancellationToken)
    {
        var temp = Guard.IsAssignableToTypeAndReturn<PupilCameraAlignmentDTO>(calibration);
        var status = Entry.Status;

        Calibration = temp;

        status.TotalCalibrationCount = 1;
        status.CalibratedCount = Calibration.IsCalibrated ? 1 : 0;
        status.VerifiedCount = Calibration.IsVerified ? 1 : 0;
        status.Details = [];
    }

    #endregion 校准
}