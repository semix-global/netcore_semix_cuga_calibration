using Core.Models.Models.Common.Cookies;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.CIB.AGCDelay;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Hosting;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.WPF.Enums;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.CIB;

[IOCAppService(ServiceType = typeof(CIBAGCDelayViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CIBAGCDelayViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override IReadOnlyList<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Image Param" },
        new() { StepName = "Find Haze Position" },
        new() { StepName = "Find Laser Light Information" },
        new() { StepName = "AGCDelay" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    public partial CIBAGCDelayDTO CalibratingItem { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<ProductivityInformationStatus> CalibratingStatuses { get; set; } = [];

    #endregion Calibrate

    [ObservableProperty]
    public partial IReadOnlyList<CIBAGCDelayDTO> Reviews { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<CIBAGCDelayDTO> SelectedReviewItems { get; set; } = [];

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    public partial CIBAGCDelayCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial CIBAGCDelayDTO[] Calibrations { get; set; } = [];

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    public ProductivityInformation LowProductivityInformation => Cache.ProductivityInformation.OpticsIlluminationModeEnum == OpticsIlluminationModeEnum.OI
        ? ApplicationCookie.OILowProductivityInformation
        : ApplicationCookie.NILowProductivityInformation;

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<CIBAGCDelayCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<CIBAGCDelayDTO>(cancellationToken);

        UpdateEntryStatus([..Calibrations], cancellationToken);

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
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition));

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
                CalibratingItem = new CIBAGCDelayDTO(ApplicationCookie.CIBInformations);

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
                return true;

            case 4:
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
    private Task<bool> Step0Async(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation
            }), HtmlLogUniqueId.LoggingHtml());

            if (Cache.ProductivityInformation != LowProductivityInformation && Calibrations.SingleOrDefault(t => t.ProductivityInformation == LowProductivityInformation)?.IsCalibrated != true)
            {
                var comment = $"Low Productivity Information {LowProductivityInformation} is not calibrated!";
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment(comment), HtmlLogUniqueId.LoggingHtml());

                DialogWindowProvider.ShowDialog(comment, DialogButtonsEnum.OK, DialogIconEnum.Error);

                return false;
            }

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
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            return ApplicationCookie.MicroscopeLensInformations.Contains(Cache.Item.MicroscopeLensInformation);
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
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
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
            var detectImageDirectory = ImageFileDirectory;
            var cibInformations = ApplicationCookie.CIBInformations;

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.Item.HazeFindBFMachinePosition,
                Cache.Item.TargetPMTValue,
                CIBInformations = new HtmlExpand(string.Empty, new HtmlTable([.. cibInformations.Select(t => t.ToHtmlAnonymous())])),
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.ProductivityInformation = Cache.ProductivityInformation;
            CalibratingItem.Coefficient = -1d;
            CalibratingItem.LaserLightInformationPMTVoltageValuePoints = [];

            CIBViewModel.SetAGC(cibInformations, false);
            CIBViewModel.SetCIBProfileModeEnum(cibInformations, CIBProfileModeEnum.PMTVoltage);
            CIBViewModel.SetGain(cibInformations, 0d);

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);

            var startCurrentHazeBFPosition = CIBViewModel.GetCIBInformationPosition(
                StageCoordinateSystemEnum.Dark,
                Cache.ProductivityInformation,
                CalibrationSetting.SettingCommonParam.MainCIBInformation,
                hazeBFPosition,
                Cache.Item.MicroscopeLensInformation);

            Logger.LogHtmlInformation("Find", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            try
            {
                foreach (var coefficient in Generate.LinearRangeContainsEdge(Cache.Item.StartCoefficient, Cache.Item.StepCoefficient, Cache.Item.StopCoefficient))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, coefficient);

                    var darkFieldImages = await CIBViewModel.GetPMTImagesAsync(
                        Cache.ProductivityInformation,
                        StageCoordinateSystemEnum.Dark,
                        startCurrentHazeBFPosition,
                        Cache.Item.ImageWidth,
                        cibInformations,
                        (true, null),
                        (false, Cache.Item.OpticsConfiguration),
                        (true, null),
                        (true, null),
                        false,
                        cancellationToken);

                    Logger.LogHtmlInformation($"{coefficient:0.###}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    var averages = new double[cibInformations.Count];
                    foreach (var (index, darkFieldImage) in darkFieldImages.Index())
                    {
                        using var _ = darkFieldImage;

                        var imageFilePath = Path.Combine(detectImageDirectory, cibInformations[index].ToString(), $"{coefficient:0.###}_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                        darkFieldImage.Image.SaveImage(imageFilePath);

                        using var hImage = darkFieldImage.Image.ToHImage();
                        var average = hImage.GetIntensity().Average;
                        averages[index] = average;

                        Logger.LogHtmlInformation(cibInformations[index].ToString(), HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                        {
                            hazeBFPosition,
                            startCurrentHazeBFPosition,
                            average,
                            Image = new HtmlImage(imageFilePath)
                        }), HtmlLogUniqueId.LoggingHtml());
                    }

                    var maxIndex = Vector<double>.Build.Dense(averages).MaximumIndex();
                    var max = averages[maxIndex];
                    CalibratingItem.LaserLightInformationPMTVoltageValuePoints = [.. CalibratingItem.LaserLightInformationPMTVoltageValuePoints, new Point(coefficient, max)];

                    if (max < Cache.Item.TargetPMTValue)
                    {
                        CalibratingItem.Coefficient = coefficient;

                        Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                        {
                            CalibratingItem.Coefficient,
                            max,
                            CIBInformation = cibInformations[maxIndex]
                        }), HtmlLogUniqueId.LoggingHtml());

                        continue;
                    }

                    break;
                }

                var isOk = CalibratingItem.Coefficient > 0;

                if (isOk)
                    Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
                    {
                        CalibratingItem.Coefficient,
                        Plot = new HtmlContainer(CalibratingItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts())
                    }), HtmlLogUniqueId.LoggingHtml());
                else Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment("No suitable Laser Light Information found!"), HtmlLogUniqueId.LoggingHtml());

                return isOk;
            }
            finally
            {
                CIBViewModel.SetAGC(cibInformations, true);
                CIBViewModel.SetCIBProfileModeEnum(cibInformations, CIBProfileModeEnum.PMTLog);
                StageViewModel.SetAbsoluteStageTheta(0d);
                StageViewModel.SetBrightFieldAbsoluteStageXy(hazeBFPosition);
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
            var cibDelays = CIBViewModel.GetDelays(Cache.ProductivityInformation, cibInformations);

            var averageZeroDelayError = 0d;
            if (Cache.ProductivityInformation != LowProductivityInformation) averageZeroDelayError = Calibrations.Single(t => t.ProductivityInformation == LowProductivityInformation).Items.Average(t => t.ZeroDelayError);

            var initDelay = 0d;
            initDelay += averageZeroDelayError;
            initDelay = CheckDelay(initDelay);

            Logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(new
            {
                Cache.ProductivityInformation,
                Cache.Item.MicroscopeLensInformation,
                OpticsConfiguration = new HtmlQuote(Cache.Item.OpticsConfiguration.ToHtmlAnonymous()),
                Cache.Item.HazeFindBFMachinePosition,
                Cache.Item.TargetPMTValue,
                Cache.Item.MarkerLengthPixel,
                Cache.CalibratingRetryTimes,
                Cache.CalibratingThreshold,
                CalibratingItem.ProductivityInformation.YPixels,
                CalibratingItem.ProductivityInformation.OriginYPixels,
                CalibratingItem.ProductivityInformation.OriginYPixelsStartIndex,
                CalibratingItem.ProductivityInformation.OriginYPixelsEndIndex,
                CIBInformations = new HtmlExpand(string.Empty, new HtmlTable([.. cibInformations.Select(t => t.ToHtmlAnonymous())])),
                CIBDelays = new HtmlExpand(string.Empty, new HtmlTable([.. cibDelays.Select(t => t.ToHtmlAnonymous())])),
                initDelay,
                averageZeroDelayError,
                detectImageDirectory
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.TargetPixelValues = new ConcurrentDictionary<CIBInformation, double>(cibInformations.Select(t => new KeyValuePair<CIBInformation, double>(t, (CalibratingItem.ProductivityInformation.OriginYPixels - 1d) / 2d)));
            CalibratingItem.Items =
            [
                .. cibInformations.Select(t => new CIBAGCDelayDTOItem
                {
                    CIBInformation = t,
                    ZeroDelayError = averageZeroDelayError,
                    Delay = initDelay
                })
            ];

            CIBViewModel.SetCIBProfileModeEnum(cibInformations, CIBProfileModeEnum.PMTVoltage);

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetCalChipHazeDarkFieldAbsoluteStageXyByNotAutoFocus(hazeBFPosition);

            var startCurrentHazeBFPosition = CIBViewModel.GetCIBInformationPosition(
                StageCoordinateSystemEnum.Dark,
                Cache.ProductivityInformation,
                CalibrationSetting.SettingCommonParam.MainCIBInformation,
                hazeBFPosition,
                Cache.Item.MicroscopeLensInformation);

            Logger.LogHtmlInformation("Find", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

            try
            {
                var times = 0;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Logger.LogHtmlInformation($"{times + 1}", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    var isSuccess = await GetDelayAsync(
                        cibInformations,
                        cibDelays,
                        CalibratingItem.Items,
                        times,
                        hazeBFPosition,
                        startCurrentHazeBFPosition,
                        detectImageDirectory,
                        cancellationToken);

                    var htmlBullet = new HtmlBullet(new
                    {
                        times,
                        CalibratingItem.ProductivityInformation,
                        CalibratingItem.Coefficient,
                        Plot = new HtmlContainer([.. CalibratingItem.ScatterPlotControls.Select(t => new HtmlExpand(t.Key.ToString(), new HtmlContainer(t.Value.GetAllHtmlPlot2DLinesCharts())))])
                    });

                    CalibratingItem.IsCalibrated = isSuccess;

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
                CIBViewModel.SetMarker(cibInformations, false);
                CIBViewModel.SetAGC(cibInformations, true);
                CIBViewModel.SetCIBProfileModeEnum(cibInformations, CIBProfileModeEnum.PMTLog);
                CIBViewModel.SetDelays(cibDelays);
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

    private async Task<bool> GetDelayAsync(
        IReadOnlyList<CIBInformation> cibInformations,
        IReadOnlyList<CIBDelayDTO> cibDelays,
        IReadOnlyList<CIBAGCDelayDTOItem> items,
        int times,
        Point hazeBFPosition,
        Point startCurrentHazeBFPosition,
        string detectImageDirectory,
        CancellationToken cancellationToken)
    {
        CIBViewModel.SetAGC(cibInformations, false);
        CIBViewModel.SetMarker(cibInformations, false);

        CIBViewModel.SetDelays([.. cibDelays.Select(t => t.Clone().WithAGCDelay(items.Single(tt => tt.CIBInformation == t.CIBInformation).Delay))]);

        CIBViewModel.SetAGC(cibInformations, true);
        CIBViewModel.SetMarker(cibInformations, true);

        LaserViewModel.SetPrescanAODWaveProfileByCoefficient(Cache.ProductivityInformation, CalibratingItem.Coefficient);

        var cibPMTImages = await CIBViewModel.GetPMTImagesAsync(
            Cache.ProductivityInformation,
            StageCoordinateSystemEnum.Dark,
            startCurrentHazeBFPosition,
            Cache.Item.ImageWidth,
            cibInformations,
            (true, null),
            (false, Cache.Item.OpticsConfiguration),
            (true, null),
            (true, null),
            false,
            cancellationToken);

        Logger.LogHtmlInformation("Images", HtmlHeaderLevelEnum.Header5, HtmlLogUniqueId.LoggingHtml());

        foreach (var (index, darkFieldImage) in cibPMTImages.Index())
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var _ = darkFieldImage;
            var itemItem = items[index];

            var imageFilePath = Path.Combine(detectImageDirectory, itemItem.CIBInformation.ToString(), $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
            darkFieldImage.Image.SaveImage(imageFilePath);

            using var hImage = darkFieldImage.Image.ToHImage();
            var horizontalProjects = hImage.GetHorizontalProjects();
            var itemItemData = new CIBAGCDelayDTOItem.Item
            {
                ImageHorizontalProjects =
                [
                    ..Generate.LinearRangeInt32(0, Cache.ProductivityInformation.OriginYPixelsStartIndex - 1).Select(_ => horizontalProjects[0]),
                    ..horizontalProjects,
                    ..Generate.LinearRangeInt32(Cache.ProductivityInformation.OriginYPixelsEndIndex, Cache.ProductivityInformation.OriginYPixels - 1).Select(_ => horizontalProjects[^1])
                ],
                RawImageFilePath = darkFieldImage.RawImageFilePath,
                ImageFilePath = imageFilePath
            };
            itemItemData.CalculateHorizontalProjectMinPixel(CalibratingItem.ProductivityInformation, Cache.Item.MarkerLengthPixel);

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

        var resultList = new List<bool>();
        foreach (var itemItem in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            itemItem.Items[times].Error = CalibratingItem.TargetPixelValues.Get(itemItem.CIBInformation) - itemItem.Items[times].HorizontalProjectMinPixel;
            itemItem.Items[times].IsOk = Math.Abs(itemItem.Items[times].Error) <= Cache.CalibratingThreshold;
            resultList.Add(itemItem.Items[times].IsOk);

            if (times == 0 && Cache.ProductivityInformation == LowProductivityInformation) itemItem.ZeroDelayError = itemItem.Items[times].Error;

            if (itemItem.Items[times].IsOk) continue;

            itemItem.Delay += itemItem.Items[times].Error;
            itemItem.Delay = CheckDelay(itemItem.Delay);
        }

        return resultList.All(t => t);
    }

    private double CheckDelay(double delay) => delay < 0d ? delay + Cache.ProductivityInformation.OriginYPixels /* 推迟到下一个周期 */ : delay;

    private bool Save(IReadOnlyList<CIBAGCDelayDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
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
        var temps = Guard.IsAssignableToTypeAndReturn<CIBAGCDelayDTO[]>(calibrations);
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
        status.ReviewCount = Calibrations.Where(t => t.IsVerified)
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