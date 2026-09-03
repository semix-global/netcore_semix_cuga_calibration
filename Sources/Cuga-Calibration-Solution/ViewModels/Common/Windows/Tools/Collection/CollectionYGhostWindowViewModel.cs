using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Setting;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.SQL.Cache.Providers.Bases;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.ComponentModel;
using System.IO;
using Constants = Net.Utilities.Models.Constants;
using Generate = MathNet.Numerics.Generate;
using Point = Net.Utilities.Models.Geometries.Point;
using Range = ScottPlot.Range;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Collection;

public sealed partial class CollectionYGhostCache : ObservableCacheBase
{
    [ObservableProperty]
    public partial OpticsIlluminationModeEnum OpticsIlluminationModeEnum { get; set; } = OpticsIlluminationModeEnum.OI;

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = null!;

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    [ObservableProperty]
    public partial int PMTId { get; set; } = 8;

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(YStep))]
    public partial Point StartPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(YStep))]
    public partial Point StopPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(YStep))]
    public partial int GrabCount { get; set; } = 8;

    public double YStep => (StopPosition.Y - StartPosition.Y) / GrabCount;

    [ObservableProperty]
    public partial double AligndBValue { get; set; } = -1;

    [ObservableProperty]
    public partial double YUmStandard { get; set; } = 23;

    [ObservableProperty]
    public partial double Threshold { get; set; } = 5e-4;
}

public sealed partial class CollectionYGhostResult : ObservableObject
{
    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial IReadOnlyList<CollectionYGhostResultItem> Items { get; set; } = [];

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    partial void OnItemsChanged(IReadOnlyList<CollectionYGhostResultItem>? oldValue, IReadOnlyList<CollectionYGhostResultItem> newValue)
    {
        foreach (var item in oldValue ?? []) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        RefreshPlot();

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshPlot();
    }

    public CollectionYGhostResult()
    {
        PlotDataSource.Configure(new Rows(), 2);
        PlotDataSource.SetTitle(0, "Y Ghost(Y: dB - X: pix)");
        PlotDataSource.SetTitle(1, "Align Y Ghost(Y: dB - X: um)");
    }

    private void RefreshPlot()
    {
        try
        {
            var yGhostScatterLines = PlotDataSource.GetOrAddScatterLines(0, Items.Count);
            var alignMarkerses = PlotDataSource.GetOrAddScatterMarkerses(0, Items.Count);

            var yGhostAlignScatterLines = PlotDataSource.GetOrAddScatterLines(1, Items.Count);
            var markerses = PlotDataSource.GetOrAddScatterMarkerses(1, Items.Count);

            foreach (var (index, item) in Items.Index())
            {
                if (item.YGhostPoints.Length <= 0) continue;

                yGhostScatterLines[index].Update(
                    $"No.{index}:{item.FindPosition:0.###}(um)",
                    item.YGhostPoints,
                    Net.Utilities.ScottPlot.Helper.Constants.Turbo.GetColor(index, new Range(0, Items.Count - 1)));

                if (item.AlignPoint != Point.Origin)
                {
                    alignMarkerses[index].Update(
                        $"No.{index} Align Point",
                        [item.AlignPoint],
                        Net.Utilities.ScottPlot.Helper.Constants.Turbo.GetColor(index, new Range(0, Items.Count - 1)),
                        MarkerShape.OpenCircle);
                    alignMarkerses[index].MarkerSize = 20;
                }

                if (item.YGhostAlignPoints.Length <= 0) continue;

                yGhostAlignScatterLines[index].Update(
                    $"No.{index}:{item.FindPosition:0.###}(um)",
                    item.YGhostAlignPoints,
                    Net.Utilities.ScottPlot.Helper.Constants.Turbo.GetColor(index, new Range(0, Items.Count - 1)));

                markerses[index].Update(
                    $"No.{index} minimum(db),Result {(item.IsOk ? "OK" : "Failed")}:{item.YGhostResultValue:0.######}",
                    [item.YGhostMinimumPoint],
                    Net.Utilities.ScottPlot.Helper.Constants.Turbo.GetColor(index, new Range(0, Items.Count - 1)),
                    MarkerShape.OpenCircle);
                markerses[index].MarkerSize = 20;
            }
        }
        finally
        {
            PlotDataSource.AutoScaleRefresh();
        }
    }

