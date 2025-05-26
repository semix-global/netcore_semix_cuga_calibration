using CommunityToolkit.Diagnostics;
using Core.Models.Models.Common.Recipe;
using CugaCalibration.ViewModels.Common.Windows.Management.Recipe;
using Microsoft.Extensions.Logging;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.DataSources;
using ScottPlot.Interactivity;
using ScottPlot.Interactivity.UserActionResponses;
using ScottPlot.Plottables;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using Colors = ScottPlot.Colors;
using Rectangle = ScottPlot.Plottables.Rectangle;
using Text = ScottPlot.Plottables.Text;

namespace CugaCalibration.Views.Common.Windows.Management.Recipe.Children;

public partial class WaferMapView
{
    private readonly ILogger<WaferMapView> _logger;
    private bool _isLoaded;

    public WaferMapView()
    {
        InitializeComponent();

        _logger = HostApplication.GetRequiredService<ILogger<WaferMapView>>();

        Loaded -= OnLoaded;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not RecipeSettingViewModel viewModel) return;
        var recipeDto = (CalibrationRecipeDto)viewModel.GetType().GetProperty(nameof(CalibrationRecipeDto))!.GetValue(viewModel);
        var waferDto = recipeDto.WaferDto;
        if (_isLoaded) return;
        _isLoaded = true;

        viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;

        Text? lastText = null;

        WaferMapWpfPlot.Plot.Title("WaferMap");
        WaferMapWpfPlot.Plot.HideAxesAndGrid();
        var highlightText = WaferMapWpfPlot.Plot.Add.Annotation(string.Empty, Alignment.UpperRight);
        highlightText.LabelBackgroundColor = Colors.Yellow;
        highlightText.IsVisible = false;

