using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Setting;
using Core.Utilities;
using Core.Utilities.SourceGenerators.Attributes;
using Local.SQL.Cache.Providers.Bases;
using Local.SQL.Cache.Providers.Extensions;
using Local.SQL.Cache.Providers.Interfaces;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using System.Collections.Immutable;
using System.IO;
using Generate = MathNet.Numerics.Generate;
using Range = ScottPlot.Range;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Optics;

public sealed partial class OpticsCollectorSlitCache : ObservableCacheBase
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private int _pmtId;

    [ObservableProperty]
    private int _imageWidthPixel;

    [ObservableProperty]
    private double _rangeEcs;

    [ObservableProperty]
    private double _stepEcs;

    #region Haze

    [ObservableProperty]
    private CIBConfiguration _hazeCIBConfiguration = new();

    [ObservableProperty]
    private LaserLightInformation _hazeLaserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private Point _hazeBrightFieldPosition;

    [ObservableProperty]
    private double _hazeAverageECS;

    [ObservableProperty]
    private IReadOnlyList<HazeResult> _hazeResults = [];

    #endregion Haze

    #region DSW

    [ObservableProperty]
    private CIBConfiguration _dSWCIBConfiguration = new();

    [ObservableProperty]
    private LaserLightInformation _dSWLaserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private Point _dSWBrightFieldPosition;

    [ObservableProperty]
    private Rect _dSWROIRect = new(0, 0, 256, 256);

    [ObservableProperty]
    private double _dSWXPixelSize;

    [ObservableProperty]
    private double _dSWYPixelSize;

    [ObservableProperty]
    private double _dSWPotDiameter;

    [ObservableProperty]
    private double _dSWXPointDiameter;

    [ObservableProperty]
    private double _dSWYPointDiameter;

    [ObservableProperty]
    private double _dSWAverageECS;

    [ObservableProperty]
    private IReadOnlyList<DSWResult> _dSWResults = [];

    #endregion DSW

    public IDictionary<int, IDictionary<string, IReadOnlyList<Point>>> GetPoints(string resultPropertyName, params string[] resultItemPropertyNames)
    {
        const string resultECSPropertyName = nameof(HazeResult.ECS);
        const string resultItemsPropertyName = nameof(HazeResult.Items);
        const string resultItemImageChannelIdPropertyName = nameof(HazeResultItem.ChannelId);

        var dictionary = new Dictionary<int, IDictionary<string, IReadOnlyList<Point>>>();

        var count = 0;
        foreach (var result in ObjectHelper.GetPropertyValue<System.Collections.IEnumerable>(this, resultPropertyName))
        {
            foreach (var resultItem in ObjectHelper.GetPropertyValue<System.Collections.IEnumerable>(result, resultItemsPropertyName))
            {
                var temp = dictionary.GetOrAdd(
                    ObjectHelper.GetPropertyValue<int>(resultItem, resultItemImageChannelIdPropertyName),
                    resultItemPropertyNames.ToDictionary<string, string, IReadOnlyList<Point>>(t => t, _ => new List<Point>()));

                foreach (var resultItemPropertyName in resultItemPropertyNames)
                {
                    Guard.IsAssignableToTypeAndReturn<List<Point>>(temp[resultItemPropertyName]).Add(new Point(
                        ObjectHelper.GetPropertyValue<double>(result, resultECSPropertyName),
                        ObjectHelper.GetPropertyValue<double>(resultItem, resultItemPropertyName)));
                }
            }

            count++;
        }

        var counts = dictionary.SelectMany(t => t.Value.Values.Select(tt => tt.Count)).ToArray();
        if (count > 0)
        {
            Guard.IsEqualTo(counts[0], count);
            Guard.IsEqualTo(counts.Distinct().Count(), 1);
        }
        else Guard.IsEqualTo(dictionary.Count, 0);

        return dictionary;
    }

    public object ToHtmlAnonymous() => new
    {
        ProductivityInformation,
        PmtId,
        ImageWidth = ImageWidthPixel,
        RangeEcs,
        StepEcs,
        HazeCIBConfiguration = new HtmlQuote(HazeCIBConfiguration.ToHtmlAnonymous()),
        HazeLaserLightInformation,
        HazeBrightFieldPosition,
        DSWCIBConfiguration = new HtmlQuote(DSWCIBConfiguration.ToHtmlAnonymous()),
        DSWLaserLightInformation,
        DSWBrightFieldPosition
    };
}

