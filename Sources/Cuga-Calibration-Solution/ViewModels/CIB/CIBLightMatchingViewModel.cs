using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.CIB.LightMatching;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
using Humanizer;
using Microsoft.Extensions.Hosting;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace CugaCalibration.ViewModels.CIB;

[IOCAppService(ServiceType = typeof(CIBLightMatchingViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBLightMatchingViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override IReadOnlyList<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Image Param" },
        new() { StepName = "Find Haze Position" },
        new() { StepName = "Find Silica Spheres Position" },
        new() { StepName = "Light Matching" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private IReadOnlyList<CIBLightMatchingDTO> _calibratings = [];

    [ObservableProperty]
    private IReadOnlyList<CIBLightMatchingDTO> _selectedCalibratingItems = [];

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationStatus> _calibratingStatuses = [];

    #endregion Calibrate

    [ObservableProperty]
    private IReadOnlyList<CIBLightMatchingDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<CIBLightMatchingDTO> _selectedReviewItems = [];

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private CIBLightMatchingCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private CIBLightMatchingDTO[] _calibrations = [];

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>();

        if (CalibratingStatuses.Count == 0)
            CalibratingStatuses = [.. ApplicationCookie.ProductivityInformations.Select(t => new ProductivityInformationStatus { SelectedItem = t })];

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<CIBLightMatchingCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<CIBLightMatchingDTO>();

        Calibrations =
        [
            .. Calibrations
                .Where(t => ApplicationCookie.ProductivityInformations.Contains(t.ProductivityInformation)
                            && ApplicationCookie.OpticsApodizationModeEnums.Contains(t.OpticsApodizationModeEnum)
                            && ApplicationCookie.OpticsPolarizationModeEnums.Contains(t.OpticsPolarizationModeEnum)
                            && ApplicationCookie.OpticsCollectorPolarizationModeEnums.Contains(t.OpticsCollectorPolarizationModeEnum))
                .Select(t =>
                {
                    t.Items = [..t.Items.Where(tt => ApplicationCookie.CIBInformations.Contains(tt.CIBInformation))];

                    CalibratingStatuses
                        .Single(tt => tt.SelectedItem == t.ProductivityInformation)
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
                .ThenBy(t => t.OpticsApodizationModeEnum)
                .ThenBy(t => t.OpticsPolarizationModeEnum)
                .ThenBy(t => t.OpticsCollectorPolarizationModeEnum)
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
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition));

                return true;

            case 4:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.SilicaSphereFindBFMachinePosition));

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
                Calibratings = [];

                return true;

            case 1:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition != Point.Origin
                    ? Cache.Item.HazeFindBFMachinePosition
                    : Guard.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition));

                return true;

            case 2:
                MicroscopeViewModel.SwitchMicroscopeLensInformation(Cache.Item.MicroscopeLensInformation);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.SilicaSphereFindBFMachinePosition != Point.Origin
                    ? Cache.Item.SilicaSphereFindBFMachinePosition
                    : Guard.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition));

                return true;

            case 3:
                return true;

            case 4:
                CalibratingStatuses
                    .Single(t => t.SelectedItem == Cache.ProductivityInformation)
                    .IsCalibrated = true;

                DialogWindowProvider.ShowDialog($"{Name} {CalibrateDirectoryName} Ok!");

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

            return ApplicationCookie.ProductivityInformations.Contains(Cache.ProductivityInformation);
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
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.Item.MicroscopeLensInformation)
                   && ApplicationCookie.LaserLightInformations.Contains(Cache.Item.LaserLightInformation);
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
                Cache.Item.HazeFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step3Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Guard.IsEqualTo(Cache.Item.MicroscopeLensInformation, MicroscopeViewModel.GetCurrentMicroscopeLensInformation());

            StageViewModel.SetAbsoluteStageTheta(0d);
            Cache.Item.SilicaSphereFindBFMachinePosition = StageViewModel.GetMachineStagePosition();

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.HazeFindBFMachinePosition,
                Cache.Item.SilicaSphereFindBFMachinePosition
            }), HtmlLogUniqueId.LoggingHtml());

            return true;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step4Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;

            var currentOpticsApodizationModeEnum = OpticsViewModel.GetApodizationMode();
            var currentOpticsPolarizationModeEnum = OpticsViewModel.GetPolarizationMode();
            var currentCollectorPolarizationModeEnum = OpticsViewModel.GetCollectorPolarizationMode();
            var cibInformations = ApplicationCookie.CIBInformations;

            try
            {
                Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                {
                    Cache.ProductivityInformation,
                    Cache.Item.MicroscopeLensInformation,
                    Cache.Item.LaserLightInformation,
                    Cache.Item.HazeFindBFMachinePosition,
                    Cache.Item.SilicaSphereFindBFMachinePosition,
                    Cache.Item.ImageWidth,
                    Cache.HazeCalibratingRetryTimes,
                    Cache.SilicaSphereCalibratingRetryTimes,
                    Cache.HazeThreshold,
                    Cache.SilicaSphereThreshold,
                    Cache.CalibratingHazeThreshold,
                    Cache.CalibratingSilicaSphereThreshold,
                    currentOpticsApodizationModeEnum,
                    currentOpticsPolarizationModeEnum,
                    currentCollectorPolarizationModeEnum,
                    CIBInformations = new HtmlExpand(string.Empty, new HtmlTable([.. cibInformations.Select(t => t.ToHtmlAnonymous())])),
                    detectImageDirectory
                }), HtmlLogUniqueId.LoggingHtml());

                Calibratings = [];

                CIBViewModel.SetAGC(cibInformations, true);
                CIBViewModel.SetCIBProfileModeEnum(cibInformations, CIBProfileModeEnum.PMTLog);
                CIBViewModel.SetLightMatching(cibInformations, 0);

                await HazeAsync();

                await SilicaSphereAsync();

                Guard.IsTrue(Save(Calibratings, cancellationToken));

                return Calibratings.All(t => t.IsCalibrated);

                async Task HazeAsync()
                {
                    var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition);
                    StageViewModel.SetAbsoluteStageTheta(0d);
                    StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);

                    Logger.LogHtmlInformation("Haze", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    foreach (var opticsApodizationModeEnum in ApplicationCookie.OpticsApodizationModeEnums)
                    {
                        foreach (var opticsPolarizationModeEnum in ApplicationCookie.OpticsPolarizationModeEnums)
                        {
                            foreach (var opticsCollectorPolarizationModeEnum in ApplicationCookie.OpticsCollectorPolarizationModeEnums)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                OpticsViewModel.SetApodizationMode(opticsApodizationModeEnum);
                                OpticsViewModel.SetPolarizationMode(opticsPolarizationModeEnum);
                                OpticsViewModel.SetCollectorPolarizationMode(opticsCollectorPolarizationModeEnum);
                                CIBViewModel.SetLightMatching(cibInformations, 0);

                                var item = new CIBLightMatchingDTO(ApplicationCookie.CIBInformationChannelIds)
                                {
                                    ProductivityInformation = Cache.ProductivityInformation,
                                    OpticsApodizationModeEnum = opticsApodizationModeEnum,
                                    OpticsPolarizationModeEnum = opticsPolarizationModeEnum,
                                    OpticsCollectorPolarizationModeEnum = opticsCollectorPolarizationModeEnum,
                                    Items = [.. cibInformations.Select(t => new CIBLightMatchingDTOItem { CIBInformation = t })]
                                };

                                Calibratings = [.. Calibratings, item];
                                SelectedCalibratingItems = [item];

                                Logger.LogHtmlInformation($"{item.OpticsApodizationModeEnum.Humanize()}, {item.OpticsPolarizationModeEnum.Humanize()}, {item.OpticsCollectorPolarizationModeEnum.Humanize()}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                                var times = 0;
                                while (true)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    Logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                                    var cibPMTImages = await CIBViewModel.GetPMTImagesAsync(
                                        Cache.ProductivityInformation,
                                        StageCoordinateSystemEnum.Dark,
                                        hazeBFPosition,
                                        Cache.Item.ImageWidth,
                                        cibInformations,
                                        (true, null),
                                        (true, null),
                                        (true, null),
                                        (false, Cache.Item.LaserLightInformation),
                                        false,
                                        cancellationToken,
                                        isKeepRawImageCIBProfileModeEnum: true);

                                    Logger.LogHtmlInformation("Images", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

                                    foreach (var (index, darkFieldImage) in cibPMTImages.Index())
                                    {
                                        cancellationToken.ThrowIfCancellationRequested();

                                        using var _ = darkFieldImage;
                                        var itemItem = item.Items[index];

                                        var imageFilePath = Path.Combine(detectImageDirectory, itemItem.CIBInformation.ToString(), $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                                        darkFieldImage.Image.SaveImage(imageFilePath);

                                        using var hImage = darkFieldImage.Image.ToHImage();
                                        var itemItemData = new CIBLightMatchingDTOItem.Item
                                        {
                                            PMTValue = HostEnvironment.IsProduction() ? hImage.GetIntensity().Average : Random.Shared.RandomDouble(1000, 2000),
                                            RawImageFilePath = darkFieldImage.RawImageFilePath,
                                            ImageFilePath = imageFilePath
                                        };
                                        itemItem.HazeItems = [.. itemItem.HazeItems, itemItemData];

                                        Logger.LogHtmlInformation(itemItem.CIBInformation.ToString(), HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                                        {
                                            itemItemData.PMTValue,
                                            itemItemData.ImageFilePath,
                                            itemItemData.RawImageFilePath
                                        }), HtmlLogUniqueId.LoggingHtml());
                                    }

                                    var results = (
                                        from itemItem in item.Items
                                        group itemItem by itemItem.CIBInformation.ChannelId
                                        into g
                                        orderby g.Key
                                        select (
                                            ChannelId: g.Key,
                                            ItemItems: g.OrderBy(t => t.CIBInformation.PMTId).ToArray()
                                        )).ToArray();

                                    var resultList = new List<bool>();
                                    foreach (var (channelId, itemItems) in results)
                                    {
                                        cancellationToken.ThrowIfCancellationRequested();

                                        var pmtValue = itemItems.Average(t => t.HazeItems[times].PMTValue);
                                        var channelIdTargetPMTValue = item.HazeTargetPMTValues.GetOrAdd(channelId, _ => pmtValue);

                                        foreach (var itemItem in itemItems)
                                        {
                                            cancellationToken.ThrowIfCancellationRequested();

                                            itemItem.HazeItems[times].Error = itemItem.HazeItems[times].PMTValue - channelIdTargetPMTValue;
                                            if (itemItem.HazeItems.Any(t => t.IsOk))
                                            {
                                                itemItem.HazeItems[times].IsOk = true;
                                                resultList.Add(itemItem.HazeItems[times].IsOk);

                                                continue;
                                            }

                                            itemItem.HazeItems[times].IsOk = Math.Abs(itemItem.HazeItems[times].Error) <= Cache.CalibratingHazeThreshold;
                                            resultList.Add(itemItem.HazeItems[times].IsOk);

                                            if (itemItem.HazeItems[times].IsOk) continue;

                                            itemItem.HazeItems[times].Result = -itemItem.HazeItems[times].Error;
                                            itemItem.DigitalGain += itemItem.HazeItems[times].Result;

                                            CIBViewModel.SetLightMatching([itemItem.CIBInformation], itemItem.DigitalGain);
                                        }
                                    }

                                    var htmlBullet = new HtmlBullet(new
                                    {
                                        times,
                                        item.ProductivityInformation,
                                        item.OpticsApodizationModeEnum,
                                        item.OpticsPolarizationModeEnum,
                                        item.OpticsCollectorPolarizationModeEnum,
                                        Plot = new HtmlContainer([.. item.ScatterPlotControls.Select(t => new HtmlExpand(t.Key.ToString(), new HtmlContainer(t.Value.GetAllHtmlPlot2DLinesCharts())))])
                                    });

                                    item.IsCalibrated = resultList.All(t => t);

                                    if (item.IsCalibrated)
                                    {
                                        LogDetails(true);
                                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                                        break;
                                    }

                                    if (++times > Cache.HazeCalibratingRetryTimes - 1)
                                    {
                                        LogDetails(false);
                                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                                        break;
                                    }

                                    Logger.LogHtmlInformation("Plots", HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                                }

                                continue;

                                void LogDetails(bool isSuccess)
                                {
                                    Logger.LogHtmlInformation("Details", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                                    foreach (var itemItem in item.Items)
                                    {
                                        var itemItemData = itemItem.HazeItems[^1];
                                        var htmlBullet = new HtmlBullet(new
                                        {
                                            itemItemData.PMTValue,
                                            itemItemData.RawImageFilePath,
                                            Image = new HtmlImage(itemItemData.ImageFilePath)
                                        });

                                        if (isSuccess) Logger.LogHtmlInformation(itemItem.CIBInformation.ToString(), HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                                        else Logger.LogHtmlError(itemItem.CIBInformation.ToString(), HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                                    }
                                }
                            }
                        }
                    }
                }

                async Task SilicaSphereAsync()
                {
                    var silicaSphereBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.SilicaSphereFindBFMachinePosition);
                    StageViewModel.SetAbsoluteStageTheta(0d);
                    StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(silicaSphereBFPosition);

                    Logger.LogHtmlInformation("Silica Sphere", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    foreach (var opticsApodizationModeEnum in ApplicationCookie.OpticsApodizationModeEnums)
                    {
                        foreach (var opticsPolarizationModeEnum in ApplicationCookie.OpticsPolarizationModeEnums)
                        {
                            foreach (var collectorPolarizationModeEnum in ApplicationCookie.OpticsCollectorPolarizationModeEnums)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                OpticsViewModel.SetApodizationMode(opticsApodizationModeEnum);
                                OpticsViewModel.SetPolarizationMode(opticsPolarizationModeEnum);
                                OpticsViewModel.SetCollectorPolarizationMode(collectorPolarizationModeEnum);
                                CIBViewModel.SetLightMatching(cibInformations, 0);

                                var item = Calibratings.Single(t => t.ProductivityInformation == Cache.ProductivityInformation
                                                                    && t.OpticsApodizationModeEnum == opticsApodizationModeEnum
                                                                    && t.OpticsPolarizationModeEnum == opticsPolarizationModeEnum
                                                                    && t.OpticsCollectorPolarizationModeEnum == collectorPolarizationModeEnum);
                                if (item.IsCalibrated == false) break;
                                item.IsCalibrated = false;

                                SelectedCalibratingItems = [item];

                                Logger.LogHtmlInformation($"{item.OpticsApodizationModeEnum.Humanize()}, {item.OpticsPolarizationModeEnum.Humanize()}, {item.OpticsCollectorPolarizationModeEnum.Humanize()}", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                                var times = 0;
                                while (true)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    Logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                                    var cibPMTImages = await CIBViewModel.GetPMTImagesAsync(
                                        Cache.ProductivityInformation,
                                        StageCoordinateSystemEnum.Dark,
                                        silicaSphereBFPosition,
                                        Cache.Item.ImageWidth,
                                        cibInformations,
                                        (true, null),
                                        (true, null),
                                        (true, null),
                                        (false, Cache.Item.LaserLightInformation),
                                        false,
                                        cancellationToken,
                                        isKeepRawImageCIBProfileModeEnum: true);

                                    Logger.LogHtmlInformation("Images", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

                                    foreach (var (index, darkFieldImage) in cibPMTImages.Index())
                                    {
                                        cancellationToken.ThrowIfCancellationRequested();

                                        using var _ = darkFieldImage;
                                        var itemItem = item.Items[index];

                                        var imageFilePath = Path.Combine(detectImageDirectory, itemItem.CIBInformation.ToString(), $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                                        darkFieldImage.Image.SaveImage(imageFilePath);

                                        // var histogram = darkFieldImage.Image.GetHistogram(0, 0b0000_1111_1111_1111);

                                        using var hImage = darkFieldImage.Image.ToHImage();
                                        var itemItemData = new CIBLightMatchingDTOItem.Item
                                        {
                                            PMTValue = HostEnvironment.IsProduction() ? hImage.GetIntensity().Average : Random.Shared.RandomDouble(1000, 2000) /*HostEnvironment.IsProduction() ? histogram.Maxima(t => t.Y).First().X : Random.Shared.RandomDouble(1000, 2000)*/,
                                            RawImageFilePath = darkFieldImage.RawImageFilePath,
                                            ImageFilePath = imageFilePath /*,
                                            Histogram = histogram*/
                                        };

                                        itemItem.SilicaSphereItems = [.. itemItem.SilicaSphereItems, itemItemData];

                                        Logger.LogHtmlInformation(itemItem.CIBInformation.ToString(), HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                                        {
                                            itemItemData.PMTValue,
                                            itemItemData.ImageFilePath,
                                            itemItemData.RawImageFilePath /*,
                                            Histogram = new HtmlPlot2DLinesChart([(string.Empty, itemItemData.Histogram)], string.Empty)*/
                                        }), HtmlLogUniqueId.LoggingHtml());
                                    }

                                    var results = (
                                        from itemItem in item.Items
                                        group itemItem by itemItem.CIBInformation.ChannelId
                                        into g
                                        orderby g.Key
                                        select (
                                            ChannelId: g.Key,
                                            ItemItems: g.OrderBy(t => t.CIBInformation.PMTId).ToArray()
                                        )).ToArray();

                                    var resultList = new List<bool>();

                                    item.SilicaSphereTargetPMTValue ??= results
                                        .SelectMany(t => t.ItemItems)
                                        .Select(t => t.SilicaSphereItems[times].PMTValue)
                                        .Average();

                                    item.SilicaSphereAveragePMTValues = [];

                                    foreach (var (channelId, itemItems) in results)
                                    {
                                        cancellationToken.ThrowIfCancellationRequested();

                                        var channelIdAveragePMTValue = itemItems.Average(t => t.SilicaSphereItems[times].PMTValue);
                                        item.SilicaSphereAveragePMTValues = new ConcurrentDictionary<int, double>(item.SilicaSphereAveragePMTValues) { [channelId] = channelIdAveragePMTValue };

                                        var error = channelIdAveragePMTValue - item.SilicaSphereTargetPMTValue.Value;

                                        foreach (var itemItem in itemItems)
                                        {
                                            cancellationToken.ThrowIfCancellationRequested();

                                            itemItem.SilicaSphereItems[times].Error = error;

                                            if (itemItem.SilicaSphereItems.Any(t => t.IsOk))
                                            {
                                                itemItem.SilicaSphereItems[times].IsOk = true;
                                                resultList.Add(itemItem.SilicaSphereItems[times].IsOk);

                                                continue;
                                            }

                                            itemItem.SilicaSphereItems[times].IsOk = Math.Abs(itemItem.SilicaSphereItems[times].Error) <= Cache.CalibratingSilicaSphereThreshold;
                                            resultList.Add(itemItem.SilicaSphereItems[times].IsOk);

                                            if (itemItem.SilicaSphereItems[times].IsOk) continue;

                                            itemItem.SilicaSphereItems[times].Result = -itemItem.SilicaSphereItems[times].Error;
                                            itemItem.MultiplicativeFactors += itemItem.SilicaSphereItems[times].Result;

                                            CIBViewModel.SetLightMatching([itemItem.CIBInformation], itemItem.DigitalGainPlusMultiplicativeFactors);
                                        }
                                    }

                                    var htmlBullet = new HtmlBullet(new
                                    {
                                        times,
                                        item.ProductivityInformation,
                                        item.OpticsApodizationModeEnum,
                                        item.OpticsPolarizationModeEnum,
                                        item.OpticsCollectorPolarizationModeEnum,
                                        Plot = new HtmlContainer([.. item.ScatterPlotControls.Select(t => new HtmlExpand(t.Key.ToString(), new HtmlContainer(t.Value.GetAllHtmlPlot2DLinesCharts())))])
                                    });

                                    item.IsCalibrated = resultList.All(t => t);

                                    if (item.IsCalibrated)
                                    {
                                        LogDetails(true);
                                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                                        break;
                                    }

                                    if (++times > Cache.SilicaSphereCalibratingRetryTimes - 1)
                                    {
                                        LogDetails(false);
                                        Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header4, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                                        break;
                                    }

                                    Logger.LogHtmlInformation("Plots", HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                                }

                                continue;

                                void LogDetails(bool isSuccess)
                                {
                                    if (isSuccess) Logger.LogHtmlInformation("Details", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                                    else Logger.LogHtmlError("Details", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());
                                    foreach (var itemItem in item.Items)
                                    {
                                        var itemItemData = itemItem.SilicaSphereItems[^1];
                                        var htmlBullet = new HtmlBullet(new
                                        {
                                            itemItemData.PMTValue,
                                            itemItemData.RawImageFilePath,
                                            Image = new HtmlImage(itemItemData.ImageFilePath),
                                            Histogram = new HtmlPlot2DLinesChart([(string.Empty, itemItemData.Histogram)], string.Empty)
                                        });

                                        if (isSuccess) Logger.LogHtmlInformation(itemItem.CIBInformation.ToString(), HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                                        else Logger.LogHtmlError(itemItem.CIBInformation.ToString(), HtmlHeaderLevelEnum.Header5, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                OpticsViewModel.SetApodizationMode(currentOpticsApodizationModeEnum);
                OpticsViewModel.SetPolarizationMode(currentOpticsPolarizationModeEnum);
                OpticsViewModel.SetCollectorPolarizationMode(currentCollectorPolarizationModeEnum);
                CIBViewModel.SetAGC(cibInformations, true);
                CIBViewModel.SetCIBProfileModeEnum(cibInformations, CIBProfileModeEnum.PMTLog);
                CIBViewModel.SetLightMatching(cibInformations, 0);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.SilicaSphereFindBFMachinePosition));
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

            foreach (var selectedReviewItem in SelectedReviewItems
                         .OrderBy(t => t.ProductivityInformation)
                         .ThenBy(t => t.OpticsApodizationModeEnum)
                         .ThenBy(t => t.OpticsPolarizationModeEnum)
                         .ThenBy(t => t.OpticsCollectorPolarizationModeEnum))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var title = $"{selectedReviewItem.ProductivityInformation}, {selectedReviewItem.OpticsApodizationModeEnum.Humanize()}, {selectedReviewItem.OpticsPolarizationModeEnum.Humanize()}, {selectedReviewItem.OpticsCollectorPolarizationModeEnum.Humanize()}";

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
                    selectedReviewItem.OpticsApodizationModeEnum,
                    selectedReviewItem.OpticsPolarizationModeEnum,
                    selectedReviewItem.OpticsCollectorPolarizationModeEnum,
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

    private bool Save(IReadOnlyList<CIBLightMatchingDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                dto,
                .. Calibrations.Where(t => t.ProductivityInformation != dto.ProductivityInformation
                                           || t.OpticsApodizationModeEnum != dto.OpticsApodizationModeEnum
                                           || t.OpticsPolarizationModeEnum != dto.OpticsPolarizationModeEnum
                                           || t.OpticsCollectorPolarizationModeEnum != dto.OpticsCollectorPolarizationModeEnum)
            ];
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}