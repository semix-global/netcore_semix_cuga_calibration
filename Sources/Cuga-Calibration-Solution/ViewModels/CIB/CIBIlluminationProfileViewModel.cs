using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.CIB.IlluminationProfile;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Utilities.SourceGenerators.Attributes;
using Humanizer;
using Local.SQL.Cache.Providers.Extensions;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using System.IO;
using System.Text;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.CIB;

[IOCAppService(ServiceType = typeof(CIBIlluminationProfileViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBIlluminationProfileViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Image Param" },
        new() { StepName = "Find Haze Position" },
        new() { StepName = "Illumination Profile" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private IReadOnlyList<CIBIlluminationProfileDTO> _calibratings = [];

    [ObservableProperty]
    private IReadOnlyList<CIBIlluminationProfileDTO> _selectedCalibratingItems = [];

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationStatus> _calibratingStatuses = [];

    #endregion Calibrate

    [ObservableProperty]
    private IReadOnlyList<CIBIlluminationProfileDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<CIBIlluminationProfileDTO> _selectedReviewItems = [];

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private CIBIlluminationProfileCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private CIBIlluminationProfileDTO[] _calibrations = [];

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        MicroscopeCalChip = CalibrationStatusService.GetCalibration<MicroscopeCalChipDTO>();

        if (CalibratingStatuses.Count == 0)
            CalibratingStatuses = [.. ApplicationCookie.ProductivityInformations.Select(t => new ProductivityInformationStatus { SelectedItem = t })];

        (var isHasCache, Cache) = RecipeCacheProvider.TryGetOrDefault<CIBIlluminationProfileCache>();
        Calibrations = CacheProvider.GetOrDefaultArray<CIBIlluminationProfileDTO>();

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
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition));

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
                StageViewModel.SetAbsoluteStageTheta(0);
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition != Point.Origin
                    ? Cache.Item.HazeFindBFMachinePosition
                    : Guard.IsNotNullAndReturn(MicroscopeCalChip.HazeItem).BrightFieldMachinePosition));

                return true;

            case 2:
                return true;

            case 3:
                CalibratingStatuses
                    .Single(t => t.SelectedItem == Cache.ProductivityInformation)
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

            StageViewModel.SetAbsoluteStageTheta(0);
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
        return InvokeCalibrateAsync(async () =>
        {
            var detectImageDirectory = ImageFileDirectory;

            var currentOpticsApodizationModeEnum = OpticsViewModel.GetApodizationMode();
            var currentOpticsPolarizationModeEnum = OpticsViewModel.GetPolarizationMode();
            var currentCollectorPolarizationModeEnum = OpticsViewModel.GetCollectorPolarizationMode();
            var cibInformations = ApplicationCookie.CIBInformations;
            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                Cache.Item.LaserLightInformation,
                Cache.Item.HazeFindBFMachinePosition,
                Cache.Item.ImageWidth,
                Cache.CalibratingRetryTimes,
                Cache.CalibrateThreshold,
                Cache.CalibrateThresholdMin,
                Cache.CalibrateThresholdMax,
                currentOpticsApodizationModeEnum,
                currentOpticsPolarizationModeEnum,
                currentCollectorPolarizationModeEnum,
                CIBInformations = new HtmlExpand(string.Empty, new HtmlTable([.. cibInformations.Select(t => t.ToHtmlAnonymous())])),
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            Calibratings = [];

            CIBViewModel.ToggleEnableAGC(cibInformations, true);
            CIBViewModel.ToggleProfileMode(cibInformations, CIBProfileModeEnum.PMTLog);
            CIBViewModel.SetIlluminationProfile(cibInformations, [.. Enumerable.Repeat(1d, Cache.ProductivityInformation.YPixel)]);

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0);
            StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);

            try
            {
                Logger.LogHtmlInformation("Illumination Profile", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

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
                            CIBViewModel.SetIlluminationProfile(cibInformations, [.. Enumerable.Repeat(1d, Cache.ProductivityInformation.YPixel)]);

                            var item = new CIBIlluminationProfileDTO(cibInformations)
                            {
                                ProductivityInformation = Cache.ProductivityInformation,
                                OpticsApodizationModeEnum = opticsApodizationModeEnum,
                                OpticsPolarizationModeEnum = opticsPolarizationModeEnum,
                                OpticsCollectorPolarizationModeEnum = opticsCollectorPolarizationModeEnum,
                                Items = [.. cibInformations.Select(t => new CIBIlluminationProfileDTOItem { CIBInformation = t })]
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
                                    darkFieldImage.Image.Save(imageFilePath);

                                    var itemItemData = new CIBIlluminationProfileDTOItem.Item
                                    {
                                        ImageHorizontalProjects = darkFieldImage.Image.GetHorizontalProjects(),
                                        RawImageFilePath = darkFieldImage.RawImageFilePath,
                                        ImageFilePath = imageFilePath
                                    };
                                    itemItem.Items = [.. itemItem.Items, itemItemData];

                                    Logger.LogHtmlInformation(itemItem.CIBInformation.ToString(), HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
                                    {
                                        itemItemData.ImageFilePath,
                                        itemItemData.RawImageFilePath
                                    }), HtmlLogUniqueId.LoggingHtml());
                                }

                                var resultList = new List<bool>();
                                foreach (var itemItem in item.Items)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    var imageHorizontalProjectsVector = Vector<double>.Build.Dense([.. itemItem.Items[times].ImageHorizontalProjects]);
                                    var targetPMTValue = item.TargetPMTValues.GetOrAdd(itemItem.CIBInformation, new Lazy<double>(imageHorizontalProjectsVector.Average));

                                    itemItem.Items[times].MaxRate = imageHorizontalProjectsVector.AbsoluteMaximum() / targetPMTValue;
                                    itemItem.Items[times].MinRate = imageHorizontalProjectsVector.AbsoluteMinimum() / targetPMTValue;
                                    if (itemItem.Items.Any(t => t.IsOk))
                                    {
                                        itemItem.Items[times].IsOk = true;
                                        resultList.Add(itemItem.Items[times].IsOk);

                                        continue;
                                    }

                                    itemItem.Items[times].IsOk = Cache.CalibrateThresholdMin <= itemItem.Items[times].MinRate
                                                                 && itemItem.Items[times].MaxRate <= Cache.CalibrateThresholdMax;
                                    resultList.Add(itemItem.Items[times].IsOk);

                                    if (itemItem.Items[times].IsOk) continue;

                                    itemItem.Items[times].Window = [.. targetPMTValue / imageHorizontalProjectsVector];
                                    if (itemItem.Window.Count <= 0) itemItem.Window = [.. Generate.Repeat(imageHorizontalProjectsVector.Count, 1d)];
                                    itemItem.Window = [.. Vector<double>.Build.Dense([.. itemItem.Window]).PointwiseMultiply(Vector<double>.Build.Dense([.. itemItem.Items[times].Window]))];

                                    CIBViewModel.SetIlluminationProfile([itemItem.CIBInformation], itemItem.Window);
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

                                if (++times > Cache.CalibratingRetryTimes - 1)
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
                    }
                }

                Guard.IsTrue(Save(Calibratings, cancellationToken));

                return Calibratings.All(t => t.IsCalibrated);
            }
            finally
            {
                OpticsViewModel.SetApodizationMode(currentOpticsApodizationModeEnum);
                OpticsViewModel.SetPolarizationMode(currentOpticsPolarizationModeEnum);
                OpticsViewModel.SetCollectorPolarizationMode(currentCollectorPolarizationModeEnum);
                CIBViewModel.ToggleEnableAGC(cibInformations, true);
                CIBViewModel.ToggleProfileMode(cibInformations, CIBProfileModeEnum.PMTLog);
                CIBViewModel.SetIlluminationProfile(cibInformations, [.. Enumerable.Repeat(1d, Cache.ProductivityInformation.YPixel)]);
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

    private bool Save(IReadOnlyList<CIBIlluminationProfileDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
    {
        update(Cache);

        foreach (var dto in dtos)
        {
            update(dto);
            Calibrations =
            [
                .. Calibrations.Where(t => t.ProductivityInformation != dto.ProductivityInformation
                                           || t.OpticsApodizationModeEnum != dto.OpticsApodizationModeEnum
                                           || t.OpticsPolarizationModeEnum != dto.OpticsPolarizationModeEnum
                                           || t.OpticsCollectorPolarizationModeEnum != dto.OpticsCollectorPolarizationModeEnum),
                dto
            ];
        }

        CacheProvider.SetArray(Calibrations, cancellationToken);
        RecipeCacheProvider.Set(Cache, cancellationToken);
    });

    #endregion 校准
}