public sealed partial class HazeResult : ObservableObject
{
    [ObservableProperty]
    private double _eCS;

    [ObservableProperty]
    private IReadOnlyList<HazeResultItem> _items = [];
}

public sealed partial class HazeResultItem : ObservableObject
{
    [ObservableProperty]
    private int _channelId;

    [ObservableProperty]
    private string _imageFilePath = string.Empty;

    [ObservableProperty]
    private double _beginningAverageGray;

    [ObservableProperty]
    private double _beginningAverageGrayNormalization;

    [ObservableProperty]
    private double _middleAverageGray;

    [ObservableProperty]
    private double _middleAverageGrayNormalization;

    [ObservableProperty]
    private double _endAverageGray;

    [ObservableProperty]
    private double _endAverageGrayNormalization;

    [ObservableProperty]
    private double _standardDeviation;

    [ObservableProperty]
    private double _standardDeviationNormalization;

    public object ToHtmlAnonymous() => new
    {
        BeginningAverageGray,
        MiddleAverageGray,
        EndAverageGray,
        StandardDeviation
    };
}

public sealed partial class DSWResult : ObservableObject
{
    [ObservableProperty]
    private double _eCS;

    [ObservableProperty]
    private IReadOnlyList<DSWResultItem> _items = [];
}

public sealed partial class DSWResultItem : ObservableObject
{
    [ObservableProperty]
    private int _channelId;

    [ObservableProperty]
    private string _imageFilePath = string.Empty;

    [ObservableProperty]
    private double _strehlRatioX;

    [ObservableProperty]
    private double _strehlRatioY;

    [ObservableProperty]
    private double _strehlRatioXNormalization;

    [ObservableProperty]
    private double _strehlRatioYNormalization;

    public object ToHtmlAnonymous() => new
    {
        StrehlRatioX,
        StrehlRatioY
    };
}

