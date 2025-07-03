using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Helper;
using Core.Models.Models.Chuck.BrightFieldStageMap;
using Core.Models.Models.Chuck.Center;
using Core.Models.Models.Chuck.DarkFieldStageMap;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Laser.LineCentricity;
using Core.Services.Interfaces;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Helper;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.Extensions;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.DataSources;
using ScottPlot.Interactivity;
using ScottPlot.Interactivity.UserActionResponses;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System.Numerics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Point = Net.Utilities.Models.Geometries.Point;
using Range = ScottPlot.Range;
using Vector = Net.Utilities.Models.Geometries.Vector;

namespace CugaCalibrationTest.ViewModels;

[IOCAppService(ServiceType = typeof(StageMapWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class StageMapWindowViewModel(
    ICacheProvider cacheProvider,
    [FromKeyedServices(LiteDbConstantHelper.RecipeDbKey)]
    ICacheProvider recipeCacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ILogger<StageMapWindowViewModel> logger) : ViewModelBase
{
    private static readonly Turbo ColorMap = new();

    [RelayCommand]
    private void MergeStageMapStep0()
    {
        if (cacheProvider.TryGetOrDefault<ChuckBrightFieldStageMapDto>(out var brightFieldStageMapDto) == false) return;
        if (cacheProvider.TryGetOrDefault<ChuckDarkFieldStageMapDto>(out var darkFieldStageMapDto) == false) return;

        cacheProvider.Set(darkFieldStageMapDto, CancellationToken.None);

        ShowWindow("Step0: Read DF BF Matrix", darkFieldStageMapDto.CalibrationStageMap, brightFieldStageMapDto.CalibrationStageMap);
    }

    [RelayCommand]
    private void MergeStageMapStep1()
    {
        if (cacheProvider.TryGetOrDefault<ChuckBrightFieldStageMapDto>(out var brightFieldStageMapDto) == false) return;
        if (cacheProvider.TryGetOrDefault<ChuckDarkFieldStageMapDto>(out var darkFieldStageMapDto) == false) return;

        var htmlLogUniqueId = Guid.NewGuid();
        var expandStageMapDto = calibrationAlgorithmService.ExpandStageMapDto(darkFieldStageMapDto.CalibrationStageMap, brightFieldStageMapDto.CalibrationStageMap, htmlLogUniqueId);
        logger.LogHtmlInformation(htmlLogUniqueId.LoggedEndHtml());

        ShowWindow("Step1: Expand Matrix By Bilinear", expandStageMapDto, brightFieldStageMapDto.CalibrationStageMap);
    }

    public void ShowWindow(string title, StageMapDto? df = null, StageMapDto? bf = null, int? width = null)
    {
        var wpfPlot = new WpfPlot();
        ConfigureWpfPlot(wpfPlot);

        if (cacheProvider.TryGetOrDefault<ChuckCenterObjDto>(out var chuckCenter) == false) return;
        if (recipeCacheProvider.TryGetOrDefault<ChuckBrightFieldStageMapCache>(out var brightFieldCache) == false) return;
        if (recipeCacheProvider.TryGetOrDefault<ChuckDarkFieldStageMapCache>(out var darkFieldCache) == false) return;
        if (cacheProvider.TryGetOrDefaultArray<LaserLineCentricityItemDto>(out var laserLineCentricityItems) == false) return;

        if (df is not null)
        {
            var laserLineCentricityItemDto = laserLineCentricityItems.Single(t => t is
            {
                PmtId: CalibrationConstantsHelper.MainPmtId,
                OpticsMagTypeEnum: CalibrationConstantsHelper.MainOpticsMagTypeEnum,
                StageSpeedEnum: CalibrationConstantsHelper.MainStageSpeedEnum
            });

            var ellipse = wpfPlot.Plot.Add.Circle(
                laserLineCentricityItemDto.ForwardDarkMachineCenterPosition.X,
                laserLineCentricityItemDto.ForwardDarkMachineCenterPosition.Y,
                darkFieldCache.WaferDiameter / 2d);
            ellipse.LineColor = Colors.DarkRed;
            ellipse.LineWidth = 2;

            ShowVectorField(wpfPlot, df.IdealStageMapItemMatrix, df.ErrorMatrix, df.RowNumber, df.ColumnNumber, Colors.Red.WithAlpha(0.3));
        }

        if (bf is not null)
        {
            var brightFieldEllipse = wpfPlot.Plot.Add.Circle(chuckCenter.NewBFCenterStagePosition.X, chuckCenter.NewBFCenterStagePosition.Y, brightFieldCache.WaferDiameter / 2d);
            brightFieldEllipse.LineColor = Colors.DarkRed;
            brightFieldEllipse.LineWidth = 2;
            ShowVectorField(wpfPlot, bf.IdealStageMapItemMatrix, bf.ErrorMatrix, bf.RowNumber, bf.ColumnNumber, Colors.Gray.WithAlpha(0.3));
        }

        wpfPlot.Plot.Axes.AutoScale();
        wpfPlot.Refresh();

        var window = new Window
        {
            Title = title,
            Content = wpfPlot,
            Width = width ?? 1440,
            Height = width ?? 1000,
            Padding = new Thickness(5, 5, 5, 5),
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        window.Show();
    }

    #region 测试算法

    [ObservableProperty]
    private Point _startPoint = Point.Origin;

    [ObservableProperty]
    private int _rowCount = 5;

    [ObservableProperty]
    private int _columnCount = 5;

    [ObservableProperty]
    private double _rowHeight = 16600;

    [ObservableProperty]
    private double _columnWidth = 15300;

    [ObservableProperty]
    private Point _searchPoint = Point.Origin;

    [ObservableProperty]
    private bool _isContainError;

    [ObservableProperty]
    private bool _isShowDetails;

    [ObservableProperty]
    private double _error1 = 10;

    [ObservableProperty]
    private double _error2 = 20;

    [ObservableProperty]
    private double _error3 = 30;

    [ObservableProperty]
    private double _error4 = 40;

    [ObservableProperty]
    private WpfPlot _wpfPlot = new();

    private StageMapDto? _currentStageMapDto;

    partial void OnIsContainErrorChanged(bool value)
    {
        if (value == false) return;

        StartPoint = Point.Origin;
        RowCount = ColumnCount = 2;
        RowHeight = 20;
        ColumnWidth = 10;
        Error1 = 10;
        Error2 = 20;
        Error3 = 30;
        Error4 = 40;

        TestBilinearGenerateMatrix();
    }

    [RelayCommand]
    private void TestBilinearGenerateMatrix()
    {
        _currentStageMapDto = new StageMapDto(RowCount, ColumnCount, RowHeight, ColumnWidth);

        var errors = (Point[])[new Point(Error1, Error1), new Point(Error2, Error2), new Point(Error3, Error3), new Point(Error4, Error4)];

        // 生成矩阵数据，使用起始点作为偏移
        var index = 0;
        for (var row = 0; row < RowCount; row++)
        {
            for (var column = 0; column < ColumnCount; column++)
            {
                _currentStageMapDto.IdealStageMapItemMatrix[row][column].Clear();

                var x = StartPoint.X + column * ColumnWidth;
                var y = StartPoint.Y + row * RowHeight;
                var idealPoint = new Point(x, y);

                _currentStageMapDto.IdealStageMapItemMatrix[row][column].Row = row;
                _currentStageMapDto.IdealStageMapItemMatrix[row][column].Column = column;
                _currentStageMapDto.IdealStageMapItemMatrix[row][column].Point = idealPoint;
                _currentStageMapDto.IdealStageMapItemMatrix[row][column].IsInWafer = true;

                _currentStageMapDto.RealMatrix[row][column] = idealPoint + (Vector)errors[index % errors.Length];
                _currentStageMapDto.ErrorMatrix[row][column] = errors[index % errors.Length];
                index++;
            }
        }

        ConfigureWpfPlot(WpfPlot, IsContainError);
        ShowVectorField(WpfPlot,
            _currentStageMapDto.IdealStageMapItemMatrix,
            _currentStageMapDto.ErrorMatrix,
            _currentStageMapDto.RowNumber,
            _currentStageMapDto.ColumnNumber,
            Colors.Gray.WithAlpha(0.3),
            IsContainError,
            IsShowDetails);
        WpfPlot.UserInputProcessor.UserActionResponses.Add(new DoubleClickResponse(StandardMouseButtons.Left, (plotControl, mousePixel) =>
        {
            var mouseLocation = plotControl.Plot.GetCoordinates(mousePixel);
            SearchPoint = new Point(mouseLocation.X, mouseLocation.Y);
            TestBilinearSearchPoint();
        }));
    }

    [RelayCommand]
    private void TestBilinearSearchPoint()
    {
        if (_currentStageMapDto is null) return;

        try
        {
            WpfPlot.Plot.PlottableList.RemoveAll(t => t is Marker);

            var (idealMatrix, valueIsOkMatrix, valueMatrix) = _currentStageMapDto.GetStageMapBilinearArray();

            var xResult = BinarySearch.TryValueIndexRange(
                [.. MatrixUtils.Row(idealMatrix, 0).Select(tt => tt.X)],
                SearchPoint.X,
                out var startColumnIndex,
                out var endColumnIndex); // x方向寻找行
            var yResult = BinarySearch.TryValueIndexRange(
                [.. MatrixUtils.Column(idealMatrix, 0).Select(tt => tt.Y)],
                SearchPoint.Y,
                out var startRowIndex,
                out var endRowIndex); // y方向寻找列
            if (xResult == false || yResult == false)
            {
                dialogWindowProvider.ShowDialog("Not Get Point");
                return;
            }

            var leftDownIdeal = idealMatrix[startRowIndex, startColumnIndex];
            var leftDownValueIsOk = valueIsOkMatrix[startRowIndex, startColumnIndex];
            var leftDownValue = valueMatrix[startRowIndex, startColumnIndex];

            var rightDownIdeal = idealMatrix[startRowIndex, endColumnIndex];
            var rightDownValueIsOk = valueIsOkMatrix[startRowIndex, endColumnIndex];
            var rightDownValue = valueMatrix[startRowIndex, endColumnIndex];

            var leftUpIdeal = idealMatrix[endRowIndex, startColumnIndex];
            var leftUpValueIsOk = valueIsOkMatrix[endRowIndex, startColumnIndex];
            var leftUpValue = valueMatrix[endRowIndex, startColumnIndex];

            var rightUpIdeal = idealMatrix[endRowIndex, endColumnIndex];
            var rightUpValueIsOk = valueIsOkMatrix[endRowIndex, endColumnIndex];
            var rightUpValue = valueMatrix[endRowIndex, endColumnIndex];

            if (IsContainError)
            {
                if (leftDownValueIsOk == false || rightDownValueIsOk == false || leftUpValueIsOk == false || rightUpValueIsOk == false)
                {
                    dialogWindowProvider.ShowDialog("Not Get Point");
                    return;
                }

                var valueX = Interpolator.Bilinear(new Point3D(leftDownIdeal.X, leftDownIdeal.Y, leftDownValue.X),
                    new Point3D(rightDownIdeal.X, rightDownIdeal.Y, rightDownValue.X),
                    new Point3D(leftUpIdeal.X, leftUpIdeal.Y, leftUpValue.X),
                    new Point3D(rightUpIdeal.X, rightUpIdeal.Y, rightUpValue.X),
                    SearchPoint);
                var valueY = Interpolator.Bilinear(new Point3D(leftDownIdeal.X, leftDownIdeal.Y, leftDownValue.Y),
                    new Point3D(rightDownIdeal.X, rightDownIdeal.Y, rightDownValue.Y),
                    new Point3D(leftUpIdeal.X, leftUpIdeal.Y, leftUpValue.Y),
                    new Point3D(rightUpIdeal.X, rightUpIdeal.Y, rightUpValue.Y),
                    SearchPoint);

                var pt = new Coordinates(SearchPoint.X, SearchPoint.Y);
                var v = new Vector2((float)valueX, (float)valueY);

                if (StartPoint == Point.Origin
                    && RowCount == 2
                    && ColumnCount == 2
                    && RowHeight - 20 == 0
                    && ColumnWidth - 10 == 0
                    && Error1 - 10 == 0
                    && Error2 - 20 == 0
                    && Error3 - 30 == 0
                    && Error4 - 40 == 0)
                {
                    Guard.IsEqualTo(valueX, valueY);
                    Guard.IsLessThanOrEqualTo(Math.Abs(valueX - (SearchPoint.X + Error1 + SearchPoint.Y)), 1e-10);

                    logger.LogInformation("SearchPoint True: {@SearchPoint}, Value: {@ValueX}, {@ValueY}", SearchPoint, valueX, valueY);
                }

                WpfPlot.Plot.PlottableList.Add(new WrapperErrorText
                {
                    LabelText = IsShowDetails == false
                        ? string.Empty
                        : $"""
                           {pt.X:0.##########}, {pt.Y:0.##########}
                           {v.X:0.##########}, {v.Y:0.##########}
                           """,
                    LabelBackgroundColor = Colors.Transparent,
                    Location = pt,
                    LabelFontSize = 10,
                    LabelPadding = 1,
                    LabelFontColor = IsShowDetails == false ? Colors.Transparent : Colors.Blue,
                    LabelAlignment = Alignment.MiddleCenter,
                    Vector = v
                });

                var vectorField = WpfPlot.Plot.PlottableList.OfType<VectorField>().First();
                var vectorFieldDataSourceCoordinatesList = (VectorFieldDataSourceCoordinatesList)typeof(VectorField).GetProperty("Source", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(vectorField);
                var rootedCoordinateVectors = (IList<RootedCoordinateVector>)vectorFieldDataSourceCoordinatesList.GetType().GetField("RootedVectors", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(vectorFieldDataSourceCoordinatesList);
                rootedCoordinateVectors.Add(new RootedCoordinateVector(pt, v));
            }

            foreach (var point in (Point[])[leftDownIdeal, rightDownIdeal, leftUpIdeal, rightUpIdeal])
            {
                var marker = WpfPlot.Plot.Add.Marker(point.X, point.Y, shape: MarkerShape.FilledCircle);
                marker.MarkerFillColor = Colors.Red;
                marker.MarkerLineColor = Colors.Red;
            }

            if (IsContainError) return;

            var searchMarker = WpfPlot.Plot.Add.Marker(SearchPoint.X, SearchPoint.Y, shape: MarkerShape.Cross);
            searchMarker.MarkerFillColor = Colors.Green;
            searchMarker.MarkerLineColor = Colors.Green;
        }
        finally
        {
            WpfPlot.Refresh();
        }
    }

    [RelayCommand]
    private void CalculateStageMapError()
    {
        var htmlLogUniqueId = Guid.NewGuid();
        try
        {
            logger.LogHtmlInformation($"Test{nameof(CalculateStageMapError)}", HtmlHeaderLevelEnum.Header2, htmlLogUniqueId.LoggingHtml());

            if (cacheProvider.TryGetOrDefault<ChuckStageMapDto>(out var stageMapDto) == false) return;
            var brightFieldStageMapDto = stageMapDto.CalibrationBrightFieldStageMap.Clone();
            var darkFieldStageMapDto = stageMapDto.CalibrationDarkFieldStageMap.Clone();
            if (cacheProvider.TryGetOrDefault<ChuckStageMapCache>(out var cache) == false) return;
            var tryCalculateStageMapError = calibrationAlgorithmService.CalculateChuckStageMapError(
                brightFieldStageMapDto,
                htmlLogUniqueId,
                cache.CalculateContainRowMinCout,
                cache.CalculateContainColumnMinCount,
                cache.CalibrationAlignmentThreshold,
                cache.CalibrationGantryThreshold,
                cache.CalibrationScaleThreshold,
                cache.WaferDiameter);
            tryCalculateStageMapError = calibrationAlgorithmService.CalculateChuckStageMapError(
                darkFieldStageMapDto,
                htmlLogUniqueId,
                cache.CalculateContainRowMinCout,
                cache.CalculateContainColumnMinCount,
                cache.CalibrationAlignmentThreshold,
                cache.CalibrationGantryThreshold,
                cache.CalibrationScaleThreshold,
                cache.WaferDiameter);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Calculate Stage Map Error Failed");
            dialogWindowProvider.ShowDialog("Calculate Stage Map Error Failed", ex.Message);
            return;
        }
        finally
        {
            logger.LogHtmlInformation(htmlLogUniqueId.LoggingPeekHtml($"{CalibrationTypeEnum.HandleCalibration}"));
        }
    }

    #endregion 测试算法

    private static void ShowVectorField(
        WpfPlot wpfPlot,
        StageMapItemDto[][] mapIdealStageMapItemMatrix,
        Point[][] mapErrorMatrix,
        int rowNumber,
        int columnNumber,
        Color gridColor,
        bool isContainError = true,
        bool isShowDetails = false)
    {
        var temp = mapErrorMatrix.SelectMany(t => t).Select(t => t.ToOriginLength).ToList();
        var errorLengthMin = temp.Min();
        var errorLengthMax = temp.Max();

        Range? range =
            errorLengthMin == 0 && errorLengthMax == 0 ? null
            : errorLengthMax - errorLengthMin == 0 ? new Range(0, errorLengthMax)
            : new Range(errorLengthMin, errorLengthMax);

        var vectors = new List<RootedCoordinateVector>();

        for (var row = 0; row < rowNumber; row++)
        {
            for (var column = 0; column < columnNumber; column++)
            {
                var idealItem = mapIdealStageMapItemMatrix[row][column];
                var point = mapErrorMatrix[row][column];

                var pt = new Coordinates(idealItem.Point.X, idealItem.Point.Y);
                var v = new Vector2((float)point.X, (float)point.Y);

                if (range is not null) vectors.Add(new RootedCoordinateVector(pt, v));

                wpfPlot.Plot.PlottableList.Add(new WrapperErrorText
                {
                    LabelText = isShowDetails == false
                        ? $"({row + 1}, {column + 1})"
                        : $"""
                           ({row + 1}, {column + 1})
                           {pt.X:0.##########}, {pt.Y:0.##########}
                           {v.X:0.##########}, {v.Y:0.##########}
                           """,
                    LabelBackgroundColor = Colors.Transparent,
                    Location = pt,
                    LabelFontSize = 10,
                    LabelPadding = 1,
                    LabelFontColor = isShowDetails == false ? Colors.Transparent : Colors.Blue,
                    LabelAlignment = Alignment.MiddleCenter,
                    Vector = v
                });
            }
        }

        // 绘制水平网格线（按行遍历）
        for (var row = 0; row < rowNumber; row++)
        {
            var rowPoints = mapIdealStageMapItemMatrix[row];
            var xStart = rowPoints.First().Point.X;
            var xEnd = rowPoints.Last().Point.X;
            var y = rowPoints.First().Point.Y;

            var line = wpfPlot.Plot.Add.Line(xStart, y, xEnd, y);
            line.LineWidth = 1;
            line.Color = gridColor;
        }

        // 绘制垂直网格线（按列遍历）
        for (var column = 0; column < columnNumber; column++)
        {
            var x = mapIdealStageMapItemMatrix[0][column].Point.X;
            var yStart = mapIdealStageMapItemMatrix[0][column].Point.Y;
            var yEnd = mapIdealStageMapItemMatrix[rowNumber - 1][column].Point.Y;

            var line = wpfPlot.Plot.Add.Line(x, yStart, x, yEnd);
            line.LineWidth = 1;
            line.Color = gridColor;
        }

        if (range is null) return;
        if (isContainError == false) return;

        var vf = wpfPlot.Plot.Add.VectorField(vectors);
        vf.Colormap = ColorMap;
    }

    private static WrapperErrorText? _lastText;

    public object HtmlLogUniqueId { get; private set; }

    private static void ConfigureWpfPlot(WpfPlot wpfPlot, bool isContainError = true)
    {
        wpfPlot.ConfigureWpfPlotCommon();

        wpfPlot.Plot.Title("Map");
        wpfPlot.Plot.HideAxesAndGrid();

        // 显示信息
        var annotation = wpfPlot.Plot.Add.Annotation(string.Empty, Alignment.UpperRight);
        annotation.LabelBackgroundColor = Colors.Yellow;
        annotation.IsVisible = false;

        // 鼠标十字线
        var crossHair = wpfPlot.Plot.Add.Crosshair(0, 0);
        crossHair.LineColor = Colors.Red;
        crossHair.TextColor = Colors.White;
        crossHair.TextBackgroundColor = Colors.Red;
        crossHair.MarkerShape = MarkerShape.OpenCircle;
        crossHair.MarkerSize = 10;
        crossHair.IsVisible = false;

        if (isContainError)
            wpfPlot.UserInputProcessor.UserActionResponses.Add(new SingleClickResponse(StandardMouseButtons.Left, (plotControl, mousePixel) =>
            {
                if (_lastText is not null)
                {
                    _lastText.LabelBorderWidth = 0;
                    _lastText.LabelBorderColor = Colors.Transparent;
                    _lastText = null;
                }

                var mouseLocation = plotControl.Plot.GetCoordinates(mousePixel);
                var scatterSourceCoordinatesArray = new ScatterSourceCoordinatesArray([
                    .. plotControl.Plot.PlottableList
                        .OfType<Text>()
                        .Select(t => t.Location)
                ]);

                var nearest = scatterSourceCoordinatesArray.GetNearest(mouseLocation, plotControl.Plot.LastRender);
                if (nearest.IsReal)
                {
                    _lastText = plotControl.Plot.PlottableList.OfType<WrapperErrorText>().Single(t => t.Location == nearest.Coordinates);
                    _lastText.LabelBorderWidth = 3;
                    _lastText.LabelBorderColor = Colors.Red;

                    annotation.LabelText = string.Join(
                        Environment.NewLine,
                        _lastText.LabelText,
                        $"Error: ({_lastText.Vector.X:0.##########}, {_lastText.Vector.Y:0.##########})",
                        $"Position: {_lastText.Location.X:0.##########}, {_lastText.Location.Y:0.##########}"
                    );

                    crossHair.IsVisible = false;
                    annotation.IsVisible = true;
                }
                else
                {
                    annotation.IsVisible = false;
                }

                plotControl.Plot.PlotControl?.Refresh();
            }));

        // 如果移除的方法的内部方法有闭包如果不是静态, 那么移除没用, 因为捕获变量导致不是同一个方法了, CLR会创建一个类来存储这个闭包, 每次都不是一个实例的方法, 移除的就不是这个方法了
        // 移除的方法的内部方法不是闭包那么也没问题, 就不会创建内部类了
        // 如果不是方法的内部方法是外部方法肯定更可以移除，没有关系
        wpfPlot.MouseMove -= OnWpfPlotOnMouseMove;
        wpfPlot.MouseMove += OnWpfPlotOnMouseMove;
        wpfPlot.MouseLeave -= OnWpfPlotOnMouseLeave;
        wpfPlot.MouseLeave += OnWpfPlotOnMouseLeave;

        wpfPlot.Plot.Axes.AutoScale();
        wpfPlot.Refresh();

        return;

        static void OnWpfPlotOnMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not WpfPlot plot) return;
            if (_lastText is not null) return;

            var position = e.GetPosition(plot);
            var mousePixel = new Pixel(position.X, position.Y);
            var mouseLocation = plot.Plot.GetCoordinates(mousePixel);

            var crossHair = plot.Plot.PlottableList.OfType<Crosshair>().First();
            var annotation = plot.Plot.PlottableList.OfType<Annotation>().First();

            crossHair.IsVisible = true;
            crossHair.Position = mouseLocation;
            annotation.IsVisible = true;
            annotation.LabelText = $"{mouseLocation.X:f3}, {mouseLocation.Y:f3}";

            e.Handled = true;
            plot.Refresh();
        }

        static void OnWpfPlotOnMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is not WpfPlot plot) return;

            var crossHair = plot.Plot.PlottableList.OfType<Crosshair>().First();
            var annotation = plot.Plot.PlottableList.OfType<Annotation>().First();

            crossHair.IsVisible = false;
            if (_lastText is null) annotation.IsVisible = false;

            e.Handled = true;
            plot.Refresh();
        }
    }

    private sealed class WrapperErrorText : Text
    {
        public Vector2 Vector { get; init; }
    }
}