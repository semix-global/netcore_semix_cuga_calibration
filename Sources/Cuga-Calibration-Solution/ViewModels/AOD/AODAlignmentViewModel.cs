using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.AOD.Alignment;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Status;
using Core.Models.Models.Microscope.CalChip;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Hosting;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Algorithms.Modules.CurveFitting;
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
using System.Text;
using Core.Models.Models.Common.Cookies;
using Constants = Net.Utilities.Models.Constants;

namespace CugaCalibration.ViewModels.AOD;

[IOCAppService(ServiceType = typeof(AODAlignmentViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AODAlignmentViewModel : CalibrationViewModelBase
{
    #region 属性

    public override string CalibrateDirectoryName => Cache.ProductivityInformation.ToString();

    public override string CalibrateFileName => Cache.ProductivityInformation.ToString();

    public override IReadOnlyList<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Select Productivity Information" },
        new() { StepName = "Image Param" },
        new() { StepName = "Find Haze Position" },
        new() { StepName = "AOD Alignment" }
    ];

    #region 界面相关

    #region Calibrate

    [ObservableProperty]
    private AODAlignmentDTO _calibratingItem = new();

    [ObservableProperty]
    private IReadOnlyList<ProductivityInformationStatus> _calibratingStatuses = [];

    #endregion Calibrate

    [ObservableProperty]
    private IReadOnlyList<AODAlignmentDTO> _reviews = [];

    [ObservableProperty]
    private IReadOnlyList<AODAlignmentDTO> _selectedReviewItems = [];

    #endregion 界面相关

    #region 缓存

    [RecipeCache]
    [ObservableProperty]
    private AODAlignmentCache _cache = new();

    [DefaultCache]
    [ObservableProperty]
    private AODAlignmentDTO[] _calibrations = [];

    [ObservableProperty]
    private MicroscopeCalChipDTO _microscopeCalChip = new();

    #endregion 缓存

    #endregion 属性

    #region 控制校准业务

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (LoadDepends() == false) return false;

        MicroscopeCalChip = ApplicationCookieService.GetCalibration<MicroscopeCalChipDTO>(cancellationToken);

        Cache = ApplicationCookieService.GetCache<AODAlignmentCache>(cancellationToken);
        Calibrations = ApplicationCookieService.GetCalibrations<AODAlignmentDTO>(cancellationToken);

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
                CalibratingItem = new AODAlignmentDTO();

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
            var prescanCache = CacheProvider.GetOrDefault<PrescanAODWaveformElectrodeOffsetCache>(cancellationToken);
            var prescanResult = prescanCache.Results.SingleOrDefault(t => t.GeneratePrescanAODWaveformParam.ProductivityInformation.OpticsIlluminationModeEnum == Cache.ProductivityInformation.OpticsIlluminationModeEnum
                                                                          && t.GeneratePrescanAODWaveformParam.ProductivityInformation.OpticsMagType == Cache.ProductivityInformation.OpticsMagType);
            if (prescanResult is null)
            {
                const string comment = "Warning: Prescan AOD Waveform Param No matched found for current Productivity Information!";
                Logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, new HtmlComment(comment), HtmlLogUniqueId.LoggingHtml());

                DialogWindowProvider.ShowDialog(comment, DialogButtonsEnum.OK, DialogIconEnum.Error);

                return false;
            }

            Cache.Item.FlatnessGeneratePrescanAODWaveformParam = prescanResult.GeneratePrescanAODWaveformParam.Clone();
            Cache.Item.FlatnessGeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation.Clone();
            Cache.Item.StartPrescanFrequency = prescanResult.GeneratePrescanAODWaveformParam.CenterFrequency - prescanResult.GeneratePrescanAODWaveformParam.BandWidth;
            Cache.Item.StopPrescanFrequency = prescanResult.GeneratePrescanAODWaveformParam.CenterFrequency + prescanResult.GeneratePrescanAODWaveformParam.BandWidth;

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
                Cache.Item.CIBInformation,
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
            var detectImageDirectory = ImageFileDirectory;

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
                FlatnessGeneratePrescanAODWaveformParam = new HtmlQuote(Cache.Item.FlatnessGeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
                Cache.Item.StartPrescanFrequency,
                Cache.Item.StepPrescanFrequency,
                Cache.Item.StopPrescanFrequency,
                Cache.Item.RangeSkipFitCount,
                Cache.Threshold,
                detectImageDirectory,
                Cache.ProductivityInformation.YPixels
            }), HtmlLogUniqueId.LoggingHtml());

            CalibratingItem.ProductivityInformation = Cache.ProductivityInformation;
            CalibratingItem.Items = [];
            CalibratingItem.Slope = 0d;
            CalibratingItem.Intercept = 0d;
            CalibratingItem.RSquared = 0d;
            CalibratingItem.FitAlignmentPoints = [];
            CalibratingItem.IsCalibrated = false;

            var hazeBFPosition = StageViewModel.MachineToBrightFieldPosition(Cache.Item.HazeFindBFMachinePosition);
            StageViewModel.SetAbsoluteStageTheta(0d);
            StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(hazeBFPosition);

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
                    var aodWaveformResultItem = AODWaveformGenerator.GeneratePrescanAODWaveform(generatePrescanAODWaveformParam.AdaptTo(), cancellationToken);

                    var itemItem = new AODAlignmentDTOItem
                    {
                        PrescanFrequency = prescanFrequency,
                        PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResultItem),
                        PrescanAODWaveformResultFilePath = aodWaveformResultItem.FilePath
                    };

                    LaserViewModel.SetPrescanAODWaveProfiles(Cache.ProductivityInformation.OpticsIlluminationModeEnum, itemItem.PrescanAODWaveformProfiles);

                    using var darkFieldImage = await CIBViewModel.GetPMTImageAsync(
                        Cache.ProductivityInformation,
                        StageCoordinateSystemEnum.Dark,
                        hazeBFPosition,
                        Cache.Item.ImageWidth,
                        Cache.Item.CIBInformation,
                        (false, CalChipSiteModelEnum.HazeModel),
                        (false, Cache.Item.OpticsConfiguration),
                        (false, Cache.Item.CIBConfiguration),
                        (true, null),
                        false,
                        cancellationToken);

                    var imageFilePath = Path.Combine(detectImageDirectory, $"{itemItem.PrescanFrequency:0.###}MHz", $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                    darkFieldImage.Image.SaveImage(imageFilePath);

                    itemItem.ImageFilePath = imageFilePath;
                    itemItem.RawImageFilePath = darkFieldImage.RawImageFilePath;

                    using var hImage = darkFieldImage.Image.ToHImage();
                    itemItem.ImageHorizontalProjects = hImage.GetHorizontalProjects();

                    CalibratingItem.Items = [.. CalibratingItem.Items, itemItem];

                    Logger.LogHtmlInformation($"{itemItem.PrescanFrequency:0.###}", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                    {
                        FlatnessGeneratePrescanAODWaveformParam = new HtmlQuote(generatePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
                        itemItem.PrescanAODWaveformResultFilePath,
                        PrescanAODWaveformProfiles = new HtmlTable([.. itemItem.PrescanAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())]),
                        itemItem.RawImageFilePath,
                        HtmlImage = new HtmlImage(itemItem.ImageFilePath)
                    }), HtmlLogUniqueId.LoggingHtml());
                }

                var skipItemItems = CalibratingItem.Items.Skip(Cache.Item.RangeSkipFitCount).SkipLast(Cache.Item.RangeSkipFitCount).ToArray();
                var (slope, intercept, rSquared, yPredicted) = PolynomialCurve.Fit1(
                    Vector<double>.Build.DenseOfEnumerable(skipItemItems.Select(t => t.PrescanFrequency)),
                    Vector<double>.Build.DenseOfEnumerable(skipItemItems.Select(t => (double)Guard.IsNotNullAndReturn(t.ProjectMaxPixel))));

                CalibratingItem.Slope = slope;
                CalibratingItem.Intercept = intercept;
                CalibratingItem.RSquared = HostEnvironment.IsProduction() ? rSquared : Random.Shared.NextDouble();
                CalibratingItem.FitAlignmentPoints = [.. skipItemItems.Index().Select(t => new Point(t.Item.PrescanFrequency, yPredicted[t.Index]))];
                CalibratingItem.IsCalibrated = CalibratingItem.RSquared >= Cache.Threshold;

                var htmlBullet = new HtmlBullet(new
                {
                    CalibratingItem.Slope,
                    CalibratingItem.Intercept,
                    CalibratingItem.RSquared,
                    ScatterPlotControl = new HtmlContainer([.. CalibratingItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
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
                StageViewModel.SetCalChipHazeBrightFieldAbsoluteStageXy(hazeBFPosition);
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
                    Cache.ProductivityInformation,
                    Cache.Threshold
                }), HtmlLogUniqueId.LoggingHtml());

                if (selectedReviewItem.IsCalibrated) selectedReviewItem.IsVerified = true;

                var htmlBullet = new HtmlBullet(new
                {
                    selectedReviewItem.Slope,
                    selectedReviewItem.Intercept,
                    selectedReviewItem.RSquared,
                    ScatterPlotControl = new HtmlContainer([.. selectedReviewItem.ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
                });

                if (selectedReviewItem.IsOk)
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

    private bool Save(IReadOnlyList<AODAlignmentDTO> dtos, CancellationToken cancellationToken) => InvokeSave(update =>
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
        status.ReviewCount = Calibrations.Count(t => t.IsVerified);
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