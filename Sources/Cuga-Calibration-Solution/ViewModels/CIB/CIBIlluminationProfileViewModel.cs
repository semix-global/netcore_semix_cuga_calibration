using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.CIB.IlluminationProfile;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Utilities.SourceGenerators.Attributes;
using Humanizer;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Net.Utilities.Calibration;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.CIB;

[IOCAppService(ServiceType = typeof(CIBIlluminationProfileViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBIlluminationProfileViewModel : CalibrationViewModelBase<CIBIlluminationProfileCache>
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override IReadOnlyList<CalibrationItemStep> CalibrationSteps { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Image Param" },
        new() { StepName = "Find Haze Position" },
        new() { StepName = "Illumination Profile" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial IReadOnlyList<CIBIlluminationProfileDTO> Calibratings { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CIBIlluminationProfileDTO> SelectedCalibratingItems { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<ProductivityInformationStatus> CalibratingStatuses { get; set; } = [];

    #endregion Calibrate

    [ObservableProperty]
    public partial IReadOnlyList<CIBIlluminationProfileDTO> Reviews { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CIBIlluminationProfileDTO> SelectedReviewItems { get; set; } = [];

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public override partial CIBIlluminationProfileCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial CIBIlluminationProfileDTO[] Calibrations { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);


        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<CIBIlluminationProfileCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<CIBIlluminationProfileDTO>(cancellationToken);

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

            CIBViewModel.SetAGC(cibInformations, true);
            CIBViewModel.SetCIBProfileModeEnum(cibInformations, CIBProfileModeEnum.PMTLog);
            CIBViewModel.SetIlluminationProfile(cibInformations, [.. Enumerable.Repeat(1d, Cache.ProductivityInformation.YPixels)]);

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0d);
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
                            CIBViewModel.SetIlluminationProfile(cibInformations, [.. Enumerable.Repeat(1d, Cache.ProductivityInformation.YPixels)]);

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
                                    darkFieldImage.Image.SaveImage(imageFilePath);

                                    using var hImage = darkFieldImage.Image.ToHImage();
                                    var itemItemData = new CIBIlluminationProfileDTOItem.Item
                                    {
                                        ImageHorizontalProjects = hImage.GetHorizontalProjects(),
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
                                    var targetPMTValue = item.TargetPMTValues.GetOrAdd(itemItem.CIBInformation, _ => imageHorizontalProjectsVector.Average());

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
                CIBViewModel.SetAGC(cibInformations, true);
                CIBViewModel.SetCIBProfileModeEnum(cibInformations, CIBProfileModeEnum.PMTLog);
                CIBViewModel.SetIlluminationProfile(cibInformations, [.. Enumerable.Repeat(1d, Cache.ProductivityInformation.YPixels)]);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetBrightFieldAbsoluteStageXy(hazeBFPosition);
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
                dto,
                .. Calibrations.Where(t => (t.ProductivityInformation == dto.ProductivityInformation
                                            && t.OpticsApodizationModeEnum == dto.OpticsApodizationModeEnum
                                            && t.OpticsPolarizationModeEnum == dto.OpticsPolarizationModeEnum
                                            && t.OpticsCollectorPolarizationModeEnum == dto.OpticsCollectorPolarizationModeEnum) == false)
            ];
        }

        ApplicationCookieService.SetCalibrations(Calibrations, cancellationToken);
        ApplicationCookieService.SetCache(Cache, cancellationToken);
    });

    public override void UpdateEntryStatus(CalibrationDTOBase[] calibrations, CancellationToken cancellationToken)
    {
        var temps = Guard.IsAssignableToTypeAndReturn<CIBIlluminationProfileDTO[]>(calibrations);
        var status = Entry.Status;

        CalibratingStatuses =
        [
            .. ApplicationCookie.ProductivityInformations.Select(t => new ProductivityInformationStatus { SelectedItem = t, IsCalibrated = false })
        ];

        Calibrations =
        [
            .. temps
                .Where(t => ApplicationCookie.ProductivityInformations.Contains(t.ProductivityInformation)
                            && ApplicationCookie.OpticsApodizationModeEnums.Contains(t.OpticsApodizationModeEnum)
                            && ApplicationCookie.OpticsPolarizationModeEnums.Contains(t.OpticsPolarizationModeEnum)
                            && ApplicationCookie.OpticsCollectorPolarizationModeEnums.Contains(t.OpticsCollectorPolarizationModeEnum))
                .DistinctBy(t => (t.ProductivityInformation, t.OpticsApodizationModeEnum, t.OpticsPolarizationModeEnum, t.OpticsCollectorPolarizationModeEnum))
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
                    .Count() == ApplicationCookie.OpticsApodizationModeEnums.Count * ApplicationCookie.OpticsPolarizationModeEnums.Count * ApplicationCookie.OpticsCollectorPolarizationModeEnums.Count
                * ApplicationCookie.CIBInformations.Count;
        }

        status.TotalCalibrationCount = ApplicationCookie.ProductivityInformations.Count
                                       * ApplicationCookie.OpticsApodizationModeEnums.Count * ApplicationCookie.OpticsPolarizationModeEnums.Count * ApplicationCookie.OpticsCollectorPolarizationModeEnums.Count
                                       * ApplicationCookie.CIBInformations.Count;
        status.CalibratedCount = Calibrations.Where(t => t.IsCalibrated)
            .SelectMany(t => t.Items)
            .Count();
        status.VerifiedCount = Calibrations.Where(t => t.IsVerified)
            .SelectMany(t => t.Items)
            .Count();

        var detailList = new List<CalibrationViewModelStatus.Detail>(status.TotalCalibrationCount);
        foreach (var productivityInformation in ApplicationCookie.ProductivityInformations)
        {
            foreach (var opticsApodizationModeEnum in ApplicationCookie.OpticsApodizationModeEnums)
            {
                foreach (var opticsPolarizationModeEnum in ApplicationCookie.OpticsPolarizationModeEnums)
                {
                    foreach (var opticsCollectorPolarizationModeEnum in ApplicationCookie.OpticsCollectorPolarizationModeEnums)
                    {
                        var item = Calibrations.SingleOrDefault(t => t.ProductivityInformation == productivityInformation
                                                                     && t.OpticsApodizationModeEnum == opticsApodizationModeEnum
                                                                     && t.OpticsPolarizationModeEnum == opticsPolarizationModeEnum
                                                                     && t.OpticsCollectorPolarizationModeEnum == opticsCollectorPolarizationModeEnum);

                        foreach (var cibInformation in ApplicationCookie.CIBInformations)
                        {
                            var itemData = item?.Items?.SingleOrDefault(i => i.CIBInformation == cibInformation);

                            detailList.Add(new CalibrationViewModelStatus.Detail(
                                $"{productivityInformation}/{opticsApodizationModeEnum}/{opticsPolarizationModeEnum}/{opticsCollectorPolarizationModeEnum}/{cibInformation}",
                                item is not null
                                    ? item.IsCalibrated ? itemData is not null : null
                                    : null,
                                item is not null
                                    ? item.IsVerified ? itemData is not null : null
                                    : null));
                        }
                    }
                }
            }
        }

        status.Details = detailList;
    }

    #endregion 校准
}