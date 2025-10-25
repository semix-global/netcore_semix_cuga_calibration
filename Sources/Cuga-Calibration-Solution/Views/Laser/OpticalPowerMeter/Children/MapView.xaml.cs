using Core.Models.Models.Laser.OpticalPower;
using CugaCalibration.ViewModels.Laser;
using MathNet.Numerics.LinearAlgebra;
using ScottPlot;
using ScottPlot.DataSources;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System.ComponentModel;
using System.Windows;
using Range = ScottPlot.Range;
using Text = ScottPlot.Plottables.Text;

namespace CugaCalibration.Views.Laser.OpticalPowerMeter.Children;

public sealed partial class MapView
{
    private readonly IColormap _colorMap = new ScottPlot.Colormaps.Turbo();
    private bool _isLoaded;

    public MapView()
    {
        InitializeComponent();

        Loaded -= OnLoaded;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;

        if (DataContext is not LaserOpticalPowerMeterCalibrationViewModel viewModel) return;

        viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;

        WpfPlot.Plot.Title("Map");
        WpfPlot.Plot.HideAxesAndGrid();

        var highlightText = WpfPlot.Plot.Add.Annotation(string.Empty, Alignment.UpperRight);
        highlightText.LabelBackgroundColor = Colors.Yellow;
        highlightText.IsVisible = false;

        Text? lastMin = null;
        float? lastMinWidth = null;
        Color? lastMinColor = null;
        WpfPlot.MouseMove += (o, mouseEventArgs) =>
        {
            if (o is not WpfPlot plot) return;
            if (lastMin is not null && lastMinWidth is not null && lastMinColor is not null)
            {
                lastMin.LabelBorderWidth = lastMinWidth.Value;
                lastMin.LabelBorderColor = lastMinColor.Value;
            }

            var position = mouseEventArgs.GetPosition(plot);
            var mousePixel = new Pixel(position.X, position.Y);
            var mouseLocation = plot.Plot.GetCoordinates(mousePixel);

            var scatterSourceCoordinatesArray = new ScatterSourceCoordinatesArray([.. plot.Plot.PlottableList.OfType<Text>().Select(t => t.Location)]);
            var dataPoint = scatterSourceCoordinatesArray.GetNearest(mouseLocation, plot.Plot.LastRender);
            if (dataPoint.IsReal)
            {
                lastMin = plot.Plot.PlottableList.OfType<Text>().OrderBy(t => t.Location.Distance(dataPoint.Coordinates)).FirstOrDefault();
                if (lastMin is not null)
                {
                    lastMinColor = lastMin.LabelBorderColor;
                    lastMinWidth = lastMin.LabelBorderWidth;

                    lastMin.LabelBorderWidth = 3;
                    lastMin.LabelBorderColor = Colors.Green;
                }

                highlightText.IsVisible = true;
                highlightText.LabelText = $"{dataPoint.X:f5}, {dataPoint.Y:f5}";
            }
            else
            {
                highlightText.IsVisible = false;
            }

            mouseEventArgs.Handled = true;

            plot.Refresh();
        };
    }

    private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (sender is not LaserOpticalPowerMeterCalibrationViewModel viewModel) return;

        switch (e.PropertyName)
        {
            case nameof(viewModel.SelectedCalibratingItem):
            case nameof(viewModel.SelectedReviewItem):
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var selected = (LaserOpticalPowerDto)viewModel.GetType().GetProperty(e.PropertyName)!.GetValue(viewModel);

                        WpfPlot.Plot.PlottableList.RemoveAll(t => t is Crosshair or Annotation == false);
                        if (selected is null || selected.Map.Count <= 0) return;

                        var maximumIndex = Vector<double>.Build.DenseOfEnumerable(selected.Map.Select(t => t.MeasurePower)).MaximumIndex();

                        var measureMinPower = selected.Map.Min(t => t.MeasurePower);
                        var measureMaxPower = selected.Map.Max(t => t.MeasurePower);
                        foreach (var (i, laserOpticalPowerItemDto) in selected.Map.Select((t, i) => (i, t)))
                        {
                            var txt = new Text
                            {
                                LabelText = $"{laserOpticalPowerItemDto.MeasurePower:00.00000}",
                                LabelBackgroundColor = _colorMap.GetColor(laserOpticalPowerItemDto.MeasurePower, new Range(measureMinPower, measureMaxPower)),
                                LabelBorderColor = Colors.Transparent,
                                Location = new Coordinates(laserOpticalPowerItemDto.MeasurePosition.X, laserOpticalPowerItemDto.MeasurePosition.Y),
                                LabelFontSize = 12,
                                LabelPadding = 2,
                                LabelFontColor = Colors.White,
                                LabelAlignment = Alignment.MiddleCenter
                            };

                            WpfPlot.Plot.PlottableList.Insert(0, txt);
                            if (i != maximumIndex) continue;

                            txt.LabelBorderColor = Colors.OrangeRed;
                            txt.LabelBorderWidth = 5;
                            txt.LabelPadding = 5;
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