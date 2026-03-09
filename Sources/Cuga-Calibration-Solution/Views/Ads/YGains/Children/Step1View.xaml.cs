using CugaCalibration.ViewModels.Ads;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.WPF.Behaviors;
using System.ComponentModel;
using System.Windows;
using static Core.Models.Models.Ads.YGains.AdsYGainsCache;

namespace CugaCalibration.Views.Ads.YGains.Children;

public sealed partial class Step1View
{
    private bool _isLoaded;

    public Step1View()
    {
        InitializeComponent();
        Loaded -= OnLoaded;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;

        if (DataContext is not AdsYGainsCalibrationViewModel viewModel) return;

        viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not AdsYGainsCalibrationViewModel viewModel) return;
        switch (e.PropertyName)
        {
            case nameof(viewModel.SelectAdsYGainsCacheItem):
                Dispatcher.Invoke(() =>
                {
                    viewModel.PlotList = [];
                    var adsYGainsCacheItem = (AdsYGainsCacheItem)viewModel.GetType().GetProperty(e.PropertyName)!.GetValue(viewModel);
                    if (adsYGainsCacheItem == null) return;
                    if (adsYGainsCacheItem.IsPositive)
                    {
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("Z1", [.. adsYGainsCacheItem.GetPlotZ1().ToPoints()], null, [.. adsYGainsCacheItem.GetPointZ1()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("Z2", [.. adsYGainsCacheItem.GetPlotZ2().ToPoints()], null, [.. adsYGainsCacheItem.GetPointZ2()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("Z3", [.. adsYGainsCacheItem.GetPlotZ3().ToPoints()], null, [.. adsYGainsCacheItem.GetPointZ3()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("SmoothZ1", [.. adsYGainsCacheItem.GetSmoothPlotZ1().ToPoints()], null, [.. adsYGainsCacheItem.GetSmoothPointZ1()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("SmoothZ2", [.. adsYGainsCacheItem.GetSmoothPlotZ2().ToPoints()], null, [.. adsYGainsCacheItem.GetSmoothPointZ2()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("SmoothZ3", [.. adsYGainsCacheItem.GetSmoothPlotZ3().ToPoints()], null, [.. adsYGainsCacheItem.GetSmoothPointZ3()])];
                    }
                    else
                    {
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("Z4", [.. adsYGainsCacheItem.GetPlotZ1().ToPoints()], null, [.. adsYGainsCacheItem.GetPointZ1()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("Z5", [.. adsYGainsCacheItem.GetPlotZ2().ToPoints()], null, [.. adsYGainsCacheItem.GetPointZ2()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("Z6", [.. adsYGainsCacheItem.GetPlotZ3().ToPoints()], null, [.. adsYGainsCacheItem.GetPointZ3()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("SmoothZ4", [.. adsYGainsCacheItem.GetSmoothPlotZ1().ToPoints()], null, [.. adsYGainsCacheItem.GetSmoothPointZ1()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("SmoothZ5", [.. adsYGainsCacheItem.GetSmoothPlotZ2().ToPoints()], null, [.. adsYGainsCacheItem.GetSmoothPointZ2()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("SmoothZ6", [.. adsYGainsCacheItem.GetSmoothPlotZ3().ToPoints()], null, [.. adsYGainsCacheItem.GetSmoothPointZ3()])];
                    }
                });
                break;

            case nameof(viewModel.SelectAdsYGainsCacheConverseItem):
                Dispatcher.Invoke(() =>
                {
                    viewModel.PlotList = [];
                    var adsYGainsCacheItem = (AdsYGainsCacheItem)viewModel.GetType().GetProperty(e.PropertyName)!.GetValue(viewModel);
                    if (adsYGainsCacheItem == null) return;
                    if (adsYGainsCacheItem.IsPositive)
                    {
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("Z1", [.. adsYGainsCacheItem.GetPlotZ1().ToPoints()], null, [.. adsYGainsCacheItem.GetPointZ1()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("Z2", [.. adsYGainsCacheItem.GetPlotZ2().ToPoints()], null, [.. adsYGainsCacheItem.GetPointZ2()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("Z3", [.. adsYGainsCacheItem.GetPlotZ3().ToPoints()], null, [.. adsYGainsCacheItem.GetPointZ3()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("SmoothZ1", [.. adsYGainsCacheItem.GetSmoothPlotZ1().ToPoints()], null, [.. adsYGainsCacheItem.GetSmoothPointZ1()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("SmoothZ2", [.. adsYGainsCacheItem.GetSmoothPlotZ2().ToPoints()], null, [.. adsYGainsCacheItem.GetSmoothPointZ2()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("SmoothZ3", [.. adsYGainsCacheItem.GetSmoothPlotZ3().ToPoints()], null, [.. adsYGainsCacheItem.GetSmoothPointZ3()])];
                    }
                    else
                    {
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("Z4", [.. adsYGainsCacheItem.GetPlotZ1().ToPoints()], null, [.. adsYGainsCacheItem.GetPointZ1()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("Z5", [.. adsYGainsCacheItem.GetPlotZ2().ToPoints()], null, [.. adsYGainsCacheItem.GetPointZ2()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("Z6", [.. adsYGainsCacheItem.GetPlotZ3().ToPoints()], null, [.. adsYGainsCacheItem.GetPointZ3()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("SmoothZ4", [.. adsYGainsCacheItem.GetSmoothPlotZ1().ToPoints()], null, [.. adsYGainsCacheItem.GetSmoothPointZ1()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("SmoothZ5", [.. adsYGainsCacheItem.GetSmoothPlotZ2().ToPoints()], null, [.. adsYGainsCacheItem.GetSmoothPointZ2()])];
                        viewModel.PlotList = [.. viewModel.PlotList, new WpfPlotModel("SmoothZ6", [.. adsYGainsCacheItem.GetSmoothPlotZ3().ToPoints()], null, [.. adsYGainsCacheItem.GetSmoothPointZ3()])];
                    }
                });
                break;
        }
    }
}