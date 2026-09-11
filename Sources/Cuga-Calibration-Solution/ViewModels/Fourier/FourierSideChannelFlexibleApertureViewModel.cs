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
using Constants = Net.Utilities.Models.Constants;
using Item = Core.Models.Models.Fourier.SideChannelFlexibleAperture.FourierSideChannelFlexibleApertureDTOItem.Item;

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
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<FourierSideChannelFlexibleApertureCache>(cancellationToken);
        Calibration = ApplicationCookieService.GetCalibration<FourierSideChannelFlexibleApertureDTO>(cancellationToken);

        UpdateEntryStatus(Calibration, cancellationToken);

        return true;
    }

    protected override async Task<bool> ReviewingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        Review = Calibration;
        Review.Channel1Item.EvenItem.Review();
        Review.Channel1Item.OddItem.Review();
        Review.Channel2Item.EvenItem.Review();
        Review.Channel2Item.OddItem.Review();

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
                var rodTotalCount = FourierViewModel.GetFourierConfig().RodNum;
                if (rodTotalCount <= 5) rodTotalCount = FourierSideChannelFlexibleApertureDTO.DefaultRodTotalCount;

                CalibratingItem.Dispose();
                CalibratingItem = new FourierSideChannelFlexibleApertureDTO(rodTotalCount);
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
                Cache.ScanLength,
                Cache.Step0AndStep1MotorAbsoluteValue,
                Cache.Step2MotorAbsoluteValue
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
                Cache.ScanLength,
                Cache.Step0AndStep1MotorAbsoluteValue,
                Cache.Step2MotorAbsoluteValue,
                Cache.HazeFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () => await InvokeAsync(CalibratingItem.Channel1Item.ChannelId, CalibratingItem.Channel1Item.EvenItem, "Even", false, cancellationToken));
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () => await InvokeAsync(CalibratingItem.Channel1Item.ChannelId, CalibratingItem.Channel1Item.OddItem, "Odd", false, cancellationToken));
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step4Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () => await InvokeAsync(CalibratingItem.Channel2Item.ChannelId, CalibratingItem.Channel2Item.EvenItem, "Even", false, cancellationToken));
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step5Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () => await InvokeAsync(CalibratingItem.Channel2Item.ChannelId, CalibratingItem.Channel2Item.OddItem, "Odd", true, cancellationToken));
    }

    private async Task<bool> InvokeAsync(int channelId, Item item, string itemName, bool isLast, CancellationToken cancellationToken)
    {
        var detectImageDirectory = ImageFileDirectory;

        Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
        {
            Cache.ProductivityInformation,
            Cache.MicroscopeLensInformation,
            Cache.LaserLightInformation,
            OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous()),
            Cache.ScanLength,
            Cache.Step0AndStep1MotorAbsoluteValue,
            Cache.Step2MotorAbsoluteValue,
            Cache.HazeFindBFMachinePosition
        }), HtmlLogUniqueId.LoggingHtml());

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

        try
        {
            var ffch = channelId == 1 ? FFCH.Ch1 : FFCH.Ch2;
            FourierViewModel.SetFFHome(ffch);

            MoveRods(ffch, item, Cache.Step0AndStep1MotorAbsoluteValue);

            var itemDirectory = Path.Combine(detectImageDirectory, $"Channel{channelId}", itemName);
            Directory.CreateDirectory(itemDirectory);

            item.Step0ChannelImageFilePath = Grab(channelId, itemDirectory, "Step0");
            item.Step1ChannelImageFilePath = Grab(channelId, itemDirectory, "Step1");

            MoveRods(ffch, item, Cache.Step2MotorAbsoluteValue);
            item.Step2ChannelImageFilePath = Grab(channelId, itemDirectory, "Step2");

            Logger.LogHtmlInformation("Image", HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToImageHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            await item.CalibratingAsync(Cache.Step0AndStep1MotorAbsoluteValue, Cache.Step2MotorAbsoluteValue, cancellationToken);

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(item.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            if (isLast)
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
    }

    private void MoveRods(FFCH ffch, Item item, double motorAbsoluteValue)
    {
        FourierViewModel.FF_Move_CH12(ffch, item.Step1Rods.Select(t => (t.Index + 1, motorAbsoluteValue)).ToList());
    }

    private string Grab(int channelId, string directory, string name)
    {
        using var bitmapImage = FourierViewModel.GetFFReviewImgForTrigger(
            channelId - 1,
            Cache.ProductivityInformation,
            Cache.LaserLightInformation.Level,
            StageViewModel.MachineToBrightFieldPosition(Cache.HazeFindBFMachinePosition),
            Cache.ScanLength);
        var imageFilePath = Path.Combine(directory, $"{name}_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
        bitmapImage.SaveImage(imageFilePath);

        return imageFilePath;
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
                Cache.ScanLength,
                Cache.Step0AndStep1MotorAbsoluteValue,
                Cache.Step2MotorAbsoluteValue,
                Cache.HazeFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Channel1EvenItem = new HtmlQuote(Review.Channel1Item.EvenItem.ToHtmlAnonymous()),
                Channel1OddItem = new HtmlQuote(Review.Channel1Item.OddItem.ToHtmlAnonymous()),
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

        status.TotalCalibrationCount = 1;
        status.CalibratedCount = Calibration.IsCalibrated ? 1 : 0;
        status.VerifiedCount = Calibration.IsVerified ? 1 : 0;
        status.Details = [];
    }

    #endregion 校准
}
