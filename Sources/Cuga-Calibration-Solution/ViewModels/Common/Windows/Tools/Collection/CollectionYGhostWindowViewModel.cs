using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.CIB.YPixelSize;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Bases;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
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
    public partial int PMTId { get; set; } = 8;

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial Point StartPosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial double YRange { get; set; }

    [ObservableProperty]
    public partial double YStep { get; set; } = 23;

    [ObservableProperty]
    public partial double AligndBValue { get; set; } = -1;

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
    public partial IScatterPlotControl ScatterPlotControl { get; set; } = HostApplication.GetRequiredService<IScatterPlotControl>();

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
        ScatterPlotControl.Configure(new Rows(), 2);
        ScatterPlotControl.SetTitle(0, "Y Ghost(Y: dB - X: um)");
        ScatterPlotControl.SetTitle(1, "Align Y Ghost(Y: dB - X: um)");
    }

    private void RefreshPlot()
    {
        try
        {
            var yGhostScatterLines = ScatterPlotControl.GetOrAddScatterLines(0, Items.Count);
            var alignMarkerses = ScatterPlotControl.GetOrAddScatterMarkerses(0, Items.Count);

            var yGhostAlignScatterLines = ScatterPlotControl.GetOrAddScatterLines(1, Items.Count);
            var markerses = ScatterPlotControl.GetOrAddScatterMarkerses(1, Items.Count);

            foreach (var (index, item) in Items.Index())
            {
                if (item.YGhostPoints.Length <= 0) continue;

                yGhostScatterLines[index].Update(
                    $"No.{index}:{item.FindPosition:0.###}(um)",
                    item.YGhostPoints,
                    Net.Utilities.ScottPlot.WPF.Helper.Constants.Turbo.GetColor(index, new Range(0, Items.Count - 1)));

                if (item.AlignPoint != Point.Origin)
                {
                    alignMarkerses[index].Update(
                        $"No.{index} Align Point",
                        [item.AlignPoint],
                        Net.Utilities.ScottPlot.WPF.Helper.Constants.Turbo.GetColor(index, new Range(0, Items.Count - 1)),
                        MarkerShape.OpenCircle);
                    alignMarkerses[index].MarkerSize = 20;
                }

                if (item.YGhostAlignPoints.Length <= 0) continue;

                yGhostAlignScatterLines[index].Update(
                    $"No.{index}:{item.FindPosition:0.###}(um)",
                    item.YGhostAlignPoints,
                    Net.Utilities.ScottPlot.WPF.Helper.Constants.Turbo.GetColor(index, new Range(0, Items.Count - 1)));

                markerses[index].Update(
                    $"No.{index} minimum(db),Result {(item.IsOk ? "OK" : "Failed")}:{item.YGhostResultValue:0.######}",
                    [item.YGhostMinimumPoint],
                    Net.Utilities.ScottPlot.WPF.Helper.Constants.Turbo.GetColor(index, new Range(0, Items.Count - 1)),
                    MarkerShape.OpenCircle);
                markerses[index].MarkerSize = 20;
            }
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }
    }

    public object ToHtmlAnonymous() => new
    {
        CIBInformation,
        Details = new HtmlTable([.. Items.Select(t => t.ToHtmlAnonymous())]),
        ScatterPlotControl = new HtmlContainer([.. ScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
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
    public partial Point AlignPoint { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial Point[] YGhostPoints { get; set; } = [];

    [ObservableProperty]
    public partial Point[] YGhostAlignPoints { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(YGhostResultValue))]
    public partial Point YGhostMinimumPoint { get; set; } = Point.Origin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(YGhostResultValue))]
    public partial Point YGhostMaximumPoint { get; set; } = Point.Origin;

    public double YGhostResultValue => YGhostMinimumPoint.Y / YGhostMaximumPoint.Y;

    public object ToHtmlAnonymous() => new
    {
        FindPosition,
        IsOk,
        MinimumdBValue = YGhostMinimumPoint.Y,
        MaximumdBValue = YGhostMaximumPoint.Y,
        YGhostResultValue,
        ResultImageh = new HtmlImage(ImageFilePath)
    };
}

