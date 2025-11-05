using Core.Models.Models.Chuck.AutoFocus;
using CugaCalibration.ViewModels.Chuck;
using Microsoft.Extensions.Logging;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.DataSources;
using ScottPlot.Interactivity;
using ScottPlot.Interactivity.UserActionResponses;
using ScottPlot.Plottables;
using System.ComponentModel;
using System.Windows;
using Range = ScottPlot.Range;

namespace CugaCalibration.Views.Chuck.AutoFocus.Children;

public sealed partial class MapView
{
    private readonly IColormap _colorMap = new ScottPlot.Colormaps.Turbo();
    private readonly ILogger<MapView> _logger;

    private bool _isLoaded;

    public MapView()
    {
        InitializeComponent();

        _logger = HostApplication.GetRequiredService<ILogger<MapView>>();

        Loaded -= OnLoaded;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;

        if (DataContext is not ChuckAutoFocusCalibrationViewModel viewModel) return;

        viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;

        Text? lastText = null;

        WpfPlot.Plot.Title("Map");
        WpfPlot.Plot.HideAxesAndGrid();
        var highlightText = WpfPlot.Plot.Add.Annotation(string.Empty, Alignment.UpperRight);
        highlightText.LabelBackgroundColor = Colors.Yellow;
        highlightText.IsVisible = false;

        WpfPlot.Menu?.AddSeparator();
        WpfPlot.Menu?.Add("Goto Position", async void (_) =>
        {
            try
            {
                await Task.Run(() =>
                {
                    if (lastText is null) return;

                    viewModel.StageViewModel.SetBrightFieldAbsoluteStageXy(new Net.Utilities.Models.Geometries.Point(lastText.Location.X, lastText.Location.Y));
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Goto Position");
            }
        });

        WpfPlot.UserInputProcessor.IsEnabled = true;
        WpfPlot.UserInputProcessor.UserActionResponses.Clear();

        WpfPlot.UserInputProcessor.UserActionResponses.Add(new MouseWheelZoom(StandardKeys.Shift, StandardKeys.Control)); // 滚轮: 缩放

        WpfPlot.UserInputProcessor.UserActionResponses.Add(new MouseDragZoomRectangle(StandardMouseButtons.Middle) { SecondaryMouseButton = new MouseButton(string.Empty), SecondaryKey = new Key(string.Empty) }); // 中间单击拖动: 矩形选择缩放
        WpfPlot.UserInputProcessor.UserActionResponses.Add(new SingleClickAutoscale(StandardMouseButtons.Middle)); // 中键单击: 自适应
        WpfPlot.UserInputProcessor.UserActionResponses.Add(new DoubleClickBenchmark(StandardMouseButtons.Middle)); // 中键双击: 性能测试

        WpfPlot.UserInputProcessor.UserActionResponses.Add(new SingleClickContextMenu(StandardMouseButtons.Right)); // 右键单击: 菜单
        WpfPlot.UserInputProcessor.UserActionResponses.Add(new MouseDragZoom(StandardMouseButtons.Right)); // 右键单击拖动: X or Y缩放

        WpfPlot.UserInputProcessor.UserActionResponses.Add(new MouseDragPan(StandardMouseButtons.Left)); // 左键拖动: 平移

        WpfPlot.UserInputProcessor.UserActionResponses.Add(new KeyboardPanAndZoom()); // 上下左右

        WpfPlot.UserInputProcessor.UserActionResponses.Add(new SingleClickResponse(StandardMouseButtons.Left, (plotControl, mousePixel) =>
        {
            var mouseLocation = plotControl.Plot.GetCoordinates(mousePixel);
            if (lastText is not null)
            {
                lastText.LabelBorderWidth = 0;
                lastText.LabelBorderColor = Colors.Transparent;
                lastText = null;
            }

            var scatterSourceCoordinatesArray = new ScatterSourceCoordinatesArray([.. plotControl.Plot.PlottableList.OfType<Text>().Select(t => t.Location)]);
            var nearest = scatterSourceCoordinatesArray.GetNearest(mouseLocation, plotControl.Plot.LastRender);
            if (nearest.IsReal)
            {
                lastText = plotControl.Plot.PlottableList.OfType<Text>().OrderBy(t => t.Location.Distance(nearest.Coordinates)).FirstOrDefault();
                if (lastText is not null)
                {
                    lastText.LabelBorderWidth = 3;
                    lastText.LabelBorderColor = Colors.Red;
                }

                highlightText.IsVisible = true;
                highlightText.LabelText = $"{nearest.X:f5}, {nearest.Y:f5}";
            }
            else
            {
                highlightText.IsVisible = false;
            }

            plotControl.Plot.PlotControl?.Refresh();
        }));
    }

    private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (sender is not ChuckAutoFocusCalibrationViewModel viewModel) return;

        switch (e.PropertyName)
        {
            case nameof(viewModel.ResultChuckAutoFocusDto):
            case nameof(viewModel.ReviewDto):
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var chuckAutoFocusDto = (ChuckAutoFocusDto)viewModel.GetType().GetProperty(e.PropertyName)!.GetValue(viewModel);
                        WpfPlot.Plot.PlottableList.RemoveAll(t => t is Crosshair or Annotation == false);
                        if (chuckAutoFocusDto is null || chuckAutoFocusDto.Map.Count <= 0) return;

                        var ellipse = WpfPlot.Plot.Add.Circle(0, 0, chuckAutoFocusDto.ChuckDiameter / 2d);
                        ellipse.LineColor = Colors.DarkRed;
                        ellipse.LineWidth = 2;

                        var tempList = chuckAutoFocusDto.Map
                            .Select(t => t.EcsValue)
                            .Where(t => t > 0)
                            .ToList();
                        var min = tempList.Count == 0 ? 0 : tempList.Min();
                        var max = tempList.Count == 0 ? 0 : tempList.Max();

                        foreach (var laserOpticalPowerItemDto in chuckAutoFocusDto.Map)
                        {
                            WpfPlot.Plot.PlottableList.Insert(0, new Text
                            {
                                LabelText = $"({laserOpticalPowerItemDto.Row}, {laserOpticalPowerItemDto.Column}){Environment.NewLine}{laserOpticalPowerItemDto.EcsValue:00000}",
                                LabelBackgroundColor = _colorMap.GetColor(laserOpticalPowerItemDto.EcsValue, new Range(min, max)),
                                LabelBorderWidth = (laserOpticalPowerItemDto.Row, laserOpticalPowerItemDto.Column) == viewModel.CurrentRowColumn ? 3 : 0,
                                LabelBorderColor = (laserOpticalPowerItemDto.Row, laserOpticalPowerItemDto.Column) == viewModel.CurrentRowColumn ? Colors.Blue : Colors.Transparent,
                                Location = new Coordinates(laserOpticalPowerItemDto.Position.X, laserOpticalPowerItemDto.Position.Y),
                                LabelFontSize = 10,
                                LabelPadding = 1,
                                LabelFontColor = Colors.White,
                                LabelAlignment = Alignment.MiddleCenter
                            });
                        }
                    }
                    finally
                    {
                        WpfPlot.Plot.Axes.AutoScale();
                        WpfPlot.Refresh();
                    }
                });
                break;
        }
    }
}