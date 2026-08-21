using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.AOD.Uniformity;
using Core.Models.Models.CIB.XTC;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using MathNet.Numerics;
using Microsoft.Extensions.Hosting;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.CIB;

[IOCAppService(ServiceType = typeof(CIBXTCViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBXTCViewModel : CalibrationViewModelBase<CIBXTCCache>
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Image Param" },
        new() { StepName = "Find Haze Position" },
        new() { StepName = "Forward & Reverse" },
        new() { StepName = "XTC" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial CIBXTCDTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<ProductivityInformationStatus> CalibratingStatuses { get; set; } = [];

    #endregion Calibrate

    [ObservableProperty]
    public partial IReadOnlyList<CIBXTCDTO> Reviews { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CIBXTCDTO> SelectedReviewItems { get; set; } = [];

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial CIBXTCCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial CIBXTCDTO[] Calibrations { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<CIBXTCCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<CIBXTCDTO>(cancellationToken);

        UpdateEntryStatus(Unsafe.As<CalibrationDTOBase[]>(Calibrations), cancellationToken);

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
                .OrderBy(t => t.ProductivityInformation)
        ];

        return Reviews.Count > 0;
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
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition), CalChipSiteModelEnum.HazeModel);

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
                CalibratingItem = new CIBXTCDTO(ApplicationCookie.CIBInformationPMTIds);

                return true;

            case 1:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.Item.HazeFindBFMachinePosition != Point.Origin
                        ? Cache.Item.HazeFindBFMachinePosition
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
    private Task<bool> Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
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
                Cache.Item.CIBInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.Item.MicroscopeLensInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation)
                   && ApplicationCookie.CIBInformations.Contains(Cache.Item.CIBInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Guard.IsEqualTo(Cache.Item.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            StageViewModel.SetAbsoluteStageTheta(0d);
            Cache.Item.HazeFindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.HazeFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            Guard.IsGreaterThanOrEqualTo(Cache.Item.PrescanAODWaveformProfileSegmentCount, 4);
            Guard.IsTrue((Cache.Item.PrescanAODWaveformProfileSegmentCount & 1) == 0, "It must be even number!");

            var detectImageDirectory = ImageFileDirectory;

            var cibInformations = ApplicationCookie.CIBInformations;
            var prescanAODWaveformProfiles = ConfigureViewModel.GetPrescanAODWaveProfiles(Cache.ProductivityInformation);
            var prescanAODWaveformCount = prescanAODWaveformProfiles[0].Shorts.Count;

            var startPrescanAODWaveformSegmentIndex = (Cache.Item.PrescanAODWaveformProfileSegmentCount - 1) / 2;
            var stopPrescanAODWaveformSegmentIndex = startPrescanAODWaveformSegmentIndex + 1;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.HazeFindBFMachinePosition,
                Cache.Item.ImageWidth,
                Cache.Item.PrescanAODWaveformProfileSegmentCount,
                CIBInformations = new HtmlExpand(string.Empty, new HtmlTable([.. cibInformations.Select(t => t.ToHtmlAnonymous())])),
                PrescanAODWaveformProfiles = new HtmlTable([.. prescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
                prescanAODWaveformCount,
                startPrescanAODWaveformSegmentIndex,
                stopPrescanAODWaveformSegmentIndex,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.ProductivityInformation = Cache.ProductivityInformation;
            CalibratingItem.StartWindowItem = new AODUniformityDTO.WindowItem();
            CalibratingItem.StopWindowItem = new AODUniformityDTO.WindowItem();

            CIBViewModel.SetAGC(cibInformations, true);
            CIBViewModel.SetCIBProfileModeEnum(cibInformations, CIBProfileModeEnum.PMTLog);

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition);
            var startCurrentHazeBFPosition = CIBViewModel.GetCIBInformationPosition(
                StageCoordinateSystemEnum.Dark,
                Cache.ProductivityInformation,
                Cache.Item.CIBInformation,
                hazeBFPosition,
                Cache.Item.MicroscopeLensInformation);

            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(startCurrentHazeBFPosition, CalChipSiteModelEnum.HazeModel);

            Logger.LogHtmlInformation("Forward and Reverse", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            try
            {
                CalibratingItem.StartWindowItem.Window = GetAndApplyWindow(Cache.Item.PrescanAODWaveformProfileSegmentCount, startPrescanAODWaveformSegmentIndex, prescanAODWaveformProfiles);
                await CatchImageAsync($"{startPrescanAODWaveformSegmentIndex}", CalibratingItem.StartWindowItem);
                CalibratingItem.StartWindowItem.CalculateHorizontalProjectMinPixel(Cache.Item.PrescanAODWaveformProfileSegmentCount);

                CalibratingItem.StopWindowItem.Window = GetAndApplyWindow(Cache.Item.PrescanAODWaveformProfileSegmentCount, stopPrescanAODWaveformSegmentIndex, prescanAODWaveformProfiles);
                await CatchImageAsync($"{stopPrescanAODWaveformSegmentIndex}", CalibratingItem.StopWindowItem);
                CalibratingItem.StopWindowItem.CalculateHorizontalProjectMinPixel(Cache.Item.PrescanAODWaveformProfileSegmentCount);

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    CalibratingItem.IsReverse,
                    Plot = new HtmlContainer(CalibratingItem.ForwardAndReverseScatterPlotControl.GetAllHtmlPlot2DLinesCharts())
                }), HtmlLogUniqueId.LoggingHtml());

                return true;

                async Task CatchImageAsync(string title, AODUniformityDTO.WindowItem windowItem)
                {
                    using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
                        Cache.ProductivityInformation,
                        StageCoordinateSystemEnum.Dark,
                        startCurrentHazeBFPosition,
                        Cache.Item.ImageWidth,
                        Cache.Item.CIBInformation,
                        (true, null),
                        (false, Cache.Item.OpticsConfiguration),
                        (false, Cache.Item.CIBConfiguration),
                        (true, null),
                        false,
                        cancellationToken);

                    var imageFilePath = Path.Combine(detectImageDirectory, Cache.Item.CIBInformation.ToString(), title, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                    darkFieldImage.Image.SaveImage(imageFilePath);

                    windowItem.ImageFilePath = imageFilePath;
                    windowItem.RawImageFilePath = darkFieldImage.RawImageFilePath;

                    using var hImage = darkFieldImage.Image.ToHImage();
                    windowItem.ImageHorizontalProjects = hImage.GetHorizontalProjects();

                    Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        hazeBFPosition,
                        startCurrentHazeBFPosition,
                        windowItem.RawImageFilePath,
                        Image = new HtmlImage(windowItem.ImageFilePath)
                    }), HtmlLogUniqueId.LoggingHtml());
                }
            }
            catch (Exception ex)
            {
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Plot = new HtmlContainer(CalibratingItem.ForwardAndReverseScatterPlotControl.GetAllHtmlPlot2DLinesCharts()),
                    Exception = ex
                }), HtmlLogUniqueId.LoggingHtml());

                return false;
            }
            finally
            {
                CIBViewModel.SetAGC(cibInformations, true);
                CIBViewModel.SetCIBProfileModeEnum(cibInformations, CIBProfileModeEnum.PMTLog);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(startCurrentHazeBFPosition, CalChipSiteModelEnum.HazeModel);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task<bool> Step4Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;

            var cibInformations = ApplicationCookie.CIBInformations;
            var prescanAODWaveformProfiles = ConfigureViewModel.GetPrescanAODWaveProfiles(Cache.ProductivityInformation);
            var prescanAODWaveformCount = prescanAODWaveformProfiles[0].Shorts.Count;

            var cibDelays = CIBViewModel.GetDelays(Cache.ProductivityInformation, cibInformations);
            var prescanAODWaveformSegmentIndexIndex = Cache.Item.PrescanAODWaveformProfileSegmentCount / 2;
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.CIBInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.HazeFindBFMachinePosition,
                Cache.Item.ImageWidth,
                Cache.Item.PrescanAODWaveformProfileSegmentCount,
                Cache.CalibratingRetryTimes,
                Cache.CalibratingThreshold,
                CIBInformations = new HtmlExpand(string.Empty, new HtmlTable([.. cibInformations.Select(t => t.ToHtmlAnonymous())])),
                PrescanAODWaveformProfiles = new HtmlTable([.. prescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
                CIBDelays = new HtmlExpand(string.Empty, new HtmlTable([.. cibDelays.Select(t => t.ToHtmlAnonymous())])),
                prescanAODWaveformCount,
                prescanAODWaveformSegmentIndexIndex,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.Items =
            [
                .. cibInformations.Select(t => new CIBXTCDTOItem
                {
                    CIBInformation = t,
                    Delay = cibDelays.Single(tt => tt.CIBInformation == t).PMTDelay
                })
            ];
            CalibratingItem.TargetPixelValues = [];

            CIBViewModel.SetAGC(cibInformations, true);
            CIBViewModel.SetCIBProfileModeEnum(cibInformations, CIBProfileModeEnum.PMTLog);
            CIBViewModel.SetDelays(cibDelays);

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition);
            var startCurrentHazeBFPosition = CIBViewModel.GetCIBInformationPosition(
                StageCoordinateSystemEnum.Dark,
                Cache.ProductivityInformation,
                Cache.Item.CIBInformation,
                hazeBFPosition,
                Cache.Item.MicroscopeLensInformation);

            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetCalChipDarkFieldAbsoluteStageXyByNotAutoFocus(startCurrentHazeBFPosition, CalChipSiteModelEnum.HazeModel);

            Logger.LogHtmlInformation("XTC", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            try
            {
                var window = GetAndApplyWindow(Cache.Item.PrescanAODWaveformProfileSegmentCount + 1, prescanAODWaveformSegmentIndexIndex, prescanAODWaveformProfiles);

                var times = 0;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    IReadOnlyList<CIBDelayDTO> currentCIBDelay = [.. cibDelays.Select(t => t.Clone().WithPMTDelayAndSenseDelay(CalibratingItem.Items.Single(tt => tt.CIBInformation == t.CIBInformation).Delay))];
                    CIBViewModel.SetDelays(currentCIBDelay);

                    var cibPMTImages = await CIBViewModel.GetPMTImagesAsync(
                        Cache.ProductivityInformation,
                        StageCoordinateSystemEnum.Dark,
                        startCurrentHazeBFPosition,
                        Cache.Item.ImageWidth,
                        cibInformations,
                        (true, null),
                        (false, Cache.Item.OpticsConfiguration),
                        (false, Cache.Item.CIBConfiguration),
                        (true, null),
                        false,
                        cancellationToken);

                    Logger.LogHtmlInformation("Images", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

                    foreach (var (index, darkFieldImage) in cibPMTImages.Index())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        using var _ = darkFieldImage;
                        var itemItem = CalibratingItem.Items[index];

                        var imageFilePath = Path.Combine(detectImageDirectory, itemItem.CIBInformation.ToString(), $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                        darkFieldImage.Image.SaveImage(imageFilePath);

                        using var hImage = darkFieldImage.Image.ToHImage();

                        var itemItemData = new CIBXTCDTOItem.Item
                        {
                            Window = window,
                            ImageHorizontalProjects = hImage.GetHorizontalProjects(),
                            RawImageFilePath = darkFieldImage.RawImageFilePath,
                            ImageFilePath = imageFilePath
                        };
                        itemItemData.CalculateHorizontalProjectMinPixel(Cache.Item.PrescanAODWaveformProfileSegmentCount);

                        if (HostEnvironment.IsDevelopment()) itemItemData.HorizontalProjectMinPixel += Random.Shared.RandomInteger(-10, 10);
                        itemItem.Items = [.. itemItem.Items, itemItemData];

                        Logger.LogHtmlInformation(itemItem.CIBInformation.ToString(), HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                        {
                            hazeBFPosition,
                            startCurrentHazeBFPosition,
                            itemItemData.ImageFilePath,
                            itemItemData.RawImageFilePath
                        }), HtmlLogUniqueId.LoggingHtml());
                    }

                    var results = (
                        from itemItem in CalibratingItem.Items
                        group itemItem by itemItem.CIBInformation.PMTId
                        into g
                        orderby g.Key
                        select (
                            PMTId: g.Key,
                            ItemItems: g.OrderBy(t => t.CIBInformation.PMTId).ToArray()
                        )).ToArray();

                    var resultList = new List<bool>();
                    CalibratingItem.TargetPixelValues = [];
                    foreach (var (pmtId, itemItems) in results)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var horizontalProjectMinPixel = itemItems.Single(t => t.CIBInformation.ChannelId == CalibrationSetting.SettingCommonParam.MainCIBInformation.ChannelId).Items[times].HorizontalProjectMinPixel;
                        var pmtIdTargetPixelValue = CalibratingItem.TargetPixelValues.GetOrAdd(pmtId, _ => horizontalProjectMinPixel);

                        foreach (var itemItem in itemItems)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            itemItem.Items[times].Error = pmtIdTargetPixelValue - itemItem.Items[times].HorizontalProjectMinPixel;
                            itemItem.Items[times].IsOk = Math.Abs(itemItem.Items[times].Error) <= Cache.CalibratingThreshold;
                            resultList.Add(itemItem.Items[times].IsOk);

                            if (itemItem.Items[times].IsOk) continue;

                            itemItem.Delay += (CalibratingItem.IsReverse ? 1 : -1) * itemItem.Items[times].Error;
                        }
                    }

                    var htmlBullet = new HtmlBullet(new
                    {
                        times,
                        CalibratingItem.ProductivityInformation,
                        CurrentCIBDelay = new HtmlExpand(string.Empty, new HtmlTable([.. currentCIBDelay.Select(t => t.ToHtmlAnonymous())])),
                        Error = new HtmlExpand(string.Empty, new HtmlTable(
                        [
                            .. CalibratingItem.Items.Select(t => new
                            {
                                t.CIBInformation,
                                t.Items[^1].Error
                            })
                        ])),
                        Plot = new HtmlContainer([.. CalibratingItem.ScatterPlotControls.Select(t => new HtmlExpand(t.Key.ToString(), new HtmlContainer(t.Value.GetAllHtmlPlot2DLinesCharts())))])
                    });

                    CalibratingItem.IsCalibrated = resultList.All(t => t);

                    if (CalibratingItem.IsCalibrated)
                    {
                        LogDetails(true);
                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                        break;
                    }

                    if (++times > Cache.CalibratingRetryTimes - 1)
                    {
                        LogDetails(false);
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                        break;
                    }

                    Logger.LogHtmlInformation("Results", HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                }

                Guard.IsTrue(Save([CalibratingItem], cancellationToken));

                return CalibratingItem.IsCalibrated;

                void LogDetails(bool isSuccess)
                {
                    Logger.LogHtmlInformation("Details", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());
                    foreach (var itemItem in CalibratingItem.Items)
                    {
                        var itemItemData = itemItem.Items[^1];
                        var htmlBullet = new HtmlBullet(new
                        {
                            itemItemData.RawImageFilePath,
                            Image = new HtmlImage(itemItemData.ImageFilePath)
                        });

                        if (isSuccess) Logger.LogHtmlInformation(itemItem.CIBInformation.ToString(), HtmlHeaderLevelEnum.Header6, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                        else Logger.LogHtmlError(itemItem.CIBInformation.ToString(), HtmlHeaderLevelEnum.Header6, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                    }
                }
            }
            finally
            {
                CIBViewModel.SetAGC(cibInformations, true);
                CIBViewModel.SetCIBProfileModeEnum(cibInformations, CIBProfileModeEnum.PMTLog);
                CIBViewModel.SetDelays(cibDelays);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetCalChipBrightFieldAbsoluteStageXy(startCurrentHazeBFPosition, CalChipSiteModelEnum.HazeModel);
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

        await InvokeVerifyAsync(() =>
        {
            var errorMessageStringBuilder = new StringBuilder();

            foreach (var selectedReviewItem in SelectedReviewItems.OrderBy(t => t.ProductivityInformation))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var title = selectedReviewItem.ProductivityInformation.ToString();

                /*if (selectedReviewItem.IsCalibrated == false)
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error");
                    continue;
                }*/

                Cache.ProductivityInformation = selectedReviewItem.ProductivityInformation;

                Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header4, new HtmlQuote(new
                {
                    Cache.ProductivityInformation
                }), HtmlLogUniqueId.LoggingHtml());

                if (selectedReviewItem.IsCalibrated) selectedReviewItem.IsVerified = true;

                var htmlBullet = new HtmlBullet(new
                {
                    selectedReviewItem.ProductivityInformation,
                    Details = new HtmlExpand(string.Empty, new HtmlTable(
                    [
                        .. selectedReviewItem.Items.Select(t => new
                        {
                            t.CIBInformation,
                            t.Delay,
                            Error = t.Items.Count > 0 ? t.Items[^1].Error.ToString("0.###") : "Not Cache"
                        })
                    ])),
                    Plot = new HtmlContainer([.. selectedReviewItem.ScatterPlotControls.Select(t => new HtmlExpand(t.Key.ToString(), new HtmlContainer(t.Value.GetAllHtmlPlot2DLinesCharts())))])
                });

                if (selectedReviewItem.IsVerified)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                {
                    errorMessageStringBuilder.AppendLine($"{title}: Error");
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
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
        }).ConfigureAwait(false);
    }

    private double[] GetAndApplyWindow(int segmentCount, int segmentIndex, IReadOnlyList<PrescanAODWaveformProfile> prescanAODWaveformProfiles)
    {
        var prescanAODWaveformCount = prescanAODWaveformProfiles[0].Shorts.Count;

        var window = Generate.LinearVShapeWindowBySegments(
            Cache.Item.LaserLightInformation.Coefficient,
            Cache.Item.LaserLightInformation.Coefficient / 1000d,
            segmentCount,
            segmentIndex,
            prescanAODWaveformCount).Window;

        foreach (var prescanAODWaveformProfile in prescanAODWaveformProfiles) prescanAODWaveformProfile.ApplyCoefficientWindowList(window);
        LaserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, prescanAODWaveformProfiles);

        return window;
    }

    private bool Save(IReadOnlyList<CIBXTCDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                dto,
                .. Calibrations.Where(t => t.ProductivityInformation != dto.ProductivityInformation)
            ];
        }

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<CIBXTCDTO[]>(calibrations);
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
                    t.Items = [.. t.Items.Where(i => ApplicationCookie.CIBInformations.Contains(i.CIBInformation))];
                    if (t.IsCalibrated) t.IsCalibrated = t.Items.Count == ApplicationCookie.CIBInformations.Count;

                    return t;
                })
        ];

        foreach (var calibratingStatus in CalibratingStatuses)
        {
            calibratingStatus.IsCalibrated = Calibrations.Where(t => t.ProductivityInformation == calibratingStatus.SelectedItem && t.IsCalibrated)
                .SelectMany(t => t.Items)
                .Count() == ApplicationCookie.CIBInformations.Count;
        }

        status.TotalCalibrationCount = ApplicationCookie.OpticsMagTypeProductivityInformations.Count * ApplicationCookie.CIBInformations.Count;
        status.CalibratedCount = Calibrations.Where(t => t.IsCalibrated)
            .SelectMany(t => t.Items)
            .Count();
        status.VerifiedCount = Calibrations.Where(t => t.IsVerified)
            .SelectMany(t => t.Items)
            .Count();

        var detailList = new List<CalibrationViewModelStatus.Detail>(status.TotalCalibrationCount);
        foreach (var productivityInformation in ApplicationCookie.OpticsMagTypeProductivityInformations)
        {
            var item = Calibrations.SingleOrDefault(t => t.ProductivityInformation == productivityInformation);

            foreach (var cibInformation in ApplicationCookie.CIBInformations)
            {
                var itemData = item?.Items?.SingleOrDefault(i => i.CIBInformation == cibInformation);

                detailList.Add(new CalibrationViewModelStatus.Detail(
                    $"{productivityInformation}/{cibInformation}",
                    item is not null
                        ? item.IsCalibrated ? itemData is not null : null
                        : null,
                    item is not null
                        ? item.IsVerified ? itemData is not null : null
                        : null));
            }
        }

        status.Details = detailList;
    }

    #endregion 校准
}