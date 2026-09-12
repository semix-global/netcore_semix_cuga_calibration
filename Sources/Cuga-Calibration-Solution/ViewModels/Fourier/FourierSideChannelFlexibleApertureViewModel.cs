using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Fourier;
using Core.Models.Models.Fourier.SideChannelFlexibleAperture;
using Core.Models.Models.Microscope.CalChip;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using System.IO;
using Core.Models.Models.Fourier.PupilCameraAlignment;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.WPF.Enums;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.Fourier;

[IOCAppService(ServiceType = typeof(FourierSideChannelFlexibleApertureViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class FourierSideChannelFlexibleApertureViewModel : CalibrationViewModelBase<FourierSideChannelFlexibleApertureCache>
{
    #region 属性

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Param" },
        new() { StepName = "Find Haze Position" },
        new() { StepName = "Channel 1 Even" },
        new() { StepName = "Channel 1 Odd" },
        new() { StepName = "Channel 2 Even" },
        new() { StepName = "Channel 2 Odd" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial FourierSideChannelFlexibleApertureDTO CalibratingItem { get; set; } = new();

    #endregion Calibrate

    [ObservableProperty]
    public partial FourierSideChannelFlexibleApertureDTO Review { get; set; } = new();

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial FourierSideChannelFlexibleApertureCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial FourierSideChannelFlexibleApertureDTO Calibration { get; set; } = new();

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

        Cache = ApplicationCookieService.GetCache<FourierSideChannelFlexibleApertureCache>(cancellationToken);
        Calibration = ApplicationCookieService.GetCalibration<FourierSideChannelFlexibleApertureDTO>(cancellationToken);

        var config = FourierViewModel.GetFourierConfig();
        Cache.RodTotalCount = config.RodNum;
        Cache.MinMotorAbsoluteValue = config.CH12MinPOS;
        Cache.MaxMotorAbsoluteValue = config.CH12MaxPOS;
        if (Cache.Step0AndStep1MotorAbsoluteValue == 0d) Cache.Step0AndStep1MotorAbsoluteValue = config.CH12MaxPOS * 0.9;
        if (Cache.Step2MotorAbsoluteValue == 0d) Cache.Step2MotorAbsoluteValue = config.CH12MaxPOS * 0.5;

        UpdateEntryStatus(Calibration, cancellationToken);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Review = Calibration;
        Review.Channel1Item.EvenItem.Review();
        Review.Channel1Item.OddItem.Review();
        Review.Channel1Item.Review();
        Review.Channel2Item.EvenItem.Review();
        Review.Channel2Item.OddItem.Review();
        Review.Channel2Item.Review();

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
                CalibratingItem = new FourierSideChannelFlexibleApertureDTO(
                    Cache.RodTotalCount,
                    FourierPupilCameraAlignment.Channel1Item.ROIChannelImageFilePath,
                    FourierPupilCameraAlignment.Channel2Item.ROIChannelImageFilePath);

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

            Guard.IsGreaterThan(Cache.RodTotalCount, 5);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.RodTotalCount,
                Cache.MinMotorAbsoluteValue,
                Cache.MaxMotorAbsoluteValue,
                Cache.ProductivityInformation,
                Cache.MicroscopeLensInformation,
                Cache.LaserLightInformation,
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.ScanLength,
                Cache.Step0AndStep1MotorAbsoluteValue,
                Cache.Step2MotorAbsoluteValue
            }), HtmlLogUniqueId.LoggingHtml());

            LaserViewModel.ToggleOpticsMagType(Cache.ProductivityInformation);
            LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, Cache.LaserLightInformation.Coefficient);
            LaserViewModel.SetChirpAODWaveProfile(Cache.ProductivityInformation);
            OpticsViewModel.SetOpticsConfiguration(Cache.OpticsConfiguration);

            Guard.IsBetweenOrEqualTo(Cache.Step0AndStep1MotorAbsoluteValue, Cache.MinMotorAbsoluteValue, Cache.MaxMotorAbsoluteValue);
            Guard.IsBetweenOrEqualTo(Cache.Step2MotorAbsoluteValue, Cache.MinMotorAbsoluteValue, Cache.MaxMotorAbsoluteValue);
            Guard.IsLessThan(Cache.Step2MotorAbsoluteValue, Cache.Step0AndStep1MotorAbsoluteValue);

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
                Cache.RodTotalCount,
                Cache.MinMotorAbsoluteValue,
                Cache.MaxMotorAbsoluteValue,
                Cache.ProductivityInformation,
                Cache.MicroscopeLensInformation,
                Cache.LaserLightInformation,
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.ScanLength,
                Cache.Step0AndStep1MotorAbsoluteValue,
                Cache.Step2MotorAbsoluteValue,
                Cache.HazeFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () => await InvokeAsync(CalibratingItem.Channel1Item, true, cancellationToken));
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () => await InvokeAsync(CalibratingItem.Channel1Item, false, cancellationToken));
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step4Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () => await InvokeAsync(CalibratingItem.Channel2Item, true, cancellationToken));
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step5Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () => await InvokeAsync(CalibratingItem.Channel2Item, false, cancellationToken));
    }

    private async Task<bool> InvokeAsync(
        FourierSideChannelFlexibleApertureDTOItem item,
        bool isEven,
        CancellationToken cancellationToken)
    {
        var detectImageDirectory = ImageFileDirectory;
        var channelId = item.ChannelId switch
        {
            1 => FFCH.Ch1,
            2 => FFCH.Ch2,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<FFCH>(nameof(item.ChannelId))
        };

        var fourierPupilCameraAlignmentItem = item.ChannelId switch
        {
            1 => FourierPupilCameraAlignment.Channel1Item,
            2 => FourierPupilCameraAlignment.Channel2Item,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<FourierPupilCameraAlignmentDTOItem>(nameof(item.ChannelId))
        };

        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Cache.RodTotalCount,
            Cache.MinMotorAbsoluteValue,
            Cache.MaxMotorAbsoluteValue,
            Cache.ProductivityInformation,
            Cache.MicroscopeLensInformation,
            Cache.LaserLightInformation,
            OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
            Cache.ScanLength,
            Cache.Step0AndStep1MotorAbsoluteValue,
            Cache.Step2MotorAbsoluteValue,
            Cache.HazeFindBFMachinePosition,
            channelId,
            fourierPupilCameraAlignmentItem.ImageROI
        }), HtmlLogUniqueId.LoggingHtml());

        Guard.IsNotNullOrWhiteSpace(fourierPupilCameraAlignmentItem.ROIChannelImageFilePath);
        Guard.IsTrue(fourierPupilCameraAlignmentItem.ImageROI is { Width: > 0d, Height: > 0d });

        var itemData = isEven ? item.EvenItem : item.OddItem;

        item.Reset();
        itemData.Reset();

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
            FourierViewModel.SetFFHome(channelId);

            MoveRods(0, Cache.Step0AndStep1MotorAbsoluteValue);
            Grab(0);
            MoveRods(1, Cache.Step0AndStep1MotorAbsoluteValue);
            Grab(1);
            MoveRods(2, Cache.Step2MotorAbsoluteValue);
            Grab(2);

            Logger.LogHtmlInformation("Image", HtmlHeaderLevelEnum.Header3, new HtmlQuote(itemData.ToImageHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            await itemData.CalibratingAsync(Cache.Step0AndStep1MotorAbsoluteValue, Cache.Step2MotorAbsoluteValue, cancellationToken);

            Logger.LogHtmlInformation("ROI", HtmlHeaderLevelEnum.Header3, new HtmlQuote(itemData.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            if (isEven == false)
            {
                item.Calibrating(Cache.MinMotorAbsoluteValue, Cache.MaxMotorAbsoluteValue, cancellationToken);

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

                if (item.ChannelId == 2)
                {
                    CalibratingItem.IsCalibrated = true;

                    Guard.IsTrue(Save(CalibratingItem, cancellationToken));
                }
            }

            return true;
        }
        finally
        {
            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetBrightFieldAbsoluteStageXy(startCurrentHazeBFPosition, CalChipSiteModelEnum.HazeModel);
        }

        void MoveRods(int stepIndex, double motorAbsoluteValue)
        {
            var indexes = stepIndex switch
            {
                0 => [itemData.Step0LeftRod.Index, itemData.Step0RightRod.Index],
                1 => [.. itemData.Step1Rods.Select(t => t.Index)],
                2 => [.. itemData.Step2Rods.Select(t => t.Index)],
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<int[]>(nameof(stepIndex))
            };

            FourierViewModel.FF_Move_CH12(channelId,
            [
                .. item.RodResults.Select(t => (t.Index,
                    indexes.Contains(t.Index)
                        ? motorAbsoluteValue
                        : Cache.MinMotorAbsoluteValue))
            ]);
        }

        void Grab(int stepIndex)
        {
            using var bitmapImage = FourierViewModel.GetFFReviewImgForTrigger(
                item.ChannelId - 1,
                Cache.ProductivityInformation,
                Cache.LaserLightInformation.Level,
                StageViewModel.MachineToBrightFieldPosition(Cache.HazeFindBFMachinePosition),
                Cache.ScanLength);
            var imageFilePath = Path.Combine(detectImageDirectory, $"Channel{item.ChannelId}", $"Step{stepIndex}_{(isEven ? "Even" : "Odd")}_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
            DirectoryHelper.CreateFileDirectoryIfNotExists(imageFilePath);
            bitmapImage.SaveImage(imageFilePath);
            var roiChannelImageFilePath = Path.Combine(Path.GetDirectoryName(imageFilePath) ?? string.Empty, $"{Path.GetFileNameWithoutExtension(imageFilePath)}_ROI_{fourierPupilCameraAlignmentItem.ImageROI}{Path.GetExtension(imageFilePath)}");

            using var roiBitmapImageDrawable = bitmapImage.ToROI(fourierPupilCameraAlignmentItem.ImageROI);
            roiBitmapImageDrawable.SaveImage(roiChannelImageFilePath);

            switch (stepIndex)
            {
                case 0:
                    itemData.Step0ChannelImageFilePath = roiChannelImageFilePath;
                  
                    break;

                case 1:
                    itemData.Step1ChannelImageFilePath = roiChannelImageFilePath;
                   
                    break;

                case 2:
                    itemData.Step2ChannelImageFilePath = roiChannelImageFilePath;
                    
                    break;

                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(stepIndex));
                    
                    break;
            }
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await InvokeVerifyAsync(() =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.RodTotalCount,
                Cache.MinMotorAbsoluteValue,
                Cache.MaxMotorAbsoluteValue,
                Cache.ProductivityInformation,
                Cache.MicroscopeLensInformation,
                Cache.LaserLightInformation,
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.ScanLength,
                Cache.Step0AndStep1MotorAbsoluteValue,
                Cache.Step2MotorAbsoluteValue,
                Cache.HazeFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Channel1Item = new HtmlQuote(Review.Channel1Item.ToHtmlAnonymous()),
                Channel1EvenItem = new HtmlQuote(Review.Channel1Item.EvenItem.ToHtmlAnonymous()),
                Channel1OddItem = new HtmlQuote(Review.Channel1Item.OddItem.ToHtmlAnonymous()),
                Channel2Item = new HtmlQuote(Review.Channel2Item.ToHtmlAnonymous()),
                Channel2EvenItem = new HtmlQuote(Review.Channel2Item.EvenItem.ToHtmlAnonymous()),
                Channel2OddItem = new HtmlQuote(Review.Channel2Item.OddItem.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            Review.IsVerified = true;
            Guard.IsTrue(Save(Review, cancellationToken));

            DialogWindowProvider.ShowDialog("Verify : OK");

            return true;
        }).ConfigureAwait(false);
    }

    private bool Save(FourierSideChannelFlexibleApertureDTO dto, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(dto);
        update(Cache);

        Calibration = dto.Clone();

        ApplicationCookieService.SetCalibration(dto, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase calibration, CancellationToken cancellationToken)
    {
        var temp = Guard.IsAssignableToTypeAndReturn<FourierSideChannelFlexibleApertureDTO>(calibration);
        var status = Entry.Status;

        Calibration = temp;

        var total = Calibration.Channel1Item.RodResults.Length + Calibration.Channel2Item.RodResults.Length;
        status.TotalCalibrationCount = total;
        status.CalibratedCount = Calibration.IsCalibrated ? total : 0;
        status.VerifiedCount = Calibration.IsVerified ? total : 0;
        status.Details = [];
    }

    #endregion 校准
}