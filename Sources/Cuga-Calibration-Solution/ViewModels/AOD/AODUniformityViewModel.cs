using System.IO;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.AOD.Uniformity;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics;
using Microsoft.Extensions.Hosting;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.AOD;

[IOCAppService(ServiceType = typeof(AODUniformityViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AODUniformityViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Select Laser Light Information" },
        new() { StepName = "Image Param" },
        new() { StepName = "Find Haze Position" },
        new() { StepName = "Forward & Reverse & Mapping" },
        new() { StepName = "Initialize Window" },
        new() { StepName = "Uniformity" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private AODUniformityDTO _calibratingItem = new();

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationAndLaserLightInformationStatus> _calibratingStatuses = [];

    #endregion Calibrate

    [ObservableProperty]
    private IReadOnlyList<AODUniformityDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<AODUniformityDTO> _selectedReviewItems = [];

    #endregion 界面相关

    #region 缓存

    [ObservableProperty]
    private AODUniformityCache _cache = new();

    [ObservableProperty]
    private AODUniformityDTO[] _calibrations = [];

    [ObservableProperty]
    private MicroscopeCalChipDto _microscopeCalChip = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (CalibrationStatusService.GetAdsCalibrationIsOKStatus() == false)
        {
            DialogWindowProvider.ShowDialog("The ADS precondition is Failure", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoItemsIsOKStatus<MicroscopeFocusItemDto>(out _, out var errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<MicroscopeCalChipDto>(out var microscopeCalChip, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        MicroscopeCalChip = microscopeCalChip;

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<LaserAutoFocusDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibrationStatusService.GetCalibrationDtoIsOKStatus<LaserBeamStabilizerObjDto>(out _, out errorMessage) == false)
        {
            DialogWindowProvider.ShowDialog($"precondition is Failure,Error:{errorMessage}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return false;
        }

        if (CalibratingStatuses.Count == 0)
            CalibratingStatuses =
            [
                .. ApplicationCookie.OpticsMagTypeProductivityInformations
                    .Select(t => new ProductivityInformationAndLaserLightInformationStatus
                    {
                        SelectedItem = t,
                        Items = [..ApplicationCookie.LaserLightInformations.Select(tt => new LaserLightInformationStatus { SelectedItem = tt })]
                    })
            ];

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<AODUniformityCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<AODUniformityDTO>();

        Calibrations =
        [
            .. Calibrations
                .Where(t => ApplicationCookie.OpticsMagTypeProductivityInformations.Contains(t.ProductivityInformation)
                            && ApplicationCookie.LaserLightInformations.Contains(t.LaserLightInformation))
                .Select(t =>
                {
                    t.Items = [..t.Items.Where(tt => ApplicationCookie.CIBInformations.Contains(tt.CIBInformation))];

                    CalibratingStatuses
                        .Single(tt => tt.SelectedItem == t.ProductivityInformation)
                        .Items
                        .Single(tt => tt.SelectedItem == t.LaserLightInformation)
                        .IsCalibrated = t.IsCalibrated;

                    return t;
                })
        ];

        if (isHasCache == false) RecipeCacheProvider.Set(Cache, cancellationToken);

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
                return true;

            case 4:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition));

                return true;

            case 5:
                return true;

            case 6:
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
                return true;

            case 1:
                CalibratingItem = new AODUniformityDTO(ApplicationCookie.CIBInformationPMTIds);

                return true;

            case 2:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition != Point.Origin
                    ? Cache.Item.HazeFindBFMachinePosition
                    : GuardUtils.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition));

                return true;

            case 3:
                return true;

            case 4:
                return true;

            case 5:
                return true;

            case 6:
                CalibratingStatuses
                    .Single(t => t.SelectedItem == Cache.ProductivityInformation)
                    .Items
                    .Single(tt => tt.SelectedItem == Cache.LaserLightInformation)
                    .IsCalibrated = true;

                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

                IsCalibrated = CalibratingStatuses.All(s => s.IsCalibrated);
                if (IsCalibrated == false) CalibrationStepIndex = -1;

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
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.OpticsMagTypeProductivityInformations.Contains(Cache.ProductivityInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.LaserLightInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.LaserLightInformations.Contains(Cache.LaserLightInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step2Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.LaserLightInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.CIBInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.Item.MicroscopeLensInformation)
                   && ApplicationCookie.CIBInformations.Contains(Cache.Item.CIBInformation);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Guard.IsEqualTo(Cache.Item.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            StageViewModel.SetAbsoluteStageTheta(0);
            Cache.Item.HazeFindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.LaserLightInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.CIBInformation,
                Cache.Item.HazeFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step4Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            Guard.IsGreaterThanOrEqualTo(Cache.Item.PrescanAODWaveformProfileSegmentCount, 4);
            Guard.IsTrue((Cache.Item.PrescanAODWaveformProfileSegmentCount & 1) == 0, "It must be even number!");
            Guard.IsTrue(Cache.ProductivityInformation.YPixel % Cache.Item.ImageSegmentCount == 0, $"Servings must be a factor of {Cache.ProductivityInformation.YPixel}!");

            var detectImageDirectory = ImageFileDirectory;

            var cibInformations = ApplicationCookie.CIBInformations;
            var prescanAODWaveformProfiles = ConfigureViewModel.GetPrescanAODWaveProfiles(Cache.ProductivityInformation);
            var startPrescanAODWaveformProfileSegmentIndex = Cache.Item.PrescanAODWaveformProfileSegmentCount / 2 - 1;
            var stopPrescanAODWaveformProfileSegmentIndex = Cache.Item.PrescanAODWaveformProfileSegmentCount / 2 + 1;
            var prescanAODWaveformProfileSegmentIndexes = GenerateUtils.LinearIndexRange(0, Cache.Item.PrescanAODWaveformProfileSegmentCount - 1).ToArray();

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.LaserLightInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.CIBInformation,
                Cache.Item.HazeFindBFMachinePosition,
                Cache.Item.ImageWidth,
                Cache.Item.PrescanAODWaveformProfileSegmentCount,
                Cache.Item.ImageSegmentCount,
                CIBInformations = new HtmlExpand(string.Empty, new HtmlTable([.. cibInformations.Select(t => t.ToHtmlAnonymous())])),
                PrescanAODWaveformProfiles = new HtmlTable([.. prescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
                startPrescanAODWaveformProfileSegmentIndex,
                stopPrescanAODWaveformProfileSegmentIndex,
                prescanAODWaveformProfileSegmentIndexes,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.ProductivityInformation = Cache.ProductivityInformation;
            CalibratingItem.StartWindowItem = new AODUniformityDTO.WindowItem();
            CalibratingItem.StopWindowItem = new AODUniformityDTO.WindowItem();
            CalibratingItem.MappingWindowItem = new AODUniformityDTO.WindowItem();
            CalibratingItem.ImageHorizontalProjectMapping = [];
            CalibratingItem.PrescanAODWaveformProfileMapping = [];

            CIBViewModel.ToggleEnableAGC(cibInformations, true);
            CIBViewModel.ToggleProfileMode(cibInformations, CIBProfileModeEnum.PMTLog);

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0);
            StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);

            Logger.LogHtmlInformation("Forward and Reverse", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            try
            {
                CalibratingItem.StartWindowItem.Window = GetAndApplyWindow(startPrescanAODWaveformProfileSegmentIndex);
                await CatchImageAsync($"{startPrescanAODWaveformProfileSegmentIndex}", CalibratingItem.StartWindowItem);
                CalibratingItem.StartWindowItem.CalculateProjectMinPixel(Cache.ProductivityInformation.YPixel, Cache.Item.PrescanAODWaveformProfileSegmentCount, startPrescanAODWaveformProfileSegmentIndex);

                CalibratingItem.StopWindowItem.Window = GetAndApplyWindow(stopPrescanAODWaveformProfileSegmentIndex);
                await CatchImageAsync($"{stopPrescanAODWaveformProfileSegmentIndex}", CalibratingItem.StopWindowItem);
                CalibratingItem.StopWindowItem.CalculateProjectMinPixel(Cache.ProductivityInformation.YPixel, Cache.Item.PrescanAODWaveformProfileSegmentCount, stopPrescanAODWaveformProfileSegmentIndex);

                var (mappingWindow, mappingRegions) = GetAndApplyMappingWindow();
                CalibratingItem.MappingWindowItem.Window = mappingWindow;
                await CatchImageAsync("All", CalibratingItem.MappingWindowItem);
                if (CalibratingItem.IsReverse)
                    CalibratingItem.MappingWindowItem.ImageHorizontalProjects = [.. CalibratingItem.MappingWindowItem.ImageHorizontalProjects.Reverse()];

                CalibratingItem.MappingWindowItem.CalculateProjectMinPixels(Cache.ProductivityInformation.YPixel, Cache.Item.PrescanAODWaveformProfileSegmentCount, prescanAODWaveformProfileSegmentIndexes);

                var imageHorizontalProjectIndex = Enumerable.Range(0, Cache.ProductivityInformation.YPixel).ToArray();
                CalibratingItem.ImageHorizontalProjectMapping = [..imageHorizontalProjectIndex.ChunkSplitEvenly(Cache.Item.ImageSegmentCount)];

                var period = ((double)mappingRegions[^1].VMiddleIndex - mappingRegions[0].VMiddleIndex) / ((double)CalibratingItem.MappingWindowItem.ProjectMinPixels[^1] - CalibratingItem.MappingWindowItem.ProjectMinPixels[0]);
                var startPrescanAODWaveformProfileIndex = (int)Math.Floor(mappingRegions[0].VMiddleIndex - (CalibratingItem.MappingWindowItem.ProjectMinPixels[0] - imageHorizontalProjectIndex[0]) * period);
                var stopPrescanAODWaveformProfileIndex = (int)Math.Floor(mappingRegions[^1].VMiddleIndex + (imageHorizontalProjectIndex[^1] - CalibratingItem.MappingWindowItem.ProjectMinPixels[^1]) * period);

                Logger.LogHtmlInformation("T", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    period,
                    startPrescanAODWaveformProfileIndex,
                    stopPrescanAODWaveformProfileIndex
                }), HtmlLogUniqueId.LoggingHtml());

                Guard.IsGreaterThanOrEqualTo(startPrescanAODWaveformProfileIndex, 0);
                Guard.IsLessThan(startPrescanAODWaveformProfileIndex, mappingWindow.Length);
                Guard.IsGreaterThanOrEqualTo(stopPrescanAODWaveformProfileIndex, 0);
                Guard.IsLessThan(stopPrescanAODWaveformProfileIndex, mappingWindow.Length);

                var mappingDetails = mappingRegions.Index().Select(t => (t.Item.VMiddleIndex, ProjectMinPixel: CalibratingItem.MappingWindowItem.ProjectMinPixels[t.Index])).ToArray();
                var mapping = new int[imageHorizontalProjectIndex.Length][];

                var pixels = GenerateUtils.LinearIndexRange(0, CalibratingItem.MappingWindowItem.ProjectMinPixels[0]);
                foreach (var (index, item) in GenerateUtils.LinearIndexRange(startPrescanAODWaveformProfileIndex, mappingRegions[0].VMiddleIndex).ChunkSplitEvenly(pixels.Length).Index())
                {
                    mapping[pixels[index]] = item;
                }

                foreach (var ((firstVMiddleIndex, firstProjectMinPixel), (secondVMiddleIndex, secondProjectMinPixel)) in mappingDetails
                             .Zip(mappingDetails.Skip(1), (t1, t2) => (First: t1, Second: t2)))
                {
                    pixels = GenerateUtils.LinearIndexRange(secondProjectMinPixel, firstProjectMinPixel);

                    foreach (var (index, item) in GenerateUtils.LinearIndexRange(firstVMiddleIndex, secondVMiddleIndex).ChunkSplitEvenly(pixels.Length).Index())
                    {
                        mapping[pixels[index]] = item;
                    }
                }


                pixels = GenerateUtils.LinearIndexRange(CalibratingItem.MappingWindowItem.ProjectMinPixels[^1], imageHorizontalProjectIndex[^1]);
                foreach (var (index, item) in GenerateUtils.LinearIndexRange(CalibratingItem.MappingWindowItem.ProjectMinPixels[^1], stopPrescanAODWaveformProfileIndex).ChunkSplitEvenly(pixels.Length).Index())
                {
                    mapping[pixels[index]] = item;
                }

                CalibratingItem.PrescanAODWaveformProfileMapping =
                [
                    ..CalibratingItem.ImageHorizontalProjectMapping
                        .Select(t => t.SelectMany(tt => mapping[tt]).ToArray())
                ];

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    CalibratingItem.IsReverse,
                    Plot = new HtmlContainer(CalibratingItem.ForwardAndReverseScatterPlotControl.GetAllHtmlPlot2DLinesCharts())
                }), HtmlLogUniqueId.LoggingHtml());

                return true;

                async Task CatchImageAsync(string title, AODUniformityDTO.WindowItem windowItem)
                {
                    using var darkFieldImage = await CIBViewModel.GetPMTImagesAsync(
                        Cache.ProductivityInformation,
                        StageCoordinateSystemEnum.Dark,
                        hazeBFPosition,
                        Cache.Item.CIBInformation,
                        Cache.Item.ImageWidth,
                        (false, CalChipSiteModelEnum.HazeModel),
                        (true, null),
                        (true, null),
                        false,
                        cancellationToken);

                    var imageFilePath = Path.Combine(detectImageDirectory, Cache.Item.CIBInformation.ToString(), title, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                    darkFieldImage.Image.Save(imageFilePath);

                    windowItem.ImageFilePath = imageFilePath;
                    windowItem.RawImageFilePath = darkFieldImage.RawImageFilePath;
                    windowItem.ImageHorizontalProjects = darkFieldImage.Image.GetHorizontalProjects();

                    Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        windowItem.RawImageFilePath,
                        Image = new HtmlImage(windowItem.ImageFilePath)
                    }), HtmlLogUniqueId.LoggingHtml());
                }

                double[] GetAndApplyWindow(int segmentIndex)
                {
                    var prescanAODWaveformProfileTotalLength = prescanAODWaveformProfiles[0].Shorts.Count;
                    var prescanAODWaveformProfileSegmentWidth = prescanAODWaveformProfileTotalLength / Cache.Item.PrescanAODWaveformProfileSegmentCount;

                    var window = Generate.LinearVShapeWindow(
                        Cache.LaserLightInformation.Coefficient,
                        Cache.LaserLightInformation.Coefficient / 1000d,
                        segmentIndex * prescanAODWaveformProfileSegmentWidth,
                        prescanAODWaveformProfileSegmentWidth,
                        prescanAODWaveformProfileTotalLength).Window;

                    foreach (var prescanAODWaveformProfile in prescanAODWaveformProfiles) prescanAODWaveformProfile.ApplyCoefficientWindowList(window);
                    LaserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, prescanAODWaveformProfiles);

                    return window;
                }

                (double[] Window, (int VStartIndex, int VMiddleIndex, int VStopIndex)[] Regions) GetAndApplyMappingWindow()
                {
                    var prescanAODWaveformProfileTotalLength = prescanAODWaveformProfiles[0].Shorts.Count;
                    var prescanAODWaveformProfileSegmentWidth = prescanAODWaveformProfileTotalLength / Cache.Item.PrescanAODWaveformProfileSegmentCount;

                    var (window, regions) = Generate.LinearVShapeWindow(
                        Cache.LaserLightInformation.Coefficient,
                        Cache.LaserLightInformation.Coefficient / 1000d,
                        [..prescanAODWaveformProfileSegmentIndexes.Select(t => t * prescanAODWaveformProfileSegmentWidth)],
                        prescanAODWaveformProfileSegmentWidth,
                        prescanAODWaveformProfileTotalLength);

                    foreach (var prescanAODWaveformProfile in prescanAODWaveformProfiles) prescanAODWaveformProfile.ApplyCoefficientWindowList(window);
                    LaserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, prescanAODWaveformProfiles);

                    return (window, regions);
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
                CIBViewModel.ToggleEnableAGC(cibInformations, true);
                CIBViewModel.ToggleProfileMode(cibInformations, CIBProfileModeEnum.PMTLog);
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step5Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;

            var cibInformations = ApplicationCookie.CIBInformations;
            var prescanAODWaveformProfiles = ConfigureViewModel.GetPrescanAODWaveProfiles(Cache.ProductivityInformation);
            var cibDelays = CIBViewModel.GetDelays(cibInformations);

            try
            {
                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.ProductivityInformation,
                    Cache.Item.MicroscopeLensInformation,
                    Cache.Item.LaserLightInformation,
                    Cache.Item.CIBInformation,
                    Cache.Item.HazeFindBFMachinePosition,
                    Cache.Item.ImageWidth,
                    Cache.Item.PrescanAODWaveformProfileSegmentCount,
                    Cache.CalibratingRetryTimes,
                    Cache.CalibratingThreshold,
                    CIBInformations = new HtmlExpand(string.Empty, new HtmlTable([.. cibInformations.Select(t => t.ToHtmlAnonymous())])),
                    PrescanAODWaveformProfiles = new HtmlTable([.. prescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
                    AODDelays = new HtmlExpand(string.Empty, new HtmlTable([.. cibDelays.Select(t => t.ToHtmlAnonymous())])),
                    detectImageDirectory
                }), HtmlLogUniqueId.LoggingHtml());

                CalibratingItem.Items =
                [
                    .. cibInformations.Select(t => new AODUniformityDTOItem
                    {
                        CIBInformation = t,
                        Delay = cibDelays.Single(tt => tt.CIBInformation == t).PMTDelay
                    })
                ];
                CalibratingItem.TargetPixelValues = [];

                CIBViewModel.ToggleEnableAGC(cibInformations, true);
                CIBViewModel.ToggleProfileMode(cibInformations, CIBProfileModeEnum.PMTLog);
                CIBViewModel.SetDelays(cibDelays);

                var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition);
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);

                Logger.LogHtmlInformation("Uniformity", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var segmentIndex = Cache.Item.PrescanAODWaveformProfileSegmentCount / 2;
                var window = GetAndApplyWindow(segmentIndex, prescanAODWaveformProfiles);

                var times = 0;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    var cibPMTImages = await CIBViewModel.GetPMTImagesAsync(
                        Cache.ProductivityInformation,
                        StageCoordinateSystemEnum.Dark,
                        hazeBFPosition,
                        cibInformations,
                        Cache.Item.ImageWidth,
                        (true, null),
                        (true, null),
                        (false, Cache.Item.LaserLightInformation),
                        false,
                        cancellationToken);

                    Logger.LogHtmlInformation("Images", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

                    foreach (var (index, darkFieldImage) in cibPMTImages.Index())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        using var _ = darkFieldImage;
                        var itemItem = CalibratingItem.Items[index];

                        var imageFilePath = Path.Combine(detectImageDirectory, itemItem.CIBInformation.ToString(), $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                        darkFieldImage.Image.Save(imageFilePath);

                        var itemItemData = new AODUniformityDTOItem.Item
                        {
                            Window = window,
                            ImageHorizontalProjects = darkFieldImage.Image.GetHorizontalProjects(),
                            RawImageFilePath = darkFieldImage.RawImageFilePath,
                            ImageFilePath = imageFilePath
                        };
                        itemItemData.CalculateProjectMinPixel(Cache.ProductivityInformation.YPixel, Cache.Item.PrescanAODWaveformProfileSegmentCount, segmentIndex);

                        if (HostEnvironment.IsDevelopment()) itemItemData.ProjectMinPixel += Random.Shared.RandomInteger(-10, 10);
                        itemItem.Items = [.. itemItem.Items, itemItemData];

                        Logger.LogHtmlInformation(itemItem.CIBInformation.ToString(), HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                        {
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
                    foreach (var (pmtId, itemItems) in results)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var pmtIdTargetPixelValue = CalibratingItem.TargetPixelValues.GetOrAdd(pmtId, itemItems.Single(t => t.CIBInformation.ChannelId == Cache.Item.CIBInformation.ChannelId).Items[times].ProjectMinPixel);

                        foreach (var itemItem in itemItems)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            itemItem.Items[times].Error = itemItem.Items[times].ProjectMinPixel - pmtIdTargetPixelValue;
                            if (itemItem.Items.Any(t => t.IsOk))
                            {
                                itemItem.Items[times].IsOk = true;
                                resultList.Add(itemItem.Items[times].IsOk);

                                continue;
                            }

                            itemItem.Items[times].IsOk = Math.Abs(itemItem.Items[times].Error) <= Cache.CalibratingThreshold;
                            resultList.Add(itemItem.Items[times].IsOk);

                            if (itemItem.Items[times].IsOk) continue;

                            itemItem.Delay += (CalibratingItem.IsReverse ? -1 : 1) * itemItem.Items[times].Error;

                            CIBViewModel.SetDelays([cibDelays.Single(t => t.CIBInformation == itemItem.CIBInformation).WithPMTDelay(itemItem.Delay)]);
                        }
                    }

                    var htmlBullet = new HtmlBullet(new
                    {
                        times,
                        CalibratingItem.ProductivityInformation,
                        Plot = new HtmlContainer([.. CalibratingItem.ScatterPlotControls.Select(t => new HtmlExpand(t.Key.ToString(), new HtmlContainer(t.Value.GetAllHtmlPlot2DLinesCharts())))])
                    });

                    CalibratingItem.IsCalibrated = resultList.All(t => t);

                    if (CalibratingItem.IsCalibrated)
                    {
                        LogDetails(true);
                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                        break;
                    }

                    if (++times > Cache.CalibratingRetryTimes - 1)
                    {
                        LogDetails(false);
                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                        break;
                    }

                    Logger.LogHtmlInformation("Plots", HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                }

                Guard.IsTrue(Save([CalibratingItem], cancellationToken));

                return CalibratingItem.IsCalibrated;

                void LogDetails(bool isSuccess)
                {
                    Logger.LogHtmlInformation("Details", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                    foreach (var itemItem in CalibratingItem.Items)
                    {
                        var itemItemData = itemItem.Items[^1];
                        var htmlBullet = new HtmlBullet(new
                        {
                            itemItemData.RawImageFilePath,
                            Image = new HtmlImage(itemItemData.ImageFilePath)
                        });

                        if (isSuccess) Logger.LogHtmlInformation(itemItem.CIBInformation.ToString(), HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                        else Logger.LogHtmlError(itemItem.CIBInformation.ToString(), HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                    }
                }
            }
            finally
            {
                CIBViewModel.ToggleEnableAGC(cibInformations, true);
                CIBViewModel.ToggleProfileMode(cibInformations, CIBProfileModeEnum.PMTLog);
                CIBViewModel.SetDelays(cibDelays);
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition));
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

    private bool Save(IReadOnlyList<AODUniformityDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                .. Calibrations.Where(t => t.ProductivityInformation != dto.ProductivityInformation),
                dto
            ];
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}