    public object ToHtmlAnonymous() => new
    {
        CIBInformation,
        Details = new HtmlTable([.. Items.Select(t => t.ToHtmlAnonymous())]),
        ScatterPlotControl = new HtmlContainer([.. PlotDataSource.GetAllHtmlPlot2DLinesCharts()])
    };
}

public sealed partial class CollectionYGhostResultItem : ObservableObject
{
    [ObservableProperty]
    public partial bool IsOk { get; set; }

    [ObservableProperty]
    public partial Point FindPosition { get; set; }

    [ObservableProperty]
    public partial string ImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RawImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Point AlignPoint { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point[] YGhostPoints { get; set; } = [];

    [ObservableProperty]
    public partial Point[] YGhostAlignPoints { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(YGhostResultValue))]
    public partial Point YGhostMinimumPoint { get; set; } = Point.Origin;

    public double YGhostResultValue => YGhostMinimumPoint.Y;

    public object ToHtmlAnonymous() => new
    {
        FindPosition,
        IsOk,
        YGhostResultValue,
        ResultImageh = new HtmlImage(ImageFilePath),
        RawImageFilePath
    };
}

[IOCAppService(ServiceType = typeof(CollectionYGhostWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CollectionYGhostWindowViewModel(
    StageViewModel stageViewModel,
    OpticsViewModel opticsViewModel,
    CIBViewModel cibViewModel,
    IOptions<ApplicationSetting> options,
    ApplicationCookie applicationCookie,
    ICacheProvider cacheProvider,
    IHostEnvironment hostEnvironment,
    IDialogWindowProvider dialogWindowProvider,
    ILogger<CollectionYGhostWindowViewModel> logger,
    IApplicationCookieService applicationCookieService,
    CalibrationSetting calibrationSetting) : ViewModelBase
{
    public string Name => "Collection Y Ghost";

    public IReadOnlyList<string> Steps { get; } =
    [
        "Step 1 Alignment",
        "Step 2 Y Ghost"
    ];

    public string ImageDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", nameof(CollectionYGhostWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public ApplicationCookie ApplicationCookie => applicationCookie;

    public Guid HtmlLogUniqueId { get; private set; }

    [DefaultCache]
    [ObservableProperty]
    public partial CollectionYGhostCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial CIBYPixelSizeDTO YPixelSize { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<CollectionYGhostResult> YGhostResults { get; set; } = [];

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = HostApplication.GetRequiredService<AlignmentUserControlViewModel>();

    [RelayCommand]
    private void Loaded()
    {
        Cache = applicationCookieService.GetOrDefault<CollectionYGhostCache>(false, CancellationToken.None);
        Cache.PMTId = calibrationSetting.SettingCommonParam.MainCIBInformation.PMTId;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step0Async(CancellationToken cancellationToken)
    {
        await InvokeAsync(0, async () =>
        {
            AlignmentUserControlViewModel.CalChipSiteModelEnum = Cache.CalChipSiteModelEnum;
            AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;

            dialogWindowProvider.TryShowDialog("Yes: use dark field alignment? No: to use bright field alignment ?",
                out var dialogResult,
                DialogButtonsEnum.YesNo,
                DialogIconEnum.Question);

            AlignmentUserControlViewModel.IsDarkFieldAlignment = dialogResult == DialogResultEnum.Yes;

            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            var alignmentResult = AlignmentUserControlViewModel.AlignmentResult;

            logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.CalChipSiteModelEnum,
                AlignmentUserControlViewModel.IsDarkFieldAlignment,
                AlignmentResult = new HtmlQuote(alignmentResult.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());

            stageViewModel.SetCalChipDswBrightFieldAbsoluteStageXy(stageViewModel.MachineToBrightFieldPosition((alignmentResult.MarkPoint1 + (Vector)alignmentResult.MarkPoint2) / 2d));

            Cache.AlignmentResult = alignmentResult;

            return true;
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeAsync(1, async () =>
        {
            YPixelSize = GuardExtensions.IsNotNullAndReturn(applicationCookieService.GetCalibrations<CIBYPixelSizeDTO>()
                .SingleOrDefault(t => t.ProductivityInformation == Cache.ProductivityInformation && t.PmtId == Cache.PMTId && t.IsOk), "Please calibrate CIB Y pixel size first!");
            YGhostResults = [];

            CIBConfiguration cibConfiguration = new()
            {
                IsAutoGainControl = true,
                IsL0K = false,
                CIBProfileMode = CIBProfileModeEnum.PMTLog
            };

            logger.LogHtmlInformation("Diagnosis Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.OpticsIlluminationModeEnum,
                Cache.ProductivityInformation,
                Cache.CalChipSiteModelEnum,
                Cache.PMTId,
                Cache.ImageWidth,
                Cache.LaserLightInformation,
                YPixelSize,
                Cache.Threshold,
                Cache.YUmStandard,
                Cache.StartPosition,
                Cache.StopPosition,
                Cache.YStep,
                MaximumdBValue = Cache.AligndBValue,
                CibConfiguration = new HtmlQuote(cibConfiguration.ToHtmlAnonymous()),
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());
            try
            {
                opticsViewModel.ToggleZoosClinder(Cache.OpticsIlluminationModeEnum, true);

                var cibInformations = ApplicationCookie.CIBInformations.Where(t => t.PMTId == Cache.PMTId).ToArray();
                YGhostResults =
                [
                    .. cibInformations.Select(t => new CollectionYGhostResult
                    {
                        CIBInformation = t
                    })
                ];

                var positionIndex = 0;
                foreach (var yPosition in Generate.LinearRange(Cache.StartPosition.Y, Cache.YStep, Cache.StopPosition.Y))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var findPosition = new Point(Cache.StartPosition.X, yPosition);
                    stageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(findPosition);

                    var darkFieldImages = await cibViewModel.GetPMTImagesAsync(
                        Cache.ProductivityInformation,
                        StageCoordinateSystemEnum.Dark,
                        findPosition,
                        Cache.ImageWidth,
                        cibInformations,
                        (false, Cache.CalChipSiteModelEnum),
                        (false, Cache.OpticsConfiguration),
                        (false, cibConfiguration),
                        (false, Cache.LaserLightInformation),
                        false,
                        cancellationToken,
                        isKeepRawImageCIBProfileModeEnum: true);

                    var bitmapImages = darkFieldImages.Select(t => t.Image).ToList();
                    if (hostEnvironment.IsDevelopment()) bitmapImages = [.. GetMockImages(cibInformations, positionIndex)];

                    foreach (var (index, bitmapImage) in bitmapImages.Select((t, i) => (i, t)))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var resultItem = GetResultItem(bitmapImage);
                        resultItem.FindPosition = findPosition;
                        if (hostEnvironment.IsDevelopment() == false)
                            resultItem.RawImageFilePath = darkFieldImages[index].RawImageFilePath;
                        YGhostResults[index].Items = [.. YGhostResults[index].Items, resultItem];
                    }

                    positionIndex++;
                }

                foreach (var yGhostResult in YGhostResults)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    yGhostResult.Items =
                    [
                        .. yGhostResult.Items.Select(t =>
                        {
                            var yGhostAlignPoints = t.YGhostPoints.Select(tt => new Point((tt.X - t.AlignPoint.X) * YPixelSize.YPixelSize, tt.Y)).ToArray();
                            var yGhostMinimumPoint = yGhostAlignPoints.Last(o => o.X < Cache.YUmStandard);

                            var resultItem = new CollectionYGhostResultItem
                            {
                                FindPosition = t.FindPosition,
                                ImageFilePath = t.ImageFilePath,
                                AlignPoint = t.AlignPoint,
                                YGhostPoints = [.. t.YGhostPoints],
                                YGhostAlignPoints = [.. yGhostAlignPoints],
                                YGhostMinimumPoint = yGhostMinimumPoint,
                            };
                            resultItem.IsOk = resultItem.YGhostResultValue <= Cache.Threshold;
                            return resultItem;
                        })
                    ];

                    logger.LogHtmlInformation($"{yGhostResult.CIBInformation} Result", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        Result = new HtmlQuote(yGhostResult.ToHtmlAnonymous())
                    }), HtmlLogUniqueId.LoggingHtml());
                }

                return YGhostResults.SelectMany(t => t.Items).All(t => t.IsOk);
            }
            finally
            {
                opticsViewModel.ToggleZoosClinder(Cache.OpticsIlluminationModeEnum, false);
            }

            CollectionYGhostResultItem GetResultItem(BitmapImage bitmapImage)
            {
                var filePath = Path.Combine(ImageDirectory, $"GUID{Guid.NewGuid()}.png");

                bitmapImage.SaveImage(filePath);

                var yProjects = bitmapImage.GetHorizontalProjects();
                var (maxGrayValue, _, _, _) = bitmapImage.GetMaxMinGrayValue(new Rect(0, 0, bitmapImage.Size.Width, bitmapImage.Size.Height));
                var yGhostValues = yProjects.Select(t => 10 * Math.Log10(t / maxGrayValue)).ToArray();

                var yGhostPoints = yGhostValues.Select((t, i) => new Point(i, t)).ToArray();

                var maxPoints = Extremumor.FindMaxima(yGhostPoints);
                var alignPoint = maxPoints.Results.LastOrDefault(t => t.Y > Cache.AligndBValue);
                if (alignPoint == Point.Origin)
                    ThrowHelper.ThrowArgumentException("Can't find the falling edge in the image. " +
                                                       "Please set proper start and end coordinates so the image always includes the falling edge.");

                return new CollectionYGhostResultItem
                {
                    ImageFilePath = filePath,
                    AlignPoint = alignPoint,
                    YGhostPoints = [.. yGhostPoints]
                };
            }
        });
    }

    [RelayCommand]
    private void Close()
    {
        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            cacheProvider.Set(Cache, cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: Failed to save cache", Name);
        }

        CloseView(true);
    }

    private static IReadOnlyList<BitmapImage> GetMockImages(CIBInformation[] cibInformations, int positionIndex)
    {
        var bitmapImages = new List<BitmapImage>();
        foreach (var _ in cibInformations)
        {
            //mock
            var mockImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"Assets\Data\YGhost\{positionIndex}.raw");
            var bytes = System.IO.File.ReadAllBytes(mockImagePath);
            var (size, _, _) = RAWImageFactory.GetSize(bytes);

            using var hImage = RAWImageFactory.CreateImage(mockImagePath, false);
            using var reduceImage = hImage.ToRoi(new Rect(0, 4, size.Width, size.Height - 8));

            using var mirrorImage = reduceImage.VerticalFlip();

            var bitmapImage = reduceImage.ToBitmapImage(12);
            bitmapImage.SaveImage(@$"G:\{cibInformations}_{positionIndex}.jpg");
            bitmapImages = [.. bitmapImages, bitmapImage];
        }

        return bitmapImages;
    }

    private async Task InvokeAsync(int stepIndex, Func<Task<bool>> func)
    {
        await Task.Run(async () =>
        {
            var isEndHtml = stepIndex == Steps.Count - 1;
            var isInitHtmlLog = stepIndex == 0 || isEndHtml;

            HtmlLogUniqueId = isInitHtmlLog ? Guid.NewGuid() : HtmlLogUniqueId;

            if (isInitHtmlLog) logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation(Steps[stepIndex], HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                isSuccess = await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                isEndHtml = true;

                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog($"{Name}: Canceled", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    return false;
                }

                dialogWindowProvider.ShowDialog($"""
                                                 {Name}: Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogHtmlError(ex, Name, HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            }
            finally
            {
                if (isEndHtml)
                    logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{Name}_{(isSuccess ? "OK" : "Failed")}"));
            }

            if (isSuccess)
            {
                if (isEndHtml) dialogWindowProvider.ShowDialog($"{Name}:{Steps[stepIndex]} Success");
            }
            else
                dialogWindowProvider.ShowDialog($"{Name}: {Steps[stepIndex]} Error", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return isSuccess;
        }).ConfigureAwait(false);
    }
}