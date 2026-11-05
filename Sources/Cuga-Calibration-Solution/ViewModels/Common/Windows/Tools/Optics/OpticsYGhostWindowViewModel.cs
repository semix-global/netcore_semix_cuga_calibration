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
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Setting;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.SQL.Cache.Providers.Bases;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
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

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Optics;

public sealed partial class OpticsYGhostCache : ObservableCacheBase
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
    [NotifyPropertyChangedFor(nameof(GrabCount))]
    public partial Point StartPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GrabCount))]
    public partial Point StopPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GrabCount))]
    public partial double YStep { get; set; } = 23;

    public int GrabCount => Convert.ToInt32((StopPosition.Y - StartPosition.Y) / YStep) + 1;

    /// <summary>肩部搜索时判断沿前鼓包的对齐 dB 值.</summary>
    public const double AligndBValue = -1d;

    /// <summary>台阶检测的滑动窗口宽度(px).</summary>
    public const int AlignStepWindow = 20;

    /// <summary>有效台阶的最小对比度(dB).</summary>
    public const double MinContrastDb = 1.5;

    /// <summary>鬼影判定取值的标准距离(um).</summary>
    public const double YUmStandard = 23d;

    public static double Threshold => 10 * Math.Log10(5e-4);
}

public sealed partial class OpticsYGhostResult : ObservableObject
{
    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial IReadOnlyList<OpticsYGhostResultItem> Items { get; set; } = [];

#pragma warning disable IDE0079
#pragma warning disable CS0657

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource PlotDataSource { get; set; } = new PlotDataSource();

#pragma warning restore CS0657
#pragma warning restore IDE0079

    partial void OnItemsChanged(IReadOnlyList<OpticsYGhostResultItem>? oldValue, IReadOnlyList<OpticsYGhostResultItem> newValue)
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

    public OpticsYGhostResult()
    {
        PlotDataSource.Configure(new Rows(), 3);
        PlotDataSource.SetTitle(0, "Y Projection(Y: gray - X: pix)");
        PlotDataSource.SetTitle(1, "Y Ghost(Y: dB - X: pix)");
        PlotDataSource.SetTitle(2, "Align Y Ghost(Y: dB - X: um)");
    }

