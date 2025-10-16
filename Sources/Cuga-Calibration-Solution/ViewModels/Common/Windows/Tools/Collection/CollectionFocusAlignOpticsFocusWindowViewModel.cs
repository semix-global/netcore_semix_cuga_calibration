using System.IO;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Utilities;
using Core.Utilities.WPF;
using Local.NoSQL.DB.Providers.Bases;
using Local.NoSQL.DB.Providers.Extensions;
using Local.NoSQL.DB.Providers.Interfaces;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM.Services;
using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.MultiplotLayouts;
using ScottPlot.WPF;
using Constants = Net.Utilities.Models.Constants;
using Generate = MathNet.Numerics.Generate;
using Range = ScottPlot.Range;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Collection;

public sealed partial class AODWaveformCommonCache : ObservableCacheBase
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private StageSpeedEnum _stageSpeedEnum;

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

    #endregion

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
    private IReadOnlyList<DswResult> _dSWResults = [];

    #endregion

    public IDictionary<int, IDictionary<string, List<Point>>> GetPoints(string resultPropertyName, params string[] resultItemPropertyNames)
    {
        const string resultECSPropertyName = nameof(HazeResult.ECS);
        const string resultItemsPropertyName = nameof(HazeResult.Items);
        const string resultItemImageChannelIdPropertyName = nameof(HazeResultItem.ChannelId);

        var dictionary = new Dictionary<int, IDictionary<string, List<Point>>>();

        var count = 0;
        foreach (var result in GuardUtils.IsNotNullAndAssignableToType<System.Collections.IEnumerable>(ObjectHelper.GetPropertyValue(this, resultPropertyName)))
        {
            foreach (var resultItem in GuardUtils.IsNotNullAndAssignableToType<System.Collections.IEnumerable>(ObjectHelper.GetPropertyValue(result, resultItemsPropertyName)))
            {
                var temp = dictionary.GetOrAdd(
                    GuardUtils.IsNotNullAndAssignableToType<int>(ObjectHelper.GetPropertyValue(resultItem, resultItemImageChannelIdPropertyName)),
                    resultItemPropertyNames.ToDictionary(t => t, _ => new List<Point>()));

                foreach (var resultItemPropertyName in resultItemPropertyNames)
                {
                    temp[resultItemPropertyName].Add(new Point(
                        GuardUtils.IsNotNullAndAssignableToType<double>(ObjectHelper.GetPropertyValue(result, resultECSPropertyName)),
                        GuardUtils.IsNotNullAndAssignableToType<double>(ObjectHelper.GetPropertyValue(resultItem, resultItemPropertyName))));
                }
            }

            count++;
        }

        var counts = dictionary.Select(t => t.Value.Count).ToArray();

        Guard.IsEqualTo(counts[0], count);
        Guard.IsEqualTo(counts.Distinct().Count(), 1);

        return dictionary;
    }

    public object ToHtmlAnonymous() => new
    {
        OpticsMagTypeEnum,
        StageSpeedEnum,
        PmtId,
        ImageWidth = ImageWidthPixel,
        RangeEcs,
        StepEcs,
        HazeCIBConfiguration = new HtmlQuote(HazeCIBConfiguration.ToHtmlAnonymous()),
        HazeLaserLightInformation = new HtmlQuote(HazeLaserLightInformation.ToHtmlAnonymous()),
        HazeBrightFieldPosition,
        DSWCIBConfiguration = new HtmlQuote(DSWCIBConfiguration.ToHtmlAnonymous()),
        DSWLaserLightInformation = new HtmlQuote(DSWLaserLightInformation.ToHtmlAnonymous()),
        DSWBrightFieldPosition
    };
}

public sealed partial class HazeResult : ObservableCacheBase
{
    [ObservableProperty]
    private double _eCS;

    [ObservableProperty]
    private IReadOnlyList<HazeResultItem> _items = [];
}

public sealed partial class HazeResultItem : ObservableCacheBase
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

public sealed partial class DswResult : ObservableCacheBase
{
    [ObservableProperty]
    private double _eCS;

    [ObservableProperty]
    private IReadOnlyList<DswResultItem> _items = [];
}

public sealed partial class DswResultItem : ObservableCacheBase
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

public sealed partial class PlotControl : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private WpfPlot _wpfPlot = new();
}