[IOCAppService(ServiceType = typeof(OpticsCollectorSlitWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class OpticsCollectorSlitWindowViewModel(
    IServiceProvider serviceProvider,
    StageViewModel stageViewModel,
    AfViewModel afViewModel,
    CIBViewModel cibViewModel,
    IOptions<ApplicationSetting> options,
    CreateRoiWindowViewModel createRoiWindowViewModel,
    ApplicationCookie applicationCookie,
    CalibrationSetting calibrationSetting,
    ICacheProvider cacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    ILogger<OpticsCollectorSlitWindowViewModel> logger) : ViewModelBase
{
    private const string DSW = nameof(DSW);
    private const string Haze = nameof(Haze);

    public string Name => "Collection Focus Align Optics Focus";

    public string ImageDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", nameof(OpticsCollectorSlitWindowViewModel), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public ApplicationCookie ApplicationCookie => applicationCookie;

    public Guid HtmlLogUniqueId { get; private set; }

    [DefaultCache]
    [ObservableProperty]
    private OpticsCollectorSlitCache _cache = new();

    [ObservableProperty]
    private IDictionary<int, IScatterPlotControl> _scatterPlotControls = ImmutableDictionary<int, IScatterPlotControl>.Empty;

    [RelayCommand]
    private void Loaded()
    {
        try
        {
            Cache = cacheProvider.GetOrDefault<OpticsCollectorSlitCache>();

            if (ScatterPlotControls.Count > 0) return;

            ScatterPlotControls = ApplicationCookie.CIBInformationChannelIds.ToDictionary(channelId => channelId, _ => GetScatterPlotControl());
        }
        finally
        {
            RefreshPlot();
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step0Async(CancellationToken cancellationToken)
    {
        await InvokeAsync(
            "Step1 Haze",
            CalChipSiteModelEnum.HazeModel,
            () => Task.CompletedTask,
            (item, darkFieldImageDto) =>
            {
                var hazeResultItem = Guard.IsAssignableToTypeAndReturn<HazeResultItem>(item);

                var matrix = Matrix<double>.Build.DenseOfArray(darkFieldImageDto.Image.RAW16BitsPerPixelToMatrix());

                var baseSize = matrix.RowCount / 3;
                var remainder = matrix.RowCount % 3;

                var rows1 = baseSize + (remainder > 0 ? 1 : 0);
                var rows2 = baseSize + (remainder > 1 ? 1 : 0);
                var rows3 = baseSize;

                var beginningSubMatrix = matrix.SubMatrix(0, rows1, 0, matrix.ColumnCount);
                var middleSubMatrix = matrix.SubMatrix(0 + rows1, rows2, 0, matrix.ColumnCount);
                var endSubMatrix = matrix.SubMatrix(0 + rows1 + rows2, rows3, 0, matrix.ColumnCount);

                hazeResultItem.BeginningAverageGray = beginningSubMatrix.Enumerate().Average();
                hazeResultItem.MiddleAverageGray = middleSubMatrix.Enumerate().Average();
                hazeResultItem.EndAverageGray = endSubMatrix.Enumerate().Average();
                hazeResultItem.StandardDeviation = matrix.Enumerate().StandardDeviation();

                logger.LogHtmlInformation($"Channel Id: {hazeResultItem.ChannelId}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                {
                    Image = new HtmlImage(hazeResultItem.ImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(Cache.DSWROIRect)]),
                    darkFieldImageDto.RawImageFilePath,
                    Result = new HtmlQuote(hazeResultItem.ToHtmlAnonymous())
                }), HtmlLogUniqueId.LoggingHtml());

                return Task.CompletedTask;
            },
            () =>
            {
                for (var i = 0; i < Cache.HazeResults[0].Items.Count; i++)
                {
                    var maxBeginningAverageGray = Cache.HazeResults.Max(t => t.Items[i].BeginningAverageGray);
                    var maxMiddleAverageGray = Cache.HazeResults.Max(t => t.Items[i].MiddleAverageGray);
                    var maxEndAverageGray = Cache.HazeResults.Max(t => t.Items[i].EndAverageGray);
                    var maxStandardDeviation = Cache.HazeResults.Max(t => t.Items[i].StandardDeviation);

                    foreach (var cacheHazeResult in Cache.HazeResults)
                    {
                        cacheHazeResult.Items[i].BeginningAverageGrayNormalization = cacheHazeResult.Items[i].BeginningAverageGray / maxBeginningAverageGray;
                        cacheHazeResult.Items[i].MiddleAverageGrayNormalization = cacheHazeResult.Items[i].MiddleAverageGray / maxMiddleAverageGray;
                        cacheHazeResult.Items[i].EndAverageGrayNormalization = cacheHazeResult.Items[i].EndAverageGray / maxEndAverageGray;
                        cacheHazeResult.Items[i].StandardDeviationNormalization = cacheHazeResult.Items[i].StandardDeviation / maxStandardDeviation;
                    }
                }

                RefreshPlot();

                foreach (var keyValuePair in ScatterPlotControls)
                {
                    var scatterPlotControl = ScatterPlotControls[keyValuePair.Key];

                    logger.LogHtmlInformation($"Channel Id: {keyValuePair.Key} OK", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                    logger.LogHtmlInformation("Origin", HtmlHeaderLevelEnum.Header4, new HtmlContainer([.. scatterPlotControl.GetFlatMapHtmlPlot2DLinesCharts(0)]), HtmlLogUniqueId.LoggingHtml());
                    logger.LogHtmlInformation("Normalization", HtmlHeaderLevelEnum.Header4, scatterPlotControl.GetHtmlPlot2DLinesChart(2), HtmlLogUniqueId.LoggingHtml());
                }

                return Task.CompletedTask;
            }, cancellationToken);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeAsync(
            "Step1 DSW",
            CalChipSiteModelEnum.DswModel,
            async () =>
            {
                var darkFieldImageDto = await cibViewModel.GetPMTImageAsync(
                    Cache.ProductivityInformation,
                    StageCoordinateSystemEnum.Bright,
                    Cache.DSWBrightFieldPosition,
                    Cache.ImageWidthPixel,
                    ApplicationCookie.CIBInformations.Single(t => t.PMTId == Cache.PmtId && t.ChannelId == calibrationSetting.SettingCommonParam.MainCIBInformation.ChannelId),
                    (false, CalChipSiteModelEnum.DswModel),
                    (false, Cache.DSWCIBConfiguration),
                    (false, Cache.DSWLaserLightInformation),
                    false,
                    cancellationToken);
                using var _ = darkFieldImageDto;

                var filePath = Path.Combine(ImageDirectory, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                darkFieldImageDto.Image.Save(filePath);
                createRoiWindowViewModel.ImageFilePath = filePath;

                if (windowManagerService.ShowDialog(createRoiWindowViewModel) == false) ThrowHelper.ThrowOperationCanceledException<bool>("Generate ROI");

                Cache.DSWROIRect = createRoiWindowViewModel.Rect;

                logger.LogHtmlInformation("Create ROI", HtmlHeaderLevelEnum.Header4, new HtmlBullet(new
                {
                    Cache.DSWROIRect,
                    Cache.DSWXPixelSize,
                    Cache.DSWYPixelSize,
                    Cache.DSWPotDiameter,
                    Cache.DSWXPointDiameter,
                    Cache.DSWYPointDiameter,
                    Image = new HtmlImage(filePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(Cache.DSWROIRect)])
                }), HtmlLogUniqueId.LoggingHtml());
            },
            (item, darkFieldImageDto) =>
            {
                var dswResultItem = Guard.IsAssignableToTypeAndReturn<DSWResultItem>(item);

                var ((strehlRatioX, xLine, xFitLine), (strehlRatioY, yLine, yFitLine)) =
                    StrehlRatioUtility.GetStrehlRatio(darkFieldImageDto.Image.RAW16BitsPerPixelToMatrix(), Cache.DSWROIRect, Cache.DSWXPixelSize, Cache.DSWYPixelSize, Cache.DSWPotDiameter, Cache.DSWXPointDiameter, Cache.DSWYPointDiameter);
                dswResultItem.StrehlRatioX = strehlRatioX;
                dswResultItem.StrehlRatioY = strehlRatioY;

                logger.LogHtmlInformation($"Channel Id: {dswResultItem.ChannelId}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                {
                    Image = new HtmlImage(dswResultItem.ImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(Cache.DSWROIRect)]),
                    darkFieldImageDto.RawImageFilePath,
                    xFitLine = new HtmlPlot2DLinesChart([(nameof(xFitLine), xFitLine.ToPoints()), (nameof(xLine), xLine.ToPoints())], string.Empty),
                    yFitLine = new HtmlPlot2DLinesChart([(nameof(yFitLine), yFitLine.ToPoints()), (nameof(yLine), yLine.ToPoints())], string.Empty),
                    Result = new HtmlQuote(dswResultItem.ToHtmlAnonymous())
                }), HtmlLogUniqueId.LoggingHtml());

                return Task.CompletedTask;
            },
            () =>
            {
                for (var i = 0; i < Cache.DSWResults[0].Items.Count; i++)
                {
                    var maxStrehlRatioX = Cache.DSWResults.Max(t => t.Items[i].StrehlRatioX);
                    var maxStrehlRatioY = Cache.DSWResults.Max(t => t.Items[i].StrehlRatioY);

                    foreach (var cacheDSWResult in Cache.DSWResults)
                    {
                        cacheDSWResult.Items[i].StrehlRatioXNormalization = cacheDSWResult.Items[i].StrehlRatioX / maxStrehlRatioX;
                        cacheDSWResult.Items[i].StrehlRatioYNormalization = cacheDSWResult.Items[i].StrehlRatioY / maxStrehlRatioY;
                    }
                }

                RefreshPlot();

                foreach (var keyValuePair in ScatterPlotControls)
                {
                    var scatterPlotControl = ScatterPlotControls[keyValuePair.Key];

                    logger.LogHtmlInformation($"Channel Id: {keyValuePair.Key} OK", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
                    logger.LogHtmlInformation("Origin", HtmlHeaderLevelEnum.Header4, new HtmlContainer([
                        ..scatterPlotControl.GetFlatMapHtmlPlot2DLinesCharts(0),
                        ..scatterPlotControl.GetFlatMapHtmlPlot2DLinesCharts(1)
                    ]), HtmlLogUniqueId.LoggingHtml());
                    logger.LogHtmlInformation("Normalization", HtmlHeaderLevelEnum.Header4, scatterPlotControl.GetHtmlPlot2DLinesChart(2), HtmlLogUniqueId.LoggingHtml());
                }

                return Task.CompletedTask;
            }, cancellationToken);
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
            logger.LogError(ex, "Failed to save cache");
        }

        CloseView(null);
    }

    private async Task InvokeAsync(
        string stepName,
        CalChipSiteModelEnum calChipSiteModelEnum,
        Func<Task> beforeAction,
        Func<object, DarkFieldImageDTO, Task> resultItemAction,
        Func<Task> finallyAction,
        CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            HtmlLogUniqueId = Guid.NewGuid();

            logger.LogHtmlInformation(Name, HtmlHeaderLevelEnum.Header1, HtmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation(stepName, HtmlHeaderLevelEnum.Header2, HtmlLogUniqueId.LoggingHtml());
            logger.LogHtmlInformation("Param", HtmlHeaderLevelEnum.Header3, new HtmlQuote(Cache.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            var isSuccess = false;
            try
            {
                await beforeAction.Invoke();

                var name = calChipSiteModelEnum switch
                {
                    CalChipSiteModelEnum.DswModel => DSW,
                    CalChipSiteModelEnum.HazeModel => Haze,
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(calChipSiteModelEnum))
                };

                var cibConfiguration = ObjectHelper.GetPropertyValue<CIBConfiguration>(Cache, nameof(Cache.HazeCIBConfiguration).Replace(Haze, name));
                var laserLightInformation = ObjectHelper.GetPropertyValue<LaserLightInformation>(Cache, nameof(Cache.HazeLaserLightInformation).Replace(Haze, name));
                var brightFieldPosition = ObjectHelper.GetPropertyValue<Point>(Cache, nameof(Cache.HazeBrightFieldPosition).Replace(Haze, name));

                var resultType = Guard.IsNotNullAndReturn(Type.GetType(typeof(HazeResult).GetAssemblyQualifiedName().Replace(Haze, name)));
                var resultItemType = Guard.IsNotNullAndReturn(Type.GetType(typeof(HazeResultItem).GetAssemblyQualifiedName().Replace(Haze, name)));

                var cacheAverageECSPropertyName = nameof(Cache.HazeAverageECS).Replace(Haze, name);
                var cacheResultsPropertyName = nameof(Cache.HazeResults).Replace(Haze, name);

                const string resultECSPropertyName = nameof(HazeResult.ECS);
                const string resultItemsPropertyName = nameof(HazeResult.Items);
                const string resultItemImageChannelIdPropertyName = nameof(HazeResultItem.ChannelId);
                const string resultItemImageFilePathPropertyName = nameof(HazeResultItem.ImageFilePath);

                ObjectHelper.SetPropertyValue(Cache, cacheAverageECSPropertyName, 0d);
                ObjectHelper.SetPropertyValue(Cache, cacheResultsPropertyName, Array.CreateInstance(resultType, 0));
                RefreshPlot();

                stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(stageViewModel.BrightFieldToMachinePosition(brightFieldPosition));
                afViewModel.ToggleDarkFieldEnable(true);

                await Task.Delay(TimeSpan.FromMilliseconds(1000), cancellationToken);

                var sensorAverageEcsValue = afViewModel.GetSensorAverageEcsValue();
                ObjectHelper.SetPropertyValue(Cache, cacheAverageECSPropertyName, sensorAverageEcsValue);

                afViewModel.ToggleBrightFieldEnable(false);

                var ecss = Generate.LinearRange(sensorAverageEcsValue - Cache.RangeEcs, Cache.StepEcs, sensorAverageEcsValue + Cache.RangeEcs);
                Guard.IsNotEmpty(ecss);

                logger.LogHtmlInformation("Action", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                foreach (var ecs in ecss)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    logger.LogHtmlInformation($"{ecs}(ECS)", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    afViewModel.SetSensorEcsValue(ecs);
                    await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);

                    var darkFieldImageDtos = await cibViewModel.GetPMTImagesAsync(
                        Cache.ProductivityInformation,
                        StageCoordinateSystemEnum.Dark,
                        brightFieldPosition,
                        Cache.ImageWidthPixel,
                        [.. ApplicationCookie.CIBInformations.Where(t => t.PMTId == Cache.PmtId)],
                        (false, calChipSiteModelEnum),
                        (false, cibConfiguration),
                        (false, laserLightInformation),
                        false,
                        cancellationToken);

                    var resultList = Guard.IsNotNullAndReturn(Activator.CreateInstance(typeof(List<>).MakeGenericType(resultType)));
                    Guard.IsNotNullAndReturn(resultList.GetType().GetMethod(nameof(List<>.AddRange))).Invoke(resultList, [ObjectHelper.GetPropertyValue(Cache, cacheResultsPropertyName)]);

                    var result = Guard.IsNotNullAndReturn(Activator.CreateInstance(resultType));
                    var resultItemList = Guard.IsNotNullAndReturn(Activator.CreateInstance(typeof(List<>).MakeGenericType(resultItemType)));

                    ObjectHelper.SetPropertyValue(result, resultECSPropertyName, ecs);
                    ObjectHelper.SetPropertyValue(result, resultItemsPropertyName, resultItemList);

                    foreach (var darkFieldImageDto in darkFieldImageDtos)
                    {
                        using var _ = darkFieldImageDto;

                        var filePath = Path.Combine(ImageDirectory, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                        darkFieldImageDto.Image.Save(filePath);

                        var resultItem = Guard.IsNotNullAndReturn(Activator.CreateInstance(resultItemType));

                        ObjectHelper.SetPropertyValue(resultItem, resultItemImageChannelIdPropertyName, darkFieldImageDto.CIBInformation.ChannelId);
                        ObjectHelper.SetPropertyValue(resultItem, resultItemImageFilePathPropertyName, filePath);
                        await resultItemAction.Invoke(resultItem, darkFieldImageDto);

                        Guard.IsNotNullAndReturn(resultItemList.GetType().GetMethod(nameof(List<>.Add))).Invoke(resultItemList, [resultItem]);
                    }

                    Guard.IsNotNullAndReturn(resultList.GetType().GetMethod(nameof(List<>.Add))).Invoke(resultList, [result]);
                    ObjectHelper.SetPropertyValue(Cache, cacheResultsPropertyName, resultList);

                    RefreshPlot();
                }

                await finallyAction.Invoke();

                RefreshPlot();

                isSuccess = true;
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    dialogWindowProvider.ShowDialog($"{Name}: {stepName} Canceled({ex.Message})", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    logger.LogHtmlWarning("Canceled", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());

                    return;
                }

                dialogWindowProvider.ShowDialog($"""
                                                 {Name}: {stepName} Failed
                                                 {ex.Message}
                                                 """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
                logger.LogHtmlError(ex, "Failed", HtmlHeaderLevelEnum.Header3, HtmlLogUniqueId.LoggingHtml());
            }
            finally
            {
                logger.LogHtmlInformation(HtmlLogUniqueId.LoggedEndHtml($"{Name}_{stepName}_{(isSuccess ? "OK" : "Failed")}"));
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    private void RefreshPlot()
    {
        var hazeOriginDictionary = Cache.GetPoints(nameof(Cache.HazeResults), nameof(HazeResultItem.BeginningAverageGray), nameof(HazeResultItem.MiddleAverageGray), nameof(HazeResultItem.EndAverageGray), nameof(HazeResultItem.StandardDeviation));
        var hazeNormalizationDictionary = Cache.GetPoints(nameof(Cache.HazeResults), nameof(HazeResultItem.BeginningAverageGrayNormalization), nameof(HazeResultItem.MiddleAverageGrayNormalization), nameof(HazeResultItem.EndAverageGrayNormalization),
            nameof(HazeResultItem.StandardDeviationNormalization));
        var dswOriginDictionary = Cache.GetPoints(nameof(Cache.DSWResults), nameof(DSWResultItem.StrehlRatioX), nameof(DSWResultItem.StrehlRatioY));
        var dswNormalizationDictionary = Cache.GetPoints(nameof(Cache.DSWResults), nameof(DSWResultItem.StrehlRatioXNormalization), nameof(DSWResultItem.StrehlRatioYNormalization));

        if (hazeOriginDictionary.Count > 0) Guard.IsTrue(ScatterPlotControls.Keys.SequenceEqual(hazeOriginDictionary.Keys));
        if (hazeNormalizationDictionary.Count > 0) Guard.IsTrue(ScatterPlotControls.Keys.SequenceEqual(hazeNormalizationDictionary.Keys));
        if (dswOriginDictionary.Count > 0) Guard.IsTrue(ScatterPlotControls.Keys.SequenceEqual(dswOriginDictionary.Keys));
        if (dswOriginDictionary.Count > 0) Guard.IsTrue(ScatterPlotControls.Keys.SequenceEqual(dswOriginDictionary.Keys));

        foreach (var keyValuePair in ScatterPlotControls)
        {
            var hazeOriginDictionaryByChannelId = hazeOriginDictionary.Count > 0 ? hazeOriginDictionary[keyValuePair.Key] : ImmutableDictionary<string, IReadOnlyList<Point>>.Empty;
            var hazeNormalizationDictionaryByChannelId = hazeNormalizationDictionary.Count > 0 ? hazeNormalizationDictionary[keyValuePair.Key] : ImmutableDictionary<string, IReadOnlyList<Point>>.Empty;
            var dswOriginDictionaryByChannelId = dswOriginDictionary.Count > 0 ? dswOriginDictionary[keyValuePair.Key] : ImmutableDictionary<string, IReadOnlyList<Point>>.Empty;
            var dswNormalizationDictionaryByChannelId = dswNormalizationDictionary.Count > 0 ? dswNormalizationDictionary[keyValuePair.Key] : ImmutableDictionary<string, IReadOnlyList<Point>>.Empty;

            var scatterPlotControl = ScatterPlotControls[keyValuePair.Key];

            try
            {
                foreach (var (index, item) in hazeOriginDictionaryByChannelId.Index())
                {
                    scatterPlotControl.GetOrAddScatterLine(0, item.Key, item.Value, index, new Range(0, hazeOriginDictionaryByChannelId.Count - 1));
                }

                foreach (var (index, item) in dswOriginDictionaryByChannelId.Index())
                {
                    scatterPlotControl.GetOrAddScatterLine(1, item.Key, item.Value, index, new Range(0, dswOriginDictionaryByChannelId.Count - 1));
                }

                foreach (var (index, item) in hazeNormalizationDictionaryByChannelId.Index())
                {
                    scatterPlotControl.GetOrAddScatterLine(2, item.Key, item.Value, index, new Range(0, hazeNormalizationDictionaryByChannelId.Count - 1));
                }

                foreach (var (index, item) in dswNormalizationDictionaryByChannelId.Index())
                {
                    scatterPlotControl.GetOrAddScatterLine(2, item.Key, item.Value, index, new Range(0, dswNormalizationDictionaryByChannelId.Count - 1));
                }
            }
            finally
            {
                scatterPlotControl.AutoScaleRefresh();
            }
        }
    }

    private IScatterPlotControl GetScatterPlotControl()
    {
        var scatterPlotControl = serviceProvider.GetRequiredService<IScatterPlotControl>();

        var customGrid = new CustomGrid();
        scatterPlotControl.Configure(customGrid, 3,
            plots =>
            {
                customGrid.Set(plots[0], new GridCell(0, 0, 2, 2));
                customGrid.Set(plots[1], new GridCell(0, 1, 2, 2));
                customGrid.Set(plots[2], new GridCell(1, 0, 2, 1));
            });

        scatterPlotControl.SetTitle(0, $"{Haze} Origin");
        scatterPlotControl.SetTitle(1, $"{DSW} Origin");
        scatterPlotControl.SetTitle(2, "Normalization");

        return scatterPlotControl;
    }
}