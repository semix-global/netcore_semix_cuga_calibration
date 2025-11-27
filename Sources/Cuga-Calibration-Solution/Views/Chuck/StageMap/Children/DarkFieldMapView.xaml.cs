using Core.Models.Helper;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.StageMap;
using CugaCalibration.ViewModels.Chuck;
using Microsoft.Extensions.Logging;
using Net.Utilities.WPF.Extensions;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.DataSources;
using ScottPlot.Interactivity;
using ScottPlot.Interactivity.UserActionResponses;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System.ComponentModel;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using Point = Net.Utilities.Models.Geometries.Point;
using Range = ScottPlot.Range;

namespace CugaCalibration.Views.Chuck.StageMap.Children;

public sealed partial class DarkFieldMapView
{
    private static readonly Turbo ColorMap = new();
    private static ILogger<DarkFieldMapView>? _logger;
    private ApplicationCookie _applicationCookie;

    public string ChildName { get; set; } = string.Empty;

    private bool _isLoaded;

    public DarkFieldMapView()
    {
        InitializeComponent();

        Loaded -= OnLoaded;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;

        if (DataContext is not ChuckStageMapCalibrationViewModel viewModel) return;

        viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;

        _applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();

        ConfigureWpfPlot(WpfPlot, viewModel);
    }

    private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (sender is not ChuckStageMapCalibrationViewModel viewModel) return;

        var laserLineCentricityItemDto = viewModel.LaserLineCentricityItems.SingleOrDefault(t => t.PmtId == CalibrationConstantsHelper.MainPmtId
                                                                                                 && t.ProductivityInformation == _applicationCookie.LowProductivityInformation);
        if (laserLineCentricityItemDto is null) return;

