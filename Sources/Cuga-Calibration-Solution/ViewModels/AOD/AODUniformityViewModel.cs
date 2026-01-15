using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.AOD.Uniformity;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.BeamStabilizer;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Focus;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Extensions;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using System.IO;
using System.Text;
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
                Cache.Item.CIBInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous())
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
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
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
            Guard.IsLessThanOrEqualTo(Cache.Item.ImageHorizontalProjectsSegmentCount, Cache.ProductivityInformation.YPixel);

            var detectImageDirectory = ImageFileDirectory;

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
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.HazeFindBFMachinePosition,
                Cache.Item.ImageWidth,
                Cache.Item.PrescanAODWaveformProfileSegmentCount,
                Cache.Item.ImageHorizontalProjectsSegmentCount,
                PrescanAODWaveformProfiles = new HtmlTable([.. prescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
                startPrescanAODWaveformProfileSegmentIndex,
                stopPrescanAODWaveformProfileSegmentIndex,
                prescanAODWaveformProfileSegmentIndexes,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.ProductivityInformation = Cache.ProductivityInformation;
            CalibratingItem.LaserLightInformation = Cache.LaserLightInformation;
            CalibratingItem.StartWindowItem = new AODUniformityDTO.WindowItem();
            CalibratingItem.StopWindowItem = new AODUniformityDTO.WindowItem();
            CalibratingItem.MappingWindowItem = new AODUniformityDTO.WindowItem();
            CalibratingItem.Mappings = [];
            CalibratingItem.ImageHorizontalProjectMappings = [];
            CalibratingItem.PrescanAODWaveformProfileMappings = [];

            OpticsViewModel.SetPolarizationMode(OpticsPolarizationModeEnum.P);

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0);
            StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);

            try
            {
                Logger.LogHtmlInformation("Images", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                CalibratingItem.StartWindowItem.Window = GetAndApplyWindow(startPrescanAODWaveformProfileSegmentIndex);
                await CatchImageAsync(
                    $"{startPrescanAODWaveformProfileSegmentIndex}",
                    hazeBFPosition,
                    detectImageDirectory,
                    CalibratingItem.StartWindowItem,
                    cancellationToken);
                CalibratingItem.StartWindowItem.CalculateHorizontalProjectMinPixel(Cache.Item.PrescanAODWaveformProfileSegmentCount, startPrescanAODWaveformProfileSegmentIndex);

                CalibratingItem.StopWindowItem.Window = GetAndApplyWindow(stopPrescanAODWaveformProfileSegmentIndex);
                await CatchImageAsync(
                    $"{stopPrescanAODWaveformProfileSegmentIndex}",
                    hazeBFPosition,
                    detectImageDirectory,
                    CalibratingItem.StopWindowItem,
                    cancellationToken);
                CalibratingItem.StopWindowItem.CalculateHorizontalProjectMinPixel(Cache.Item.PrescanAODWaveformProfileSegmentCount, stopPrescanAODWaveformProfileSegmentIndex);

                var (mappingWindow, mappingRegions) = GetWindow();
                CalibratingItem.MappingWindowItem.Window = mappingWindow;
                await CatchImageAsync("All",
                    hazeBFPosition,
                    detectImageDirectory,
                    CalibratingItem.MappingWindowItem,
                    cancellationToken);

                CalibratingItem.MappingWindowItem.CalculateHorizontalProjectMinPixels(Cache.Item.PrescanAODWaveformProfileSegmentCount, prescanAODWaveformProfileSegmentIndexes);

                Logger.LogHtmlInformation("Forward & Reverse", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    CalibratingItem.IsReverse,
                    Plot = new HtmlContainer(CalibratingItem.IsReverseScatterPlotControl.GetAllHtmlPlot2DLinesCharts())
                }), HtmlLogUniqueId.LoggingHtml());

                var mappingMinIndexes = mappingRegions.Select(t => t.VMiddleIndex).ToArray();
                var imageHorizontalProjectMinIndexes = CalibratingItem.MappingWindowItem.HorizontalProjectMinPixels.ToArray();

                var mappingList = new List<AODUniformityDTO.Mapping>();

                var linearSpline = LinearSpline.InterpolateSorted([.. imageHorizontalProjectMinIndexes], [.. mappingMinIndexes]);
                for (var i = 0; i < CalibratingItem.MappingWindowItem.ImageHorizontalProjects.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var mappingIndex = linearSpline.Interpolate(i);

                    var indexOf = imageHorizontalProjectMinIndexes.IndexOf(i);
                    var isNotLinearSpline = indexOf != -1;

                    mappingList.Add(new AODUniformityDTO.Mapping
                    {
                        IsNotLinearSpline = isNotLinearSpline,
                        ImageHorizontalProjectIndex = i,
                        LinearSplineMappingIndex = mappingIndex
                    });
                }

                var leftMappingMinIndexes = GenerateUtils.LinearIndexRange(0, mappingList[0].MappingIndex - 1);
                for (var i = 0; i < mappingList.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (i == mappingList.Count - 1)
                        mappingList[i].MappingIndices = [.. leftMappingMinIndexes, .. GenerateUtils.LinearIndexRange(mappingList[^1].MappingIndex, mappingWindow.Length - 1)];
                    else
                    {
                        var mappingIndexes = GenerateUtils.LinearIndexRange(mappingList[i].MappingIndex, mappingList[i + 1].MappingIndex);
                        mappingIndexes = mappingIndexes.Except((int[])[mappingList[i].MappingIndex, mappingList[i + 1].MappingIndex]).ToArray();

                        var chunks = mappingIndexes.ChunkSplitEvenly(2).ToArray();

                        mappingList[i].MappingIndices = [.. leftMappingMinIndexes, mappingList[i].MappingIndex, .. chunks[0]];

                        leftMappingMinIndexes = chunks.ElementAtOrDefault(1) ?? [];
                    }
                }

                CalibratingItem.Mappings = mappingList;

                Logger.LogHtmlInformation("Mapping", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Mappings = new HtmlExpand(string.Empty, new HtmlTable(CalibratingItem.Mappings)),
                    Plot = new HtmlContainer(CalibratingItem.MappingScatterPlotControl.GetAllHtmlPlot2DLinesCharts())
                }), HtmlLogUniqueId.LoggingHtml());

                foreach (var mapping in mappingList)
                {
                    Guard.IsGreaterThanOrEqualTo(mapping.MappingIndex, 0);
                    Guard.IsLessThanOrEqualTo(mapping.MappingIndex, mappingWindow.Length - 1);
                    if (mapping.IsNotLinearSpline) Guard.IsEqualTo(mappingMinIndexes[imageHorizontalProjectMinIndexes.IndexOf(mapping.ImageHorizontalProjectIndex)], mapping.MappingIndex);
                }

                CalibratingItem.ImageHorizontalProjectMappings = [.. imageHorizontalProjectMinIndexes.ChunkSplitEvenly(Cache.Item.ImageHorizontalProjectsSegmentCount)];
                CalibratingItem.PrescanAODWaveformProfileMappings =
                [
                    ..CalibratingItem.ImageHorizontalProjectMappings
                        .Select(t => t
                            .SelectMany(tt => CalibratingItem.Mappings
                                .Single(ttt => ttt.ImageHorizontalProjectIndex == tt).MappingIndices).ToArray())
                ];

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    ImageHorizontalProjectMappings = new HtmlExpand(string.Empty, new HtmlTable([.. CalibratingItem.ImageHorizontalProjectMappings.Index().Select(t => new { t.Index, t.Item })])),
                    PrescanAODWaveformProfileMappings = new HtmlExpand(string.Empty, new HtmlTable([.. CalibratingItem.PrescanAODWaveformProfileMappings.Index().Select(t => new { t.Index, t.Item })])),
                }), HtmlLogUniqueId.LoggingHtml());

                return true;

                double[] GetAndApplyWindow(int segmentIndex)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var prescanAODWaveformProfileTotalLength = prescanAODWaveformProfiles[0].Shorts.Count;
                    var prescanAODWaveformProfileSegmentWidth = prescanAODWaveformProfileTotalLength / Cache.Item.PrescanAODWaveformProfileSegmentCount;

                    var window = Generate.LinearVShapeWindow(
                        Cache.LaserLightInformation.Coefficient,
                        Cache.LaserLightInformation.Coefficient / 1000d,
                        segmentIndex * prescanAODWaveformProfileSegmentWidth,
                        prescanAODWaveformProfileSegmentWidth,
                        prescanAODWaveformProfileTotalLength).Window;

                    return window;
                }

                (double[] Window, (int VStartIndex, int VMiddleIndex, int VStopIndex)[] Regions) GetWindow()
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var prescanAODWaveformProfileTotalLength = prescanAODWaveformProfiles[0].Shorts.Count;
                    var prescanAODWaveformProfileSegmentWidth = prescanAODWaveformProfileTotalLength / Cache.Item.PrescanAODWaveformProfileSegmentCount;

                    var (window, regions) = Generate.LinearVShapeWindow(
                        Cache.LaserLightInformation.Coefficient,
                        Cache.LaserLightInformation.Coefficient / 1000d,
                        [.. prescanAODWaveformProfileSegmentIndexes.Select(t => t * prescanAODWaveformProfileSegmentWidth)],
                        prescanAODWaveformProfileSegmentWidth,
                        prescanAODWaveformProfileTotalLength);

                    return (window, regions);
                }
            }
            finally
            {
                CIBViewModel.ToggleEnableAGC(ApplicationCookie.CIBInformations, true);
                CIBViewModel.ToggleProfileMode(ApplicationCookie.CIBInformations, CIBProfileModeEnum.PMTLog);
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

            var prescanAODWaveformProfiles = ConfigureViewModel.GetPrescanAODWaveProfiles(Cache.ProductivityInformation);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.LaserLightInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.CIBInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.HazeFindBFMachinePosition,
                Cache.Item.ImageWidth,
                Cache.Item.PrescanAODWaveformProfileSegmentCount,
                Cache.Item.ImageHorizontalProjectsSegmentCount,
                PrescanAODWaveformProfiles = new HtmlTable([.. prescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.Item = new AODUniformityDTOItem
            {
                OpticsPolarizationModeEnum = OpticsPolarizationModeEnum.P,
                CIBInformation = Cache.Item.CIBInformation
            };

            OpticsViewModel.SetPolarizationMode(OpticsPolarizationModeEnum.P);

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0);
            StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);

            try
            {
                Logger.LogHtmlInformation("Images", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                foreach (var coefficient in Enumerable.Range(0, 11).Select(t => 0.5 * Cache.LaserLightInformation.Coefficient + t * 0.05 * Cache.LaserLightInformation.Coefficient))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var itemItemData = new AODUniformityDTOItem.Item
                    {
                        Window = Generate.Repeat(prescanAODWaveformProfiles[0].Shorts.Count, coefficient)
                    };

                    await CatchImageAsync(
                        $"{coefficient:0.###}",
                        hazeBFPosition,
                        detectImageDirectory,
                        itemItemData,
                        cancellationToken);

                    CalibratingItem.Item.InitializeWindowItems = [.. CalibratingItem.Item.InitializeWindowItems, itemItemData];
                }

                var window = Generate.Repeat(prescanAODWaveformProfiles[0].Shorts.Count, Cache.LaserLightInformation.Coefficient);

                foreach (var (index, imageHorizontalProjectIndexes) in CalibratingItem.ImageHorizontalProjectMappings.Index())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var maximumIndex = Vector<double>.Build.Dense([.. CalibratingItem.Item.InitializeWindowItems.Select(t => imageHorizontalProjectIndexes.Select(tt => t.ImageHorizontalProjects[tt]).Average())]).MaximumIndex();
                    Guard.IsGreaterThan(maximumIndex, 0);

                    Vector<double>.Build.Dense(window).SetSubVectorIndexes(
                        CalibratingItem.PrescanAODWaveformProfileMappings[index],
                        CalibratingItem.Item.InitializeWindowItems[maximumIndex].Window[0]);
                }

                Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                {
                    Plot = new HtmlContainer(CalibratingItem.InitializeWindowScatterPlotControl.GetAllHtmlPlot2DLinesCharts())
                }), HtmlLogUniqueId.LoggingHtml());

                return true;
            }
            finally
            {
                CIBViewModel.ToggleEnableAGC(ApplicationCookie.CIBInformations, true);
                CIBViewModel.ToggleProfileMode(ApplicationCookie.CIBInformations, CIBProfileModeEnum.PMTLog);
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);
            }
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step6Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;

            var cibInformations = ApplicationCookie.CIBInformations;
            var windowLimitMin = Math.Max(0, Cache.LaserLightInformation.Coefficient - Cache.LaserLightInformation.Coefficient * Cache.Item.WindowLimitRate);
            var windowLimitMax = Math.Min(1d, Cache.LaserLightInformation.Coefficient + Cache.LaserLightInformation.Coefficient * Cache.Item.WindowLimitRate);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.LaserLightInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.CIBInformation,
                CIBConfiguration = new HtmlQuote(Cache.Item.CIBConfiguration.ToHtmlAnonymous()),
                Cache.Item.HazeFindBFMachinePosition,
                Cache.Item.ImageWidth,
                Cache.Item.PrescanAODWaveformProfileSegmentCount,
                Cache.Item.ImageHorizontalProjectsSegmentCount,
                Cache.Item.ImageHorizontalProjectsSkipCout,
                Cache.Item.ImageHorizontalProjectsSkipLastCout,
                Cache.Item.WindowLimitRate,
                Cache.Item.WindowInterval,
                CIBInformations = new HtmlExpand(string.Empty, new HtmlTable([.. cibInformations.Select(t => t.ToHtmlAnonymous())])),
                windowLimitMin,
                windowLimitMax,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.Item.WindowLimitMin = windowLimitMin;
            CalibratingItem.Item.WindowLimitMax = windowLimitMax;
            CalibratingItem.TargetPMTValues = [];
            CalibratingItem.Items =
            [
                .. cibInformations
                    .Where(t => t != CalibratingItem.Item.CIBInformation)
                    .Select(t => new AODUniformityDTOItem
                    {
                        OpticsPolarizationModeEnum = OpticsPolarizationModeEnum.P,
                        CIBInformation = t,
                        WindowLimitMin = windowLimitMin,
                        WindowLimitMax = windowLimitMax,
                    })
            ];

            var itemItems = CalibratingItem.Items.Concat([CalibratingItem.Item]).ToArray();

            OpticsViewModel.SetPolarizationMode(OpticsPolarizationModeEnum.P);

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0);
            StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);

            try
            {
                Logger.LogHtmlInformation("Uniformity", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                var windowIntervals = Generate.Repeat(CalibratingItem.ImageHorizontalProjectMappings.Count, Cache.Item.WindowInterval);
                var mappingStatuses = Generate.Repeat(CalibratingItem.ImageHorizontalProjectMappings.Count, Status.None);

                var times = 0;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    var prescanAODWaveformProfiles = ConfigureViewModel.GetPrescanAODWaveProfiles(Cache.ProductivityInformation);
                    foreach (var prescanAODWaveformProfile in prescanAODWaveformProfiles) prescanAODWaveformProfile.ApplyCoefficientWindowList(CalibratingItem.Item.Window);
                    LaserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, prescanAODWaveformProfiles);

                    var cibPMTImages = await CIBViewModel.GetPMTImagesAsync(
                        Cache.ProductivityInformation,
                        StageCoordinateSystemEnum.Dark,
                        hazeBFPosition,
                        cibInformations,
                        Cache.Item.ImageWidth,
                        (true, null),
                        (false, Cache.Item.CIBConfiguration),
                        (true, null),
                        false,
                        cancellationToken);

                    Logger.LogHtmlInformation("Images", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                    {
                        PrescanAODWaveformProfiles = new HtmlTable([.. prescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
                    }), HtmlLogUniqueId.LoggingHtml());

                    foreach (var (index, darkFieldImage) in cibPMTImages.Index())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        using var _ = darkFieldImage;
                        var itemItem = itemItems.Single(t => t.CIBInformation == cibInformations[index]);

                        var imageFilePath = Path.Combine(detectImageDirectory, itemItem.CIBInformation.ToString(), $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                        darkFieldImage.Image.Save(imageFilePath);

                        var itemItemData = new AODUniformityDTOItem.Item
                        {
                            Window = CalibratingItem.Item.Window,
                            PrescanAODWaveformProfiles = prescanAODWaveformProfiles,
                            ImageHorizontalProjects = darkFieldImage.Image.GetHorizontalProjects(),
                            RawImageFilePath = darkFieldImage.RawImageFilePath,
                            ImageFilePath = imageFilePath
                        };

                        if (CalibratingItem.IsReverse) itemItemData.ImageHorizontalProjects = [.. itemItemData.ImageHorizontalProjects.Reverse()];

                        itemItem.Items = [.. itemItem.Items, itemItemData];

                        Logger.LogHtmlInformation(itemItem.CIBInformation.ToString(), HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                        {
                            itemItemData.ImageFilePath,
                            itemItemData.RawImageFilePath
                        }), HtmlLogUniqueId.LoggingHtml());
                    }

                    foreach (var itemItem in itemItems)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var imageHorizontalProjectsVector = Vector<double>.Build.Dense([.. itemItem.Items[times].ImageHorizontalProjects.Skip(Cache.Item.ImageHorizontalProjectsSkipCout).SkipLast(Cache.Item.ImageHorizontalProjectsSkipLastCout)]);
                        var targetPMTValue = CalibratingItem.TargetPMTValues.GetOrAdd(itemItem.CIBInformation, imageHorizontalProjectsVector.Average());

                        itemItem.Items[times].MaxRate = imageHorizontalProjectsVector.AbsoluteMaximum() / targetPMTValue;
                        itemItem.Items[times].MinRate = imageHorizontalProjectsVector.AbsoluteMinimum() / targetPMTValue;

                        itemItem.Items[times].IsOk = Cache.CalibrateThresholdMin <= itemItem.Items[times].MinRate
                                                     && itemItem.Items[times].MaxRate <= Cache.CalibrateThresholdMax;
                    }

                    var notSkipImageHorizontalProjectIndexes = GenerateUtils.LinearIndexRange(0, CalibratingItem.Item.Items[times].ImageHorizontalProjects.Count - 1)
                        .Skip(Cache.Item.ImageHorizontalProjectsSkipCout)
                        .SkipLast(Cache.Item.ImageHorizontalProjectsSkipLastCout)
                        .ToArray();

                    var window = CalibratingItem.Item.Window.ToArray();
                    foreach (var (index, imageHorizontalProjectIndexes) in CalibratingItem.ImageHorizontalProjectMappings
                                 .Index()
                                 .Where(t => t.Item.All(tt => notSkipImageHorizontalProjectIndexes.Contains(tt))))
                    {
                        var vector = Vector<double>.Build.Dense(window);
                        var value = imageHorizontalProjectIndexes.Select(t => CalibratingItem.Item.Items[times].ImageHorizontalProjects[t]).Average();

                        var targetPMTValue = CalibratingItem.TargetPMTValues.Get(CalibratingItem.Item.CIBInformation);
                        if (value > targetPMTValue * (Cache.CalibrateThresholdMax - Cache.CalibrateThreshold * 0.5))
                        {
                            if (mappingStatuses[index] == Status.LessThan) windowIntervals[index] *= 0.5d;

                            vector.SetSubVectorIndexes(
                                CalibratingItem.PrescanAODWaveformProfileMappings[index],
                                vector.SubVectorIndexes(CalibratingItem.PrescanAODWaveformProfileMappings[index]) - windowIntervals[index]);

                            if (vector.Any(t => t <= CalibratingItem.Item.WindowLimitMin))
                            {
                                vector.SetSubVectorIndexes(CalibratingItem.PrescanAODWaveformProfileMappings[index], CalibratingItem.Item.WindowLimitMin);

                                mappingStatuses[index] = Status.Ok;
                            }
                            else
                            {
                                mappingStatuses[index] = Status.GreaterThan;
                            }
                        }
                        else if (value < targetPMTValue * (Cache.CalibrateThresholdMin + Cache.CalibrateThreshold * 0.5))
                        {
                            if (mappingStatuses[index] == Status.GreaterThan) windowIntervals[index] *= 0.5d;

                            vector.SetSubVectorIndexes(
                                CalibratingItem.PrescanAODWaveformProfileMappings[index],
                                vector.SubVectorIndexes(CalibratingItem.PrescanAODWaveformProfileMappings[index]) + windowIntervals[index]);

                            if (vector.Any(t => t >= CalibratingItem.Item.WindowLimitMax))
                            {
                                vector.SetSubVectorIndexes(CalibratingItem.PrescanAODWaveformProfileMappings[index], CalibratingItem.Item.WindowLimitMax);

                                mappingStatuses[index] = Status.Ok;
                            }
                            else
                            {
                                mappingStatuses[index] = Status.LessThan;
                            }
                        }
                        else
                        {
                            mappingStatuses[index] = Status.Ok;
                        }
                    }

                    CalibratingItem.Item.Window = window;

                    CalibratingItem.IsCalibrated = mappingStatuses.All(t => t is Status.Ok or Status.None);

                    var htmlBullet = new HtmlBullet(new
                    {
                        times,
                        windowIntervals = new HtmlExpand(string.Empty, new HtmlTable([.. windowIntervals.Index().Select(t => new { t.Index, t.Item })])),
                        mappingStatuses = new HtmlExpand(string.Empty, new HtmlTable([.. mappingStatuses.Index().Select(t => new { t.Index, t.Item })])),
                        CalibratingItem.ProductivityInformation,
                        Plot = new HtmlContainer(CalibratingItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()),
                        Plots = new HtmlContainer([.. CalibratingItem.ScatterPlotControls.Select(t => new HtmlExpand(t.Key.ToString(), new HtmlContainer(t.Value.GetAllHtmlPlot2DLinesCharts())))])
                    });

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
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);
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

    private async Task CatchImageAsync(
        string title,
        Point hazeBFPosition,
        string detectImageDirectory,
        AODUniformityDTO.WindowItem windowItem,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var prescanAODWaveformProfiles = ConfigureViewModel.GetPrescanAODWaveProfiles(Cache.ProductivityInformation);

        foreach (var prescanAODWaveformProfile in prescanAODWaveformProfiles) prescanAODWaveformProfile.ApplyCoefficientWindowList(windowItem.Window);
        LaserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, prescanAODWaveformProfiles);

        windowItem.PrescanAODWaveformProfiles = prescanAODWaveformProfiles;

        using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
            Cache.ProductivityInformation,
            StageCoordinateSystemEnum.Dark,
            hazeBFPosition,
            Cache.Item.CIBInformation,
            Cache.Item.ImageWidth,
            (false, CalChipSiteModelEnum.HazeModel),
            (false, Cache.Item.CIBConfiguration),
            (true, null),
            false,
            cancellationToken);

        var imageFilePath = Path.Combine(detectImageDirectory, Cache.Item.CIBInformation.ToString(), title, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
        darkFieldImage.Image.Save(imageFilePath);

        windowItem.ImageHorizontalProjects = darkFieldImage.Image.GetHorizontalProjects();
        if (CalibratingItem.IsReverse) windowItem.ImageHorizontalProjects = [.. windowItem.ImageHorizontalProjects.Reverse()];

        windowItem.ImageFilePath = imageFilePath;
        windowItem.RawImageFilePath = darkFieldImage.RawImageFilePath;

        Logger.LogHtmlInformation(title, HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
        {
            PrescanAODWaveformProfiles = new HtmlTable([.. windowItem.PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())]),
            windowItem.RawImageFilePath,
            Image = new HtmlImage(windowItem.ImageFilePath)
        }), HtmlLogUniqueId.LoggingHtml());
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

    private enum Status
    {
        None,
        GreaterThan,
        LessThan,
        Ok
    }
}