    private void RefreshPlot()
    {
        try
        {
            var yProjectionScatterLines = PlotDataSource.GetOrAddScatterLines(0, Items.Count);
            var yProjectionAlignMarkerses = PlotDataSource.GetOrAddScatterMarkerses(0, Items.Count);

            var yGhostScatterLines = PlotDataSource.GetOrAddScatterLines(1, Items.Count);
            var alignMarkerses = PlotDataSource.GetOrAddScatterMarkerses(1, Items.Count);

            var yGhostAlignScatterLines = PlotDataSource.GetOrAddScatterLines(2, Items.Count);
            var markerses = PlotDataSource.GetOrAddScatterMarkerses(2, Items.Count);

            foreach (var (index, item) in Items.Index())
            {
                if (item.YProjects.Length > 0)
                {
                    yProjectionScatterLines[index].Update(
                        $"No.{index}:{item.FindPosition:0.###}(um)",
                        item.YProjects.Select((value, i) => new Point(i, value)).ToArray(),
                        Net.Utilities.ScottPlot.Helper.Constants.Turbo.GetColor(index, new Range(0, Items.Count - 1)));

                    if (item.AlignPoint != Point.Origin)
                    {
                        var alignIndex = (int)item.AlignPoint.X;
                        if (alignIndex >= 0 && alignIndex < item.YProjects.Length)
                        {
                            yProjectionAlignMarkerses[index].Update(
                                $"No.{index} Align Point",
                                [new Point(alignIndex, item.YProjects[alignIndex])],
                                Net.Utilities.ScottPlot.Helper.Constants.Turbo.GetColor(index, new Range(0, Items.Count - 1)),
                                MarkerShape.OpenCircle);
                            yProjectionAlignMarkerses[index].MarkerSize = 20;
                        }
                    }
                }

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

public sealed partial class OpticsYGhostResultItem : ObservableObject
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
    public partial bool IsRisingEdge { get; set; }

    [ObservableProperty]
    public partial double[] YProjects { get; set; } = [];

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
        IsRisingEdge,
        YGhostResultValue,
        ResultImageh = new HtmlImage(ImageFilePath),
        RawImageFilePath
    };
}

[IOCAppService(ServiceType = typeof(OpticsYGhostWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsYGhostWindowViewModel(
    StageViewModel stageViewModel,
    OpticsViewModel opticsViewModel,
    CIBViewModel cibViewModel,
    IOptions<ApplicationSetting> options,
    ApplicationCookie applicationCookie,
    ICacheProvider cacheProvider,
    IHostEnvironment hostEnvironment,
    IDialogWindowProvider dialogWindowProvider,
    ILogger<OpticsYGhostWindowViewModel> logger,
    IApplicationCookieService applicationCookieService,
    CalibrationSetting calibrationSetting) : ViewModelBase
{
    public string Name => "Collection Y Ghost";

    public IReadOnlyList<string> Steps { get; } =
    [
        "Step 1 Alignment",
        "Step 2 Y Ghost"
    ];

    public string ImageDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", nameof(OpticsYGhostWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public ApplicationCookie ApplicationCookie => applicationCookie;

    public Guid HtmlLogUniqueId { get; private set; }

    [DefaultCache]
    [ObservableProperty]
    public partial OpticsYGhostCache Cache { get; set; } = new();

    [DefaultCache]
    [ObservableProperty]
    public partial CIBYPixelSizeDTO YPixelSize { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<OpticsYGhostResult> YGhostResults { get; set; } = [];

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = HostApplication.GetRequiredService<AlignmentUserControlViewModel>();

    [RelayCommand]
    private void Loaded()
    {
        Cache = applicationCookieService.GetOrDefault<OpticsYGhostCache>(false, CancellationToken.None);
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

            stageViewModel.SetBrightFieldAbsoluteStageXy(stageViewModel.MachineToBrightFieldPosition((alignmentResult.MarkPoint1 + (Vector)alignmentResult.MarkPoint2) / 2d), CalChipSiteModelEnum.DswModel);

            Cache.AlignmentResult = alignmentResult;

            return true;
        }).ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeAsync(1, async () =>
        {
            YPixelSize = GuardExtensions.IsNotNullAndReturn(
                applicationCookieService.GetCalibrations<CIBYPixelSizeDTO>().SingleOrDefault(t => t.ProductivityInformation == Cache.ProductivityInformation && t.PmtId == Cache.PMTId && t.IsOk),
                "Please calibrate CIB Y pixel size first!");
            YGhostResults = [];

            CIBConfiguration cibConfiguration = new()
            {
                IsAutoGainControl = true,
                IsL0K = false,
                CIBProfileMode = CIBProfileModeEnum.PMTLog,
                IsKeepRawImageCIBProfileModeEnum = false
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
                OpticsYGhostCache.Threshold,
                OpticsYGhostCache.YUmStandard,
                Cache.StartPosition,
                Cache.StopPosition,
                Cache.YStep,
                AligndBValue = OpticsYGhostCache.AligndBValue,
                AlignStepWindow = OpticsYGhostCache.AlignStepWindow,
                MinContrastDb = OpticsYGhostCache.MinContrastDb,
                CibConfiguration = new HtmlQuote(cibConfiguration.ToHtmlAnonymous()),
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());
            try
            {
                opticsViewModel.ToggleZoosClinder(Cache.OpticsIlluminationModeEnum, true);

                var cibInformations = ApplicationCookie.CIBInformations.Where(t => t.PMTId == Cache.PMTId).ToArray();
                YGhostResults =
                [
                    .. cibInformations.Select(t => new OpticsYGhostResult
                    {
                        CIBInformation = t
                    })
                ];

                var positionIndex = 0;
                foreach (var yPosition in Generate.LinearRange(Cache.StartPosition.Y, Cache.YStep, Cache.StopPosition.Y))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var findPosition = new Point(Cache.StartPosition.X, yPosition);
                    stageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(findPosition, CalChipSiteModelEnum.ChuckModel);

                    var isDevelopment = hostEnvironment.IsDevelopment();
                    if (isDevelopment) cibViewModel.UseSimulatorImages(GetSimulatorImageFilePaths(positionIndex, cibInformations));

                    IReadOnlyList<DarkFieldImageDTO> darkFieldImages;
                    try
                    {
                        darkFieldImages = await cibViewModel.GetPMTImagesAsync(
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
                            cancellationToken);
                    }
                    finally
                    {
                        if (isDevelopment) cibViewModel.UseSimulatorImages(null);
                    }

                    foreach (var (index, darkFieldImage) in darkFieldImages.Index())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var resultItem = GetResultItem(darkFieldImage.Image);
                        resultItem.FindPosition = findPosition;
                        resultItem.RawImageFilePath = darkFieldImage.RawImageFilePath;

                        YGhostResults[index].Items = [.. YGhostResults[index].Items, resultItem];
                    }

                    positionIndex++;
                }

                if (YGhostResults.SelectMany(t => t.Items).Any() == false)
                    ThrowHelper.ThrowArgumentException("Can't find the edge in the image. " +
                                                       "Please set proper start and end coordinates so the image always includes the rising or falling edge.");

                foreach (var yGhostResult in YGhostResults)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (yGhostResult.Items.Count == 0)
                        continue;

                    yGhostResult.Items =
                    [
                        .. yGhostResult.Items.Select(t =>
                        {
                            var yGhostAlignPoints = t.YGhostPoints.Select(tt => new Point((tt.X - t.AlignPoint.X) * YPixelSize.YPixelSize, tt.Y)).ToArray();
                            var resultItem = new OpticsYGhostResultItem
                            {
                                FindPosition = t.FindPosition,
                                ImageFilePath = t.ImageFilePath,
                                RawImageFilePath = t.RawImageFilePath,
                                AlignPoint = t.AlignPoint,
                                IsRisingEdge = t.IsRisingEdge,
                                YProjects = [.. t.YProjects],
                                YGhostPoints = [.. t.YGhostPoints],
                                YGhostAlignPoints = [.. yGhostAlignPoints]
                            };

                            // 对齐后覆盖不到 YUmStandard 的曲线直接失败
                            var hasYUmStandardRange = t.IsRisingEdge
                                ? yGhostAlignPoints.Any(o => o.X <= -OpticsYGhostCache.YUmStandard) && yGhostAlignPoints.Any(o => o.X > -OpticsYGhostCache.YUmStandard)
                                : yGhostAlignPoints.Any(o => o.X >= OpticsYGhostCache.YUmStandard) && yGhostAlignPoints.Any(o => o.X < OpticsYGhostCache.YUmStandard);
                            if (hasYUmStandardRange == false)
                            {
                                resultItem.IsOk = false;

                                return resultItem;
                            }

                            resultItem.YGhostMinimumPoint = t.IsRisingEdge
                                ? yGhostAlignPoints.First(o => o.X > -OpticsYGhostCache.YUmStandard)
                                : yGhostAlignPoints.Last(o => o.X < OpticsYGhostCache.YUmStandard);
                            resultItem.IsOk = resultItem.YGhostResultValue <= OpticsYGhostCache.Threshold;

                            return resultItem;
                        })
                    ];

                    logger.LogHtmlInformation($"{yGhostResult.CIBInformation} Result", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
                    {
                        Result = new HtmlQuote(yGhostResult.ToHtmlAnonymous())
                    }), HtmlLogUniqueId.LoggingHtml());
                }

                var result = YGhostResults.SelectMany(t => t.Items).All(t => t.IsOk);

                var htmlBullet = new HtmlBullet(new
                {
                    Images = new HtmlContainer([
                        new HtmlTable([
                            .. YGhostResults
                                .SelectMany(yGhostResult => yGhostResult.Items.Select(item => new
                                {
                                    yGhostResult.CIBInformation,
                                    item.FindPosition,
                                    item.RawImageFilePath
                                }))
                        ])
                    ])
                });
                if (result)
                    logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());
                else
                    logger.LogHtmlHeaderIsError(HtmlHeaderLevelEnum.Header3, htmlBullet, HtmlLogUniqueId.LoggingHtml());

                return result;
            }
            finally
            {
                opticsViewModel.ToggleZoosClinder(Cache.OpticsIlluminationModeEnum, false);
            }

            OpticsYGhostResultItem GetResultItem(BitmapImage bitmapImage)
            {
                // 1. Y 方向投影
                var yProjects = bitmapImage.GetHorizontalProjects();
                var filePath = Path.Combine(ImageDirectory, $"GUID{Guid.NewGuid()}.png");
                bitmapImage.SaveImage(filePath);

                var resultItem = new OpticsYGhostResultItem
                {
                    ImageFilePath = filePath,
                    YProjects = yProjects
                };

                // 2. 对齐点: 主台阶亮侧平台的肩部
                if (TryFindAlignEdge(yProjects, out var alignPoint, out var isRisingEdge) == false)
                    return resultItem;

                // 3. 各曲线以自己的对齐点独立归一化, 对齐点处为 0 dB
                resultItem.AlignPoint = new Point(alignPoint.X, 0);
                resultItem.IsRisingEdge = isRisingEdge;
                resultItem.YGhostPoints = [.. yProjects.Select((t, i) => new Point(i, 10d * Math.Log10(t / alignPoint.Y)))];

                return resultItem;
            }

            PlotDataSource CreateYProjectionPlot(OpticsYGhostResultItem item)
            {
                var plot = new PlotDataSource();
                plot.SetTitle("Y Projection(Y: gray - X: pix)");
                plot.GetOrAddScatterLines(1)[0].Update(
                    "Y Projection",
                    item.YProjects.Select((value, i) => new Point(i, value)).ToArray());

                return plot;
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
            logger.LogError(ex, "{Name}: Failed to save cache", Name);
        }

        CloseView(true);
    }

    // 按 日期_索引_*_*_*_*_PMTID-CHchannelID_* 匹配模拟图
    private static string[] GetSimulatorImageFilePaths(int positionIndex, IReadOnlyList<CIBInformation> cibInformations)
    {
        var directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\YGhost");

        return
        [
            .. cibInformations.Select(cibInformation =>
            {
                var pmtChannelToken = $"PMT{cibInformation.PMTId:D2}-CH{cibInformation.ChannelId}";
                string[] files =
                [
                    .. Directory.GetFiles(directoryPath, "*.raw", SearchOption.AllDirectories)
                        .Select(filePath =>
                        {
                            var fileName = Path.GetFileNameWithoutExtension(filePath);

                            if (fileName.Contains(pmtChannelToken) == false)
                                return (FilePath: filePath, Index: null);

                            var parts = fileName.Split('_');

                            if (parts.Length < 2 || int.TryParse(parts[1], out var value) == false)
                                return (FilePath: filePath, Index: null);

                            return (FilePath: filePath, Index: (int?)value);
                        })
                        .Where(t => t.Index is not null)
                        .OrderBy(t => t.Index)
                        .Select(t => t.FilePath)
                ];

                if (positionIndex >= files.Length) throw new FileNotFoundException($"{pmtChannelToken}[{positionIndex}]");

                return files[positionIndex];
            })
        ];
    }

    /// <summary>
    /// 在线性 Y 投影上定位主光斑台阶, 并在亮侧平台肩部取对齐点.
    /// 多个台阶时取对比度最大、且亮侧接近整条投影峰值的一个, 避免锁到曲线起始处的弱上升沿.
    /// 肩部是平滑后靠近台阶的第一个局部高点; 再往亮侧高出对齐 dB 值时, 视为沿前鼓包并继续向平台找.
    /// </summary>
    private static bool TryFindAlignEdge(
        double[] yProjects,
        out Point alignPoint,
        out bool isRisingEdge)
    {
        alignPoint = Point.Origin;
        isRisingEdge = false;

        var alignDbValue = OpticsYGhostCache.AligndBValue;
        var stepWindow = OpticsYGhostCache.AlignStepWindow;
        var minContrastDb = OpticsYGhostCache.MinContrastDb;

        var length = yProjects.Length;

        if (stepWindow < 1 || length < stepWindow * 2 + 1)
            return false;

        var maxProject = 0d;
        for (var i = 0; i < length; i++)
        {
            if (yProjects[i] > maxProject)
                maxProject = yProjects[i];
        }

        if (maxProject <= 0d)
            return false;

        const double spotLevelDb = 8d;
        var spotFloor = maxProject * Math.Pow(10d, -spotLevelDb / 10d);
        var separation = stepWindow * 2;
        var stepAt = new double[length];
        var contrastAt = new double[length];
        var highAt = new double[length];
        var valid = new bool[length];

        for (var i = stepWindow; i <= length - stepWindow; i++)
        {
            var left = 0d;
            var right = 0d;
            for (var k = 0; k < stepWindow; k++)
            {
                left += yProjects[i - stepWindow + k];
                right += yProjects[i + k];
            }

            left /= stepWindow;
            right /= stepWindow;
            var high = Math.Max(left, right);
            var low = Math.Min(left, right);

            if (high <= 0d)
                continue;

            var safeLow = low <= 0d ? double.Epsilon : low;
            stepAt[i] = right - left;
            highAt[i] = high;
            contrastAt[i] = 10d * Math.Log10(high / safeLow);
            valid[i] = true;
        }

        var suppressed = new bool[length];
        var bestContrast = 0d;
        var bestAbs = 0d;
        var bestIndex = -1;
        var bestStep = 0d;
        while (true)
        {
            var peakIndex = -1;
            var peakAbs = 0d;
            for (var i = stepWindow; i <= length - stepWindow; i++)
            {
                if (valid[i] == false || suppressed[i])
                    continue;

                var abs = Math.Abs(stepAt[i]);
                if (abs > peakAbs)
                {
                    peakAbs = abs;
                    peakIndex = i;
                }
            }

            if (peakIndex < 0 || peakAbs <= 0d)
                break;

            if (contrastAt[peakIndex] >= minContrastDb && highAt[peakIndex] >= spotFloor)
            {
                var contrast = contrastAt[peakIndex];
                if (contrast > bestContrast || (contrast == bestContrast && peakAbs > bestAbs))
                {
                    bestContrast = contrast;
                    bestAbs = peakAbs;
                    bestIndex = peakIndex;
                    bestStep = stepAt[peakIndex];
                }
            }

            var suppressStart = Math.Max(stepWindow, peakIndex - separation);
            var suppressEnd = Math.Min(length - stepWindow, peakIndex + separation);
            for (var i = suppressStart; i <= suppressEnd; i++)
                suppressed[i] = true;
        }

        if (bestIndex < 0)
            return false;

        isRisingEdge = bestStep > 0d;

        var half = Math.Max(1, stepWindow / 2);
        var smoothed = new double[length];
        for (var i = 0; i < length; i++)
        {
            var smoothStart = Math.Max(0, i - half);
            var smoothEnd = Math.Min(length - 1, i + half);
            var sum = 0d;
            for (var k = smoothStart; k <= smoothEnd; k++)
                sum += yProjects[k];

            smoothed[i] = sum / (smoothEnd - smoothStart + 1);
        }

        var lipRatio = Math.Pow(10d, -Math.Abs(alignDbValue) / 10d);
        const int shoulderMargin = 5;
        var maxDistance = stepWindow * 9;
        var lipLook = stepWindow * 4;
        var direction = isRisingEdge ? 1 : -1;
        var alignIndex = -1;
        var searchStart = bestIndex + (direction * shoulderMargin);
        var searchStop = bestIndex + (direction * maxDistance);

        for (var i = searchStart; direction > 0 ? i < length && i <= searchStop : i >= 0 && i >= searchStop; i += direction)
        {
            var neighborStart = Math.Max(0, i - half);
            var neighborEnd = Math.Min(length - 1, i + half);
            var isLocalMax = true;
            for (var k = neighborStart; k <= neighborEnd; k++)
            {
                if (smoothed[k] > smoothed[i])
                {
                    isLocalMax = false;
                    break;
                }
            }

            if (isLocalMax == false)
                continue;

            var edgeSide = i - (direction * half);
            if (edgeSide < 0)
                edgeSide = 0;
            if (edgeSide >= length)
                edgeSide = length - 1;

            if (smoothed[i] <= smoothed[edgeSide])
                continue;

            var furtherStart = i + direction;
            var furtherEnd = i + (direction * lipLook);
            if (furtherStart > furtherEnd)
            {
                var swap = furtherStart;
                furtherStart = furtherEnd;
                furtherEnd = swap;
            }

            if (furtherStart < 0)
                furtherStart = 0;
            if (furtherEnd >= length)
                furtherEnd = length - 1;

            if (furtherStart <= furtherEnd)
            {
                var furtherMax = smoothed[furtherStart];
                for (var k = furtherStart + 1; k <= furtherEnd; k++)
                {
                    if (smoothed[k] > furtherMax)
                        furtherMax = smoothed[k];
                }

                if (smoothed[i] < furtherMax * lipRatio)
                    continue;
            }

            alignIndex = i;
            break;
        }

        if (alignIndex < 0)
            return false;

        var alignY = yProjects[alignIndex];

        if (alignY <= 0d)
            return false;

        alignPoint = new Point(alignIndex, alignY);

        return true;
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