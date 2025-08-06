using Core.Models.Models.Laser.XYAstigmatism;
using CugaCalibration.ViewModels.Laser;
using Microsoft.Extensions.Logging;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.Plottables;
using System.ComponentModel;
using System.Windows;

namespace CugaCalibration.Views.Laser.XYAstigmatism.Children;

public partial class Step3View
{
    private readonly ILogger<Step3View> _logger;
    private readonly Turbo _turbo = new();
    private bool _isLoaded;

    public Step3View()
    {
        InitializeComponent();
        _logger = HostApplication.GetRequiredService<ILogger<Step3View>>();

        Loaded -= OnLoaded;
        Loaded += OnLoaded;

        WpfPlotAodWaveSignalListOfWindow.Plot.Title("Wave Signal List of Window");
        WpfPlotAodWaveSignalFourierListOfWindow.Plot.Title("Wave Signal Fourier List of Window");
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;

        if (DataContext is not LaserXYAstigmatismCalibrationViewModel viewModel) return;

        viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;

        var aodWaveSignalText = WpfPlotAodWaveSignalListOfWindow.Plot.Add.Annotation(string.Empty, Alignment.UpperRight);
        aodWaveSignalText.LabelBackgroundColor = Colors.Yellow;
        aodWaveSignalText.IsVisible = false;

        var AodWaveSignalFourierText = WpfPlotAodWaveSignalFourierListOfWindow.Plot.Add.Annotation(string.Empty, Alignment.UpperRight);
        AodWaveSignalFourierText.LabelBackgroundColor = Colors.Green;
        AodWaveSignalFourierText.IsVisible = false;
    }

    private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (sender is not LaserXYAstigmatismCalibrationViewModel viewModel) return;

        switch (e.PropertyName)
        {
            case nameof(viewModel.Cache):
                viewModel.Cache.PropertyChanged -= CacheOnPropertyChanged;
                viewModel.Cache.PropertyChanged += CacheOnPropertyChanged;
                break;
        }
    }

    private void CacheOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        try
        {
            if (sender is not LaserXYAstigmatismCalibrationCache cache) return;

            //var isWindow = e.PropertyName switch
            //{
            //    nameof(cache.AodWaveSignal) or nameof(cache.AodWaveSignalFourier) => true,
            //    _ => throw new ArgumentOutOfRangeException()
            //};
            var name = e.PropertyName switch
            {
                nameof(cache.AodWaveSignal) => "Wave Signal",
                nameof(cache.AodWaveSignalFourier) => "Wave Signal Fourier",
                nameof(cache.EcsPlotList) => "Ecs Quality Result",
                _ => null
            };
            var color = e.PropertyName switch
            {
                nameof(cache.AodWaveSignal) => Colors.Red,
                nameof(cache.AodWaveSignalFourier) => Colors.DarkRed,
                nameof(cache.EcsPlotList) => Colors.Blue,
                _ => default
            };
            switch (e.PropertyName)
            {
                case nameof(cache.AodWaveSignal):
                    var plottable = WpfPlotAodWaveSignalListOfWindow.Plot.PlottableList
                        .OfType<Scatter>()
                        .SingleOrDefault(t => t.LegendText == name);
                    if (plottable is not null) WpfPlotAodWaveSignalListOfWindow.Plot.Remove(plottable);

                    var list = (List<(double, double)>)ObjectHelper.GetPropertyValue(cache, e.PropertyName)!;
                    if (list.Count == 0) return;

                    var plot = list.Select(t => (X: t.Item1, Y: t.Item2)).ToList();
                    var scatter = WpfPlotAodWaveSignalListOfWindow.Plot.Add.Scatter(
                        plot.Select(t => t.X).ToList(),
                        plot.Select(t => t.Y).ToList(),
                        color);
                    scatter.LegendText = name!;

                    Refresh(1);

                    break;

                case nameof(cache.AodWaveSignalFourier):
                    plottable = WpfPlotAodWaveSignalFourierListOfWindow.Plot.PlottableList
                        .OfType<Scatter>()
                        .SingleOrDefault(t => t.LegendText == name);
                    if (plottable is not null) WpfPlotAodWaveSignalFourierListOfWindow.Plot.Remove(plottable);

                    list = (List<(double, double)>)ObjectHelper.GetPropertyValue(cache, e.PropertyName)!;
                    if (list.Count == 0) return;

                    plot = [.. list.Select(t => (X: t.Item1, Y: t.Item2))];
                    scatter = WpfPlotAodWaveSignalFourierListOfWindow.Plot.Add.Scatter(
                        plot.Select(t => t.X).ToList(),
                        plot.Select(t => t.Y).ToList(),
                        color);
                    scatter.LegendText = name!;

                    Refresh(1, 1);

                    break;

                case nameof(cache.EcsPlotList):
                    plottable = WpfPlotResultDtoListOfWindow.Plot.PlottableList
                        .OfType<Scatter>()
                        .SingleOrDefault(t => t.LegendText == name);
                    if (plottable is not null) WpfPlotResultDtoListOfWindow.Plot.Remove(plottable);
                    list = (List<(double, double)>)ObjectHelper.GetPropertyValue(cache, e.PropertyName)!;
                    if (list.Count == 0) return;
                    plot = [.. list.Select(t => (X: t.Item1, Y: t.Item2))];
                    scatter = WpfPlotResultDtoListOfWindow.Plot.Add.Scatter(
                        plot.Select(t => t.X).ToList(),
                        plot.Select(t => t.Y).ToList(),
                        color);
                    scatter.LegendText = name!;

                    Refresh(0);

                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{@Name}: Plot Failed", nameof(Step3View));
        }
    }

    private void Refresh(int tableId, int windowId = 0)
    {
        Dispatcher.Invoke(() =>
        {
            TabControlDetails.SelectedIndex = 0;
            switch (tableId)
            {
                case 0:
                    WpfPlotResultDtoListOfWindow.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);
                    WpfPlotResultDtoListOfWindow.Plot.Axes.AutoScale();
                    WpfPlotResultDtoListOfWindow.Refresh();
                    break;

                case 1:
                    switch (windowId)
                    {
                        case 0:
                            WpfPlotAodWaveSignalListOfWindow.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);
                            WpfPlotAodWaveSignalListOfWindow.Plot.Axes.AutoScale();
                            WpfPlotAodWaveSignalListOfWindow.Refresh();
                            break;

                        case 1:
                            WpfPlotAodWaveSignalFourierListOfWindow.Plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);
                            WpfPlotAodWaveSignalFourierListOfWindow.Plot.Axes.AutoScale();
                            WpfPlotAodWaveSignalFourierListOfWindow.Refresh();
                            break;
                    }

                    break;
            }
        });
    }
}