        switch (e.PropertyName)
        {
            case nameof(viewModel.ResultChuckStageMapDto.CalibrationDarkFieldStageMap):
                switch (ChildName)
                {
                    case "Step4View" when viewModel.CalibrationStepIndex == 7:
                    case "Step5View" when viewModel.CalibrationStepIndex is 7 or 8:
                        Dispatcher.Invoke(() =>
                        {
                            ShowVectorField(WpfPlot,
                                viewModel.ResultChuckStageMapDto.CalibrationDarkFieldStageMap.IdealStageMapItemMatrix,
                                viewModel.ResultChuckStageMapDto.CalibrationDarkFieldStageMap.ErrorMatrix,
                                viewModel.ResultChuckStageMapDto.CalibrationDarkFieldStageMap.RowNumber,
                                viewModel.ResultChuckStageMapDto.CalibrationDarkFieldStageMap.ColumnNumber,
                                Colors.Gray.WithAlpha(0.3),
                                laserLineCentricityItemDto.DarkMachineCenterPosition,
                                viewModel.Cache.WaferDiameter);
                        });

                        break;
                }

                break;

            case nameof(viewModel.ResultChuckStageMapDto.ExpandStageMapDto):
                switch (ChildName)
                {
                    case "Step4View" when viewModel.CalibrationStepIndex == 9:
                        Dispatcher.Invoke(() =>
                        {
                            ShowVectorField(WpfPlot,
                                viewModel.ResultChuckStageMapDto.ExpandStageMapDto.IdealStageMapItemMatrix,
                                viewModel.ResultChuckStageMapDto.ExpandStageMapDto.ErrorMatrix,
                                viewModel.ResultChuckStageMapDto.ExpandStageMapDto.RowNumber,
                                viewModel.ResultChuckStageMapDto.ExpandStageMapDto.ColumnNumber,
                                Colors.Red.WithAlpha(0.3),
                                laserLineCentricityItemDto.DarkMachineCenterPosition,
                                viewModel.Cache.WaferDiameter,
                                labelColor: Colors.Transparent);
                            ShowVectorField(WpfPlot,
                                viewModel.ResultChuckStageMapDto.CalibrationBrightFieldStageMap.IdealStageMapItemMatrix,
                                viewModel.ResultChuckStageMapDto.CalibrationBrightFieldStageMap.ErrorMatrix,
                                viewModel.ResultChuckStageMapDto.CalibrationBrightFieldStageMap.RowNumber,
                                viewModel.ResultChuckStageMapDto.CalibrationBrightFieldStageMap.ColumnNumber,
                                Colors.Gray.WithAlpha(0.3),
                                viewModel.ChuckCenter.NewBFCenterStagePosition,
                                viewModel.Cache.WaferDiameter,
                                false,
                                labelColor: Colors.Transparent);
                        });
                        break;
                }

                break;

            case nameof(viewModel.ReviewDto):
                switch (ChildName)
                {
                    case "ReviewCalibration":
                        Dispatcher.Invoke(() =>
                        {
                            if (viewModel.ReviewDto is null) return;

                            ShowVectorField(WpfPlot,
                                viewModel.ReviewDto.ExpandStageMapDto.IdealStageMapItemMatrix,
                                viewModel.ReviewDto.ExpandStageMapDto.ErrorMatrix,
                                viewModel.ReviewDto.ExpandStageMapDto.RowNumber,
                                viewModel.ReviewDto.ExpandStageMapDto.ColumnNumber,
                                Colors.Red.WithAlpha(0.3),
                                laserLineCentricityItemDto.DarkMachineCenterPosition,
                                viewModel.Cache.WaferDiameter,
                                labelColor: Colors.Transparent);
                            ShowVectorField(WpfPlot,
                                viewModel.ResultChuckStageMapDto.CalibrationBrightFieldStageMap.IdealStageMapItemMatrix,
                                viewModel.ResultChuckStageMapDto.CalibrationBrightFieldStageMap.ErrorMatrix,
                                viewModel.ResultChuckStageMapDto.CalibrationBrightFieldStageMap.RowNumber,
                                viewModel.ResultChuckStageMapDto.CalibrationBrightFieldStageMap.ColumnNumber,
                                Colors.Gray.WithAlpha(0.3),
                                viewModel.ChuckCenter.NewBFCenterStagePosition,
                                viewModel.Cache.WaferDiameter,
                                false,
                                labelColor: Colors.Transparent);
                        });
                        break;
                }

                break;

            case nameof(viewModel.ReviewDto.VerifyDarkFieldStageMap):
            case nameof(viewModel.ReviewDto.VerifyBrightFieldStageMap):
                switch (ChildName)
                {
                    case "ReviewVerify":
                        Dispatcher.Invoke(() =>
                        {
                            if (viewModel.ReviewDto is null) return;

                            ShowVectorField(WpfPlot,
                                viewModel.ReviewDto.VerifyDarkFieldStageMap.IdealStageMapItemMatrix,
                                viewModel.ReviewDto.VerifyDarkFieldStageMap.ErrorMatrix,
                                viewModel.ReviewDto.VerifyDarkFieldStageMap.RowNumber,
                                viewModel.ReviewDto.VerifyDarkFieldStageMap.ColumnNumber,
                                Colors.Red.WithAlpha(0.3),
                                laserLineCentricityItemDto.DarkMachineCenterPosition,
                                viewModel.Cache.WaferDiameter,
                                labelColor: Colors.Transparent);
                            ShowVectorField(WpfPlot,
                                viewModel.ReviewDto.VerifyBrightFieldStageMap.IdealStageMapItemMatrix,
                                viewModel.ReviewDto.VerifyBrightFieldStageMap.ErrorMatrix,
                                viewModel.ReviewDto.VerifyBrightFieldStageMap.RowNumber,
                                viewModel.ReviewDto.VerifyBrightFieldStageMap.ColumnNumber,
                                Colors.Gray.WithAlpha(0.3),
                                viewModel.ChuckCenter.NewBFCenterStagePosition,
                                viewModel.Cache.WaferDiameter,
                                false,
                                labelColor: Colors.Transparent);
                        }); break;
                }

                break;
        }
    }

    private static void ShowVectorField(
        WpfPlot wpfPlot,
        StageMapItemDto[][] mapIdealStageMapItemMatrix,
        Point[][] mapErrorMatrix,
        int rowNumber,
        int columnNumber,
        Color gridColor,
        Point centerPointOfCircle,
        double diameter,
        bool isRemoveAll = true,
        Color? labelColor = null)
    {
        try
        {
            if (isRemoveAll) wpfPlot.Plot.PlottableList.RemoveAll(t => t is Crosshair or Annotation == false);
            if (mapErrorMatrix.ElementAtOrDefault(0)?.ElementAtOrDefault(0) is null) return;
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
                    var backgroundColor = labelColor ?? (idealItem.IsInWafer == false
                        ? Colors.DarkRed
                        : idealItem.IsMatchOk == false
                            ? Colors.Green
                            : Colors.Transparent);

                    if (range is not null) vectors.Add(new RootedCoordinateVector(pt, v));

                    wpfPlot.Plot.PlottableList.Add(new WrapperErrorText
                    {
                        LabelText = $"({row + 1}, {column + 1})",
                        LabelBackgroundColor = backgroundColor,
                        Location = pt,
                        LabelFontSize = 10,
                        LabelPadding = 1,
                        LabelFontColor = labelColor ?? Colors.White,
                        LabelAlignment = Alignment.MiddleCenter,
                        Vector = v
                    });
                }
            }

            var ellipse = wpfPlot.Plot.Add.Circle(centerPointOfCircle.X, centerPointOfCircle.Y, diameter / 2d);
            ellipse.LineColor = Colors.DarkRed;
            ellipse.LineWidth = 2;

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

            var vf = wpfPlot.Plot.Add.VectorField(vectors);
            vf.Colormap = ColorMap;
        }
        finally
        {
            wpfPlot.Plot.Axes.AutoScale();
            wpfPlot.Refresh();
        }
    }

    private static WrapperErrorText? _lastText;

    private static void ConfigureWpfPlot(WpfPlot wpfPlot, ChuckStageMapCalibrationViewModel viewModel)
    {
        _logger ??= HostApplication.GetRequiredService<ILogger<DarkFieldMapView>>();

        wpfPlot.ConfigureWpfPlotCommon();

        wpfPlot.Plot.Title("Map");
        wpfPlot.Plot.HideAxesAndGrid();

        wpfPlot.Menu?.Reset();
        wpfPlot.Menu?.AddSeparator();
        wpfPlot.Menu?.Add("Goto Position", async void (_) =>
        {
            try
            {
                await Task.Run(() =>
                {
                    if (_lastText is null) return;

                    viewModel.StageViewModel.SetMachineAbsoluteStageXy(new Point(_lastText.Location.X, _lastText.Location.Y));
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Goto Position");
            }
        });

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