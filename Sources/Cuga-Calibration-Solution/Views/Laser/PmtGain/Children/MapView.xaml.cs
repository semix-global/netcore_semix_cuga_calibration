using Core.Models.Models.Laser.PmtGain;
using CugaCalibration.ViewModels.Laser;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System.ComponentModel;
using System.Windows;

namespace CugaCalibration.Views.Laser.PmtGain.Children;

public sealed partial class MapView
{
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

        if (DataContext is not LaserPmtGainCalibrationViewModel viewModel) return;

        viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        var crossHair = WpfPlot.Plot.Add.Crosshair(0, 0);
        crossHair.LineColor = Colors.Red;
        crossHair.TextColor = Colors.White;
        crossHair.TextBackgroundColor = Colors.Red;
        WpfPlot.MouseMove += (sender, args) =>
        {
            if (sender is not WpfPlot plot) return;

            var position = args.GetPosition(plot);
            var mousePixel = new Pixel(position.X, position.Y);
            var mouseCoordinates = plot.Plot.GetCoordinates(mousePixel);

            crossHair.Position = mouseCoordinates;
            crossHair.VerticalLine.Text = $"{mouseCoordinates.X:f3}";
            crossHair.HorizontalLine.Text = $"{mouseCoordinates.Y:f3}";

            plot.Refresh();
        };
    }

    private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (sender is not LaserPmtGainCalibrationViewModel viewModel) return;
        switch (e.PropertyName)
        {
            case nameof(viewModel.SelectLaserPmtGainDto):
                //case nameof(viewModel.ReviewDto):
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var laserPmtGainDto = (LaserPmtGainDto)viewModel.GetType().GetProperty(e.PropertyName)!.GetValue(viewModel);
                        if (laserPmtGainDto == null) return;
                        WpfPlot.Plot.Title("PMT:" + laserPmtGainDto.PmtId.ToString() + " Channel:" + laserPmtGainDto.Channel.ToString());
                        WpfPlot.Plot.PlottableList.RemoveAll(t => t is Crosshair or Annotation == false);
                        if (laserPmtGainDto is null || laserPmtGainDto.Plot.Count <= 0) return;
                        var plotlists = laserPmtGainDto.Plot;
                        for (var j = 0; j < plotlists.Count; j++)
                        {
                            if (plotlists[j] != null)
                            {
                                var scatterPoints = WpfPlot.Plot.Add.Scatter(plotlists[j].VoltageLightListPoint.Select(t => t.X).ToList(), plotlists[j].VoltageLightListPoint.Select(t => t.Y).ToList(), Colors.Category10[j % Colors.Category10.Length]);
                                scatterPoints.LegendText = plotlists[j].MeasurePower.ToString();
                                scatterPoints.MarkerShape = (MarkerShape)(j + 1);
                            }
                        }

                        WpfPlot.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);
                        WpfPlot.Plot.Axes.AutoScale();
                        WpfPlot.Refresh();
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