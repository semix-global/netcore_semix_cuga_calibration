using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.AOD.Alignment;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.AOD;

[IOCAppService(ServiceType = typeof(AODAlignmentViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AODAlignmentViewModel : CalibrationViewModelBase<AODAlignmentCache>
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Image Param" },
        new() { StepName = "Find Haze Position" },
        new() { StepName = "AOD Alignment" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial AODAlignmentDTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<ProductivityInformationStatus> CalibratingStatuses { get; set; } = [];

    #endregion Calibrate

    [ObservableProperty]
    public partial IReadOnlyList<AODAlignmentDTO> Reviews { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<AODAlignmentDTO> SelectedReviewItems { get; set; } = [];

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial AODAlignmentCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial AODAlignmentDTO[] Calibrations { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<AODAlignmentCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<AODAlignmentDTO>(cancellationToken);

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
                .Select(t => t.Clone())
                .OrderBy(t => t.ProductivityInformation)
        ];

        return Reviews.Count > 0;
    }

    protected override async Task<bool> PreviousingAsync(CancellationToken cancellationToken)
    {
        switch (CalibrationStepIndex)
        {
            case 0:
            case 1:
            case 2:
                return true;

            case 3:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.Item.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.Item.HazeFindBFMachinePosition == Point.Origin
                        ? MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.HazeModel)
                        : Cache.Item.HazeFindBFMachinePosition), CalChipSiteModelEnum.HazeModel);

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
                CalibratingItem = new AODAlignmentDTO();

                return true;

            case 1:
                await MicroscopeViewModel.SwitchMicroscopeLensInformationAsync(Cache.Item.MicroscopeLensInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(
                    Cache.Item.HazeFindBFMachinePosition == Point.Origin
                        ? MicroscopeCalChip.GetBFMachinePosition(CalChipSiteModelEnum.HazeModel)
                        : Cache.Item.HazeFindBFMachinePosition), CalChipSiteModelEnum.HazeModel);

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

    #endregion 控制校准业务

    #region 校准

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            var prescanCache = ApplicationCookieService.GetOrDefault<PrescanAODWaveformElectrodeDelayCache>(false, cancellationToken: cancellationToken);
            var prescanResult = prescanCache.Results.SingleOrDefault(t => t.GeneratePrescanAODWaveformParam.ProductivityInformation.OpticsIlluminationModeEnum == Cache.ProductivityInformation.OpticsIlluminationModeEnum
                                                                          && t.GeneratePrescanAODWaveformParam.ProductivityInformation.OpticsMagType == Cache.ProductivityInformation.OpticsMagType);
            if (prescanResult is null)
            {
                const string comment = "Warning: Prescan AOD Waveform Param No matched found for current Productivity Information!";
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment(comment), HtmlLogUniqueId.LoggingHtml());

                DialogWindowProvider.ShowDialog(comment, dialogButtonsEnum: DialogButtonsEnum.OK, dialogIconEnum: DialogIconEnum.Error);

                return false;
            }

            Cache.Item.FlatnessGeneratePrescanAODWaveformParam = prescanResult.GeneratePrescanAODWaveformParam.Clone();
            Cache.Item.FlatnessGeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation.Clone();
            Cache.Item.StartPrescanFrequency = prescanResult.GeneratePrescanAODWaveformParam.CenterFrequency - prescanResult.GeneratePrescanAODWaveformParam.BandWidth / 2d;
            Cache.Item.StopPrescanFrequency = prescanResult.GeneratePrescanAODWaveformParam.CenterFrequency + prescanResult.GeneratePrescanAODWaveformParam.BandWidth / 2d;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                OriginGeneratePrescanAODWaveformParam = new HtmlQuote(Cache.Item.FlatnessGeneratePrescanAODWaveformParam.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.OpticsMagTypeProductivityInformations.Contains(Cache.ProductivityInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            if (Cache.Item.CIBInformations.Count == 0)
            {
                const string comment = "Please select CIB Information.";
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment(comment), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(comment, dialogButtonsEnum: DialogButtonsEnum.OK, dialogIconEnum: DialogIconEnum.Warning);

                return false;
            }

            if (Cache.Item.CIBInformations.Select(t => t.PMTId).Distinct().Count() != 1)
            {
                const string comment = "Selected channels must use the same PMT Id.";
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment(comment), HtmlLogUniqueId.LoggingHtml());
                DialogWindowProvider.ShowDialog(comment, dialogButtonsEnum: DialogButtonsEnum.OK, dialogIconEnum: DialogIconEnum.Warning);

                return false;
            }

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                CIBInformations = new HtmlTable([.. Cache.Item.CIBInformations.OrderBy(t => t.ChannelId).Select(t => t.ToHtmlAnonymous())]),
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.Item.MicroscopeLensInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation)
                   && Cache.Item.CIBInformations.All(ApplicationCookie.CIBInformations.Contains);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
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
                CIBInformations = new HtmlTable([.. Cache.Item.CIBInformations.OrderBy(t => t.ChannelId).Select(t => t.ToHtmlAnonymous())]),
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.HazeFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                CIBInformations = new HtmlTable([.. Cache.Item.CIBInformations.OrderBy(t => t.ChannelId).Select(t => t.ToHtmlAnonymous())]),
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.HazeFindBFMachinePosition,
                Cache.Item.ImageWidth,
                FlatnessGeneratePrescanAODWaveformParam = new HtmlQuote(Cache.Item.FlatnessGeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
                Cache.Item.StartPrescanFrequency,
                Cache.Item.StepPrescanFrequency,
                Cache.Item.StopPrescanFrequency,
                Cache.RSquaredThreshold,
                Cache.MinFitPoints,
                Cache.ExcludedEdgePixelCount,
                Cache.MaxSubpixelOffset,
                ImageFileDirectory,
                Cache.ProductivityInformation.OriginYPixels
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem = new AODAlignmentDTO
            {
                ProductivityInformation = Cache.ProductivityInformation,
                ChannelItems =
                [
                    .. Cache.Item.CIBInformations
                        .OrderBy(t => t.ChannelId)
                        .Select(t => new AODAlignmentChannel { CIBInformation = t.Clone() })
                ]
            };

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetBrightFieldAbsoluteStageXy(hazeBFPosition, CalChipSiteModelEnum.HazeModel);

            try
            {
                Logger.LogHtmlInformation("Alignment", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var prescanFrequencies = Generate.LinearRange(Cache.Item.StartPrescanFrequency, Cache.Item.StepPrescanFrequency, Cache.Item.StopPrescanFrequency);
                Guard.IsNotEmpty(prescanFrequencies);

                foreach (var prescanFrequency in prescanFrequencies)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var generatePrescanAODWaveformParam = Cache.Item.FlatnessGeneratePrescanAODWaveformParam.Clone();

                    generatePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
                    generatePrescanAODWaveformParam.WithFrequencyFlatness(prescanFrequency);
                    generatePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
                    var aodWaveformResultItem = AODWaveformGenerator1.GeneratePrescanAODWaveform(generatePrescanAODWaveformParam.AdaptTo(), cancellationToken);

                    var prescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResultItem);
                    foreach (var aodWaveProfile in prescanAODWaveformProfiles) aodWaveProfile.ApplyCoefficient(Cache.Item.LaserLightInformation.Coefficient);
                    LaserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, prescanAODWaveformProfiles);

                    var cibInformations = Cache.Item.CIBInformations.OrderBy(t => t.ChannelId).ToArray();
                    var darkFieldImages = await CIBViewModel.GetPMTImagesAsync(
                        Cache.ProductivityInformation,
                        StageCoordinateSystemEnum.Dark,
                        hazeBFPosition,
                        Cache.Item.ImageWidth,
                        cibInformations,
                        (false, CalChipSiteModelEnum.HazeModel),
                        (false, Cache.Item.OpticsConfiguration),
                        (false, Cache.Item.CIBConfiguration),
                        (true, null),
                        false,
                        cancellationToken);

                    foreach (var darkFieldImage in darkFieldImages.OrderBy(image => image.CIBInformation.ChannelId))
                    {
                        using (darkFieldImage)
                        {
                            var channel = CalibratingItem.ChannelItems.Single(t => t.CIBInformation == darkFieldImage.CIBInformation);
                            var item = new AODAlignmentDTOItem
                            {
                                PrescanFrequency = prescanFrequency,
                                PrescanAODWaveformProfiles = [.. prescanAODWaveformProfiles.Select(profile => profile.Clone())],
                                PrescanAODWaveformResultFilePath = aodWaveformResultItem.FilePath
                            };

                            var imageFilePath = Path.Combine(ImageFileDirectory, $"{channel.CIBInformation}", $"{item.PrescanFrequency:0.###}MHz", $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                            darkFieldImage.Image.SaveImage(imageFilePath);

                            item.ImageFilePath = imageFilePath;
                            item.RawImageFilePath = darkFieldImage.RawImageFilePath;
                            item.ImageHorizontalProjects = darkFieldImage.Image.GetHorizontalProjects();

                            var peakStatus = AssignPeak(item);
                            channel.Items = [.. channel.Items, item];

                            Logger.LogHtmlInformation($"{channel.CIBInformation} {item.PrescanFrequency:0.###}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                            {
                                CIBInformation = channel.CIBInformation.ToHtmlAnonymous(),
                                FlatnessGeneratePrescanAODWaveformParam = new HtmlQuote(generatePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
                                item.PrescanAODWaveformResultFilePath,
                                PrescanAODWaveformProfiles = new HtmlTable([.. item.PrescanAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())]),
                                item.RawImageFilePath,
                                item.PeakInteger,
                                item.PeakPosition,
                                PeakStatus = peakStatus,
                                HtmlImage = new HtmlImage(item.ImageFilePath)
                            }), HtmlLogUniqueId.LoggingHtml());
                        }
                    }
                }

                FitAlignment(CalibratingItem);

                var htmlBullet = new HtmlBullet(new
                {
                    CalibratingItem.Slope,
                    CalibratingItem.Intercept,
                    ChannelItems = new HtmlTable([
                        .. CalibratingItem.ChannelItems.Select(t => new
                        {
                            CIBInformation = t.CIBInformation.ToHtmlAnonymous(),
                            t.FitStatus,
                            t.Slope,
                            t.Intercept,
                            t.RSquared,
                            t.LeftRemovedCount,
                            t.RightRemovedCount,
                            t.KeptPointCount,
                            t.FitStartFrequency,
                            t.FitStopFrequency
                        })
                    ]),
                    PlotDataSource = new HtmlContainer(
                    [
                        .. CalibratingItem.PlotDataSource.GetAllHtmlPlot2DLinesCharts(),
                        .. CalibratingItem.ChannelItems.SelectMany(t => t.PlotDataSource.GetAllHtmlPlot2DLinesCharts())
                    ])
                });

                if (CalibratingItem.IsCalibrated)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                Guard.IsTrue(Save([CalibratingItem], cancellationToken));

                return CalibratingItem.IsCalibrated;
            }
            finally
            {
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetBrightFieldAbsoluteStageXy(hazeBFPosition, CalChipSiteModelEnum.HazeModel);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        if (SelectedReviewItems.Count == 0)
        {
            DialogWindowProvider.ShowDialog("Please select a review item!", dialogButtonsEnum: DialogButtonsEnum.OK, dialogIconEnum: DialogIconEnum.Warning);

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
                    Cache.ProductivityInformation,
                    Cache.RSquaredThreshold
                }), HtmlLogUniqueId.LoggingHtml());

                string? generateError = null;

                if (selectedReviewItem.IsCalibrated)
                {
                    try
                    {
                        GenerateReviewPrescanAODWaveform(selectedReviewItem, cancellationToken);
                        selectedReviewItem.IsVerified = true;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        selectedReviewItem.IsVerified = false;
                        generateError = ex.Message;
                        Logger.LogError(ex, "Generate Prescan AOD Waveform Failed: {Productivity}", selectedReviewItem.ProductivityInformation);
                    }
                }

                var htmlBullet = new HtmlBullet(new
                {
                    selectedReviewItem.Slope,
                    selectedReviewItem.Intercept,
                    Cache.DeltaPixels,
                    ImageHeight = Cache.ProductivityInformation.OriginYPixels + Cache.DeltaPixels,
                    GeneratePrescanAODWaveformParam = new HtmlQuote(selectedReviewItem.GeneratePrescanAODWaveformParam.ToHtmlAnonymous()),
                    selectedReviewItem.PrescanAODWaveformResultFilePath,
                    PrescanAODWaveformProfiles = new HtmlTable([.. selectedReviewItem.PrescanAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())]),
                    ChannelItems = new HtmlTable([
                        .. selectedReviewItem.ChannelItems.Select(t => new
                        {
                            CIBInformation = t.CIBInformation.ToHtmlAnonymous(),
                            t.FitStatus,
                            t.Slope,
                            t.Intercept,
                            t.RSquared
                        })
                    ]),
                    PlotDataSource = new HtmlContainer(
                    [
                        .. selectedReviewItem.PlotDataSource.GetAllHtmlPlot2DLinesCharts(),
                        .. selectedReviewItem.ChannelItems.SelectMany(t => t.PlotDataSource.GetAllHtmlPlot2DLinesCharts())
                    ])
                });

                if (selectedReviewItem.IsOk)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                {
                    errorMessageStringBuilder.AppendLine(generateError is null ? $"{title}: Error" : $"{title}: {generateError}");
                    Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                }
            }

            Guard.IsTrue(Save(SelectedReviewItems, cancellationToken));

            var result = SelectedReviewItems.All(t => t.IsOk);

            DialogWindowProvider.ShowDialog($"""
                                             Verify : {(result ? "OK" : "Failed")}
                                             {errorMessageStringBuilder}
                                             """,
                dialogButtonsEnum: DialogButtonsEnum.OK,
                dialogIconEnum: result ? DialogIconEnum.Information : DialogIconEnum.Warning);

            return result;
        }).ConfigureAwait(false);
    }

    private void GenerateReviewPrescanAODWaveform(AODAlignmentDTO review, CancellationToken cancellationToken)
    {
        review.PrescanAODWaveformProfiles = [];
        review.PrescanAODWaveformResultFilePath = string.Empty;

        var deltaPixels = Cache.DeltaPixels;
        Guard.IsGreaterThanOrEqualTo(deltaPixels, 0);

        var imageHeight = review.ProductivityInformation.OriginYPixels + deltaPixels;
        Guard.IsGreaterThan(imageHeight, 0);

        Guard.IsTrue(double.IsFinite(review.Slope) && review.Slope != 0d, name: nameof(review.Slope));
        Guard.IsTrue(double.IsFinite(review.Intercept), name: nameof(review.Intercept));

        double yPixelRate = CIBViewModel.GetYSamplingRate(); //450_000_000d;
        var headerFrequency = -review.Intercept / review.Slope;
        var footerFrequency = (imageHeight - review.Intercept) / review.Slope;

        var param = Cache.Item.FlatnessGeneratePrescanAODWaveformParam.Clone();
        param.ProductivityInformation = review.ProductivityInformation.Clone();
        param.DirectoryPath = AODWaveformDirectoryPath;
        param.IsHeaderAndFooter = true;
        param.HeaderFrequency = headerFrequency;
        param.FooterFrequency = footerFrequency;
        param.FlatnessTime = imageHeight / yPixelRate * 1_000_000_000d;

        Guard.IsTrue(double.IsFinite(param.HeaderFrequency) && double.IsFinite(param.FooterFrequency) && double.IsFinite(param.FlatnessTime));

        var aodWaveformResult = AODWaveformGenerator1.GeneratePrescanAODWaveform(param.AdaptTo(), cancellationToken);

        review.GeneratePrescanAODWaveformParam = param;
        review.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);
        review.PrescanAODWaveformResultFilePath = aodWaveformResult.FilePath;
    }

    private string AssignPeak(AODAlignmentDTOItem item)
    {
        var edge = Cache.ExcludedEdgePixelCount;
        var projection = item.ImageHorizontalProjects;
        var peakIndex = projection.Index().MaxBy(p => p.Item).Index;

        if (peakIndex < edge || peakIndex > projection.Length - 1 - edge)
        {
            item.PeakInteger = null;
            item.PeakPosition = null;
            item.IsPeakValid = false;

            return "BorderPeak";
        }

        item.PeakInteger = peakIndex;
        item.IsPeakValid = true;

        var denominator = projection[peakIndex - 1] - 2d * projection[peakIndex] + projection[peakIndex + 1];
        var offset = (projection[peakIndex - 1] - projection[peakIndex + 1]) / (2d * denominator);
        var isSubpixel = double.IsFinite(denominator)
                         && denominator < 0d
                         && double.IsFinite(offset)
                         && Math.Abs(offset) <= Cache.MaxSubpixelOffset;
        item.PeakPosition = isSubpixel ? peakIndex + offset : peakIndex;

        return isSubpixel ? "Valid" : "SubpixelFallback";
    }

    private void FitAlignment(AODAlignmentDTO calibratingItem)
    {
        calibratingItem.IsCalibrated = false;

        foreach (var channel in calibratingItem.ChannelItems) FitChannel(channel);

        if (calibratingItem.ChannelItems.All(channel => channel.FitStatus == "Qualified") == false) return;

        calibratingItem.Slope = calibratingItem.ChannelItems.Average(channel => channel.Slope);
        calibratingItem.Intercept = calibratingItem.ChannelItems.Average(channel => channel.Intercept);
        calibratingItem.IsCalibrated = true;
    }

    private void FitChannel(AODAlignmentChannel channel)
    {
        var ordered = channel.Items.OrderBy(item => item.PrescanFrequency).ToArray();
        var isInvalidInput = ordered.Any(item => double.IsFinite(item.PrescanFrequency) == false)
                             || ordered.Zip(ordered.Skip(1), (previous, current) => current.PrescanFrequency.Equals(previous.PrescanFrequency)).Any(isDuplicate => isDuplicate);

        if (isInvalidInput || ordered.Length < Cache.MinFitPoints)
        {
            channel.FitStatus = isInvalidInput ? "InvalidInput" : "InsufficientPoints";

            return;
        }

        for (var removed = 0; removed <= ordered.Length - Cache.MinFitPoints; removed++)
        {
            var best = (RSquared: double.NaN, Left: -1, Slope: 0d, Intercept: 0d);

            for (var left = 0; left <= removed; left++)
            {
                var window = ordered.Skip(left).Take(ordered.Length - removed).ToArray();

                if (IsQualifiedWindow(window) == false) continue;

                if (FitLine(window) is not { } fit || fit.RSquared <= Cache.RSquaredThreshold) continue;

                if (fit.RSquared <= best.RSquared) continue;

                best = (fit.RSquared, left, fit.Slope, fit.Intercept);
            }

            if (best.Left < 0) continue;

            var windowLength = ordered.Length - removed;
            channel.Slope = best.Slope;
            channel.Intercept = best.Intercept;
            channel.RSquared = best.RSquared;
            channel.LeftRemovedCount = best.Left;
            channel.RightRemovedCount = removed - best.Left;
            channel.KeptPointCount = windowLength;
            channel.FitStartFrequency = ordered[best.Left].PrescanFrequency;
            channel.FitStopFrequency = ordered[best.Left + windowLength - 1].PrescanFrequency;
            channel.FitAlignmentPoints =
            [
                .. ordered.Skip(best.Left).Take(windowLength).Select(item => new Point(item.PrescanFrequency, best.Intercept + best.Slope * item.PrescanFrequency))
            ];
            channel.FitStatus = "Qualified";

            return;
        }

        channel.FitStatus = "NoQualifiedWindow";

        return;

        bool IsQualifiedWindow(AODAlignmentDTOItem[] window)
        {
            if (window.Any(item => item.IsPeakValid == false)) return false;

            return window.Any(item => item.PeakInteger != window[0].PeakInteger);
        }
    }

    private static (double Slope, double Intercept, double RSquared)? FitLine(AODAlignmentDTOItem[] window)
    {
        var points = window.Select(item => (X: item.PrescanFrequency, Y: item.PeakPosition!.Value)).ToArray();

        var meanX = points.Average(point => point.X);
        var meanY = points.Average(point => point.Y);
        var centered = points.Select(point => (DX: point.X - meanX, DY: point.Y - meanY)).ToArray();
        var sxx = centered.Sum(point => point.DX * point.DX);
        var sxy = centered.Sum(point => point.DX * point.DY);
        var sst = centered.Sum(point => point.DY * point.DY);

        if (sxx <= 0d || sst <= 0d || double.IsFinite(sxx) == false || double.IsFinite(sxy) == false || double.IsFinite(sst) == false) return null;

        var slope = sxy / sxx;
        var intercept = meanY - slope * meanX;

        if (double.IsFinite(slope) == false || double.IsFinite(intercept) == false) return null;

        var sse = centered.Sum(point => (point.DY - slope * point.DX) * (point.DY - slope * point.DX));

        if (double.IsFinite(sse) == false) return null;

        return (slope, intercept, 1d - sse / sst);
    }

    private bool Save(IReadOnlyList<AODAlignmentDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
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
        var temps = Guard.IsAssignableToTypeAndReturn<AODAlignmentDTO[]>(calibrations);
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