[IOCAppService(ServiceType = typeof(CollectionYGhostWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CollectionYGhostWindowViewModel(
    StageViewModel stageViewModel,
    OpticsViewModel opticsViewModel,
    CIBViewModel cibViewModel,
    IOptions<ApplicationSetting> options,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ApplicationCookie applicationCookie,
    ICacheProvider cacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    ILogger<CollectionYGhostWindowViewModel> logger,
    IApplicationCookieService applicationCookieService) : ViewModelBase
{
    public string Name => "Collection Y Ghost";

    public string ImageDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", nameof(CollectionYGhostWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public ApplicationCookie ApplicationCookie => applicationCookie;

    public Guid HtmlLogUniqueId { get; private set; }

    [DefaultCache]
    [ObservableProperty]
    public partial CollectionYGhostCache Cache { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<CollectionYGhostResult> YGhostResults { get; set; } = [];

    [RelayCommand]
    private void Loaded()
    {
        Cache = cacheProvider.GetOrDefault<CollectionYGhostCache>();
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            YGhostResults = [];

            CIBConfiguration cibConfiguration = new()
            {
                IsAutoGainControl = true,
                IsL0K = false,
                CIBProfileMode = CIBProfileModeEnum.PMTLog
            };
            var yPixelSize = GuardExtensions.IsNotNullAndReturn(applicationCookieService.GetCalibrations<CIBYPixelSizeDTO>(cancellationToken)
                .SingleOrDefault(t => t.ProductivityInformation == Cache.ProductivityInformation && t.PmtId == Cache.PMTId && t.IsOk), "Please calibrate CIB Y pixel size first!");

            logger.LogHtmlInformation("Diagnosis Param", HtmlHeaderLevelEnum.Header3, new HtmlBullet(new
            {
                Cache.OpticsIlluminationModeEnum,
                Cache.ProductivityInformation,
                Cache.CalChipSiteModelEnum,
                Cache.PMTId,
                Cache.ImageWidth,
                Cache.LaserLightInformation,
                Cache.Threshold,
                Cache.StartPosition,
                YStop = Cache.YRange,
                Cache.YStep,
                MaximumdBValue = Cache.AligndBValue,
                yPixelSize,
                CibConfiguration = new HtmlQuote(cibConfiguration.ToHtmlAnonymous()),
                OpticsConfiguration = new HtmlQuote(Cache.OpticsConfiguration.ToHtmlAnonymous())
            }), HtmlLogUniqueId.LoggingHtml());
            await InvokeAsync(async () =>
            {
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
                    foreach (var yPosition in Generate.LinearRange(Cache.StartPosition.Y, Cache.YStep, Cache.StartPosition.Y + Cache.YRange))
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
                            cancellationToken);

                        foreach (var (index, darkFieldImageDTO) in darkFieldImages.Select((t, i) => (i, t)))
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            // //mock
                            // var mockImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"Assets\Data\YGhost\{positionIndex}.raw");
                            // var bytes = System.IO.File.ReadAllBytes(mockImagePath);
                            // var (size, _, _) = RAWImageFactory.GetSize(bytes);
                            // var darkFieldImageDTO = new DarkFieldImageDTO().AdaptIn(new DarkFieldRawScanImageDTO
                            // {
                            //     CIBInformation = dto.CIBInformation,
                            //     Size = size,
                            //     IsForward = dto.IsForward,
                            //     RawImageCIBProfileModeEnum = CIBProfileModeEnum.PMTLog,
                            //     RawImageFilePath = mockImagePath,
                            //     IsKeepRawImageCIBProfileModeEnum = true
                            // });

                            var resultItem = GetResultItem(darkFieldImageDTO);
                            resultItem.FindPosition = findPosition;
                            YGhostResults[index].Items = [.. YGhostResults[index].Items, resultItem];
                        }

                        positionIndex++;
                    }

                    foreach (var yGhostResult in YGhostResults)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // 对齐
                        var alignResultItem = yGhostResult.Items.OrderByDescending(t => t.AlignPoint.X).First();

                        yGhostResult.Items =
                        [
                            .. yGhostResult.Items.Select(t =>
                            {
                                var alignOffsetX = alignResultItem.AlignPoint.X - t.AlignPoint.X;
                                var yGhostAlignPoints = t.YGhostPoints.Select(tt => new Point(tt.X + alignOffsetX, tt.Y)).ToArray();
                                var yGhostMinimumPoint = yGhostAlignPoints.OrderBy(tt => tt.Y).First();
                                var yGhostMaximumPoint = yGhostAlignPoints.OrderByDescending(tt => tt.Y).First();
                                var yGhostResultValue = yGhostMinimumPoint.Y / Cache.AligndBValue;

                                return new CollectionYGhostResultItem
                                {
                                    FindPosition = t.FindPosition,
                                    ImageFilePath = t.ImageFilePath,
                                    AlignPoint = t.AlignPoint,
                                    YGhostPoints = [.. t.YGhostPoints],
                                    YGhostAlignPoints = [.. yGhostAlignPoints],
                                    YGhostMinimumPoint = yGhostMinimumPoint,
                                    YGhostMaximumPoint = yGhostMaximumPoint,
                                    IsOk = yGhostResultValue <= Cache.Threshold
                                };
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

                CollectionYGhostResultItem GetResultItem(DarkFieldImageDTO darkFieldImageDTO)
                {
                    var filePath = Path.Combine(ImageDirectory, $"{darkFieldImageDTO.CIBInformation}_GUID{Guid.NewGuid()}.png");
                    using var rotateImage = darkFieldImageDTO.Image.RotateCounterClockwise90Degree();
                    using var mirrorImage = darkFieldImageDTO.Image.VerticalFlip();
                    darkFieldImageDTO.Image.SaveImage(filePath);

                    var yGhostValues = calibrationAlgorithmService.GetImageGrayYProjectionsPixels(darkFieldImageDTO.Image);
                    var yGhostPoints = yGhostValues.Select((t, i) => new Point(i, t)).ToArray();
                    var alignPoint = yGhostPoints.Select(t => t).OrderBy(t => Math.Abs(t.Y - Cache.AligndBValue)).First();

                    return new CollectionYGhostResultItem
                    {
                        ImageFilePath = filePath,
                        AlignPoint = alignPoint,
                        YGhostPoints = [.. yGhostPoints]
                    };
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: Failed to action", Name);
            dialogWindowProvider.ShowDialog($"{Name} action error:{ex}", DialogButtonsEnum.OK, DialogIconEnum.Error);
        }
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

    private async Task InvokeAsync(Func<Task<bool>> func)
    {
        await Task.Run(async () =>
        {
            HtmlLogUniqueId = Guid.NewGuid();

            logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                isSuccess = await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
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
                logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{(isSuccess ? "OK" : "Failed")}"));
            }

            if (isSuccess)
            {
                dialogWindowProvider.ShowDialog($"{Name}: Success");
            }
            else
                dialogWindowProvider.ShowDialog($"{Name}: Error", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return isSuccess;
        }).ConfigureAwait(false);
    }
}