        WaferMapWpfPlot.Menu?.AddSeparator();
        WaferMapWpfPlot.Menu?.Add("Goto Position", async void (_) =>
        {
            try
            {
                await Task.Run(() =>
                {
                    if (lastText is null) return;
                    viewModel.StageViewModel.SetBrightFieldAbsoluteStageXy(new Net.Utilities.Models.Point(lastText.Location.X + waferDto.WaferCenterBrightFieldPosition!.Value.X, lastText.Location.Y + waferDto.WaferCenterBrightFieldPosition!.Value.Y));
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Goto Position");
            }
        });

        WaferMapWpfPlot.Menu?.AddSeparator();

        WaferMapWpfPlot.UserInputProcessor.IsEnabled = true;
        WaferMapWpfPlot.UserInputProcessor.UserActionResponses.Clear();

        WaferMapWpfPlot.UserInputProcessor.UserActionResponses.Add(new MouseWheelZoom(StandardKeys.Shift, StandardKeys.Control)); // 滚轮: 缩放

        WaferMapWpfPlot.UserInputProcessor.UserActionResponses.Add(new MouseDragZoomRectangle(StandardMouseButtons.Middle) { SecondaryMouseButton = new MouseButton(string.Empty), SecondaryKey = new Key(string.Empty) }); // 中间单击拖动: 矩形选择缩放
        WaferMapWpfPlot.UserInputProcessor.UserActionResponses.Add(new SingleClickAutoscale(StandardMouseButtons.Middle)); // 中键单击: 自适应
        WaferMapWpfPlot.UserInputProcessor.UserActionResponses.Add(new DoubleClickBenchmark(StandardMouseButtons.Middle)); // 中键双击: 性能测试

        WaferMapWpfPlot.UserInputProcessor.UserActionResponses.Add(new SingleClickContextMenu(StandardMouseButtons.Right)); // 右键单击: 菜单
        WaferMapWpfPlot.UserInputProcessor.UserActionResponses.Add(new MouseDragZoom(StandardMouseButtons.Right)); // 右键单击拖动: X or Y缩放

        WaferMapWpfPlot.UserInputProcessor.UserActionResponses.Add(new MouseDragPan(StandardMouseButtons.Left)); // 左键拖动: 平移
        WaferMapWpfPlot.UserInputProcessor.UserActionResponses.Add(new KeyboardPanAndZoom()); // 上下左右

        WaferMapWpfPlot.UserInputProcessor.UserActionResponses.Add(new SingleClickResponse(StandardMouseButtons.Left, (plotControl, mousePixel) =>
        {
            var mouseLocation = plotControl.Plot.GetCoordinates(mousePixel);
            if (lastText is not null)
            {
                lastText.LabelBorderWidth = 0;
                lastText.LabelBorderColor = Colors.Transparent;
                lastText = null;
            }

            var scatterSourceCoordinatesArray = new ScatterSourceCoordinatesArray([.. plotControl.Plot.PlottableList.OfType<Rectangle>().Select(t => new Coordinates(t.X1, t.Y1))]);

            var nearest = scatterSourceCoordinatesArray.GetNearest(mouseLocation, plotControl.Plot.LastRender);
            if (nearest.IsReal)
            {
                lastText = plotControl.Plot.PlottableList.OfType<Text>().OrderBy(t => t.Location.Distance(nearest.Coordinates)).FirstOrDefault();
                var vectorField = plotControl.Plot.PlottableList.OfType<VectorField>().SingleOrDefault();
                var textList = new List<string>();

                if (lastText is not null)
                {
                    lastText.LabelBorderWidth = 3;
                    lastText.LabelBorderColor = Colors.Red;

                    highlightText.IsVisible = true;
                    textList.Add(lastText.LabelText);
                }

                if (vectorField is not null)
                {
                    // 获取非公开的属性
                    var property = vectorField.GetType().GetProperty("Source", BindingFlags.NonPublic | BindingFlags.Instance);
                    Guard.IsNotNull(property, nameof(property));
                    var vectorFieldSource = (IVectorFieldSource)property.GetValue(vectorField);
                    Guard.IsNotNull(vectorFieldSource, nameof(vectorFieldSource));

                    var vector = vectorFieldSource.GetRootedVectors().Single(t => t.Point == nearest.Coordinates);
                    textList.Add($"Error: ({vector.Vector.X:f5}, {vector.Vector.Y:f5})");
                }

                textList.Add($"Wafer Position: {nearest.X:f5}, {nearest.Y:f5}");
                textList.Add($"Bright Position: {nearest.X + waferDto.WaferCenterBrightFieldPosition!.Value.X:f5}" +
                             $", {nearest.Y + waferDto.WaferCenterBrightFieldPosition!.Value.Y:f5}");

                highlightText.LabelText = string.Join(Environment.NewLine, textList);
            }
            else
            {
                highlightText.IsVisible = false;
            }

            plotControl.Plot.PlotControl?.Refresh();

            //WaferMapWpfPlot.MouseMove += (sender, e) =>
            //{
            //    if (sender is not WpfPlot plot) return;

            //    var position = e.GetPosition(plot);
            //    var mousePixel = new Pixel(position.X, position.Y);
            //    var mouseLocation = plot.Plot.GetCoordinates(mousePixel);

            //    var nearestList = plot.Plot.PlottableList
            //        .OfType<Scatter>()
            //        .Select(t => t.Data.GetNearest(mouseLocation, plot.Plot.LastRender))
            //        .Where(t => t.IsReal)
            //        .ToList();

            //    if (nearestList.Count > 0 && nearestList[0].IsReal)
            //    {
            //        highlightText.IsVisible = true;
            //        highlightText.LabelText = $"{nearestList[0].X:f3}, {nearestList[0].Y:f3}";
            //    }
            //    else
            //    {
            //        highlightText.IsVisible = false;
            //    }

            //    e.Handled = true;
            //    plot.Refresh();
            //};
        }));
    }

    private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (sender is not RecipeSettingViewModel viewModel) return;

        switch (e.PropertyName)
        {
            case nameof(viewModel.CalibrationRecipeDto):
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var recipeDto = (CalibrationRecipeDto)viewModel.GetType().GetProperty(e.PropertyName)!.GetValue(viewModel);
                        var waferMapDto = recipeDto.WaferDto.WaferMapDto;

                        WaferMapWpfPlot.Plot.PlottableList.RemoveAll(t => t is Crosshair or Annotation == false);

                        var ellipse = WaferMapWpfPlot.Plot.Add.Circle(0, 0, waferMapDto.WaferMapData.WaferDiameter / 2d);
                        ellipse.LineColor = Colors.DarkRed;
                        ellipse.LineWidth = 2;

                        float originMaxLengthPixel = 30;

                        var positionMarker1 = WaferMapWpfPlot.Plot.Add.Marker(0, 0, shape: MarkerShape.Cross, size: originMaxLengthPixel * 3, color: Colors.Red);
                        positionMarker1.LineWidth = 2;

                        var positionMarker2 = WaferMapWpfPlot.Plot.Add.Marker(0, 0, shape: MarkerShape.OpenCircle, size: originMaxLengthPixel * 1.5f, color: Colors.Red);
                        positionMarker2.LineWidth = 2;

                        var originPositionMarker1 = WaferMapWpfPlot.Plot.Add.Marker(waferMapDto.OriginDieDto.WaferPosition.X, waferMapDto.OriginDieDto.WaferPosition.Y, shape: MarkerShape.Cross, size: originMaxLengthPixel * 3, color: Colors.DarkBlue);
                        originPositionMarker1.LineWidth = 2;

                        var originPositionMarker2 = WaferMapWpfPlot.Plot.Add.Marker(waferMapDto.OriginDieDto.WaferPosition.X, waferMapDto.OriginDieDto.WaferPosition.Y, shape: MarkerShape.OpenCircle, size: originMaxLengthPixel * 1.5f, color: Colors.DarkBlue);
                        originPositionMarker2.LineWidth = 2;

                        var waferMapDieDtoItemList = waferMapDto.WaferMapDieDtoItemList;
                        var waferMapReticleDieDtoItemList = waferMapDto.WaferMapReticleDieDtoItemList;

                        for (var row = 0; row < waferMapDieDtoItemList.Count; row++)
                        {
                            for (var column = 0; column < waferMapDieDtoItemList[row].Count; column++)
                            {
                                var cellDieItem = waferMapDieDtoItemList[row][column];

                                var pt = new Coordinates(cellDieItem.WaferPosition.X, cellDieItem.WaferPosition.Y);
                                var labelBackgroundColor = cellDieItem.IsInWafer == false
                                    ? Colors.DarkRed
                                    : Colors.DarkGray;

                                WaferMapWpfPlot.Plot.PlottableList.Add(new Rectangle()
                                {
                                    X1 = pt.X,
                                    X2 = pt.X + waferMapDto.WaferMapData.DiePitchWidth,
                                    Y1 = pt.Y,
                                    Y2 = pt.Y - waferMapDto.WaferMapData.DiePitchHeight,
                                    LineColor = labelBackgroundColor,
                                    FillStyle = new FillStyle()
                                    {
                                        Color = Colors.Transparent,
                                    }
                                });
                                if (viewModel.IsReticleMode == false)
                                {
                                    WaferMapWpfPlot.Plot.PlottableList.Add(new Text
                                    {
                                        LabelText = $"({row}, {column})",
                                        LabelBackgroundColor = labelBackgroundColor,
                                        Location = pt,
                                        LabelFontSize = 20,
                                        LabelPadding = 1,
                                        LabelFontColor = Colors.White,
                                        LabelAlignment = Alignment.MiddleCenter
                                    });
                                }
                            }
                        }

                        if (viewModel.IsReticleMode)
                        {
                            for (var row = 0; row < waferMapReticleDieDtoItemList.Count; row++)
                            {
                                for (var column = 0; column < waferMapReticleDieDtoItemList[row].Count; column++)
                                {
                                    var cellDieItem = waferMapReticleDieDtoItemList[row][column];

                                    var pt = new Coordinates(cellDieItem.WaferPosition.X, cellDieItem.WaferPosition.Y);
                                    var labelBackgroundColor = cellDieItem.IsInWafer == false
                                        ? Colors.DarkRed
                                        : Colors.Blue;

                                    WaferMapWpfPlot.Plot.PlottableList.Add(new Rectangle()
                                    {
                                        X1 = pt.X,
                                        X2 = pt.X + waferMapDto.WaferMapData.ReticleWidth,
                                        Y1 = pt.Y,
                                        Y2 = pt.Y - waferMapDto.WaferMapData.ReticleHeight,
                                        LineColor = labelBackgroundColor,
                                        LineWidth = 3,
                                        FillStyle = new FillStyle()
                                        {
                                            Color = Colors.Transparent,
                                        }
                                    });

                                    WaferMapWpfPlot.Plot.PlottableList.Add(new Text
                                    {
                                        LabelText = $"({row + 1}, {column + 1})",
                                        LabelBackgroundColor = labelBackgroundColor,
                                        Location = pt,
                                        LabelFontSize = 20,
                                        LabelPadding = 1,
                                        LabelFontColor = Colors.White,
                                        LabelAlignment = Alignment.MiddleCenter
                                    });
                                }
                            }
                        }
                    }
                    finally
                    {
                        WaferMapWpfPlot.Plot.Axes.AutoScale();
                        WaferMapWpfPlot.Refresh();
                    }
                });
                break;
        }
    }
}