[IOCAppService(ServiceType = typeof(CollectionFocusAlignOpticsFocusWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CollectionFocusAlignOpticsFocusWindowViewModel(
    StageViewModel stageViewModel,
    AfViewModel afViewModel,
    LaserViewModel laserViewModel,
    IOptions<ApplicationSetting> options,
    CreateRoiWindowViewModel createRoiWindowViewModel,
    ApplicationCookie applicationCookie,
    ICacheProvider cacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    ILogger<CollectionFocusAlignOpticsFocusWindowViewModel> logger) : ViewModelBase
{
    private const string DSW = nameof(DSW);
    private const string Haze = nameof(Haze);
    private static readonly Turbo Turbo = new();

    public string Name => "Collection Focus Align Optics Focus";

    public string ImageDirectory => Path.Combine(options.Value.AppHomeDirectory, "Images", DirectoryHelper.RemoveInvalidDirectoryName(nameof(CollectionFocusAlignOpticsFocusWindowViewModel)), DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public IReadOnlyList<LaserLightInformation> LaserLightInformations => applicationCookie.LaserLightInformationList;

    public Guid HtmlLogUniqueId { get; set; }


    [ObservableProperty]
    private AODWaveformCommonCache _cache = new();

    [ObservableProperty]
    private IReadOnlyList<PlotControl> _plotControls = [];

    [RelayCommand]
    private void Loaded()
    {
        Cache = cacheProvider.GetOrDefault<AODWaveformCommonCache>();
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeAsync(
            "Step1 Haze",
            CalChipSiteModelEnum.HazeModel,
            () => { },
            (item, darkFieldImageDto) =>
            {
                var hazeResultItem = GuardUtils.IsAssignableToType<HazeResultItem>(item);

                var matrix = Matrix<double>.Build.DenseOfArray(darkFieldImageDto.Matrix);

                var partSizes = new int[3];
                partSizes[0] = matrix.RowCount / 3 + (matrix.RowCount % 3 > 0 ? 1 : 0);
                partSizes[1] = partSizes[0] + matrix.RowCount / 3 + (matrix.RowCount % 3 > 1 ? 1 : 0);
                partSizes[2] = matrix.RowCount - partSizes[0] - partSizes[1];

                var beginningSubMatrix = matrix.SubMatrix(0, partSizes[0], 0, matrix.ColumnCount);
                var middleSubMatrix = matrix.SubMatrix(partSizes[0], partSizes[1], 0, matrix.ColumnCount);
                var endSubMatrix = matrix.SubMatrix(partSizes[1], partSizes[2], 0, matrix.ColumnCount);

                hazeResultItem.BeginningAverageGray = beginningSubMatrix.Enumerate().Average();
                hazeResultItem.MiddleAverageGray = middleSubMatrix.Enumerate().Average();
                hazeResultItem.EndAverageGray = endSubMatrix.Enumerate().Average();
                hazeResultItem.StandardDeviation = matrix.Enumerate().StandardDeviation();

                logger.LogHtmlInformation($"{hazeResultItem.ChannelId}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                {
                    Image = new HtmlImage(hazeResultItem.ImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(Cache.DSWROIRect)]),
                    RawImageFile = new HtmlDownload(darkFieldImageDto.Bytes, $"{Path.GetFileName(hazeResultItem.ImageFilePath)}.raw"),
                    Result = new HtmlQuote(hazeResultItem.ToHtmlAnonymous())
                }), HtmlLogUniqueId.LoggingHtml());
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

                var hazeOriginDictionary = Cache.GetPoints(nameof(Cache.HazeResults), nameof(HazeResultItem.BeginningAverageGray), nameof(HazeResultItem.MiddleAverageGray), nameof(HazeResultItem.EndAverageGray), nameof(HazeResultItem.StandardDeviation));
                var hazeNormalizationDictionary = Cache.GetPoints(nameof(Cache.HazeResults), nameof(HazeResultItem.BeginningAverageGrayNormalization), nameof(HazeResultItem.MiddleAverageGrayNormalization), nameof(HazeResultItem.EndAverageGrayNormalization), nameof(HazeResultItem.StandardDeviationNormalization));

                Guard.IsTrue(hazeOriginDictionary.Keys.SequenceEqual(hazeNormalizationDictionary.Keys));

                foreach (var keyValuePair in hazeOriginDictionary)
                {
                    var hazeOriginDictionaryByChannelId = hazeOriginDictionary[keyValuePair.Key];
                    var hazeNormalizationDictionaryByChannelId = hazeNormalizationDictionary[keyValuePair.Key];

                    logger.LogHtmlInformation($"{keyValuePair.Key} Origin", HtmlHeaderLevelEnum.Header3, new HtmlContainer([
                        ..hazeOriginDictionaryByChannelId.Select(t => new HtmlPlot2DLinesChart([(t.Key, [..t.Value])], t.Key))
                    ]), HtmlLogUniqueId.LoggingHtml());
                    logger.LogHtmlInformation($"{keyValuePair.Key} Normalization", HtmlHeaderLevelEnum.Header3, new HtmlPlot2DLinesChart([
                        ..hazeNormalizationDictionaryByChannelId.Select<KeyValuePair<string, List<Point>>, (string Name, Point[] Points)>(t => (t.Key, [..t.Value]))
                    ], string.Empty), HtmlLogUniqueId.LoggingHtml());
                }
            }, cancellationToken);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step2Async(CancellationToken cancellationToken)
    {
        await InvokeAsync(
            "DSW Haze",
            CalChipSiteModelEnum.DswModel,
            () =>
            {
                var darkFieldImageDto = laserViewModel.GetDarkFieldLineScanImage(
                    CalChipSiteModelEnum.DswModel,
                    Cache.DSWBrightFieldPosition,
                    (false, Cache.DSWLaserLightInformation),
                    false,
                    Cache.DSWCIBConfiguration,
                    Cache.ImageWidthPixel,
                    Cache.OpticsMagTypeEnum,
                    Cache.StageSpeedEnum,
                    stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright,
                    pmtId: Cache.PmtId,
                    isAutoFocus: true);
                using var _ = darkFieldImageDto;

                var filePath = Path.Combine(ImageDirectory, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                darkFieldImageDto.Image.Save(filePath);
                createRoiWindowViewModel.ImageFilePath = filePath;

                if (windowManagerService.ShowDialog(createRoiWindowViewModel) == false) ThrowHelper.ThrowOperationCanceledException<bool>("Generate ROI");

                Cache.DSWROIRect = createRoiWindowViewModel.Rect;

                logger.LogHtmlInformation("Create ROI", HtmlHeaderLevelEnum.Header1, new HtmlBullet(new
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
                var dswResultItem = GuardUtils.IsAssignableToType<DswResultItem>(item);

                var ((strehlRatioX, xLine, xFitLine), (strehlRatioY, yLine, yFitLine)) = StrehlRatioUtility.GetStrehlRatio(darkFieldImageDto.Matrix, Cache.DSWROIRect, Cache.DSWXPixelSize, Cache.DSWYPixelSize, Cache.DSWPotDiameter, Cache.DSWXPointDiameter, Cache.DSWYPointDiameter);
                dswResultItem.StrehlRatioX = strehlRatioX;
                dswResultItem.StrehlRatioY = strehlRatioY;

                logger.LogHtmlInformation($"{dswResultItem.ChannelId}", HtmlHeaderLevelEnum.Header5, new HtmlBullet(new
                {
                    Image = new HtmlImage(dswResultItem.ImageFilePath, htmlImageOverlays: [new HtmlImageRectangleOverlay(Cache.DSWROIRect)]),
                    RawImageFile = new HtmlDownload(darkFieldImageDto.Bytes, $"{Path.GetFileName(dswResultItem.ImageFilePath)}.raw"),
                    xFitLine = new HtmlPlot2DLinesChart([(nameof(xFitLine), xFitLine.ToPoints()), (nameof(xLine), xLine.ToPoints())], string.Empty),
                    yFitLine = new HtmlPlot2DLinesChart([(nameof(yFitLine), yFitLine.ToPoints()), (nameof(yLine), yLine.ToPoints())], string.Empty),
                    Result = new HtmlQuote(dswResultItem.ToHtmlAnonymous())
                }), HtmlLogUniqueId.LoggingHtml());
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

                var hazeOriginDictionary = Cache.GetPoints(nameof(Cache.HazeResults), nameof(HazeResultItem.BeginningAverageGray), nameof(HazeResultItem.MiddleAverageGray), nameof(HazeResultItem.EndAverageGray), nameof(HazeResultItem.StandardDeviation));
                var hazeNormalizationDictionary = Cache.GetPoints(nameof(Cache.HazeResults), nameof(HazeResultItem.BeginningAverageGrayNormalization), nameof(HazeResultItem.MiddleAverageGrayNormalization), nameof(HazeResultItem.EndAverageGrayNormalization), nameof(HazeResultItem.StandardDeviationNormalization));
                var dswOriginDictionary = Cache.GetPoints(nameof(Cache.DSWResults), nameof(DswResultItem.StrehlRatioX), nameof(DswResultItem.StrehlRatioY));
                var dswNormalizationDictionary = Cache.GetPoints(nameof(Cache.DSWResults), nameof(DswResultItem.StrehlRatioXNormalization), nameof(DswResultItem.StrehlRatioYNormalization));
                Guard.IsTrue(hazeOriginDictionary.Keys.SequenceEqual(hazeNormalizationDictionary.Keys));
                Guard.IsTrue(hazeNormalizationDictionary.Keys.SequenceEqual(dswOriginDictionary.Keys));
                Guard.IsTrue(dswOriginDictionary.Keys.SequenceEqual(dswNormalizationDictionary.Keys));

                foreach (var keyValuePair in hazeOriginDictionary)
                {
                    var hazeOriginDictionaryByChannelId = hazeOriginDictionary[keyValuePair.Key];
                    var hazeNormalizationDictionaryByChannelId = hazeNormalizationDictionary[keyValuePair.Key];
                    var dswOriginDictionaryByChannelId = dswOriginDictionary[keyValuePair.Key];
                    var dswNormalizationDictionaryByChannelId = dswNormalizationDictionary[keyValuePair.Key];

                    logger.LogHtmlInformation($"{keyValuePair.Key} Origin", HtmlHeaderLevelEnum.Header3, new HtmlContainer([
                        ..hazeOriginDictionaryByChannelId.Select(t => new HtmlPlot2DLinesChart([(t.Key, [..t.Value])], t.Key)),
                        ..dswOriginDictionaryByChannelId.Select(t => new HtmlPlot2DLinesChart([(t.Key, [..t.Value])], t.Key))
                    ]), HtmlLogUniqueId.LoggingHtml());
                    logger.LogHtmlInformation($"{keyValuePair.Key} Normalization", HtmlHeaderLevelEnum.Header3, new HtmlPlot2DLinesChart(
                        [
                            ..hazeNormalizationDictionaryByChannelId.Select<KeyValuePair<string, List<Point>>, (string Name, Point[] Points)>(t => (t.Key, [..t.Value])),
                            ..dswNormalizationDictionaryByChannelId.Select<KeyValuePair<string, List<Point>>, (string Name, Point[] Points)>(t => (t.Key, [..t.Value.Select(tt => new Point(tt.X + (Cache.HazeAverageECS - Cache.DSWAverageECS), tt.Y))]))
                        ], string.Empty
                    ), HtmlLogUniqueId.LoggingHtml());
                }
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
        Action beforeAction,
        Action<object, DarkFieldImageDto> resultItemAction,
        Action finallyAction,
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
                beforeAction.Invoke();

                var name = calChipSiteModelEnum switch
                {
                    CalChipSiteModelEnum.DswModel => DSW,
                    CalChipSiteModelEnum.HazeModel => Haze,
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(calChipSiteModelEnum))
                };

                var cibConfiguration = GuardUtils.IsNotNullAndAssignableToType<CIBConfiguration>(ObjectHelper.GetPropertyValue(Cache, nameof(Cache.HazeCIBConfiguration).Replace(Haze, name)));
                var laserLightInformation = GuardUtils.IsNotNullAndAssignableToType<LaserLightInformation>(ObjectHelper.GetPropertyValue(Cache, nameof(Cache.HazeLaserLightInformation).Replace(Haze, name)));
                var brightFieldPosition = GuardUtils.IsNotNullAndAssignableToType<Point>(ObjectHelper.GetPropertyValue(Cache, nameof(Cache.HazeBrightFieldPosition).Replace(Haze, name)));

                var resultType = GuardUtils.IsNotNullAndReturn(Type.GetType(typeof(HazeResult).GetAssemblyQualifiedName().Replace(Haze, name)));
                var resultItemType = GuardUtils.IsNotNullAndReturn(Type.GetType(typeof(HazeResultItem).GetAssemblyQualifiedName().Replace(Haze, name)));

                var cacheAverageECSPropertyName = nameof(Cache.HazeAverageECS).Replace(Haze, name);
                var cacheResultsPropertyName = nameof(Cache.HazeResults).Replace(Haze, name);

                const string resultECSPropertyName = nameof(HazeResult.ECS);
                const string resultItemsPropertyName = nameof(HazeResult.Items);
                const string resultItemImageChannelIdPropertyName = nameof(HazeResultItem.ChannelId);
                const string resultItemImageFilePathPropertyName = nameof(HazeResultItem.ImageFilePath);

                ObjectHelper.SetPropertyValue(Cache, cacheAverageECSPropertyName, 0d);
                ObjectHelper.SetPropertyValue(Cache, cacheResultsPropertyName, Array.CreateInstance(resultType, 0));

                stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(stageViewModel.BrightFieldToMachinePosition(brightFieldPosition));
                afViewModel.SetDarkFieldAutoFocus(null, Cache.OpticsMagTypeEnum, calChipSiteModelEnum);
                afViewModel.ToggleDarkFieldEnable(true);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                var sensorAverageEcsValue = afViewModel.GetSensorAverageEcsValue();

                ObjectHelper.SetPropertyValue(Cache, cacheAverageECSPropertyName, sensorAverageEcsValue);

                afViewModel.ToggleBrightFieldEnable(false);

                var ecss = Generate.LinearRange(sensorAverageEcsValue - Cache.RangeEcs, Cache.StepEcs, sensorAverageEcsValue + Cache.RangeEcs);
                Guard.IsNotEmpty(ecss);

                foreach (var ecs in ecss)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    logger.LogHtmlInformation($"{ecs}(ECS)", HtmlHeaderLevelEnum.Header4, HtmlLogUniqueId.LoggingHtml());

                    afViewModel.SetSensorEcsValue(ecs);

                    var darkFieldImageDtos = laserViewModel.GetDarkFieldLineScanImageList(
                        calChipSiteModelEnum,
                        brightFieldPosition,
                        Cache.ImageWidthPixel,
                        Cache.OpticsMagTypeEnum,
                        Cache.StageSpeedEnum,
                        Cache.PmtId,
                        StageCoordinateSystemEnum.Dark,
                        cibConfiguration,
                        (false, laserLightInformation),
                        false,
                        isAutoFocus: false);

                    var resultList = GuardUtils.IsNotNullAndReturn(Activator.CreateInstance(typeof(List<>).MakeGenericType(resultType)));
                    GuardUtils.IsNotNullAndReturn(resultList.GetType().GetMethod(nameof(List<string>.AddRange))).Invoke(resultList, [ObjectHelper.GetPropertyValue(Cache, cacheResultsPropertyName)]);

                    var result = GuardUtils.IsNotNullAndReturn(Activator.CreateInstance(resultType));
                    var resultItemList = GuardUtils.IsNotNullAndReturn(Activator.CreateInstance(typeof(List<>).MakeGenericType(resultItemType)));

                    ObjectHelper.SetPropertyValue(result, resultECSPropertyName, ecs);
                    ObjectHelper.SetPropertyValue(result, resultItemsPropertyName, resultItemList);

                    foreach (var darkFieldImageDto in darkFieldImageDtos)
                    {
                        using var _ = darkFieldImageDto;

                        var filePath = Path.Combine(ImageDirectory, $"{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.jpg");
                        darkFieldImageDto.Image.Save(filePath);

                        var resultItem = GuardUtils.IsNotNullAndReturn(Activator.CreateInstance(resultItemType));

                        ObjectHelper.SetPropertyValue(resultItem, resultItemImageChannelIdPropertyName, darkFieldImageDto.ChannelId);
                        ObjectHelper.SetPropertyValue(resultItem, resultItemImageFilePathPropertyName, filePath);
                        resultItemAction.Invoke(resultItem, darkFieldImageDto);

                        GuardUtils.IsNotNullAndReturn(resultItemList.GetType().GetMethod(nameof(List<string>.Add))).Invoke(resultItemList, [resultItem]);
                    }

                    GuardUtils.IsNotNullAndReturn(resultList.GetType().GetMethod(nameof(List<string>.Add))).Invoke(resultItemList, [result]);

                    RefreshPlot();
                }

                finallyAction.Invoke();

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
        var hazeNormalizationDictionary = Cache.GetPoints(nameof(Cache.HazeResults), nameof(HazeResultItem.BeginningAverageGrayNormalization), nameof(HazeResultItem.MiddleAverageGrayNormalization), nameof(HazeResultItem.EndAverageGrayNormalization), nameof(HazeResultItem.StandardDeviationNormalization));
        var dswOriginDictionary = Cache.GetPoints(nameof(Cache.DSWResults), nameof(DswResultItem.StrehlRatioX), nameof(DswResultItem.StrehlRatioY));
        var dswNormalizationDictionary = Cache.GetPoints(nameof(Cache.DSWResults), nameof(DswResultItem.StrehlRatioXNormalization), nameof(DswResultItem.StrehlRatioYNormalization));
        Guard.IsTrue(hazeOriginDictionary.Keys.SequenceEqual(hazeNormalizationDictionary.Keys));
        Guard.IsTrue(hazeNormalizationDictionary.Keys.SequenceEqual(dswOriginDictionary.Keys));
        Guard.IsTrue(dswOriginDictionary.Keys.SequenceEqual(dswNormalizationDictionary.Keys));

        var plotControlList = new List<PlotControl>();

        foreach (var keyValuePair in hazeOriginDictionary)
        {
            var hazeOriginDictionaryByChannelId = hazeOriginDictionary.Count > 0 ? hazeOriginDictionary[keyValuePair.Key] : new Dictionary<string, List<Point>>();
            var hazeNormalizationDictionaryByChannelId = hazeNormalizationDictionary.Count > 0 ? hazeNormalizationDictionary[keyValuePair.Key] : new Dictionary<string, List<Point>>();
            var dswOriginDictionaryByChannelId = dswOriginDictionary.Count > 0 ? dswOriginDictionary[keyValuePair.Key] : new Dictionary<string, List<Point>>();
            var dswNormalizationDictionaryByChannelId = dswNormalizationDictionary.Count > 0 ? dswNormalizationDictionary[keyValuePair.Key] : new Dictionary<string, List<Point>>();

            var wpfPlot = new WpfPlot();
            var plotControl = new PlotControl
            {
                Title = $"Channel {keyValuePair.Key}",
                WpfPlot = wpfPlot
            };

            var customGrid = new CustomGrid();
            wpfPlot.ConfigureWpfPlotScatter(customGrid, 3);

#pragma warning disable IDE0079
#pragma warning disable IDISP001
            var plot0 = wpfPlot.Multiplot.GetPlot(0);
            var plot1 = wpfPlot.Multiplot.GetPlot(1);
            var plot2 = wpfPlot.Multiplot.GetPlot(2);
#pragma warning restore IDISP001
#pragma warning restore IDE0079

            customGrid.Set(plot0, new GridCell(0, 0, 2, 2));
            plot0.Title($"{Haze} Origin");


            foreach (var (i, pair) in hazeOriginDictionaryByChannelId.Select((pair, i) => (i, pair)))
            {
                var scatter = plot0.Add.Scatter(pair.Value.Select(t => new Coordinates(t.X, t.Y)).ToArray(), Turbo.GetColor(i, new Range(0, hazeOriginDictionaryByChannelId.Count - 1)));

                scatter.LegendText = pair.Key;
            }

            customGrid.Set(plot1, new GridCell(0, 1, 2, 2));
            plot1.Title($"{DSW} Origin");

            foreach (var (i, pair) in dswOriginDictionaryByChannelId.Select((pair, i) => (i, pair)))
            {
                var scatter = plot1.Add.Scatter(pair.Value.Select(t => new Coordinates(t.X, t.Y)).ToArray(), Turbo.GetColor(i, new Range(0, hazeOriginDictionaryByChannelId.Count - 1)));

                scatter.LegendText = pair.Key;
            }

            customGrid.Set(plot2, new GridCell(1, 0, 2, 2, colSpan: 2));
            plot1.Title("Normalization");

            foreach (var (i, pair) in hazeNormalizationDictionaryByChannelId.Select((pair, i) => (i, pair)))
            {
                var scatter = plot1.Add.Scatter(pair.Value.Select(t => new Coordinates(t.X, t.Y)).ToArray(), Turbo.GetColor(i, new Range(0, hazeOriginDictionaryByChannelId.Count - 1)));

                scatter.LegendText = pair.Key;
            }

            foreach (var (i, pair) in dswNormalizationDictionaryByChannelId.Select((pair, i) => (i, pair)))
            {
                var scatter = plot1.Add.Scatter(pair.Value.Select(t => new Coordinates(t.X + (Cache.HazeAverageECS - Cache.DSWAverageECS), t.Y)).ToArray(), Turbo.GetColor(i, new Range(0, hazeOriginDictionaryByChannelId.Count - 1)));

                scatter.LegendText = pair.Key;
            }

            foreach (var plot in wpfPlot.Multiplot.GetPlots())
            {
                plot.ShowLegend(ScottPlot.Alignment.UpperLeft, Orientation.Vertical);
                plot.Axes.AutoScale();
            }

            wpfPlot.Refresh();

            plotControlList.Add(plotControl);
        }

        PlotControls = plotControlList;
    }
}